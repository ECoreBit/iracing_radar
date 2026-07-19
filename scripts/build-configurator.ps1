$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'Configurator'
$outputDir = Join-Path $source 'bin\Release'
$output = Join-Path $outputDir 'IRacingRadar.Configurator.exe'
$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) { throw 'The .NET Framework C# compiler was not found.' }

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$arguments = @(
    '/nologo', '/target:winexe', '/optimize+',
    "/out:$output",
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    '/reference:System.IO.Compression.dll',
    '/reference:System.IO.Compression.FileSystem.dll',
    "/win32manifest:$(Join-Path $source 'IRacingRadarConfigurator.manifest')",
    "/win32icon:$(Join-Path $source 'IRacingRadar.ico')",
    (Join-Path $source 'AssemblyInfo.cs'),
    (Join-Path $source 'UpdateChecker.cs'),
    (Join-Path $source 'UpdateInstaller.cs'),
    (Join-Path $source 'RadarConfiguratorSettings.cs'),
    (Join-Path $source 'ConfiguratorPreferences.cs'),
    (Join-Path $source 'RadarPreviewMath.cs'),
    (Join-Path $source 'RadarOverlayMath.cs'),
    (Join-Path $source 'PreviewScenario.cs'),
    (Join-Path $source 'OverlayRadarPreviewControl.cs'),
    (Join-Path $source 'SimHubRestartService.cs'),
    (Join-Path $source 'RestartSimHubDialog.cs'),
    (Join-Path $source 'IRacingRadarConfigurator.cs'),
    (Join-Path $source 'ConfiguratorFeatures.cs')
)
& $csc @arguments
if ($LASTEXITCODE -ne 0) { throw "Configurator compilation failed with exit code $LASTEXITCODE." }
& (Join-Path $PSScriptRoot 'build-updater.ps1')
Write-Host "Built: $output"
