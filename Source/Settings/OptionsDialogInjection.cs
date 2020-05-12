using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// Injects the "Mod Settings" button into the Options dialog.
	/// </summary>
	internal static class OptionsDialogInjection {
		private static readonly Color ButtonColor = new Color(.55f, 1f, .55f);

		public static void DrawModSettingsButton(Listing_Standard listing) {
			var prevColor = GUI.color;
			GUI.color = ButtonColor;
			if (listing.ButtonText("HugsLib_settings_btn".Translate())) {
				HugsLibUtility.OpenModSettingsDialog();
			}
			GUI.color = prevColor;
		}
	}
}