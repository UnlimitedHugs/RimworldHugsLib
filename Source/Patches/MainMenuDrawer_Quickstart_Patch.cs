using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Quickstart;
using RimWorld;

namespace HugsLib.Patches {
	/// <summary>
	/// Rewire the main menu "Dev quicktest" button to trigger the HugsLib quickstarter.
	/// </summary>
	[HarmonyPatch(typeof(MainMenuDrawer))]
	[HarmonyPatch(nameof(MainMenuDrawer.DoMainMenuControls))]
	internal class MainMenuDrawer_Quickstart_Patch {
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> QuicktestButtonUsesQuickstarter(
			IEnumerable<CodeInstruction> instructions) {
			return new CodeMatcher(instructions)
				.MatchStartForward(new CodeMatch(OpCodes.Ldstr, "DevQuickTest"))
				.MatchStartForward(new CodeMatch(OpCodes.Ldftn))
				.SetOperandAndAdvance(new Action(QuickstartController.InitiateMapGeneration).Method)
				.InstructionEnumeration();
		}
	}
}