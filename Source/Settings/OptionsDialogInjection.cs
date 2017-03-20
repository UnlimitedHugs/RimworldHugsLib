using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// Injects the "Mod Settings" button into the Options dialog.
	/// </summary>
	internal static class OptionsDialogInjection {
		private const float ButtonBottomOffset = 90f;
		private static readonly Vector2 ButtonSize = new Vector2(277f, 40f);
		private static readonly Color ButtonColor = new Color(.55f, 1f, .55f);

		public static void DrawSettingsDialogExtensions(Window window, Rect inRect) {
			var btnRect = new Rect(inRect.x, inRect.height - ButtonBottomOffset - ButtonSize.y, ButtonSize.x, ButtonSize.y);
			var prevColor = GUI.color;
			GUI.color = ButtonColor;
			if (Widgets.ButtonText(btnRect, "HugsLib_settings_btn".Translate())) {
				Find.WindowStack.TryRemove(window);
				Find.WindowStack.Add(new Dialog_ModSettings());
			}
			GUI.color = prevColor;
		} 
	}
}