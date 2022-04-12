using NLog;

namespace DSTvmFarm.Core
{
    public static class NLogger
    {
        public static Logger Log = LogManager.GetCurrentClassLogger();
    }
}