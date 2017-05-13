using Harmony;
using HugsLib.Logs;
using UnityEngine;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds extra buttons to the Log window.
	/// </summary>
	[HarmonyPatch(typeof(EditWindow_Log))]
	[HarmonyPatch("DoMessagesListing")]
	[HarmonyPatch(new[]{typeof(Rect)})]
	internal static class EditWindow_Log_Patch {
		[HarmonyPrefix]
		private static bool ExtraLogWindowButtons(Window __instance, ref Rect listingRect) {
			var extensionsRect = new Rect(listingRect);
			listingRect.yMax -= LogWindowExtensions.ExtensionsAreaHeight;
			extensionsRect.yMin = listingRect.yMax;
			LogWindowExtensions.DrawLogWindowExtensions(__instance, extensionsRect);
			return true;
		} 
	}
}