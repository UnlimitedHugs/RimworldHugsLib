using System.Collections.Generic;
using System.Reflection;
using HugsLib.Shell;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Logs {
	/// <summary>
	/// Injects two new buttons into the logger window.
	/// "Copy" will copy the selected message to the clipboard and "Share logs" will initate the log publishing process.
	/// </summary>
	internal static class LogWindowInjection {
		private const float MessageDetailsScrollBarWidth = 16f;
		private static readonly Color shareButtonColor = new Color(.3f, 1f, .3f, 1f);
		private static FieldInfo selectedMessageField;

		public static void PrepareReflection() {
			selectedMessageField = typeof(EditWindow_Log).GetField("selectedMessage", BindingFlags.NonPublic | BindingFlags.Static);
			if (selectedMessageField != null && selectedMessageField.FieldType != typeof(LogMessage)) selectedMessageField = null;
			if (selectedMessageField == null) HugsLibController.Logger.Error("Failed to reflect EditWindow_Log.selectedMessage");
		}

		public static void DrawLogWindowExtensions(Window window, Rect inRect) {
			var selectedMessage = selectedMessageField != null ? (LogMessage)selectedMessageField.GetValue(window) : null;
			var prevColor = GUI.color;
			var widgetRow = new WidgetRow(inRect.width, inRect.y, UIDirection.LeftThenUp);
			// publish logs button
			GUI.color = shareButtonColor;
			if (widgetRow.ButtonText("HugsLib_logs_shareBtn".Translate())) {
				HugsLibController.Instance.LogUploader.ShowPublishPrompt();
			}
			GUI.color = prevColor;
			// Files drop-down menu
			if (widgetRow.ButtonText("HugsLib_logs_filesBtn".Translate())) {
				Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> {
					new FloatMenuOption("HugsLib_logs_openLogFile".Translate(), () => {
						ShellOpenLog.Execute();
					}),
					new FloatMenuOption("HugsLib_logs_openSaveDir".Translate(), () => {
						ShellOpenDirectory.Execute(GenFilePaths.SaveDataFolderPath);
					}),
					new FloatMenuOption("HugsLib_logs_openModsDir".Translate(), () => {
						ShellOpenDirectory.Execute(GenFilePaths.CoreModsFolderPath);
					})
				}));
			}
		}

	

		private static bool DoAutoWidthButton(Vector2 position, string label) {
			return Widgets.ButtonText(GetAutoWidthRect(position, label), label);
		}

		private static Rect GetAutoWidthRect(Vector2 position, string label) {
			const float ButtonPaddingX = 16f;
			const float ButtonPaddingY = 2f;
			var buttonSize = Text.CalcSize(label);
			buttonSize.x += ButtonPaddingX;
			buttonSize.y += ButtonPaddingY;
			return new Rect(position.x - buttonSize.x, position.y, buttonSize.x, buttonSize.y);
		}
	}
}