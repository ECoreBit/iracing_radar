$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'Configurator'
& (Join-Path $PSScriptRoot 'build-updater.ps1')
$testDir = Join-Path $source 'bin\SyntheticTest'
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
$output = Join-Path $testDir 'RadarUpdaterSyntheticTest.exe'
$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
& $csc /nologo /target:exe /main:IRacingRadarUpdater.RadarUpdaterSyntheticTest "/out:$output" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll `
    /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll `
    (Join-Path $source 'RadarUpdater.cs') `
    (Join-Path $source 'RadarUpdaterSyntheticTest.cs')
if ($LASTEXITCODE -ne 0) { throw "Updater test compilation failed with exit code $LASTEXITCODE." }
& $output
if ($LASTEXITCODE -ne 0) { throw "Updater test failed with exit code $LASTEXITCODE." }
