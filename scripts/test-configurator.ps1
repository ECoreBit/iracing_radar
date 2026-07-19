$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'Configurator'
& (Join-Path $PSScriptRoot 'build-configurator.ps1')

$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$testDir = Join-Path $source 'bin\SyntheticTest'
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
$test = Join-Path $testDir 'RadarConfiguratorSyntheticTest.exe'
& $csc /nologo /target:exe "/out:$test" `
    (Join-Path $source 'RadarConfiguratorSettings.cs') `
    (Join-Path $source 'ConfiguratorPreferences.cs') `
    (Join-Path $source 'UpdateChecker.cs') `
    (Join-Path $source 'RadarPreviewMath.cs') `
    (Join-Path $source 'RadarOverlayMath.cs') `
    (Join-Path $source 'RadarConfiguratorSyntheticTest.cs')
if ($LASTEXITCODE -ne 0) { throw "Configurator test compilation failed with exit code $LASTEXITCODE." }
& $test
if ($LASTEXITCODE -ne 0) { throw "Configurator synthetic test failed with exit code $LASTEXITCODE." }

$overlayPath = Join-Path $root 'SimHubPlugin\Overlay\iRacing Radar.djson'
$resourcesPath = "$overlayPath.ressources"
$overlay = Get-Content -Raw -LiteralPath $overlayPath | ConvertFrom-Json
if ($overlay.BaseWidth -ne 420 -or $overlay.BaseHeight -ne 260) {
    throw 'Configurator preview expects the SimHub overlay to remain 420 x 260.'
}
$requiredItems = @{
    'Radar circle outline' = '81,1,258,258,#52101620'
    'Player marker' = '201,109,18,42,#FF727E8A'
    'Left opponent marker' = '167,109,18,42,#F0E31B2C'
    'Right opponent marker' = '235,109,18,42,#F0E31B2C'
    'Left position rail' = '175,34,2,192,#80D51B2A'
    'Right position rail' = '243,34,2,192,#80D51B2A'
}
foreach ($name in $requiredItems.Keys) {
    $item = $overlay.Screens[0].Items | Where-Object { $_.Name -eq $name } | Select-Object -First 1
    if (-not $item) { throw "Missing overlay item required by preview: $name" }
    $actual = "$($item.Left),$($item.Top),$($item.Width),$($item.Height),$($item.BackgroundColor)"
    if ($actual -ne $requiredItems[$name]) { throw "Preview contract changed for ${name}: $actual" }
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($resourcesPath)
try {
    $frames = @($archive.Entries | Where-Object { $_.Name -match '^(Front|Rear)(Fan|GreenArc)\d+\.png$' })
    if ($frames.Count -ne 240) { throw "Expected 240 Overlay preview frames, found $($frames.Count)." }
}
finally { $archive.Dispose() }
Write-Host 'PASS configurator/SimHub overlay visual contract'

$builtConfigurator = Join-Path $source 'bin\Release\IRacingRadar.Configurator.exe'
Add-Type -AssemblyName System.Drawing
$embeddedIcon = [Drawing.Icon]::ExtractAssociatedIcon($builtConfigurator)
if (-not $embeddedIcon -or $embeddedIcon.Width -lt 32 -or $embeddedIcon.Height -lt 32) {
    throw 'The configurator executable does not contain the radar application icon.'
}
$iconBitmap = $embeddedIcon.ToBitmap()
try {
    $hasGreen = $false
    $hasRed = $false
    for ($y = 0; $y -lt $iconBitmap.Height; $y++) {
        for ($x = 0; $x -lt $iconBitmap.Width; $x++) {
            $pixel = $iconBitmap.GetPixel($x, $y)
            if ($pixel.G -gt 150 -and $pixel.G -gt $pixel.R * 1.4) { $hasGreen = $true }
            if ($pixel.R -gt 170 -and $pixel.R -gt $pixel.G * 1.5) { $hasRed = $true }
        }
    }
    if (-not $hasGreen -or -not $hasRed) { throw 'The embedded icon does not contain the expected radar colors.' }
}
finally {
    $iconBitmap.Dispose()
    $embeddedIcon.Dispose()
}
Write-Host 'PASS configurator embedded radar icon'
& (Join-Path $PSScriptRoot 'test-preview-contract.ps1')
& (Join-Path $PSScriptRoot 'test-configurator-layout.ps1')
& (Join-Path $PSScriptRoot 'test-green-arc-edges.ps1')
& (Join-Path $PSScriptRoot 'test-updater.ps1')
