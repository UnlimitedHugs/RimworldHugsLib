using System;
using HugsLib;
using UnityEngine;
using Verse;

// TodoMajor: fix namespace
// ReSharper disable once CheckNamespace
namespace RimWorld {
	/// <summary>
	/// Displays custom settings controls for mods that use the vanilla mod settings system.
	/// The dialog shows the controls for a single mod only and is opened through Dialog_ModSettings.
	/// </summary>
	public class Dialog_VanillaModSettings : Window {

		private const float TitleLabelHeight = 32f;
		private readonly Color TitleLineColor = new Color(0.3f, 0.3f, 0.3f);

		private readonly Mod selectedMod;
		private Exception guiException;

		public override Vector2 InitialSize {
			get { return new Vector2(900f, 700f); }
		}

		public Dialog_VanillaModSettings(Mod mod) {
			selectedMod = mod;
			forcePause = true;
			doCloseX = true;
			closeOnCancel = true;
			closeOnAccept = false;
			doCloseButton = true;
			closeOnClickedOutside = true;
			absorbInputAroundWindow = true;
		}

		public override void PreClose() {
			base.PreClose();
			if (selectedMod != null) {
				selectedMod.WriteSettings();
			}
		}

		public override void DoWindowContents(Rect inRect) {
			if (selectedMod == null) return;
			var modName = selectedMod.SettingsCategory();
			if (!modName.NullOrEmpty()) {
				var titleRect = new Rect(0f, 0f, inRect.width, TitleLabelHeight);
				Text.Font = GameFont.Medium;
				Widgets.Label(titleRect, "HugsLib_setting_mod_name_title".Translate(modName));

				var prevColor = GUI.color;
				GUI.color = TitleLineColor;
				Widgets.DrawLineHorizontal(0f, TitleLabelHeight, inRect.width);
				GUI.color = prevColor;
			}
				
			Text.Font = GameFont.Small;
			var modContentRect = new Rect(0f, 40f, inRect.width, inRect.height - 40f - CloseButSize.y);
			if (guiException == null) {
				try {
					selectedMod.DoSettingsWindowContents(modContentRect);
				} catch (Exception e) {
					guiException = e;
					HugsLibController.Logger.ReportException(e);
				}
			} else {
				Text.Font = GameFont.Tiny;
				Widgets.Label(modContentRect, "An error occurred while displaying the mods settings page:\n"+guiException);
			}
		}
	}
}