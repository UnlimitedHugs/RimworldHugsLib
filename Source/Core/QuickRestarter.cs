using HugsLib.Utils;
using Verse;

namespace HugsLib.Core {
	/// <summary>
	/// Checks for Dev mode and bypasses the Restart message box.
	/// Holding Ctrl will prevent the automatic restart.
	/// </summary>
	public static class QuickRestarter {
		public static void BypassOrShowDialog(Window originalDialog) {
			if (Prefs.DevMode) {
				if (!HugsLibUtility.ControlIsHeld) {
					GenCommandLine.Restart();
				}
			} else {
				Find.WindowStack.Add(originalDialog);
			}
		}
	}
}