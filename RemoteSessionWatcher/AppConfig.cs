using System.Collections.Generic;

namespace RemoteSessionWatcher
{
    public sealed class AppConfig
    {
        public int CheckIntervalMs { get; set; } = 1500;
        public int RequiredHitsToActivate { get; set; } = 2;
        public int RequiredHitsToDeactivate { get; set; } = 2;
        public string MarkerExePath { get; set; } = "service\\RemoteSessionMarker.exe";
        public List<WatchRule> Rules { get; set; } = new List<WatchRule>();

        public static AppConfig CreateDefault()
        {
            return new AppConfig
            {
                CheckIntervalMs = 1500,
                RequiredHitsToActivate = 2,
                RequiredHitsToDeactivate = 2,
                MarkerExePath = "service\\RemoteSessionMarker.exe",
                Rules = new List<WatchRule>
                {
                    new WatchRule
                    {
                        Name = "AnyDesk remote session",
                        Enabled = true,
                        ProcessName = "AnyDesk.exe",
                        CommandLineContains = new List<string> { "--backend" },
                        CommandLineNotContains = new List<string>()
                    },
                    new WatchRule
                    {
                        Name = "RuDesktop remote session",
                        Enabled = true,
                        ProcessName = "rudesktop.exe",
                        CommandLineContains = new List<string> { "--cm" },
                        CommandLineNotContains = new List<string>()
                    }
                }
            };
        }
    }
}