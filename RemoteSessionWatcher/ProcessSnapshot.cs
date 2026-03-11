namespace RemoteSessionWatcher
{
    public sealed class ProcessSnapshot
    {
        public int ProcessId { get; set; }
        public string Name { get; set; }
        public string CommandLine { get; set; }
    }
}