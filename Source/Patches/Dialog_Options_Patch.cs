using Harmony;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(Dialog_Options))]
	[HarmonyPatch("DoWindowContents")]
	[HarmonyPatch(new[] { typeof(Rect) })]
	internal static class Dialog_Options_Patch {
		private static void Postfix(Window __instance, Rect inRect) {
			OptionsDialogInjection.DrawSettingsDialogExtensions(__instance, inRect);
		}
	}
}