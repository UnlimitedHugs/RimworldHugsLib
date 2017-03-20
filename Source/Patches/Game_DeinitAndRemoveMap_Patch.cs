using Harmony;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof (Game))]
	[HarmonyPatch("DeinitAndRemoveMap")]
	[HarmonyPatch(new[] { typeof(Map) })]
	internal static class Game_DeinitAndRemoveMap_Patch {
		private static void Postfix(Map map) {
			HugsLibController.Instance.OnMapDiscarded(map);
		}
	}
}