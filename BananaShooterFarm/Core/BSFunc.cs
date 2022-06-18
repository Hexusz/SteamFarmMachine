using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
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
using Win32Interop.WinHandles;

namespace BananaShooterFarm.Core
{
    public class BSFunc
    {
        public static List<Account> Accounts { get; set; }
        public static AppConfig AppConfig;
        public static Dictionary<string, int> accItems = new Dictionary<string, int>();
        public static ObservableCollection<AccountStats> accountStatses = new ObservableCollection<AccountStats>();

        public static int NoneCount { get; set; }

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

                for (int i = 0; i < 5; i++)
                {
                    Process[] processlist = Process.GetProcesses();
                    foreach (Process process in processlist)
                    {
                        if (process.MainWindowTitle.Contains(account.SteamGuardAccount.AccountName) && process.MainWindowTitle.Contains("Banana Shooter"))
                        {
                            return process;
                        }
                    }
                    await Task.Delay(2000);
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

        public static void RefreshPIDs()
        {
            foreach (var account in accountStatses)
            {
                account.PID = GetAccountPID(account);
            }
        }

        public static async Task RefreshAllAccount()
        {
            RefreshPIDs();

            foreach (var accountStatse in accountStatses)
            {
                await Task.Delay(5000);
                try
                {
                    Process.GetProcessById(accountStatse.PID).Kill();
                    accountStatse.Status = AccountStatus.Wait;
                }
                catch (Exception e)
                {
                    NLogger.Log.Error("Ошибка закрытия процесса");
                }
            }

            await Task.Delay(10000);
        }

        public static int GetAccountPID(AccountStats account)
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

            return accProc;
        }

        public static WindowHandle GetSteamPID(AccountStats account)
        {
            return TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Contains(account.Account) &&
                wh.GetWindowText().Contains("Steam"));
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
                        throw new Exception("PID аккаунта не верный");
                    }
                }
                catch (Exception e)
                {
                    await Task.Delay(2000);

                    await LaunchGame(Accounts.First(x => x.SteamGuardAccount.AccountName == account.Account));
                }

            }

            return true;
        }

        public static async Task<bool> SettingAccounts(ObservableCollection<AccountStats> accStats, string master)
        {
            //Ищем мастер аккаунт
            var masterAcc = accStats.FirstOrDefault(x => x.Account == master);

            if (masterAcc.Status == AccountStatus.Launched)
            {
                await SettingMasterAccount(masterAcc.PID);

                lock (masterAcc)
                {
                    masterAcc.Status = AccountStatus.InGame;
                }
            }

            //Настраиваем все не мастер аккаунты
            foreach (var account in accStats)
            {
                if (account.Account == masterAcc.Account) { continue; }

                if (account.Status == AccountStatus.Launched)
                {
                    await SettingStandardAccount(account.PID);

                    lock (account)
                    {
                        account.Status = AccountStatus.InGame;
                    }
                }
            }

            return true;
        }

        public static async Task<bool> CheckStateAccounts(ObservableCollection<AccountStats> accStats, string master)
        {
            await Task.Delay(2000);

            foreach (var account in accStats)
            {
                await Task.Delay(1000);

                Process accountProcess = new Process();
                var handler = IntPtr.Zero;

                try
                {
                    accountProcess = Process.GetProcessById(account.PID);
                }
                catch (Exception e)
                {
                    NLogger.Log.Error("Не удалось получить процесс");
                    return false;
                }

                handler = accountProcess.MainWindowHandle;

                var playStatus = await CheckPlayStatus(account.PID);

                SteamUtils.SetForegroundWindow(handler);

                await Task.Delay(2000);

                //SteamUtils.ReturnFocus(handler);

                switch (playStatus)
                {
                    case PlayStatus.PlayGame:
                        BSFunc.NoneCount = 0;
                        account.Status = AccountStatus.PlayGame;
                        await Task.Delay(3000);
                        continue;

                    case PlayStatus.PauseInGame:
                        SteamUtils.SetForegroundWindow(handler);
                        var rect = new SteamUtils.Rect();
                        SteamUtils.GetWindowRect(handler, ref rect);
                        await Task.Delay(1000);
                        SteamUtils.LeftMouseClickSlow((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 2), (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 46));
                        await Task.Delay(1000);
                        playStatus = await CheckPlayStatus(account.PID);

                        if (playStatus == PlayStatus.PlayGame)
                        {
                            goto case PlayStatus.PlayGame;
                        }

                        if (playStatus == PlayStatus.ReadyToPlay)
                        {
                            goto case PlayStatus.ReadyToPlay;
                        }

                        if (playStatus == PlayStatus.NotReady)
                        {
                            goto case PlayStatus.NotReady;
                        }
                        break;

                    case PlayStatus.ReadyToPlay:
                        account.Status = AccountStatus.ReadyToPlay;
                        continue;

                    case PlayStatus.NotReady:
                        SteamUtils.SetForegroundWindow(handler);
                        await Task.Delay(500);
                        SteamUtils.SendE(handler, VirtualInputMethod.PostMessage);
                        await Task.Delay(500);
                        playStatus = await CheckPlayStatus(account.PID);
                        if (playStatus == PlayStatus.ReadyToPlay)
                        {
                            account.Status = AccountStatus.ReadyToPlay;
                            continue;
                        }
                        break;

                    case PlayStatus.None:
                        await Task.Delay(10000);
                        BSFunc.NoneCount++;
                        SteamUtils.SetForegroundWindow(handler);
                        await Task.Delay(500);
                        playStatus = await CheckPlayStatus(account.PID);

                        if (playStatus == PlayStatus.ReadyToPlay)
                        {
                            goto case PlayStatus.ReadyToPlay;
                        }

                        if (playStatus == PlayStatus.PauseInGame)
                        {
                            goto case PlayStatus.PauseInGame;
                        }

                        if (playStatus == PlayStatus.NotReady)
                        {
                            goto case PlayStatus.NotReady;
                        }
                        break;

                    case PlayStatus.Error:

                        break;
                }


            }

            return true;
        }

        public static async Task<PlayStatus> CheckPlayStatus(int pid)
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
                return PlayStatus.Error;
            }

            handler = accountProcess.MainWindowHandle;


            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(2000);
            var rect = new SteamUtils.Rect();
            SteamUtils.GetWindowRect(handler, ref rect);
            await Task.Delay(1000);

            var yellowFinded = false;
            var blueFinded = false;

            for (int i = 0; i < 20; i++)
            {
                var pixelColor = SteamUtils.GetPixelColor(rect.Right - 40 - i, rect.Top + 47);

                if (pixelColor == Color.FromArgb(255, 255, 242, 0))
                {
                    yellowFinded = true;
                }
                else if (pixelColor == Color.FromArgb(255, 0, 104, 152))
                {
                    blueFinded = true;
                }
            }

            if (yellowFinded && blueFinded)
            {
                return PlayStatus.PauseInGame;
            }

            for (int i = 0; i < 20; i++)
            {
                var pixelColor = SteamUtils.GetPixelColor((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 1.78) + i, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 91));
                if (pixelColor == Color.FromArgb(255, 0, 255, 0))
                {
                    return PlayStatus.ReadyToPlay;
                }

                if (pixelColor == Color.FromArgb(255, 255, 0, 0))
                {
                    return PlayStatus.NotReady;
                }
            }

            if (SteamUtils.GetPixelColor(rect.Right - 33, rect.Bottom - 33) == Color.FromArgb(255, 255, 255, 255))
            {
                return PlayStatus.PlayGame;
            }

            return PlayStatus.None;
        }

        public static async Task<bool> SettingMasterAccount(int pid)
        {
            await Task.Delay(10000);

            Process masterProcess = new Process();
            var handler = IntPtr.Zero;

            try
            {
                masterProcess = Process.GetProcessById(pid);

                if (!masterProcess.Responding)
                {
                    await Task.Delay(10000);
                }

                handler = masterProcess.MainWindowHandle;

                SteamUtils.SetForegroundWindow(handler);
                await Task.Delay(2000);
                //SteamUtils.ReturnFocus(handler);
                await Task.Delay(2000);
                var rect = new SteamUtils.Rect();
                SteamUtils.GetWindowRect(handler, ref rect);
                await Task.Delay(2000);
                SteamUtils.LeftMouseClickSlow((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 1.1), (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 20));
                SteamUtils.SetForegroundWindow(handler);
                await Task.Delay(2000);
                SteamUtils.LeftMouseClickSlow((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 1.37),
                    (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 43));
                SteamUtils.SetForegroundWindow(handler);
                await Task.Delay(2000);
                SteamUtils.LeftMouseClickSlow((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 1.14),
                    (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 95));
                SteamUtils.SetForegroundWindow(handler);
                await Task.Delay(2000);
                SteamUtils.LeftMouseClickSlow(rect.Right - Math.Abs(rect.Left - rect.Right) / 2 + 100, (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 96));
                SteamUtils.SetForegroundWindow(handler);
                await Task.Delay(7000);
                SteamUtils.SendEsc(handler, VirtualInputMethod.SendMessage);
                await Task.Delay(2000);
                SteamUtils.LeftMouseClickSlow(rect.Right - 40, rect.Top + 47);
                await Task.Delay(2000);
                SteamUtils.LeftMouseClickSlow((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 2), (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 46));
                await Task.Delay(2000);

                return true;
            }
            catch (Exception e)
            {
                NLogger.Log.Error("Не удалось получить процесс мастера");
                return false;
            }
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
                NLogger.Log.Error("Не удалось получить процесс мастера " + e.Message);
                return false;
            }

            handler = accountProcess.MainWindowHandle;

            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(2000);
            //SteamUtils.ReturnFocus(handler);
            await Task.Delay(2000);
            var rect = new SteamUtils.Rect();
            SteamUtils.GetWindowRect(handler, ref rect);
            await Task.Delay(3000);
            SteamUtils.LeftMouseClickSlow((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 1.3), (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 20));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(3000);
            SteamUtils.LeftMouseClickSlow((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 7), (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 9));
            SteamUtils.SetForegroundWindow(handler);
            await Task.Delay(2000);
            SteamUtils.SendCtrlhotKey('V');
            await Task.Delay(2000);
            SteamUtils.LeftMouseClickSlow((int)(rect.Right - Math.Abs(rect.Left - rect.Right) / 20), (int)(rect.Top + Math.Abs(rect.Bottom - rect.Top) / 100 * 9));
            await Task.Delay(2000);

            return true;
        }

        public static async Task LaunchGame(Account account)
        {
            if (!accItems.ContainsKey(account.SteamGuardAccount.AccountName))
                accItems.Add(account.SteamGuardAccount.AccountName, SteamFunc.GetItemsCount(account.SteamGuardAccount.Session.SteamID.ToString(), "1949740", "2"));

            var currentAcc = accountStatses.FirstOrDefault(x => x.Account == account.SteamGuardAccount.AccountName);

            await Task.Delay(1000);

            if (GetAccountPID(currentAcc) == -1)
            {
                currentAcc.Status = AccountStatus.Launching;

                await Task.Delay(1000);

                if (!GetSteamPID(currentAcc).IsValid)
                {
                    var steamLogin = await SteamFunc.SandLogin(account, AppConfig.SteamPath, AppConfig.SandBoxiePath);

                    if (steamLogin)
                    {
                        var proc = await BSStart(account, AppConfig.SteamPath, AppConfig.SandBoxiePath);
                        await Task.Delay(5000);

                        currentAcc.PID = proc.Id;
                        currentAcc.Status = AccountStatus.Launched;
                    }
                    else
                    {
                        currentAcc.Status = AccountStatus.Error;
                    }
                }
                else
                {
                    await Task.Delay(1000);

                    var proc = await BSStart(account, AppConfig.SteamPath, AppConfig.SandBoxiePath);

                    currentAcc.PID = proc.Id;
                    currentAcc.Status = AccountStatus.Launched;
                }
            }
            else
            {
                currentAcc.PID = GetAccountPID(currentAcc);
                currentAcc.Status = AccountStatus.Launched;
            }
        }
    }
}