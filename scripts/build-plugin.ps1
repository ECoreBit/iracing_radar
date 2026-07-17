param(
    [string]$SimHubPath = "C:\Program Files (x86)\SimHub"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'SimHubPlugin\IRacingRadarPlugin'
$outputDir = Join-Path $project 'bin\Release'
$output = Join-Path $outputDir 'User.IRacingRadarPlugin.dll'
$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $csc) { throw 'The .NET Framework C# compiler was not found.' }
if (-not (Test-Path -LiteralPath $SimHubPath)) { throw "SimHub was not found: $SimHubPath" }

$references = @('GameReaderCommon.dll', 'SimHub.Plugins.dll', 'SimHub.Logging.dll', 'log4net.dll')
foreach ($name in $references) {
    $path = Join-Path $SimHubPath $name
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing SimHub dependency: $path" }
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$arguments = @('/nologo', '/target:library', '/optimize+', "/out:$output")
$arguments += $references | ForEach-Object { "/reference:$(Join-Path $SimHubPath $_)" }
$arguments += @(
    (Join-Path $project 'IRacingRadarPlugin.cs'),
    (Join-Path $project 'RadarMath.cs'),
    (Join-Path $project 'RadarSettings.cs')
)

& $csc @arguments
if ($LASTEXITCODE -ne 0) { throw "Plugin compilation failed with exit code $LASTEXITCODE." }
Write-Host "Built: $output"

