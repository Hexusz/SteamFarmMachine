using System;
using System.Diagnostics;

namespace DSTvmFarm
{
    class Program
    {
        public static Watcher watcher = new Watcher();

        static void Main(string[] args)
        {
            watcher.StartWatch();
        }
    }
}
