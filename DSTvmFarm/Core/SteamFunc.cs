using System;
using System.Diagnostics;
using System.IO;

namespace DSTvmFarm.Core
{
    public static class SteamFunc
    {
        public static bool SteamIsRunning()
        {
            Process[] procs;
            procs = Process.GetProcessesByName("Steam");
            if (procs.Length > 0)
            {
                return true;
            }

            return false;
        }

        public static void SteamStart()
        {
            string target = Program.watcher.MainConfig.SteamPath;
            try
            {
                Process.Start(Path.Combine(target, "steam.exe"));
            }
            catch (Exception ex)
            {
            }
        }
    }
}