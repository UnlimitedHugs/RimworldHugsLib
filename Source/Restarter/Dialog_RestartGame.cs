using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Restarter {
	/**
	 * A dialog that offers the player to restart the game after a mod configuration change.
	 */
	public class Dialog_RestartGame : Window {
		private static readonly Color RestartButtonColor = new Color(.55f, 1f, .55f);

		private bool autoCheckboxIsOn;

		public override Vector2 InitialSize {
			get { return new Vector2(500f, 300f); }
		}

		public Dialog_RestartGame() {
			closeOnEscapeKey = false;
			absorbInputAroundWindow = true;
			doCloseX = true;
			doCloseButton = false;
			autoCheckboxIsOn = HugsLibController.Instance.AutoRestarter.AutoRestartSetting.Value;
		}

		public override void DoWindowContents(Rect inRect) {
			const float titleHeight = 42f;
			const float textHeight = 100f;
			const float buttonMargin = 20f;
			const float buttonHeight = 35f;
			const float toggleHeight = 30f;
			const float toggleExtraWidth = 40f;
			
			// title
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(0, inRect.y, inRect.width, titleHeight), "HugsLib_restart_title".Translate());
			
			// text
			Text.Font = GameFont.Small;
			Rect textRect = new Rect(inRect.x, inRect.y + titleHeight, inRect.width, textHeight);
			Widgets.Label(textRect, "HugsLib_restart_text".Translate());
			
			// toggle
			bool toggleWasOn = autoCheckboxIsOn;
			var toggleLabelText = "HugsLib_restart_autoToggle".Translate();
			var labelTextSize = Text.CalcSize(toggleLabelText);
			Widgets.CheckboxLabeled(new Rect(inRect.x, inRect.y + titleHeight + textHeight, labelTextSize.x + toggleExtraWidth, toggleHeight), toggleLabelText, ref autoCheckboxIsOn);
			if (toggleWasOn != autoCheckboxIsOn) {
				CheckboxToggleAction();
			}

			// restart button
			GUI.color = RestartButtonColor;
			float buttonWidth = inRect.width / 2f - buttonMargin;
			if (Widgets.ButtonText(new Rect(inRect.width / 2f + 20f, inRect.height - buttonHeight, buttonWidth, buttonHeight), "HugsLib_restart_restartNowBtn".Translate()) ||
				(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)) {
				RestartAction();
				Close();
			}

			// close button
			GUI.color = Color.white;
			if (Widgets.ButtonText(new Rect(inRect.x, inRect.height - buttonHeight, buttonWidth, buttonHeight), "CloseButton".Translate()) ||
				(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)) {
				CloseAction();
				Close();
			}
		}

		private void CheckboxToggleAction() {
			HugsLibController.Instance.AutoRestarter.AutoRestartSetting.Value = autoCheckboxIsOn;
			HugsLibController.SettingsManager.SaveChanges();
		}

		private void RestartAction() {
			AutoRestarter.PerformRestart();
		}

		private void CloseAction() {
			var modsDialog = Find.WindowStack.WindowOfType<Page_ModsConfig>();
			if (modsDialog != null) {
				modsDialog.Close();
			}
		}
	}
}