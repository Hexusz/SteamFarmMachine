using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSTvmFarm.Entities;

namespace DSTvmFarm.Core
{
    public class DstFunc
    {
        public static async Task<bool> StartFarm()
        {
            Thread.Sleep(10000);

            StringBuilder parametersBuilder = new StringBuilder();

            parametersBuilder.Append($" -applaunch 322330");


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
                NLogger.Log.Info("Запуск DST");
            }
            catch (Exception ex)
            {
                NLogger.Log.Error("Ошибка при запуске DST " + ex.Message);
                return false;
            }

            Thread.Sleep(15000);

            var dstWindows = Utils.GetDstWindow();
            var dstInstallWindows = Utils.GetDstInstallWindow();

            if (dstInstallWindows.IsValid)
            {
                NLogger.Log.Warn("Обнаружено окно установки DST");
                Thread.Sleep(10000);
                Utils.SetForegroundWindow(dstInstallWindows.RawPtr);
                Thread.Sleep(1000);
                Utils.SendEnter(dstInstallWindows.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
                NLogger.Log.Warn("Отправка Enter окну установки");
                Thread.Sleep(15000);
            }

            for (int i = 0; i <= 5; i++)
            {
                dstWindows = Utils.GetDstWindow();
                if (dstWindows.IsValid)
                {
                    break;
                }
                Thread.Sleep(5000);
                if (i == 5)
                {
                    NLogger.Log.Error("Окно DST не появилось, переход к следующему аккаунту");
                    return false;
                }
            }

            NLogger.Log.Info("DST окно обнаружено!");

            Thread.Sleep(20000);
            Utils.SetForegroundWindow(dstWindows.RawPtr);
            Thread.Sleep(2000);
            NLogger.Log.Info("Отправка Enter окну");
            Utils.SendEnter(dstWindows.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
            Thread.Sleep(2000);
            Utils.SendEnter(dstWindows.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
            Thread.Sleep(2000);
            Utils.SendEnter(dstWindows.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
            Thread.Sleep(2000);
            Utils.SendEnter(dstWindows.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
            Thread.Sleep(2000);
            Utils.SendEnter(dstWindows.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
            Thread.Sleep(2000);
            Utils.SendEnter(dstWindows.RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
            Thread.Sleep(2000);

            return true;
        }
    }
}