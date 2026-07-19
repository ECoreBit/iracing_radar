param(
    [string]$SimHubPath = "C:\Program Files (x86)\SimHub",
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'build-plugin.ps1') -SimHubPath $SimHubPath
    & (Join-Path $PSScriptRoot 'build-configurator.ps1')
}

$plugin = Join-Path $root 'SimHubPlugin\IRacingRadarPlugin\bin\Release\User.IRacingRadarPlugin.dll'
$configurator = Join-Path $root 'Configurator\bin\Release\IRacingRadar.Configurator.exe'
$updater = Join-Path $root 'Configurator\bin\Release\IRacingRadar.Updater.exe'
$overlaySource = Join-Path $root 'SimHubPlugin\Overlay'
$overlayTarget = Join-Path $SimHubPath 'DashTemplates\iRacing Radar'
$settingsTarget = Join-Path $SimHubPath 'IRacingRadar.settings.ini'

if (-not (Test-Path -LiteralPath $plugin)) { throw "Plugin DLL was not found: $plugin" }
if (-not (Test-Path -LiteralPath $configurator)) { throw "Configurator was not found: $configurator" }
if (-not (Test-Path -LiteralPath $updater)) { throw "Updater was not found: $updater" }
New-Item -ItemType Directory -Path $overlayTarget -Force | Out-Null
Copy-Item -LiteralPath $plugin -Destination (Join-Path $SimHubPath 'User.IRacingRadarPlugin.dll') -Force
Copy-Item -LiteralPath $configurator -Destination (Join-Path $SimHubPath 'IRacingRadar.Configurator.exe') -Force
Copy-Item -LiteralPath $updater -Destination (Join-Path $SimHubPath 'IRacingRadar.Updater.exe') -Force
Copy-Item -LiteralPath (Join-Path $overlaySource 'iRacing Radar.djson') -Destination $overlayTarget -Force
Copy-Item -LiteralPath (Join-Path $overlaySource 'iRacing Radar.djson.ressources') -Destination $overlayTarget -Force

if (-not (Test-Path -LiteralPath $settingsTarget)) {
    Copy-Item -LiteralPath (Join-Path $root 'IRacingRadar.settings.ini') -Destination $settingsTarget
}

Write-Host 'Installation completed. Restart SimHub, enable iRacing Radar, then start the iRacing Radar overlay.'
Write-Host "Settings: $settingsTarget"

