using System;
using System.Diagnostics;

namespace DSTvmFarm
{
    class Program
    {
        static void Main(string[] args)
        {
            Watcher watcher = new Watcher();
            watcher.StartWatch();
        }
    }
}
