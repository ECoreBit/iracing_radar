$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root 'Configurator'
$outputDir = Join-Path $source 'bin\Release'
$output = Join-Path $outputDir 'IRacingRadar.Updater.exe'
$csc = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) { throw 'The .NET Framework C# compiler was not found.' }

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
& $csc /nologo /target:winexe /optimize+ "/out:$output" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll `
    /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll `
    "/win32icon:$(Join-Path $source 'IRacingRadar.ico')" `
    (Join-Path $source 'RadarUpdater.cs')
if ($LASTEXITCODE -ne 0) { throw "Updater compilation failed with exit code $LASTEXITCODE." }
Write-Host "Built: $output"
