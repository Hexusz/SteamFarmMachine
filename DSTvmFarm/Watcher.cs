using System;
using DSTvmFarm.Core;
using DSTvmFarm.Entities;
using NLog;

namespace DSTvmFarm
{
    public class Watcher
    {
        public AppConfig MainConfig { get; set; }
        
        public void StartWatch()
        {
            NLogger.Log.Info("Запуск приложения");
            MainConfig = AppFunc.LoadConfig().Result;

            if (SteamFunc.SteamIsRunning())
            {
                NLogger.Log.Info("debug message");
                Console.ReadLine();
            }
            else
            {
                NLogger.Log.Info("Steam не запущен, заывфф");
                SteamFunc.SteamStart();
            }
        }
    }
}