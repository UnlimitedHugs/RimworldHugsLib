using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof (Game))]
	[HarmonyPatch("FinalizeInit")]
	[HarmonyPatch(new Type[0])]
	internal static class Game_FinalizeInit_Patch {
		private static void Postfix() {
			HugsLibController.Instance.OnPlayingStateEntered();
		}
	}
}