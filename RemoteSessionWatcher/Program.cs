using System;
using System.Threading;
using System.Windows.Forms;

namespace RemoteSessionWatcher
{
    internal static class Program
    {
        private static Mutex _mutex;

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            _mutex = new Mutex(true, @"Local\RemoteSessionWatcher_SingleInstance", out createdNew);

            if (!createdNew)
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());

            GC.KeepAlive(_mutex);
        }
    }
}