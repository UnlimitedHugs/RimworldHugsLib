using System;
using System.Collections.Generic;
using System.Reflection;
using HugsLib.Shell;
using HugsLib.Utils;
using LudeonTK;
using UnityEngine;
using Verse;

namespace HugsLib.Logs {
	/// <summary>
	/// Allows adding custom buttons to the EditWindow_Log window.
	/// </summary>
	[StaticConstructorOnStartup] 
	public static class LogWindowExtensions {
		/// <summary>
		/// Alignment side for custom widgets.
		/// </summary>
		public enum WidgetAlignMode {
			Left, Right
		}

		/// <summary>
		/// Callback to draw log window widgets in.
		/// </summary>
		/// <param name="logWindow">The log window being dawn.</param>
		/// <param name="widgetArea">Window area for custom widgets.</param>
		/// <param name="selectedLogMessage">The currently selected log message, or null.</param>
		/// <param name="widgetRow">Draw your widget using this to automatically align it with the others.</param>
		public delegate void WidgetDrawer(Window logWindow, Rect widgetArea, LogMessage selectedLogMessage, WidgetRow widgetRow);

		private static readonly float widgetRowHeight = 23f;
		private static readonly float buttonRowMarginTop = 2f;
		private static readonly Color shareButtonColor = new Color(.3f, 1f, .3f, 1f);
		private static readonly Color separatorLineColor = GenColor.FromHex("303030");
		private static readonly List<LogWindowWidget> widgets = new List<LogWindowWidget>();
		
		private static Texture2D lineTexture;
		private static FieldInfo selectedMessageField;

		internal static float ExtensionsAreaHeight {
			get { return widgets.Count > 0 ? widgetRowHeight : 0f; }
		}

		/// <summary>
		/// Adds a new drawing callback to the log window widget drawer.
		/// </summary>
		/// <param name="drawerDelegate">The delegate called each OnGUI to draw the widget.</param>
		/// <param name="align">The side of the WidgetRow this widget should be drawn into.</param>
		public static void AddLogWindowWidget(WidgetDrawer drawerDelegate, WidgetAlignMode align = WidgetAlignMode.Left) {
			if(drawerDelegate == null) throw new NullReferenceException("Drawer delegate required");
			widgets.Add(new LogWindowWidget(drawerDelegate, align));
		}

		internal static void PrepareReflection() {
			selectedMessageField = typeof(EditWindow_Log).GetField("selectedMessage", BindingFlags.NonPublic | BindingFlags.Static);
			if (selectedMessageField != null && selectedMessageField.FieldType != typeof(LogMessage)) selectedMessageField = null;
			if (selectedMessageField == null) HugsLibController.Logger.Error("Failed to reflect EditWindow_Log.selectedMessage");
			LongEventHandler.ExecuteWhenFinished(() => lineTexture = SolidColorMaterials.NewSolidColorTexture(separatorLineColor));
			AddOwnWidgets();
		}

		internal static void DrawLogWindowExtensions(Window window, Rect inRect) {
			if(widgets.Count == 0) return;
			var selectedMessage = selectedMessageField != null ? (LogMessage)selectedMessageField.GetValue(window) : null;
			Text.Font = GameFont.Tiny;
			var buttonsRect = new Rect(inRect.x, inRect.y + buttonRowMarginTop, inRect.width, inRect.height - buttonRowMarginTop);
			var widgetRowLeft = new WidgetRow(buttonsRect.x, buttonsRect.y);
			var widgetRowRight = new WidgetRow(buttonsRect.width, buttonsRect.y, UIDirection.LeftThenUp);
			// horizontal line
			GUI.DrawTexture(new Rect(inRect.x, inRect.y, inRect.width, 1f), lineTexture);
			// widgets
			for (int i = 0; i < widgets.Count; i++) {
				var widget = widgets[i];
				try {
					var row = widget.Alignment == WidgetAlignMode.Left ? widgetRowLeft : widgetRowRight;
					widget.Drawer(window, inRect, selectedMessage, row);
				} catch (Exception e) {
					HugsLibController.Logger.Error("Exception while drawing log window widget: "+e);
					widgets.RemoveAt(i);
					break;
				}
			}
			Text.Font = GameFont.Small;
		}
		
		private static void CopyMessage(LogMessage logMessage) {
			HugsLibUtility.CopyToClipboard(logMessage.text + "\n" + logMessage.StackTrace);
		}

		private static void AddOwnWidgets() {
			// publish logs button
			AddLogWindowWidget((window, area, message, row) => {
				var prevColor = GUI.color;
				GUI.color = shareButtonColor;
				if (row.ButtonText("HugsLib_logs_shareBtn".Translate())) {
					HugsLibController.Instance.LogUploader.ShowPublishPrompt();
				}
				GUI.color = prevColor;	
			});
			// Files drop-down menu
			AddLogWindowWidget((window, area, message, row) => {
				if (row.ButtonText("HugsLib_logs_filesBtn".Translate())) {
					Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption> {
					new FloatMenuOption("HugsLib_logs_openLogFile".Translate(), () => {
						ShellOpenLog.Execute();
					}),
					new FloatMenuOption("HugsLib_logs_openSaveDir".Translate(), () => {
						ShellOpenDirectory.Execute(GenFilePaths.SaveDataFolderPath);
					}),
					new FloatMenuOption("HugsLib_logs_openModsDir".Translate(), () => {
						ShellOpenDirectory.Execute(GenFilePaths.ModsFolderName);
					})
				}));
				}
			});
			// copy button
			AddLogWindowWidget((window, area, message, row) => {
				if (message != null) {
					if (row.ButtonText("HugsLib_logs_copy".Translate())) {
						CopyMessage(message);
					}
				}
			}, WidgetAlignMode.Right);
		}

		private class LogWindowWidget {
			public readonly WidgetDrawer Drawer;
			public readonly WidgetAlignMode Alignment;
			public LogWindowWidget(WidgetDrawer drawer, WidgetAlignMode alignment) {
				Drawer = drawer;
				Alignment = alignment;
			}
		}
	}
}