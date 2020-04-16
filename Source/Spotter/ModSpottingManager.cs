using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using HugsLib.Core;
using Verse;

namespace HugsLib.Spotter {
	/// <summary>
	/// Keeps track of mod packageIds that ever were loaded together with HugsLib
	/// by the player and the first/last time they were seen.
	/// </summary>
	public partial class ModSpottingManager : PersistentDataManager {
		private readonly Dictionary<string, TrackingEntry> entries = 
			new Dictionary<string, TrackingEntry>(StringComparer.OrdinalIgnoreCase);
		private bool erroredOnLoad;
		private Task inspectTask;

		protected override string FileName {
			get { return "SpottedMods.xml"; }
		}
		
		protected override bool SuppressLoadSaveExceptions {
			get { return false; }
		}
		
		internal ICurrentDateTimeSource DateTimeSource { get; set; } = new SystemDateTimeSource();
		
		internal ModSpottingManager() {
		}

		internal ModSpottingManager(string overrideFilePath) {
			OverrideFilePath = overrideFilePath;
		}
		
		/// <summary>
		/// Toggles the "first time seen" status of a packageId until the game is restarted.
		/// If the status is modified, changes are immediately written to disk.
		/// </summary>
		/// <exception cref="ArgumentNullException">Throws on null packageId</exception>
		public void ToggleFirstTimeSeen(string packageId, bool setFirstTimeSeen) {
			if (packageId == null) throw new ArgumentNullException(nameof(packageId));
			WaitForInspectionCompletion();
			var now = DateTimeSource.Now;
			var currentlyFirstTimeSeen = FirstTimeSeen(packageId);
			if (setFirstTimeSeen != currentlyFirstTimeSeen) {
				var entry = new TrackingEntry(packageId, now, now);
				entries[packageId] = entry;
				if (!setFirstTimeSeen) {
					entry.PreviouslyLastSeen = now;
				}
				SaveEntries();
			}
		}
		
		/// <summary>
		/// Returns true if the provided packageId was recorded for the first time during the current run.
		/// </summary>
		/// <exception cref="ArgumentNullException">Throws on null packageId</exception>
		public bool FirstTimeSeen(string packageId) {
			if (packageId == null) throw new ArgumentNullException(nameof(packageId));
			WaitForInspectionCompletion();
			return entries.TryGetValue(packageId, out var entry) && !entry.PreviouslyLastSeen.HasValue;
		}
		
		/// <summary>
		/// Returns the previous time the provided packageId has been recorded.
		/// Returns null if packageId is unknown or the current run is the first time the packageId has been recorded.
		/// </summary>
		/// <exception cref="ArgumentNullException">Throws on null packageId</exception>
		public DateTime? TryGetLastSeenTime(string packageId) {
			if (packageId == null) throw new ArgumentNullException(nameof(packageId));
			WaitForInspectionCompletion();
			return entries.TryGetValue(packageId)?.PreviouslyLastSeen;
		}

		/// <summary>
		/// Returns the first time the provided packageId has been recorded.
		/// Return null if the packageId has never been seen running together with HugsLib.
		/// </summary>
		/// <exception cref="ArgumentNullException">Throws on null packageId</exception>
		public DateTime? TryGetFirstSeenTime(string packageId) {
			if (packageId == null) throw new ArgumentNullException(nameof(packageId));
			WaitForInspectionCompletion();
			return entries.TryGetValue(packageId)?.FirstSeen;
		}

		private void WaitForInspectionCompletion() {
			bool waitSuccess = inspectTask?.Wait(TimeSpan.FromSeconds(3)) ?? true;
			if (!waitSuccess) {
				throw new TaskCanceledException($"Ran out of time waiting for {nameof(ModSpottingManager)} " +
												"background task completion.");
			}
		}

		internal void OnEarlyInitialize() {
			// We don't need the results of this operation right away, so we can happily offload it to a worker thread.
			RunInspectPackageIdsBackgroundTask(
				ModsConfig.ActiveModsInLoadOrder.Select(m => m.PackageIdPlayerFacing)
			);
		}

		internal void InspectPackageIds(IEnumerable<string> packageIds) {
			RunInspectPackageIdsBackgroundTask(packageIds);
			WaitForInspectionCompletion();
		}

		private void RunInspectPackageIdsBackgroundTask(IEnumerable<string> packageIds) {
			try {
				var packageIdsArray = packageIds.ToArray();
				inspectTask = Task.Run(() => InspectPackageIdsSync(packageIdsArray));
			} catch (Exception e) {
				HugsLibController.Logger.Error($"Error during {nameof(ModSpottingManager)} background task: {e}");
			}
		}

		private void InspectPackageIdsSync(string[] packageIds) {
			LoadEntries();
			UpdateEntriesWithPackageIds(packageIds);
			SaveEntries();
		}

		private void UpdateEntriesWithPackageIds(string[] packageIds) {
			UpdateEntriesLastSeenTime(packageIds);
			AddNewlySeenEntries(packageIds);
		}

		private void UpdateEntriesLastSeenTime(IEnumerable<string> packageIds) {
			var currentPackageIdSet = new HashSet<string>(packageIds, StringComparer.InvariantCultureIgnoreCase);
			var now = DateTimeSource.Now;
			foreach (var entry in entries.Values) {
				entry.PreviouslyLastSeen = entry.LastSeen;
				if (currentPackageIdSet.Contains(entry.PackageId)) {
					entry.LastSeen = now;
				}
			}
		}

		private void AddNewlySeenEntries(IEnumerable<string> packageIds) {
			var now = DateTimeSource.Now;
			foreach (var packageId in packageIds) {
				if (!entries.ContainsKey(packageId)) {
					entries.Add(packageId, new TrackingEntry(packageId, now, now));
				}
			}
		}

		protected override void LoadFromXml(XDocument xml) {
			entries.Clear();
			if (xml.Root == null) throw new NullReferenceException("Missing root node");
			foreach (var entryNode in xml.Root.Elements()) {
				try {
					var entry = TrackingEntry.FromXMLElement(entryNode);
					entries[entry.PackageId] = entry;
				} catch (Exception e) {
					throw new FormatException($"Failed to parse entry:\n{entryNode}\nException: {e}");
				}

			}
		}

		protected override void WriteXml(XDocument xml) {
			var root = new XElement("Mods");
			xml.Add(root);
			foreach (var entry in entries.Values) {
				root.Add(entry.Serialize());
			}
		}

		private void LoadEntries() {
			erroredOnLoad = false;
			try {
				LoadData();
			} catch (Exception) {
				// suppress exception, warning is logged to console
				erroredOnLoad = true;
			}
		}

		private void SaveEntries() {
			if (erroredOnLoad) {
				HugsLibController.Logger.Warning(
					$"Skipping {nameof(ModSpottingManager)} saving to preserve improperly " +
					"loaded file data. Fix or delete the data file and try again."
				);
			} else {
				try {
					SaveData();
				} catch (Exception) {
					// suppress exception, warning is logged to console
				}
			}
		}
	}
}