using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace User.IRacingRadarPlugin
{
    [PluginName("iRacing Radar")]
    [PluginAuthor("ECoreBit")]
    [PluginDescription("A compact left/right proximity radar for iRacing overlays.")]
    public sealed class IRacingRadarPlugin : IPlugin, IDataPlugin
    {
        private const double HoldSeconds = 0.45;
        private const double PositionRangeMeters = 18.0;
        private const double ColorTransitionMeters = 2.5;
        private const double MotionSmoothingSeconds = 0.30;
        private const double MotionEnterMetersPerSecond = 0.45;
        private const double MotionNeutralMetersPerSecond = 0.20;
        private const double CatchEstimateMinClosingSpeed = 2.0;
        private const double CatchEstimateMaxSeconds = 15.0;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private SideState left = SideState.Hidden;
        private SideState right = SideState.Hidden;
        private bool frontVisible;
        private bool rearVisible;
        private bool frontFarVisible;
        private bool rearFarVisible;
        private bool frontFarLabelVisible;
        private bool rearFarLabelVisible;
        private double leftVisualOpacity;
        private double rightVisualOpacity;
        private double lastSideVisualUpdate;
        private double frontMeters;
        private double rearMeters;
        private double frontSeconds;
        private double frontCatchSeconds;
        private double rearSeconds;
        private double frontProximityOpacity;
        private double rearProximityOpacity;
        private double frontNearProgress;
        private double rearNearProgress;
        private double frontNearBlend;
        private double rearNearBlend;
        private double frontFarProgress;
        private double rearFarProgress;
        private MotionTracker frontMotion = MotionTracker.Empty;
        private MotionTracker rearMotion = MotionTracker.Empty;
        private double lastProgressUpdate;
        private readonly string settingsPath = ResolveSettingsPath();
        private RadarSettings settings = RadarSettings.Default();
        private double nextSettingsRefresh;
        private double radarVisualOpacity;
        private bool radarVisible;

        public PluginManager PluginManager { get; set; }

        private static string ResolveSettingsPath()
        {
            string dllDirectory = System.IO.Path.GetDirectoryName(typeof(IRacingRadarPlugin).Assembly.Location);
            string dllLocal = System.IO.Path.Combine(dllDirectory ?? string.Empty, "IRacingRadar.settings.ini");
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string preferred = System.IO.Path.Combine(documents, "iRacingRadar", "IRacingRadar.settings.ini");
            string legacy = System.IO.Path.Combine(documents, "iraing_Rader", "IRacingRadar.settings.ini");

            if (System.IO.File.Exists(dllLocal)) return dllLocal;
            if (System.IO.File.Exists(preferred)) return preferred;
            if (System.IO.File.Exists(legacy)) return legacy;
            return dllLocal;
        }

        public void Init(PluginManager pluginManager)
        {
            PluginManager = pluginManager;
            settings = RadarSettings.Load(settingsPath);
            Add("Connected", false, "True while iRacing telemetry is active.");
            Add("LeftVisible", false, "Show the left opponent block.");
            Add("RightVisible", false, "Show the right opponent block.");
            Add("LeftVisualOpacity", 0.0, "Smoothed opacity for the real left-side alert.");
            Add("RightVisualOpacity", 0.0, "Smoothed opacity for the real right-side alert.");
            Add("LeftTop", RadarMath.CenterTop, "Vertical overlay position of the left opponent block.");
            Add("RightTop", RadarMath.CenterTop, "Vertical overlay position of the right opponent block.");
            Add("LeftRelativeMeters", 0.0, "Signed fore/aft distance of the left-side opponent.");
            Add("RightRelativeMeters", 0.0, "Signed fore/aft distance of the right-side opponent.");
            Add("FrontVisible", false, "Show the nearest opponent ahead.");
            Add("RearVisible", false, "Show the nearest opponent behind.");
            Add("FrontFarVisible", false, "Show the green far-distance front semicircle.");
            Add("RearFarVisible", false, "Show the green far-distance rear semicircle.");
            Add("FrontFarLabelVisible", false, "Show the far-distance front text independently from the green arc.");
            Add("RearFarLabelVisible", false, "Show the far-distance rear text independently from the green arc.");
            Add("FrontTop", 35.0, "Vertical position of the nearest opponent ahead.");
            Add("RearTop", 151.0, "Vertical position of the nearest opponent behind.");
            Add("FrontRelativeMeters", 0.0, "Signed distance to the nearest opponent ahead.");
            Add("RearRelativeMeters", 0.0, "Signed distance to the nearest opponent behind.");
            Add("FrontRelativeSeconds", 0.0, "Time gap to the nearest opponent ahead.");
            Add("RearRelativeSeconds", 0.0, "Time gap to the nearest opponent behind.");
            Add("FrontDisplayText", "--", "Configured front distance/time label.");
            Add("RearDisplayText", "--", "Configured rear distance/time label.");
            Add("FrontProximityOpacity", 0.0, "Active-trigger opacity for front alerts.");
            Add("RearProximityOpacity", 0.0, "Active-trigger opacity for rear alerts.");
            Add("FrontNearProgress", 0.0, "Front red fan expansion from 0 to 100.");
            Add("RearNearProgress", 0.0, "Rear red fan expansion from 0 to 100.");
            Add("FrontNearBlend", 0.0, "Front red/green transition blend.");
            Add("RearNearBlend", 0.0, "Rear red/green transition blend.");
            Add("FrontFarProgress", 0.0, "Remaining front green arc length from 0 to 100.");
            Add("RearFarProgress", 0.0, "Remaining rear green arc length from 0 to 100.");
            Add("FrontMotionState", 0, "1=closing, -1=separating, 0=steady.");
            Add("RearMotionState", 0, "1=closing, -1=separating, 0=steady.");
            Add("FrontClosingSpeed", 0.0, "Smoothed front closing speed in m/s.");
            Add("FrontCatchSeconds", 0.0, "Estimated seconds until catching the front car.");
            Add("CatchEstimateEnabled", settings.CatchEstimateEnabled, "Configured catch-time estimate switch.");
            Add("RearClosingSpeed", 0.0, "Smoothed rear closing speed in m/s.");
            Add("LabelFontSize", settings.LabelFontSize, "Configured front/rear label font size.");
            Add("DisplayMode", settings.DisplayMode, "None, Distance, Time, or Both." );
            Add("RadarVisible", false, "True while the smoothed radar opacity is above zero.");
            Add("RadarVisualOpacity", 0.0, "Distance/time-driven overall radar opacity from 0 to 100.");
            Add("RadarRangeMeters", settings.RadarRangeMeters, "Configured distance alert threshold.");
            Add("TimeAlertSeconds", settings.TimeAlertSeconds, "Configured time-gap alert threshold.");
            Add("RadarFadeBandPercent", settings.RadarFadeBandPercent, "Configured outer opacity transition band percentage.");
            Add("FrontGreenArcEnabled", settings.FrontGreenArcEnabled, "Configured front green arc switch.");
            Add("RearGreenArcEnabled", settings.RearGreenArcEnabled, "Configured rear green arc switch.");
            Add("NearDistanceMeters", settings.NearDistanceMeters, "Configured red marker distance.");
            Add("OverlayOpacity", settings.OverlayOpacity, "Configured overlay opacity percentage.");
            Add("SettingsPath", settingsPath, "Path to the live radar settings file.");
            Add("RawCarLeftRight", 0, "Raw iRacing CarLeftRight value.");
            Add("StatusText", "waiting for iRacing", "Radar diagnostic state.");
            SimHub.Logging.Current.Info("iRacing Radar plugin started");
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            try
            {
                StatusDataBase telemetry = data.NewData;
                bool connected = data.GameRunning && telemetry != null;
                int nativeLeftRight = ReadNativeLeftRight();
                double now = clock.Elapsed.TotalSeconds;
                RefreshSettings(now);

                if (!connected)
                {
                    left = SideState.Hidden;
                    right = SideState.Hidden;
                    frontVisible = false;
                    rearVisible = false;
                    frontFarVisible = false;
                    rearFarVisible = false;
                    frontFarLabelVisible = false;
                    rearFarLabelVisible = false;
                    leftVisualOpacity = 0.0;
                    rightVisualOpacity = 0.0;
                    lastSideVisualUpdate = 0.0;
                    frontMeters = double.NaN;
                    rearMeters = double.NaN;
                    frontSeconds = double.NaN;
                    frontCatchSeconds = double.NaN;
                    rearSeconds = double.NaN;
                    frontProximityOpacity = 0.0;
                    rearProximityOpacity = 0.0;
                    frontNearProgress = 0.0;
                    rearNearProgress = 0.0;
                    frontNearBlend = 0.0;
                    rearNearBlend = 0.0;
                    frontFarProgress = 0.0;
                    rearFarProgress = 0.0;
                    frontMotion = MotionTracker.Empty;
                    rearMotion = MotionTracker.Empty;
                    lastProgressUpdate = 0.0;
                    radarVisualOpacity = 0.0;
                    radarVisible = false;
                    Publish(false, nativeLeftRight);
                    return;
                }
                bool leftDetected = telemetry.SpotterCarLeft != 0 ||
                    nativeLeftRight == 2 || nativeLeftRight == 4 || nativeLeftRight == 5;
                bool rightDetected = telemetry.SpotterCarRight != 0 ||
                    nativeLeftRight == 3 || nativeLeftRight == 4 || nativeLeftRight == 6;
                double sideVisualElapsed = lastSideVisualUpdate > 0.0 ? now - lastSideVisualUpdate : 0.016;
                lastSideVisualUpdate = now;
                leftVisualOpacity = SmoothSideOpacity(leftVisualOpacity, leftDetected ? 100.0 : 0.0, sideVisualElapsed);
                rightVisualOpacity = SmoothSideOpacity(rightVisualOpacity, rightDetected ? 100.0 : 0.0, sideVisualElapsed);

                double[] nearby = GetRelativeDistances(telemetry, PositionRangeMeters);
                Opponent frontOpponent = FindNearestOpponent(telemetry, settings, true);
                Opponent rearOpponent = FindNearestOpponent(telemetry, settings, false);
                frontMeters = ReadOpponentDistance(frontOpponent);
                rearMeters = ReadOpponentDistance(rearOpponent);
                frontSeconds = ReadOpponentGap(frontOpponent);
                rearSeconds = ReadOpponentGap(rearOpponent);
                frontMotion = UpdateMotion(frontMotion, frontOpponent, frontMeters, now);
                rearMotion = UpdateMotion(rearMotion, rearOpponent, rearMeters, now);
                frontCatchSeconds = CalculateCatchSeconds(frontMeters, frontMotion.ClosingSpeed);
                frontProximityOpacity = CalculateProximityOpacity(frontMeters, frontSeconds, settings);
                rearProximityOpacity = CalculateProximityOpacity(rearMeters, rearSeconds, settings);
                double progressElapsed = lastProgressUpdate > 0.0 ? now - lastProgressUpdate : 0.016;
                lastProgressUpdate = now;
                double frontAlertProgress = frontOpponent != null
                    ? CalculateAlertProgress(frontMeters, frontSeconds, settings) : 0.0;
                double rearAlertProgress = rearOpponent != null
                    ? CalculateAlertProgress(rearMeters, rearSeconds, settings) : 0.0;
                double nearStartProgress = CalculateNearStartProgress(settings);
                double nearTransitionProgress = Math.Max(2.0,
                    ColorTransitionMeters / settings.RadarRangeMeters * 100.0);
                double frontProgressTarget = CalculateNearProgress(frontAlertProgress, nearStartProgress);
                double rearProgressTarget = CalculateNearProgress(rearAlertProgress, nearStartProgress);
                double frontNearBlendTarget = CalculateNearBlend(frontAlertProgress, nearStartProgress, nearTransitionProgress);
                double rearNearBlendTarget = CalculateNearBlend(rearAlertProgress, nearStartProgress, nearTransitionProgress);
                frontNearProgress = SmoothProgress(frontNearProgress, frontProgressTarget, progressElapsed);
                rearNearProgress = SmoothProgress(rearNearProgress, rearProgressTarget, progressElapsed);
                frontNearBlend = SmoothProgress(frontNearBlend, frontNearBlendTarget, progressElapsed);
                rearNearBlend = SmoothProgress(rearNearBlend, rearNearBlendTarget, progressElapsed);
                frontFarProgress = SmoothProgress(frontFarProgress,
                    frontOpponent != null ? CalculateFarProgress(frontAlertProgress, nearStartProgress) : 0.0,
                    progressElapsed);
                rearFarProgress = SmoothProgress(rearFarProgress,
                    rearOpponent != null ? CalculateFarProgress(rearAlertProgress, nearStartProgress) : 0.0,
                    progressElapsed);
                bool sideClear = !leftDetected && !rightDetected;
                frontVisible = frontOpponent != null && frontNearBlendTarget > 0.5 && sideClear;
                rearVisible = rearOpponent != null && rearNearBlendTarget > 0.5 && sideClear;
                frontFarLabelVisible = frontOpponent != null && frontNearBlendTarget < 99.5 && sideClear &&
                    settings.FrontGreenArcEnabled;
                rearFarLabelVisible = rearOpponent != null && rearNearBlendTarget < 99.5 && sideClear &&
                    settings.RearGreenArcEnabled;
                frontFarVisible = frontFarLabelVisible;
                rearFarVisible = rearFarLabelVisible;
                double leftRelative = double.NaN;
                double rightRelative = double.NaN;

                if (leftDetected && rightDetected)
                {
                    AssignTwoSides(nearby, out leftRelative, out rightRelative);
                }
                else if (leftDetected && nearby.Length > 0)
                {
                    leftRelative = nearby[0];
                }
                else if (rightDetected && nearby.Length > 0)
                {
                    rightRelative = nearby[0];
                }

                left = UpdateSide(left, leftDetected, leftRelative, now, sideVisualElapsed);
                right = UpdateSide(right, rightDetected, rightRelative, now, sideVisualElapsed);

                double frontRadarOpacity = CalculateDirectionalRadarOpacity(
                    frontOpponent != null && sideClear, settings.FrontGreenArcEnabled,
                    frontVisible, frontNearBlendTarget, frontProximityOpacity);
                double rearRadarOpacity = CalculateDirectionalRadarOpacity(
                    rearOpponent != null && sideClear, settings.RearGreenArcEnabled,
                    rearVisible, rearNearBlendTarget, rearProximityOpacity);
                double sideRadarOpacity = leftDetected || rightDetected ? 100.0 : 0.0;
                double radarTargetOpacity = Math.Max(sideRadarOpacity,
                    Math.Max(frontRadarOpacity, rearRadarOpacity));
                radarVisualOpacity = radarTargetOpacity;
                radarVisible = radarVisualOpacity > 0.1;
                Publish(true, nativeLeftRight);
            }
            catch (Exception ex)
            {
                Set("StatusText", "radar error: " + ex.GetType().Name);
            }
        }

        public void End(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("iRacing Radar plugin stopped");
        }

        private static Opponent FindNearestOpponent(StatusDataBase telemetry, RadarSettings settings, bool ahead)
        {
            Opponent nearest = null;
            double nearestMagnitude = double.MaxValue;
            IEnumerable<Opponent> opponents = ahead
                ? telemetry.OpponentsAheadOnTrack
                : telemetry.OpponentsBehindOnTrack;
            if (opponents == null) return null;

            foreach (Opponent opponent in opponents)
            {
                if (opponent == null || opponent.IsPlayer || !opponent.IsConnected) continue;
                if (opponent.IsCarInGarage.HasValue && opponent.IsCarInGarage.Value) continue;
                if (opponent.IsCarInPit || opponent.IsCarInPitLane || opponent.StandingStillInPitLane) continue;
                if (!opponent.RelativeDistanceToPlayer.HasValue) continue;

                double meters = opponent.RelativeDistanceToPlayer.Value;
                if (!IsFinite(meters)) continue;
                if (ahead ? meters >= -0.25 : meters <= 0.25) continue;

                double seconds = ReadOpponentGap(opponent);
                bool triggered = ShouldTrigger(settings, meters, seconds);
                if (!triggered) continue;

                double magnitude = Math.Abs(meters);
                if (magnitude < nearestMagnitude)
                {
                    nearest = opponent;
                    nearestMagnitude = magnitude;
                }
            }

            return nearest;
        }
        private static bool ShouldTrigger(RadarSettings settings, double meters, double seconds)
        {
            bool distanceTriggered = IsFinite(meters) && Math.Abs(meters) <= settings.RadarRangeMeters;
            bool timeTriggered = IsFinite(seconds) && Math.Abs(seconds) <= settings.TimeAlertSeconds;
            if (settings.DisplayMode == "Distance") return distanceTriggered;
            if (settings.DisplayMode == "Time") return timeTriggered;
            return distanceTriggered || timeTriggered;
        }
        private static double ReadOpponentDistance(Opponent opponent)
        {
            return opponent != null && opponent.RelativeDistanceToPlayer.HasValue
                ? opponent.RelativeDistanceToPlayer.Value
                : double.NaN;
        }

        private static double ReadOpponentGap(Opponent opponent)
        {
            return opponent != null && opponent.RelativeGapToPlayer.HasValue && IsFinite(opponent.RelativeGapToPlayer.Value)
                ? opponent.RelativeGapToPlayer.Value
                : double.NaN;
        }

        private static double CalculateProximityOpacity(double meters, double seconds, RadarSettings settings)
        {
            double distanceOpacity = CalculateThresholdOpacity(Math.Abs(meters), settings.RadarRangeMeters,
                settings.RadarFadeBandPercent);
            double timeOpacity = CalculateThresholdOpacity(Math.Abs(seconds), settings.TimeAlertSeconds,
                settings.RadarFadeBandPercent);

            if (settings.DisplayMode == "Distance") return distanceOpacity;
            if (settings.DisplayMode == "Time") return timeOpacity;
            return Math.Max(distanceOpacity, timeOpacity);
        }

        private static double CalculateThresholdOpacity(double value, double threshold, double fadeBandPercent)
        {
            if (!IsFinite(value) || threshold <= 0.0 || value > threshold) return 0.0;
            double ratio = Math.Max(0.01, Math.Min(0.50, fadeBandPercent / 100.0));
            double fadeBand = Math.Max(0.0001, threshold * ratio);
            double fullOpacityAt = threshold - fadeBand;
            if (value <= fullOpacityAt) return 100.0;
            return Math.Max(0.0, Math.Min(100.0,
                (threshold - value) / fadeBand * 100.0));
        }
        private static double CalculateDirectionalRadarOpacity(bool opponentPresent, bool greenArcEnabled,
            bool nearVisible, double nearBlend, double proximityOpacity)
        {
            if (!opponentPresent) return 0.0;
            double opacity = Math.Max(0.0, Math.Min(100.0, proximityOpacity));
            if (greenArcEnabled) return opacity;
            if (!nearVisible) return 0.0;
            double blend = Math.Max(0.0, Math.Min(100.0, nearBlend));
            return opacity * blend / 100.0;
        }

        private static double CalculateAlertProgress(double meters, double seconds, RadarSettings settings)
        {
            double distanceProgress = CalculateThresholdProgress(Math.Abs(meters), settings.RadarRangeMeters);
            double timeProgress = CalculateThresholdProgress(Math.Abs(seconds), settings.TimeAlertSeconds);
            if (settings.DisplayMode == "Distance") return distanceProgress;
            if (settings.DisplayMode == "Time") return timeProgress;
            return Math.Max(distanceProgress, timeProgress);
        }

        private static double CalculateThresholdProgress(double value, double threshold)
        {
            if (!IsFinite(value) || threshold <= 0.0 || value > threshold) return 0.0;
            return Math.Max(0.0, Math.Min(100.0, (1.0 - value / threshold) * 100.0));
        }

        private static double CalculateNearStartProgress(RadarSettings settings)
        {
            return Math.Max(1.0, Math.Min(95.0,
                (1.0 - settings.NearDistanceMeters / settings.RadarRangeMeters) * 100.0));
        }

        private static double SmoothSideOpacity(double current, double target, double elapsed)
        {
            elapsed = Math.Max(0.0, Math.Min(elapsed, 0.25));
            double timeConstant = target > current ? 0.07 : 0.18;
            double alpha = 1.0 - Math.Exp(-elapsed / timeConstant);
            return current + (target - current) * alpha;
        }

        private static double CalculateNearProgress(double alertProgress, double nearStartProgress)
        {
            if (!IsFinite(alertProgress) || alertProgress <= nearStartProgress) return 0.0;
            return Math.Min(100.0,
                (alertProgress - nearStartProgress) / (100.0 - nearStartProgress) * 100.0);
        }

        private static double CalculateFarProgress(double alertProgress, double nearStartProgress)
        {
            if (!IsFinite(alertProgress) || nearStartProgress <= 0.0) return 0.0;
            if (alertProgress >= nearStartProgress) return 0.0;
            return Math.Max(0.0, Math.Min(100.0,
                (1.0 - alertProgress / nearStartProgress) * 100.0));
        }

        private static double SmoothProgress(double current, double target, double elapsed)
        {
            if (!IsFinite(current)) current = target;
            elapsed = Math.Max(0.0, Math.Min(elapsed, 0.25));
            double alpha = 1.0 - Math.Exp(-elapsed / 0.12);
            return current + (target - current) * alpha;
        }

        private static double CalculateNearBlend(double alertProgress, double nearStartProgress, double transition)
        {
            if (!IsFinite(alertProgress)) return 0.0;
            double start = nearStartProgress - transition;
            double end = nearStartProgress + transition;
            if (alertProgress <= start) return 0.0;
            if (alertProgress >= end) return 100.0;
            double t = (alertProgress - start) / (end - start);
            t = t * t * (3.0 - 2.0 * t);
            return t * 100.0;
        }
        private static MotionTracker UpdateMotion(
            MotionTracker previous, Opponent opponent, double meters, double now)
        {
            if (opponent == null || !IsFinite(meters)) return MotionTracker.Empty;
            string opponentKey = GetOpponentKey(opponent);
            if (!previous.Valid || previous.OpponentKey != opponentKey || now <= previous.SampleTime)
                return new MotionTracker(true, opponentKey, meters, now, 0.0, 0);

            double elapsed = now - previous.SampleTime;
            if (elapsed > 0.5)
                return new MotionTracker(true, opponentKey, meters, now, 0.0, 0);

            double rawClosingSpeed = CalculateClosingSpeed(previous.DistanceMeters, meters, elapsed);
            rawClosingSpeed = Math.Max(-50.0, Math.Min(50.0, rawClosingSpeed));
            double alpha = 1.0 - Math.Exp(-elapsed / MotionSmoothingSeconds);
            double smoothed = previous.ClosingSpeed + (rawClosingSpeed - previous.ClosingSpeed) * alpha;
            int state = ClassifyMotion(smoothed, previous.State);
            return new MotionTracker(true, opponentKey, meters, now, smoothed, state);
        }

        private static double CalculateClosingSpeed(double previousMeters, double currentMeters, double elapsed)
        {
            if (!IsFinite(previousMeters) || !IsFinite(currentMeters) || elapsed <= 0.0) return 0.0;
            return (Math.Abs(previousMeters) - Math.Abs(currentMeters)) / elapsed;
        }

        private static int ClassifyMotion(double closingSpeed, int previousState)
        {
            if (closingSpeed >= MotionEnterMetersPerSecond) return 1;
            if (closingSpeed <= -MotionEnterMetersPerSecond) return -1;
            if (Math.Abs(closingSpeed) <= MotionNeutralMetersPerSecond) return 0;
            return previousState;
        }

        private static string GetOpponentKey(Opponent opponent)
        {
            if (!string.IsNullOrEmpty(opponent.Id)) return opponent.Id;
            return (opponent.CarNumber ?? string.Empty) + "|" + (opponent.Name ?? string.Empty);
        }
        private static double CalculateCatchSeconds(double meters, double closingSpeed)
        {
            if (!IsFinite(meters) || meters >= -0.25 || !IsFinite(closingSpeed) ||
                closingSpeed < CatchEstimateMinClosingSpeed) return double.NaN;
            double seconds = Math.Abs(meters) / closingSpeed;
            return seconds <= CatchEstimateMaxSeconds ? seconds : double.NaN;
        }

        private string BuildFrontDisplayText(double meters, double seconds)
        {
            string text = BuildDisplayText("F", meters, seconds);
            if (text.Length == 0 || !settings.CatchEstimateEnabled || !IsFinite(frontCatchSeconds)) return text;
            return text + "\nCatch " + frontCatchSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        }

        private string BuildDisplayText(string prefix, double meters, double seconds)
        {
            if (settings.DisplayMode == "None") return string.Empty;

            string distance = IsFinite(meters)
                ? Math.Abs(meters).ToString("0", CultureInfo.InvariantCulture) + "m"
                : "--m";
            string time = IsFinite(seconds)
                ? Math.Abs(seconds).ToString("0.0", CultureInfo.InvariantCulture) + "s"
                : "--.-s";

            if (settings.DisplayMode == "Distance") return distance;
            if (settings.DisplayMode == "Time") return time;
            return distance + " / " + time;
        }
        private static double[] GetRelativeDistances(StatusDataBase telemetry, double rangeMeters)
        {
            List<double> result = new List<double>();
            List<Opponent> opponents = new List<Opponent>();
            AddUniqueOpponents(opponents, telemetry.OpponentsAheadOnTrack);
            AddUniqueOpponents(opponents, telemetry.OpponentsBehindOnTrack);

            foreach (Opponent opponent in opponents)
            {
                if (opponent == null || opponent.IsPlayer || !opponent.IsConnected) continue;
                if (opponent.IsCarInGarage.HasValue && opponent.IsCarInGarage.Value) continue;
                if (opponent.IsCarInPit || opponent.IsCarInPitLane || opponent.StandingStillInPitLane) continue;
                if (!opponent.RelativeDistanceToPlayer.HasValue) continue;

                double meters = opponent.RelativeDistanceToPlayer.Value;
                if (double.IsNaN(meters) || double.IsInfinity(meters)) continue;
                if (Math.Abs(meters) > rangeMeters) continue;
                result.Add(meters);
            }

            result.Sort(delegate(double a, double b)
            {
                return Math.Abs(a).CompareTo(Math.Abs(b));
            });
            return result.ToArray();
        }

        private static void AddUniqueOpponents(List<Opponent> target, IEnumerable<Opponent> source)
        {
            if (source == null) return;
            foreach (Opponent opponent in source)
                if (opponent != null && !target.Contains(opponent)) target.Add(opponent);
        }

        private static double FindNearestAhead(double[] distances)
        {
            foreach (double value in distances) if (value < -0.25) return value;
            return double.NaN;
        }

        private static double FindNearestBehind(double[] distances)
        {
            foreach (double value in distances) if (value > 0.25) return value;
            return double.NaN;
        }

        private void AssignTwoSides(double[] nearby, out double leftMeters, out double rightMeters)
        {
            leftMeters = double.NaN;
            rightMeters = double.NaN;
            if (nearby.Length == 0) return;
            if (nearby.Length == 1)
            {
                leftMeters = nearby[0];
                rightMeters = nearby[0];
                return;
            }

            // Preserve side continuity when possible so two opponents do not swap
            // sides from one telemetry frame to the next.
            double a = nearby[0];
            double b = nearby[1];
            double keep = Math.Abs(a - left.RelativeMeters) + Math.Abs(b - right.RelativeMeters);
            double swap = Math.Abs(b - left.RelativeMeters) + Math.Abs(a - right.RelativeMeters);
            if (left.Visible && right.Visible && swap < keep)
            {
                leftMeters = b;
                rightMeters = a;
            }
            else
            {
                leftMeters = a;
                rightMeters = b;
            }
        }

        private static SideState UpdateSide(
            SideState previous,
            bool detected,
            double relativeMeters,
            double now,
            double elapsed)
        {
            if (detected)
            {
                double resolved = IsFinite(relativeMeters) ? relativeMeters : previous.RelativeMeters;
                double targetTop = RadarMath.CalculateTopFromRelativeMeters(resolved, previous.Top);
                double top = previous.Visible
                    ? RadarMath.SmoothSideTop(previous.Top, targetTop, elapsed)
                    : targetTop;
                return new SideState(true, top, resolved, now + HoldSeconds);
            }

            if (previous.Visible && now < previous.HoldUntil) return previous;
            return SideState.Hidden;
        }

        private void Publish(bool connected, int nativeLeftRight)
        {
            Set("Connected", connected);
            Set("RadarVisible", radarVisible);
            Set("RadarVisualOpacity", radarVisualOpacity);
            Set("RadarRangeMeters", settings.RadarRangeMeters);
            Set("TimeAlertSeconds", settings.TimeAlertSeconds);
            Set("RadarFadeBandPercent", settings.RadarFadeBandPercent);
            Set("FrontGreenArcEnabled", settings.FrontGreenArcEnabled);
            Set("RearGreenArcEnabled", settings.RearGreenArcEnabled);
            Set("NearDistanceMeters", settings.NearDistanceMeters);
            Set("OverlayOpacity", settings.OverlayOpacity);
            Set("LeftVisible", left.Visible);
            Set("RightVisible", right.Visible);
            Set("LeftVisualOpacity", leftVisualOpacity);
            Set("RightVisualOpacity", rightVisualOpacity);
            Set("LeftTop", left.Top);
            Set("RightTop", right.Top);
            Set("LeftRelativeMeters", left.RelativeMeters);
            Set("RightRelativeMeters", right.RelativeMeters);
            Set("FrontVisible", frontVisible);
            Set("RearVisible", rearVisible);
            Set("FrontFarVisible", frontFarVisible);
            Set("RearFarVisible", rearFarVisible);
            Set("FrontFarLabelVisible", frontFarLabelVisible);
            Set("RearFarLabelVisible", rearFarLabelVisible);
            Set("FrontTop", RadarMath.CalculateCenterCarTop(frontMeters));
            Set("RearTop", RadarMath.CalculateCenterCarTop(rearMeters));
            Set("FrontRelativeMeters", IsFinite(frontMeters) ? frontMeters : 0.0);
            Set("RearRelativeMeters", IsFinite(rearMeters) ? rearMeters : 0.0);
            Set("FrontRelativeSeconds", IsFinite(frontSeconds) ? frontSeconds : 0.0);
            Set("RearRelativeSeconds", IsFinite(rearSeconds) ? rearSeconds : 0.0);
            Set("FrontDisplayText", BuildFrontDisplayText(frontMeters, frontSeconds));
            Set("RearDisplayText", BuildDisplayText("B", rearMeters, rearSeconds));
            Set("FrontProximityOpacity", frontProximityOpacity);
            Set("RearProximityOpacity", rearProximityOpacity);
            Set("FrontNearProgress", frontNearProgress);
            Set("RearNearProgress", rearNearProgress);
            Set("FrontNearBlend", frontNearBlend);
            Set("RearNearBlend", rearNearBlend);
            Set("FrontFarProgress", frontFarProgress);
            Set("RearFarProgress", rearFarProgress);
            Set("FrontMotionState", frontMotion.State);
            Set("RearMotionState", rearMotion.State);
            Set("FrontClosingSpeed", frontMotion.ClosingSpeed);
            Set("FrontCatchSeconds", IsFinite(frontCatchSeconds) ? frontCatchSeconds : 0.0);
            Set("CatchEstimateEnabled", settings.CatchEstimateEnabled);
            Set("RearClosingSpeed", rearMotion.ClosingSpeed);
            Set("LabelFontSize", settings.LabelFontSize);
            Set("DisplayMode", settings.DisplayMode);
            Set("RawCarLeftRight", nativeLeftRight);

            string state = !connected ? "waiting for iRacing" :
                left.Visible && right.Visible ? "LEFT + RIGHT" :
                left.Visible ? "LEFT" : right.Visible ? "RIGHT" : "CLEAR";
            Set("StatusText", string.Format(CultureInfo.InvariantCulture,
                "{0} | L={1:+0.0;-0.0;0.0}m R={2:+0.0;-0.0;0.0}m | LR={3}",
                state, left.RelativeMeters, right.RelativeMeters, nativeLeftRight));
        }

        private void RefreshSettings(double now)
        {
            if (now < nextSettingsRefresh) return;
            nextSettingsRefresh = now + 1.0;

            try
            {
                settings = RadarSettings.Load(settingsPath);
            }
            catch
            {
                // Keep the last valid settings while the file is being saved.
            }
        }

        private int ReadNativeLeftRight()
        {
            string[] paths =
            {
                "DataCorePlugin.GameRawData.Telemetry.CarLeftRight",
                "DataCorePlugin.GameRawData.CarLeftRight",
                "CarLeftRight"
            };

            foreach (string path in paths)
            {
                try
                {
                    object value = PluginManager.GetPropertyValue(path);
                    if (value != null) return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }
            return 0;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void Add<T>(string name, T value, string description)
        {
            PluginManager.AddProperty(name, GetType(), value, description);
        }

        private void Set(string name, object value)
        {
            PluginManager.SetPropertyValue(name, GetType(), value);
        }

        private struct MotionTracker
        {
            public static readonly MotionTracker Empty =
                new MotionTracker(false, string.Empty, 0.0, 0.0, 0.0, 0);

            public MotionTracker(bool valid, string opponentKey, double distanceMeters,
                double sampleTime, double closingSpeed, int state)
            {
                this = default(MotionTracker);
                Valid = valid;
                OpponentKey = opponentKey;
                DistanceMeters = distanceMeters;
                SampleTime = sampleTime;
                ClosingSpeed = closingSpeed;
                State = state;
            }

            public bool Valid { get; private set; }
            public string OpponentKey { get; private set; }
            public double DistanceMeters { get; private set; }
            public double SampleTime { get; private set; }
            public double ClosingSpeed { get; private set; }
            public int State { get; private set; }
        }
        private struct SideState
        {
            public static readonly SideState Hidden =
                new SideState(false, RadarMath.CenterTop, 0.0, 0.0);

            public SideState(bool visible, double top, double relativeMeters, double holdUntil)
            {
                this = default(SideState);
                Visible = visible;
                Top = top;
                RelativeMeters = relativeMeters;
                HoldUntil = holdUntil;
            }

            public bool Visible { get; private set; }
            public double Top { get; private set; }
            public double RelativeMeters { get; private set; }
            public double HoldUntil { get; private set; }
        }
    }
}
