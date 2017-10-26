using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Hooks into the flow of the vanilla MonoBehavior.OnGUI()
	/// This allows to take advantage of automatic UI scaling and prevents GUI updates during a loading screen.
	/// </summary>
	[HarmonyPatch(typeof(UIRoot))]
	[HarmonyPatch("UIRootOnGUI")]
	[HarmonyPatch(new Type[0])]
	internal static class UIRoot_Patch {
		[HarmonyPostfix]
		private static void OnGUIHook() {
			HugsLibController.Instance.OnGUI();
		}
	}
}