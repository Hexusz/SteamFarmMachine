using System;
using DSTvmFarm.Core;

namespace DSTvmFarm
{
    public class Watcher
    {
        public void StartWatch()
        {
            if (SteamFunc.SteamIsRunning())
            {
                Console.WriteLine("asdasdasd");
                Console.ReadLine();
            }
        }
    }
}