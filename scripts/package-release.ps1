param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$SimHubPath = "C:\Program Files (x86)\SimHub"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$releaseRoot = Join-Path $root 'release-build'
$packageName = "iracing-radar-$Version"
$stage = Join-Path $releaseRoot $packageName
$zip = Join-Path $releaseRoot "$packageName.zip"
$overlayStage = Join-Path $stage 'DashTemplates\iRacing Radar'

$releaseRootFull = [IO.Path]::GetFullPath($releaseRoot).TrimEnd('\') + '\'
$stageFull = [IO.Path]::GetFullPath($stage)
if (-not $stageFull.StartsWith($releaseRootFull, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe package staging path: $stageFull"
}

& (Join-Path $PSScriptRoot 'build-plugin.ps1') -SimHubPath $SimHubPath
& (Join-Path $PSScriptRoot 'build-configurator.ps1')

if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
New-Item -ItemType Directory -Path $overlayStage -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $root 'SimHubPlugin\IRacingRadarPlugin\bin\Release\User.IRacingRadarPlugin.dll') -Destination $stage
Copy-Item -LiteralPath (Join-Path $root 'IRacingRadar.settings.ini') -Destination $stage
Copy-Item -LiteralPath (Join-Path $root 'Configurator\bin\Release\IRacingRadar.Configurator.exe') -Destination $stage
Copy-Item -LiteralPath (Join-Path $root 'Configurator\bin\Release\IRacingRadar.Updater.exe') -Destination $stage
Copy-Item -LiteralPath (Join-Path $root 'SimHubPlugin\Overlay\iRacing Radar.djson') -Destination $overlayStage
Copy-Item -LiteralPath (Join-Path $root 'SimHubPlugin\Overlay\iRacing Radar.djson.ressources') -Destination $overlayStage

Compress-Archive -LiteralPath (Get-ChildItem -LiteralPath $stage | ForEach-Object FullName) -DestinationPath $zip -CompressionLevel Optimal
Write-Host "Built release package: $zip"
Write-Host 'Users can extract the ZIP directly into the SimHub root directory.'
