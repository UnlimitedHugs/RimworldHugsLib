using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace HugsLib.News {
	/// <summary>
	/// Handles the custom loading mechanics of <see cref="UpdateFeatureDef"/>s.
	/// </summary>
	internal static class UpdateFeatureDefLoader {
		public static void ResolveAndInjectUpdateFeatureDefs(IEnumerable<DefXMLNode> loadedNodes) {
			ResetUpdateFeatureDefTranslations();
			var parsedDefs = ResolveUpdateFeatureDefsFromNodes(loadedNodes);
			ReinitUpdateFeatureDefDatabase(parsedDefs);
		}

		public static void ReloadAllUpdateFeatureDefs() {
			var (nodes, errors) = LoadUpdateFeatureDefNodes();
			HandleDefLoadingErrors(errors);
			ResolveAndInjectUpdateFeatureDefs(nodes);
			InjectTranslationsIntoUpdateFeatureDefs();
		}

		public static void HandleDefLoadingErrors(IEnumerable<string> errors) {
			foreach (var error in errors) {
				HugsLibController.Logger.Error(error);
			}
		}

		public static (IEnumerable<DefXMLNode> nodes, IEnumerable<string> loadingErrors) LoadUpdateFeatureDefNodes() {
			var nodes = new List<DefXMLNode>();
			var loadingErrors = new List<string>(0);
			foreach (var modContentPack in LoadedModManager.RunningMods) {
				try {
					var modNewsXmlAssets = DirectXmlLoader.XmlAssetsInModFolder(modContentPack,
						UpdateFeatureManager.UpdateFeatureDefFolder);
					foreach (var xmlAsset in modNewsXmlAssets) {
						var rootElement = xmlAsset.xmlDoc?.DocumentElement;
						if (rootElement != null) {
							foreach (var childNode in rootElement.ChildNodes.OfType<XmlNode>()) {
								nodes.Add(new DefXMLNode(modContentPack, childNode, xmlAsset));
							}
						}
					}
				} catch (Exception e) {
					loadingErrors.Add("Failed to load UpdateFeatureDefs for mod " +
						$"{modContentPack.PackageIdPlayerFacing}: {e}");
				}
			}
			return (nodes, loadingErrors);
		}

		private static IEnumerable<UpdateFeatureDef> ResolveUpdateFeatureDefsFromNodes(IEnumerable<DefXMLNode> nodes) {
			// The following replicates most of the vanilla Def loading mechanism, albeit without patches.
			// Patch metadata has already been cleared, and parsing it again would add too much overhead.
			// Def resolution can't be offloaded to a worker thread, since XmlInheritance is not thread safe.
			var defNodes = nodes.ToArray();

			// register for inheritance
			XmlInheritance.Clear();
			foreach (var (modContent, node, _) in defNodes) {
				if (node is {NodeType: XmlNodeType.Element}) {
					XmlInheritance.TryRegister(node, modContent);
				}
			}
			XmlInheritance.Resolve();

			// resolve defs from nodes
			var resolvedDefs = new List<UpdateFeatureDef>();
			foreach (var (pack, node, asset) in defNodes) {
				try {
					if (DirectXmlLoader.DefFromNode(node, asset) is UpdateFeatureDef def) {
						def.modContentPack = pack;
						def.ResolveReferences();
						resolvedDefs.Add(def);
					}
				} catch (Exception e) {
					HugsLibController.Logger.Error(
						$"Failed to parse UpdateFeatureDef from mod {pack.PackageIdPlayerFacing}:\n" +
						$"{GetExceptionChainMessage(e)}\n" +
						// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
						$"Context: {node?.OuterXml.ToStringSafe()}\n" +
						$"File: {asset?.FullFilePath.ToStringSafe()}\n" +
						// ReSharper restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
						$"Exception: {e}");
				}
			}

			XmlInheritance.Clear();

			return resolvedDefs;
		}

		private static void ReinitUpdateFeatureDefDatabase(IEnumerable<UpdateFeatureDef> defs) {
			// defs "inherited" from 1.0 through the folder versioning system are removed at this point
			DefDatabase<UpdateFeatureDef>.Clear();
			DefDatabase<UpdateFeatureDef>.Add(defs);
		}

		private static void ResetUpdateFeatureDefTranslations() {
			// translations might have already been applied to news defs inherited from 1.0
			// through the versioning system, and must be reset so they can be applied again
			if (LanguageDatabase.activeLanguage == null) return;
			foreach (var defInjection in LanguageDatabase.activeLanguage.defInjections) {
				if (defInjection.defType == typeof(UpdateFeatureDef)) {
					foreach (var injection in defInjection.injections) {
						injection.Value.injected = false;
					}
					defInjection.loadErrors.Clear();
				}
			}
		}

		private static void InjectTranslationsIntoUpdateFeatureDefs() {
			// injection happens automatically during loading at game startup, but must
			// be done manually when defs are reloaded using the update news dev tools 
			if (LanguageDatabase.activeLanguage == null) return;
			var updateFeatureDefInjections = LanguageDatabase.activeLanguage.defInjections
				.Where(i => i.defType == typeof(UpdateFeatureDef));
			foreach (var injectionPackage in updateFeatureDefInjections) {
				try {
					injectionPackage.InjectIntoDefs(true);
				} catch (Exception e) {
					HugsLibController.Logger.Warning(
						$"Error while injecting translations into {nameof(UpdateFeatureDef)}: {e}");
				}
			}
		}

		private static string GetExceptionChainMessage(Exception e) {
			var message = e.Message;
			while (e.InnerException != null) {
				e = e.InnerException;
				message += $" -> {e.Message}";
			}
			return message;
		}

		public struct DefXMLNode {
			public ModContentPack ContentPack { get; }
			public XmlNode Node { get; }
			public LoadableXmlAsset SourceAsset { get; }

			public DefXMLNode(ModContentPack contentPack, XmlNode node, LoadableXmlAsset sourceAsset) {
				ContentPack = contentPack;
				Node = node;
				SourceAsset = sourceAsset;
			}
			public void Deconstruct(out ModContentPack pack, out XmlNode node, out LoadableXmlAsset asset) {
				pack = ContentPack;
				node = Node;
				asset = SourceAsset;
			}
		}
	}
}