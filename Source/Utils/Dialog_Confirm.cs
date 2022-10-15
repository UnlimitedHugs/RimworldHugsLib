using System;
using UnityEngine;
using Verse;

namespace HugsLib.Utils {
	/// <summary>
	/// A classic A15-style confirm dialog with Esc and Enter key support.
	/// </summary>
	public class Dialog_Confirm : Dialog_MessageBox {
		private const float TitleHeight = 42f;
		private const float DialogWidth = 500f;
		private const float DialogHeight = 300f;

		public override Vector2 InitialSize {
			get {
				float height = DialogHeight;
				if (title != null) height += TitleHeight;
				return new Vector2(DialogWidth, height);
			}
		}

		public Dialog_Confirm(string text, Action? confirmedAct = null, bool destructive = false, string? title = null)
			: base(text, "Confirm".Translate(), confirmedAct, "GoBack".Translate(), null, title, destructive) {
			closeOnCancel = false;
			closeOnAccept = false;
		}

		public override void DoWindowContents(Rect inRect) {
			base.DoWindowContents(inRect);
			if (Event.current.type == EventType.KeyDown) {
				if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) {
					if (buttonAAction != null) buttonAAction();
					Close();	
				} else if (Event.current.keyCode == KeyCode.Escape) {
					if (buttonBAction != null) buttonBAction();
					Close();
				}
			}
		}
	}
}