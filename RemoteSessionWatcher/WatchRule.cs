using System.Collections.Generic;

namespace RemoteSessionWatcher
{
    public sealed class WatchRule
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public string ProcessName { get; set; }
        public List<string> CommandLineContains { get; set; } = new List<string>();
        public List<string> CommandLineNotContains { get; set; } = new List<string>();
    }
}