﻿using System;
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
		private static Dictionary<MethodBase, bool> IgnoredPatchedMethods = new Dictionary<MethodBase, bool>();
		
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
		/// Register/Deregister a method to not be printed in the HugsLog if there are no active patches.
		/// </summary>
		/// <param name="method">The method to be registered.</param>
		/// <param name="value">If true the method will be ignored; if false ensures it is not ignored.</param>
		public static void RegisterIgnoredPatchedMethod(MethodBase method, bool value = true)
		{
			if (IgnoredPatchedMethods.ContainsKey(method))
				IgnoredPatchedMethods[method] &= value;
			else
				IgnoredPatchedMethods.Add(method, value);
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
					// Look through nested Types and mark nested type
					bool nestedFlag = (method.DeclaringType?.IsNested == true);
					string methodName = string.Concat(method.Name, nestedFlag ? " ]" : "");
					Type currType = method.DeclaringType;
					while (currType != null)
					{
						methodName = string.Concat(currType.Name, !currType.IsNested && nestedFlag ? ".[ " : ".", methodName);
						currType = currType.DeclaringType;
					}
					namedMethodList.Add(new NameMethodPair(methodName, method));
				}
				if (namedMethodList.Count == 0) {
					return "No patches have been reported.";
				}
				// sort patches by patched method name
				namedMethodList.Sort((m1, m2) => string.Compare(m1.MethodName, m2.MethodName, StringComparison.Ordinal));

				var builder = new StringBuilder("Format = {BaseType}. [{NestedType}.{Method}] : {Active_Harmony_Patches}\n");
				foreach (var pair in namedMethodList) {
					// write patched method
					var LogLine = new StringBuilder(pair.MethodName + " : ");
					// write patches
					var patches = Harmony.GetPatchInfo(pair.Method);
					if (HasActivePatches(patches)) {
						// write prefixes
						if (patches.Prefixes != null && patches.Prefixes.Count > 0) {
							LogLine.Append("PRE: ");
							AppendPatchList(patches.Prefixes, LogLine);
						}
						// write postfixes
						if (patches.Postfixes != null && patches.Postfixes.Count > 0) {
							EnsureEndsWithSpace(LogLine);
							LogLine.Append("post: ");
							AppendPatchList(patches.Postfixes, LogLine);
						}
						// write transpilers
						if (patches.Transpilers != null && patches.Transpilers.Count > 0) {
							EnsureEndsWithSpace(LogLine);
							LogLine.Append("TRANS: ");
							AppendPatchList(patches.Transpilers, LogLine);
						}
						builder.AppendLine(LogLine.ToString());
					} else {
						if (!IgnoredPatchedMethods.TryGetValue(pair.Method, false))
							builder.AppendLine(LogLine.ToString() + "(no patches)");
					}
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