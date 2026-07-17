$ErrorActionPreference = 'Stop'

$path = Join-Path $PSScriptRoot '..\SimHubPlugin\IRacingRadarPlugin\IRacingRadarPlugin.cs'
$source = [System.IO.File]::ReadAllText((Resolve-Path $path))
$nl = [Environment]::NewLine

if ($source -notmatch 'frontSeconds') {
    $source = $source.Replace(
        '        private double rearMeters;',
        ('        private double rearMeters;' + $nl +
         '        private double frontSeconds;' + $nl +
         '        private double rearSeconds;' + $nl +
         '        private double frontProximityOpacity;' + $nl +
         '        private double rearProximityOpacity;'))
}

if ($source -notmatch 'FrontDisplayText') {
    $source = $source.Replace(
        '            Add("RearRelativeMeters", 0.0, "Signed distance to the nearest opponent behind.");',
        ('            Add("RearRelativeMeters", 0.0, "Signed distance to the nearest opponent behind.");' + $nl +
         '            Add("FrontRelativeSeconds", 0.0, "Time gap to the nearest opponent ahead.");' + $nl +
         '            Add("RearRelativeSeconds", 0.0, "Time gap to the nearest opponent behind.");' + $nl +
         '            Add("FrontDisplayText", "F --", "Configured front distance/time label.");' + $nl +
         '            Add("RearDisplayText", "B --", "Configured rear distance/time label.");' + $nl +
         '            Add("FrontProximityOpacity", 0.0, "Distance-based opacity for front alerts.");' + $nl +
         '            Add("RearProximityOpacity", 0.0, "Distance-based opacity for rear alerts.");' + $nl +
         '            Add("LabelFontSize", settings.LabelFontSize, "Configured front/rear label font size.");' + $nl +
         '            Add("DisplayMode", settings.DisplayMode, "Distance, Time, or Both." );'))
}

$source = $source.Replace(
    ('                    rearMeters = double.NaN;' + $nl + '                    radarVisible = false;'),
    ('                    rearMeters = double.NaN;' + $nl +
     '                    frontSeconds = double.NaN;' + $nl +
     '                    rearSeconds = double.NaN;' + $nl +
     '                    frontProximityOpacity = 0.0;' + $nl +
     '                    rearProximityOpacity = 0.0;' + $nl +
     '                    radarVisible = false;'))

$oldSelection = @'
                double[] radarCars = GetRelativeDistances(telemetry, settings.RadarRangeMeters);
                frontMeters = FindNearestAhead(radarCars);
                rearMeters = FindNearestBehind(radarCars);
'@
$newSelection = @'
                Opponent frontOpponent = FindNearestOpponent(telemetry, settings.RadarRangeMeters, true);
                Opponent rearOpponent = FindNearestOpponent(telemetry, settings.RadarRangeMeters, false);
                frontMeters = ReadOpponentDistance(frontOpponent);
                rearMeters = ReadOpponentDistance(rearOpponent);
                frontSeconds = ReadOpponentGap(frontOpponent);
                rearSeconds = ReadOpponentGap(rearOpponent);
                frontProximityOpacity = CalculateProximityOpacity(frontMeters, settings.RadarRangeMeters);
                rearProximityOpacity = CalculateProximityOpacity(rearMeters, settings.RadarRangeMeters);
'@
$source = $source.Replace($oldSelection.Trim(), $newSelection.Trim())

if ($source -notmatch 'private static Opponent FindNearestOpponent') {
    $helpers = @'
        private static Opponent FindNearestOpponent(StatusDataBase telemetry, double rangeMeters, bool ahead)
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
                if (!IsFinite(meters) || Math.Abs(meters) > rangeMeters) continue;
                if (ahead ? meters >= -0.25 : meters <= 0.25) continue;

                double magnitude = Math.Abs(meters);
                if (magnitude < nearestMagnitude)
                {
                    nearest = opponent;
                    nearestMagnitude = magnitude;
                }
            }

            return nearest;
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

        private static double CalculateProximityOpacity(double meters, double rangeMeters)
        {
            if (!IsFinite(meters) || rangeMeters <= 0.0) return 0.0;
            double normalized = 1.0 - Math.Min(Math.Abs(meters), rangeMeters) / rangeMeters;
            return 25.0 + normalized * 75.0;
        }

        private string BuildDisplayText(string prefix, double meters, double seconds)
        {
            string distance = IsFinite(meters)
                ? Math.Abs(meters).ToString("0", CultureInfo.InvariantCulture) + "m"
                : "--m";
            string time = IsFinite(seconds)
                ? Math.Abs(seconds).ToString("0.0", CultureInfo.InvariantCulture) + "s"
                : "--.-s";

            if (settings.DisplayMode == "Distance") return prefix + " " + distance;
            if (settings.DisplayMode == "Time") return prefix + " " + time;
            return prefix + " " + distance + " / " + time;
        }

'@
    $source = $source.Replace(
        '        private static double[] GetRelativeDistances(StatusDataBase telemetry, double rangeMeters)',
        ($helpers + '        private static double[] GetRelativeDistances(StatusDataBase telemetry, double rangeMeters)'))
}

if ($source -notmatch 'Set\("FrontRelativeSeconds"') {
    $source = $source.Replace(
        '            Set("RearRelativeMeters", IsFinite(rearMeters) ? rearMeters : 0.0);',
        ('            Set("RearRelativeMeters", IsFinite(rearMeters) ? rearMeters : 0.0);' + $nl +
         '            Set("FrontRelativeSeconds", IsFinite(frontSeconds) ? frontSeconds : 0.0);' + $nl +
         '            Set("RearRelativeSeconds", IsFinite(rearSeconds) ? rearSeconds : 0.0);' + $nl +
         '            Set("FrontDisplayText", BuildDisplayText("F", frontMeters, frontSeconds));' + $nl +
         '            Set("RearDisplayText", BuildDisplayText("B", rearMeters, rearSeconds));' + $nl +
         '            Set("FrontProximityOpacity", frontProximityOpacity);' + $nl +
         '            Set("RearProximityOpacity", rearProximityOpacity);' + $nl +
         '            Set("LabelFontSize", settings.LabelFontSize);' + $nl +
         '            Set("DisplayMode", settings.DisplayMode);'))
}

[System.IO.File]::WriteAllText((Resolve-Path $path), $source, [System.Text.UTF8Encoding]::new($false))
