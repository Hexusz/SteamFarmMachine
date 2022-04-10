using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSTvmFarm.Entities;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace DSTvmFarm.Core
{
    public static class SteamFunc
    {
        private static byte[] steamGuardCodeTranslations = new byte[] { 50, 51, 52, 53, 54, 55, 56, 57, 66, 67, 68, 70, 71, 72, 74, 75, 77, 78, 80, 81, 82, 84, 86, 87, 88, 89 };

        private static int maxRetry = 2;

        public static async Task<bool> Login(int index)
        {

            ProcessStartInfo stopInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Path.Combine(Program.watcher.MainConfig.SteamPath, "steam.exe"),
                WorkingDirectory = Program.watcher.MainConfig.SteamPath,
                Arguments = "-shutdown"
            };

            try
            {
                Process SteamProc = Process.GetProcessesByName("Steam")[0];
                try
                {
                    Process.Start(stopInfo);
                    SteamProc.WaitForExit();
                }
                catch (Exception ex)
                {
                    NLogger.Log.Warn("Не удалось закрыть Steam " + ex.Message);
                }
            }
            catch
            {
            }

            StringBuilder parametersBuilder = new StringBuilder();

            parametersBuilder.Append($" -silent -login {Program.watcher.Accounts[index].Name} {Program.watcher.Accounts[index].Password}");


            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Path.Combine(Program.watcher.MainConfig.SteamPath, "steam.exe"),
                WorkingDirectory = Program.watcher.MainConfig.SteamPath,
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

            var t = Type2Fa(index, 0);
            var res = await t;
            NLogger.Log.Info($"{(res ? ("Успешная авторизация " + Program.watcher.Accounts[index].Name) : ("Ошибка авторизации " + Program.watcher.Accounts[index].Name))}");

            return res;
        }

        private static async Task<bool> Type2Fa(int index, int tryCount)
        {
            var steamLoginWindow = Utils.GetSteamLoginWindow();
            var steamGuardWindow = Utils.GetSteamGuardWindow();

            while (!steamLoginWindow.IsValid || !steamGuardWindow.IsValid)
            {
                Thread.Sleep(10);
                steamLoginWindow = Utils.GetSteamLoginWindow();
                steamGuardWindow = Utils.GetSteamGuardWindow();

                var steamWarningWindow = Utils.GetSteamWarningWindow();
                if (steamWarningWindow.IsValid)
                {
                    return false;
                }
            }

            Process steamGuardProcess = Utils.WaitForSteamProcess(steamGuardWindow);
            steamGuardProcess.WaitForInputIdle();

            Thread.Sleep(3000);

            NLogger.Log.Info("Вводим 2FA код");

            Utils.SetForegroundWindow(steamGuardWindow.RawPtr);
            Thread.Sleep(10);

            var code2Fa = GenerateSteamGuardCodeForTime(AppFunc.GetSystemUnixTime(), Program.watcher.Accounts[index].SharedSecret);
            foreach (char c in code2Fa)
            {
                Utils.SetForegroundWindow(steamGuardWindow.RawPtr);
                Thread.Sleep(10);

                Utils.SendCharacter(steamGuardWindow.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod, c);
            }

            Utils.SetForegroundWindow(steamGuardWindow.RawPtr);

            Thread.Sleep(10);

            Utils.SendEnter(steamGuardWindow.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);

            Thread.Sleep(5000);

            steamGuardWindow = Utils.GetSteamGuardWindow();

            if (tryCount <= maxRetry && steamGuardWindow.IsValid)
            {
                NLogger.Log.Info("2FA Ошибка кода, повтор");
                var t = Type2Fa(index, tryCount + 1);
                return await t;
            }
            else if (tryCount == maxRetry + 1 && steamGuardWindow.IsValid)
            {
                NLogger.Log.Error("2FA Ошибка, проверьте данные аккаунта");
                return false;
            }

            return !steamGuardWindow.IsValid;
        }

        public static string GenerateSteamGuardCodeForTime(long time, string sharedSecret)
        {
            if (sharedSecret == null || sharedSecret.Length == 0)
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
    }
}