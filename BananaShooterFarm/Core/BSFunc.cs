using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BananaShooterFarm.Entities;
using Newtonsoft.Json;
using SteamLibrary;
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

        public static async Task<bool> SettingAccounts(ObservableCollection<AccountStats> accStats, string master)
        {
            //Ищем мастер аккаунт
            var masterAcc = accStats.FirstOrDefault(x => x.Account == master);

            if(masterAcc.Status == AccountStatus.Launched)
            {
                await SettingMasterAccount(masterAcc.PID);
                masterAcc.Status = AccountStatus.Ready;
            }

            //Настраиваем все не мастер аккаунты
            foreach (var account in accStats)
            {
                if (account.Account == masterAcc.Account) { continue;}

                if (account.Status == AccountStatus.Launched)
                {
                    await SettingStandardAccount(account.PID);
                    account.Status = AccountStatus.Ready;
                }
            }

            return true;
        }

        public static async Task<bool> SettingMasterAccount(int pid)
        {
            Process masterProcess = new Process();
            var handler = IntPtr.Zero;

            try
            {
                masterProcess = Process.GetProcessById(pid);
            }
            catch (Exception e)
            {
                NLogger.Log.Error("Не удалось получить процесс мастера");
                return false;
            }

            handler = masterProcess.MainWindowHandle;

            SteamUtils.SetForegroundWindow(handler);
            var rect = new SteamUtils.Rect();
            SteamUtils.GetWindowRect(handler, ref rect);
            await Task.Delay(4000);
            SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 46));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(2000);
            SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 44));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(2000);
            SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2+33, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 48));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(1000);
            SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2 + 100, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 96));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(10000);
            SteamUtils.SendEsc(handler,VirtualInputMethod.PostMessage);
            await Task.Delay(3000);
            SteamUtils.LeftMouseClickSlow(rect.Right - 40, rect.Top + 47);
            await Task.Delay(1000);

            return true;
        }

        public static async Task<bool> SettingStandardAccount(int pid)
        {
            Process accountProcess = new Process();
            var handler = IntPtr.Zero;

            try
            {
                accountProcess = Process.GetProcessById(pid);
            }
            catch (Exception e)
            {
                NLogger.Log.Error("Не удалось получить процесс мастера");
                return false;
            }

            handler = accountProcess.MainWindowHandle;

            SteamUtils.SetForegroundWindow(handler);
            var rect = new SteamUtils.Rect();
            SteamUtils.GetWindowRect(handler, ref rect);
            await Task.Delay(4000);
            SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 46));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(2000);
            SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 48));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(2000);
            SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2 - 40, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 36));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(1000);
            SteamUtils.SendCtrlhotKey('V');
            await Task.Delay(1000);
            SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2 + 30, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 36));

            return true;
        }
    }
}