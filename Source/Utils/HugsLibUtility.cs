using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;

namespace HugsLib.Utils {
	// A catch-all place for extension methods and other useful stuff
	public static class HugsLibUtility {

		public static bool ShiftIsHeld {
			get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
		}

		public static bool AltIsHeld {
			get { return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt); }
		}

		public static bool ControlIsHeld {
			get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand); }
		} 

		// Returns an enumerable as a comma-separated string.
		public static string ListElements(this IEnumerable list) {
			return list.Join(", ", true);
		}

		// Returns an enumerable as a string, soined by a separator string. By default null values appear as an empty string.
		public static string Join(this IEnumerable list, string separator, bool explicitNullValues = false) {
			if (list == null) return "";
			var builder = new StringBuilder();
			var useSeparator = false;
			foreach (var elem in list) {
				if (useSeparator) builder.Append(separator);
				useSeparator = true;
				if (elem != null || explicitNullValues) {
					builder.Append(elem != null ? elem.ToString() : "null");
				}
			}
			return builder.ToString();
		}

		// Returns a version as major.minor.patch formatted string.
		public static string ToShortString(this Version version) {
			if(version == null) version = new Version();
			return String.Concat(version.Major, ".", version.Minor, ".", version.Build);
		}

		// Checks if a Thing has a designation of a given def
		public static bool HasDesignation(this Thing thing, DesignationDef def) {
			if (thing.Map == null || thing.Map.designationManager == null) return false;
			return thing.Map.designationManager.DesignationOn(thing, def) != null;
		}

		// Adds or removes a designation of a given def on a Thing. Fails silently if designation is already in the desired state.
		public static void ToggleDesignation(this Thing thing, DesignationDef def, bool enable) {
			if (thing.Map == null || thing.Map.designationManager == null) throw new Exception("Thing must belong to a map to toggle designations on it");
			var des = thing.Map.designationManager.DesignationOn(thing, def);
			if (enable && des == null) {
				thing.Map.designationManager.AddDesignation(new Designation(thing, def));
			} else if(!enable && des != null) {
				thing.Map.designationManager.RemoveDesignation(des);
			}
		}

		public static IEnumerable<Assembly> GetAllActiveAssemblies() {
			var listed = new HashSet<Assembly>();
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				foreach (var loadedAssembly in modContentPack.assemblies.loadedAssemblies) {
					if(listed.Contains(loadedAssembly)) continue;
					listed.Add(loadedAssembly);
					yield return loadedAssembly;
				}
			}
		}

		public static bool IsModActive(string modName) {
			foreach (var current in ModLister.AllInstalledMods) {
				if (current.Active && modName.Equals(current.Name)) {
					return true;
				}
			}
			return false;
		}

		public static void CopyToClipboard(string data) {
			GUIUtility.systemCopyBuffer = data;
			Messages.Message("HugsLib_copiedToClipboard".Translate(), MessageSound.Benefit);
		}

		internal static void BlameCallbackException(string schedulerName, Action callback, Exception e) {
			string exceptionCause = null;
			if (callback != null) {
				var method = callback.Method;
				var isAnonymous = method.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
				var methodName = isAnonymous ? "An anonymous method" : method.DeclaringType + "." + method.Name;
				exceptionCause = string.Format("{0} ({1})", methodName, e.Source);
			}
			HugsLibController.Logger.ReportException(e, exceptionCause, true, string.Format("a {0} callback", schedulerName));
		}
	}


}