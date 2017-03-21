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
		/// Enumerates all methods that have been patched by Harmony.
		/// </summary>
		public static IEnumerable<MethodBase> AllPatchedMethods() {
			const string AssemblyNameField = "name";
			const string StateField = "state";
			const string AssemblyRetrievalMethod = "SharedStateAssembly";
			// get the name of the dynamic assembly
			var stateAssemblyName = Traverse.Create(typeof(HarmonySharedState)).Field(AssemblyNameField).GetValue<string>();
			if(stateAssemblyName == null) throw new Exception("Failed to get state assembly name.");
			// request the assembly object
			var assembly = Traverse.Create(typeof(HarmonySharedState)).Method(AssemblyRetrievalMethod).GetValue<Assembly>();
			if (assembly == null) throw new Exception("State assembly could not be retrieved.");
			// extract private patch dictionary
			var harmonyState = Traverse.Create(assembly.GetType(stateAssemblyName)).Field(StateField).GetValue<Dictionary<MethodBase, byte[]>>();
			if (harmonyState == null) throw new Exception("Failed to retrieve state from state assembly.");
			return harmonyState.Keys;
		}

		/// <summary>
		/// Produces a human-readable list of all patched methods and their respective patches.
		/// </summary>
		/// <param name="instance">A HarmonyInstance that can be queried for patch information.</param>
		/// <param name="patchOwners">A list of all patch owner identifiers that have at least one patch on the list.</param>
		public static string DescribePatchedMethods(HarmonyInstance instance, out IEnumerable<string> patchOwners) {
			var patchOwnerIds = new HashSet<string>();
			patchOwners = patchOwnerIds;
			try {
				IEnumerable<MethodBase> patchedMethods;
				try {
					patchedMethods = AllPatchedMethods();
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
					if (patches == null ||
					    ((patches.Prefixes == null || patches.Prefixes.Count == 0) &&
					     (patches.Postfixes == null || patches.Postfixes.Count == 0))) {
						builder.Append("(no patches)");
					} else {
						// write prefixes
						if (patches.Prefixes != null && patches.Prefixes.Count > 0) {
							builder.Append("Pre: ");
							AppendPatchList(patches.Prefixes, builder, patchOwnerIds);
						}
						// write postfixes
						if (patches.Postfixes != null && patches.Postfixes.Count > 0) {
							if (builder[builder.Length - 1] != ' ') {
								builder.Append(" ");
							}
							builder.Append("Post: ");
							AppendPatchList(patches.Postfixes, builder, patchOwnerIds);
						}
					}
					builder.AppendLine();
				}
				return builder.ToString();
			} catch (Exception e) {
				return "An exception occurred while collating patch data:\n" + e;
			}
		}

		private static void AppendPatchList(IEnumerable<Patch> patchList, StringBuilder builder, HashSet<string> patchOwners) {
			// ensure that patches appear in the same order they execute
			var sortedPatches = new List<Patch>(patchList);
			sortedPatches.Sort();

			var isFirstEntry = true;
			foreach (var patch in sortedPatches) {
				patchOwners.Add(patch.owner);
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