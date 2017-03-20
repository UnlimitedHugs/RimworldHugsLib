using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(UIRoot))]
	[HarmonyPatch("UIRootOnGUI")]
	[HarmonyPatch(new Type[0])]
	internal static class UIRoot_Patch {
		private static void Postfix() {
			HugsLibController.Instance.OnGUI();
		}
	}
}