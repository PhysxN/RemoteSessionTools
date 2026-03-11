using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace RemoteSessionWatcher
{
    public static class ProcessScanner
    {
        public static List<ProcessSnapshot> GetProcesses(IEnumerable<string> processNames)
        {
            var names = (processNames ?? Enumerable.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var result = new List<ProcessSnapshot>();

            if (names.Count == 0)
                return result;

            string where = string.Join(" OR ", names.Select(n => "Name = '" + EscapeWmiString(n) + "'"));
            string query = "SELECT ProcessId, Name, CommandLine FROM Win32_Process WHERE " + where;

            using (var searcher = new ManagementObjectSearcher(@"root\CIMV2", query))
            using (var collection = searcher.Get())
            {
                foreach (ManagementObject obj in collection)
                {
                    result.Add(new ProcessSnapshot
                    {
                        ProcessId = Convert.ToInt32(obj["ProcessId"]),
                        Name = obj["Name"]?.ToString() ?? "",
                        CommandLine = obj["CommandLine"]?.ToString() ?? ""
                    });
                }
            }

            return result;
        }

        private static string EscapeWmiString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'");
        }
    }
}