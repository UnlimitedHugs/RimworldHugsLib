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
		private const float HoverMenuIconSize = 32f;

		private static Texture2D InfoIconTexture {
			get { return HugsLibTextures.HLInfoIcon; }
		}
		private static Texture2D MenuIconNormalTexture {
			get { return HugsLibTextures.HLMenuIcon; }
		}
		private static Texture2D MenuIconPlusTexture {
			get { return HugsLibTextures.HLMenuIconPlus; }
		}

		public static float HoverMenuHeight {
			get { return HoverMenuIconSize; }
		}

		/// <summary>
		/// Draws a hovering menu of 2 buttons: info and menu.
		/// </summary>
		/// <param name="topRight"></param>
		/// <param name="infoTooltip">Text for the info button tooltip. Null to disable.</param>
		/// <param name="menuEnabled">When false, the menu button is semi-transparent and non-interactable</param>
		/// <param name="extraMenuOptions">When true, uses menu-with-plus-badge icon for the button</param>
		/// <returns>true if the menu button was clicked</returns>
		/// <returns>true if the menu button was clicked</returns>
		public static bool DrawHandleHoverMenu(
			Vector2 topRight, string infoTooltip, bool menuEnabled, bool extraMenuOptions) {
			var menuClicked = DrawHoverMenuButton(topRight, menuEnabled, extraMenuOptions);

			var infoEnabled = !string.IsNullOrEmpty(infoTooltip); 
			var infoButtonTopLeft = new Vector2(
				topRight.x - HoverMenuIconSize - HoverMenuButtonSpacing - InfoIconTexture.width, topRight.y);
			var (infoHovered, _) = DoHoverMenuButton(infoButtonTopLeft, InfoIconTexture, infoEnabled);
			if (infoHovered) {
				DrawImmediateTooltip(infoTooltip);
			}

			return menuClicked;
		}

		/// <summary>
		/// Draws the menu button for the hovering menu.
		/// </summary>
		/// <param name="topRight"></param>
		/// <param name="enabled">When false, the button is semi-transparent and non-interactable</param>
		/// <param name="extraMenuOptions">When true, uses menu-with-plus-badge icon for the button</param>
		/// <returns>true if the menu button was clicked</returns>
		public static bool DrawHoverMenuButton(Vector2 topRight, bool enabled, bool extraMenuOptions) {
			var texture = extraMenuOptions ? MenuIconPlusTexture : MenuIconNormalTexture;
			var menuButtonTopLeft = new Vector2(topRight.x - texture.width, topRight.y);
			var (_, menuClicked) = DoHoverMenuButton(menuButtonTopLeft, texture, enabled);
			return menuClicked;
		}

		internal static void OpenFloatMenu(IEnumerable<FloatMenuOption> options) {
			Find.WindowStack.Add(new FloatMenu(options.ToList()));
		}

		internal static void OpenExtensibleContextMenu(
			string firstEntryLabel, Action firstEntryActivated, Action anyEntryActivated, IEnumerable<ContextMenuEntry> 
			additionalEntries) {
			OpenFloatMenu(
				GetOptionalMenuEntry(firstEntryLabel, firstEntryActivated + anyEntryActivated)
					.Concat(CreateContextMenuOptions(additionalEntries, anyEntryActivated))
			);
		}

		private static IEnumerable<FloatMenuOption> GetOptionalMenuEntry(string label, Action onActivated) {
			return label != null
				? new[] {new FloatMenuOption(label, onActivated)}
				: Enumerable.Empty<FloatMenuOption>();
		}

		private static IEnumerable<FloatMenuOption> CreateContextMenuOptions(
			IEnumerable<ContextMenuEntry> entries, Action anyEntryActivated) {
			var options = new List<FloatMenuOption>();
			try {
				entries = entries ?? Enumerable.Empty<ContextMenuEntry>();
				foreach (var entry in entries) {
					entry.Validate();
					options.Add(
						new FloatMenuOption(entry.Label, entry.Action + anyEntryActivated) {Disabled = entry.Disabled}
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