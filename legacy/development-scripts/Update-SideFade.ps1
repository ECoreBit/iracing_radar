$ErrorActionPreference = 'Stop'

$overlayPath = Join-Path $PSScriptRoot '..\SimHubPlugin\Overlay\iRacing Radar.djson'
$overlay = Get-Content -Raw -Encoding UTF8 $overlayPath | ConvertFrom-Json
$items = [System.Collections.Generic.List[object]]::new()

function New-SideSegment {
    param(
        [string]$Side,
        [int]$Index,
        [int]$Left,
        [int]$Opacity
    )

    $propertyPrefix = if ($Side -eq 'Left') { 'IRacingRadarPlugin.Left' } else { 'IRacingRadarPlugin.Right' }
    [pscustomobject][ordered]@{
        '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.RectangleItem, SimHub.Plugins'
        IsRectangleItem = $true
        BackgroundColor = '#D9F51C25'
        BorderColor = '#00FFFFFF'
        Height = 48
        Left = $Left
        Opacity = $Opacity
        Top = 86
        Visible = $false
        BlinkPhasisInverted = $false
        Width = 22
        Name = "$Side opponent fade $Index"
        RenderingSkip = 0
        MinimumRefreshIntervalMS = 0
        Bindings = [pscustomobject][ordered]@{
            Visible = [pscustomobject][ordered]@{
                Formula = [pscustomobject][ordered]@{
                    JSExt = 0
                    Interpreter = 1
                    Expression = "return isnull(`$prop('$propertyPrefix`Visible'), false);"
                }
                Mode = 2
                TargetPropertyName = 'Visible'
            }
            Top = [pscustomobject][ordered]@{
                Formula = [pscustomobject][ordered]@{
                    JSExt = 0
                    Interpreter = 1
                    Expression = "return isnull(`$prop('$propertyPrefix`Top'), 86)+0;"
                }
                Mode = 2
                TargetPropertyName = 'Top'
            }
        }
    }
}

foreach ($item in $overlay.Screens[0].Items) {
    if ($item.Name -in @('Left warning glow', 'Right warning glow', 'Left opponent', 'Right opponent')) {
        continue
    }

    if ($item.Name -eq 'Left distance') {
        $leftPositions = @(70, 92, 114, 136, 158)
        $leftOpacities = @(20, 36, 50, 64, 78)
        $rightPositions = @(180, 202, 224, 246, 268)
        $rightOpacities = @(78, 64, 50, 36, 20)

        for ($i = 0; $i -lt 5; $i++) {
            $items.Add((New-SideSegment -Side 'Left' -Index ($i + 1) -Left $leftPositions[$i] -Opacity $leftOpacities[$i]))
        }
        for ($i = 0; $i -lt 5; $i++) {
            $items.Add((New-SideSegment -Side 'Right' -Index ($i + 1) -Left $rightPositions[$i] -Opacity $rightOpacities[$i]))
        }

        $item.Left = 78
    }
    elseif ($item.Name -eq 'Right distance') {
        $item.Left = 210
    }

    $items.Add($item)
}

$overlay.Screens[0].Items = $items.ToArray()
$json = $overlay | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path $overlayPath), $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
