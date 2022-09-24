using HugsLib.Core;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HugsLib.Settings;

internal static class OptionsDialogExtensions {
	public static void DrawHugsLibSettingsButton(Dialog_Options self, ref int buttonIndex) {
		// Adapted from Dialog_Options.DoCategoryRow
		var buttonRect = new Rect(0f, buttonIndex * 50f, 160f, 48f).ContractedBy(4f);
		Widgets.DrawOptionBackground(buttonRect, false);
		if (Widgets.ButtonInvisible(buttonRect)) {
			HugsLibUtility.OpenModSettingsDialog();
			SoundDefOf.Click.PlayOneShotOnCamera();
		}
		var iconLeftPadding = buttonRect.x + 10f;
		var iconPos = new Rect(iconLeftPadding, buttonRect.y + (buttonRect.height - 20f) / 2f, 20f, 20f);
		GUI.DrawTexture(iconPos, HugsLibTextures.HLOptionsIcon);
		var labelLeftPadding = iconLeftPadding + 37f;
		Widgets.Label(
			new Rect(labelLeftPadding, buttonRect.y, buttonRect.width - labelLeftPadding, buttonRect.height),
			"HugsLib_settings_btn".Translate() + " (HugsLib)"
		);
		buttonIndex += 1;
	}
}