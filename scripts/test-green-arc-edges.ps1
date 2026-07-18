$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path $PSScriptRoot -Parent
$archivePath = Join-Path $root 'SimHubPlugin\Overlay\iRacing Radar.djson.ressources'
$archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    foreach ($direction in @('Front', 'Rear')) {
        for ($index = 1; $index -le 60; $index++) {
            $name = "$direction`GreenArc$index.png"
            $entry = $archive.Entries | Where-Object Name -eq $name | Select-Object -First 1
            if (-not $entry) { throw "Missing green arc frame: $name" }
            $stream = $entry.Open()
            try {
                $bitmap = [Drawing.Bitmap]::FromStream($stream)
                try {
                    $edgeY = if ($direction -eq 'Front') { 0 } else { $bitmap.Height - 1 }
                    for ($x = 0; $x -lt $bitmap.Width; $x++) {
                        if ($bitmap.GetPixel($x, $edgeY).A -ne 0) {
                            throw "$name touches image edge at ($x,$edgeY) and will appear clipped."
                        }
                    }
                }
                finally { $bitmap.Dispose() }
            }
            finally { $stream.Dispose() }
        }
    }
}
finally { $archive.Dispose() }

Write-Host 'PASS all green arc frames have transparent outer edges'
