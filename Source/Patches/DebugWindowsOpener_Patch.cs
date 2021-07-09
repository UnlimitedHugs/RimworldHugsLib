using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Quickstart;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Adds an entry point to draw and additional debug button on the toolbar.
	/// The infix is necessary to catch the WidgetRow that the stock buttons are drawn to.
	/// </summary>
	[HarmonyPatch(typeof(DebugWindowsOpener))]
	[HarmonyPatch("DrawButtons")]
	internal class DebugWindowsOpener_Patch {
		private static bool patched;

		[HarmonyPrepare]
		public static bool Prepare() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patched) HugsLibController.Logger.Warning("DebugWindowsOpener_Patch could not be applied.");
			});
			return true;
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawAdditionalButtons(IEnumerable<CodeInstruction> instructions) {

			patched = false;
			var instructionsArr = instructions.ToArray();
			var widgetRowField = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRow");
			var programStateProp = AccessTools.PropertyGetter(typeof(Current), "ProgramState");

            for (int i = 0; i < instructionsArr.Length; i++)
            {
				CodeInstruction inst = instructionsArr[i];
				if (!patched && widgetRowField != null && i == 0 )
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, widgetRowField);
					yield return new CodeInstruction(OpCodes.Call, ((Action<WidgetRow>)QuickstartController.DrawDebugToolbarButton).Method);
					patched = true;
				}
				yield return inst;

			}
		}
	}

	/// <summary>
	/// Extends the width of the immediate window the dev toolbar buttons are drawn to to accommodate an additional button
	/// </summary>
	[HarmonyPatch(typeof(DebugWindowsOpener))]
	[HarmonyPatch("DevToolStarterOnGUI")]
	internal class DevToolStarterOnGUI_Patch {
		private static bool patched;

		[HarmonyPrepare]
		public static bool Prepare() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patched) HugsLibController.Logger.Error("DevToolStarterOnGUI_Patch could not be applied.");
			});
			return true;
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> ExtendButtonsWindow(IEnumerable<CodeInstruction> instructions) {
			patched = false;
			foreach (var inst in instructions) {
				if (!patched && inst.opcode == OpCodes.Ldc_R4 && 28f.Equals(inst.operand)) {
					// add one to the number of expected buttons
					yield return new CodeInstruction(OpCodes.Ldc_R4, 1f);
					yield return new CodeInstruction(OpCodes.Add);
					patched = true;
				}
				yield return inst;
			}
		}
	}
}