using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;

namespace HugsLib.Utils {
	/// <summary>
	/// Tools for working with the Harmony library.
	/// </summary>
	public static class HarmonyUtility {
		private const int DefaultPatchPriority = 400;
		
		/// <summary>
		/// Produces a human-readable list of all patched methods and their respective patches.
		/// </summary>
		/// <param name="instance">A HarmonyInstance that can be queried for patch information.</param>
		public static string DescribePatchedMethods(HarmonyInstance instance) {
			try {
				IEnumerable<MethodBase> patchedMethods;
				try {
					patchedMethods = instance.GetPatchedMethods();
				} catch (Exception e) {
					return "Could not retrieve patched methods from the Harmony library:\n" + e;
				}
				var methodList = patchedMethods.ToList();
				// generate method name strings so we can sort the patches alphabetically
				var namedMethodList = new List<NameMethodPair>(methodList.Count);
				foreach (var method in methodList) {
					if (method == null) continue;
					string methodName;
					if (method.DeclaringType != null) {
						methodName = string.Concat(method.DeclaringType.Name, ".", method.Name);
					} else {
						methodName = method.Name;
					}
					namedMethodList.Add(new NameMethodPair(methodName, method));
				}
				if (namedMethodList.Count == 0) {
					return "No patches have been reported.";
				}
				// sort patches by patched method name
				namedMethodList.Sort((m1, m2) => String.Compare(m1.MethodName, m2.MethodName, StringComparison.Ordinal));

				var builder = new StringBuilder();
				foreach (var pair in namedMethodList) {
					// write patched method
					builder.Append(pair.MethodName);
					builder.Append(": ");
					// write patches
					var patches = instance.IsPatched(pair.Method);
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
		/// <param name="instance">A HarmonyInstance that can be queried for version information.</param>
		/// <returns></returns>
		public static string DescribeHarmonyVersions(HarmonyInstance instance) {
			try {
				Version currentVersion;
				var allVersions = instance.VersionInfo(out currentVersion);
				var builder = new StringBuilder("Harmony versions present: ");
				var firstElement = true;
				foreach (var pair in allVersions) {
					if (!firstElement) {
						builder.Append(", ");
					}
					firstElement = false;
					builder.Append(pair.Key);
					builder.Append(':');
					builder.Append(pair.Value);
				}
				return builder.ToString();
			} catch (Exception e) {
				return "An exception occurred while collating Harmony version data:\n" + e;
			}
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
				builder.Append(patch.patch.FullName());
			}
		}

		private static bool HasActivePatches(Harmony.Patches patches) {
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