using System;
using UnityEngine;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// A compact message dialog with a title and a custom close button label.
	/// </summary>
	public class Dialog_Message : Window {
		private readonly string title;
		private readonly string message;
		private readonly string? closeButtonText;
		private readonly Action? postCloseAction;

		public override Vector2 InitialSize {
			get { return new Vector2(500f, 400f); }
		}

		/// <param name="title">A title to display in the dialog</param>
		/// <param name="message">A message to display in the dialog</param>
		/// <param name="closeButtonText">A custom label to the close button. Optional- when null, the default label will be used instead.</param>
		/// <param name="postCloseAction">A callback to call when the dialog is closed</param>
		public Dialog_Message(string title, string message, string? closeButtonText = null, Action? postCloseAction = null) {
			this.title = title;
			this.message = message;
			this.closeButtonText = closeButtonText;
			this.postCloseAction = postCloseAction;
			closeOnCancel = true;
			closeOnAccept = false;
			doCloseButton = false;
			doCloseX = true;
			forcePause = true;
			absorbInputAroundWindow = true;
			layer = WindowLayer.SubSuper;
		}

		public override void DoWindowContents(Rect inRect) {
			Text.Font = GameFont.Medium;
			var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 40);
			Widgets.Label(titleRect, title);
			Text.Font = GameFont.Small;
			Widgets.Label(new Rect(inRect.x, inRect.y + titleRect.height, inRect.width, inRect.height - titleRect.height), message);
			var closeButtonRect = new Rect(inRect.width/2f - CloseButSize.x/2f, inRect.height - CloseButSize.y, CloseButSize.x, CloseButSize.y);
			var closeText = closeButtonText ?? "CloseButton".Translate();
			if (Widgets.ButtonText(closeButtonRect, closeText)) {
				Close();
			}
		}

		public override void PostClose() {
			base.PostClose();
			if (postCloseAction != null) postCloseAction();
		}
	}
}