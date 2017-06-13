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
	/// <summary>
	/// A catch-all place for extension methods and other useful stuff
	/// </summary>
	public static class HugsLibUtility {
		public static readonly BindingFlags AllBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		/// <summary>
		/// Returns true if the left or right Shift keys are currently pressed.
		/// </summary>
		public static bool ShiftIsHeld {
			get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
		}

		/// <summary>
		/// Returns true if the left or right Alt keys are currently pressed.
		/// </summary>
		public static bool AltIsHeld {
			get { return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt); }
		}

		/// <summary>
		/// Returns true if the left or right Control keys are currently pressed.
		/// Mac command keys are supported, as well.
		/// </summary>
		public static bool ControlIsHeld {
			get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand); }
		}

		/// <summary>
		/// Returns an enumerable as a comma-separated string.
		/// </summary>
		/// <param name="list">A list of elements to string together</param>
		public static string ListElements(this IEnumerable list) {
			return list.Join(", ", true);
		}

		/// <summary>
		/// Returns an enumerable as a string, soined by a separator string. By default null values appear as an empty string.
		/// </summary>
		/// <param name="list">A list of elements to string together</param>
		/// <param name="separator">A string to inset between elements</param>
		/// <param name="explicitNullValues">If true, null elements will appear as "[null]"</param>
		public static string Join(this IEnumerable list, string separator, bool explicitNullValues = false) {
			if (list == null) return "";
			var builder = new StringBuilder();
			var useSeparator = false;
			foreach (var elem in list) {
				if (useSeparator) builder.Append(separator);
				useSeparator = true;
				if (elem != null || explicitNullValues) {
					builder.Append(elem != null ? elem.ToString() : "[null]");
				}
			}
			return builder.ToString();
		}

		/// <summary>
		/// Returns a version as major.minor.patch formatted string.
		/// </summary>
		public static string ToShortString(this Version version) {
			if(version == null) version = new Version();
			return String.Concat(version.Major, ".", version.Minor, ".", version.Build);
		}

		/// <summary>
		/// Checks if a Thing has a designation of a given def.
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="def">The designation def to check for</param>
		public static bool HasDesignation(this Thing thing, DesignationDef def) {
			if (thing.Map == null || thing.Map.designationManager == null) return false;
			return thing.Map.designationManager.DesignationOn(thing, def) != null;
		}

		/// <summary>
		/// Adds or removes a designation of a given def on a Thing. Fails silently if designation is already in the desired state.
		/// </summary>
		/// <param name="thing">The thing to designate</param>
		/// <param name="def">The DesignationDef to apply or remove</param>
		/// <param name="enable">True to add the designation, false to remove</param>
		public static void ToggleDesignation(this Thing thing, DesignationDef def, bool enable) {
			if (thing.Map == null || thing.Map.designationManager == null) throw new Exception("Thing must belong to a map to toggle designations on it");
			var des = thing.Map.designationManager.DesignationOn(thing, def);
			if (enable && des == null) {
				thing.Map.designationManager.AddDesignation(new Designation(thing, def));
			} else if(!enable && des != null) {
				thing.Map.designationManager.RemoveDesignation(des);
			}
		}

		/// <summary>
		/// Checks if a cell has a designation of a given def
		/// </summary>
		/// <param name="pos">The map position to check</param>
		/// <param name="def">The DesignationDef to detect</param>
		/// <param name="map">The map to look on. When null, defaults to VisibleMap.</param>
		public static bool HasDesignation(this IntVec3 pos, DesignationDef def, Map map = null) {
			if (map == null) {
				map = Find.VisibleMap;
			}
			if (map == null || map.designationManager == null) return false;
			return map.designationManager.DesignationAt(pos, def) != null;
		}

		/// <summary>
		/// Adds or removes a designation of a given def on a cell. Fails silently if designation is already in the desired state.
		/// </summary>
		/// <param name="pos">The position to designate</param>
		/// <param name="def">The DesignationDef to apply or remove</param>
		/// <param name="enable">True to add the designation, false to remove</param>
		/// <param name="map">The map to operate on. When null, defaults to VisibleMap.</param>
		public static void ToggleDesignation(this IntVec3 pos, DesignationDef def, bool enable, Map map = null) {
			if (map == null) {
				map = Find.VisibleMap;
			}
			if (map == null || map.designationManager == null) throw new Exception("ToggleDesignation requires a map argument or VisibleMap must be set");
			var des = map.designationManager.DesignationAt(pos, def);
			if (enable && des == null) {
				map.designationManager.AddDesignation(new Designation(pos, def));
			} else if (!enable && des != null) {
				map.designationManager.RemoveDesignation(des);
			}
		}

		/// <summary>
		/// Returns true, if a MethodInfo matches the provided signature.
		/// </summary>
		/// <remarks>Note: instance methods always take their parent type as the first parameter.</remarks>
		/// <param name="method">The method to check</param>
		/// <param name="expectedReturnType">Expected return type of the checked method</param>
		/// <param name="expectedParameters">Expected parameter types of the checked method</param>
		public static bool MethodMatchesSignature(this MethodInfo method, Type expectedReturnType, params Type[] expectedParameters) {
			if (method == null) return false;
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

		/// <summary>
		/// Returns an attribute from a member, if it exists.
		/// Mods could include attributes from libraries that are not loaded, which would throw an exception, so error checking is included.
		/// </summary>
		/// <typeparam name="T">The type of the attribute to fetch</typeparam>
		/// <param name="member">The member to fetch the attribute from</param>
		public static T TryGetAttributeSafely<T>(this MemberInfo member) where T : Attribute {
			try {
				var attrs = member.GetCustomAttributes(typeof (T), false);
				if (attrs.Length > 0) return (T)attrs[0];
			} catch {
				//
			}
			return null;
		}

		/// <summary>
		/// Enumerates all loaded assemblies, inluding stock and enabled mods.
		/// </summary>
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

		/// <summary>
		/// Returns true if the mod with a matching name is currently loaded in the mod configuration.
		/// </summary>
		/// <param name="modName">The ModMetaData.Name to match</param>
		public static bool IsModActive(string modName) {
			foreach (var current in ModLister.AllInstalledMods) {
				if (current.Active && modName.Equals(current.Name)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Copies a string to the system copy buffer and displays a confirmation message.
		/// </summary>
		public static void CopyToClipboard(string data) {
			GUIUtility.systemCopyBuffer = data;
			Messages.Message("HugsLib_copiedToClipboard".Translate(), MessageSound.Benefit);
		}

		/// <summary>
		/// Expands a shorthand unix user directory path with its full system path.
		/// </summary>
		public static string TryReplaceUserDirectory(this string text) {
	        if (text != null && (text.StartsWith(@"~\") || text.StartsWith(@"~/"))) {
		        text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), text.Remove(0, 2));
	        }
	        return text;
        }

		/// <summary>
		/// Adds double quotes to the start and end of a string.
		/// </summary>
        public static string SurroundWithDoubleQuotes(this string text) {
            return string.Format("\"{0}\"", text);
        }

		/// <summary>
		/// Attempts to return the patch of the log file Unity is writing to.
		/// </summary>
		/// <returns></returns>
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
					logfile = Path.Combine(UnityData.dataPath, "output_log.txt");
					if (File.Exists(logfile)) {
						return logfile;
					}
					return Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%") + $@"Low\{Application.companyName}\{Application.productName}\output_log.txt";
				default:
					return null;
			}
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

		internal static string FullName(this MethodInfo methodInfo) {
			if (methodInfo == null) return "[null reference]";
			if (methodInfo.DeclaringType == null) return methodInfo.Name;
			return methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
		}

	}


}
