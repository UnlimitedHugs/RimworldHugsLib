using HarmonyLib;
using HugsLib.Core;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Restarts the game automatically, bypassing the message dialog, if changes in the mod configuration have been made and dev mode is on.
	/// Holding Shift will prevent the restart, while allowing the config changes to still be saved.
	/// </summary>
	[HarmonyPatch(typeof(ModsConfig))]
	[HarmonyPatch("RestartFromChangedMods")]
	internal static class ModsConfig_RestartFromChangedMods_Patch {
		[HarmonyPrefix]
		private static bool QuickRestartInDevMode() {
			return QuickRestarter.ShowRestartDialogOutsideDevMode();
		}
	}
}