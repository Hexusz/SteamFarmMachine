using System;
using System.Text;

namespace DstDailyFarm
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
