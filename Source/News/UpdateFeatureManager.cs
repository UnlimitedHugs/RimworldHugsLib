using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
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
	public class UpdateFeatureManager : PersistentDataManager {
		private const string UpdateFeatureDefFolder = "News/";

		protected override string FileName {
			get { return "LastSeenNews.xml"; }
		}

		// the highest news item version we have previously displayed for a mod packageId
		private readonly Dictionary<string, Version> highestSeenVersions = new Dictionary<string, Version>();

		private SettingHandle<IgnoredNewsIds> IgnoredNewsProvidersSetting { get; set; }
		private SettingHandle<bool> ShowNewsSetting { get; set; }

		internal UpdateFeatureManager() {
			LoadData();
		}

		internal void OnStaticConstructorInit() {
			UpdateFeatureDefLoader.ReloadNewsFeatureDefs();
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
				if (manuallyOpened) {
					defsToShow = allNewsFeatureDefs.ToList();
				} else {
					// try to find defs newer than already featured, exclude ignored mods, remember highest found version
					defsToShow = EnumerateFeatureDefsWithMoreRecentVersions(allNewsFeatureDefs, highestSeenVersions)
						.Where(def => !NewsProviderPackageIdIsIgnored(def.modContentPack.PackageId))
						.ToList();
					UpdateMostRecentKnownFeatureVersions(defsToShow, highestSeenVersions);
				}
				if (defsToShow.Count > 0) {
					SortFeatureDefsByModNameAndVersion(defsToShow);
					Find.WindowStack.Add(new Dialog_UpdateFeatures(defsToShow, IgnoredNewsProvidersSetting));
					SaveData();
					return true;
				}
			}
			return false;
		}

		private static IEnumerable<UpdateFeatureDef> EnumerateFeatureDefsWithMoreRecentVersions(
			IEnumerable<UpdateFeatureDef> featureDefs, Dictionary<string, Version> highestSeenVersions) {
			foreach (var featureDef in featureDefs) {
				var ownerPackageId = featureDef.modContentPack?.PackageId;
				if (!ownerPackageId.NullOrEmpty()) {
					var highestSeenVersion = highestSeenVersions.TryGetValue(ownerPackageId);
					if (highestSeenVersion == null || featureDef.VersionParsed > highestSeenVersion) {
						yield return featureDef;
					}
				}
			}
		}

		private bool NewsProviderPackageIdIsIgnored(string packageId) {
			return IgnoredNewsProvidersSetting.Value.Contains(packageId);
		}

		private static void UpdateMostRecentKnownFeatureVersions(
			IEnumerable<UpdateFeatureDef> shownNewsFeatureDefs, Dictionary<string, Version> highestSeenVersions) {
			foreach (var featureDef in shownNewsFeatureDefs) {
				var ownerPackageId = featureDef.modContentPack.PackageId;
				highestSeenVersions[ownerPackageId] = featureDef.VersionParsed;
			}
		}

		private static void SortFeatureDefsByModNameAndVersion(List<UpdateFeatureDef> featureDefs) {
			// sort defs by modNameReadable first, VersionParsed of the news item second
			featureDefs.Sort((def1, def2) => def1.modNameReadable != def2.modNameReadable
				? string.Compare(def1.modNameReadable, def2.modNameReadable, StringComparison.Ordinal)
				: def1.VersionParsed.CompareTo(def2.VersionParsed));
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
			if (IgnoredNewsProvidersSetting.Value == null) IgnoredNewsProvidersSetting.Value = new IgnoredNewsIds();
			IgnoredNewsProvidersSetting.VisibilityPredicate = () => false;
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

		public class IgnoredNewsIds : SettingHandleConvertible {
			private const char SerializationSeparator = '|';
			private HashSet<string> ignoredPackageIds = new HashSet<string>();

			public bool Contains(string packageId) {
				return ignoredPackageIds.Contains(packageId);
			}

			public void SetIgnored(string packageId, bool ignore) {
				var changed = ignore ? ignoredPackageIds.Add(packageId) : ignoredPackageIds.Remove(packageId);
				if (changed) HugsLibController.SettingsManager.SaveChanges();
			}

			public override bool ShouldBeSaved {
				get { return ignoredPackageIds.Count > 0; }
			}

			public override void FromString(string settingValue) {
				if (string.IsNullOrEmpty(settingValue)) return;
				ignoredPackageIds = new HashSet<string>(settingValue.Split(SerializationSeparator));
			}

			public override string ToString() {
				return ignoredPackageIds.Join(SerializationSeparator.ToString());
			}
		}

		private static class UpdateFeatureDefLoader {
			public static void ReloadNewsFeatureDefs() {
				// defs "inherited" from 1.0 through the folder versioning system are removed at this point
				DefDatabase<UpdateFeatureDef>.Clear();
				var parsedDefs = LoadAndParseNewsFeatureDefs();
				DefDatabase<UpdateFeatureDef>.Add(parsedDefs);
			}

			private static IEnumerable<UpdateFeatureDef> LoadAndParseNewsFeatureDefs() {
				XmlInheritance.Clear();
				// As we're moving the updates out of /Defs and into /News, we can no longer rely on the DefDatabase to magically 
				// load all the UpdateFeatureDefs. Instead, we'll have to manually point the reader to the relevant folders. 
				// Overall, we'll stick as much as we can to the vanilla def loading experience, albeit without patches.
				// Patch metadata has already been cleared, and parsing it again would add too much overhead.
				// First, gather all XML nodes that represent an UpdateFeatureDef, and remember where they came from
				// We can't parse them info defs the spot, because there are abstract nodes and inheritance to consider.
				var newsItemNodes = new List<(ModContentPack pack, XmlNode node)>();
				foreach (var modContentPack in LoadedModManager.RunningMods) {
					// this also handles versioned folder shenanigans
					var modNewsXmlAssets = DirectXmlLoader.XmlAssetsInModFolder(modContentPack, UpdateFeatureDefFolder);
					foreach (var xmlAsset in modNewsXmlAssets) {
						var rootElement = xmlAsset.xmlDoc?.DocumentElement;
						if (rootElement != null) {
							foreach (var childNode in rootElement.ChildNodes.OfType<XmlNode>()) {
								newsItemNodes.Add((modContentPack, childNode));
							}
						}
					}
				}

				// deal with inheritance
				foreach (var (_, node) in newsItemNodes) {
					if (node != null && node.NodeType == XmlNodeType.Element) {
						XmlInheritance.TryRegister(node, null);
					}
				}
				XmlInheritance.Resolve();

				var parsedFeatureDefs = new List<UpdateFeatureDef>();
				foreach (var (pack, node) in newsItemNodes) {
					// parse defs
					var def = DirectXmlLoader.DefFromNode(node, null);
					if (def is UpdateFeatureDef featureDef) {
						if (pack == null) {
							HugsLibController.Logger.Warning($"{nameof(UpdateFeatureDef)} with defName \"{def.defName}\" " +
															$"has unknown {nameof(ModContentPack)}. Discarding def.");
						} else if (featureDef.HasDeprecatedFormat) {
							HugsLibController.Logger.Warning($"{nameof(UpdateFeatureDef)} by mod {pack.PackageIdPlayerFacing} " +
															$"with defName \"{def.defName}\" is using deprecated fields modIdentifier " +
															"and/or assemblyVersion. Discarding def.");
						} else {
							def.modContentPack = pack;
							featureDef.ResolveReferences();
							parsedFeatureDefs.Add(featureDef);
						}
					}
				}
				
				XmlInheritance.Clear();

				return parsedFeatureDefs;
			}
		}
	}
}