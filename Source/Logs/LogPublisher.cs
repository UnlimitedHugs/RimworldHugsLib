using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace HugsLib.Logs {
	/// <summary>
	/// Collects the game logs and loaded mods and posts the information on GitHub as a gist.
	/// </summary>
	public class LogPublisher {
		private const string RequestUserAgent = "HugsLib_log_uploader";
		private const string OutputLogFilename = "output_log.txt";
		private const string GistApiUrl = "https://api.github.com/gists";
		private const string ShortenerUrl = "https://git.io/";
		private const string GistPayloadJson = "{{\"description\":\"{0}\",\"public\":true,\"files\":{{\"{1}\":{{\"content\":\"{2}\"}}}}}}";
		private const string GistDescription = "Rimworld output log published using HugsLib";
		private const int MaxLogLineCount = 10000;
		private readonly string GitHubAuthToken = "6b69be56e8d8eaf678377c992a3d0c9b6da917e0".Reverse().Join(""); // GitHub will revoke any tokens committed
		private readonly Regex UploadResponseUrlMatch = new Regex("\"html_url\":\"(https://gist\\.github\\.com/\\w+)\"");

		public enum PublisherStatus {
			Ready,
			Uploading,
			Shortening,
			Done,
			Error
		}

		public PublisherStatus Status { get; private set; }
		public string ErrorMessage { get; private set; }
		public string ResultUrl { get; private set; }
		private static SettingHandle<LogPublisherOptions> optionsHandle;
		private LogPublisherOptions publishOptions;
		private bool userAborted;
		private UnityWebRequest activeRequest;
		private Thread mockThread;

		public void ShowPublishPrompt() {
			if (PublisherIsReady()) {
				UpdateCustomOptionsUsage();
				Find.WindowStack.Add(new Dialog_PublishLogsOptions(
					"HugsLib_logs_shareConfirmTitle".Translate(),
					"HugsLib_logs_shareConfirmMessage".Translate(),
					optionsHandle.Value
				) {
					OnUpload = OnPublishConfirmed,
					OnCopy = CopyToClipboard,
					OnOptionsToggled = UpdateCustomOptionsUsage,
					OnPostClose = () => HugsLibController.Instance.Settings.SaveChanges()
				});
			} else {
				ShowPublishDialog();
			}
		}

		private void UpdateCustomOptionsUsage() {
			publishOptions = optionsHandle.Value.UseCustomOptions ? optionsHandle.Value : new LogPublisherOptions();
		}

		public void AbortUpload() {
			if (Status != PublisherStatus.Uploading && Status != PublisherStatus.Shortening) return;
			userAborted = true;
			if (activeRequest != null && !activeRequest.isDone) {
				activeRequest.Abort();
			}
			activeRequest = null;
			if (mockThread != null && mockThread.IsAlive) {
				mockThread.Interrupt();
			}
			if (Status == PublisherStatus.Shortening) {
				FinalizeUpload(true);
			} else {
				ErrorMessage = "Aborted by user";
				FinalizeUpload(false);
			}
		}

		public void BeginUpload() {
			if (!PublisherIsReady()) return;
			Status = PublisherStatus.Uploading;
			ErrorMessage = null;
			userAborted = false;

			var collatedData = PrepareLogData();

#if TEST_MOCK_UPLOAD
			HugsLibController.Logger.Message(collatedData);
			HugsLibUtility.CopyToClipboard(collatedData);
			MockUpload();
			return;
#endif

			if (collatedData == null) {
				ErrorMessage = "Failed to collect data";
				FinalizeUpload(false);
				return;
			}
			Action<Exception> onRequestFailed = ex => {
				if (userAborted) return;
				OnRequestError(ex.Message);
				HugsLibController.Logger.Warning("Exception during log publishing (gist creation): " + ex);
			};
			try {
				collatedData = CleanForJSON(collatedData);
				var payload = string.Format(GistPayloadJson, GistDescription, OutputLogFilename, collatedData);
				activeRequest = new UnityWebRequest(GistApiUrl, UnityWebRequest.kHttpVerbPOST);
				activeRequest.SetRequestHeader("Authorization", "token " + GitHubAuthToken);
				activeRequest.SetRequestHeader("User-Agent", RequestUserAgent);
				activeRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)) {contentType = "application/json"};
				activeRequest.downloadHandler = new DownloadHandlerBuffer();
				HugsLibUtility.AwaitUnityWebResponse(activeRequest, OnUploadComplete, onRequestFailed, HttpStatusCode.Created);
			} catch (Exception e) {
				onRequestFailed(e);
			}
		}

		public void CopyToClipboard() {
			UpdateCustomOptionsUsage();
			HugsLibUtility.CopyToClipboard(PrepareLogData());
		}

		// this is for testing the uploader gui without making requests	
		private void MockUpload() {
			mockThread = new Thread(() => {
				Thread.Sleep(1500);
				Status = PublisherStatus.Shortening;
				Thread.Sleep(1500);
				ResultUrl = "copied to clipboard";
				FinalizeUpload(true);
			});
			mockThread.Start();
		}

		private void OnPublishConfirmed() {
			BeginUpload();
			ShowPublishDialog();
		}

		private void ShowPublishDialog() {
			Find.WindowStack.Add(new Dialog_PublishLogs());
		}

		private void OnRequestError(string errorMessage) {
			ErrorMessage = errorMessage;
			FinalizeUpload(false);
		}

		private void OnUploadComplete(string response) {
			var matchedUrl = TryExtractGistUrlFromUploadResponse(response);
			if (matchedUrl == null) {
				OnRequestError("Failed to parse response");
				return;
			}
			ResultUrl = matchedUrl;
			if (publishOptions.UseUrlShortener) {
				BeginUrlShortening();
			} else {
				FinalizeUpload(true);
			}
		}

		private void BeginUrlShortening() {
			Status = PublisherStatus.Shortening;

			Action<Exception> onRequestFailed = ex => {
				if (userAborted) return;
				FinalizeUpload(true);
				HugsLibController.Logger.Warning("Exception during log publishing (url shortening): " + ex);
			};
			try {
				var formData = new Dictionary<string, string> {
					{"url", ResultUrl}
				};
				activeRequest = UnityWebRequest.Post(ShortenerUrl, formData);
				activeRequest.SetRequestHeader("User-Agent", RequestUserAgent);
				HugsLibUtility.AwaitUnityWebResponse(activeRequest, OnUrlShorteningComplete, onRequestFailed, HttpStatusCode.Created);
			} catch (Exception e) {
				onRequestFailed(e);
			}
		}

		private void OnUrlShorteningComplete(string shortUrl) {
			ResultUrl = activeRequest.GetResponseHeader("Location");
			FinalizeUpload(true);
		}

		private void FinalizeUpload(bool success) {
			Status = success ? PublisherStatus.Done : PublisherStatus.Error;
			activeRequest = null;
			mockThread = null;
		}

		private string TryExtractGistUrlFromUploadResponse(string response) {
			var match = UploadResponseUrlMatch.Match(response);
			if (!match.Success) return null;
			return match.Groups[1].ToString();
		}

		private bool PublisherIsReady() {
			return Status == PublisherStatus.Ready || Status == PublisherStatus.Done || Status == PublisherStatus.Error;
		}

		private string PrepareLogData() {
			try {
				var logSection = GetLogFileContents();
				logSection = NormalizeLineEndings(logSection);
				// redact logs for privacy
				logSection = RedactRimworldPaths(logSection);
				logSection = RedactPlayerConnectInformation(logSection);
				logSection = RedactRendererInformation(logSection);
				logSection = RedactHomeDirectoryPaths(logSection);
				logSection = RedactSteamId(logSection);
				logSection = RedactUselessLines(logSection);
				logSection = TrimExcessLines(logSection);
				var collatedData = string.Concat(MakeLogTimestamp(),
					ListActiveMods(), "\n",
					ListHarmonyPatches(), "\n",
					ListPlatformInfo(), "\n",
					logSection);
				return collatedData;
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
			return null;
		}

		private string NormalizeLineEndings(string log) {
			return log.Replace("\r\n", "\n");
		}

		private string TrimExcessLines(string log) {
			if (publishOptions.AllowUnlimitedLogSize) return log;
			var indexOfLastNewline = IndexOfOccurence(log, '\n', MaxLogLineCount);
			if (indexOfLastNewline >= 0) {
				log = $"{log.Substring(0, indexOfLastNewline + 1)}(log trimmed to {MaxLogLineCount:N0} lines. Use publishing options to upload the full log)";
			}
			return log;
		}

		private int IndexOfOccurence(string s, char match, int occurence) {
			int currentOccurence = 1;
			int curentIndex = 0;
			while (currentOccurence <= occurence && (curentIndex = s.IndexOf(match, curentIndex + 1)) != -1) {
				if (currentOccurence == occurence) return curentIndex;
				currentOccurence++;
			}
			return -1;
		}

		private string RedactUselessLines(string log) {
			log = Regex.Replace(log, "Non platform assembly:.+\n", "");
			log = Regex.Replace(log, "Platform assembly: .+\n", "");
			log = Regex.Replace(log, "Fallback handler could not load library.+\n", "");
			log = Regex.Replace(log, "- Completed reload, in [\\d\\. ]+ seconds\n", "");
			log = Regex.Replace(log, "UnloadTime: [\\d\\. ]+ ms\n", "");
			log = Regex.Replace(log, "<RI> Initializing input\\.\r\n", "");
			log = Regex.Replace(log, "<RI> Input initialized\\.\r\n", "");
			log = Regex.Replace(log, "<RI> Initialized touch support\\.\r\n", "");
			log = Regex.Replace(log, "\\(Filename: C:/buildslave.+\n", "");
			log = Regex.Replace(log, "\n \n", "\n");
			return log;
		}

		// only relevant on linux
		private string RedactSteamId(string log) {
			const string IdReplacement = "[Steam Id redacted]";
			return Regex.Replace(log, "Steam_SetMinidumpSteamID.+", IdReplacement);
		}

		private string RedactHomeDirectoryPaths(string log) {
			// not necessary for windows logs
			if (PlatformUtility.GetCurrentPlatform() == PlatformType.Windows) {
				return log;
			}
			const string pathReplacement = "[Home_dir]";
			var homePath = Environment.GetEnvironmentVariable("HOME");
			if (homePath == null) {
				return log;
			}
			return log.Replace(homePath, pathReplacement);
		}

		private string RedactRimworldPaths(string log) {
			const string pathReplacement = "[Rimworld_dir]";
			// easiest way to get the game folder is one level up from dataPath
			var appPath = Path.GetFullPath(Application.dataPath);
			var pathParts = appPath.Split(Path.DirectorySeparatorChar).ToList();
			pathParts.RemoveAt(pathParts.Count - 1);
			appPath = pathParts.Join(Path.DirectorySeparatorChar.ToString());
			log = log.Replace(appPath, pathReplacement);
			if (Path.DirectorySeparatorChar != '/') {
				// log will contain mixed windows and unix style paths
				appPath = appPath.Replace(Path.DirectorySeparatorChar, '/');
				log = log.Replace(appPath, pathReplacement);
			}
			return log;
		}

		private string RedactRendererInformation(string log) {
			if (publishOptions.IncludePlatformInfo) return log;
			// apparently renderer information can appear multiple times in the log
			for (int i = 0; i < 5; i++) {
				var redacted = RedactString(log, "GfxDevice: ", "\nBegin MonoManager", "[Renderer information redacted]");
				if (log.Length == redacted.Length) break;
				log = redacted;
			}
			return log;
		}

		private string RedactPlayerConnectInformation(string log) {
			return RedactString(log, "PlayerConnection ", "Initialize engine", "[PlayerConnect information redacted]\n");
		}

		private string GetLogFileContents() {
			var filePath = HugsLibUtility.TryGetLogFilePath();
			if (filePath.NullOrEmpty() || !File.Exists(filePath)) {
				throw new FileNotFoundException("Log file not found:" + filePath);
			}
			var tempPath = Path.GetTempFileName();
			File.Delete(tempPath);
			// we need to copy the log file since the original is already opened for writing by Unity
			File.Copy(filePath, tempPath);
			var fileContents = File.ReadAllText(tempPath);
			File.Delete(tempPath);
			return "Log file contents:\n" + fileContents;
		}

		private string MakeLogTimestamp() {
			return string.Concat("Log uploaded on ", DateTime.Now.ToLongDateString(), ", ", DateTime.Now.ToLongTimeString(), "\n");
		}

		private string RedactString(string original, string redactStart, string redactEnd, string replacement) {
			var startIndex = original.IndexOf(redactStart, StringComparison.Ordinal);
			var endIndex = original.IndexOf(redactEnd, StringComparison.Ordinal);
			string result = original;
			if (startIndex >= 0 && endIndex >= 0) {
				var LogTail = original.Substring(endIndex);
				result = original.Substring(0, startIndex + redactStart.Length);
				result += replacement;
				result += LogTail;
			}
			return result;
		}

		private string ListHarmonyPatches() {
			var harmonyInstance = HugsLibController.Instance.HarmonyInst;
			var patchListing = HarmonyUtility.DescribePatchedMethods(harmonyInstance);

			return string.Concat("Active Harmony patches:\n",
				patchListing,
				patchListing.EndsWith("\n") ? "" : "\n",
				HarmonyUtility.DescribeHarmonyVersions(harmonyInstance), "\n");
		}

		private string ListPlatformInfo() {
			const string sectionTitle = "Platform information: ";
			if (publishOptions.IncludePlatformInfo) {
				return string.Concat(sectionTitle, "\nCPU: ",
					SystemInfo.processorType,
					"\nOS: ",
					SystemInfo.operatingSystem,
					"\nMemory: ",
					SystemInfo.systemMemorySize,
					" MB",
					"\n");
			} else {
				return sectionTitle + "(hidden, use publishing options to include)\n";
			}
		}

		private string ListActiveMods() {
			var builder = new StringBuilder();
			builder.Append("Loaded mods:\n");
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				builder.Append(modContentPack.Name);
				var versionFile = VersionFile.TryParse(modContentPack);
				if (versionFile != null && versionFile.OverrideVersion != null) {
					builder.AppendFormat("[ov:{0}]: ", versionFile.OverrideVersion);
				} else {
					builder.Append(": ");
				}
				var firstAssembly = true;
				var anyAssemblies = false;
				foreach (var loadedAssembly in modContentPack.assemblies.loadedAssemblies) {
					if (!firstAssembly) {
						builder.Append(", ");
					}
					firstAssembly = false;
					builder.Append(loadedAssembly.GetName().Name);
					builder.AppendFormat("({0})", AssemblyVersionInfo.ReadModAssembly(loadedAssembly, modContentPack));
					anyAssemblies = true;
				}
				if (!anyAssemblies) {
					builder.Append("(no assemblies)");
				}
				builder.Append("\n");
			}
			return builder.ToString();
		}

		// sanitizes a string for valid inclusion in JSON
		private static string CleanForJSON(string s) {
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
							var t = "000" + "X";
							sb.Append("\\u" + t.Substring(t.Length - 4));
						} else {
							sb.Append(c);
						}
						break;
				}
			}
			return sb.ToString();
		}

		internal static void RegisterSettings(ModSettingsPack pack) {
			optionsHandle = pack.GetHandle<LogPublisherOptions>("logPublisherSettings", null, null);
			if (optionsHandle.Value == null) optionsHandle.Value = new LogPublisherOptions();
			optionsHandle.VisibilityPredicate = () => false; // invisible, but can be reset by "Reset all settings"
		}
	}
}