using System;
using System.IO;
using UnityEngine;

namespace HugsLib.Shell {
    /**
     * A command to open a directory in the systems defualt file explorer
     *
     * Since Unity's OpenUrl() is broken on OS X, we can use a shell to do it correctly
     *
     * See Shell.cs for more info on Commands.
     */
    public class ShellOpenDirectory : Shell {
        private string DirectoryPath { get; set; }

        public ShellOpenDirectory(string Directory) : base(Directory, false) {
            this.DirectoryPath = ParsePath(Directory);
            DoCommand();
        }

        public override bool DoCommand() {
            if(string.IsNullOrEmpty(DirectoryPath)) {
                HugsLibController.Logger.Warning("Attempted to open a directory but none was set.");
                return false;
            }
            HugsLibController.Logger.Message(DirectoryPath);
            if (Utils.PlatformUtility.GetCurrentPlatform() == Utils.PlatformType.MacOSX)
                return DoCommand(new ShellCommand { FileName = "open", Args = this.DirectoryPath });
            Application.OpenURL(this.DirectoryPath);
            return false;
        }

        private static string ParsePath(string path) {
            if (path.StartsWith(@"~\") || path.StartsWith(@"~/"))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), path.Remove(0, 2));
            if (!path.StartsWith("\"") && !path.EndsWith("\""))
                return Utils.HugsLibUtility.SurroundWithDoubleQuotes(path);
            return path;
        }
    }
}
