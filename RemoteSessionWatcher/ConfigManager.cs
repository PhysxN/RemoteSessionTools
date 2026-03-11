using System;
using System.IO;
using Newtonsoft.Json;

namespace RemoteSessionWatcher
{
    public static class ConfigManager
    {
        public static readonly string AppBaseDirectory =
            AppDomain.CurrentDomain.BaseDirectory;

        public static readonly string AppDataDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RemoteSessionWatcher");

        public static readonly string ConfigPath =
            Path.Combine(AppBaseDirectory, "config.json");

        public static readonly string LogPath =
            Path.Combine(AppDataDirectory, "log.txt");

        static ConfigManager()
        {
            Directory.CreateDirectory(AppDataDirectory);
        }

        public static AppConfig LoadOrCreateDefault()
        {
            if (!File.Exists(ConfigPath))
            {
                var config = AppConfig.CreateDefault();
                Save(config);
                return config;
            }

            string json = File.ReadAllText(ConfigPath);
            var loaded = JsonConvert.DeserializeObject<AppConfig>(json);

            if (loaded == null)
                throw new InvalidOperationException("config.json пустой или не распознан.");

            if (loaded.Rules == null)
                loaded.Rules = new System.Collections.Generic.List<WatchRule>();

            if (loaded.CheckIntervalMs < 500)
                loaded.CheckIntervalMs = 500;

            if (loaded.RequiredHitsToActivate < 1)
                loaded.RequiredHitsToActivate = 1;

            if (loaded.RequiredHitsToDeactivate < 1)
                loaded.RequiredHitsToDeactivate = 1;

            if (string.IsNullOrWhiteSpace(loaded.MarkerExePath))
                loaded.MarkerExePath = "service\\RemoteSessionMarker.exe";

            return loaded;
        }

        public static void Save(AppConfig config)
        {
            Directory.CreateDirectory(AppDataDirectory);

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
    }
}