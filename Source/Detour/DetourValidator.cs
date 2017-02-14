using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

namespace HugsLib.Source.Detour {
	/**
	 * Utility methods for ensuring that the source-destination pair of a detour is compatible and won't cause issues.
	 * The following code was provided by 1000101 and Zhentar.
	 */
	public static class DetourValidator {
		/// <summary>
		/// This classification is used to validate detours and to get the appropriate
		/// operating context of methods.
		/// A method should never be classed as "invalid", this classification only happens
		/// when a method has the "ExtensionAttribute" but no parameters and therefore no
		/// "this" parameter.
		/// </summary>
		private enum MethodType {
			Invalid,
			Instance,
			Extension,
			Static
		}
		
		private static string failReason;

		public static bool IsValidDetourPair(MethodInfo sourceMethod, MethodInfo destinationMethod) {
			failReason = null;
			if (sourceMethod == null) {
				ExplainFailure("Source MethodInfo is null", null, destinationMethod);
				return false;
			}
			if (destinationMethod == null) {
				ExplainFailure("Destination MethodInfo is null", sourceMethod, null);
				return false;
			}
			// check for instance fields in the destination declaring type
			if (destinationMethod.DeclaringType != null && !DetourContainerClassIsFieldSafe(destinationMethod.DeclaringType)) {
				ExplainFailure("Destination type contains non-static fields. This can have unpredictable and game-breaking side effects.", sourceMethod, destinationMethod);
				return false;
			}
			// check for matching parameters and return type in source and destination
			string reason;
			if (!MethodsAreCallCompatible(GetMethodTargetClass(sourceMethod), sourceMethod, GetMethodTargetClass(destinationMethod), destinationMethod, out reason)) {
				ExplainFailure(string.Format("Methods are not call compatible: {0}", reason), sourceMethod, destinationMethod);
				return false;
			}
			return true;
		}

		public static string GetLastError() {
			return failReason;
		}

		/// <summary>
		/// Checks that the class containing a detour does not contain instance fields.
		/// </summary>
		/// <returns>True if there are no instance fields; False if any instance field is contained in the class</returns>
		/// <param name="detourContainerClass">Detour container class</param>
		private static bool DetourContainerClassIsFieldSafe(Type detourContainerClass) {
			var fields = detourContainerClass.GetFields(Helpers.AllBindingFlags);
			if (fields.NullOrEmpty()) {
				// No fields, no worries
				return true;
			}
			
			string[] baseFields;
			if (detourContainerClass.BaseType != null) {
				// select non-private field names in base type
				baseFields = detourContainerClass.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => !f.IsPrivate).Select(finfo => finfo.Name).ToArray();
			} else {
				baseFields = new string[0];
			}
			
            //Make sure all instance fields were inherited from the parent class
            return fields.All(f => f.IsStatic || baseFields.Contains(f.Name));
		}

		/// <summary>
		/// Validates that two methods are call compatible
		/// </summary>
		/// <returns>True if the methods are call compatible; False and reason string set otherwise</returns>
		/// <param name="sourceTargetClass">Source method target class</param>
		/// <param name="sourceMethod">Source method</param>
		/// <param name="destinationTargetClass">Destination method target class</param>
		/// <param name="destinationMethod">Destination method</param>
		/// <param name="reason">Return string with reason for failure</param>
		private static bool MethodsAreCallCompatible(Type sourceTargetClass, MethodInfo sourceMethod, Type destinationTargetClass, MethodInfo destinationMethod, out string reason) {
			reason = string.Empty;
			if (sourceMethod.ReturnType != destinationMethod.ReturnType) {   // Return types don't match
				reason = string.Format("Return type mismatch :: Source={1}, Destination={0}",
					sourceMethod.ReturnType.Name, destinationMethod.ReturnType.Name
				);
				return false;
			}

			// Get the method types
			var sourceMethodType = GetMethodType(sourceMethod);
			var destinationMethodType = GetMethodType(destinationMethod);

			// Make sure neither method is invalid
			if (sourceMethodType == MethodType.Invalid) {
				reason = "Source method is not an instance, valid extension, or static method";
				return false;
			}
			if (destinationMethodType == MethodType.Invalid) {
				reason = "Destination method is not an instance, valid extension, or static method";
				return false;
			}

			// Check validity of target classes for 
			if (!DetourTargetsAreValid(
				sourceTargetClass, sourceMethodType, FullMethodName(sourceMethod),
				destinationTargetClass, destinationMethodType, FullMethodName(destinationMethod),
				out reason)) {
				return false;
			}

			// Method types and targets are all valid, now check the parameter lists
			var sourceParamsBase = sourceMethod.GetParameters();
			var destinationParamsBase = destinationMethod.GetParameters();

			// Get the first parameter index that isn't "this"
			var sourceParamBaseIndex = sourceMethodType == MethodType.Extension ? 1 : 0;
			var destinationParamBaseIndex = destinationMethodType == MethodType.Extension ? 1 : 0;

			// Parameter counts less "this"
			var sourceParamCount = sourceParamsBase.Length - sourceParamBaseIndex;
			var destinationParamCount = destinationParamsBase.Length - destinationParamBaseIndex;

			// Easy check that they have the same number of parameters
			if (sourceParamCount != destinationParamCount) {
				reason = "Parameter count mismatch";
				return false;
			}

			// Pick smaller parameter count (to skip "this")
			var paramCount = sourceParamCount > destinationParamCount ? destinationParamCount : sourceParamCount;

			// Now examine parameter-for-parameter
			if (paramCount > 0) {
				for (var offset = 0; offset < paramCount; offset++) {
					// Get parameter
					var sourceParam = sourceParamsBase[sourceParamBaseIndex + offset];
					var destinationParam = destinationParamsBase[destinationParamBaseIndex + offset];

					// Parameter types and attributes are all we care about
					if (
						(sourceParam.ParameterType != destinationParam.ParameterType) ||
						(sourceParam.Attributes != destinationParam.Attributes)
					) {   // Parameter type mismatch
						reason = string.Format(
							"Parameter type mismatch at index {6} :: Source='{0}', type='{1}', attributes='{2}'; Destination='{3}', type='{4}', attributes='{5}'",
							sourceParam.Name, sourceParam.ParameterType.FullName, sourceParam.Attributes,
							destinationParam.Name, destinationParam.ParameterType.FullName, destinationParam.Attributes,
							offset
						);
						return false;
					}
				}
			}

			// Methods are call compatible!
			return true;
		}

		/// <summary>
		/// Return the type of method from the MethodInfo
		/// </summary>
		/// <returns>MethodType of method</returns>
		/// <param name="methodInfo">MethodInfo of method</param>
		private static MethodType GetMethodType(MethodInfo methodInfo) {
			if (!methodInfo.IsStatic) {
				return MethodType.Instance;
			}
			if (methodInfo.IsDefined(typeof (ExtensionAttribute), false)) {
				return (!methodInfo.GetParameters().NullOrEmpty())
					? MethodType.Extension
					: MethodType.Invalid;
			}
			return MethodType.Static;
		}

		/// <summary>
		/// Checks that B is a valid detour for A based on method types and class context (targets)
		/// Modified (relaxed restrictions) to allow detouring instance to instance methods.
		/// </summary>
		/// <returns>True if B is a valid detour for A; False and reason string set otherwise</returns>
		/// <param name="targetA">Target class of A</param>
		/// <param name="typeA">MethodType of A</param>
		/// <param name="nameA">Method name of A</param>
		/// <param name="targetB">Target class of B</param>
		/// <param name="typeB">MethodType of B</param>
		/// <param name="nameB">Method name of B</param>
		/// <param name="reason">Return string with reason for failure</param>
		private static bool DetourTargetsAreValid(Type targetA, MethodType typeA, string nameA, Type targetB, MethodType typeB, string nameB, out string reason) {
			if (((typeA == MethodType.Instance) || (typeA == MethodType.Extension)) && typeB == MethodType.Extension) {
				if (typeB == MethodType.Static) {
					reason = string.Format("'{0}' is static but not an extension method", nameB);
					return false;
				} else if (targetB != null && !targetB.IsAssignableFrom(targetA)) {
					reason = string.Format("Target classes do not match :: '{0}' target is '{1}'; '{2}' target is '{3}'",
						nameA, FullNameOfType(targetA), nameB, FullNameOfType(targetB));
					return false;
				}
			}
			reason = string.Empty;
			return true;
		}

		/// <summary>
		/// Return the full class and method name with optional method address
		/// </summary>
		/// <returns>Full class and name</returns>
		/// <param name="methodInfo">MethodInfo of method</param>
		/// <param name="withAddress">Optional bool flag to add the address</param>
		private static string FullMethodName(MethodInfo methodInfo, bool withAddress = false) {
			if (methodInfo.DeclaringType == null) {
				return methodInfo.Name;
			}
			var rVal = methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
			if (withAddress) {
				rVal += " @ 0x" + methodInfo.MethodHandle.GetFunctionPointer().ToString("X" + (IntPtr.Size * 2).ToString());
			}
			return rVal;
		}

		/// <summary>
		/// Gets the class that the method will operate in the context of.
		/// This is NOT necessarily the class that the method exists in.
		/// For extension methods the "this" parameter (first) will be returned
		/// regardless of whether it is a detour or not, pure static methods
		/// will return null (again, regardless of being a detour) as they
		/// don't operate in the context of class.  Instance methods will
		/// return the defining class for non-detours and the class being
		/// injected into for detours.
		/// </summary>
		/// <returns>The method target class</returns>
		/// <param name="info">MethodInfo of the method to check</param>
		private static Type GetMethodTargetClass(MethodInfo info) {
			var methodType = GetMethodType(info);
			if (methodType == MethodType.Static) {   // Pure static methods don't have a target class
				return null;
			}
			if (methodType == MethodType.Extension) {   // Regardless of whether this is the detour method or the method to be detoured, for extension methods we take the target class from the first parameter
				return info.GetParameters()[0].ParameterType;
			}
			return info.DeclaringType;
		}

		/// <summary>
		/// null safe method to get the full name of a type for debugging
		/// </summary>
		/// <returns>The name of type or "null"</returns>
		/// <param name="type">Type to get the full name of</param>
		private static string FullNameOfType(Type type) {
			return type == null ? "null" : type.FullName;
		}

		private static void ExplainFailure(string errorMessage, MethodInfo sourceMethod, MethodInfo destinationMethod) {
			failReason = string.Format("Dangerous detour detected! {0}. Reason: {1}",
				DetourProvider.DetourPairToString(sourceMethod, destinationMethod), errorMessage);
		}
	}
}