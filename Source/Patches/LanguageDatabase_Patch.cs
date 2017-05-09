using Harmony;
using Verse;

namespace HugsLib.Patches {
	/// <summary>
	/// Forces a game restart after a language change.
	/// This is necessary to avoid creating problems for running mods caused by reloaded graphics and defs.
	/// </summary>
	[HarmonyPatch(typeof(LanguageDatabase))]
	[HarmonyPatch("SelectLanguage")]
	[HarmonyPatch(new[] { typeof(LoadedLanguage) })]
	internal static class LanguageDatabase_Patch {
		[HarmonyPrefix]
		public static bool ForceRestartAfterLangChange(LoadedLanguage lang) {
			Prefs.LangFolderName = lang.folderName;
			Prefs.Save();
			Find.WindowStack.Add(new Dialog_MessageBox("HugsLib_restart_language_text".Translate(), null, () => {
				LongEventHandler.ExecuteWhenFinished(GenCommandLine.Restart);
			}));
			return false;
		}
	}
}