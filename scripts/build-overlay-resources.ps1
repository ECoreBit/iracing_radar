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
            $path.AddPie(4, 4, 252, 252, [single](270.0 - $span / 2.0), [single]$span)
            $start = [System.Drawing.Color]::FromArgb(55, 255, 24, 38)
            $end = [System.Drawing.Color]::FromArgb(225, 255, 22, 36)
        } else {
            $path.AddPie(4, -126, 252, 252, [single](90.0 - $span / 2.0), [single]$span)
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
            $bounds = New-Object System.Drawing.RectangleF 10, 10, 240, 240
            $startAngle = 270.0 - $span / 2.0
        } else {
            $bounds = New-Object System.Drawing.RectangleF 10, -120, 240, 240
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

# A continuous feathered edge for the radar circle. Calculate alpha per pixel
# around the exact half-pixel center so all four sides remain symmetrical and
# the transparent outer margin cannot be clipped by GDI ellipse rounding.
$bitmap = New-Object System.Drawing.Bitmap 260, 260
for ($y = 0; $y -lt 260; $y++) {
    for ($x = 0; $x -lt 260; $x++) {
        $dx = ($x + 0.5) - 130.0
        $dy = ($y + 0.5) - 130.0
        $radius = [Math]::Sqrt($dx * $dx + $dy * $dy)
        $distance = $radius - 121.0
        $alpha = [int][Math]::Round(115.0 * [Math]::Exp(-0.5 * $distance * $distance / (1.8 * 1.8)))
        if ($alpha -lt 2) { $alpha = 0 }
        if ($alpha -gt 0) {
            $bitmap.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, 221, 230, 238))
        }
    }
}
$bitmap.Save((Join-Path $resourceDir 'RadarEdgeGlow.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

function New-VehicleMarker([string]$name, [System.Drawing.Color]$fillColor,
    [System.Drawing.Color]$borderColor) {
    $vehicleBitmap = New-Object System.Drawing.Bitmap 96, 200
    $vehicleGraphics = [System.Drawing.Graphics]::FromImage($vehicleBitmap)
    $vehicleGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $vehicleGraphics.Clear([System.Drawing.Color]::Transparent)
    $vehiclePath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = 16.0
    $diameter = $radius * 2.0
    $vehiclePath.AddArc(4, 4, $diameter, $diameter, 180, 90)
    $vehiclePath.AddArc(92 - $diameter, 4, $diameter, $diameter, 270, 90)
    $vehiclePath.AddArc(92 - $diameter, 196 - $diameter, $diameter, $diameter, 0, 90)
    $vehiclePath.AddArc(4, 196 - $diameter, $diameter, $diameter, 90, 90)
    $vehiclePath.CloseFigure()
    $vehicleBrush = New-Object System.Drawing.SolidBrush $fillColor
    $vehiclePen = New-Object System.Drawing.Pen $borderColor, 4
    $vehicleGraphics.FillPath($vehicleBrush, $vehiclePath)
    $vehicleGraphics.DrawPath($vehiclePen, $vehiclePath)
    $vehicleBitmap.Save((Join-Path $resourceDir "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $vehiclePen.Dispose(); $vehicleBrush.Dispose(); $vehiclePath.Dispose()
    $vehicleGraphics.Dispose(); $vehicleBitmap.Dispose()
}
New-VehicleMarker 'VehicleMarkerPlayer' ([System.Drawing.Color]::FromArgb(255, 52, 170, 224)) `
    ([System.Drawing.Color]::FromArgb(220, 202, 244, 255))
New-VehicleMarker 'VehicleMarkerRed' ([System.Drawing.Color]::FromArgb(240, 227, 27, 44)) `
    ([System.Drawing.Color]::FromArgb(184, 255, 122, 130))

if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($resourceDir, $archivePath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
Write-Host "Built: $archivePath"

