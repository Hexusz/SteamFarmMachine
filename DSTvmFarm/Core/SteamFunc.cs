using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSTvmFarm.Entities;
using Microsoft.VisualBasic.CompilerServices;

namespace DSTvmFarm.Core
{
    public static class SteamFunc
    {
        private static byte[] steamGuardCodeTranslations = new byte[] { 50, 51, 52, 53, 54, 55, 56, 57, 66, 67, 68, 70, 71, 72, 74, 75, 77, 78, 80, 81, 82, 84, 86, 87, 88, 89 };

        private static int maxRetry = 2;

        public static void Login(int index, int tryCount)
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
                    NLogger.Log.Warn("Не удалось закрыть Steam");
                }
            }
            catch
            {
            }

            StringBuilder parametersBuilder = new StringBuilder();

            parametersBuilder.Append($" -login {Program.watcher.Accounts[index].Name} {Program.watcher.Accounts[index].Password}");


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
                return;
            }

            Task.Run(() => Type2FA(index, 0));
        }

        private static void Type2FA(int index, int tryCount)
        {
            // Need both the Steam Login and Steam Guard windows.
            // Can't focus the Steam Guard window directly.
            var steamLoginWindow = Utils.GetSteamLoginWindow();
            var steamGuardWindow = Utils.GetSteamGuardWindow();

            while (!steamLoginWindow.IsValid || !steamGuardWindow.IsValid)
            {
                Thread.Sleep(10);
                steamLoginWindow = Utils.GetSteamLoginWindow();
                steamGuardWindow = Utils.GetSteamGuardWindow();

                // Check for Steam warning window.
                var steamWarningWindow = Utils.GetSteamWarningWindow();
                if (steamWarningWindow.IsValid)
                {
                    //Cancel the 2FA process since Steam connection is likely unavailable. 
                    return;
                }
            }

            NLogger.Log.Info("Found window.");

            Process steamGuardProcess = Utils.WaitForSteamProcess(steamGuardWindow);
            steamGuardProcess.WaitForInputIdle();

            // Wait a bit for the window to fully initialize just in case.
            Thread.Sleep(3000);

            // Generate 2FA code, then send it to the client.
            NLogger.Log.Info("It is idle now, typing code...");

            Utils.SetForegroundWindow(steamGuardWindow.RawPtr);
            Thread.Sleep(10);

            var code2fa = GenerateSteamGuardCodeForTime(AppFunc.GetSystemUnixTime(), Program.watcher.Accounts[index].SharedSecret);
            foreach (char c in code2fa)
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

            if (tryCount < maxRetry && steamGuardWindow.IsValid)
            {
                NLogger.Log.Info("2FA code failed, retrying...");
                Type2FA(index, tryCount + 1);
                return;
            }
            else if (tryCount == maxRetry && steamGuardWindow.IsValid)
            {
                NLogger.Log.Error("2FA Failed Please wait or bring the Steam Guard");
                Type2FA(index, tryCount + 1);
            }
            else if (tryCount == maxRetry + 1 && steamGuardWindow.IsValid)
            {
                NLogger.Log.Error("2FA Failed Please verify your shared secret is correct!");
            }
        }

        public static string GenerateSteamGuardCodeForTime(long time, string SharedSecret)
        {
            if (SharedSecret == null || SharedSecret.Length == 0)
            {
                return "";
            }

            string sharedSecretUnescaped = Regex.Unescape(SharedSecret);
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
    }
}