using System;
using HarmonyLib;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Hooks into the flow of the vanilla MonoBehavior.OnGUI()
	/// This allows to take advantage of automatic UI scaling (called after <see cref="UI.ApplyUIScale"/>)
	/// and prevents GUI updates during a loading screen.
	/// </summary>
	[HarmonyPatch(typeof(Root))]
	[HarmonyPatch(nameof(Root.OnGUI))]
	[HarmonyPatch(new Type[0])]
	internal static class Root_OnGUI_Patch {
		[HarmonyPostfix]
		private static void OnGUIHook() {
			if (!LongEventHandler.ShouldWaitForEvent) {
				HugsLibController.Instance.OnGUI();
			}
			HugsLibController.Instance.OnGUIUnfiltered();
		}
	}
}