using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds a hook to produce the MapLoaded callback for ModBase mods.
	/// </summary>
	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch("FinalizeInit")]
	[HarmonyPatch(new Type[0])]
	internal static class Map_FinalizeInit_Patch {
		[HarmonyPostfix]
		private static void MapLoadedHook(Map __instance) {
			HugsLibController.Instance.OnMapInitFinalized(__instance);
		} 
	}
}