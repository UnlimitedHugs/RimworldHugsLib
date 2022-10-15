using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.News;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Spotter {
	/// <summary>
	/// Keeps track of mod packageIds that ever were loaded together with HugsLib
	/// by the player and the first/last time they were seen.
	/// </summary>
	public partial class ModSpottingManager : PersistentDataManager, IModSpotterDevActions {
		private readonly Dictionary<string, TrackingEntry> entries = 
			new Dictionary<string, TrackingEntry>(StringComparer.OrdinalIgnoreCase);
		private readonly IModLogger logger = HugsLibController.Logger;
		private bool erroredOnLoad;
		private Task? inspectTask;

		protected override string FileName => "SpottedMods.xml";

		protected override bool SuppressLoadSaveExceptions => false;

		internal ModSpottingManager() {
		}

		internal ModSpottingManager(string overrideFilePath, IModLogger logger) {
			this.logger = DataManagerLogger = logger;
			OverrideFilePath = overrideFilePath;
		}
		
		/// <summary>
		/// Sets the "first time seen" status of a packageId until the game is restarted.
		/// </summary>
		/// <exception cref="ArgumentNullException">Throws on null packageId</exception>
		public void SetFirstTimeSeen(string packageId, bool setFirstTimeSeen) {
			if (packageId == null) throw new ArgumentNullException(nameof(packageId));
			WaitForInspectionCompletion();
			var currentlyFirstTimeSeen = FirstTimeSeen(packageId);
			if (setFirstTimeSeen != currentlyFirstTimeSeen) {
				entries[packageId] = new TrackingEntry(packageId) {FirstTimeSeen = setFirstTimeSeen};
			}
		}
		
		/// <summary>
		/// Returns true if the provided packageId was recorded for the first time during the current run.
		/// </summary>
		/// <exception cref="ArgumentNullException">Throws on null packageId</exception>
		public bool FirstTimeSeen(string packageId) {
			if (packageId == null) throw new ArgumentNullException(nameof(packageId));
			WaitForInspectionCompletion();
			return entries!.TryGetValue(packageId)?.FirstTimeSeen ?? false;
		}
		
		/// <summary>
		/// Returns true if the provided mod packageId was at any time seen running together with HugsLib.
		/// </summary>
		/// <exception cref="ArgumentNullException">Throws on null packageId</exception>
		public bool AnytimeSeen(string packageId) {
			if (packageId == null) throw new ArgumentNullException(nameof(packageId));
			WaitForInspectionCompletion();
			return entries.ContainsKey(packageId);
		}

		bool IModSpotterDevActions.GetFirstTimeUserStatus(string packageId) {
			return FirstTimeSeen(packageId);
		}

		void IModSpotterDevActions.ToggleFirstTimeUserStatus(string packageId) {
			var newStatus = !FirstTimeSeen(packageId);
			SetFirstTimeSeen(packageId, newStatus);
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

		private void UpdateEntriesWithPackageIds(IEnumerable<string> packageIds) {
			foreach (var packageId in packageIds) {
				if (!entries.ContainsKey(packageId)) {
					entries.Add(packageId, new TrackingEntry(packageId) {FirstTimeSeen = true});
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
				logger.Warning(
					$"Skipping {nameof(ModSpottingManager)} saving to preserve improperly " +
					"loaded file data. Fix or delete the data file and try again."
				);
				return;
			}
			try {
				SaveData();
			} catch (Exception) {
				// suppress exception, warning is logged to console
			}
		}
	}
}