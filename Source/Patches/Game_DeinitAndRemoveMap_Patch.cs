using HarmonyLib;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds a hook for discarding maps.
	/// </summary>
	[HarmonyPatch(typeof (Game))]
	[HarmonyPatch("DeinitAndRemoveMap")]
	[HarmonyPatch(new[] { typeof(Map) })]
	internal static class Game_DeinitAndRemoveMap_Patch {
		[HarmonyPostfix]
		private static void MapRemovalHook(Map map) {
			HugsLibController.Instance.OnMapDiscarded(map);
		}
	}
}