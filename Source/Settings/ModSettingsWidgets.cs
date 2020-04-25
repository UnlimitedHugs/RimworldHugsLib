using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Core;
using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	/// <summary>
	/// Helper methods for drawing elements and controls that appear in the <see cref="Dialog_ModSettings"/> window.
	/// </summary>
	public static class ModSettingsWidgets {
		private const float HoverMenuButtonSpacing = 3f;

		private static Texture2D InfoIconTexture {
			get { return HugsLibTextures.HLInfoIcon; }
		}
		private static Texture2D MenuIconTexture {
			get { return HugsLibTextures.HLMenuIcon; }
		}

		public static float HoverMenuHeight {
			get { return MenuIconTexture.height; }
		}

		/// <summary>
		/// Draws a floating menu of 2 buttons: info and menu.
		/// </summary>
		/// <returns>true if the menu button was clicked</returns>
		public static bool DrawHandleHoverMenu(Vector2 topRight, string infoTooltip, float opacity = .5f) {
			var menuClicked = DrawHoverMenuButton(topRight, opacity);
			
			if (!string.IsNullOrWhiteSpace(infoTooltip)) {
				var infoButtonTopLeft = new Vector2(
					topRight.x - MenuIconTexture.width - HoverMenuButtonSpacing - InfoIconTexture.width, topRight.y);
				var (infoHovered, _) = DoHoverMenuButton(infoButtonTopLeft, InfoIconTexture, opacity);
				if (infoHovered) {
					DrawImmediateTooltip(infoTooltip);
				}
			}

			return menuClicked;
		}

		internal static bool DrawHoverMenuButton(Vector2 topRight, float opacity = .5f) {
			var menuButtonTopLeft = new Vector2(topRight.x - MenuIconTexture.width, topRight.y);
			var (_, menuClicked) = DoHoverMenuButton(menuButtonTopLeft, MenuIconTexture, opacity);
			return menuClicked;
		}

		internal static IEnumerable<FloatMenuOption> GetResetContextMenuOption(
			IResettable forHandle, string label, Action selectedAction) {
			return new[] {
				new FloatMenuOption(label, selectedAction)
					{Disabled = !forHandle.CanBeReset}
			};
		}

		internal static void OpenFloatMenu(IEnumerable<FloatMenuOption> options) {
			Find.WindowStack.Add(new FloatMenu(options.ToList()));
		}
		
		internal static IEnumerable<FloatMenuOption> GetHandleContextMenuEntries(IContextMenuEntryProvider forHandle) {
			var options = new List<FloatMenuOption>();
			try {
				var entries = forHandle.ContextMenuEntries ?? Enumerable.Empty<ContextMenuEntry>();
				foreach (var entry in entries) {
					entry.Validate();
					options.Add(
						new FloatMenuOption(entry.Label, entry.Action) {Disabled = entry.Disabled}
					);
				}
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
			return options;
		}

		private static (bool hovered, bool clicked) DoHoverMenuButton(Vector2 topLeft, Texture texture, float opacity) {
			bool hovered = false, clicked = false;
			var buttonRect = new Rect(topLeft.x, topLeft.y, InfoIconTexture.width, InfoIconTexture.height);
			if (Mouse.IsOver(buttonRect)) {
				Widgets.DrawHighlight(buttonRect);
				hovered = true;
			}
			var prevColor = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, opacity);
			GUI.DrawTexture(buttonRect, texture);
			GUI.color = prevColor;
			if (Widgets.ButtonInvisible(buttonRect)) {
				clicked = true;
			}
			return (hovered, clicked);
		}

		private static void DrawImmediateTooltip(string tipText) {
			if (Event.current.type != EventType.Repaint) return;
			var tip = new ActiveTip(tipText);
			var tipRect = tip.TipRect;
			var tipPosition = GenUI.GetMouseAttachedWindowPos(tipRect.width, tipRect.height);
			tipPosition = GUIPositionLocalToGlobal(tipPosition);
			tip.DrawTooltip(tipPosition);
		}

		private static Vector2 GUIPositionLocalToGlobal(Vector2 localPosition) {
			return localPosition + (UI.MousePositionOnUIInverted - Event.current.mousePosition);
		}
	}

	internal interface IResettable {
		bool CanBeReset { get; }
		void ResetToDefault();
	}
	
	internal interface IContextMenuEntryProvider {
		IEnumerable<ContextMenuEntry> ContextMenuEntries { get; }
	}
}