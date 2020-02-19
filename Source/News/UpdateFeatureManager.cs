using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using HugsLib.Core;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace HugsLib.News {
	/// <summary>
	/// Stores the last displayed update news item for all mods. Shows the news dialog window when there are not yet displayed news items available.
	/// </summary>
	public class UpdateFeatureManager : PersistentDataManager
    {
        private const string UpdateFeatureDefFolder = "/News";

        protected override string FileName {
			get { return "LastSeenNews.xml"; }
		}
		// highest version that has displayed features
		private readonly Dictionary<string, Version> knownVersions = new Dictionary<string, Version>();
		// current version if higher than last featured
		private readonly Dictionary<string, Version> freshVersions = new Dictionary<string, Version>();
        private readonly List<ModContentPack> relevantMods = new List<ModContentPack>();

		private SettingHandle<IgnoredNewsIds> IgnoredNewsProvidersSetting { get; set; }
		private SettingHandle<bool> ShowNewsSetting { get; set; }

		public UpdateFeatureManager() {
			LoadData();
		}

        public void InspectActiveMod( ModBase mod )
        {
            InspectActiveMod( mod.ModIdentifier, mod.ModContentPack, mod.GetVersion() );
        }

        public void InspectActiveMod( ModContentPack pack )
        {
            InspectActiveMod( pack.PackageId, pack, pack.GetVersion() );
        }

		public void InspectActiveMod( string identifier, ModContentPack pack, Version currentVersion) {
			var knownVersion = TryGetKnownVersion(identifier);
			if (knownVersion == null || currentVersion > knownVersion) {
				var existingFreshVersion = freshVersions.TryGetValue(identifier);
				freshVersions[identifier] = existingFreshVersion == null || currentVersion > existingFreshVersion ? 
					currentVersion : existingFreshVersion;
                relevantMods.Add( pack );
            }
		}

		public bool TryShowDialog(bool manuallyOpened) {
			if ((!ShowNewsSetting.Value && !manuallyOpened) || (freshVersions.Count == 0 && !manuallyOpened)) return false;
            TryLoadUpdates();

			List<UpdateFeatureDef> defsToShow;
			if (manuallyOpened) {
				defsToShow = _updateFeatureDefs;
			} else {
				// try to pull defs newer than already featured, remember highest pulled version
				defsToShow = new List<UpdateFeatureDef>();
				foreach (var freshVersionPair in freshVersions) {
					var modId = freshVersionPair.Key;
					if(IgnoredNewsProvidersSetting.Value.Contains(modId)) continue;
					var freshVersion = freshVersionPair.Value;
					var knownVersion = TryGetKnownVersion(modId) ?? new Version();
					Version highestVersionWithFeature = null;
					foreach (var def in _updateFeatureDefs) {
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

        private List<UpdateFeatureDef> _updateFeatureDefs;
        public void TryLoadUpdates()
        {
            if ( _updateFeatureDefs != null ) return;
            _updateFeatureDefs = new List<UpdateFeatureDef>();

            // as we're moving the updates out of /Defs and into /News, we can no longer rely on the DefDatabase to magically 
            // load all the updateFeatureDefs. Instead, we'll have to manually point the reader to the relevant folders. 
            // Overall, we'll stick as much as we can to the vanilla def loading experience, albeit without patches
            var newsAssets = new List<LoadableXmlAsset>();
            foreach ( var mod in relevantMods )
                // this also handles versioned folder shenanigans.
                newsAssets.AddRange(DirectXmlLoader.XmlAssetsInModFolder( mod, UpdateFeatureDefFolder ) ); 

            // create a single doc
            var news = LoadedModManager.CombineIntoUnifiedXML( newsAssets, new Dictionary<XmlNode, LoadableXmlAsset>() );

            // we could now apply patches, but that seems like it's overkill
            XmlInheritance.Clear();
            var nodes = new List<XmlNode>();
            foreach ( var child in news.DocumentElement.ChildNodes )
                nodes.Add( child as XmlNode );
            
            // deal with inheritance
            foreach ( var node in nodes )
                if ( node.NodeType == XmlNodeType.Element )
                    XmlInheritance.TryRegister( node, null );
            XmlInheritance.Resolve();

            // load defs
            foreach ( var node in nodes )
            {
                var def = DirectXmlLoader.DefFromNode( node, null );
                if ( def is UpdateFeatureDef update )
                    _updateFeatureDefs.Add( update );
            }

            // resolve them references
            foreach ( var updateFeatureDef in _updateFeatureDefs )
                updateFeatureDef.ResolveReferences();
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

			public override bool ShouldBeSaved {
				get { return ignoredModIds.Count > 0; }
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