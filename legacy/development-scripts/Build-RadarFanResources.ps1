$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem

$overlayDir = Join-Path $PSScriptRoot '..\SimHubPlugin\Overlay'
$resourceDir = Join-Path $overlayDir 'FanResources'
$archivePath = Join-Path $overlayDir 'iRacing Radar.djson.ressources'

if (Test-Path $resourceDir) {
    Remove-Item -LiteralPath $resourceDir -Recurse -Force
}
New-Item -ItemType Directory -Path $resourceDir | Out-Null

$levelCount = 60
$spans = @()
for ($i = 0; $i -lt $levelCount; $i++) {
    $spans += 28.0 + (176.0 - 28.0) * $i / ($levelCount - 1)
}
foreach ($direction in @('Front', 'Rear')) {
    for ($index = 0; $index -lt $spans.Count; $index++) {
        $bitmap = New-Object System.Drawing.Bitmap 260, 130
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)

        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $span = $spans[$index]
        if ($direction -eq 'Front') {
            $path.AddPie(0, 0, 260, 260, [single](270.0 - $span / 2.0), [single]$span)
            $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
                (New-Object System.Drawing.Rectangle 0, 0, 260, 130),
                ([System.Drawing.Color]::FromArgb(55, 255, 24, 38)),
                ([System.Drawing.Color]::FromArgb(225, 255, 22, 36)),
                [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
        }
        else {
            $path.AddPie(0, -130, 260, 260, [single](90.0 - $span / 2.0), [single]$span)
            $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
                (New-Object System.Drawing.Rectangle 0, 0, 260, 130),
                ([System.Drawing.Color]::FromArgb(225, 255, 22, 36)),
                ([System.Drawing.Color]::FromArgb(55, 255, 24, 38)),
                [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
        }

        $graphics.FillPath($brush, $path)
        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(145, 255, 65, 75)), 2
        $graphics.DrawPath($pen, $path)

        $name = "$direction`Fan$($index + 1).png"
        $bitmap.Save((Join-Path $resourceDir $name), [System.Drawing.Imaging.ImageFormat]::Png)

        $pen.Dispose()
        $brush.Dispose()
        $path.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

if (Test-Path $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    (Resolve-Path $resourceDir),
    $archivePath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)
