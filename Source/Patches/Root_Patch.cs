using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(Root))]
	[HarmonyPatch("Update")]
	[HarmonyPatch(new Type[0])]
	internal static class Root_Patch {
		private static void Postfix() {
			HugsLibController.Instance.OnUpdate();
		}
	}
}