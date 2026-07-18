using System;
using System.Globalization;

namespace IRacingRadarConfigurator
{
    public static class RadarPreviewMath
    {
        public static bool IsTriggered(RadarConfiguratorSettings settings, double distanceMeters, double timeSeconds)
        {
            bool distance = Math.Abs(distanceMeters) <= settings.RadarRangeMeters;
            bool time = Math.Abs(timeSeconds) <= settings.TimeAlertSeconds;
            if (settings.DisplayMode == "Distance") return distance;
            if (settings.DisplayMode == "Time") return time;
            return distance || time;
        }

        public static double Opacity(RadarConfiguratorSettings settings, double distanceMeters, double timeSeconds)
        {
            double distance = ThresholdOpacity(Math.Abs(distanceMeters), settings.RadarRangeMeters, settings.RadarFadeBandPercent);
            double time = ThresholdOpacity(Math.Abs(timeSeconds), settings.TimeAlertSeconds, settings.RadarFadeBandPercent);
            if (settings.DisplayMode == "Distance") return distance;
            if (settings.DisplayMode == "Time") return time;
            return Math.Max(distance, time);
        }

        public static bool IsNear(RadarConfiguratorSettings settings, double distanceMeters)
        {
            return Math.Abs(distanceMeters) <= settings.NearDistanceMeters;
        }

        public static string DisplayText(RadarConfiguratorSettings settings, double distanceMeters, double timeSeconds)
        {
            if (settings.DisplayMode == "None" || Math.Abs(distanceMeters) < 2.5) return string.Empty;
            string distance = Math.Abs(distanceMeters).ToString("0", CultureInfo.InvariantCulture) + "m";
            string time = Math.Abs(timeSeconds).ToString("0.0", CultureInfo.InvariantCulture) + "s";
            if (settings.DisplayMode == "Distance") return distance;
            if (settings.DisplayMode == "Time") return time;
            return distance + " / " + time;
        }

        private static double ThresholdOpacity(double value, double threshold, double fadePercent)
        {
            if (threshold <= 0 || value > threshold) return 0;
            double band = threshold * Math.Max(0.01, Math.Min(0.5, fadePercent / 100.0));
            double fullAt = threshold - band;
            if (value <= fullAt) return 100;
            return Math.Max(0, Math.Min(100, (threshold - value) / band * 100));
        }
    }
}
