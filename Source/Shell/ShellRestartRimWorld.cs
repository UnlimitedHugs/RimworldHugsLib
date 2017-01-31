using System;
using Verse;

namespace HugsLib.Shell {
    /**
     * A Command to cleanly restart RimWorld on the target machine.
     * 
     * See Shell.cs for more info on Commands.
     */
    public static class ShellRestartRimWorld {
        public static bool Execute() {
            HugsLibController.Logger.Message("Restarting RimWorld");
	        if (Shell.StartProcess(GetParsedArgs())) {
		        Root.Shutdown();
		        return true;
	        }
	        return false;
        }

        private static Shell.ShellCommand GetParsedArgs() {
            var args = Environment.GetCommandLineArgs();
            var command = new Shell.ShellCommand { FileName = args[0] };
            var argsString = string.Empty;

            for (int index = 1; index < args.GetLength(0); ++index) {
                if (index > 1)
                    argsString += " ";
                argsString += "\"" + args[index] + "\"";
            }
            command.Args = argsString;
            return command;
        }
    }
}