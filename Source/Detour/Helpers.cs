using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HugsLib.Source.Attrib;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Source.Detour {
	public static class Helpers {
		public const BindingFlags AllBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private struct DetourPair {
			public readonly MethodInfo source;
			public readonly MethodInfo destination;

			public DetourPair(MethodInfo source, MethodInfo destination) {
				this.source = source;
				this.destination = destination;
			}
		}

		/**
		 * Called by AttributeDetector for every method marked with DetourFallbackAttribute
		 * Validates detour fallback handlers to make sure they will work properly when called
		 */
		[DetectableAttributeHandler(typeof (DetourFallbackAttribute))]
		private static void ValidateFallbackHandler(MemberInfo info, Attribute attrib) {
			if (!Prefs.DevMode) return; // skip unnecessary overhead
			var handler = info as MethodInfo;
			var attribute = attrib as DetourFallbackAttribute;
			if(handler == null || attribute == null) throw new Exception("Null or unexpected argument types!");
			// check is static
			if (!handler.IsStatic) HugsLibController.Logger.Error("Detour fallback handlers must be static ({0})", handler.FullName());
			// check signature
			if (!handler.MethodMatchesSignature(typeof(void), typeof(MemberInfo), typeof(MethodInfo), typeof(Exception))) {
				HugsLibController.Logger.Error(
					"Improper Detour fallback handlers signature ({0}). Expected signature: {1}", handler.FullName(), DetourFallbackAttribute.ExpectedSignature);
			}
			// check for referenced methods
			var declaringType = handler.DeclaringType;
			if (declaringType == null) throw new Exception("Null DeclaringType on handler");
			var members = declaringType.GetMembers(AllBindingFlags);
			foreach (var memberName in attribute.targetMemberNames) {
				if (members.All(m => m.Name != memberName)) {
					HugsLibController.Logger.Error("Detour fallback handler references missing member ({0}): {1}", handler.FullName(), memberName);
				}
			}
		}

		// called by AttributeDetector for every method marked with DetourMethodAttribute
		[DetectableAttributeHandler(typeof (DetourMethodAttribute))]
		private static void DetourMethodByAttribute(MemberInfo info, Attribute attrib) {
			var detourAttribute = attrib as DetourMethodAttribute;
			var destination = info as MethodInfo;
			if (destination == null || detourAttribute == null) throw new Exception("Null or unexpected argument types!");
			var pair = new DetourPair();
			try {
				try {
					pair = GetDetourPairFromAttribute(detourAttribute, destination);
					DetourProvider.CompatibleDetourWithExceptions(pair.source, pair.destination);
				} catch (Exception e) {
					DetourProvider.ThrowClearerDetourException(e, pair.source, info, "method");
				}
			} catch (Exception e) {
				if (!TryCallDetourFallbackHandler(pair.source, destination, e)) {
					HugsLibController.Logger.ReportException(e);
				}
			}
		}

		// called by AttributeDetector for every property marked with DetourPropertyAttribute
		[DetectableAttributeHandler(typeof(DetourPropertyAttribute))]
		private static void DetourPropertyByAttribute(MemberInfo info, Attribute attrib) {
			var detourAttribute = attrib as DetourPropertyAttribute;
			var destination = info as PropertyInfo;
			if (destination == null || detourAttribute == null) throw new Exception("Null or unexpected argument types!");
			var pair = new DetourPair();
			try {
				try {
					var pairs = GetDetourPairsFromAttribute(detourAttribute, destination);
					foreach (var detourPair in pairs) {
						pair = detourPair;
						DetourProvider.CompatibleDetourWithExceptions(pair.source, pair.destination);
					}
				} catch (Exception e) {
					DetourProvider.ThrowClearerDetourException(e, pair.source, info, "property");
				}
			} catch (Exception e) {
				if (!TryCallDetourFallbackHandler(pair.source, destination, e)) {
					HugsLibController.Logger.ReportException(e);
				}
			}
		}

		// get source and destination MethodInfo-s from DetourMethodAttribute
		private static DetourPair GetDetourPairFromAttribute(DetourMethodAttribute sourceAttribute, MethodInfo destinationInfo) {
			// we need to get the method info of the source (usually, vanilla) method. 
			// if it was specified in the attribute, this is easy. Otherwise, we'll have to do some digging.
			var sourceInfo = sourceAttribute.WasSetByMethodInfo
				? sourceAttribute.sourceMethodInfo
				: GetMatchingMethodInfo(sourceAttribute, destinationInfo);

			// make sure we've got what we wanted.
			if (sourceInfo == null)
				throw new NullReferenceException("sourceMethodInfo could not be found based on attribute");
			if (destinationInfo == null)
				throw new ArgumentNullException("destinationInfo");
			return new DetourPair(sourceInfo, destinationInfo);
		}

		// get source and destination MethodInfo-s from DetourPropertyAttribute
		private static IEnumerable<DetourPair> GetDetourPairsFromAttribute(DetourPropertyAttribute sourceAttribute, PropertyInfo destinationInfo) {
			// first, lets get the source propertyInfo - there's no ambiguity here.
			var sourceInfo = sourceAttribute.sourcePropertyInfo;
			// if getter was flagged (so Getter | Both )
			if ((sourceAttribute.detourProperty & DetourProperty.Getter) == DetourProperty.Getter) {
				yield return new DetourPair(sourceInfo.GetGetMethod(true), destinationInfo.GetGetMethod(true));
			}
			// if setter was flagged
			if ((sourceAttribute.detourProperty & DetourProperty.Setter) == DetourProperty.Setter) {
				yield return new DetourPair(sourceInfo.GetSetMethod(true), destinationInfo.GetSetMethod(true));
			}
		}

		private static bool TryCallDetourFallbackHandler(MethodInfo attemptedSource, MemberInfo attemptedDestination, Exception detourException) {
			if (attemptedDestination.DeclaringType == null) return false;
			// find a matching handler method
			var handlers = attemptedDestination.DeclaringType.GetMethods(AllBindingFlags).Where(m => m.TryGetAttributeSafely<DetourFallbackAttribute>() != null);
			MethodInfo matchingHandler = null;
			foreach (var handler in handlers) {
				var attribute = handler.TryGetAttributeSafely<DetourFallbackAttribute>();
				if (attribute == null || (attribute.targetMemberNames.Length > 0 && !attribute.targetMemberNames.Contains(attemptedDestination.Name))) continue;
				matchingHandler = handler;
				break;
			}
			if (matchingHandler == null) return false;
			// try get the method the detour was already routed to
			MethodInfo existingDestination = null;
			if (attemptedSource != null) {
				existingDestination = DetourProvider.TryGetExistingDetourDestination(attemptedSource);
			}
			// call the handler
			try {
				matchingHandler.Invoke(null, new object[] {attemptedDestination, existingDestination, detourException});
				return true;
			} catch (Exception e) {
				HugsLibController.Logger.Error("Exception while invoking detour fallback handler: {0} Exception was: {1}", matchingHandler.FullName(), e);
			}
			return false;
		}

		private static MethodInfo GetMatchingMethodInfo(DetourMethodAttribute sourceAttribute, MethodInfo destinationInfo) {
			// we should only ever get here in case the attribute was not defined with a sourceMethodInfo, but let's check just in case.
			if (sourceAttribute.WasSetByMethodInfo)
				return sourceAttribute.sourceMethodInfo;

			// aight, let's search by name
			MethodInfo[] candidates =
				sourceAttribute.sourceType.GetMethods(AllBindingFlags)
					.Where(mi => mi.Name == sourceAttribute.sourceMethodName).ToArray();

			// if we only get one result, we've got our method info - if the length is zero, the method doesn't exist.
			if (candidates.Length == 0)
				return null;
			if (candidates.Length == 1)
				return candidates.First();

			// this is where things get slightly complicated, we'll have to search by parameters.
			var destinationParameterTypes = destinationInfo.GetParameters().Select(pi => pi.ParameterType).ToList();
			var extensionMethodType = GetExtensionMethodType(destinationInfo);
			if (extensionMethodType != null) {
				// the "this" parameter should not be used in the parameter sequence comparison
				destinationParameterTypes.RemoveAt(0);
			}
			
			candidates = candidates.Where(mi =>
				mi.ReturnType == destinationInfo.ReturnType &&
				(extensionMethodType == null || mi.DeclaringType == extensionMethodType) &&
				mi.GetParameters().Select(pi => pi.ParameterType).SequenceEqual(destinationParameterTypes)).ToArray();

			// if we only get one result, we've got our method info - if the length is zero, the method doesn't exist.
			if (candidates.Length == 0)
				return null;
			if (candidates.Length == 1)
				return candidates.First();

			// if we haven't returned anything by this point there were still multiple candidates. This is theoretically impossible,
			// unless I missed something.
			return null;
		}

		internal static string FullName(this MethodInfo methodInfo) {
			if (methodInfo == null) return "[null reference]";
			if (methodInfo.DeclaringType == null) return methodInfo.Name;
			return methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
		}

		// returns the type of the first parameter is the method is an extension method, null otherwise
		private static Type GetExtensionMethodType(MethodInfo method) {
			if (method.TryGetAttributeSafely<ExtensionAttribute>() != null) {
				return method.GetParameters().Select(p => p.ParameterType).FirstOrDefault();
			}
			return null;
		}
	}
}