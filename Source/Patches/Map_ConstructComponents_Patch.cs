using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch("ConstructComponents")]
	[HarmonyPatch(new Type[0])]
	internal static class Map_ConstructComponents_Patch {
		private static void Postfix(Map __instance) {
			HugsLibController.Instance.OnMapComponentsConstructed(__instance);
		}
	}
}