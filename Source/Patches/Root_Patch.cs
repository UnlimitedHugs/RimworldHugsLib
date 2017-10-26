using System;
using Harmony;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Hooks into the flow of the vanilla MonoBehavior.Update()
	/// </summary>
	[HarmonyPatch(typeof(Root))]
	[HarmonyPatch("Update")]
	[HarmonyPatch(new Type[0])]
	internal static class Root_Patch {
		[HarmonyPostfix]
		private static void UpdateHook() {
			HugsLibController.Instance.OnUpdate();
		}
	}
}