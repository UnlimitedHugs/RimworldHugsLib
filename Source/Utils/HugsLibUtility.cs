using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using UnityEngine.Networking;
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
		/// Returns an enumerable as a string, joined by a separator string. By default null values appear as an empty string.
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
				map = Find.CurrentMap;
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
				map = Find.CurrentMap;
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
		/// Enumerates all loaded assemblies, including stock and enabled mods.
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
			Messages.Message("HugsLib_copiedToClipboard".Translate(), MessageTypeDefOf.TaskCompletion);
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
					return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Logs/Unity/Player.log");
				case PlatformType.Windows:
					return Path.Combine(Application.persistentDataPath, "Player.log");
				default:
					return null;
			}
		}

		/// <summary>
		/// Sends a constructed UnityWebRequest, waits for the result, and returns the data via callbacks.
		/// </summary>
		/// <param name="request">Use UnityWebRequest or WWW to construct a request. Do not call Send().</param>
		/// <param name="onSuccess">Called with the response body if server replied with status 200.</param>
		/// <param name="onFailure">Called with the error message in case of a network error or if server replied with status other than 200.</param>
		/// <param name="successStatus">The expected status code in the response for the request to be considered successful</param>
		/// <param name="timeout">How long to wait before aborting the request</param>
		public static void AwaitUnityWebResponse(UnityWebRequest request, Action<string> onSuccess, Action<Exception> onFailure, HttpStatusCode successStatus = HttpStatusCode.OK, float timeout = 30f) {
			/* TODO: next major update: scrap whole method, revert to System.Net.WebClient
			.NET version has been updated and SSL should work again */
			#pragma warning disable 618
			request.Send();
			#pragma warning restore 618
			var timeoutTime = Time.unscaledTime + timeout;
			Action pollingAction = null;
			pollingAction = () => {
				var timedOut = Time.unscaledTime > timeoutTime;
				try {
					if (!request.isDone && !timedOut) {
						HugsLibController.Instance.DoLater.DoNextUpdate(pollingAction);
					} else {
						if (timedOut) {
							if (!request.isDone) {
								request.Abort();
							}
							throw new Exception("timed out");
						}
						if (request.isNetworkError || request.isHttpError) {
							throw new Exception(request.error);
						}
						var status = (HttpStatusCode)request.responseCode;
						if (status != successStatus) {
							throw new Exception(string.Format("{0} replied with {1}: {2}", request.url, status, request.downloadHandler.text));
						} else {
							if (onSuccess != null) onSuccess(request.downloadHandler.text);
						}
					}
				} catch (Exception e) {
					if (onFailure != null) {
						onFailure(e);
					} else {
						HugsLibController.Logger.Warning("UnityWebRequest failed: " + e);
					}
				}
			};
			pollingAction();
		}

		/// <summary>
		/// Tries to find the file handle for a given mod assembly name.
		/// </summary>
		/// <remarks>This is a replacement for <see cref="Assembly.Location"/> mod assemblies are loaded from byte arrays.</remarks>
		/// <param name="assemblyName">The <see cref="AssemblyName.Name"/> of the assembly</param>
		/// <param name="contentPack">The content pack the assembly was presumably loaded from</param>
		/// <returns>Returns null if the file is not found</returns>
		public static FileInfo GetModAssemblyFileInfo(string assemblyName, [NotNull] ModContentPack contentPack) {
			if (contentPack == null) throw new ArgumentNullException(nameof(contentPack));
			const string AssembliesFolderName = "Assemblies";
			var expectedAssemblyFileName = $"{assemblyName}.dll"; 
			var modAssemblyFolderFiles = ModContentPack.GetAllFilesForMod(contentPack, AssembliesFolderName);
			return modAssemblyFolderFiles.Values.FirstOrDefault(f => f.Name == expectedAssemblyFileName);
		}

		/// <summary>
		/// Same as <see cref="GetModAssemblyFileInfo"/> but suppresses all exceptions.
		/// </summary>
		public static FileInfo TryGetModAssemblyFileInfo(string assemblyName, ModContentPack modPack) {
			try {
				return GetModAssemblyFileInfo(assemblyName, modPack);
			} catch (Exception) {
				return null;
			}
		}

		internal static void BlameCallbackException(string schedulerName, Delegate callback, Exception e) {
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
