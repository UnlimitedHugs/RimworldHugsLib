using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using HugsLib.Utils;
using Verse;

namespace HugsLib.Shell {
	/// <summary>
	/// Commands start a new process on the target machine using platform specific commands and args to pass to the shell.
	/// Refer to the Microsoft documentation for dotNet 3.5 for more info on a process.
	/// https://msdn.microsoft.com/en-us/library/system.diagnostics.process(v=vs.90).aspx
	/// </summary>
	public static class Shell {
		public static bool StartProcess(ShellCommand shellCommand) {
			var process = new Process();
			return StartProcess(new ProcessStartInfo {
				CreateNoWindow = true,
				FileName = shellCommand.FileName,
				Arguments = shellCommand.Args
			}, ref process);
		}

		public static bool StartProcess(ProcessStartInfo psi, ref Process process) {
			if (process == null) {
				process = new Process();
			}
			if (psi == null) {
				HugsLibController.Logger.ReportException(new Exception("Could not start process, ProcessStartInfo is empty"));
				return false;
			}
			try {
				psi.FileName.TryReplaceUserDirectory();
				psi.Arguments.TryReplaceUserDirectory();
				process.StartInfo = psi;
				process.Start();
				return true;
			} catch (Win32Exception e) {
				if (psi.FileName.StartsWith("\"") &&
					psi.FileName.EndsWith("\""))
					HugsLibController.Logger.Warning("Detected surround quotes on shell filename, " +
					                                 "some platforms interpret them as part of the filename.");
				HugsLibController.Logger.ReportException(e);
			} catch (Exception e) {
				HugsLibController.Logger.ReportException(e);
			}
			return false;
		}

		[Serializable]
		public class UnsupportedPlatformException : Exception {
			private static string ExpandCommandName(string commandName) {
				return string.Format("{0} is not compatible with {1}", commandName, UnityData.platform);
			}
			public UnsupportedPlatformException(string commandName) : base(ExpandCommandName(commandName)) { }
			public UnsupportedPlatformException(string commandName, Exception inner) : base(ExpandCommandName(commandName), inner) { }
			protected UnsupportedPlatformException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		}

		public class ShellCommand {
			public string FileName { get; set; }
			public string Args { get; set; }
		}
	}
}