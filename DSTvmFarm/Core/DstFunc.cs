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


            //Если появилось окно установки
            if (Utils.GetDstInstallWindow().IsValid)
            {
                NLogger.Log.Warn("Обнаружено окно установки DST");
                Thread.Sleep(10000);
                Utils.SetForegroundWindow(Utils.GetDstInstallWindow().RawPtr);
                Thread.Sleep(1000);
                Utils.SendEnter(Utils.GetDstInstallWindow().RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
                NLogger.Log.Warn("Отправка Enter окну установки");
                Thread.Sleep(15000);
            }

            //Если DST обновляется
            if (Utils.GetDstUpdateWindow().IsValid)
            {
                NLogger.Log.Warn("Обнаружено окно обновления DST");
                NLogger.Log.Warn("Ждем завершения обновления");
                for (int i = 0; i <= 5; i++)
                {
                    Thread.Sleep(10000);
                    if (!Utils.GetDstUpdateWindow().IsValid)
                    {
                        NLogger.Log.Warn("Обновление завершено");
                        Thread.Sleep(5000);
                        break;
                    }

                    if (i == 5)
                    {
                        NLogger.Log.Error("Не удалось дождаться завершения обновления, переход к следующему аккаунту");
                        return false;
                    }
                    else
                    {
                        NLogger.Log.Warn("Обновление продолжается...");
                    }
                }
            }

            //После обновление, нужно нажать кнопку "Играть"
            if (Utils.GetDstReadyToLaunchWindow().IsValid)
            {
                NLogger.Log.Warn("Кликаем на кнопку: Играть");
                Utils.SetForegroundWindow(Utils.GetDstReadyToLaunchWindow().RawPtr);
                Thread.Sleep(100);
                Utils.Rect dstPlayRect = new Utils.Rect();
                Utils.GetWindowRect(Utils.GetDstReadyToLaunchWindow().RawPtr, ref dstPlayRect);
                Utils.LeftMouseClick(dstPlayRect.Right - 90, dstPlayRect.Top + 145);
                Thread.Sleep(5000);
                if (Utils.GetDstReadyToLaunchWindow().IsValid)
                {
                    NLogger.Log.Error("Ошибка при клике на кнопку, переход к следующему аккаунту");
                    return false;
                }
                Thread.Sleep(10000);
            }

            for (int i = 0; i <= 5; i++)
            {
                if (Utils.GetDstWindow().IsValid)
                {
                    NLogger.Log.Info("DST окно обнаружено!");
                    break;
                }
                else
                {
                    NLogger.Log.Info("Ждем окно DST...");
                }
                Thread.Sleep(5000);
                if (i == 5)
                {
                    NLogger.Log.Error("Окно DST не появилось, переход к следующему аккаунту");
                    return false;
                }
            }

            Thread.Sleep(20000);
            Utils.SetForegroundWindow(Utils.GetDstWindow().RawPtr);
            Thread.Sleep(2000);
            NLogger.Log.Info("Отправка Enter окну");
            for (int i = 0; i < 12; i++)
            {
                Utils.SendEnter(Utils.GetDstWindow().RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
                Thread.Sleep(1000);
            }

            return true;
        }
    }
}