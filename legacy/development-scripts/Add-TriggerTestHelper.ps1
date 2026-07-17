$ErrorActionPreference = 'Stop'

$path = Join-Path $PSScriptRoot '..\SimHubPlugin\IRacingRadarPlugin\IRacingRadarPlugin.cs'
$source = [System.IO.File]::ReadAllText((Resolve-Path $path))

$source = [regex]::Replace(
    $source,
    '\s*bool distanceTriggered = Math\.Abs\(meters\) <= settings\.RadarRangeMeters;\s*bool timeTriggered = IsFinite\(seconds\) && Math\.Abs\(seconds\) <= settings\.TimeAlertSeconds;\s*bool triggered = settings\.DisplayMode == "Distance" \? distanceTriggered :\s*settings\.DisplayMode == "Time" \? timeTriggered :\s*distanceTriggered \|\| timeTriggered;',
    "`r`n                bool triggered = ShouldTrigger(settings, meters, seconds);",
    1)

if ($source -notmatch 'private static bool ShouldTrigger') {
    $helper = @'
        private static bool ShouldTrigger(RadarSettings settings, double meters, double seconds)
        {
            bool distanceTriggered = IsFinite(meters) && Math.Abs(meters) <= settings.RadarRangeMeters;
            bool timeTriggered = IsFinite(seconds) && Math.Abs(seconds) <= settings.TimeAlertSeconds;
            if (settings.DisplayMode == "Distance") return distanceTriggered;
            if (settings.DisplayMode == "Time") return timeTriggered;
            return distanceTriggered || timeTriggered;
        }

'@
    $source = $source.Replace(
        '        private static double ReadOpponentDistance(Opponent opponent)',
        ($helper + '        private static double ReadOpponentDistance(Opponent opponent)'))
}

[System.IO.File]::WriteAllText((Resolve-Path $path), $source, [System.Text.UTF8Encoding]::new($false))
