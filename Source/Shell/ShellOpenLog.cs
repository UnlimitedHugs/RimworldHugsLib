using HugsLib.Utils;
using System.IO;
using Verse;

namespace HugsLib.Shell {
    /**
     * A Command to cleanly restart RimWorld on the target machine.
     * See Shell.cs for more info on Commands.
     */
    class ShellOpenLog : Shell {
        public override bool DoCommand() {
            string logfile = string.Empty;
            if (GenCommandLine.TryGetCommandLineArg("logfile", out logfile) && logfile.NullOrEmpty())
                return DoCommand(new ShellCommand() { FileName = logfile });
            PlatformType platform = PlatformUtility.GetCurrentPlatform();
            switch (platform)
            {
                case PlatformType.Linux:
                    return DoCommand(new ShellCommand() { FileName = @"/tmp/rimworld_log" });
                case PlatformType.MacOSX:
                    return DoCommand(new ShellCommand() { FileName = "open", Args = "~/Library/Logs/Unity/Player.log" });
                case PlatformType.Windows:
                    return DoCommand(new ShellCommand() { FileName = Path.Combine(UnityData.dataPath, "output_log.txt") });
                default:
                    HugsLibController.Logger.ReportException(new UnsupportedPlatformException(this.GetType()), null, false, this.GetType().Name);
                    return false;
            }
        }
    }
}
