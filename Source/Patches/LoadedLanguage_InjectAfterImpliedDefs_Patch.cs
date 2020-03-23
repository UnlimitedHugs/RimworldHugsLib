using HarmonyLib;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Immediately before def translation injection is a good place to inject our custom-loaded 
	/// <see cref="UpdateFeatureDef"/>s as it takes advantage of the vanilla translation mechanism.
	/// </summary>
	[HarmonyPatch(typeof(LoadedLanguage))]
	[HarmonyPatch("InjectIntoData_AfterImpliedDefs")]
	internal static class LoadedLanguage_InjectAfterImpliedDefs_Patch {
		[HarmonyPrefix]
		public static void LoadUpdateFeatureDefs() {
			HugsLibController.Instance.OnBeforeLanguageDataInjected();
		}
	}
}