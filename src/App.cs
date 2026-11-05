using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

[assembly: TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]
[assembly: AssemblyTitle("牛马鼓励器")]
[assembly: AssemblyProduct("牛马鼓励器")]
[assembly: AssemblyVersion("1.0.0.0")]

namespace NiuMa
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            bool created;
            using (var mutex = new Mutex(true, "NiuMaTuoMinQi_SingleInstance", out created))
            {
                if (!created)
                {
                    Native.PostMessage((IntPtr)0xFFFF, Native.ShowMeMessage, IntPtr.Zero, IntPtr.Zero);
                    return;
                }
                var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.DispatcherUnhandledException += (s, e) =>
                {
                    try { File.AppendAllText(Path.Combine(AppSettings.Dir, "error.log"), DateTime.Now + "  " + e.Exception + "\r\n\r\n"); } catch { }
                    e.Handled = true;
                };
                var win = new MainWindow(AppSettings.Load());
                win.Show();
                app.Run();
            }
        }
    }

    public static class Native
    {
        public static readonly int ShowMeMessage = RegisterWindowMessage("NiuMaTuoMinQi_ShowMe");

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int RegisterWindowMessage(string s);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int index, int value);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x20;
        public const int WS_EX_TOOLWINDOW = 0x80;
        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_NOREPEAT = 0x4000;

        public static void SetExStyle(Window w, int flag, bool on)
        {
            var h = new WindowInteropHelper(w).Handle;
            if (h == IntPtr.Zero) return;
            int st = GetWindowLong(h, GWL_EXSTYLE);
            SetWindowLong(h, GWL_EXSTYLE, on ? (st | flag) : (st & ~flag));
        }
    }

}
