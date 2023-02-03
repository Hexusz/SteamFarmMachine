using SteamLibrary.Core;
using SteamLibrary.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Win32Interop.WinHandles;

namespace SteamLibrary
{
    public class SteamUtils
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

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
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);
        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

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

        public const int WM_GETTEXT = 0xD;
        public const int WM_GETTEXTLENGTH = 0xE;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int VK_RETURN = 0x0D;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_TAB = 0x09;
        public const int VK_E = 0x45;
        public const int VK_SPACE = 0x20;
        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;
        public const int WM_CLOSE = 0x10;

        public static int API_KEY_LENGTH = 32;

        public static WindowHandle GetSteamLoginWindow(Process process)
        {
            IEnumerable<IntPtr> windows = EnumerateProcessWindowHandles(process);

            foreach (IntPtr windowHandle in windows)
            {
                string text = GetWindowTextRaw(windowHandle);

                if ((text.Contains("Steam") && text.Length > 5) || text.Equals("蒸汽平台登录"))
                {
                    return new WindowHandle(windowHandle);
                }
            }

            return WindowHandle.Invalid;
        }

        private static string GetWindowTextRaw(IntPtr hwnd)
        {
            // Allocate correct string length first
            int length = (int)SendMessage(hwnd, WM_GETTEXTLENGTH, 0, IntPtr.Zero);
            StringBuilder sb = new StringBuilder(length + 1);
            SendMessage(hwnd, WM_GETTEXT, (IntPtr)sb.Capacity, sb);
            return sb.ToString();
        }

        private static IEnumerable<IntPtr> EnumerateProcessWindowHandles(Process process)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in process.Threads)
                EnumThreadWindows(thread.Id, (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }

        public static bool TryCodeEntry(WindowHandle loginWindow, string secret)
        {
            if (!loginWindow.IsValid)
            {
                return false;
            }

            SetForegroundWindow(loginWindow.RawPtr);

            using (var automation = new UIA3Automation())
            {
                AutomationElement window = automation.FromHandle(loginWindow.RawPtr);

                if (window == null)
                {
                    return false;
                }

                AutomationElement document = window.FindFirstDescendant(e => e.ByControlType(ControlType.Document));

                if (document == null || document.FindAllChildren().Length == 0)
                {
                    return false;
                }

                int childNum = document.FindAllChildren().Length;

                if (childNum == 3 || childNum == 4)
                {
                    return false;
                }

                AutomationElement[] elements = document.FindAllChildren(e => e.ByControlType(ControlType.Edit));

                if (elements != null)
                {
                    if (elements.Length == 5)
                    {
                        string code = SteamFunc.GenerateSteamGuardCodeForTime(AppFunc.GetSystemUnixTime(), secret);

                        try
                        {
                            for (int i = 0; i < elements.Length; i++)
                            {
                                TextBox textBox = elements[i].AsTextBox();
                                textBox.Focus();
                                textBox.WaitUntilEnabled();
                                textBox.Text = code[i].ToString();
                            }
                        }
                        catch (Exception)
                        {
                            return false;
                        }

                        return true;
                    }
                }
            }

            return false;
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

        public static void ReturnFocus(IntPtr handler)
        {
            Rect rect = new SteamUtils.Rect();

            GetWindowRect(handler, ref rect);
            LeftMouseClickSlow(rect.Left + 50, rect.Top + 10);
            SetForegroundWindow(handler);
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
                    Thread.Sleep(100);
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

        public static void SendEsc(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_ESCAPE, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_ESCAPE, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_ESCAPE, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_ESCAPE, IntPtr.Zero);
                    break;
            }
        }

        public static void SendE(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_E, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_E, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_E, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_E, IntPtr.Zero);
                    break;
            }
        }

        public static Point GetCursorPosition()
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

        public static void LeftMouseClickSlow(int xPos, int yPos)
        {
            SetCursorPos(xPos, yPos);
            Thread.Sleep(500);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xPos, yPos, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, xPos, yPos, 0, 0);
            Thread.Sleep(200);
        }

        public static void SendCtrlhotKey(char key)
        {
            keybd_event(0x11, 0, 0, 0);
            keybd_event((byte)key, 0, 0, 0);
            keybd_event((byte)key, 0, 0x2, 0);
            keybd_event(0x11, 0, 0x2, 0);
        }

        public static Color GetPixelColor(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            Color color = Color.FromArgb((int)(pixel & 0x000000FF), (int)(pixel & 0x0000FF00) >> 8, (int)(pixel & 0x00FF0000) >> 16);
            return color;
        }

        public static void CloseWindow(IntPtr handle)
        {
            SendMessage(handle, WM_CLOSE, 0, IntPtr.Zero);
        }
    }
}