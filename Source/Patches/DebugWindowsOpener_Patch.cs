using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
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
		public static void Prepare() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patched) HugsLibController.Logger.Warning("DebugWindowsOpener_Patch could not be applied.");
			});
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawAdditonalButtons(IEnumerable<CodeInstruction> instructions) {
			var instructionsArr = instructions.ToArray();
			var widgetRowIndex = TryGetLocalIndexOfConstructedObject(instructionsArr, typeof(WidgetRow));
			foreach (var inst in instructionsArr) {
				if (!patched && widgetRowIndex >= 0 && inst.opcode == OpCodes.Bne_Un) {
					yield return new CodeInstruction(OpCodes.Ldloc, widgetRowIndex);
					yield return new CodeInstruction(OpCodes.Call, ((Action<WidgetRow>)QuickstartController.DrawDebugToolbarButton).Method);
					patched = true;
				}
				yield return inst;
			}
		}

		private static int TryGetLocalIndexOfConstructedObject(IEnumerable<CodeInstruction> instructions, Type constructedType, Type[] constructorParams = null) {
			var constructor = AccessTools.Constructor(constructedType, constructorParams);
			int localIndex = -1;
			if (constructor == null) {
				HugsLibController.Logger.Error("Could not reflect constructor for type {0}: {1}", constructedType, Environment.StackTrace);
				return localIndex;
			}
			CodeInstruction prevInstrunction = null;
			foreach (var inst in instructions) {
				if (prevInstrunction != null && prevInstrunction.opcode == OpCodes.Newobj && constructor.Equals(prevInstrunction.operand)) {
					if (inst.opcode == OpCodes.Stloc_0) {
						localIndex = 0;
					} else if (inst.opcode == OpCodes.Stloc_1) {
						localIndex = 1;
					} else if (inst.opcode == OpCodes.Stloc_2) {
						localIndex = 2;
					} else if (inst.opcode == OpCodes.Stloc_3) {
						localIndex = 3;
					} else if (inst.opcode == OpCodes.Stloc && inst.operand is int) {
						localIndex = (int)inst.operand;
					}
					if (localIndex >= 0) break;
				}
				prevInstrunction = inst;
			}
			if (localIndex < 0) {
				HugsLibController.Logger.Error("Could not determine local index for constructed type {0}: {1}", constructedType, Environment.StackTrace);
			}
			return localIndex;
		}
	}
}