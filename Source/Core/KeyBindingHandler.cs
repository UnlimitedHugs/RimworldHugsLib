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
			if (HugsLibKeyBingings.PublishLogs.JustPressed && HugsLibUtility.ControlIsHeld) {
				HugsLibController.Instance.LogUploader.ShowPublishPrompt();
			}
			if (HugsLibKeyBingings.OpenLogFile.JustPressed) {
				ShellOpenLog.Execute();
			}
			if (HugsLibKeyBingings.RestartRimworld.JustPressed) {
				LongEventHandler.ExecuteWhenFinished(GenCommandLine.Restart);
			}
		}
	}
}