$ErrorActionPreference = 'Stop'

$path = Join-Path $PSScriptRoot '..\SimHubPlugin\IRacingRadarPlugin\IRacingRadarPlugin.cs'
$source = [System.IO.File]::ReadAllText((Resolve-Path $path))
$nl = [Environment]::NewLine

if ($source -notmatch 'ColorTransitionMeters') {
    $source = $source.Replace(
        '        private const double PositionRangeMeters = 18.0;',
        ('        private const double PositionRangeMeters = 18.0;' + $nl +
         '        private const double ColorTransitionMeters = 2.5;'))
}

if ($source -notmatch 'frontNearBlend') {
    $source = $source.Replace(
        '        private double rearNearProgress;',
        ('        private double rearNearProgress;' + $nl +
         '        private double frontNearBlend;' + $nl +
         '        private double rearNearBlend;' + $nl +
         '        private double lastProgressUpdate;'))
}

if ($source -notmatch 'Add\("FrontNearBlend"') {
    $source = $source.Replace(
        '            Add("RearNearProgress", 0.0, "Rear red fan expansion from 0 to 100.");',
        ('            Add("RearNearProgress", 0.0, "Rear red fan expansion from 0 to 100.");' + $nl +
         '            Add("FrontNearBlend", 0.0, "Front red/green transition blend.");' + $nl +
         '            Add("RearNearBlend", 0.0, "Rear red/green transition blend.");'))
}

$source = $source.Replace(
    ('                    rearNearProgress = 0.0;' + $nl),
    ('                    rearNearProgress = 0.0;' + $nl +
     '                    frontNearBlend = 0.0;' + $nl +
     '                    rearNearBlend = 0.0;' + $nl +
     '                    lastProgressUpdate = 0.0;' + $nl))

$oldProgress = @'
                frontNearProgress = CalculateNearProgress(frontMeters, settings.NearDistanceMeters);
                rearNearProgress = CalculateNearProgress(rearMeters, settings.NearDistanceMeters);
'@
$newProgress = @'
                double progressElapsed = lastProgressUpdate > 0.0 ? now - lastProgressUpdate : 0.016;
                lastProgressUpdate = now;
                double frontProgressTarget = CalculateNearProgress(frontMeters, settings.NearDistanceMeters);
                double rearProgressTarget = CalculateNearProgress(rearMeters, settings.NearDistanceMeters);
                frontNearProgress = SmoothProgress(frontNearProgress, frontProgressTarget, progressElapsed);
                rearNearProgress = SmoothProgress(rearNearProgress, rearProgressTarget, progressElapsed);
                frontNearBlend = CalculateNearBlend(frontMeters, settings.NearDistanceMeters);
                rearNearBlend = CalculateNearBlend(rearMeters, settings.NearDistanceMeters);
'@
$source = $source.Replace($oldProgress.Trim(), $newProgress.Trim())

$source = $source.Replace(
    'frontVisible = IsFinite(frontMeters) && Math.Abs(frontMeters) <= settings.NearDistanceMeters && sideClear;',
    'frontVisible = IsFinite(frontMeters) && Math.Abs(frontMeters) <= settings.NearDistanceMeters + ColorTransitionMeters && sideClear;')
$source = $source.Replace(
    'rearVisible = IsFinite(rearMeters) && Math.Abs(rearMeters) <= settings.NearDistanceMeters && sideClear;',
    'rearVisible = IsFinite(rearMeters) && Math.Abs(rearMeters) <= settings.NearDistanceMeters + ColorTransitionMeters && sideClear;')
$source = $source.Replace(
    'frontFarVisible = IsFinite(frontMeters) && Math.Abs(frontMeters) > settings.NearDistanceMeters && sideClear;',
    'frontFarVisible = IsFinite(frontMeters) && Math.Abs(frontMeters) >= Math.Max(0.0, settings.NearDistanceMeters - ColorTransitionMeters) && sideClear;')
$source = $source.Replace(
    'rearFarVisible = IsFinite(rearMeters) && Math.Abs(rearMeters) > settings.NearDistanceMeters && sideClear;',
    'rearFarVisible = IsFinite(rearMeters) && Math.Abs(rearMeters) >= Math.Max(0.0, settings.NearDistanceMeters - ColorTransitionMeters) && sideClear;')

if ($source -notmatch 'private static double SmoothProgress') {
    $methods = @'
        private static double SmoothProgress(double current, double target, double elapsed)
        {
            if (!IsFinite(current)) current = target;
            elapsed = Math.Max(0.0, Math.Min(elapsed, 0.25));
            double alpha = 1.0 - Math.Exp(-elapsed / 0.12);
            return current + (target - current) * alpha;
        }

        private static double CalculateNearBlend(double meters, double nearDistanceMeters)
        {
            if (!IsFinite(meters) || nearDistanceMeters <= 0.0) return 0.0;
            double distance = Math.Abs(meters);
            double start = Math.Max(0.0, nearDistanceMeters - ColorTransitionMeters);
            double end = nearDistanceMeters + ColorTransitionMeters;
            if (distance <= start) return 100.0;
            if (distance >= end) return 0.0;
            return (end - distance) / (end - start) * 100.0;
        }

'@
    $source = $source.Replace(
        '        private string BuildDisplayText(string prefix, double meters, double seconds)',
        ($methods + '        private string BuildDisplayText(string prefix, double meters, double seconds)'))
}

if ($source -notmatch 'Set\("FrontNearBlend"') {
    $source = $source.Replace(
        '            Set("RearNearProgress", rearNearProgress);',
        ('            Set("RearNearProgress", rearNearProgress);' + $nl +
         '            Set("FrontNearBlend", frontNearBlend);' + $nl +
         '            Set("RearNearBlend", rearNearBlend);'))
}

[System.IO.File]::WriteAllText((Resolve-Path $path), $source, [System.Text.UTF8Encoding]::new($false))
