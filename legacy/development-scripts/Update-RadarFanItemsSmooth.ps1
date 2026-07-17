$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$levelCount = 60
$overlayPath = Join-Path $PSScriptRoot '..\SimHubPlugin\Overlay\iRacing Radar.djson'
$resourceDir = Join-Path $PSScriptRoot '..\SimHubPlugin\Overlay\FanResources'
$overlay = Get-Content -Raw -Encoding UTF8 $overlayPath | ConvertFrom-Json

function New-Binding([string]$expression, [string]$target) {
    [pscustomobject][ordered]@{
        Formula = [pscustomobject][ordered]@{
            JSExt = 0
            Interpreter = 1
            Expression = $expression
        }
        Mode = 2
        TargetPropertyName = $target
    }
}

function New-FanItem([string]$direction, [int]$level) {
    $progressProperty = "$direction`NearProgress"
    $blendProperty = "$direction`NearBlend"
    $visibleProperty = "$direction`Visible"
    $step = 100.0 / ($levelCount - 1)
    $center = ($level - 1) * $step
    $centerText = $center.ToString('0.0000', [Globalization.CultureInfo]::InvariantCulture)
    $stepText = $step.ToString('0.0000', [Globalization.CultureInfo]::InvariantCulture)

    [pscustomobject][ordered]@{
        '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.ImageItem, SimHub.Plugins'
        Image = "$direction`Fan$level"
        AutoSize = $false
        BackgroundColor = '#00FFFFFF'
        Height = 130
        Left = 80
        Opacity = 100
        Top = if ($direction -eq 'Front') { 0 } else { 130 }
        Visible = $false
        BlinkPhasisInverted = $false
        Width = 260
        Name = "$direction smooth fan $level"
        RenderingSkip = 0
        MinimumRefreshIntervalMS = 0
        Bindings = [pscustomobject][ordered]@{
            Visible = New-Binding "const p=isnull(`$prop('IRacingRadarPlugin.$progressProperty'),0); return isnull(`$prop('IRacingRadarPlugin.RadarVisible'),false) && isnull(`$prop('IRacingRadarPlugin.$visibleProperty'),false) && Math.abs(p-$centerText) <= $stepText;" 'Visible'
            Opacity = New-Binding "const p=Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.$progressProperty'),0))); const w=Math.max(0,1-Math.abs(p-$centerText)/$stepText); const b=Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.$blendProperty'),0))); return w*(40+p*0.6)*b/100*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92)))/100;" 'Opacity'
        }
    }
}

$items = [System.Collections.Generic.List[object]]::new()
$inserted = $false
foreach ($item in $overlay.Screens[0].Items) {
    if ($item.Name -match '^(Front|Rear) near fan level' -or
        $item.Name -match '^(Front|Rear) smooth fan') {
        continue
    }

    if ($item.Name -in @('Front far semicircle', 'Rear far semicircle')) {
        $direction = if ($item.Name.StartsWith('Front')) { 'Front' } else { 'Rear' }
        $blendProperty = "$direction`NearBlend"
        $baseOpacity = 88
        $item.Bindings.Opacity.Formula.Expression =
            "const b=100-Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.$blendProperty'),0))); return $baseOpacity*b/100*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92)))/100;"
    }
    elseif ($item.Name -in @('Front far distance', 'Rear far distance')) {
        $direction = if ($item.Name.StartsWith('Front')) { 'Front' } else { 'Rear' }
        $blendProperty = "$direction`NearBlend"
        $item.Bindings.Opacity.Formula.Expression =
            "const b=100-Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.$blendProperty'),0))); return b*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92)))/100;"
    }
    elseif ($item.Name -in @('Front distance', 'Rear distance')) {
        $direction = if ($item.Name.StartsWith('Front')) { 'Front' } else { 'Rear' }
        $blendProperty = "$direction`NearBlend"
        $item.Bindings.Opacity.Formula.Expression =
            "const b=Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.$blendProperty'),0))); return b*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92)))/100;"
    }

    $items.Add($item)
    if (-not $inserted -and $item.Name -eq 'Rear far semicircle') {
        for ($level = 1; $level -le $levelCount; $level++) {
            $items.Add((New-FanItem 'Front' $level))
            $items.Add((New-FanItem 'Rear' $level))
        }
        $inserted = $true
    }
}
$overlay.Screens[0].Items = $items.ToArray()

$images = [System.Collections.Generic.List[object]]::new()
foreach ($direction in @('Front', 'Rear')) {
    for ($level = 1; $level -le $levelCount; $level++) {
        $name = "$direction`Fan$level"
        $path = Join-Path $resourceDir "$name.png"
        $file = Get-Item $path
        $bitmap = [System.Drawing.Image]::FromFile($file.FullName)
        $images.Add([pscustomobject][ordered]@{
            Name = $name
            Extension = '.png'
            Modified = $false
            Optimized = $true
            Width = $bitmap.Width
            Height = $bitmap.Height
            Length = $file.Length
            MD5 = (Get-FileHash -Algorithm MD5 $file.FullName).Hash.ToLowerInvariant()
        })
        $bitmap.Dispose()
    }
}
$overlay.Images = $images.ToArray()

$json = $overlay | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path $overlayPath), $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
