using Newtonsoft.Json;
using SteamLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SteamLibrary.Core
{
    public static class SteamFunc
    {
        private static readonly byte[] steamGuardCodeTranslations = new byte[] { 50, 51, 52, 53, 54, 55, 56, 57, 66, 67, 68, 70, 71, 72, 74, 75, 77, 78, 80, 81, 82, 84, 86, 87, 88, 89 };

        private static int _maxRetry = 2;

        public static async Task<bool> Login(Account account, string steamPath)
        {
            ProcessStartInfo stopInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Path.Combine(steamPath, "steam.exe"),
                WorkingDirectory = steamPath,
                Arguments = "-shutdown"
            };

            try
            {
                Process steamProc = Process.GetProcessesByName("Steam")[0];
                Process.Start(stopInfo);
                steamProc.WaitForExit();
            }
            catch (Exception ex)
            {
                NLogger.Log.Warn("Не удалось закрыть Steam " + ex.Message);
            }

            StringBuilder parametersBuilder = new StringBuilder();

            parametersBuilder.Append($" -silent -login {account.SteamGuardAccount.AccountName} {account.Password}");


            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Path.Combine(steamPath, "steam.exe"),
                WorkingDirectory = steamPath,
                Arguments = parametersBuilder.ToString()
            };

            try
            {
                Process steamProcess = Process.Start(startInfo);
                NLogger.Log.Info("Steam запущен");
            }
            catch (Exception ex)
            {
                NLogger.Log.Warn("Не удалось запустить Steam " + ex.Message);
                return false;
            }

            var t = Type2Fa(account, 0);
            var res = await t;
            NLogger.Log.Info($"{(res ? ("Успешная авторизация " + account.SteamGuardAccount.AccountName) : ("Ошибка авторизации " + account.SteamGuardAccount.AccountName))}");

            return res;
        }

        private static async Task<bool> Type2Fa(Account account, int tryCount)
        {
            var steamLoginWindow = SteamUtils.GetSteamLoginWindow();
            var steamGuardWindow = SteamUtils.GetSteamGuardWindow();

            while (!steamLoginWindow.IsValid || !steamGuardWindow.IsValid)
            {
                Thread.Sleep(10);
                steamLoginWindow = SteamUtils.GetSteamLoginWindow();
                steamGuardWindow = SteamUtils.GetSteamGuardWindow();

                var steamWarningWindow = SteamUtils.GetSteamWarningWindow();
                if (steamWarningWindow.IsValid)
                {
                    return false;
                }
            }

            Process steamGuardProcess = SteamUtils.WaitForSteamProcess(steamGuardWindow);
            steamGuardProcess.WaitForInputIdle();

            Thread.Sleep(3000);

            NLogger.Log.Info("Вводим 2FA код");

            SteamUtils.SetForegroundWindow(steamGuardWindow.RawPtr);
            Thread.Sleep(50);

            var code2Fa = GenerateSteamGuardCodeForTime(AppFunc.GetSystemUnixTime(), account.SteamGuardAccount.SharedSecret);
            foreach (char c in code2Fa)
            {
                SteamUtils.SetForegroundWindow(steamGuardWindow.RawPtr);
                Thread.Sleep(50);

                SteamUtils.SendCharacter(steamGuardWindow.RawPtr, VirtualInputMethod.SendMessage, c);
            }

            SteamUtils.SetForegroundWindow(steamGuardWindow.RawPtr);

            Thread.Sleep(10);

            SteamUtils.SendEnter(steamGuardWindow.RawPtr, VirtualInputMethod.SendMessage);

            Thread.Sleep(5000);

            steamGuardWindow = SteamUtils.GetSteamGuardWindow();

            if (tryCount <= _maxRetry && steamGuardWindow.IsValid)
            {
                NLogger.Log.Info("2FA Ошибка кода, повтор");
                var t = Type2Fa(account, tryCount + 1);
                return await t;
            }
            else if (tryCount == _maxRetry + 1 && steamGuardWindow.IsValid)
            {
                NLogger.Log.Error("2FA Ошибка, проверьте данные аккаунта");
                return false;
            }

            return !steamGuardWindow.IsValid;
        }

        public static async Task<bool> SandLogin(Account account, string sandSteamPath, string sandPath)
        {
            StringBuilder parametersBuilder = new StringBuilder();

            parametersBuilder.Append($"/box:{account.SteamGuardAccount.AccountName} {sandSteamPath}\\steam.exe -silent -login {account.SteamGuardAccount.AccountName} {account.Password}");

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
                NLogger.Log.Info("Sand Steam запущен");
            }
            catch (Exception ex)
            {
                NLogger.Log.Warn("Не удалось запустить Sand Steam " + ex.Message);
                return false;
            }

            var t = SandType2Fa(account, 0);
            var res = await t;
            NLogger.Log.Info($"{(res ? ("Успешная авторизация " + account.SteamGuardAccount.AccountName) : ("Ошибка авторизации " + account.SteamGuardAccount.AccountName))}");

            return true;
        }

        private static async Task<bool> SandType2Fa(Account account, int tryCount)
        {
            var steamLoginWindow = SandSteamUtils.GetSandSteamLoginWindow(account.SteamGuardAccount.AccountName);
            var steamGuardWindow = SandSteamUtils.GetSandSteamGuardWindow(account.SteamGuardAccount.AccountName);

            while (!steamLoginWindow.IsValid || !steamGuardWindow.IsValid)
            {
                Thread.Sleep(10);
                steamLoginWindow = SandSteamUtils.GetSandSteamLoginWindow(account.SteamGuardAccount.AccountName);
                steamGuardWindow = SandSteamUtils.GetSandSteamGuardWindow(account.SteamGuardAccount.AccountName);

                var steamWarningWindow = SandSteamUtils.GetSandSteamWarningWindow(account.SteamGuardAccount.AccountName);
                if (steamWarningWindow.IsValid)
                {
                    return false;
                }
            }

            Process steamGuardProcess = SteamUtils.WaitForSteamProcess(steamGuardWindow);
            steamGuardProcess.WaitForInputIdle();

            Thread.Sleep(3000);

            NLogger.Log.Info("Вводим 2FA код");

            SteamUtils.SetForegroundWindow(steamGuardWindow.RawPtr);
            Thread.Sleep(50);

            var code2Fa = GenerateSteamGuardCodeForTime(AppFunc.GetSystemUnixTime(), account.SteamGuardAccount.SharedSecret);
            foreach (char c in code2Fa)
            {
                SteamUtils.SetForegroundWindow(steamGuardWindow.RawPtr);
                Thread.Sleep(50);

                SteamUtils.SendCharacter(steamGuardWindow.RawPtr, VirtualInputMethod.SendMessage, c);
            }

            SteamUtils.SetForegroundWindow(steamGuardWindow.RawPtr);

            Thread.Sleep(10);

            SteamUtils.SendEnter(steamGuardWindow.RawPtr, VirtualInputMethod.SendMessage);

            Thread.Sleep(5000);

            steamGuardWindow = SandSteamUtils.GetSandSteamGuardWindow(account.SteamGuardAccount.AccountName);

            if (tryCount <= _maxRetry && steamGuardWindow.IsValid)
            {
                NLogger.Log.Info("2FA Ошибка кода, повтор");
                var t = Type2Fa(account, tryCount + 1);
                return await t;
            }
            else if (tryCount == _maxRetry + 1 && steamGuardWindow.IsValid)
            {
                NLogger.Log.Error("2FA Ошибка, проверьте данные аккаунта");
                return false;
            }

            return !steamGuardWindow.IsValid;
        }

        public static string GenerateSteamGuardCodeForTime(long time, string sharedSecret)
        {
            if (string.IsNullOrEmpty(sharedSecret))
            {
                return "";
            }

            string sharedSecretUnescaped = Regex.Unescape(sharedSecret);
            byte[] sharedSecretArray = Convert.FromBase64String(sharedSecretUnescaped);
            byte[] timeArray = new byte[8];

            time /= 30L;

            for (int i = 8; i > 0; i--)
            {
                timeArray[i - 1] = (byte)time;
                time >>= 8;
            }

            HMACSHA1 hmacGenerator = new HMACSHA1();
            hmacGenerator.Key = sharedSecretArray;
            byte[] hashedData = hmacGenerator.ComputeHash(timeArray);
            byte[] codeArray = new byte[5];
            try
            {
                byte b = (byte)(hashedData[19] & 0xF);
                int codePoint = (hashedData[b] & 0x7F) << 24 | (hashedData[b + 1] & 0xFF) << 16 | (hashedData[b + 2] & 0xFF) << 8 | (hashedData[b + 3] & 0xFF);

                for (int i = 0; i < 5; ++i)
                {
                    codeArray[i] = steamGuardCodeTranslations[codePoint % steamGuardCodeTranslations.Length];
                    codePoint /= steamGuardCodeTranslations.Length;
                }
            }
            catch (Exception)
            {
                return null; //Change later, catch-alls are bad!
            }
            return Encoding.UTF8.GetString(codeArray);
        }

        public static int GetItemsCount(string steamId)
        {
            try
            {
                string url = $"https://steamcommunity.com/inventory/{steamId}/322330/1";
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                string response;
                using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    response = streamReader.ReadToEnd();
                }
                var totalInventoryCount = JsonConvert.DeserializeObject<TotalInventoryCount>(response);
                return totalInventoryCount.Total_Inventory_Count;
            }
            catch
            {
                return -1;
            }

        }

        public class TotalInventoryCount
        {
            public int Total_Inventory_Count { get; set; }
        }

        public static bool CheckSandIni(List<Account> accounts)
        {
            var readText = new List<string>();
            var copyLineList = new List<string>();
            var defaultBoxLine = false;
            try
            {
                readText = File.ReadAllLines(@"C:\Windows\Sandboxie.ini").ToList();
            }
            catch (Exception ex)
            {
                NLogger.Log.Fatal("Ошибка чтения Sandboxie.ini");
                return false;
            }

            var readTextJoin = string.Join("\n", readText);
            //Если все аккаунты уже есть в ini файле возвращаем true
            if (accounts.TrueForAll(x => readTextJoin.Contains($"[{x.SteamGuardAccount.AccountName}]")))
            {
                return true;
            }

            var pastLine = 0;

            for (int i = 0; i < readText.Count; i++)
            {
                if (readText[i].Contains("[DefaultBox]"))
                {
                    defaultBoxLine = true;
                    continue;
                }
                if (defaultBoxLine && readText[i] == "")
                {
                    defaultBoxLine = false;
                    pastLine = i;
                    break;
                }

                if (defaultBoxLine)
                {
                    copyLineList.Add(readText[i]);
                }
            }

            var pastLineList = new List<string>();

            foreach (var account in accounts)
            {
                if (readTextJoin.Contains($"[{account.SteamGuardAccount.AccountName}]")) { continue; }
                pastLineList.Add("");
                pastLineList.Add($"[{account.SteamGuardAccount.AccountName}]");
                pastLineList.AddRange(copyLineList);
            }

            readText.InsertRange(pastLine, pastLineList);
            NLogger.Log.Warn("Не для всех аккаунтов готовы песочницы");
            NLogger.Log.Warn(@"Замените C:\Windows\Sandboxie.ini на этот код:");
            NLogger.Log.Info("\n" + string.Join("\n", readText));


            return false;
        }
    }
}