using System;
using HarmonyLib;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds a hook to produce the WorldLoaded callback for ModBase mods.
	/// </summary>
	[HarmonyPatch(typeof (Game))]
	[HarmonyPatch("FinalizeInit")]
	[HarmonyPatch(new Type[0])]
	internal static class Game_FinalizeInit_Patch {
		[HarmonyPostfix]
		private static void WorldLoadedHook() {
			HugsLibController.Instance.OnPlayingStateEntered();
		}
	}
}