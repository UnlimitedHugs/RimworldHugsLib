using HugsLib.Utils;
using System;
using System.Diagnostics;

namespace HugsLib.Commands {
    /**
     * Commands start a new process on the target machine using platform specific commands and args to pass to the process.
     *
     * DoCommand is overridable for any needed extra handling of the process.
     * StartProcess is overridable for any needed extra options passed to the process.
     *
     * Refer to the Microsoft documentation for dotNet 3.5 for more info on a process.
     * https://msdn.microsoft.com/en-us/library/system.diagnostics.process(v=vs.90).aspx
     */
    class Command {
        public static CommandData WinCommand { get; set; }
        public static CommandData UnixCommand { get; set; }
        public static CommandData OSXCommand { get; set; }

        public Command() {
            //common shells
            WinCommand = new CommandData() { Command = "cmd.exe" };
            UnixCommand = new CommandData() { Command = "bash" };
            OSXCommand = new CommandData() { Command = "open" };
        }

        public virtual void DoCommand() {
            PlatformID currentPlatform = PlatformUtility.GetCurrentPlatform();
            switch (currentPlatform) {
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                    StartProcess(WinCommand);
                    break;
                case PlatformID.Unix:
                case (PlatformID)128: // Also Unix
                    StartProcess(UnixCommand);
                    break;
                case PlatformID.MacOSX:
                    StartProcess(OSXCommand);
                    break;
                case PlatformID.Win32S: // Used as Unsupported
                default:
                    HugsLibController.Logger.Warning("{0} is not compatible with {1}", this.GetType().Name, Verse.UnityData.platform);
                    break;
            }
        }

        public virtual Process StartProcess(CommandData commandData) {
            ProcessStartInfo psi = new ProcessStartInfo() {
                UseShellExecute = false, // Needs to be false to work on some platforms
                CreateNoWindow = true,
                FileName = commandData.Command,
                Arguments = commandData.Args,
            };
            return Process.Start(psi);
        }
    }

    public class CommandData {
        public string Command { get; set; }
        public string Args { get; set; }
    }
}
