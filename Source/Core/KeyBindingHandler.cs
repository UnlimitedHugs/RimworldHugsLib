using HugsLib.Shell;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Handles the key presses for key bindings added by HugsLib
	/// </summary>
	internal static class KeyBindingHandler {
		public static void OnGUI() {
			if (Event.current.type != EventType.KeyDown) return;
			if (HugsLibKeyBindings.PublishLogs.JustPressed && HugsLibUtility.ControlIsHeld) {
				if (HugsLibUtility.AltIsHeld) {
					HugsLibController.Instance.LogUploader.CopyToClipboard();
				} else {
					HugsLibController.Instance.LogUploader.ShowPublishPrompt();
				}
			}
			if (HugsLibKeyBindings.OpenLogFile.JustPressed) {
				ShellOpenLog.Execute();
			}
			if (HugsLibKeyBindings.RestartRimworld.JustPressed) {
				GenCommandLine.Restart();
			}
			if (HugsLibKeyBindings.HLOpenModSettings.JustPressed) {
				HugsLibUtility.OpenModSettingsDialog();
			}
			if (HugsLibKeyBindings.HLOpenUpdateNews.JustPressed) {
				HugsLibController.Instance.UpdateFeatures.TryShowDialog(true);
			}
		}
	}
}