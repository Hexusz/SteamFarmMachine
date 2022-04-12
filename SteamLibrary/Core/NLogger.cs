using NLog;

namespace SteamLibrary.Core
{
    public static class NLogger
    {
        public static Logger Log = LogManager.GetCurrentClassLogger();
    }
}