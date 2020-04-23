using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using Verse;

namespace HugsLib.News {
	/// <summary>
	/// Stores the highest displayed update news version for all mods that provide update news via <see cref="UpdateFeatureDef"/>.
	/// Defs are loaded from the News folder in the root mod directory.
	/// </summary>
	public class UpdateFeatureManager : PersistentDataManager, IUpdateFeaturesDevActions {
		internal const string UpdateFeatureDefFolder = "News/";

		protected override string FileName {
			get { return "LastSeenNews.xml"; }
		}

		// the highest news item version we have previously displayed for a UpdateFeatureDef.OwningModId
		private readonly Dictionary<string, Version> highestSeenVersions = new Dictionary<string, Version>();

		private SettingHandle<IgnoredNewsIds> IgnoredNewsProvidersSetting { get; set; }
		private SettingHandle<bool> ShowNewsSetting { get; set; }

		public UpdateFeatureManager() {
			LoadData();
		}

		// TodoMajor: remove this
		[Obsolete("Mods no longer need to call in, fresh news are automatically detected based on their defs")]
		public void InspectActiveMod(string modId, Version currentVersion) {
		}

		internal void OnEarlyInitialize() {
			// offload reading and parsing XML files to a worker thread
			var loadingTask = Task.Run(UpdateFeatureDefLoader.LoadUpdateFeatureDefNodes);
			
			// this should put us just before backstory loading in the DoPlayLoad cycle
			// we inject our defs early on to take advantage of the stock translation injection system
			LongEventHandler.ExecuteWhenFinished(ResolveAndInjectNewsDefs);
			
			void ResolveAndInjectNewsDefs() {
				// this must be done synchronously to avoid creating potential race conditions with other mods
				try {
					if (!loadingTask.Wait(TimeSpan.FromSeconds(3)))
						throw new InvalidOperationException("XML loading did not resolve in time");
					var (nodes, errors) = loadingTask.Result;
					UpdateFeatureDefLoader.HandleDefLoadingErrors(errors);
					UpdateFeatureDefLoader.ResolveAndInjectUpdateFeatureDefs(nodes);
				} catch (Exception e) {
					HugsLibController.Logger.Error("Failed to load UpdateFeatureDefs: " + e);
				}
			}
		}

		/// <summary>
		/// Shows the news dialog window when there are not yet displayed news items available.
		/// </summary>
		/// <param name="manuallyOpened">Pass true to disable filtering based on what has 
		/// and has not been seen and open the dialog with all available news items.</param>
		/// <returns>true, if there have been found news items that were not displayed before, and the dialog has been opened</returns>
		public bool TryShowDialog(bool manuallyOpened) {
			if (ShowNewsSetting.Value || manuallyOpened) {
				var allNewsFeatureDefs = DefDatabase<UpdateFeatureDef>.AllDefs;
				List<UpdateFeatureDef> defsToShow;
				bool seenVersionsNeedSaving;
				if (manuallyOpened) {
					defsToShow = allNewsFeatureDefs.ToList();
					seenVersionsNeedSaving = true;
				} else {
					var filteredByVersion = EnumerateFeatureDefsWithMoreRecentVersions(allNewsFeatureDefs, highestSeenVersions)
							.Where(def => !NewsProviderOwningModIdIsIgnored(def.OwningModId))
							.ToArray();
					UpdateMostRecentKnownFeatureVersions(filteredByVersion, highestSeenVersions);
					seenVersionsNeedSaving = filteredByVersion.Length > 0;
					
					var filteredByAudience = FilterFeatureDefsByMatchingAudience(filteredByVersion,
						HugsLibController.Instance.ModSpotter.FirstTimeSeen,
						e => HugsLibController.Logger.ReportException(e)
					);
					defsToShow = filteredByAudience.ToList();
				}
				if (seenVersionsNeedSaving) {
					SaveData();
				}
				if (defsToShow.Count > 0) {
					SortFeatureDefsByModNameAndVersion(defsToShow);
					var newsDialog = manuallyOpened
						? new Dialog_UpdateFeaturesFiltered(defsToShow, IgnoredNewsProvidersSetting, 
							this, HugsLibController.Instance.ModSpotter)
						: new Dialog_UpdateFeatures(defsToShow, IgnoredNewsProvidersSetting);
					Find.WindowStack.Add(newsDialog);
					return true;
				}
			}
			return false;
		}

		private static IEnumerable<UpdateFeatureDef> EnumerateFeatureDefsWithMoreRecentVersions(
			IEnumerable<UpdateFeatureDef> featureDefs, Dictionary<string, Version> highestSeenVersions) {
			foreach (var featureDef in featureDefs) {
				var ownerId = featureDef.OwningModId;
				if (!ownerId.NullOrEmpty()) {
					var highestSeenVersion = highestSeenVersions.TryGetValue(ownerId);
					if (highestSeenVersion == null || featureDef.Version > highestSeenVersion) {
						yield return featureDef;
					}
				}
			}
		}

		private bool NewsProviderOwningModIdIsIgnored(string ownerId) {
			return IgnoredNewsProvidersSetting.Value.Contains(ownerId);
		}

		private static void UpdateMostRecentKnownFeatureVersions(
			IEnumerable<UpdateFeatureDef> shownNewsFeatureDefs, Dictionary<string, Version> highestSeenVersions) {
			foreach (var featureDef in shownNewsFeatureDefs) {
				var ownerId = featureDef.OwningModId;
				var highestSeenVersion = highestSeenVersions.TryGetValue(ownerId);
				if (highestSeenVersion == null || featureDef.Version > highestSeenVersion) {
					highestSeenVersions[ownerId] = featureDef.Version;
				}
			}
		}

		internal static IEnumerable<UpdateFeatureDef> FilterFeatureDefsByMatchingAudience(
			IEnumerable<UpdateFeatureDef> featureDefs, Predicate<string> packageIdFirstTimeSeen, Action<Exception> exceptionReporter) {
			foreach (var featureDef in featureDefs) {
				bool firstTimeSeen;
				try {
					var owningPackageId = featureDef.OwningPackageId;
					firstTimeSeen = packageIdFirstTimeSeen(owningPackageId);
				} catch (Exception e) {
					exceptionReporter(e);
					continue;
				}
				var requiredTargetAudienceFlag = firstTimeSeen
					? UpdateFeatureTargetAudience.NewPlayers
					: UpdateFeatureTargetAudience.ReturningPlayers;
				if ((featureDef.targetAudience & requiredTargetAudienceFlag) != 0) {
					yield return featureDef;
				}
			}
		}

		private static void SortFeatureDefsByModNameAndVersion(List<UpdateFeatureDef> featureDefs) {
			// sort defs by modNameReadable first, Version of the news item second
			featureDefs.Sort((def1, def2) => def1.modNameReadable != def2.modNameReadable
				? string.Compare(def1.modNameReadable, def2.modNameReadable, StringComparison.Ordinal)
				: def1.Version.CompareTo(def2.Version));
		}

		internal void RegisterSettings(ModSettingsPack pack) {
			ShowNewsSetting = pack.GetHandle("modUpdateNews", "HugsLib_setting_showNews_label".Translate(), "HugsLib_setting_showNews_desc".Translate(), true);
			var allNewsHandle = pack.GetHandle("showAllNews", "HugsLib_setting_allNews_label".Translate(), "HugsLib_setting_allNews_desc".Translate(), false);
			allNewsHandle.Unsaved = true;
			allNewsHandle.CustomDrawer = rect => {
				if (Widgets.ButtonText(rect, "HugsLib_setting_allNews_button".Translate())) {
					if (!TryShowDialog(true)) {
						Find.WindowStack.Add(new Dialog_MessageBox("HugsLib_setting_allNews_fail".Translate()));
					}
				}
				return false;
			};
			IgnoredNewsProvidersSetting = pack.GetHandle<IgnoredNewsIds>("ignoredUpdateNews", null, null);
			if (IgnoredNewsProvidersSetting.Value == null) {
				IgnoredNewsProvidersSetting.Value = new IgnoredNewsIds();
				IgnoredNewsProvidersSetting.HasUnsavedChanges = false;
			}
			IgnoredNewsProvidersSetting.NeverVisible = true;
			IgnoredNewsProvidersSetting.Value.Handle = IgnoredNewsProvidersSetting;
		}

		protected override void LoadFromXml(XDocument xml) {
			highestSeenVersions.Clear();
			if (xml.Root == null) throw new Exception("missing root node");
			foreach (var element in xml.Root.Elements()) {
				highestSeenVersions.Add(element.Name.ToString(), new Version(element.Value));
			}
		}

		protected override void WriteXml(XDocument xml) {
			var root = new XElement("mods");
			xml.Add(root);
			foreach (var pair in highestSeenVersions) {
				root.Add(new XElement(pair.Key, new XText(pair.Value.ToString())));
			}
		}

		Version IUpdateFeaturesDevActions.GetLastSeenNewsVersion(string modIdentifier) {
			return highestSeenVersions.TryGetValue(modIdentifier);
		}

		IEnumerable<UpdateFeatureDef> IUpdateFeaturesDevActions.ReloadAllUpdateFeatureDefs() {
			UpdateFeatureDefLoader.ReloadAllUpdateFeatureDefs();
			return DefDatabase<UpdateFeatureDef>.AllDefs;
		}

		bool IUpdateFeaturesDevActions.TryShowAutomaticNewsPopupDialog() {
			return TryShowDialog(false);
		}

		void IUpdateFeaturesDevActions.SetLastSeenNewsVersion(string modIdentifier, Version version) {
			var changesNeedSaving = false;
			if (version != null) {
				highestSeenVersions[modIdentifier] = version;
				changesNeedSaving = true;
			} else {
				if (highestSeenVersions.ContainsKey(modIdentifier)) {
					highestSeenVersions.Remove(modIdentifier);
					changesNeedSaving = true;
				}
			}
			if (changesNeedSaving) {
				SaveData();
			}
		}

		public class IgnoredNewsIds : SettingHandleConvertible, IIgnoredNewsProviderStore {
			private const char SerializationSeparator = '|';
			private HashSet<string> ignoredOwnerIds = new HashSet<string>();

			public SettingHandle<IgnoredNewsIds> Handle { private get; set; }

			public bool Contains(string ownerId) {
				return ignoredOwnerIds.Contains(ownerId);
			}

			public void SetIgnored(string ownerId, bool ignore) {
				var changed = ignore ? ignoredOwnerIds.Add(ownerId) : ignoredOwnerIds.Remove(ownerId);
				if (changed) Handle.ForceSaveChanges();
			}

			public override bool ShouldBeSaved {
				get { return ignoredOwnerIds.Count > 0; }
			}

			public override void FromString(string settingValue) {
				if (string.IsNullOrEmpty(settingValue)) return;
				ignoredOwnerIds = new HashSet<string>(settingValue.Split(SerializationSeparator));
			}

			public override string ToString() {
				return ignoredOwnerIds.Join(SerializationSeparator.ToString());
			}
		}
	}

	internal interface IIgnoredNewsProviderStore {
		bool Contains(string providerId);
	}
}