using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Checks the current version of the library against the About.xml -> requiredLibraryVersion of all mods.
	/// Shows a popup window (<see cref="Dialog_LibraryUpdateRequired"/>) if one of the loaded mods requires a
	/// more recent version of the library. 
	/// </summary>
	internal class LibraryVersionChecker {
		private readonly Version currentLibraryVersion;
		private readonly IModLogger logger;

		public LibraryVersionChecker(Version currentLibraryVersion, IModLogger logger) {
			this.currentLibraryVersion = currentLibraryVersion;
			this.logger = logger;
		}

		internal IEnumerable<(string modName, Version requiredVersion)> RequiredLibraryVersionEnumerator { get; set; } =
			new EnumerateRequiredLibraryVersionsInMods();

		public void OnEarlyInitialize() {
			var versionCheckTask = RunVersionCheckAsync();
			// queuing a callback to LongEventHandler ensures thread safety with other mods when opening our window
			LongEventHandler.QueueLongEvent(
				() => ShowVersionMismatchDialogIfNeeded(versionCheckTask), null, false, null);
		}

		private void ShowVersionMismatchDialogIfNeeded(Task<VersionMismatchReport?> versionCheckTask) {
			// background task should be long since finished, so the wait should be a no-op
			if (TryWaitForTaskResult(versionCheckTask, TimeSpan.FromSeconds(1)) is { } report) {
				Find.WindowStack.Add(new Dialog_LibraryUpdateRequired(report.ModName, report.ExpectedVersion));
			}
		}

		internal Task<VersionMismatchReport?> RunVersionCheckAsync() {
			return Task.Run(() => {
				VersionMismatchReport? report = null;
				try {
					var (modName, highestRequiredVersion) = RequiredLibraryVersionEnumerator
						.OrderByDescending(t => t.requiredVersion)
						.FirstOrDefault();
					if (highestRequiredVersion != null && highestRequiredVersion > currentLibraryVersion) {
						report = new VersionMismatchReport(modName, highestRequiredVersion);
					}
				} catch (Exception e) {
					logger.ReportException(e);
				}
				return report;
			});
		}

		internal VersionMismatchReport? TryWaitForTaskResult(Task<VersionMismatchReport?> task, TimeSpan waitTime) {
			try {
				var waitSuccess = task.Wait(waitTime); 
				if (waitSuccess && task.IsCompleted) {
					return task.Result;
				}
				if (!waitSuccess) {
					throw new Exception(
						$"Ran out of time waiting for {nameof(LibraryVersionChecker)} background task completion.");
				}
			} catch (Exception e) {
				logger.ReportException(e);
			}
			return null;
		}

		public struct VersionMismatchReport {
			public string ModName { get; }
			public Version ExpectedVersion { get; }
			public VersionMismatchReport(string modName, Version expectedVersion) {
				ModName = modName;
				ExpectedVersion = expectedVersion;
			}
		}

		private class EnumerateRequiredLibraryVersionsInMods : IEnumerable<(string, Version)> {
			public IEnumerator<(string, Version)> GetEnumerator() {
				foreach (var contentPack in LoadedModManager.RunningMods) {
					var requiredVersion = VersionFile.TryParseVersionFile(contentPack)?.RequiredLibraryVersion;
					if (requiredVersion != null) {
						yield return (contentPack.Name, requiredVersion);
					}
				}
			}
			IEnumerator IEnumerable.GetEnumerator() {
				return GetEnumerator();
			}
		}
	}
}