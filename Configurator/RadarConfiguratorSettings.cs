using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace IRacingRadarConfigurator
{
    public sealed class RadarConfiguratorSettings
    {
        public string DisplayMode { get; set; }
        public double RadarRangeMeters { get; set; }
        public double NearDistanceMeters { get; set; }
        public bool FrontGreenArcEnabled { get; set; }
        public bool RearGreenArcEnabled { get; set; }
        public bool CatchEstimateEnabled { get; set; }
        public double TimeAlertSeconds { get; set; }
        public double RadarFadeBandPercent { get; set; }
        public double LabelFontSize { get; set; }
        public double OverlayOpacity { get; set; }

        public static RadarConfiguratorSettings Defaults()
        {
            return new RadarConfiguratorSettings
            {
                DisplayMode = "Both",
                RadarRangeMeters = 70.0,
                NearDistanceMeters = 20.0,
                FrontGreenArcEnabled = true,
                RearGreenArcEnabled = true,
                CatchEstimateEnabled = true,
                TimeAlertSeconds = 0.7,
                RadarFadeBandPercent = 15.0,
                LabelFontSize = 22.0,
                OverlayOpacity = 92.0
            };
        }

        public static RadarConfiguratorSettings Load(string path)
        {
            return File.Exists(path) ? Parse(File.ReadAllText(path)) : Defaults();
        }

        public static RadarConfiguratorSettings Parse(string text)
        {
            RadarConfiguratorSettings value = Defaults();
            if (text == null) return value;
            foreach (string source in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = source.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                int separator = line.IndexOf('=');
                if (separator <= 0) continue;
                string key = line.Substring(0, separator).Trim();
                string raw = line.Substring(separator + 1).Trim();
                double number;
                bool boolean;
                if (key.Equals("DisplayMode", StringComparison.OrdinalIgnoreCase))
                    value.DisplayMode = NormalizeMode(raw);
                else if (key.Equals("FrontGreenArcEnabled", StringComparison.OrdinalIgnoreCase) && TryBoolean(raw, out boolean))
                    value.FrontGreenArcEnabled = boolean;
                else if (key.Equals("RearGreenArcEnabled", StringComparison.OrdinalIgnoreCase) && TryBoolean(raw, out boolean))
                    value.RearGreenArcEnabled = boolean;
                else if (key.Equals("CatchEstimateEnabled", StringComparison.OrdinalIgnoreCase) && TryBoolean(raw, out boolean))
                    value.CatchEstimateEnabled = boolean;
                else if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                {
                    if (key.Equals("RadarRangeMeters", StringComparison.OrdinalIgnoreCase)) value.RadarRangeMeters = Clamp(number, 5, 200);
                    else if (key.Equals("NearDistanceMeters", StringComparison.OrdinalIgnoreCase)) value.NearDistanceMeters = Clamp(number, 1, 100);
                    else if (key.Equals("TimeAlertSeconds", StringComparison.OrdinalIgnoreCase)) value.TimeAlertSeconds = Clamp(number, 0.1, 30);
                    else if (key.Equals("RadarFadeBandPercent", StringComparison.OrdinalIgnoreCase)) value.RadarFadeBandPercent = Clamp(number, 1, 50);
                    else if (key.Equals("LabelFontSize", StringComparison.OrdinalIgnoreCase)) value.LabelFontSize = Clamp(number, 10, 36);
                    else if (key.Equals("OverlayOpacity", StringComparison.OrdinalIgnoreCase)) value.OverlayOpacity = Clamp(number, 0, 100);
                }
            }
            value.NearDistanceMeters = Math.Min(value.NearDistanceMeters, value.RadarRangeMeters);
            return value;
        }

        public void Validate()
        {
            DisplayMode = NormalizeMode(DisplayMode ?? "Both");
            RadarRangeMeters = Clamp(RadarRangeMeters, 5, 200);
            NearDistanceMeters = Clamp(NearDistanceMeters, 1, Math.Min(100, RadarRangeMeters));
            TimeAlertSeconds = Clamp(TimeAlertSeconds, 0.1, 30);
            RadarFadeBandPercent = Clamp(RadarFadeBandPercent, 1, 50);
            LabelFontSize = Clamp(LabelFontSize, 10, 36);
            OverlayOpacity = Clamp(OverlayOpacity, 0, 100);
        }

        public string UpdateDocument(string original)
        {
            Validate();
            string text = string.IsNullOrEmpty(original) ? DefaultDocument() : original;
            foreach (KeyValuePair<string, string> pair in Values())
            {
                string pattern = "(?im)^(\\s*" + Regex.Escape(pair.Key) + "\\s*=).*$";
                if (Regex.IsMatch(text, pattern))
                    text = Regex.Replace(text, pattern, "${1}" + pair.Value);
                else
                    text = text.TrimEnd() + Environment.NewLine + pair.Key + "=" + pair.Value + Environment.NewLine;
            }
            return text;
        }

        public void Save(string path)
        {
            string original = File.Exists(path) ? File.ReadAllText(path) : DefaultDocument();
            string updated = UpdateDocument(original);
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, updated, new UTF8Encoding(true));
            File.Copy(temporary, path, true);
            File.Delete(temporary);
        }

        private IEnumerable<KeyValuePair<string, string>> Values()
        {
            yield return Pair("DisplayMode", DisplayMode);
            yield return Pair("RadarRangeMeters", Number(RadarRangeMeters));
            yield return Pair("NearDistanceMeters", Number(NearDistanceMeters));
            yield return Pair("FrontGreenArcEnabled", FrontGreenArcEnabled ? "true" : "false");
            yield return Pair("RearGreenArcEnabled", RearGreenArcEnabled ? "true" : "false");
            yield return Pair("CatchEstimateEnabled", CatchEstimateEnabled ? "true" : "false");
            yield return Pair("TimeAlertSeconds", Number(TimeAlertSeconds));
            yield return Pair("RadarFadeBandPercent", Number(RadarFadeBandPercent));
            yield return Pair("LabelFontSize", Number(LabelFontSize));
            yield return Pair("OverlayOpacity", Number(OverlayOpacity));
        }

        private static KeyValuePair<string, string> Pair(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }

        private static string Number(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string NormalizeMode(string value)
        {
            if (value.Equals("None", StringComparison.OrdinalIgnoreCase)) return "None";
            if (value.Equals("Distance", StringComparison.OrdinalIgnoreCase)) return "Distance";
            if (value.Equals("Time", StringComparison.OrdinalIgnoreCase)) return "Time";
            return "Both";
        }

        private static bool TryBoolean(string value, out bool parsed)
        {
            if (bool.TryParse(value, out parsed)) return true;
            if (value.Equals("1") || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase)) { parsed = true; return true; }
            if (value.Equals("0") || value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("off", StringComparison.OrdinalIgnoreCase)) { parsed = false; return true; }
            parsed = false;
            return false;
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static string DefaultDocument()
        {
            return "# iRacing 雷达设置 / iRacing Radar Settings" + Environment.NewLine;
        }
    }
}
