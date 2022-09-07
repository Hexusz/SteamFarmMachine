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
        public static Dictionary<string, int> AccItems = new Dictionary<string, int>();
        public static List<LastAccountStatus> LastAccountStatus = new List<LastAccountStatus>();
        public static ObservableCollection<AccountStats> AccountStatses = new ObservableCollection<AccountStats>();
        public static string CurrentServerId;

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
            foreach (var account in AccountStatses)
            {
                account.PID = GetAccountPID(account);
            }
        }

        public static async Task RefreshAllAccount()
        {
            RefreshPIDs();

            foreach (var accountStatse in AccountStatses)
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
                accountStatse.LastStatusChange = DateTime.Now;
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

        public static Task<string> GetClipboardAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            var thread = new Thread(() =>
            {
                try
                {
                    var result = Clipboard.GetText();
                    tcs.SetResult(result);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static async Task<bool> SettingAccounts(ObservableCollection<AccountStats> accStats, string master)
        {
            await Task.Delay(5000);

            //Ищем мастер аккаунт
            var masterAcc = accStats.FirstOrDefault(x => x.Account == master);

            if (masterAcc.Status == AccountStatus.Launched)
            {
                await SettingMasterAccount(masterAcc);

                masterAcc.Status = AccountStatus.InGame;
            }

            //Настраиваем все не мастер аккаунты
            foreach (var account in accStats)
            {
                if (account.Account == masterAcc.Account) { continue; }

                if (account.Status == AccountStatus.Launched)
                {
                    var acc = Accounts.FirstOrDefault(x => x.SteamGuardAccount.AccountName == account.Account);

                    await SettingStandardAccount(acc);

                    account.Status = AccountStatus.InGame;
                }
            }

            await Task.Delay(5000);

            return true;
        }

        public static async Task CheckChangeStateAccounts(ObservableCollection<AccountStats> accStats)
        {
            foreach (var account in accStats)
            {
                var last = LastAccountStatus.First(x => x.Account == account.Account);

                if (last.LastStatus != account.Status)
                {
                    account.LastStatusChange = DateTime.Now;
                    last.LastStatus = account.Status;
                }

                if (DateTime.Now - account.LastStatusChange > TimeSpan.FromMinutes(15))
                {
                    NLogger.Log.Warn($"Статус аккаунта {account.Account} завис, перезагружаем аккаунты");
                    await RefreshAllAccount();
                    return;
                }
            }
        }

        public static async Task<bool> CheckStateAccounts(ObservableCollection<AccountStats> accStats, string master)
        {

            foreach (var account in accStats)
            {
                if (CurrentServerId.Length < 1)
                {
                    //Копируем id сервера после настройки мастер аккаунта
                    CurrentServerId = await GetClipboardAsync();
                }

                await Task.Delay(500);

                Process accountProcess;
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

                StringBuilder parametersBuilder = new StringBuilder();

                parametersBuilder.Append($"/box:{account.Account} steam://joinlobby/1949740/{CurrentServerId}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = Path.Combine(AppConfig.SandBoxiePath, "Start.exe"),
                    WorkingDirectory = AppConfig.SandBoxiePath,
                    Arguments = parametersBuilder.ToString()
                };

                try
                {
                    Process steamProcess = Process.Start(startInfo);
                }
                catch { NLogger.Log.Error($"Не удачная попытка входа на сервер для аккаунта {account.Account}"); }

                handler = accountProcess.MainWindowHandle;

                SteamUtils.SetForegroundWindow(handler);

                SteamUtils.MoveWindow(handler, 350, 150, 816, 639, true);

                await Task.Delay(2000);

                var playStatus = await CheckPlayStatus(account.PID);

                switch (playStatus)
                {
                    case PlayStatus.PlayGame:
                        account.Status = AccountStatus.PlayGame;
                        var rect2 = new SteamUtils.Rect();
                        SteamUtils.GetWindowRect(handler, ref rect2);
                        SteamUtils.LeftMouseClickSlow((int)(rect2.Right - Math.Abs(rect2.Left - rect2.Right) / 2), (int)(rect2.Top + Math.Abs(rect2.Bottom - rect2.Top) / 100 * 46));
                        await Task.Delay(1000);
                        continue;

                    case PlayStatus.PauseInGame:
                        var rect = new SteamUtils.Rect();
                        SteamUtils.GetWindowRect(handler, ref rect);
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
                        SteamUtils.SendE(handler, VirtualInputMethod.PostMessage);
                        playStatus = await CheckPlayStatus(account.PID);

                        if (playStatus == PlayStatus.ReadyToPlay)
                        {
                            goto case PlayStatus.ReadyToPlay;
                        }
                        break;

                    case PlayStatus.None:
                        await Task.Delay(10000);
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
                        account.Status = AccountStatus.Error;
                        break;
                }

                var last = LastAccountStatus.First(x => x.Account == account.Account);

                if (last.LastStatus != account.Status)
                {
                    account.LastStatusChange = DateTime.Now;
                    last.LastStatus = account.Status;
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
            await Task.Delay(1000);
            var rect = new SteamUtils.Rect();
            SteamUtils.GetWindowRect(handler, ref rect);

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

        public static async Task<bool> SettingMasterAccount(AccountStats master)
        {
            await Task.Delay(10000);

            Process masterProcess = new Process();
            var handler = IntPtr.Zero;

            try
            {
                masterProcess = Process.GetProcessById(master.PID);

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

                //Если окно не верного размера или свернуто
                if (rect.Bottom <= 0 && rect.Top <= 0 && rect.Left <= 0 && rect.Right <= 0)
                {
                    //Обновляем все PID
                    NLogger.Log.Error($"Ошибка поиска окна мастера, обновляем pid {master.PID}");
                    BSFunc.RefreshPIDs();
                    NLogger.Log.Error($"Новый pid мастер аккаунта: {master.PID}");

                    masterProcess = Process.GetProcessById(master.PID);

                    if (!masterProcess.Responding)
                    {
                        await Task.Delay(10000);
                    }

                    handler = masterProcess.MainWindowHandle;

                    SteamUtils.GetWindowRect(handler, ref rect);
                }

                SteamUtils.SetForegroundWindow(handler);

                SteamUtils.MoveWindow(handler, 350, 150, 800, 600, true);
                SteamUtils.GetWindowRect(handler, ref rect);

                await Task.Delay(2000);
                SteamUtils.LeftMouseClickSlow(rect.Left+65,rect.Top+175);
                await Task.Delay(2000);
                SteamUtils.LeftMouseClickSlow(rect.Left + 217, rect.Top + 242);
                await Task.Delay(1000);
                SteamUtils.LeftMouseClickSlow(rect.Left + 320, rect.Top + 303);
                await Task.Delay(1000);
                SteamUtils.LeftMouseClickSlow(rect.Left + 100, rect.Top + 534);
                await Task.Delay(10000);
                SteamUtils.SendEsc(handler, VirtualInputMethod.SendMessage);
                await Task.Delay(2000);

                SteamUtils.MoveWindow(handler, 350, 150, 800, 600, true);
                SteamUtils.GetWindowRect(handler, ref rect);

                await Task.Delay(1000);
                SteamUtils.LeftMouseClickSlow(rect.Left + 762, rect.Top + 47);
                await Task.Delay(2000);

                //Копируем id сервера после настройки мастер аккаунта
                CurrentServerId = await GetClipboardAsync();

                return true;
            }
            catch (Exception e)
            {
                NLogger.Log.Error("Не удалось получить процесс мастера");
                return false;
            }
        }

        public static async Task<bool> SettingStandardAccount(Account account)
        {
            await Task.Delay(6000);
            StringBuilder parametersBuilder = new StringBuilder();

            parametersBuilder.Append($"/box:{account.SteamGuardAccount.AccountName} steam://joinlobby/1949740/{CurrentServerId}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Path.Combine(AppConfig.SandBoxiePath, "Start.exe"),
                WorkingDirectory = AppConfig.SandBoxiePath,
                Arguments = parametersBuilder.ToString()
            };

            try
            {
                Process steamProcess = Process.Start(startInfo);
            }
            catch { NLogger.Log.Error($"Не удачная попытка входа на сервер для аккаунта {account.SteamGuardAccount.AccountName}"); }

            await Task.Delay(2000);

            return true;
        }

        public static async Task LaunchGame(Account account)
        {
            if (!AccItems.ContainsKey(account.SteamGuardAccount.AccountName))
                AccItems.Add(account.SteamGuardAccount.AccountName, SteamFunc.GetItemsCount(account.SteamGuardAccount.Session.SteamID.ToString(), "1949740", "2"));

            var currentAcc = AccountStatses.FirstOrDefault(x => x.Account == account.SteamGuardAccount.AccountName);

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