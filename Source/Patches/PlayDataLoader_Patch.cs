using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(PlayDataLoader))]
	[HarmonyPatch("DoPlayLoad")]
	[HarmonyPatch(new Type[0])]
	internal static class PlayDataLoader_Patch {
		private static void Postfix() {
			HugsLibController.Instance.LoadReloadInitialize();
		}
	}
}