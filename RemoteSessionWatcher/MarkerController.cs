using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RemoteSessionWatcher
{
    public sealed class MarkerController
    {
        private readonly string _markerExePath;
        private readonly string _markerProcessName;

        public MarkerController(string markerExePath)
        {
            if (string.IsNullOrWhiteSpace(markerExePath))
                markerExePath = "service\\RemoteSessionMarker.exe";

            _markerExePath = markerExePath;
            _markerProcessName = Path.GetFileNameWithoutExtension(GetFullPath());
        }

        public void EnsureStarted()
        {
            if (IsRunning())
                return;

            string fullPath = GetFullPath();

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Не найден marker exe: " + fullPath, fullPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = fullPath,
                WorkingDirectory = Path.GetDirectoryName(fullPath),
                UseShellExecute = false
            };

            Process.Start(startInfo);
        }

        public void EnsureStopped()
        {
            var processes = Process.GetProcessesByName(_markerProcessName);

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        public bool IsRunning()
        {
            return Process.GetProcessesByName(_markerProcessName).Any();
        }

        private string GetFullPath()
        {
            if (Path.IsPathRooted(_markerExePath))
                return _markerExePath;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _markerExePath);
        }
    }
}