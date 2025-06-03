using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace MyTasksAndNotes
{


    class HotkeyManager
    {

        Dictionary<int, List<Action>> actions = new Dictionary<int, List<Action>>();
        IntPtr hWnd;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        Window mainWindow;
        public HotkeyManager(Window _mainWindow)
        {
            mainWindow = _mainWindow;
            init();
            instance = this;
        }


        // ugly singleton implemenation
        static HotkeyManager instance;
        public static HotkeyManager getInstance() { return instance; }



        private void init()
        {


            // wait for window to load
            mainWindow.Loaded += (sender, e) => {

                // Configure window to be invisible
                Window tWindow = new Window();
                tWindow.WindowStyle = WindowStyle.None;
                tWindow.AllowsTransparency = true;
                tWindow.Background = System.Windows.Media.Brushes.Transparent;
                tWindow.ShowInTaskbar = false;
                tWindow.Width = 0;
                tWindow.Height = 0;

                if (!tWindow.IsLoaded)
                {
                    // Ensure window is created before getting handle
                    tWindow.Show();
                    tWindow.Hide();
                }


                // Optional: Prevent window from being shown
                tWindow.ShowActivated = false;


                var helper = new WindowInteropHelper(tWindow);
                hWnd = helper.Handle; // Should now be valid

                // Register Ctrl + Alt + Up
                bool failed = true;
                failed = !RegisterHotKey(hWnd, HotKeyIds.MENU_UP, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_ALT, VirtualKeys.UP);
                failed = !RegisterHotKey(hWnd, HotKeyIds.MENU_DOWN, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_ALT, VirtualKeys.DOWN);
                failed = !RegisterHotKey(hWnd, HotKeyIds.MENU_LEFT, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_ALT, VirtualKeys.LEFT);
                failed = !RegisterHotKey(hWnd, HotKeyIds.MENU_RIGHT, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_ALT, VirtualKeys.RIGHT);
                if (failed)
                {
                    MessageBox.Show("Failed to register hotkey.");
                }

                // Hook into the window message loop
                HwndSource source = HwndSource.FromHwnd(hWnd);
                source.AddHook(HwndHook);

            };




        }

        public void subscribeHotkey(Action callback, int hotKeyId ) 
        {
            if (actions.ContainsKey(hotKeyId)) 
            {
                actions[hotKeyId].Add(callback);
            }
            else
            {
                actions.Add(hotKeyId,new List<Action>() { callback });
            }
        }

        public void notifySubscribers(int hotkeyId) 
        {
            var subscribers = actions[hotkeyId];
            foreach(var callback in subscribers) 
            {
                callback();
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020;
            const int SC_CLOSE = 0xF060;

            if (msg == WM_HOTKEY)
            {
                notifySubscribers(wParam.ToInt32());
                handled = true;

            }
            else if (msg == WM_SYSCOMMAND)
            {
                // Always allow minimize/close commands
                if (wParam.ToInt32() == SC_MINIMIZE || wParam.ToInt32() == SC_CLOSE)
                {
                    handled = false;
                }
            }
            return IntPtr.Zero;
        }
    }

    public static class HotKeyIds 
    {
        public const int OPEN_MENU = 0x0000;
        public const int MENU_LEFT = 90000;
        public const int MENU_RIGHT = 90001;
        public const int MENU_UP = 90002;
        public const int MENU_DOWN = 90003;
    }

    public static class VirtualKeys
    {
        public const uint MOD_NONE = 0x0000;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        // Function keys
        public const int F1 = 0x70;
        public const int F2 = 0x71;
        public const int F3 = 0x72;
        public const int F4 = 0x73;
        public const int F5 = 0x74;
        public const int F6 = 0x75;
        public const int F7 = 0x76;
        public const int F8 = 0x77;
        public const int F9 = 0x78;
        public const int F10 = 0x79;
        public const int F11 = 0x7A;
        public const int F12 = 0x7B;

        // Arrow keys
        public const int UP = 0x26;
        public const int DOWN = 0x28;
        public const int LEFT = 0x25;
        public const int RIGHT = 0x27;

        // Special keys
        public const int ESCAPE = 0x1B;
        public const int SPACE = 0x20;
        public const int RETURN = 0x0D;
        public const int TAB = 0x09;
        public const int BACK = 0x08;
        public const int DELETE = 0x2E;
        public const int INSERT = 0x2D;
        public const int HOME = 0x24;
        public const int END = 0x23;
        public const int PAGE_UP = 0x21;
        public const int PAGE_DOWN = 0x22;

        // Letter keys (A-Z)
        public const int A = 0x41;
        public const int B = 0x42;
        public const int C = 0x43;
        public const int D = 0x44;
        public const int E = 0x45;
        public const int F = 0x46;
        public const int G = 0x47;
        public const int H = 0x48;
        public const int I = 0x49;
        public const int J = 0x4A;
        public const int K = 0x4B;
        public const int L = 0x4C;
        public const int M = 0x4D;
        public const int N = 0x4E;
        public const int O = 0x4F;
        public const int P = 0x50;
        public const int Q = 0x51;
        public const int R = 0x52;
        public const int S = 0x53;
        public const int T = 0x54;
        public const int U = 0x55;
        public const int V = 0x56;
        public const int W = 0x57;
        public const int X = 0x58;
        public const int Y = 0x59;
        public const int Z = 0x5A;

        // Number keys (0-9)
        public const int ZERO = 0x30;
        public const int ONE = 0x31;
        public const int TWO = 0x32;
        public const int THREE = 0x33;
        public const int FOUR = 0x34;
        public const int FIVE = 0x35;
        public const int SIX = 0x36;
        public const int SEVEN = 0x37;
        public const int EIGHT = 0x38;
        public const int NINE = 0x39;

        // Numpad keys
        public const int NUMPAD0 = 0x60;
        public const int NUMPAD1 = 0x61;
        public const int NUMPAD2 = 0x62;
        public const int NUMPAD3 = 0x63;
        public const int NUMPAD4 = 0x64;
        public const int NUMPAD5 = 0x65;
        public const int NUMPAD6 = 0x66;
        public const int NUMPAD7 = 0x67;
        public const int NUMPAD8 = 0x68;
        public const int NUMPAD9 = 0x69;
        public const int ADD = 0x6B;
        public const int SUBTRACT = 0x6D;
        public const int MULTIPLY = 0x6A;
        public const int DIVIDE = 0x6F;
        public const int DECIMAL = 0x6E;

        // Other keys
        public const int OEM_1 = 0xBA;   // ;:
        public const int OEM_PLUS = 0xBB; // =+
        public const int OEM_COMMA = 0xBC; // ,<
        public const int OEM_MINUS = 0xBD; // -_
        public const int OEM_PERIOD = 0xBE; // .>
        public const int OEM_2 = 0xBF;    // /?
        public const int OEM_3 = 0xC0;    // `~
        public const int OEM_4 = 0xDB;    // [{
        public const int OEM_5 = 0xDC;    // \|
        public const int OEM_6 = 0xDD;    // ]}
        public const int OEM_7 = 0xDE;    // '"
    }

}
