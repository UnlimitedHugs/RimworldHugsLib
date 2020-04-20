using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Core;
using UnityEngine;
using Verse;

namespace HugsLib.Settings {
	public class ModSettingsHoverMenu {
		private const float HoverMenuButtonSpacing = 3f;

		private static Texture2D InfoIconTexture {
			get { return HugsLibTextures.HLInfoIcon; }
		}
		private static Texture2D MenuIconTexture {
			get { return HugsLibTextures.HLMenuIcon; }
		}

		public event Action<IHoverMenuHandle> HandleReset;
		
		public Vector2 DrawSize {
			get {
				return new Vector2(
					InfoIconTexture.width + HoverMenuButtonSpacing + MenuIconTexture.width,
					Mathf.Max(InfoIconTexture.height, MenuIconTexture.width)
				);
			}
		}

		public void Draw(Vector2 topLeft, IHoverMenuHandle forHandle, float opacity = .5f) {
			if (!string.IsNullOrWhiteSpace(forHandle.Description)) {
				var (infoHovered, _) = DoHoverMenuButton(topLeft, InfoIconTexture, opacity);
				if (infoHovered) {
					DrawImmediateTooltip(forHandle.Description);
				}
			}
			var menuButtonTopLeft = new Vector2(topLeft.x + HoverMenuButtonSpacing + InfoIconTexture.width, topLeft.y);
			var (_, menuClicked) = DoHoverMenuButton(menuButtonTopLeft, MenuIconTexture, opacity);
			if (menuClicked) {
				OpenHandleFloatMenu(forHandle);
			}
		}

		public void OpenHandleFloatMenu(IHoverMenuHandle forHandle) {
			var options = GetFloatMenuOptions(forHandle);
			Find.WindowStack.Add(new FloatMenu(options));
		}

		private List<FloatMenuOption> GetFloatMenuOptions(IHoverMenuHandle forHandle) {
			var options = new List<FloatMenuOption> {
				new FloatMenuOption("HugsLib_settings_resetValue".Translate(), ResetHandleAction)
					{Disabled = !forHandle.CanBeReset}
			};
			options.AddRange(GetCustomFloatMenuOptions(forHandle));
			return options;

			void ResetHandleAction() {
				forHandle.ResetToDefault();
				HandleReset?.Invoke(forHandle);
			}
		}

		private static IEnumerable<FloatMenuOption> GetCustomFloatMenuOptions(IHoverMenuHandle forHandle) {
			var options = new List<FloatMenuOption>();
			try {
				var entries = forHandle.ContextMenuEntries?.Invoke() ?? Enumerable.Empty<ContextMenuEntry>();
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

	public interface IHoverMenuHandle {
		string Description { get; }
		Func<IEnumerable<ContextMenuEntry>> ContextMenuEntries { get; }
		bool CanBeReset { get; }
		void ResetToDefault();
	}
}