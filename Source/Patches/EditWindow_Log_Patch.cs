using Harmony;
using HugsLib.Logs;
using UnityEngine;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds extra buttons to the Log window.
	/// </summary>
	[HarmonyPatch(typeof(EditWindow_Log))]
	[HarmonyPatch("DoWindowContents")]
	[HarmonyPatch(new[]{typeof(Rect)})]
	internal static class EditWindow_Log_Patch {
		[HarmonyPostfix]
		private static void ExtraLogWindowButtons(Window __instance, Rect inRect) {
			LogWindowInjection.DrawLogWindowExtensions(__instance, inRect);
		} 
	}
}