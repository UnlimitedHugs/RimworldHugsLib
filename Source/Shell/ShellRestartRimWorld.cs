using System;
using Verse;

namespace HugsLib.Shell {
    /**
     * A Command to cleanly restart RimWorld on the target machine.
     * See Shell.cs for more info on Commands.
     */
    class ShellRestartRimWorld : Shell {
        public override bool DoCommand() {
            HugsLibController.Logger.Message("Restarting RimWorld");
            if (base.DoCommand(GetParsedArgs()))
                Root.Shutdown();
            return false;
        }

        private ShellCommand GetParsedArgs() {
            var args = Environment.GetCommandLineArgs();
            ShellCommand command = new ShellCommand() { FileName = args[0] };
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