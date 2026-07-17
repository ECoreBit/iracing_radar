using System;

namespace User.IRacingRadarPlugin
{
    public static class RadarMath
    {
        // Car markers are 18 x 42 px in a 420 x 260 overlay.
        public const double CenterTop = 109.0;
        public const double MinimumTop = 10.0;
        public const double MaximumTop = 208.0;
        private const double SidePixelsPerMeter = 4.8;
        private const double CenterCarBaseTop = 109.0;
        private const double CenterCarMaximumOffset = 94.0;
        private const double RadarRangeMeters = 70.0;
        private const double SidePositionTimeConstantSeconds = 0.08;

        public static double CalculateTopFromRelativeMeters(double relativeMeters, double previousTop)
        {
            if (!IsFinite(relativeMeters))
            {
                return IsFinite(previousTop) ? Clamp(previousTop, MinimumTop, MaximumTop) : CenterTop;
            }

            // SimHub/iRacing RelativeDistanceToPlayer is negative ahead and positive behind.
            return Clamp(CenterTop + relativeMeters * SidePixelsPerMeter, MinimumTop, MaximumTop);
        }

        public static double SmoothSideTop(double currentTop, double targetTop, double elapsedSeconds)
        {
            if (!IsFinite(targetTop))
            {
                return IsFinite(currentTop) ? Clamp(currentTop, MinimumTop, MaximumTop) : CenterTop;
            }

            if (!IsFinite(currentTop)) currentTop = targetTop;
            elapsedSeconds = Clamp(elapsedSeconds, 0.0, 0.25);
            double alpha = 1.0 - Math.Exp(-elapsedSeconds / SidePositionTimeConstantSeconds);
            return Clamp(currentTop + (targetTop - currentTop) * alpha, MinimumTop, MaximumTop);
        }

        public static double CalculateCenterCarTop(double relativeMeters)
        {
            if (!IsFinite(relativeMeters)) return CenterCarBaseTop;

            double magnitude = Math.Min(Math.Abs(relativeMeters), RadarRangeMeters);
            double normalized = Math.Pow(magnitude / RadarRangeMeters, 0.65);
            double offset = normalized * CenterCarMaximumOffset;
            return Clamp(CenterCarBaseTop + Math.Sign(relativeMeters) * offset, 4.0, 214.0);
        }

        public static double CalculateTop(double distanceMeters, double angle, double previousTop)
        {
            if (!IsFinite(distanceMeters) || distanceMeters <= 0 || !IsFinite(angle))
            {
                return IsFinite(previousTop) ? Clamp(previousTop, MinimumTop, MaximumTop) : CenterTop;
            }

            double radians = Math.Abs(angle) <= Math.PI * 2.0 + 0.01
                ? angle
                : angle * Math.PI / 180.0;
            return CalculateTopFromRelativeMeters(distanceMeters * Math.Cos(radians), previousTop);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
