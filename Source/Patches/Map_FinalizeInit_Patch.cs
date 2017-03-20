using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch("FinalizeInit")]
	[HarmonyPatch(new Type[0])]
	internal static class Map_FinalizeInit_Patch {
		private static void Postfix(Map __instance) {
			HugsLibController.Instance.OnMapInitFinalized(__instance);
		} 
	}
}