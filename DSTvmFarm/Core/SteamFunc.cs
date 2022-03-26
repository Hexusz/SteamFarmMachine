using System;
using System.Diagnostics;

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
    }
}