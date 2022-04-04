using System;
using System.Diagnostics;
using System.Text;

namespace DSTvmFarm
{
    class Program
    {
        public static Watcher watcher = new Watcher();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            watcher.StartWatch();
            Console.ReadKey();
        }
    }
}
