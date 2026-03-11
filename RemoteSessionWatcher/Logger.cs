using System;
using System.IO;
using System.Text;

namespace RemoteSessionWatcher
{
    public static class Logger
    {
        private static readonly object Sync = new object();

        public static void Log(string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(ConfigManager.AppDataDirectory);

                    string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message + Environment.NewLine;
                    File.AppendAllText(ConfigManager.LogPath, line, Encoding.UTF8);
                }
            }
            catch
            {
            }
        }
    }
}