using HugsLib.Commands;
using System;
using System.IO;
using UnityEngine;
using Verse;

namespace HugsLib.Utils {
    /**
     * Platform specific methods
     */
    public static class PlatformUtility {
        public static PlatformID GetCurrentPlatform() {
            // Will need changing if another platform is supported by RimWorld
            if (UnityData.platform == RuntimePlatform.OSXPlayer ||
                    UnityData.platform == RuntimePlatform.OSXEditor ||
                    UnityData.platform == RuntimePlatform.OSXDashboardPlayer)
                return PlatformID.MacOSX;
            else if (UnityData.platform == RuntimePlatform.WindowsPlayer ||
                UnityData.platform == RuntimePlatform.WindowsEditor)
                return PlatformID.Win32NT;
            else if (UnityData.platform == RuntimePlatform.LinuxPlayer)
                return PlatformID.Unix;
            else // Use Win32S as unsupported, should be safe to assume Win 3.1 won't be making a comeback
                return PlatformID.Win32S;
        }

        internal static string GetAppExecutable() {
            string filename = ""; // OSX apps are really just paths, root dir is just apps full name
            if (GetCurrentPlatform() == PlatformID.Win32NT)
                filename = "RimWorldWin.exe";
            else if (GetCurrentPlatform() == PlatformID.Unix)
                filename = "start_RimWorld.sh";
            return Path.Combine(new DirectoryInfo(UnityData.dataPath).Parent.FullName, filename);
        }

        public static void RestartRimWorld() { // should be moved
            HugsLibController.Logger.Message("Restarting RimWorld using HugsLib");
            new CommandRestart().DoCommand();
        }
    }
}