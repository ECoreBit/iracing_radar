using System;

namespace IRacingRadarConfigurator
{
    public static class RadarOverlayMath
    {
        public static double AlertProgress(RadarConfiguratorSettings settings, double distanceMeters, double timeSeconds)
        {
            double distance = Progress(Math.Abs(distanceMeters), settings.RadarRangeMeters);
            double time = Progress(Math.Abs(timeSeconds), settings.TimeAlertSeconds);
            if (settings.DisplayMode == "Distance") return distance;
            if (settings.DisplayMode == "Time") return time;
            return Math.Max(distance, time);
        }

        public static double NearStart(RadarConfiguratorSettings settings)
        {
            return Clamp((1.0 - settings.NearDistanceMeters / settings.RadarRangeMeters) * 100.0, 1, 95);
        }

        public static double NearProgress(double alertProgress, double nearStart)
        {
            if (alertProgress <= nearStart) return 0;
            return Clamp((alertProgress - nearStart) / (100 - nearStart) * 100, 0, 100);
        }

        public static double FarProgress(double alertProgress, double nearStart)
        {
            if (nearStart <= 0 || alertProgress >= nearStart) return 0;
            return Clamp((1.0 - alertProgress / nearStart) * 100, 0, 100);
        }

        public static double NearBlend(RadarConfiguratorSettings settings, double alertProgress)
        {
            double nearStart = NearStart(settings);
            double transition = Math.Max(2.0, 2.5 / settings.RadarRangeMeters * 100.0);
            double start = nearStart - transition;
            double end = nearStart + transition;
            if (alertProgress <= start) return 0;
            if (alertProgress >= end) return 100;
            double t = (alertProgress - start) / (end - start);
            return t * t * (3.0 - 2.0 * t) * 100.0;
        }

        public static int FrameIndex(double progress)
        {
            return Math.Max(0, Math.Min(59, (int)Math.Round(Clamp(progress, 0, 100) * 59 / 100.0)));
        }

        public static double OvertakeRelative(double progress)
        {
            double t = Clamp(progress, 0, 1);
            t = t * t * (3.0 - 2.0 * t);
            return 9.0 - 18.0 * t;
        }
        public static double SideTop(double relativeMeters)
        {
            return Clamp(109.0 + relativeMeters * 4.8, 10, 208);
        }

        private static double Progress(double value, double threshold)
        {
            if (threshold <= 0 || value > threshold) return 0;
            return Clamp((1.0 - value / threshold) * 100, 0, 100);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
