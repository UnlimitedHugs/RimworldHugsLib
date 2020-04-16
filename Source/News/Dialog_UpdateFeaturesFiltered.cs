using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HugsLib.News {
	/// <summary>
	/// The extended update news dialog, with filtering by mod and a menu button in entry headers for dev mode actions.
	/// </summary>
	public class Dialog_UpdateFeaturesFiltered : Dialog_UpdateFeatures {
		private readonly List<UpdateFeatureDef> fullDefList;
		private readonly UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders;
		private readonly string filterButtonLabel;
		private readonly string allModsFilterLabel;
		private readonly TaggedString currentFilterReadout;
		private readonly TaggedString dropdownEntryTemplate;
		private readonly TaggedString ignoredModLabelSuffix;
		private readonly UpdateFeatureDefFilteringProvider defFilter;
		private float bottomButtonWidth;

		public Dialog_UpdateFeaturesFiltered(List<UpdateFeatureDef> featureDefs,
			UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders) 
				: base(FilterOutIgnoredProviders(featureDefs, ignoredNewsProviders), ignoredNewsProviders) {
			fullDefList = featureDefs;
			this.ignoredNewsProviders = ignoredNewsProviders;
			filterButtonLabel = "HugsLib_features_filterBtn".Translate();
			allModsFilterLabel = "HugsLib_features_filterAllMods".Translate();
			currentFilterReadout = "HugsLib_features_filterStatus".Translate();
			dropdownEntryTemplate = "HugsLib_features_filterDropdownEntry".Translate();
			ignoredModLabelSuffix = "HugsLib_features_filterIgnoredModSuffix".Translate();
			defFilter = new UpdateFeatureDefFilteringProvider(featureDefs);
			AdjustButtonSizeToLabel();
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
			var filteredDefList = defFilter.MatchingDefsOf(fullDefList).ToList();
			if (defFilter.CurrentFilterModIdentifier == null) {
				filteredDefList = FilterOutIgnoredProviders(filteredDefList, ignoredNewsProviders);
			}
			InstallUpdateFeatureDefs(filteredDefList);
			ResetScrollPosition();
		}

		private static List<UpdateFeatureDef> FilterOutIgnoredProviders(IEnumerable<UpdateFeatureDef> featureDefs,
			UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders) {
			return featureDefs
				.Where(d => !ignoredNewsProviders.Contains(d.OwningModId))
				.ToList();
		}
	}
}