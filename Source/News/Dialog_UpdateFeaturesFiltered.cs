using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HugsLib.News {
	/// <summary>
	/// The extended update news dialog, with filtering by mod and a menu button in entry headers for dev mode actions.
	/// </summary>
	public class Dialog_UpdateFeaturesFiltered : Dialog_UpdateFeatures {
		private readonly string filterButtonLabel;
		private readonly float bottomButtonWidth;
		private readonly TaggedString currentFilterReadout;
		private readonly string allModsFilterReadout;
		
		public Dialog_UpdateFeaturesFiltered(List<UpdateFeatureDef> featureDefs,
			UpdateFeatureManager.IgnoredNewsIds ignoredNewsProviders) : base(featureDefs, ignoredNewsProviders) {
			filterButtonLabel = "HugsLib_features_filterBtn".Translate();
			currentFilterReadout = "HugsLib_features_filterStatus".Translate();
			allModsFilterReadout = "HugsLib_features_filterAllMods".Translate();
			// size buttons to fit translated labels
			const float buttonPadding = 16f;
			bottomButtonWidth = Mathf.Max(Text.CalcSize(filterButtonLabel).x + buttonPadding, CloseButSize.x);
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

		private void DrawCurrentFilterLabel(Rect inRect) {
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			var prevColor = GUI.color;
			GUI.color = new Color(.5f, .5f, .5f);
			Widgets.Label(inRect, currentFilterReadout.Formatted(allModsFilterReadout));
			GUI.color = prevColor;
			GenUI.ResetLabelAlign();
		}

		private void DrawFilterButton(Rect inRect) {
			Widgets.ButtonText(inRect, filterButtonLabel);
		}
	}
}