using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// Tools for working with the Harmony library.
	/// </summary>
	public static class HarmonyUtility {
		private const int DefaultPatchPriority = 400;
		
		/// <summary>
		/// Produces a human-readable list of all methods patched by all Harmony instances and their respective patches.
		/// </summary>
		public static string DescribeAllPatchedMethods() {
			try {
				return DescribePatchedMethodsList(Harmony.GetAllPatchedMethods());
			} catch (Exception e) {
				return "Could not retrieve patched methods from the Harmony library:\n" + e;
			}
		}

		/// <summary>
		/// Produces a human-readable list of all methods patched by a single Harmony instance and their respective patches.
		/// </summary>
		/// <param name="instance">A Harmony instance that can be queried for patch information.</param>
		public static string DescribePatchedMethods(Harmony instance) {
			try {
				return DescribePatchedMethodsList(instance.GetPatchedMethods());
			} catch (Exception e) {
				return $"Could not retrieve patched methods from Harmony instance (id: {instance.Id}):\n{e}";
			}
		}

		/// <summary>
		/// Produces a human-readable list of Harmony patches on a given set of methods.
		/// </summary>
		public static string DescribePatchedMethodsList(IEnumerable<MethodBase> patchedMethods) {
			try {
				var methodList = patchedMethods.ToList();
				// generate method name strings so we can sort the patches alphabetically
				var namedMethodList = new List<NameMethodPair>(methodList.Count);
				foreach (var method in methodList) {
					if (method == null) continue;
					var nestedName = GetNestedMemberName(method);
					namedMethodList.Add(new NameMethodPair(nestedName, method));
				}
				if (namedMethodList.Count == 0) {
					return "No patches have been reported.";
				}
				// sort patches by patched method name
				namedMethodList.Sort((m1, m2) => string.Compare(m1.MethodName, m2.MethodName, StringComparison.Ordinal));

				var builder = new StringBuilder();
				foreach (var pair in namedMethodList) {
					// write patched method
					builder.Append(pair.MethodName);
					builder.Append(": ");
					// write patches
					var patches = Harmony.GetPatchInfo(pair.Method);
					if (HasActivePatches(patches)) {
						// write prefixes
						if (patches.Prefixes != null && patches.Prefixes.Count > 0) {
							builder.Append("PRE: ");
							AppendPatchList(patches.Prefixes, builder);
						}
						// write postfixes
						if (patches.Postfixes != null && patches.Postfixes.Count > 0) {
							EnsureEndsWithSpace(builder);
							builder.Append("post: ");
							AppendPatchList(patches.Postfixes, builder);
						}
						// write transpilers
						if (patches.Transpilers != null && patches.Transpilers.Count > 0) {
							EnsureEndsWithSpace(builder);
							builder.Append("TRANS: ");
							AppendPatchList(patches.Transpilers, builder);
						}
					} else {
						builder.Append("(no patches)");
					}
					builder.AppendLine();
				}
				return builder.ToString();
			} catch (Exception e) {
				return "An exception occurred while collating patch data:\n" + e;
			}
		}

		/// <summary>
		/// Produces a human-readable list of all Harmony versions present and their respective owners.
		/// </summary>
		/// <param name="instance">A Harmony instance that can be queried for version information.</param>
		/// <returns></returns>
		public static string DescribeHarmonyVersions(Harmony instance) {
			try {
				var modVersionPairs = Harmony.VersionInfo(out _);
				return "Harmony versions present: " + 
					modVersionPairs.GroupBy(kv => kv.Value, kv => kv.Key).OrderByDescending(grp => grp.Key)
						.Select(grp => string.Format("{0}: {1}", grp.Key, grp.Join(", "))).Join("; ");
			} catch (Exception e) {
				return "An exception occurred while collating Harmony version data:\n" + e;
			}
		}

		internal static string GetNestedMemberName(MemberInfo member, int maxParentTypes = 10) {
			var sb = new StringBuilder(member.Name);
			var currentDepth = 0;
			var currentType = member.DeclaringType;
			while (currentType != null && currentDepth < maxParentTypes) {
				sb.Insert(0, '.');
				sb.Insert(0, currentType.Name);
				currentType = currentType.DeclaringType;
				currentDepth++;
			}
			return sb.ToString();
		}

		private static void AppendPatchList(IEnumerable<Patch> patchList, StringBuilder builder) {
			// ensure that patches appear in the same order they execute
			var sortedPatches = new List<Patch>(patchList);
			sortedPatches.Sort();

			var isFirstEntry = true;
			foreach (var patch in sortedPatches) {
				if (!isFirstEntry) {
					builder.Append(", ");
				}
				isFirstEntry = false;
				// write priority if set
				if (patch.priority != DefaultPatchPriority) {
					builder.AppendFormat("[{0}]", patch.priority);
				}
				// write full destination method name
				builder.Append(patch.PatchMethod.FullName());
			}
		}

		/// <summary>
		/// Logs an error if any issues with Harmony patches are detected
		/// </summary>
		public static void LogHarmonyPatchIssueErrors() {
			LogObsoleteMethodPatchErrors();
		}

		private static void LogObsoleteMethodPatchErrors() {
			foreach (var (owner, methods) in EnumerateObsoleteMethodPatchOwners()) {
				Log.Warning($"[{owner}] Patches on methods annotated as Obsolete were detected by HugsLib: " +
					$"{methods.Distinct().Select(HugsLibUtility.FullName).ListElements()}");
			}
		}

		private static IEnumerable<(string owner, IEnumerable<MethodBase> methods)> EnumerateObsoleteMethodPatchOwners() {
			return Harmony.GetAllPatchedMethods()
				.Select(method => (method, info: Harmony.GetPatchInfo(method)))
				.Where(pair => HasActivePatches(pair.info) && pair.method.HasAttribute<ObsoleteAttribute>())
				.SelectMany(pair => pair.info.Owners, (pair, owner) => (owner, pair.method))
				.GroupBy(pair => pair.owner, pair => pair.method)
				.Select(grp => (grp.Key, grp as IEnumerable<MethodBase>));
		}

		private static bool HasActivePatches(HarmonyLib.Patches patches) {
			return patches != null &&
				((patches.Prefixes != null && patches.Prefixes.Count != 0) ||
					(patches.Postfixes != null && patches.Postfixes.Count != 0) ||
					(patches.Transpilers != null && patches.Transpilers.Count != 0));
		}

		private static void EnsureEndsWithSpace(StringBuilder builder) {
			if (builder[builder.Length - 1] != ' ') {
				builder.Append(" ");
			}
		}

		private struct NameMethodPair {
			public readonly string MethodName;
			public readonly MethodBase Method;

			public NameMethodPair(string methodName, MethodBase method) {
				MethodName = methodName;
				Method = method;
			}
		}
	}
}