using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds a hook to produce the MapComponentsInitializing callback for ModBase mods.
	/// </summary>
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch("ConstructComponents")]
	[HarmonyPatch(new Type[0])]
	internal static class Map_ConstructComponents_Patch {
		[HarmonyPostfix]
		private static void MapComponentsInitHook(Map __instance) {
			HugsLibController.Instance.OnMapComponentsConstructed(__instance);
		}
	}
}