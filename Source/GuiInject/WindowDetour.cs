using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Source.Detour;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace HugsLib.GuiInject {
	/**
	 * Detours Window.WindowOnGUI to allow injection for modded code using WindowInjectionManager 
	 * // TODO: on update, look over the vanilla code to ensure it still matches the original
	 */
	public class WindowDetour : Window {
		// how often fields for already closed windows will be cleaned up
		private const float PruneFieldsIntervalSeconds = 10f;

		// by using a lookup we can avoid the overhead the reflection of private fields would incur
		private static readonly Dictionary<int, WindowFields> windowFields = new Dictionary<int, WindowFields>();
		private static float lastPruneTime;


		[DetourMethod(typeof (Window), "WindowOnGUI")]
		private void _WindowOnGUI() {
			TryPruneFields();
			WindowFields fields;
			var windowHash = GetHashCode();
			windowFields.TryGetValue(windowHash, out fields);
			if (fields == null) {
				fields = new WindowFields();
				windowFields.Add(windowHash, fields);
			}

			// ===== Vanilla begin ===
			if (this.resizeable) {
				if (fields.resizer == null) {
					fields.resizer = new WindowResizer();
				}
				if (fields.resizeLater) {
					fields.resizeLater = false;
					this.windowRect = fields.resizeLaterRect;
				}
			}
			Rect winRect = this.windowRect.AtZero();
			this.windowRect = GUI.Window(this.ID, this.windowRect, delegate(int x) {
				Find.WindowStack.currentlyDrawnWindow = this;
				if (this.doWindowBackground) {
					Widgets.DrawWindowBackground(winRect);
				}
				if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) {
					Find.WindowStack.Notify_PressedEscape();
				}
				if (Event.current.type == EventType.MouseDown) {
					Find.WindowStack.Notify_ClickedInsideWindow(this);
				}
				if (Event.current.type == EventType.KeyDown && !Find.WindowStack.GetsInput(this)) {
					Event.current.Use();
				}
				if (!this.optionalTitle.NullOrEmpty()) {
					GUI.Label(new Rect(this.Margin, this.Margin, this.windowRect.width, 25f), this.optionalTitle);
				}
				if (this.doCloseX && Widgets.CloseButtonFor(winRect)) {
					this.Close(true);
				}
				if (this.resizeable && Event.current.type != EventType.Repaint) {
					Rect lhs = fields.resizer.DoResizeControl(this.windowRect);
					if (lhs != this.windowRect) {
						fields.resizeLater = true;
						fields.resizeLaterRect = lhs;
					}
				}
				Rect rect = winRect.ContractedBy(this.Margin);
				if (!this.optionalTitle.NullOrEmpty()) {
					rect.yMin += this.Margin + 25f;
				}
				GUI.BeginGroup(rect);
				// ===== Vanilla end ===
				var inRect = rect.AtZero();
				var injectionSet = WindowInjectionManager.GetSetForWindowType(GetType());
				if (injectionSet != null && injectionSet.beforeContents != null) {
					InvokeCallbackList(injectionSet.beforeContents, this, inRect);
				}
				if (injectionSet != null && injectionSet.replaceContents != null) {
					InvokeCallbackList(injectionSet.replaceContents, this, inRect);
				} else {
					// ===== Vanilla begin ===
					try {
						this.DoWindowContents(inRect);
					} catch (Exception ex) {
						Log.Error(string.Concat(new object[] {
							"Exception filling window for ",
							this.GetType().ToString(),
							": ",
							ex
						}));
					}
					// ===== Vanilla end ===
				}
				if (injectionSet != null && injectionSet.afterContents != null) {
					InvokeCallbackList(injectionSet.afterContents, this, inRect);
				}
				// ===== Vanilla begin ===
				GUI.EndGroup();
				if (this.resizeable && Event.current.type == EventType.Repaint) {
					fields.resizer.DoResizeControl(this.windowRect);
				}
				if (this.doCloseButton) {
					Text.Font = GameFont.Small;
					Rect rect2 = new Rect(winRect.width/2f - this.CloseButSize.x/2f, winRect.height - 55f, this.CloseButSize.x, this.CloseButSize.y);
					if (Widgets.ButtonText(rect2, "CloseButton".Translate(), true, false, true)) {
						this.Close(true);
					}
				}
				if (this.closeOnEscapeKey && Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Escape || Event.current.keyCode == KeyCode.Return)) {
					this.Close(true);
					Event.current.Use();
				}
				if (this.draggable) {
					GUI.DragWindow();
				} else if (Event.current.type == EventType.MouseDown) {
					Event.current.Use();
				}
				ScreenFader.OverlayOnGUI(winRect.size);
				Find.WindowStack.currentlyDrawnWindow = null;
			}, string.Empty, Widgets.EmptyStyle);

			// ===== Vanilla end ===

		}

		private static void InvokeCallbackList(List<WindowInjectionManager.DrawInjectedContents> callbacks, Window _this, Rect inRect) {
			List<WindowInjectionManager.DrawInjectedContents> faultyCallbacks = null;
			for (int i = 0; i < callbacks.Count; i++) {
				try {
					callbacks[i](_this, inRect);
				} catch (Exception e) {
					HugsLibController.Logger.Error("Injected window callback ({0}) caused an exception and was removed. Exception was: {1}", 
						HugsLibUtility.DescribeDelegate(callbacks[0]), e);
					if (WindowInjectionManager.RemoveErroringCallbacks || !Prefs.DevMode) {
						if (faultyCallbacks == null) {
							faultyCallbacks = new List<WindowInjectionManager.DrawInjectedContents>();
						}
						faultyCallbacks.Add(callbacks[i]);
					}
				}
			}
			if (faultyCallbacks != null) {
				callbacks.RemoveAll(c => faultyCallbacks.Contains(c));
			}
		}

		// every few seconds remove WindowFields objects for windows no longer on the stack to avoid leaking memory
		private static void TryPruneFields() {
			if(Event.current.type != EventType.Repaint) return;
			if (lastPruneTime + PruneFieldsIntervalSeconds > Time.realtimeSinceStartup) return;
			lastPruneTime = Time.realtimeSinceStartup;
			if (Find.UIRoot == null || Find.UIRoot.windows == null || Find.UIRoot.windows.Windows == null) return;
			var windowHashes = Find.UIRoot.windows.Windows.Select(w => w.GetHashCode());
			var countBefore = windowFields.Count;
			windowFields.RemoveAll(pair => !windowHashes.Contains(pair.Key));
			var countAfter = windowFields.Count;
			if (countAfter<countBefore) {
				//Log.Message("Pruned: " + (countBefore - countAfter) + ", current window fields: " + windowFields.Count);
			}
		}

		// this is never actually called
		public override void DoWindowContents(Rect inRect) {
		}

		// 
		private class WindowFields {
			public WindowResizer resizer;
			public bool resizeLater;
			public Rect resizeLaterRect;
		}
	}
}