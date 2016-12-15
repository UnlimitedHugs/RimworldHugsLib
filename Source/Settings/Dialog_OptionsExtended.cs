using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/**
	 * Extends the Options dialog with a "Mod Settings" button
	 */
	public class Dialog_OptionsExtended : Dialog_Options {
		private const float ButtonBottomOffset = 90f;
		private readonly Vector2 ButtonSize = new Vector2(277f, 40f);
		private readonly Color ButtonColor = new Color(.55f, 1f, .55f);

		public override void DoWindowContents(Rect inRect) {
			base.DoWindowContents(inRect);
			var btnRect = new Rect(inRect.x, inRect.height - ButtonBottomOffset - ButtonSize.y, ButtonSize.x, ButtonSize.y);
			var prevColor = GUI.color;
			GUI.color = ButtonColor;
			if (Widgets.ButtonText(btnRect, "HugsLib_settings_btn".Translate())) {
				Find.WindowStack.TryRemove(this);
				Find.WindowStack.Add(new Dialog_ModSettings());
			}
			GUI.color = prevColor;
		}
	}
}