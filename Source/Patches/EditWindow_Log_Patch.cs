using Harmony;
using HugsLib.Logs;
using UnityEngine;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(EditWindow_Log))]
	[HarmonyPatch("DoWindowContents")]
	[HarmonyPatch(new[]{typeof(Rect)})]
	internal static class EditWindow_Log_Patch {
		private static void Postfix(Window __instance, Rect inRect) {
			LogWindowInjection.DrawLogWindowExtensions(__instance, inRect);
		} 
	}
}