using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using HugsLib.Core;
using HugsLib.Utils;
using UnityEngine;
using Verse;
using Object = System.Object;

namespace HugsLib.Logs {
	/**
	 * Collects the game logs and loaded mods and posts the information on GitHub as a gist.
	 * The url is then shortened using Git.io for convenience.
	 */
	public class LogPublisher {
		private const string RequestUserAgent = "HugsLib_log_uploader";
		private const string OutputLogFilename = "output_log.txt";
		private const string GistApiUrl = "https://api.github.com/gists";
		private const string GistPayloadJson = "{{\"description\":\"{0}\",\"public\":true,\"files\":{{\"{1}\":{{\"content\":\"{2}\"}}}}}}";
		private const string GistDescription = "Rimworld output log published using HugsLib";
		private const string SuccessStatusResponse = "201 Created";
		private const string ShortenerUrl = "https://git.io/";
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
		private bool userAborted;

		private Thread workerThread;

		public void OnGUI() {
			if (Event.current.type != EventType.KeyDown) return;
			if (HugsLibKeyBingings.PublishLogs.JustPressed && HugsLibUtility.ControlIsHeld) {
				ShowPublishPrompt();
			}
		}

		public void ShowPublishPrompt() {
			if (PublisherIsReady()) {
				Find.WindowStack.Add(new Dialog_Confirm("HugsLib_logs_shareConfirmMessage".Translate(), OnPublishConfirmed, false, "HugsLib_logs_shareConfirmTitle".Translate()));
			} else {
				ShowPublishDialog();
			}
		}

		public void AbortUpload() {
			if(Status != PublisherStatus.Uploading && Status != PublisherStatus.Shortening) return;
			userAborted = true;
			if (workerThread != null && workerThread.IsAlive) {
				workerThread.Interrupt();
			}
			ErrorMessage = "Aborted by user";
			Status = PublisherStatus.Error;
		}

		public void BeginUpload() {
			if(!PublisherIsReady()) return;
			Status = PublisherStatus.Uploading;
			ErrorMessage = null;
			userAborted = false;
			
#if TEST_MOCK_UPLOAD
			MockUpload();
			return;
#endif

			var collatedData = PrepareLogData();
			if (collatedData == null) {
				ErrorMessage = "Failed to collect data";
				Status = PublisherStatus.Error;
				return;
			}

			ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallback;

			workerThread = new Thread(() => {
				try {
					using (var client = new WebClient()) {
						client.Headers.Add("Authorization", "token " + GitHubAuthToken);
						client.Headers.Add("User-Agent", RequestUserAgent);
						collatedData = CleanForJSON(collatedData);
						var payload = String.Format(GistPayloadJson, GistDescription, OutputLogFilename, collatedData);
						var response = client.UploadString(GistApiUrl, payload);
						var status = client.ResponseHeaders.Get("Status");
						if (status == SuccessStatusResponse) {
							OnUploadComplete(response);
						} else {
							OnRequestError(status);
						}
					}
				} catch (Exception e) {
					if (userAborted) return;
					OnRequestError(e.Message);
					HugsLibController.Logger.Warning("Exception during log publishing (gist creation): " + e);
				}
			});
			workerThread.Start();
		}

		// this is for testing the uploader gui without making requests	
		private void MockUpload() {
			workerThread = new Thread(() => {
				Thread.Sleep(1500);
				Status = PublisherStatus.Shortening;
				Thread.Sleep(1500);
				Status = PublisherStatus.Done;
				ResultUrl = Rand.Int.ToString();
			});
			workerThread.Start();
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
			Status = PublisherStatus.Error;
		}

		private void OnUploadComplete(string response) {
			var matchedUrl = TryExtractGistUrlFromUploadResponse(response);
			if (matchedUrl == null) {
				OnRequestError("Failed to parse response");
				return;
			}
			ResultUrl = matchedUrl;
			LongEventHandler.ExecuteWhenFinished(BeginUrlShortening);
		}

		private void BeginUrlShortening() {
			Status = PublisherStatus.Shortening;

			workerThread = new Thread(() => {
				try {
					using (var client = new WebClient()) {
						client.Headers.Add("User-Agent", RequestUserAgent);
						client.Headers.Add("Content-Type", "multipart/form-data");
						var payload = "url=" + ResultUrl;
						client.UploadString(ShortenerUrl, payload);
						var status = client.ResponseHeaders.Get("Status");
						var location = client.ResponseHeaders.Get("Location");
						if (status == SuccessStatusResponse) {
							OnUrlShorteningComplete(location);
						} else {
							OnRequestError(status);
						}
					}
				} catch (Exception e) {
					// Git.io shortening has failed, just return the original url as fallback
					if (userAborted) return;
					OnUrlShorteningComplete(ResultUrl);
					HugsLibController.Logger.Warning("Exception during log publishing (url shortening): " + e);
				}
			});
			workerThread.Start();
		}

		private void OnUrlShorteningComplete(string shortUrl) {
			ResultUrl = shortUrl;
			Status = PublisherStatus.Done;
		}

		private string TryExtractGistUrlFromUploadResponse(string response) {
			var match = UploadResponseUrlMatch.Match(response);
			if (!match.Success) return null;
			return match.Groups[1].ToString();
		}

		private bool CertificateValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			// we don't care about the certificate, assume all is well
			return true;
		}

		private bool PublisherIsReady() {
			return Status == PublisherStatus.Ready || Status == PublisherStatus.Done || Status == PublisherStatus.Error;
		}

		private string PrepareLogData() {
			try {
				var logSection = GetLogFileContents();
				// redact logs for privacy
				logSection = RedactRimworldPaths(logSection);
				logSection = RedactPlayerConnectInformation(logSection);
				logSection = RedactRendererInformation(logSection);
				if (logSection == null) return null;
				return String.Concat(MakeLogTimestamp(), ListActiveMods(), "\n", logSection);
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
			return null;
		}

		private string RedactRimworldPaths(string log) {
			const string pathReplacement = "[Rimworld_dir]";
			var appPath = Path.GetFullPath(Application.dataPath);
			var pathParts = appPath.Split(Path.DirectorySeparatorChar).ToList();
			pathParts.RemoveAt(pathParts.Count-1);
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
			return RedactString(log, "GfxDevice: ", "\nBegin MonoManager", "[Renderer information redacted]");
		}

		private string RedactPlayerConnectInformation(string log) {
			return RedactString(log, "PlayerConnection ", "\nInitialize engine", "[PlayerConnect information redacted]");
		}

		private string GetLogFileContents() {
			var filePath = Path.Combine(Application.dataPath, OutputLogFilename);
			if (!File.Exists(filePath)) return null;
			var tempPath = Path.GetTempFileName();
			File.Delete(tempPath);
			// we need to copy the log file since the original is already opened for writing by Unity
			File.Copy(filePath, tempPath);
			var fileContents = File.ReadAllText(tempPath);
			File.Delete(tempPath);
			return "Log file contents:\n" + fileContents;
		}

		private string MakeLogTimestamp() {
			return String.Concat("Log uploaded on ", DateTime.Now.ToLongDateString(), ", ", DateTime.Now.ToLongTimeString(), "\n");
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

		private string ListActiveMods() {
			var builder = new StringBuilder();
			builder.Append("Loaded mods:\n");
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				builder.Append(modContentPack.Name);
				var versionFile = VersionFile.TryParseVersionFile(modContentPack);
				if (versionFile != null && versionFile.OverrideVersion != null) {
					builder.AppendFormat("[{0}]: ", versionFile.OverrideVersion);
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
					builder.AppendFormat("({0})", loadedAssembly.GetName().Version);
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
			if (String.IsNullOrEmpty(s)) {
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
	}
}