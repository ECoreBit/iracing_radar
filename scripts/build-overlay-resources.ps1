$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path $PSScriptRoot -Parent
$overlayDir = Join-Path $root 'SimHubPlugin\Overlay'
$resourceDir = Join-Path $overlayDir 'FanResources'
$archivePath = Join-Path $overlayDir 'iRacing Radar.djson.ressources'

if (Test-Path -LiteralPath $resourceDir) { Remove-Item -LiteralPath $resourceDir -Recurse -Force }
New-Item -ItemType Directory -Path $resourceDir | Out-Null

$levelCount = 60
for ($index = 0; $index -lt $levelCount; $index++) {
    $span = 28.0 + (176.0 - 28.0) * $index / ($levelCount - 1)
    foreach ($direction in @('Front', 'Rear')) {
        $bitmap = New-Object System.Drawing.Bitmap 260, 130
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath

        if ($direction -eq 'Front') {
            $path.AddPie(0, 0, 260, 260, [single](270.0 - $span / 2.0), [single]$span)
            $start = [System.Drawing.Color]::FromArgb(55, 255, 24, 38)
            $end = [System.Drawing.Color]::FromArgb(225, 255, 22, 36)
        } else {
            $path.AddPie(0, -130, 260, 260, [single](90.0 - $span / 2.0), [single]$span)
            $start = [System.Drawing.Color]::FromArgb(225, 255, 22, 36)
            $end = [System.Drawing.Color]::FromArgb(55, 255, 24, 38)
        }

        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            (New-Object System.Drawing.Rectangle 0, 0, 260, 130), $start, $end,
            [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(145, 255, 65, 75)), 2
        $graphics.FillPath($brush, $path)
        $graphics.DrawPath($pen, $path)
        $bitmap.Save((Join-Path $resourceDir "$direction`Fan$($index + 1).png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $pen.Dispose(); $brush.Dispose(); $path.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
    }
}

# Green warning arcs keep the same 260 px circle geometry at every level.
# Only the angular span changes, so the arc disappears symmetrically from
# both ends toward the center without changing curvature.
for ($index = 0; $index -lt $levelCount; $index++) {
    $span = 180.0 * $index / ($levelCount - 1)
    foreach ($direction in @('Front', 'Rear')) {
        $bitmap = New-Object System.Drawing.Bitmap 260, 130
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)

        if ($direction -eq 'Front') {
            $bounds = New-Object System.Drawing.RectangleF 3, 3, 254, 254
            $startAngle = 270.0 - $span / 2.0
        } else {
            $bounds = New-Object System.Drawing.RectangleF 3, -127, 254, 254
            $startAngle = 90.0 - $span / 2.0
        }

        if ($span -gt 0.0) {
            $glow = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(55, 85, 255, 136)), 12
            $core = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(245, 85, 255, 136)), 6
            $glow.StartCap = $glow.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $core.StartCap = $core.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $graphics.DrawArc($glow, $bounds, [single]$startAngle, [single]$span)
            $graphics.DrawArc($core, $bounds, [single]$startAngle, [single]$span)
            $core.Dispose(); $glow.Dispose()
        }

        $bitmap.Save((Join-Path $resourceDir "$direction`GreenArc$($index + 1).png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose(); $bitmap.Dispose()
    }
}
if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($resourceDir, $archivePath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Write-Host "Built: $archivePath"

