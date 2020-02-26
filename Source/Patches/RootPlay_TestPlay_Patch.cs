using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Quickstart;
using RimWorld;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds an entry point during map quickstart for the quickstarter system.
	/// Will replace the standard scenario and map size if the quickstarter is enabled.
	/// </summary>
	[HarmonyPatch(typeof(Root_Play))]
	[HarmonyPatch("SetupForQuickTestPlay")]
	internal class RootPlay_TestPlay_Patch {
		private static bool patchedScenario;
		private static bool patchedSize;

		[HarmonyPrepare]
		public static bool Prepare() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patchedScenario || !patchedSize) HugsLibController.Logger.Error("RootPlay_TestPlay_Patch was partial or unsuccessful: {0}, {1}", patchedScenario, patchedSize);
			});
			return true;
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> InjectCustomQuickstartSettings(IEnumerable<CodeInstruction> instructions) {
			patchedScenario = patchedSize = false;
			var gameSetScenarioMethod = AccessTools.Method(typeof(Game), "set_Scenario");
			var gameInitDataMapSizeField = AccessTools.Field(typeof(GameInitData), "mapSize");
			if (gameSetScenarioMethod == null || gameInitDataMapSizeField == null) {
				HugsLibController.Logger.Warning("Failed to reflect a required member: " + Environment.StackTrace);
			}
			foreach (var inst in instructions) {
				if (inst.opcode == OpCodes.Callvirt && Equals(inst.operand, gameSetScenarioMethod)) {
					yield return new CodeInstruction(OpCodes.Call, ((Func<Scenario, Scenario>) QuickstartController.ReplaceQuickstartScenarioIfNeeded).Method);
					patchedScenario = true;
				} else if (inst.opcode == OpCodes.Stfld && Equals(inst.operand, gameInitDataMapSizeField)) {
					yield return new CodeInstruction(OpCodes.Call, ((Func<int, int>) QuickstartController.ReplaceQuickstartMapSizeIfNeeded).Method);
					patchedSize = true;
				}
				yield return inst;
			}
		}
	}
}