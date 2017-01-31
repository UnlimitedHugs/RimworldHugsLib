using HugsLib.Utils;
using System.IO;
using Verse;

namespace HugsLib.Shell {
    /**
     * A Command to open the log file in the systems default text editor.
     *
     * See Shell.cs for more info on Commands.
     */
    public static class ShellOpenLog {
        public static bool Execute() {
            string logfile;
            if (GenCommandLine.TryGetCommandLineArg("logfile", out logfile) && logfile.NullOrEmpty())
                return Shell.StartProcess(new Shell.ShellCommand { FileName = logfile });
            var platform = PlatformUtility.GetCurrentPlatform();
            switch (platform)
            {
                case PlatformType.Linux:
                    return Shell.StartProcess(new Shell.ShellCommand { FileName = @"/tmp/rimworld_log" });
                case PlatformType.MacOSX:
					return Shell.StartProcess(new Shell.ShellCommand { FileName = "open", Args = "~/Library/Logs/Unity/Player.log" });
                case PlatformType.Windows:
					return Shell.StartProcess(new Shell.ShellCommand { FileName = Path.Combine(UnityData.dataPath, "output_log.txt") });
                default:
					HugsLibController.Logger.ReportException(new Shell.UnsupportedPlatformException("ShellOpenLog"));
                    return false;
            }
        }
    }
}
