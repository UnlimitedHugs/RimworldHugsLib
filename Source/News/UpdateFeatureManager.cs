using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using Verse;

namespace HugsLib.News {
	/// <summary>
	/// Stores the last displayed update news item for all mods. Shows the news dialog window when there are not yet displayed news items available.
	/// </summary>
	public class UpdateFeatureManager : PersistentDataManager {

		protected override string FileName {
			get { return "LastSeenNews.xml"; }
		}
		// highest version that has displayed features
		private readonly Dictionary<string, Version> knownVersions = new Dictionary<string, Version>();
		// current version if higher than last featured
		private readonly Dictionary<string, Version> freshVersions = new Dictionary<string, Version>();

		private SettingHandle<IgnoredNewsIds> IgnoredNewsProvidersSetting { get; set; }
		private SettingHandle<bool> ShowNewsSetting { get; set; }

		public UpdateFeatureManager() {
			LoadData();
		}

		public void InspectActiveMod(string modId, Version currentVersion) {
			var knownVersion = TryGetKnownVersion(modId);
			if (knownVersion == null || currentVersion > knownVersion) {
				var existingFreshVersion = freshVersions.TryGetValue(modId);
				freshVersions[modId] = existingFreshVersion == null || currentVersion > existingFreshVersion ? 
					currentVersion : existingFreshVersion;
			}
		}

		public bool TryShowDialog(bool manuallyOpened) {
			if ((!ShowNewsSetting.Value && !manuallyOpened) || (freshVersions.Count == 0 && !manuallyOpened)) return false;
			List<UpdateFeatureDef> defsToShow;
			if (manuallyOpened) {
				defsToShow = DefDatabase<UpdateFeatureDef>.AllDefs.ToList();
			} else {
				// try to pull defs newer than already featured, remember highest pulled version
				defsToShow = new List<UpdateFeatureDef>();
				foreach (var freshVersionPair in freshVersions) {
					var modId = freshVersionPair.Key;
					if(IgnoredNewsProvidersSetting.Value.Contains(modId)) continue;
					var freshVersion = freshVersionPair.Value;
					var knownVersion = TryGetKnownVersion(modId) ?? new Version();
					Version highestVersionWithFeature = null;
					foreach (var def in DefDatabase<UpdateFeatureDef>.AllDefs) {
						if (def.modIdentifier != modId) continue;
						if (def.Version <= knownVersion || def.Version > freshVersion) continue;
						defsToShow.Add(def);
						if (highestVersionWithFeature == null || def.Version > highestVersionWithFeature) highestVersionWithFeature = def.Version;
					}
					if (highestVersionWithFeature != null) knownVersions[modId] = highestVersionWithFeature;
				}
			}
			if (defsToShow.Count > 0) {
				// sort defs by modNameReadable, Version
				defsToShow.Sort((d1, d2) => {
					if (d1.modNameReadable == d2.modNameReadable) return d1.Version.CompareTo(d2.Version);
					return String.Compare(d1.modNameReadable, d2.modNameReadable, StringComparison.Ordinal);
				});
				Find.WindowStack.Add(new Dialog_UpdateFeatures(defsToShow, IgnoredNewsProvidersSetting));
				SaveData();
				return true;
			}
			return false;
		}

		public void ClearSavedData() {
			knownVersions.Clear();
			SaveData();
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
			if(IgnoredNewsProvidersSetting.Value == null) IgnoredNewsProvidersSetting.Value = new IgnoredNewsIds();
			IgnoredNewsProvidersSetting.VisibilityPredicate = () => false;
		}

		protected override void LoadFromXml(XDocument xml) {
			knownVersions.Clear();
			if(xml.Root == null) throw new Exception("missing root node");
			foreach (var element in xml.Root.Elements()) {
				knownVersions.Add(element.Name.ToString(), new Version(element.Value));
			}
		}

		protected override void WriteXml(XDocument xml) {
			var root = new XElement("mods");
			xml.Add(root);
			foreach (var pair in knownVersions) {
				root.Add(new XElement(pair.Key, new XText(pair.Value.ToString())));
			}
		}

		private Version TryGetKnownVersion(string modId) {
			Version knownVersion;
			knownVersions.TryGetValue(modId, out knownVersion);
			return knownVersion;
		}

		public class IgnoredNewsIds : SettingHandleConvertible {
			private const char SerializationSeparator = '|';
			private HashSet<string> ignoredModIds = new HashSet<string>();

			public bool Contains(string modId) {
				return ignoredModIds.Contains(modId);
			}

			public void SetIgnored(string modId, bool ignore) {
				var changed = ignore ? ignoredModIds.Add(modId) : ignoredModIds.Remove(modId);
				if(changed) HugsLibController.SettingsManager.SaveChanges();
			}
			
			public override void FromString(string settingValue) {
				if (string.IsNullOrEmpty(settingValue)) return;
				ignoredModIds = new HashSet<string>(settingValue.Split(SerializationSeparator));
			}

			public override string ToString() {
				return ignoredModIds.Join(SerializationSeparator.ToString());
			}
		}
	}
}