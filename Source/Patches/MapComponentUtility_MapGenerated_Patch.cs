using HarmonyLib;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds a hook to produce the MapGenerated callback for ModBase mods.
	/// </summary>
	[HarmonyPatch(typeof(MapComponentUtility))]
	[HarmonyPatch("MapGenerated")]
	[HarmonyPatch(new[]{typeof(Map)})]
	internal static class MapComponentUtility_MapGenerated_Patch {
		[HarmonyPostfix]
		public static void MapGeneratedHook(Map map) {
			HugsLibController.Instance.OnMapGenerated(map);
		}
	}
}