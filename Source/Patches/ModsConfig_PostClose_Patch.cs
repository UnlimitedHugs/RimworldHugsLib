using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using HugsLib.Core;
using RimWorld;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Restarts the game automatically, bypassing the message dialog, if changes in the mod configuration have been made and dev mode is on.
	/// Holding Ctrl will prevent the restart, while allowing the config changes to still be saved.
	/// </summary>
	[HarmonyPatch(typeof(Page_ModsConfig))]
	[HarmonyPatch("PostClose")]
	internal static class ModsConfig_PostClose_Patch {
		private static bool patched;

		[HarmonyPrepare]
		public static void Prepare() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patched) HugsLibController.Logger.Warning("ModsConfig_PostClose_Patch could not be applied.");
			});
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> QuickRestartInDevMode(IEnumerable<CodeInstruction> instructions) {
			var originalMethod = AccessTools.Method(typeof(WindowStack), "Add", new[] {typeof(Window)});
			foreach (var inst in instructions) {
				if (!patched && originalMethod!=null && inst.opcode == OpCodes.Callvirt && originalMethod.Equals(inst.operand)) {
					yield return new CodeInstruction(OpCodes.Call, ((Action<WindowStack, Window>) TryQuickRestart).Method);
					patched = true;
				} else {
					yield return inst;
				}
			}
		}

		private static void TryQuickRestart(WindowStack stack, Window messageWindow) {
			QuickRestarter.BypassOrShowDialog(messageWindow);
		}
	}
}