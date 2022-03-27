using System;
using DSTvmFarm.Core;
using DSTvmFarm.Entities;

namespace DSTvmFarm
{
    public class Watcher
    {
        public AppConfig MainConfig { get; set; }

        public void StartWatch()
        {
            MainConfig = AppFunc.LoadConfig().Result;

            if (SteamFunc.SteamIsRunning())
            {
                Console.WriteLine("asdasdasd");
                Console.ReadLine();
            }
            else
            {
                SteamFunc.SteamStart();
            }
        }
    }
}