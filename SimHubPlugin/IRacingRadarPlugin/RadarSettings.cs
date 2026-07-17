using System;
using System.Globalization;
using System.IO;

namespace User.IRacingRadarPlugin
{
    internal sealed class RadarSettings
    {
        public double RadarRangeMeters { get; private set; }
        public double NearDistanceMeters { get; private set; }
        public double TimeAlertSeconds { get; private set; }
        public double OverlayOpacity { get; private set; }
        public double HideDelaySeconds { get; private set; }
        public string DisplayMode { get; private set; }
        public double LabelFontSize { get; private set; }

        public static RadarSettings Default()
        {
            return new RadarSettings
            {
                RadarRangeMeters = 70.0,
                NearDistanceMeters = 20.0,
                TimeAlertSeconds = 0.7,
                OverlayOpacity = 92.0,
                HideDelaySeconds = 0.8,
                DisplayMode = "Both",
                LabelFontSize = 22.0
            };
        }

        public static RadarSettings Load(string path)
        {
            RadarSettings value = Default();
            if (!File.Exists(path)) return value;

            foreach (string sourceLine in File.ReadAllLines(path))
            {
                string line = sourceLine.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;

                int separator = line.IndexOf('=');
                if (separator <= 0) continue;

                string key = line.Substring(0, separator).Trim();
                string text = line.Substring(separator + 1).Trim();
                if (key.Equals("DisplayMode", StringComparison.OrdinalIgnoreCase))
                {
                    value.DisplayMode = NormalizeDisplayMode(text);
                    continue;
                }

                double parsed;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) continue;

                if (key.Equals("RadarRangeMeters", StringComparison.OrdinalIgnoreCase))
                    value.RadarRangeMeters = Clamp(parsed, 5.0, 200.0);
                else if (key.Equals("NearDistanceMeters", StringComparison.OrdinalIgnoreCase))
                    value.NearDistanceMeters = Clamp(parsed, 1.0, 100.0);
                else if (key.Equals("TimeAlertSeconds", StringComparison.OrdinalIgnoreCase))
                    value.TimeAlertSeconds = Clamp(parsed, 0.1, 30.0);
                else if (key.Equals("OverlayOpacity", StringComparison.OrdinalIgnoreCase))
                    value.OverlayOpacity = Clamp(parsed, 0.0, 100.0);
                else if (key.Equals("HideDelaySeconds", StringComparison.OrdinalIgnoreCase))
                    value.HideDelaySeconds = Clamp(parsed, 0.0, 5.0);
                else if (key.Equals("LabelFontSize", StringComparison.OrdinalIgnoreCase))
                    value.LabelFontSize = Clamp(parsed, 10.0, 36.0);
            }

            value.NearDistanceMeters = Math.Min(value.NearDistanceMeters, value.RadarRangeMeters);
            return value;
        }

        private static string NormalizeDisplayMode(string value)
        {
            if (value.Equals("Distance", StringComparison.OrdinalIgnoreCase)) return "Distance";
            if (value.Equals("Time", StringComparison.OrdinalIgnoreCase)) return "Time";
            return "Both";
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
