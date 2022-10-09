using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Settings;
using RimWorld;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(Dialog_Options))]
	[HarmonyPatch(nameof(Dialog_Options.PostOpen))]
	internal class DialogOptions_PostOpen_Patch {
		[HarmonyPostfix]
		public static void InjectHugsLibEntries(Dialog_Options __instance) {
			OptionsDialogExtensions.InjectHugsLibModEntries(__instance);
		}
	}

	[HarmonyPatch(typeof(Dialog_Options))]
	[HarmonyPatch("DoModOptions")]
	[HarmonyPatch(new[] { typeof(Listing_Standard) })]
	internal class DialogOptions_DoModOptions_Patch {
		private static bool patched;

		[HarmonyCleanup]
		public static void Cleanup() {
			if (!patched)
				HugsLibController.Logger.Error(nameof(DialogOptions_DoModOptions_Patch) + " could not be applied.");
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> InterceptWindowOpening(IEnumerable<CodeInstruction> instructions) {
			patched = false;
			var modSettingsWindowConstructorInfo = typeof(RimWorld.Dialog_ModSettings)
				.GetConstructor(new[] { typeof(Mod) });
			if (modSettingsWindowConstructorInfo == null) throw new Exception("Failed to reflect required method");

			foreach (var inst in instructions) {
				if (!patched && inst.Is(OpCodes.Newobj, modSettingsWindowConstructorInfo)) {
					// replace vanilla Dialog_ModSettings construction with our own for HugsLib mod entries 
					yield return new CodeInstruction(
						OpCodes.Call, ((Func<Mod, Window>)OptionsDialogExtensions.GetModSettingsWindow).Method);
					patched = true;
				} else {
					yield return inst;
				}
			}
		}
	}
}