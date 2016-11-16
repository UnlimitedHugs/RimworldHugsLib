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
	// A catch-all place for all the useful things
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

		/**
		 * Injects a map component into the current map if it does not already exist. 
		 * Required for new MapComponents that were not active at map creation.
		 * The injection is performed at ExecuteWhenFinished to allow calling this method in MapComponent constructors.
		 */
		public static void EnsureIsActive(this MapComponent mapComponent) {
			if (mapComponent == null) throw new Exception("MapComponent is null");
			LongEventHandler.ExecuteWhenFinished(() => {
				if (Current.Game == null || Current.Game.Map == null || Current.Game.Map.components == null) throw new Exception("Can only inject in a loaded map.");
				var components = Current.Game.Map.components;
				if (components.Any(c => c == mapComponent)) return;
				Find.Map.components.Add(mapComponent);
			});
		}

		public static bool HasDesignation(this Thing thing, DesignationDef def) {
			if (Current.Game == null || Current.Game.Map == null || Current.Game.Map.designationManager == null) return false;
			return Find.DesignationManager.DesignationOn(thing, def) != null;
		}

		public static void ToggleDesignation(this Thing thing, DesignationDef def, bool enable) {
			if (Current.Game == null || Current.Game.Map == null || Current.Game.Map.designationManager == null) return;
			var des = Find.DesignationManager.DesignationOn(thing, def);
			if (enable && des == null) {
				Find.DesignationManager.AddDesignation(new Designation(thing, def));
			} else if(!enable && des != null) {
				Find.DesignationManager.RemoveDesignation(des);
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

		// Adds a hash to a manually instantiated def to avoid def collisions
		public static void AddSHortHashToInjectedDef(Def newDef) {
			var hashMethod = typeof(ShortHashGiver).GetMethod("GiveShortHash", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(Def) }, null);
			if (hashMethod == null) {
				HugsLibController.Logger.Warning("Failed to reflect ShortHashGiver.GiveShortHash");
				return;
			}
			hashMethod.Invoke(null, new object[] { newDef });
		}

		// sanitizes a string for valid inclusion in JSON
		public static string CleanForJSON(string s) {
			if (string.IsNullOrEmpty(s)) {
				return "";
			}
			int i;
			int len = s.Length;
			var sb = new StringBuilder(len + 4);
			for (i = 0; i < len; i += 1) {
				var c = s[i];
				switch (c) {
					case '\\':
					case '"':
						sb.Append('\\');
						sb.Append(c);
						break;
					case '/':
						sb.Append('\\');
						sb.Append(c);
						break;
					case '\b':
						sb.Append("\\b");
						break;
					case '\t':
						sb.Append("\\t");
						break;
					case '\n':
						sb.Append("\\n");
						break;
					case '\f':
						sb.Append("\\f");
						break;
					case '\r':
						sb.Append("\\r");
						break;
					default:
						if (c < ' ') {
							var t = "000" + String.Format("X");
							sb.Append("\\u" + t.Substring(t.Length - 4));
						} else {
							sb.Append(c);
						}
						break;
				}
			}
			return sb.ToString();
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