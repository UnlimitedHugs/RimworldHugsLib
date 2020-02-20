using HarmonyLib;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds a hook for the early initialization of a Game.
	/// </summary>
	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch("FillComponents")]
	internal static class Game_FillComponents_Patch {
		[HarmonyPrefix]
		public static void GameInitializationHook(Game __instance) {
			HugsLibController.Instance.OnGameInitializationStart(__instance);
		}
	}
}