using SteamLibrary.Core;
using SteamLibrary.Entities;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Win32Interop.WinHandles;

namespace SteamLibrary
{
    public class SteamUtils
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        public struct POINT
        {
            private int X;
            private int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int VK_RETURN = 0x0D;
        public const int VK_TAB = 0x09;
        public const int VK_SPACE = 0x20;
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        public static int API_KEY_LENGTH = 32;

        public static WindowHandle GetSteamLoginWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Equals("vguiPopupWindow") &&
                (wh.GetWindowText().Contains("Steam") &&
                 !wh.GetWindowText().Contains("-") &&
                 !wh.GetWindowText().Contains("—") &&
                 wh.GetWindowText().Length > 5));
        }

        public static WindowHandle GetSteamGuardWindow()
        {
            // Also checking for vguiPopupWindow class name to avoid catching things like browser tabs.
            WindowHandle windowHandle = TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Equals("vguiPopupWindow") &&
                (wh.GetWindowText().StartsWith("Steam Guard") ||
                 wh.GetWindowText().StartsWith("Steam 令牌") ||
                 wh.GetWindowText().StartsWith("Steam ガード")));
            return windowHandle;
        }

        public static WindowHandle GetSteamWarningWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
                wh.GetClassName().Equals("vguiPopupWindow") &&
                (wh.GetWindowText().StartsWith("Steam - ") ||
                 wh.GetWindowText().StartsWith("Steam — ")));
        }

        public static Process WaitForSteamProcess(WindowHandle windowHandle)
        {
            Process process = null;

            // Wait for valid process to wait for input idle.
            NLogger.Log.Info("Ждем окно Steam Guard");
            while (process == null)
            {
                int procId = 0;
                GetWindowThreadProcessId(windowHandle.RawPtr, out procId);

                // Wait for valid process id from handle.
                while (procId == 0)
                {
                    Thread.Sleep(10);
                    GetWindowThreadProcessId(windowHandle.RawPtr, out procId);
                }

                try
                {
                    process = Process.GetProcessById(procId);
                }
                catch
                {
                    process = null;
                }
            }

            return process;
        }

        public static void SendCharacter(IntPtr hwnd, VirtualInputMethod inputMethod, char c)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_CHAR, c, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_CHAR, (IntPtr)c, IntPtr.Zero);
                    break;
            }
        }

        public static void SendEnter(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_RETURN, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_RETURN, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_RETURN, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_RETURN, IntPtr.Zero);
                    break;
            }
        }

        private static Point GetCursorPosition()
        {
            GetCursorPos(out var lpPoint);
            // NOTE: If you need error handling
            // bool success = GetCursorPos(out lpPoint);
            // if (!success)

            return lpPoint;
        }

        public static void LeftMouseClick(int xPos, int yPos)
        {
            var mousePos = GetCursorPosition();
            SetCursorPos(xPos, yPos);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xPos, yPos, 0, 0);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, xPos, yPos, 0, 0);
            Thread.Sleep(50);
            SetCursorPos(mousePos.X, mousePos.Y);
        }
    }
}