$ErrorActionPreference = 'Stop'

$overlayPath = Join-Path $PSScriptRoot '..\SimHubPlugin\Overlay\iRacing Radar.djson'
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

function New-RedLayer([string]$direction, [int]$index, [int]$inset, [int]$baseOpacity) {
    $front = $direction -eq 'Front'
    $left = 80 + $inset
    $width = 260 - $inset * 2
    $height = 130 - $inset
    $top = 130
    if ($front) { $top = $inset }
    $radius = [int]($width / 2)
    $visibleProperty = "$direction`Visible"
    $opacityProperty = "$direction`ProximityOpacity"

    [pscustomobject][ordered]@{
        '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.RectangleItem, SimHub.Plugins'
        IsRectangleItem = $true
        BackgroundColor = '#FFF51C25'
        BorderColor = '#00FFFFFF'
        BorderStyle = if ($front) {
            [pscustomobject][ordered]@{
                BorderColor = '#00FFFFFF'
                BorderTop = 0; BorderBottom = 0; BorderLeft = 0; BorderRight = 0
                RadiusTopLeft = $radius; RadiusTopRight = $radius
                RadiusBottomLeft = 0; RadiusBottomRight = 0
            }
        } else {
            [pscustomobject][ordered]@{
                BorderColor = '#00FFFFFF'
                BorderTop = 0; BorderBottom = 0; BorderLeft = 0; BorderRight = 0
                RadiusTopLeft = 0; RadiusTopRight = 0
                RadiusBottomLeft = $radius; RadiusBottomRight = $radius
            }
        }
        Height = $height
        Left = $left
        Opacity = $baseOpacity
        Top = $top
        Visible = $false
        BlinkPhasisInverted = $false
        Width = $width
        Name = "$direction near red gradient $index"
        RenderingSkip = 0
        MinimumRefreshIntervalMS = 0
        Bindings = [pscustomobject][ordered]@{
            Visible = New-Binding "return isnull(`$prop('IRacingRadarPlugin.RadarVisible'), false) && isnull(`$prop('IRacingRadarPlugin.$visibleProperty'), false);" 'Visible'
            Opacity = New-Binding "return $baseOpacity * Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92))) / 100 * Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.$opacityProperty'),0))) / 100;" 'Opacity'
        }
    }
}

function Set-DistanceOpacity($item, [string]$direction, [int]$baseOpacity) {
    $property = "$direction`ProximityOpacity"
    $item.Opacity = $baseOpacity
    $item.Bindings.Opacity.Formula.Expression =
        "return $baseOpacity * Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92))) / 100 * Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.$property'),0))) / 100;"
}

function Set-Label($item, [string]$direction, [bool]$far) {
    $item.FontSize = 22
    $item.Left = 110
    $item.Width = 200
    $item.Height = 40
    $item.Top = if ($direction -eq 'Front') { 42 } else { 178 }
    $item.Text = if ($direction -eq 'Front') { 'F --' } else { 'B --' }

    if ($null -ne $item.Bindings.PSObject.Properties['Top']) {
        $item.Bindings.PSObject.Properties.Remove('Top')
    }

    $displayProperty = "$direction`DisplayText"
    $fallback = if ($direction -eq 'Front') { 'F --' } else { 'B --' }
    $item.Bindings.Text = New-Binding "return isnull(`$prop('IRacingRadarPlugin.$displayProperty'),'$fallback');" 'Text'

    if ($null -eq $item.Bindings.PSObject.Properties['FontSize']) {
        $item.Bindings | Add-Member -NotePropertyName FontSize -NotePropertyValue (
            New-Binding "return isnull(`$prop('IRacingRadarPlugin.LabelFontSize'),22);" 'FontSize'
        )
    }
    else {
        $item.Bindings.FontSize.Formula.Expression = "return isnull(`$prop('IRacingRadarPlugin.LabelFontSize'),22);"
    }

    Set-DistanceOpacity $item $direction 100
}

$removeNames = @(
    'Front opponent glow', 'Rear opponent glow',
    'Nearest car ahead', 'Nearest car behind'
)

$items = [System.Collections.Generic.List[object]]::new()
$redInserted = $false

foreach ($item in $overlay.Screens[0].Items) {
    if ($item.Name -in $removeNames -or $item.Name -match '^(Front|Rear) near red gradient') { continue }

    if ($item.Name -eq 'Front far semicircle') { Set-DistanceOpacity $item 'Front' 88 }
    elseif ($item.Name -eq 'Rear far semicircle') { Set-DistanceOpacity $item 'Rear' 88 }
    elseif ($item.Name -eq 'Front far distance') { Set-Label $item 'Front' $true }
    elseif ($item.Name -eq 'Rear far distance') { Set-Label $item 'Rear' $true }
    elseif ($item.Name -eq 'Front distance') { Set-Label $item 'Front' $false }
    elseif ($item.Name -eq 'Rear distance') { Set-Label $item 'Rear' $false }

    $items.Add($item)

    if (-not $redInserted -and $item.Name -eq 'Rear far semicircle') {
        $insets = @(0, 10, 22, 36)
        $opacities = @(22, 30, 38, 48)
        for ($i = 0; $i -lt $insets.Count; $i++) {
            $items.Add((New-RedLayer 'Front' ($i + 1) $insets[$i] $opacities[$i]))
            $items.Add((New-RedLayer 'Rear' ($i + 1) $insets[$i] $opacities[$i]))
        }
        $redInserted = $true
    }
}

$overlay.Screens[0].Items = $items.ToArray()
$json = $overlay | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path $overlayPath), $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
