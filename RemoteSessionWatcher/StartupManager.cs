using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace RemoteSessionWatcher
{
    public static class StartupManager
    {
        private const string TaskName = "RemoteSessionWatcher_AutoStart";

        public static bool IsEnabled()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/Query /TN \"" + TaskName + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = System.Text.Encoding.GetEncoding(866),
                    StandardErrorEncoding = System.Text.Encoding.GetEncoding(866)
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                        return false;

                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Ошибка проверки задачи планировщика: " + ex);
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
                CreateScheduledTask();
            else
                DeleteScheduledTask();
        }

        private static void CreateScheduledTask()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string currentUser = WindowsIdentity.GetCurrent().Name;

            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
                throw new FileNotFoundException("Не найден exe для создания задачи.", exePath);

            string taskRun = "\"" + exePath + "\"";

            string arguments =
                "/Create " +
                "/F " +
                "/TN \"" + TaskName + "\" " +
                "/SC ONLOGON " +
                "/RL HIGHEST " +
                "/RU \"" + currentUser + "\" " +
                "/TR \"" + taskRun.Replace("\"", "\\\"") + "\"";

            RunSchtasks(arguments);
        }

        public static void RunNow()
        {
            string arguments = "/Run /TN \"" + TaskName + "\"";
            RunSchtasks(arguments);
        }

        private static void DeleteScheduledTask()
        {
            string arguments = "/Delete /F /TN \"" + TaskName + "\"";
            RunSchtasks(arguments);
        }

        private static void RunSchtasks(string arguments)
        {
            Logger.Log("schtasks arguments: " + arguments);

            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.GetEncoding(866),
                StandardErrorEncoding = System.Text.Encoding.GetEncoding(866)
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                    throw new InvalidOperationException("Не удалось запустить schtasks.exe");

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                Logger.Log("schtasks exit code: " + process.ExitCode);

                if (!string.IsNullOrWhiteSpace(output))
                    Logger.Log("schtasks output: " + output.Trim());

                if (!string.IsNullOrWhiteSpace(error))
                    Logger.Log("schtasks error: " + error.Trim());

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "schtasks завершился с кодом " + process.ExitCode +
                        ". Output: " + output +
                        ". Error: " + error);
                }
            }
        }
    }
}