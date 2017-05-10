using HugsLib.Source.Detour;
using Verse;

namespace HugsLib.Restarter {
	/// <summary>
	/// 
	/// </summary>
	public static class LanguageDatabaseDetour {

		[DetourMethod(typeof (LanguageDatabase), "SelectLanguage")]
		private static void _SelectLanguage(LoadedLanguage lang) {
			Prefs.LangFolderName = lang.folderName;
			Prefs.Save();

			AutoRestarter.AutoRestartOrShowRestartDialog("HugsLib_restart_language_text", () => {
				LongEventHandler.QueueLongEvent(delegate {
					PlayDataLoader.ClearAllPlayData();
					PlayDataLoader.LoadAllPlayData();
				}, "LoadingLongEvent", true, null);
			});
		}
	}
}