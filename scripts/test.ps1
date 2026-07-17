param(
    [string]$SimHubPath = "C:\Program Files (x86)\SimHub"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'SimHubPlugin\IRacingRadarPlugin'
& (Join-Path $PSScriptRoot 'build-plugin.ps1') -SimHubPath $SimHubPath

$testDir = Join-Path $project 'bin\SyntheticTest'
New-Item -ItemType Directory -Path $testDir -Force | Out-Null
foreach ($name in @('GameReaderCommon.dll', 'SimHub.Plugins.dll', 'SimHub.Logging.dll', 'log4net.dll')) {
    Copy-Item -LiteralPath (Join-Path $SimHubPath $name) -Destination $testDir -Force
}
Copy-Item -LiteralPath (Join-Path $project 'bin\Release\User.IRacingRadarPlugin.dll') -Destination $testDir -Force

$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
$testExe = Join-Path $testDir 'RadarSyntheticTest.exe'
& $csc /nologo /target:exe "/out:$testExe" "/reference:$(Join-Path $testDir 'GameReaderCommon.dll')" (Join-Path $project 'RadarSyntheticTest.cs')
if ($LASTEXITCODE -ne 0) { throw "Test compilation failed with exit code $LASTEXITCODE." }

Push-Location $testDir
try { & $testExe } finally { Pop-Location }
if ($LASTEXITCODE -ne 0) { throw "Synthetic test failed with exit code $LASTEXITCODE." }

