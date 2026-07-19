$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'Configurator'
$testDir = Join-Path $source 'bin\SyntheticTest'
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
$output = Join-Path $testDir 'ConfiguratorLayoutTest.exe'
$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

& $csc /nologo /target:exe /main:IRacingRadarConfigurator.ConfiguratorLayoutTest "/out:$output" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    (Join-Path $source 'UpdateChecker.cs') `
    (Join-Path $source 'UpdateInstaller.cs') `
    (Join-Path $source 'RadarConfiguratorSettings.cs') `
    (Join-Path $source 'ConfiguratorPreferences.cs') `
    (Join-Path $source 'RadarPreviewMath.cs') `
    (Join-Path $source 'RadarOverlayMath.cs') `
    (Join-Path $source 'PreviewScenario.cs') `
    (Join-Path $source 'OverlayRadarPreviewControl.cs') `
    (Join-Path $source 'SimHubRestartService.cs') `
    (Join-Path $source 'RestartSimHubDialog.cs') `
    (Join-Path $source 'IRacingRadarConfigurator.cs') `
    (Join-Path $source 'ConfiguratorFeatures.cs') `
    (Join-Path $source 'ConfiguratorLayoutTest.cs')
if ($LASTEXITCODE -ne 0) { throw "Configurator layout-test compilation failed with exit code $LASTEXITCODE." }
& $output $root
if ($LASTEXITCODE -ne 0) { throw "Configurator layout test failed with exit code $LASTEXITCODE." }
