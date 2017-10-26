using HugsLib.Utils;
using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Checks for Dev mode and bypasses the Restart message box.
	/// Holding Shift will prevent the automatic restart.
	/// </summary>
	public static class QuickRestarter {
		public static bool ShowRestartDialogOutsideDevMode() {
			if (Prefs.DevMode) {
				if (!HugsLibUtility.ShiftIsHeld) {
					GenCommandLine.Restart();
				}
				return false;
			}
			return true;
		}
	}
}