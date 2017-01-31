using HugsLib.Restarter;
using HugsLib.Shell;
using HugsLib.Utils;
using UnityEngine;

namespace HugsLib.Core {
	/**
	 * Handles the key presses for key bindings added by HugsLib
	 */
	internal static class KeyBindingHandler {
		public static void OnGUI() {
			if (Event.current.type != EventType.KeyDown) return;
			if (HugsLibKeyBingings.PublishLogs.JustPressed && HugsLibUtility.ControlIsHeld) {
				HugsLibController.Instance.LogUploader.ShowPublishPrompt();
			}
			if (HugsLibKeyBingings.OpenLogFile.JustPressed) {
				ShellOpenLog.Execute();
			}
			if (HugsLibKeyBingings.RestartRimworld.JustPressed) {
				AutoRestarter.PerformRestart();
			}
		}
	}
}