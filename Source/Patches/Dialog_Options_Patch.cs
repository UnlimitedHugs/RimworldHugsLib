using HarmonyLib;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace HugsLib.Patches {
	
	/// <summary>
	/// Replaces the "Mod Settings" button in the Options dialog with our own.
	/// </summary>
	[HarmonyPatch(typeof(Dialog_Options))]
	[HarmonyPatch("DoOptions")]
	[HarmonyPatch(new[] { typeof(OptionCategoryDef), typeof(Rect) })]
	internal static class Dialog_Options_Patch {

		[HarmonyPrefix]
		public static bool ReplaceDoOptions(OptionCategoryDef category, Rect inRect)
		{
			if (category == OptionCategoryDefOf.Mods)
			{
				HugsLibUtility.OpenModSettingsDialog();
				return false;
			}
			
			return true;
		}
	}
}