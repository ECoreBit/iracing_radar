$ErrorActionPreference = 'Stop'

$overlayPath = Join-Path $PSScriptRoot '..\SimHubPlugin\Overlay\iRacing Radar.djson'
$overlay = Get-Content -Raw -Encoding UTF8 $overlayPath | ConvertFrom-Json

function New-VisibleBinding([string]$propertyName) {
    [pscustomobject][ordered]@{
        Formula = [pscustomobject][ordered]@{
            JSExt = 0
            Interpreter = 1
            Expression = "return isnull(`$prop('IRacingRadarPlugin.$propertyName'), false);"
        }
        Mode = 2
        TargetPropertyName = 'Visible'
    }
}

function New-CircleOutline {
    [pscustomobject][ordered]@{
        '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.RectangleItem, SimHub.Plugins'
        IsRectangleItem = $true
        BackgroundColor = '#00000000'
        BorderColor = '#00FFFFFF'
        BorderStyle = [pscustomobject][ordered]@{
            BorderColor = '#70FFFFFF'
            BorderTop = 2
            BorderBottom = 2
            BorderLeft = 2
            BorderRight = 2
            RadiusTopLeft = 109
            RadiusTopRight = 109
            RadiusBottomLeft = 109
            RadiusBottomRight = 109
        }
        Height = 218
        Left = 71
        Opacity = 35
        Top = 1
        Visible = $true
        BlinkPhasisInverted = $false
        Width = 218
        Name = 'Radar circle outline'
        RenderingSkip = 0
        MinimumRefreshIntervalMS = 0
    }
}

function New-FarSemicircle([string]$direction) {
    $front = $direction -eq 'Front'
    [pscustomobject][ordered]@{
        '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.RectangleItem, SimHub.Plugins'
        IsRectangleItem = $true
        BackgroundColor = '#2639E58C'
        BorderColor = '#00FFFFFF'
        BorderStyle = if ($front) {
            [pscustomobject][ordered]@{
                BorderColor = '#DD39E58C'
                BorderTop = 6
                BorderBottom = 0
                BorderLeft = 6
                BorderRight = 6
                RadiusTopLeft = 110
                RadiusTopRight = 110
                RadiusBottomLeft = 0
                RadiusBottomRight = 0
            }
        } else {
            [pscustomobject][ordered]@{
                BorderColor = '#DD39E58C'
                BorderTop = 0
                BorderBottom = 6
                BorderLeft = 6
                BorderRight = 6
                RadiusTopLeft = 0
                RadiusTopRight = 0
                RadiusBottomLeft = 110
                RadiusBottomRight = 110
            }
        }
        Height = 110
        Left = 70
        Opacity = 88
        Top = if ($front) { 0 } else { 110 }
        Visible = $false
        BlinkPhasisInverted = $false
        Width = 220
        Name = "$direction far semicircle"
        RenderingSkip = 0
        MinimumRefreshIntervalMS = 0
        Bindings = [pscustomobject][ordered]@{
            Visible = New-VisibleBinding "$direction`FarVisible"
        }
    }
}

function New-FarDistance([string]$direction) {
    $front = $direction -eq 'Front'
    $symbol = if ($front) { [string][char]0x25B2 } else { [string][char]0x25BC }
    $distanceProperty = "$direction`RelativeMeters"
    [pscustomobject][ordered]@{
        '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.TextItem, SimHub.Plugins'
        IsTextItem = $true
        Font = 'Segoe UI'
        FontWeight = 'Bold'
        FontSize = 13
        Text = "$symbol 0m"
        TextColor = '#F239E58C'
        HorizontalAlignment = 1
        VerticalAlignment = 1
        BackgroundColor = '#00000000'
        Height = 28
        Left = 139
        Top = if ($front) { 28 } else { 164 }
        Visible = $false
        BlinkPhasisInverted = $false
        Width = 82
        Name = "$direction far distance"
        RenderingSkip = 0
        MinimumRefreshIntervalMS = 0
        Bindings = [pscustomobject][ordered]@{
            Visible = New-VisibleBinding "$direction`FarVisible"
            Text = [pscustomobject][ordered]@{
                Formula = [pscustomobject][ordered]@{
                    JSExt = 0
                    Interpreter = 1
                    Expression = "return '$symbol ' + format(Math.abs(isnull(`$prop('IRacingRadarPlugin.$distanceProperty'),0)),'0') + 'm';"
                }
                Mode = 2
                TargetPropertyName = 'Text'
            }
        }
    }
}

$removeNames = @(
    'Radar circle outline',
    'Front far semicircle',
    'Rear far semicircle',
    'Front far distance',
    'Rear far distance'
)

$items = [System.Collections.Generic.List[object]]::new()
$items.Add((New-CircleOutline))
$items.Add((New-FarSemicircle 'Front'))
$items.Add((New-FarSemicircle 'Rear'))
$items.Add((New-FarDistance 'Front'))
$items.Add((New-FarDistance 'Rear'))

foreach ($item in $overlay.Screens[0].Items) {
    if ($item.Name -in $removeNames) { continue }
    $items.Add($item)
}

$overlay.Screens[0].Items = $items.ToArray()
$json = $overlay | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path $overlayPath), $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
