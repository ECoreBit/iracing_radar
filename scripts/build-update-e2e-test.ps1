param([string]$OutputDirectory)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'Configurator'
$outputDir = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $root 'release-build\update-e2e-test'
} elseif ([IO.Path]::IsPathRooted($OutputDirectory)) {
    [IO.Path]::GetFullPath($OutputDirectory)
} else {
    [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
}
$output = Join-Path $outputDir 'IRacingRadar.Configurator.exe'
$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) { throw 'The .NET Framework C# compiler was not found.' }

& (Join-Path $PSScriptRoot 'build-updater.ps1')
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
    (Join-Path $source 'AssemblyInfo.UpdateTest.cs'),
    (Join-Path $source 'UpdateChecker.cs'),
    (Join-Path $source 'UpdateInstaller.cs'),
    (Join-Path $source 'UpdateAvailableDialog.cs'),
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
if ($LASTEXITCODE -ne 0) { throw "Update E2E test configurator compilation failed with exit code $LASTEXITCODE." }

Copy-Item -LiteralPath (Join-Path $source 'bin\Release\IRacingRadar.Updater.exe') -Destination $outputDir -Force
Copy-Item -LiteralPath (Join-Path $root 'IRacingRadar.settings.ini') -Destination $outputDir -Force
Write-Host "Built isolated update test: $outputDir"
Write-Host 'Close SimHub before running this test so the formal installation is not interrupted.'
