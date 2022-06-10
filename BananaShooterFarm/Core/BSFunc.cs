using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

        public static Dictionary<string, Process> RefreshPIDs(List<Account> accounts)
        {
            Dictionary<string, Process> PIDs = new Dictionary<string, Process>();

            foreach (var account in accounts)
            {
                Process[] processlist = Process.GetProcesses();
                foreach (Process process in processlist)
                {
                    if (process.MainWindowTitle.Contains(account.SteamGuardAccount.AccountName) &&
                        process.MainWindowTitle.Contains("Banana Shooter"))
                    {
                        PIDs.Add(account.SteamGuardAccount.AccountName, process);
                    }
                }
            }

            return PIDs;
        }
    }
}