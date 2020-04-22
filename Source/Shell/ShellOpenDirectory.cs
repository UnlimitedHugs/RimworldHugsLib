using System;
using System.IO;
using UnityEngine;

namespace HugsLib.Shell {
	/// <summary>
	/// A command to open a directory in the systems default file explorer.
	/// Since Unity's OpenUrl() is broken on OS X, we can use a shell to do it correctly.
	/// </summary>
	public static class ShellOpenDirectory {
		public static bool Execute(string directory) {
			var directoryPath = ParsePath(directory);
			if (string.IsNullOrEmpty(directoryPath)) {
				HugsLibController.Logger.Warning("Attempted to open a directory but none was set.");
				return false;
			}
			if (Utils.PlatformUtility.GetCurrentPlatform() == Utils.PlatformType.MacOSX) {
				return Shell.StartProcess(new Shell.ShellCommand {FileName = "open", Args = directory});
			} else {
				Application.OpenURL(directoryPath);
			}
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
