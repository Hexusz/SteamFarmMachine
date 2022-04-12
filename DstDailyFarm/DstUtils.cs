using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SteamLibrary;
using SteamLibrary.Entities;
using Win32Interop.WinHandles;

namespace DstDailyFarm
{
    public class DstUtils
    {
        public static WindowHandle GetDstWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Equals("opengles2.0") &&
                (wh.GetWindowText().StartsWith("Don't Starve Together")));
        }

        public static WindowHandle GetDstInstallWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Equals("vguiPopupWindow") &&
                (wh.GetWindowText().StartsWith("Install - Don't Starve Together") ||
                 wh.GetWindowText().StartsWith("Установка — Don't Starve Together")));
        }

        public static WindowHandle GetDstReadyToLaunchWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Equals("vguiPopupWindow") &&
                (wh.GetWindowText().StartsWith("Ready - Don't Starve Together") ||
                 wh.GetWindowText().StartsWith("Готово — Don't Starve Together")));
        }

        public static WindowHandle GetDstUpdateWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Equals("vguiPopupWindow") &&
                (wh.GetWindowText().StartsWith("Updating Don't Starve Together") ||
                wh.GetWindowText().StartsWith("Обновление Don't Starve Together")));
        }

        public static WindowHandle GetDstCloudErrorWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Equals("SDL_app") &&
                (wh.GetWindowText().StartsWith("Steam Dialog") ||
                wh.GetWindowText().StartsWith("Диалоговое окно Steam")));
        }
    }
}