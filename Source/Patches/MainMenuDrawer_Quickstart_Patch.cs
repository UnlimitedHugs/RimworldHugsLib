using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Quickstart;
using RimWorld;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Rewire the main menu "Dev quicktest" button to trigger the HugsLib quickstarter.
	/// </summary>
	[HarmonyPatch(typeof(MainMenuDrawer))]
	[HarmonyPatch(nameof(MainMenuDrawer.DoMainMenuControls))]
	internal class MainMenuDrawer_Quickstart_Patch {
		private static bool patched;

		[HarmonyCleanup]
		public static void Cleanup() {
			if (!patched)
				HugsLibController.Logger.Warning(nameof(MainMenuDrawer_Quickstart_Patch) + " could not be applied.");
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> QuicktestButtonUsesQuickstarter(
			IEnumerable<CodeInstruction> instructions) {
			patched = false;
			CodeInstruction lastBrFalseSInstruction = null;
			foreach (var inst in instructions) {
				if (!patched) {
					if (inst.opcode == OpCodes.Brfalse_S) lastBrFalseSInstruction = inst;
					if (inst.Is(OpCodes.Ldstr, "DevQuickTest")) {
						if (lastBrFalseSInstruction == null) throw new Exception("Expected branch not found");
						// add replacement button
						yield return new CodeInstruction(OpCodes.Call,
							((Action<List<ListableOption>>)QuickstartController.AddReplacementQuickstartButton).Method);
						// jump to end of if statement
						yield return new CodeInstruction(OpCodes.Br, lastBrFalseSInstruction.operand);
						patched = true;
					}
				}
				yield return inst;
			}
		}
	}
}