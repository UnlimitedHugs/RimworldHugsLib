using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Source.Detour {
	public static class Helpers {
		public const BindingFlags AllBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		// keep track of scanned types
		private static List<Type> scanned = new List<Type>();

		private static MemberInfo lastAttemptedDetourSource;

		// log errors for improperly declared fallback handlers
		internal static void CheckFallbackHandlers() {
			if(!Prefs.DevMode) return; // skip unnecessary overhead
			foreach (var type in GenTypes.AllTypes) {
				var members = type.GetMembers(AllBindingFlags);
				for (int i = 0; i < members.Length; i++) {
					var handler = members[i] as MethodInfo;
					if(handler == null || !handler.HasAttribute<DetourFallbackAttribute>()) continue;
					// check is static
					if(!handler.IsStatic) HugsLibController.Logger.Error("Detour fallback handlers must be static ({0})", handler.FullName());
					// check signature
					if (!handler.MethodMatchesSignature(typeof(void), typeof(MemberInfo), typeof(MethodInfo), typeof(Exception))) {
						HugsLibController.Logger.Error("Improper Detour fallback handlers signature ({0}). Expected signature: void (MemberInfo attemptedDestination, MethodInfo existingDestination, Exception e)", handler.FullName());
					}
					// check for referenced methods
					var attribute = (DetourFallbackAttribute)handler.GetCustomAttributes(typeof(DetourFallbackAttribute), false).FirstOrDefault();
					if(attribute == null) continue;
					foreach (var memberName in attribute.targetMemberNames) {
						if (members.All(m => m.Name != memberName)) {
							HugsLibController.Logger.Error("Detour fallback handler references at least one missing member ({0})", handler.FullName());
						}
					}
				}
			}
		}

		internal static void DoDetours() {
			// get all types for mod
			IEnumerable<Type> types = HugsLibUtility.GetAllActiveAssemblies()
				.SelectMany(a => a.GetTypes())
				.Except(scanned).ToArray();

			// mark scanned
			scanned = scanned.Concat(types).ToList();

			// loop over all methods with the detour attribute set
			foreach (MethodInfo destination in types
				.SelectMany(t => t.GetMethods(AllBindingFlags))
				.Where(m => m.HasAttribute<DetourMethodAttribute>())) {
				var detourAttribute =
					destination.GetCustomAttributes(typeof (DetourMethodAttribute), false).First() as
						DetourMethodAttribute;
				try {
					lastAttemptedDetourSource = null;
					HandleMethodDetour(detourAttribute, destination);
				} catch (Exception e) {
					if (!TryCallDetourFallbackHandler(lastAttemptedDetourSource, destination, e)) {
						HugsLibController.Logger.ReportException(e);
					}
				}
			}

			// loop over all properties with the detour attribute set
			foreach (PropertyInfo destination in types
				.SelectMany(t => t.GetProperties(AllBindingFlags))
				.Where(m => m.HasAttribute<DetourPropertyAttribute>())) {
				var detourAttribute =
					destination.GetCustomAttributes(typeof (DetourPropertyAttribute), false).First() as
						DetourPropertyAttribute;
				try {
					lastAttemptedDetourSource = null;
					HandlePropertyDetour(detourAttribute, destination);
				} catch (Exception e) {
					if (!TryCallDetourFallbackHandler(lastAttemptedDetourSource, destination, e)) {
						HugsLibController.Logger.ReportException(e);
					}
				}
			}
			lastAttemptedDetourSource = null;
		}

		private static bool TryCallDetourFallbackHandler(MemberInfo attemptedSource, MemberInfo attemptedDestination, Exception detourException) {
			if (attemptedDestination.DeclaringType == null) return false;
			// find a matching handler method
			var handlers = attemptedDestination.DeclaringType.GetMethods(AllBindingFlags).Where(m => m.HasAttribute<DetourFallbackAttribute>());
			MethodInfo matchingHandler = null;
			foreach (var handler in handlers) {
				var attribute = (DetourFallbackAttribute)handler.GetCustomAttributes(typeof(DetourFallbackAttribute), false).FirstOrDefault();
				if(attribute == null || (attribute.targetMemberNames.Length>0 && !attribute.targetMemberNames.Contains(attemptedDestination.Name))) continue;
				matchingHandler = handler;
				break;
			}
			if (matchingHandler == null) return false;
			// try get the method the detour was already routed to
			MethodInfo existingDestination = null;
			if (attemptedSource!=null) {
				var sourceAsMethod = attemptedSource as MethodInfo;
				var sourceAsProperty = attemptedSource as PropertyInfo;
				if (sourceAsMethod != null) {
					// for methods
					existingDestination = DetourProvider.TryGetExistingDetourDestination(sourceAsMethod);
				} else if (sourceAsProperty != null) {
					// for properties. There might be a getter/setter only attribute discrepancy, but this is good enough
					var getter = sourceAsProperty.GetGetMethod(true);
					var setter = sourceAsProperty.GetSetMethod(true);
					if (getter != null) {
						existingDestination = DetourProvider.TryGetExistingDetourDestination(getter);
					}
					if (existingDestination == null && setter != null) {
						existingDestination = DetourProvider.TryGetExistingDetourDestination(setter);
					}
				}
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

		private static void HandleMethodDetour(DetourMethodAttribute sourceAttribute, MethodInfo targetInfo) {
			MethodInfo sourceInfo = null;
			try {
				// we need to get the method info of the source (usually, vanilla) method. 
				// if it was specified in the attribute, this is easy. Otherwise, we'll have to do some digging.
				sourceInfo = sourceAttribute.WasSetByMethodInfo
					? sourceAttribute.sourceMethodInfo
					: GetMatchingMethodInfo(sourceAttribute, targetInfo);

				// make sure we've got what we wanted.
				if (sourceInfo == null)
					throw new NullReferenceException("sourceMethodInfo could not be found based on attribute");
				if (targetInfo == null)
					throw new ArgumentNullException("targetInfo");

				lastAttemptedDetourSource = sourceInfo;
				// call the actual detour
				DetourProvider.CompatibleDetourWithExceptions(sourceInfo, targetInfo);
			} catch (Exception e) {
				DetourProvider.ThrowClearerDetourException(e, sourceInfo, targetInfo, "method");
			}
		}

		private static MethodInfo GetMatchingMethodInfo(DetourMethodAttribute sourceAttribute, MethodInfo targetInfo) {
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
			candidates = candidates.Where(mi =>
				mi.ReturnType == targetInfo.ReturnType &&
				mi.GetParameters()
					.Select(pi => pi.ParameterType)
					.SequenceEqual(targetInfo.GetParameters().Select(pi => pi.ParameterType)))
				.ToArray();

			// if we only get one result, we've got our method info - if the length is zero, the method doesn't exist.
			if (candidates.Length == 0)
				return null;
			if (candidates.Length == 1)
				return candidates.First();

			// if we haven't returned anything by this point there were still multiple candidates. This is theoretically impossible,
			// unless I missed something.
			return null;
		}

		private static void HandlePropertyDetour(DetourPropertyAttribute sourceAttribute, PropertyInfo targetInfo) {
			PropertyInfo sourceInfo = null;
			try {
				// first, lets get the source propertyInfo - there's no ambiguity here.
				sourceInfo = sourceAttribute.sourcePropertyInfo;
				lastAttemptedDetourSource = sourceInfo;
				// do our detours
				// if getter was flagged (so Getter | Both )
				if ((sourceAttribute.detourProperty & DetourProperty.Getter) == DetourProperty.Getter) {
					DetourProvider.CompatibleDetourWithExceptions(sourceInfo.GetGetMethod(true), targetInfo.GetGetMethod(true));
				}

				// if setter was flagged
				if ((sourceAttribute.detourProperty & DetourProperty.Setter) == DetourProperty.Setter) {
					DetourProvider.CompatibleDetourWithExceptions(sourceInfo.GetSetMethod(true), targetInfo.GetSetMethod(true));
				}
			} catch (Exception e) {
				DetourProvider.ThrowClearerDetourException(e, sourceInfo, targetInfo, "property");
			}
		}

		internal static string FullName(this MethodInfo methodInfo) {
			if (methodInfo == null) return "[null reference]";
			if (methodInfo.DeclaringType == null) return methodInfo.Name;
			return methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
		}
	}
}