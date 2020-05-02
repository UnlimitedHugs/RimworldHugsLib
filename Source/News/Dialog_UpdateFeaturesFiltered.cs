using System.Collections.Generic;
using System.Linq;
using HugsLib.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.News {
	/// <summary>
	/// The extended update news dialog, with filtering by mod and a menu button in entry headers for dev mode actions.
	/// </summary>
	internal class Dialog_UpdateFeaturesFiltered : Dialog_UpdateFeatures {
		private readonly IIgnoredNewsProviderStore ignoredNewsProviders;
		private readonly string filterButtonLabel;
		private readonly string allModsFilterLabel;
		private readonly TaggedString currentFilterReadout;
		private readonly TaggedString dropdownEntryTemplate;
		private readonly TaggedString ignoredModLabelSuffix;
		private readonly UpdateFeatureDefFilteringProvider defFilter;
		private readonly UpdateFeaturesDevMenu devMenu;
		private List<UpdateFeatureDef> fullDefList;
		private float bottomButtonWidth;

		public Dialog_UpdateFeaturesFiltered(List<UpdateFeatureDef> featureDefs,
			UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders,
			IUpdateFeaturesDevActions news, IModSpotterDevActions spotter)
			: base(FilterOutIgnoredProviders(featureDefs, ignoredNewsProviders), ignoredNewsProviders) {

			fullDefList = featureDefs;
			this.ignoredNewsProviders = ignoredNewsProviders;
			filterButtonLabel = "HugsLib_features_filterBtn".Translate();
			allModsFilterLabel = "HugsLib_features_filterAllMods".Translate();
			currentFilterReadout = "HugsLib_features_filterStatus".Translate();
			dropdownEntryTemplate = "HugsLib_features_filterDropdownEntry".Translate();
			ignoredModLabelSuffix = "HugsLib_features_filterIgnoredModSuffix".Translate();
			defFilter = new UpdateFeatureDefFilteringProvider(featureDefs);
			devMenu = new UpdateFeaturesDevMenu(news, spotter, new PlayerMessageSender());
			devMenu.UpdateFeatureDefsReloaded += DevMenuDefsReloadedHandler;
			AdjustButtonSizeToLabel();
		}

		public override void ExtraOnGUI() {
			base.ExtraOnGUI();
			CheckForReloadKeyPress();
		}

		protected override void DrawEntryTitleWidgets(Rect titleRect, UpdateFeatureDef forDef) {
			var linkWidgetWidth = DrawEntryLinkWidget(titleRect, forDef);
			if (Prefs.DevMode) {
				DrawDevToolsMenuWidget(titleRect, linkWidgetWidth, forDef);
			}
		}

		private void CheckForReloadKeyPress() {
			if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.F5) {
				Event.current.Use();
				devMenu.ReloadNewsDefs();
			}
		}

		private void DevMenuDefsReloadedHandler(IEnumerable<UpdateFeatureDef> loadedDefs) {
			fullDefList = loadedDefs.ToList();
			InstallFilteredDefs(fullDefList);
		}

		private void InstallFilteredDefs(IEnumerable<UpdateFeatureDef> defs) {
			var filteredDefs = defFilter.MatchingDefsOf(defs);
			if (defFilter.CurrentFilterModIdentifier == null) {
				filteredDefs = FilterOutIgnoredProviders(filteredDefs, ignoredNewsProviders);
			}
			InstallUpdateFeatureDefs(filteredDefs);
		}

		private void DrawDevToolsMenuWidget(Rect titleRect, float widgetOffset, UpdateFeatureDef forDef) {
			var widgetSize = titleRect.height;
			var widgetRect = new Rect(titleRect.width - widgetOffset - widgetSize,
				titleRect.y, widgetSize, widgetSize);
			if (Mouse.IsOver(widgetRect)) {
				Widgets.DrawHighlight(widgetRect);
			}
			var buttonTexture = HugsLibTextures.HLMenuIcon;
			var textureRect = new Rect(
				widgetRect.center.x - buttonTexture.width / 2f, widgetRect.center.y - buttonTexture.height / 2f,
				buttonTexture.width, buttonTexture.height
			);
			Widgets.DrawTextureFitted(textureRect, buttonTexture, 1f);
			if (Widgets.ButtonInvisible(widgetRect)) {
				OpenDevToolsDropdownMenu(forDef);
			}
		}

		private void OpenDevToolsDropdownMenu(UpdateFeatureDef forDef) {
			Find.WindowStack.Add(
				new FloatMenu(
					devMenu.GetMenuOptions(forDef)
						.Select(o => new FloatMenuOption(o.label, o.action) {Disabled = o.disabled})
						.ToList()
				)
			);
		}

		protected override void DrawBottomButtonRow(Rect inRect) {
			DrawFilterButton(inRect.LeftPartPixels(bottomButtonWidth));
			const float filterLabelOffset = 13f;
			DrawCurrentFilterLabel(new Rect(
				inRect.x + bottomButtonWidth + filterLabelOffset, inRect.y,
				inRect.width - (bottomButtonWidth * 2f + Margin * 2f), inRect.height
			));
			DrawCloseButton(inRect.RightPartPixels(bottomButtonWidth));
		}

		private void AdjustButtonSizeToLabel() {
			const float buttonPadding = 16f;
			bottomButtonWidth = Mathf.Max(Text.CalcSize(filterButtonLabel).x + buttonPadding, CloseButSize.x);
		}

		private void DrawCurrentFilterLabel(Rect inRect) {
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			var prevColor = GUI.color;
			GUI.color = new Color(.5f, .5f, .5f);
			var currentFilterModNameReadable = defFilter.CurrentFilterModNameReadable ?? allModsFilterLabel;
			Widgets.Label(inRect, currentFilterReadout.Formatted(currentFilterModNameReadable));
			GUI.color = prevColor;
			GenUI.ResetLabelAlign();
		}

		private void DrawFilterButton(Rect inRect) {
			if (Widgets.ButtonText(inRect, filterButtonLabel)) {
				ShowFilterOptionsMenu();
			}
		}

		private void ShowFilterOptionsMenu() {
			Find.WindowStack.Add(
				new FloatMenu(GetFilterMenuOptions())
			);
		}

		private List<FloatMenuOption> GetFilterMenuOptions() {
			var options = new List<FloatMenuOption> {
				new FloatMenuOption(allModsFilterLabel, () => SetFilterAndUpdateShownDefs(null))
			};
			options.AddRange(defFilter.GetAvailableFilters()
				.Select(f => {
						var ignoredLabelSuffix = ignoredNewsProviders.Contains(f.id)
							? ignoredModLabelSuffix.RawText
							: string.Empty;
						var optionLabel = dropdownEntryTemplate.Formatted(f.label, f.defCount, ignoredLabelSuffix);
						return new FloatMenuOption(optionLabel, () => SetFilterAndUpdateShownDefs(f.id));
					}
				)
			);
			return options;
		}

		private void SetFilterAndUpdateShownDefs(string newFilterModIdentifier) {
			if (defFilter.CurrentFilterModIdentifier == newFilterModIdentifier) return;
			defFilter.CurrentFilterModIdentifier = newFilterModIdentifier;
			InstallFilteredDefs(fullDefList);
			ResetScrollPosition();
		}

		private static List<UpdateFeatureDef> FilterOutIgnoredProviders(IEnumerable<UpdateFeatureDef> featureDefs,
			IIgnoredNewsProviderStore ignoredNewsProviders) {
			return featureDefs
				.Where(d => !ignoredNewsProviders.Contains(d.OwningModId))
				.ToList();
		}
		
		private class PlayerMessageSender : IStatusMessageSender {
			public void Send(string message, bool success) {
				var messageType = success
					? MessageTypeDefOf.TaskCompletion
					: MessageTypeDefOf.RejectInput;
				Messages.Message(message, messageType);
			}
		}
	}
}