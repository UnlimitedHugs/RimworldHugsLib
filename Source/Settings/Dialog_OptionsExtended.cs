using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/**
	 * Extends the Options dialog with a "Mod Settings" button
	 */
	public class Dialog_OptionsExtended : Dialog_Options {
		private const int HorizontalColumnSpacing = 14;
		private const int NumColumnsBeforeButton = 2;
		private const int ButtonVerticalOffset = 220;
		private Vector2 ButtonSize = new Vector2(277, 30);

		public override void DoWindowContents(Rect inRect) {
			base.DoWindowContents(inRect);
			var optionsColumnWidth = (windowRect.width - 34f) / 3f;
			var btnRect = new Rect(optionsColumnWidth * NumColumnsBeforeButton + HorizontalColumnSpacing * NumColumnsBeforeButton - Margin, ButtonVerticalOffset - Margin, ButtonSize.x, ButtonSize.y);
			if (Widgets.ButtonText(btnRect, "HugsLib_settings_btn".Translate())) {
				Find.WindowStack.TryRemove(this);
				Find.WindowStack.Add(new Dialog_ModSettings());
			}
		}
	}
}