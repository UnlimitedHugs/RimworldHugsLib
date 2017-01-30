using System;
using System.Diagnostics;
using System.IO;

namespace HugsLib.Shell {
    /**
     * Commands start a new process on the target machine using platform specific commands and args to pass to the shell.
     *
     * DoCommand is overridable for any needed extra handling of the process.
     *
     * Refer to the Microsoft documentation for dotNet 3.5 for more info on a process.
     * https://msdn.microsoft.com/en-us/library/system.diagnostics.process(v=vs.90).aspx
     */
    public abstract class Shell {
        public abstract bool DoCommand();
        public Shell(params object[] args) { }
        public Shell() { DoCommand(); }

        public bool DoCommand(ShellCommand command = null) {
            if (command != null)
                return StartProcess(command);
            else
                return DoCommand();
        }

        public bool StartProcess(ShellCommand shellCommand) {
            Process process = new Process();
            return StartProcess(new ProcessStartInfo() {
                CreateNoWindow = true,
                FileName = shellCommand.FileName,
                Arguments = shellCommand.Args
            }, ref process);
        }

        public bool StartProcess(ProcessStartInfo psi, ref Process process) {
            if (process == null)
                process = new Process();
            if (psi != null)
                try {
                    Utils.HugsLibUtility.TryReplaceUserDirectory(psi.FileName);
                    Utils.HugsLibUtility.TryReplaceUserDirectory(psi.Arguments);
                    process.StartInfo = psi;
                    process.Start();
                    return true;
                }
                catch (System.ComponentModel.Win32Exception e) {
                    if (psi != null && psi.FileName.StartsWith("\"") &&
                            psi.FileName.EndsWith("\""))
                        HugsLibController.Logger.Warning("Detected surround quotes on shell filename, " +
                            "some platforms interpret them as part of the filename.");
                    HugsLibController.Logger.ReportException(e);
                }
                catch (Exception e) {
                    HugsLibController.Logger.ReportException(e);
                    return false;
                }
            else
                HugsLibController.Logger.Warning("Could not start {0}, ProcessStartInfo is empty!", this.GetType().Name);
            return false;
        }

        [Serializable]
        public class UnsupportedPlatformException : Exception {
            public UnsupportedPlatformException(Type source) { new UnsupportedPlatformException(string.Format("{0} is not compatible with {1}", source.Name, Verse.UnityData.platform)); }
            public UnsupportedPlatformException(string message) : base(message) { }
            public UnsupportedPlatformException(string message, Exception inner) : base(message, inner) { }
            protected UnsupportedPlatformException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }

    public class ShellCommand {
        public string FileName { get; set; }
        public string Args { get; set; }
    }
}