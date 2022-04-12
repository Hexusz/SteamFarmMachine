using DstDailyFarm.Entities;
using SteamLibrary;
using SteamLibrary.Core;
using SteamLibrary.Entities;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace DstDailyFarm.Core
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

            //Ждем после запуска DST
            Thread.Sleep(15000);

            //Если появилось окно установки
            if (DstUtils.GetDstInstallWindow().IsValid)
            {
                NLogger.Log.Warn("Обнаружено окно установки DST");
                Thread.Sleep(10000);
                SteamUtils.SetForegroundWindow(DstUtils.GetDstInstallWindow().RawPtr);
                Thread.Sleep(1000);
                SteamUtils.SendEnter(DstUtils.GetDstInstallWindow().RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
                NLogger.Log.Warn("Отправка Enter окну установки");
                Thread.Sleep(15000);
            }

            //Если DST обновляется
            if (DstUtils.GetDstUpdateWindow().IsValid)
            {
                NLogger.Log.Warn("Обнаружено окно обновления DST");
                NLogger.Log.Warn("Ждем завершения обновления");
                for (int i = 0; i <= 5; i++)
                {
                    Thread.Sleep(10000);
                    if (!DstUtils.GetDstUpdateWindow().IsValid)
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
            if (DstUtils.GetDstReadyToLaunchWindow().IsValid)
            {
                NLogger.Log.Warn("Кликаем на кнопку: Играть");
                SteamUtils.SetForegroundWindow(DstUtils.GetDstReadyToLaunchWindow().RawPtr);
                Thread.Sleep(100);
                SteamUtils.Rect dstPlayRect = new SteamLibrary.SteamUtils.Rect();
                SteamUtils.GetWindowRect(DstUtils.GetDstReadyToLaunchWindow().RawPtr, ref dstPlayRect);
                SteamUtils.LeftMouseClick(dstPlayRect.Right - 90, dstPlayRect.Top + 145);
                Thread.Sleep(5000);
                if (DstUtils.GetDstReadyToLaunchWindow().IsValid)
                {
                    NLogger.Log.Error("Ошибка при клике на кнопку, переход к следующему аккаунту");
                    return false;
                }
                Thread.Sleep(10000);
            }

            //Если появилось окно с ошибкой облачной синхронизации
            if (!DstUtils.GetDstWindow().IsValid && DstUtils.GetDstCloudErrorWindow().IsValid)
            {
                NLogger.Log.Warn("Обнаружено окно ошибки облачной синхронизации");
                SteamUtils.SetForegroundWindow(DstUtils.GetDstCloudErrorWindow().RawPtr);
                SteamUtils.Rect cloudErrorWindow = new SteamLibrary.SteamUtils.Rect();
                SteamUtils.GetWindowRect(DstUtils.GetDstCloudErrorWindow().RawPtr, ref cloudErrorWindow);
                SteamUtils.LeftMouseClick(cloudErrorWindow.Left + 60, cloudErrorWindow.Bottom - 40);
                NLogger.Log.Warn("Пытаемся продолжить запуск");
                Thread.Sleep(5000);
            }

            //Ищем окно DST несколько раз
            for (int i = 0; i <= 5; i++)
            {
                if (DstUtils.GetDstWindow().IsValid)
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
            SteamUtils.SetForegroundWindow(DstUtils.GetDstWindow().RawPtr);
            Thread.Sleep(2000);
            NLogger.Log.Info("Отправка Enter окну");
            for (int i = 0; i < 12; i++)
            {
                SteamUtils.SendEnter(DstUtils.GetDstWindow().RawPtr, (VirtualInputMethod)Program.watcher.MainConfig.VirtualInputMethod);
                Thread.Sleep(1000);
            }

            return true;
        }

        public static async Task<DstAppConfig> LoadConfig()
        {
            var cfgPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DstDailyFarm.conf");
            if (File.Exists(cfgPath))
            {
                await using FileStream fs = new FileStream(cfgPath, FileMode.OpenOrCreate);
                var conf = await JsonSerializer.DeserializeAsync<DstAppConfig>(fs);
                return conf;
            }
            else
            {
                await using FileStream fs = new FileStream("DstDailyFarm.conf", FileMode.OpenOrCreate);
                var defaultConfig = new DstAppConfig()
                {
                    SteamPath = @"C:\Program Files (x86)\Steam",
                    VirtualInputMethod = 0
                };

                await JsonSerializer.SerializeAsync<DstAppConfig>(fs, defaultConfig);
                NLogger.Log.Info("Загрузка конфига завершена");
                return defaultConfig;
            }
        }
    }
}