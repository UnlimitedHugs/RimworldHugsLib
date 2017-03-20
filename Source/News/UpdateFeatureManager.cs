using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using HugsLib.Core;
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
		private readonly Dictionary<string, VersionShort> knownVersions = new Dictionary<string, VersionShort>();
		// current version if higher than last featured
		private readonly Dictionary<string, VersionShort> freshVersions = new Dictionary<string, VersionShort>(); 

		public UpdateFeatureManager() {
			LoadData();
		}

		public void InspectActiveMod(string modId, VersionShort currentVersion) {
			var knownVersion = TryGetKnownVersion(modId);
			if (knownVersion == null || currentVersion > knownVersion) {
				freshVersions.Add(modId, currentVersion);
			}
		}

		public bool TryShowDialog(bool includeSeen = false) {
			if (freshVersions.Count == 0 && !includeSeen) return false;
			List<UpdateFeatureDef> defsToShow;
			if (includeSeen) {
				defsToShow = DefDatabase<UpdateFeatureDef>.AllDefs.ToList();
			} else {
				// try to pull defs newer than already featured, remember highest pulled version
				defsToShow = new List<UpdateFeatureDef>();
				foreach (var freshVersionPair in freshVersions) {
					var modId = freshVersionPair.Key;
					var freshVersion = freshVersionPair.Value;
					var knownVersion = TryGetKnownVersion(modId) ?? new VersionShort();
					VersionShort highestVersionWithFeature = null;
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
				Find.WindowStack.Add(new Dialog_UpdateFeatures(defsToShow));
				SaveData();
				return true;
			}
			return false;
		}

		public void ClearSavedData() {
			knownVersions.Clear();
			SaveData();
		}

		protected override void LoadFromXml(XDocument xml) {
			knownVersions.Clear();
			if(xml.Root == null) throw new Exception("missing root node");
			foreach (var element in xml.Root.Elements()) {
				knownVersions.Add(element.Name.ToString(), VersionShort.Parse(element.Value));
			}
		}

		protected override void WriteXml(XDocument xml) {
			var root = new XElement("mods");
			xml.Add(root);
			foreach (var pair in knownVersions) {
				root.Add(new XElement(pair.Key, new XText(pair.Value.ToString())));
			}
		}

		private VersionShort TryGetKnownVersion(string modId) {
			VersionShort knownVersion;
			knownVersions.TryGetValue(modId, out knownVersion);
			return knownVersion;
		}
	}
}