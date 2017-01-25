using HugsLib.Utils;
using Verse.Steam;
using Verse;

namespace HugsLib.Commands {
    /**
     * A Command to cleanly restart RimWorld on the target machine.
     * See Command.cs for more info on Commands.
     */
    class CommandRestart : Command {
        public CommandRestart() {
            if (SteamManager.Initialized)
                WinCommand.Args = "/c start steam://rungameid/294100";
            else
                WinCommand.Args = string.Format("/c \"{0}\"", PlatformUtility.GetAppExecutable());

            if (SteamManager.Initialized)
                UnixCommand.Args = "steam steam://rungameid/294100";
            else
                UnixCommand.Args = string.Format("\"{0}\"", PlatformUtility.GetAppExecutable());

            if (SteamManager.Initialized)
                OSXCommand.Args = "steam://rungameid/294100";
            else // -n tag for new instance
                OSXCommand.Args = string.Format("-n -a \"{0}\"", PlatformUtility.GetAppExecutable());
        }

        public override void DoCommand() {
            base.DoCommand();
            Root.Shutdown();
        }
    }
}