using System;
using System.Threading;
using HugsLib.GuiInject;
using HugsLib.Settings;
using HugsLib.Shell;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Restarter {
	/**
	 * Makes the the Close button in the Mods dialog prompt the player to restart the game if the mod configuration has changed.
	 * Will restart the game automatically if the autoRestart setting is active.
	 * Unlike in vanilla, changes in mod order are also detected and will require a restart.
	 */
	internal class AutoRestarter {
		private static readonly Vector2 CloseButSize = new Vector2(120f, 40f);
		
		private static int lastSeenWindowHash;
		private static int lastModListHash;

		public SettingHandle<bool> AutoRestartSetting { get; private set; }
		
		/// <summary>
		/// Puts up a loading screen and restarts the game.
		/// </summary>
		public static void PerformRestart() {
			LongEventHandler.QueueLongEvent(() => {
				// put up the loading screen while the game shuts down
				Thread.Sleep(5000);
			}, "HugsLib_restart_restarting", true, null);
			// execute in main thread
			LongEventHandler.ExecuteWhenFinished(() => ShellRestartRimWorld.Execute());
		}

		/// <summary>
		/// Auto-restarts the game if the player selected the auto-restart setting, or shows the restart dialog otherwise.
		/// </summary>
		/// <param name="dialogTextKey">The text key for the dialog text.</param>
		/// <param name="closeAction">Called when the player closes the dialog without choosing to restart.</param>
		public static void AutoRestartOrShowRestartDialog(string dialogTextKey, Action closeAction) {
			if (HugsLibController.Instance.AutoRestarter.AutoRestartSetting) {
					PerformRestart();
				} else {
					Find.WindowStack.Add(new Dialog_RestartGame(dialogTextKey, closeAction));
				}

		}

		[WindowInjection(typeof (Page_ModsConfig), Mode = WindowInjectionManager.InjectMode.AfterContents)]
		private static void DoModsDialogControls(Window window, Rect inRect) {
			// update mod list hash
			if (window.GetHashCode() != lastSeenWindowHash) {
				lastSeenWindowHash = window.GetHashCode();
				lastModListHash = GetModListHash();
			}
			// replace close button, handle keys
			Text.Font = GameFont.Small;
			window.doCloseButton = false;
			window.closeOnEscapeKey = false;
			var closeBtnRect = new Rect(inRect.width / 2f - CloseButSize.x / 2f, inRect.height - CloseButSize.y, CloseButSize.x, CloseButSize.y);
			var keyUsed = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape);
			if (Widgets.ButtonText(closeBtnRect, "CloseButton".Translate()) || keyUsed) {
				CloseAction(window);
				if(keyUsed) Event.current.Use();
			}
		}

		// TODO: on update, verify that this still does the same thing as Page_ModsConfig.PostClose 
		private static void CloseAction(Window window) {
			ModsConfig.Save();
			if (lastModListHash != GetModListHash()) {
				AutoRestartOrShowRestartDialog("HugsLib_restart_text", () => {
					window.Close();
				});
			} else {
				window.Close();
			}
		}

		// an alternative way to detect changes to the mod list that takes mod order into account
		private static int GetModListHash() {
			int hash = 42;
			foreach (var modMetaData in ModsConfig.ActiveModsInLoadOrder) {
				if (modMetaData.enabled){
					unchecked {
						hash <<= 1;
						hash += hash + modMetaData.GetHashCode();
					}
				}
			}
			return hash;

		}

		public void CreateSettingsHandles(ModSettingsPack pack) {
			AutoRestartSetting = pack.GetHandle("autoRestart", "HugsLib_setting_autoRestart_label".Translate(), "HugsLib_setting_autoRestart_desc".Translate(), false);
		}
	}
}