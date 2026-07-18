using System;
using System.IO;

namespace IRacingRadarConfigurator
{
    internal sealed class ConfiguratorPreferences
    {
        public bool English { get; set; }
        public bool DayMode { get; set; }

        public static string DefaultPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "iRacingRadar", "Configurator.settings.ini");
            }
        }

        public static ConfiguratorPreferences Load(string path)
        {
            ConfiguratorPreferences preferences = new ConfiguratorPreferences();
            if (!File.Exists(path)) return preferences;
            foreach (string source in File.ReadAllLines(path))
            {
                string line = source.Trim();
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;
                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                if (key.Equals("Language", StringComparison.OrdinalIgnoreCase))
                    preferences.English = value.Equals("English", StringComparison.OrdinalIgnoreCase);
                else if (key.Equals("Theme", StringComparison.OrdinalIgnoreCase))
                    preferences.DayMode = value.Equals("Day", StringComparison.OrdinalIgnoreCase);
            }
            return preferences;
        }

        public void Save(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllLines(path, new[]
            {
                "# iRacing Radar Configurator UI preferences",
                "Language=" + (English ? "English" : "Chinese"),
                "Theme=" + (DayMode ? "Day" : "Night")
            });
        }
    }
}
