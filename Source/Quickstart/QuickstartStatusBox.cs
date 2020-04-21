using System;
using System.Text;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.Quickstart {
	/// <summary>
	/// Displays at game startup when the quickstarter is scheduled to run.
	/// Shows the pending operation and allows to abort or disable the quickstart.
	/// </summary>
	internal class QuickstartStatusBox {
		public delegate void AbortHandler(bool abortAndDisable);
		
		private static readonly Vector2 StatusRectSize = new Vector2(240f, 75f);
		private static readonly Vector2 StatusRectPadding = new Vector2(26f, 18f);

		public event AbortHandler AbortRequested;

		private readonly IOperationMessageProvider pendingOperation;

		public QuickstartStatusBox(IOperationMessageProvider pendingOperation) {
			this.pendingOperation = pendingOperation ?? throw new ArgumentNullException(nameof(pendingOperation));
		}

		public void OnGUI() {
			var statusText = GetStatusBoxText();
			var boxRect = GetStatusBoxRect(statusText);
			DrawStatusBox(boxRect, statusText);
			HandleKeyPressEvents();
		}

		private string GetStatusBoxText() {
			var sb = new StringBuilder("HugsLib quickstarter preparing to\n");
			sb.Append(pendingOperation.Message);
			sb.AppendLine();
			sb.AppendLine();
			sb.Append("<color=#777777>");
			sb.AppendLine("Press Space to abort");
			sb.Append("Press Shift+Space to disable");
			sb.Append("</color>");
			return sb.ToString();
		}

		private static Rect GetStatusBoxRect(string statusText) {
			var statusTextSize = Text.CalcSize(statusText);
			var boxWidth = Mathf.Max(StatusRectSize.x, statusTextSize.x + StatusRectPadding.x * 2f);
			var boxHeight = Mathf.Max(StatusRectSize.y, statusTextSize.y + StatusRectPadding.y * 2f);
			var boxRect = new Rect(
				(UI.screenWidth - boxWidth) / 2f,
				(UI.screenHeight / 2f - boxHeight) / 2f,
				boxWidth, boxHeight
			);
			boxRect = boxRect.Rounded();
			return boxRect;
		}

		private static void DrawStatusBox(Rect rect, string statusText) {
			Widgets.DrawShadowAround(rect);
			Widgets.DrawWindowBackground(rect);
			var prevAnchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, statusText);
			Text.Anchor = prevAnchor;
		}

		private void HandleKeyPressEvents() {
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space) {
				var abortAndDisable = HugsLibUtility.ShiftIsHeld;
				Event.current.Use();
				AbortRequested?.Invoke(abortAndDisable);
			}
		}

		public interface IOperationMessageProvider {
			string Message { get; }
		}
		
		public class LoadSaveOperation : IOperationMessageProvider {
			private readonly string fileName;
			public string Message {
				get { return $"load save file: {fileName}"; }
			}
			public LoadSaveOperation(string fileName) {
				this.fileName = fileName;
			}
		}
		
		public class GenerateMapOperation : IOperationMessageProvider {
			private readonly string scenario;
			private readonly int mapSize;
			public string Message {
				get { return $"generate map: {scenario} ({mapSize}x{mapSize})"; }
			}
			public GenerateMapOperation(string scenario, int mapSize) {
				this.scenario = scenario;
				this.mapSize = mapSize;
			}
		}
	}
}