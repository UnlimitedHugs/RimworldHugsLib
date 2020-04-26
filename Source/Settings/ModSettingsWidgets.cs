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
		private const float HoverMenuOpacityEnabled = .5f;
		private const float HoverMenuOpacityDisabled = .08f;

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
		public static bool DrawHandleHoverMenu(Vector2 topRight, string infoTooltip, bool menuEnabled) {
			var menuClicked = DrawHoverMenuButton(topRight, menuEnabled);

			var infoEnabled = !string.IsNullOrEmpty(infoTooltip); 
			var infoButtonTopLeft = new Vector2(
				topRight.x - MenuIconTexture.width - HoverMenuButtonSpacing - InfoIconTexture.width, topRight.y);
			var (infoHovered, _) = DoHoverMenuButton(infoButtonTopLeft, InfoIconTexture, infoEnabled);
			if (infoHovered) {
				DrawImmediateTooltip(infoTooltip);
			}

			return menuClicked;
		}

		internal static bool DrawHoverMenuButton(Vector2 topRight, bool enabled) {
			var menuButtonTopLeft = new Vector2(topRight.x - MenuIconTexture.width, topRight.y);
			var (_, menuClicked) = DoHoverMenuButton(menuButtonTopLeft, MenuIconTexture, enabled);
			return menuClicked;
		}

		internal static void OpenFloatMenu(IEnumerable<FloatMenuOption> options) {
			Find.WindowStack.Add(new FloatMenu(options.ToList()));
		}
		
		internal static IEnumerable<FloatMenuOption> CreateContextMenuOptions(IEnumerable<ContextMenuEntry> entries) {
			var options = new List<FloatMenuOption>();
			try {
				entries = entries ?? Enumerable.Empty<ContextMenuEntry>();
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

		private static (bool hovered, bool clicked) DoHoverMenuButton(Vector2 topLeft, Texture texture, bool enabled) {
			bool hovered = false, clicked = false;
			var buttonRect = new Rect(topLeft.x, topLeft.y, InfoIconTexture.width, InfoIconTexture.height);
			if (enabled && Mouse.IsOver(buttonRect)) {
				Widgets.DrawHighlight(buttonRect);
				hovered = true;
			}
			var prevColor = GUI.color;
			var opacity = enabled ? HoverMenuOpacityEnabled : HoverMenuOpacityDisabled;
			GUI.color = new Color(1f, 1f, 1f, opacity);
			GUI.DrawTexture(buttonRect, texture);
			GUI.color = prevColor;
			if (enabled && Widgets.ButtonInvisible(buttonRect)) {
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
}