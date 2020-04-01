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

		[Obsolete("Mods no longer need to call in, fresh news are automatically detected based on their defs")]
		public void InspectActiveMod(string modId, Version currentVersion) {
		}

		internal void OnEarlyInitialize() {
			// this should put us just before backstory loading in the DoPlayLoad cycle
			// we inject our defs early on to take advantage of the stock translation injection system
			LongEventHandler.ExecuteWhenFinished(UpdateFeatureDefLoader.LoadUpdateFeatureDefs);
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
						.Where(def => !NewsProviderOwningModIdIsIgnored(def.OwningModId))
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

		public class IgnoredNewsIds : SettingHandleConvertible {
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

		private static class UpdateFeatureDefLoader {

			public static void LoadUpdateFeatureDefs() {
				ResetUpdateFeatureDefTranslations();
				var parsedDefs = LoadAndParseNewsFeatureDefs();
				ReinitUpdateFeatureDefDatabase(parsedDefs);
			}

			private static IEnumerable<UpdateFeatureDef> LoadAndParseNewsFeatureDefs() {
				XmlInheritance.Clear();
				// As we're moving the updates out of /Defs and into /News, we can no longer rely on the DefDatabase to magically 
				// load all the UpdateFeatureDefs. Instead, we'll have to manually point the reader to the relevant folders. 
				// Overall, we'll stick as much as we can to the vanilla def loading experience, albeit without patches.
				// Patch metadata has already been cleared, and parsing it again would add too much overhead.
				// First, gather all XML nodes that represent an UpdateFeatureDef, and remember where they came from
				// We can't parse them info defs on the spot, because there is inheritance to consider.
				var newsItemNodes = new List<(ModContentPack pack, XmlNode node, LoadableXmlAsset asset)>();
				foreach (var modContentPack in LoadedModManager.RunningMods) {
					try {
						var modNewsXmlAssets = DirectXmlLoader.XmlAssetsInModFolder(modContentPack, UpdateFeatureDefFolder);
						foreach (var xmlAsset in modNewsXmlAssets) {
							var rootElement = xmlAsset.xmlDoc?.DocumentElement;
							if (rootElement != null) {
								foreach (var childNode in rootElement.ChildNodes.OfType<XmlNode>()) {
									newsItemNodes.Add((modContentPack, childNode, xmlAsset));
								}
							}
						}
					} catch (Exception e) {
						HugsLibController.Logger.Error("Failed to load UpdateFeatureDefs for mod " +
														$"{modContentPack.PackageIdPlayerFacing}: {e}");
						throw;
					}
				}

				// deal with inheritance
				foreach (var (modContent, node, _) in newsItemNodes) {
					if (node != null && node.NodeType == XmlNodeType.Element) {
						XmlInheritance.TryRegister(node, modContent);
					}
				}
				XmlInheritance.Resolve();

				var parsedFeatureDefs = new List<UpdateFeatureDef>();
				foreach (var (pack, node, asset) in newsItemNodes) {
					// parse defs
					try {
						var def = DirectXmlLoader.DefFromNode(node, asset) as UpdateFeatureDef;
						if (def != null) {
							def.modContentPack = pack;
							def.ResolveReferences();
							parsedFeatureDefs.Add(def);
						}
					} catch (Exception e) {
						HugsLibController.Logger.Error($"Failed to parse UpdateFeatureDef from mod {pack.PackageIdPlayerFacing}:\n" +
														$"{GetExceptionChainMessage(e)}\n" +
														$"Context: {node?.OuterXml.ToStringSafe()}\n" +
														$"File: {asset?.FullFilePath.ToStringSafe()}\n" +
														$"Exception: {e}");
					}
				}
				
				XmlInheritance.Clear();

				return parsedFeatureDefs;
			}

			private static void ResetUpdateFeatureDefTranslations() {
				// translations might have already been applied to news defs inherited from 1.0
				// through the versioning system, and must be reset so they can be applied again
				if(LanguageDatabase.activeLanguage == null) return;
				foreach (var defInjection in LanguageDatabase.activeLanguage.defInjections) {
					if (defInjection.defType == typeof(UpdateFeatureDef)) {
						foreach (var injection in defInjection.injections) {
							injection.Value.injected = false;
						}
						defInjection.loadErrors.Clear();
					}
				}
			}

			private static void ReinitUpdateFeatureDefDatabase(IEnumerable<UpdateFeatureDef> defs) {
				// defs "inherited" from 1.0 through the folder versioning system are removed at this point
				DefDatabase<UpdateFeatureDef>.Clear();
				DefDatabase<UpdateFeatureDef>.Add(defs);
			}

			private static string GetExceptionChainMessage(Exception e) {
				var message = e.Message;
				while (e.InnerException != null) {
					e = e.InnerException;
					message += $" -> {e.Message}";
				}
				return message;
			}
		}
	}
}