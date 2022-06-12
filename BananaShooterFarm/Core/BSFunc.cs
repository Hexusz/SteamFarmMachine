using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BananaShooterFarm.Entities;
using Newtonsoft.Json;
using SteamLibrary.Core;
using SteamLibrary.Entities;

namespace BananaShooterFarm.Core
{
    public class BSFunc
    {
        public static async Task<Process> BSStart(Account account, string sandSteamPath, string sandPath)
        {
            StringBuilder parametersBuilder = new StringBuilder();

            parametersBuilder.Append($"/box:{account.SteamGuardAccount.AccountName} {sandSteamPath}\\steam.exe -applaunch 1949740");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Path.Combine(sandPath, "Start.exe"),
                WorkingDirectory = sandPath,
                Arguments = parametersBuilder.ToString()
            };

            try
            {
                Process steamProcess = Process.Start(startInfo);
                NLogger.Log.Info("Banana Shooter запущен");

                await Task.Delay(2000);

                Process[] processlist = Process.GetProcesses();
                foreach (Process process in processlist)
                {
                    if (process.MainWindowTitle.Contains(account.SteamGuardAccount.AccountName) && process.MainWindowTitle.Contains("Banana Shooter"))
                    {
                        return process;
                    }
                }

                NLogger.Log.Info("Не нашли процесс Banana Shooter для аккаунта " + account.SteamGuardAccount.AccountName);

                return null;
            }
            catch (Exception ex)
            {
                NLogger.Log.Warn("Не удалось запустить Banana Shooter " + ex.Message);
                return null;
            }
        }

        public static void RefreshPIDs(ObservableCollection<AccountStats> accountStatses)
        {
            foreach (var account in accountStatses)
            {
                var accProc = -1;
                var processlist = Process.GetProcesses();
                foreach (Process process in processlist)
                {
                    if (process.MainWindowTitle.Contains(account.Account) &&
                        process.MainWindowTitle.Contains("Banana Shooter"))
                    {
                        accProc = process.Id;
                    }
                }

                account.PID = accProc;
            }
        }

        public static async Task<AppConfig> LoadConfig()
        {
            var cfgPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "BS.conf");
            if (File.Exists(cfgPath))
            {
                using (StreamReader file = File.OpenText(cfgPath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var conf = (AppConfig)serializer.Deserialize(file, typeof(AppConfig));
                    return conf;
                }
            }
            else
            {
                var defaultConfig = new AppConfig()
                {
                    SteamPath = @"C:\Program Files (x86)\Steam",
                    SandBoxiePath = @"C:\Program Files\Sandboxie-Plus",
                };

                File.WriteAllText(cfgPath, JsonConvert.SerializeObject(defaultConfig));
                NLogger.Log.Info("Загрузка конфига завершена");
                return defaultConfig;
            }
        }

        public static async Task<bool> CheckingAndFixRunningAccounts(ObservableCollection<AccountStats> accStats)
        {
            foreach (var account in accStats)
            {
                try
                {
                    Process process = Process.GetProcessById(account.PID);

                    if (!(process.MainWindowTitle.Contains(account.Account) &&
                          process.MainWindowTitle.Contains("Banana Shooter")))
                    {
                        MessageBox.Show("run " + account.Account);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("error " + account.Account);
                }

            }

            return true;
        }
    }
}