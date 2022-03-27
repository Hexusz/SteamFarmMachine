using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using DSTvmFarm.Entities;

namespace DSTvmFarm.Core
{
    public static class AppFunc
    {
        public static async Task<AppConfig> LoadConfig()
        {
            var appPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DSTvmFarm.conf");
            if (File.Exists(appPath))
            {
                using (FileStream fs = new FileStream(appPath, FileMode.OpenOrCreate))
                {
                    AppConfig? conf = await JsonSerializer.DeserializeAsync<AppConfig>(fs);
                    return conf;
                }
            }
            else
            {
                using (FileStream fs = new FileStream("DSTvmFarm.conf", FileMode.OpenOrCreate))
                {
                    var defaulConfig = new AppConfig()
                    {
                        SteamPath = @"C:\Program Files (x86)\Steam",
                    };

                    await JsonSerializer.SerializeAsync<AppConfig>(fs, defaulConfig);

                    return defaulConfig;
                }
            }
        }
    }
}