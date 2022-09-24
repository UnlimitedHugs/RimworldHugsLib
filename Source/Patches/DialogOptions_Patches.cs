using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Patches {
	[HarmonyPatch(typeof(Dialog_Options))]
	[HarmonyPatch("DoWindowContents")]
	[HarmonyPatch(new[] { typeof(Rect) })]
	internal class DialogOptions_DoWindowContents_Patch {
		private delegate void DrawCategories(Dialog_Options self, ref int buttonIndex);
		private static bool patched;

		[HarmonyCleanup]
		public static void Cleanup() {
			if (!patched) HugsLibController.Logger.Error("Option categories patch could not be applied.");
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> InjectModSettingsButton(IEnumerable<CodeInstruction> instructions) {
			patched = false;

			var modHandlesMethodInfo = typeof(LoadedModManager).GetProperty(nameof(LoadedModManager.ModHandles),
					BindingFlags.Static | BindingFlags.Public)
				?.GetMethod;
			if (modHandlesMethodInfo == null) throw new Exception("Failed to reflect required method");

			// ensure we are using the right local variable
			var instructionsArr = instructions.ToArray();
			const byte currentButtonLocalIndex = 5;
			var currentButtonLocalFound = false;
			for (var i = 0; i < instructionsArr.Length - 1; i++) {
				var next = instructionsArr[i + 1];
				if (instructionsArr[i].opcode == OpCodes.Ldc_I4_0
					&& next.opcode == OpCodes.Stloc_S
					&& next.operand is LocalBuilder lb
					&& lb.LocalType == typeof(int)
					&& lb.LocalIndex == currentButtonLocalIndex) {
					currentButtonLocalFound = true;
					break;
				}
			}
			if (!currentButtonLocalFound) throw new Exception("Failed to find expected local variable");

			foreach (var inst in instructionsArr) {
				yield return inst;
				// insert after our target instruction because it is referenced by a jump
				if (!patched && inst.Is(OpCodes.Call, modHandlesMethodInfo)) {
					// load reference to window (this)
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					// load reference to index of current button
					yield return new CodeInstruction(OpCodes.Ldloca_S, currentButtonLocalIndex);
					// draw settings category buttons
					yield return new CodeInstruction(OpCodes.Call,
						((DrawCategories)OptionsDialogExtensions.DrawHugsLibSettingsButton).Method);
					patched = true;
				}
			}
		}
	}
}