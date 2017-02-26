using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

		public static bool MethodMatchesSignature(this MethodInfo method, Type expectedReturnType, params Type[] expectedParameters) {
			if (method.ReturnType != expectedReturnType) return false;
			var methodParams = method.GetParameters();
			if (expectedParameters != null) {
				if (methodParams.Length != expectedParameters.Length) return false;
				for (int i = 0; i < methodParams.Length; i++) {
					if (methodParams[i].ParameterType != expectedParameters[i]) return false;
				}
			} else {
				if (methodParams.Length != 0) return false;
			}
			return true;
		}

		public static T TryGetAttributeSafely<T>(this MemberInfo member) where T : Attribute {
			try {
				var attrs = member.GetCustomAttributes(typeof (T), false);
				if (attrs.Length > 0) return (T)attrs[0];
			} catch {
				//mods could include attributes from libraries that are not loaded, which would throw an exception
			}
			return null;
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
				var methodName = DescribeDelegate(callback);
				exceptionCause = String.Format("{0} ({1})", methodName, e.Source);
			}
			HugsLibController.Logger.ReportException(e, exceptionCause, true, String.Format("a {0} callback", schedulerName));
		}

		internal static string DescribeDelegate(Delegate del) {
			if (del == null) return "null";
			var method = del.Method;
			var isAnonymous = method.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any();
			return isAnonymous ? "an anonymous method" : method.DeclaringType + "." + method.Name;
		}

        public static string TryReplaceUserDirectory(this string text) {
	        if (text != null && (text.StartsWith(@"~\") || text.StartsWith(@"~/"))) {
		        text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), text.Remove(0, 2));
	        }
	        return text;
        }

        public static string SurroundWithDoubleQuotes(this string text) {
            return string.Format("\"{0}\"", text);
        }

		public static string TryGetLogFilePath() {
			string logfile;
			if (GenCommandLine.TryGetCommandLineArg("logfile", out logfile) && logfile.NullOrEmpty()) {
				return logfile;
			}
			var platform = PlatformUtility.GetCurrentPlatform();
			switch (platform) {
				case PlatformType.Linux:
					return @"/tmp/rimworld_log";
				case PlatformType.MacOSX:
					return "~/Library/Logs/Unity/Player.log";
				case PlatformType.Windows:
					return Path.Combine(UnityData.dataPath, "output_log.txt");
				default:
					return null;
			}
		}
    }


}