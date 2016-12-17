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
					HandleDetour(detourAttribute, destination);
				} catch (Exception e) {
					HugsLibController.Logger.ReportException(e);
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
					HandleDetour(detourAttribute, destination);
				} catch (Exception e) {
					HugsLibController.Logger.ReportException(e);
				}
			}
		}

		private static void HandleDetour(DetourMethodAttribute sourceAttribute, MethodInfo targetInfo) {
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

				// call the actual detour
				DetourProvider.CompatibleDetourWithPreCheck(sourceInfo, targetInfo);
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

		private static void HandleDetour(DetourPropertyAttribute sourceAttribute, PropertyInfo targetInfo) {
			PropertyInfo sourceInfo = null;
			try {
				// first, lets get the source propertyInfo - there's no ambiguity here.
				sourceInfo = sourceAttribute.sourcePropertyInfo;
				
				// do our detours
				// if getter was flagged (so Getter | Both )
				if ((sourceAttribute.detourProperty & DetourProperty.Getter) == DetourProperty.Getter)
					DetourProvider.CompatibleDetourWithPreCheck(sourceInfo.GetGetMethod(true), targetInfo.GetGetMethod(true));

				// if setter was flagged
				if ((sourceAttribute.detourProperty & DetourProperty.Setter) == DetourProperty.Setter)
					DetourProvider.CompatibleDetourWithPreCheck(sourceInfo.GetSetMethod(true), targetInfo.GetSetMethod(true));
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