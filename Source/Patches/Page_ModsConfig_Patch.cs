using Harmony;
using HugsLib.Restarter;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(Page_ModsConfig))]
	[HarmonyPatch("DoWindowContents")]
	[HarmonyPatch(new[] { typeof(Rect) })]
	internal static class Page_ModsConfig_Patch {
		private static void Postfix(Window __instance, Rect rect) {
			AutoRestarter.DoModsDialogControls(__instance, rect);
		}
	}
}