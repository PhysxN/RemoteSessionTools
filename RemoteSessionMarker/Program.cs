using System;
using System.Threading;
using System.Windows.Forms;

namespace RemoteSessionMarker
{
    internal static class Program
    {
        private static Mutex _mutex;

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            _mutex = new Mutex(true, @"Local\RemoteSessionMarker_SingleInstance", out createdNew);

            if (!createdNew)
                return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ApplicationContext());

            GC.KeepAlive(_mutex);
        }
    }
}