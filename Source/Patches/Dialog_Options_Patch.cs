using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Replaces the "Mod Settings" button in the Options dialog with our own.
	/// </summary>
	[HarmonyPatch(typeof(Dialog_Options))]
	[HarmonyPatch("DoWindowContents")]
	[HarmonyPatch(new[] { typeof(Rect) })]
	internal static class Dialog_Options_Patch {
		private delegate bool ModSettingsButtonReplacementMethod(Listing_Standard _this, string label, string highlightTag);

		private const string ButtonLabelToKill = "ModSettings";
		//private static readonly MethodInfo ExpectedButtonMethod = Traverse.Create<Listing_Standard>().Method("ButtonText", new[] {typeof(Listing_Standard), typeof (string), typeof (string)}).GetValue<MethodInfo>();

		private static bool patched;

		[HarmonyPrepare]
		public static void Prepare() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patched) HugsLibController.Logger.Error("Dialog_Options_Patch could not be applied.");
			});
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> ReplaceModOptionsButton(this IEnumerable<CodeInstruction> instructions) {
			patched = false;
			var expectedButtonMethod = AccessTools.Method(typeof (Listing_Standard), "ButtonText", new[] {typeof (string), typeof (string)});
			if (expectedButtonMethod == null || expectedButtonMethod.ReturnType != typeof(bool)) {
				HugsLibController.Logger.Error("Failed to reflect required method for transpiler: "+Environment.StackTrace);
			}
			var labelFound = false;
			foreach (var instruction in instructions) {
				if (expectedButtonMethod != null && !patched) {
					// find the right button by its untranslated label
					if (instruction.opcode == OpCodes.Ldstr && instruction.operand as string == ButtonLabelToKill) {
						labelFound = true;
					} else if (labelFound && instruction.opcode == OpCodes.Callvirt && expectedButtonMethod.Equals(instruction.operand)) {
						// replace the button call with our own method
						instruction.operand = ((Func<Listing_Standard, string, string, bool>)DrawReplacementButton).Method;
						patched = true;
					}
				}

				yield return instruction;
			}
		}

		private static bool DrawReplacementButton(Listing_Standard _this, string label, string highlightTag) {
			OptionsDialogInjection.DrawModSettingsButton(_this);
			return false;
		}
	}
}