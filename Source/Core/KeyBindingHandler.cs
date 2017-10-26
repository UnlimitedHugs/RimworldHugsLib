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
				HugsLibController.Instance.LogUploader.ShowPublishPrompt();
			}
			if (HugsLibKeyBindings.OpenLogFile.JustPressed) {
				ShellOpenLog.Execute();
			}
			if (HugsLibKeyBindings.RestartRimworld.JustPressed) {
				LongEventHandler.ExecuteWhenFinished(GenCommandLine.Restart);
			}
		}
	}
}