$ErrorActionPreference = 'Stop'

$path = Join-Path $PSScriptRoot '..\SimHubPlugin\IRacingRadarPlugin\IRacingRadarPlugin.cs'
$source = [System.IO.File]::ReadAllText((Resolve-Path $path))
$nl = [Environment]::NewLine

$source = $source.Replace(
    'FindNearestOpponent(telemetry, settings.RadarRangeMeters, true)',
    'FindNearestOpponent(telemetry, settings, true)')
$source = $source.Replace(
    'FindNearestOpponent(telemetry, settings.RadarRangeMeters, false)',
    'FindNearestOpponent(telemetry, settings, false)')
$source = $source.Replace(
    'CalculateProximityOpacity(frontMeters, settings.RadarRangeMeters)',
    'CalculateProximityOpacity(frontMeters, frontSeconds, settings)')
$source = $source.Replace(
    'CalculateProximityOpacity(rearMeters, settings.RadarRangeMeters)',
    'CalculateProximityOpacity(rearMeters, rearSeconds, settings)')

$findMethod = @'
        private static Opponent FindNearestOpponent(StatusDataBase telemetry, RadarSettings settings, bool ahead)
        {
            Opponent nearest = null;
            double nearestMagnitude = double.MaxValue;
            if (telemetry.Opponents == null) return null;

            foreach (Opponent opponent in telemetry.Opponents)
            {
                if (opponent == null || opponent.IsPlayer || !opponent.IsConnected) continue;
                if (opponent.IsCarInGarage.HasValue && opponent.IsCarInGarage.Value) continue;
                if (!opponent.RelativeDistanceToPlayer.HasValue) continue;

                double meters = opponent.RelativeDistanceToPlayer.Value;
                if (!IsFinite(meters)) continue;
                if (ahead ? meters >= -0.25 : meters <= 0.25) continue;

                double seconds = ReadOpponentGap(opponent);
                bool distanceTriggered = Math.Abs(meters) <= settings.RadarRangeMeters;
                bool timeTriggered = IsFinite(seconds) && Math.Abs(seconds) <= settings.TimeAlertSeconds;
                bool triggered = settings.DisplayMode == "Distance" ? distanceTriggered :
                    settings.DisplayMode == "Time" ? timeTriggered :
                    distanceTriggered || timeTriggered;
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

'@
$source = [regex]::Replace(
    $source,
    '        private static Opponent FindNearestOpponent[\s\S]*?(?=        private static double ReadOpponentDistance)',
    $findMethod,
    1)

$opacityMethod = @'
        private static double CalculateProximityOpacity(double meters, double seconds, RadarSettings settings)
        {
            double distanceOpacity = CalculateThresholdOpacity(Math.Abs(meters), settings.RadarRangeMeters);
            double timeOpacity = CalculateThresholdOpacity(Math.Abs(seconds), settings.TimeAlertSeconds);

            if (settings.DisplayMode == "Distance") return distanceOpacity;
            if (settings.DisplayMode == "Time") return timeOpacity;
            return Math.Max(distanceOpacity, timeOpacity);
        }

        private static double CalculateThresholdOpacity(double value, double threshold)
        {
            if (!IsFinite(value) || threshold <= 0.0 || value > threshold) return 0.0;
            double normalized = 1.0 - value / threshold;
            return 25.0 + normalized * 75.0;
        }

'@
$source = [regex]::Replace(
    $source,
    '        private static double CalculateProximityOpacity[\s\S]*?(?=        private string BuildDisplayText)',
    $opacityMethod,
    1)

if ($source -notmatch 'Add\("TimeAlertSeconds"') {
    $source = $source.Replace(
        '            Add("RadarRangeMeters", settings.RadarRangeMeters, "Configured maximum radar distance.");',
        ('            Add("RadarRangeMeters", settings.RadarRangeMeters, "Configured distance alert threshold.");' + $nl +
         '            Add("TimeAlertSeconds", settings.TimeAlertSeconds, "Configured time-gap alert threshold.");'))
}

if ($source -notmatch 'Set\("TimeAlertSeconds"') {
    $source = $source.Replace(
        '            Set("RadarRangeMeters", settings.RadarRangeMeters);',
        ('            Set("RadarRangeMeters", settings.RadarRangeMeters);' + $nl +
         '            Set("TimeAlertSeconds", settings.TimeAlertSeconds);'))
}

$source = $source.Replace('Distance-based opacity for front alerts.', 'Active-trigger opacity for front alerts.')
$source = $source.Replace('Distance-based opacity for rear alerts.', 'Active-trigger opacity for rear alerts.')

[System.IO.File]::WriteAllText((Resolve-Path $path), $source, [System.Text.UTF8Encoding]::new($false))
