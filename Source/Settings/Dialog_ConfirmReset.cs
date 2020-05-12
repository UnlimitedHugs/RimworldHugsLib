using System;
using System.Linq;
using System.Text;
using HugsLib.Utils;
using UnityEngine;
using Verse;
using OwnedHandleNames = System.Collections.Generic.IEnumerable<(string ownerName, string[] handleNames)>;

namespace HugsLib.Settings {
	/// <summary>
	/// Shown when selecting the reset option from a mod settings dialog context menu.
	/// Has an option to include hidden settings in the reset.
	/// </summary>
	internal class Dialog_ConfirmReset : Dialog_Confirm {
		private readonly string message;
		private readonly int hiddenHandleCount;
		private readonly string hiddenHandlesTooltip;

		private bool includeHidden;
		public bool IncludeHidden {
			get { return includeHidden; }
		}

		public Dialog_ConfirmReset(string message, OwnedHandleNames hiddenHandles, 
			Action<Dialog_ConfirmReset> confirmedAction)
			: base(string.Empty, null, true) {
			this.message = message;
			var handles = hiddenHandles.ToArray();
			hiddenHandleCount = CountHiddenSettingHandles(handles);
			hiddenHandlesTooltip = GetHiddenHandleListing(handles);
			buttonAAction = () => confirmedAction(this);
			forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
			closeOnAccept = true;
			closeOnCancel = true;
		}

		public override void DoWindowContents(Rect inRect) {
			base.DoWindowContents(inRect);
			var messageHeight = Text.CalcHeight(message, inRect.width);
			DrawMessageLabel();
			if (hiddenHandleCount > 0) {
				DrawHiddenSettingsCheckbox();
			}

			void DrawMessageLabel() {
				var messageRect = inRect.TopPartPixels(messageHeight);
				Widgets.Label(messageRect, message);
			}

			void DrawHiddenSettingsCheckbox() {
				const float checkboxSize = 24f;
				var checkboxRect = new Rect(inRect.x, inRect.y + messageHeight + Margin,
					inRect.width / 2f + 10f + checkboxSize, checkboxSize);
				if (Mouse.IsOver(checkboxRect)) {
					Widgets.DrawHighlight(checkboxRect);
					TooltipHandler.TipRegion(checkboxRect, hiddenHandlesTooltip);
				}
				Widgets.CheckboxLabeled(checkboxRect, 
					"HugsLib_settings_resetIncludeHidden".Translate(hiddenHandleCount), ref includeHidden);
			}
		}

		private static int CountHiddenSettingHandles(OwnedHandleNames handles) {
			return handles.SelectMany(t => t.handleNames)
				.Count();
		}

		private static string GetHiddenHandleListing(OwnedHandleNames handles) {
			var sb = new StringBuilder("HugsLib_settings_hiddenSettingsHeader".Translate());
			sb.AppendLine();
			foreach (var (ownerName, handleNames) in handles) {
				sb.AppendLine();
				sb.AppendFormat("{0}:\n", ownerName);
				foreach (var name in handleNames) {
					sb.Append("  ");
					sb.AppendLine(name);
				}
			}
			return sb.ToString();
		}
	}
}