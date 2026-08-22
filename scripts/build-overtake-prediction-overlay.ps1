$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$path = Join-Path $root 'SimHubPlugin\Overlay\iRacing Radar.djson'
$overlay = Get-Content -Raw -LiteralPath $path -Encoding UTF8 | ConvertFrom-Json
function Convert-ToVehicleImage($item, [string]$imageName) {
    if ($null -eq $item) { return }
    $item.'$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.ImageItem, SimHub.Plugins'
    $item.PSObject.Properties.Remove('IsRectangleItem')
    $item.PSObject.Properties.Remove('BorderStyle')
    $item.PSObject.Properties.Remove('BorderColor')
    $item | Add-Member -Force -NotePropertyName Image -NotePropertyValue $imageName
    $item | Add-Member -Force -NotePropertyName AutoSize -NotePropertyValue $false
    $item.BackgroundColor = '#00FFFFFF'
    foreach ($corner in @('RadiusTopLeft', 'RadiusTopRight', 'RadiusBottomLeft', 'RadiusBottomRight')) {
        $item.Bindings.PSObject.Properties.Remove($corner)
    }
}
$player = @($overlay.Screens[0].Items | Where-Object { $_.Name -eq 'Player marker' }) | Select-Object -First 1
if ($null -ne $player) {
    Convert-ToVehicleImage $player 'VehicleMarkerPlayer'
    $player.Bindings.Width.Formula.Expression = "return isnull(`$prop('IRacingRadarPlugin.TrackPlayerWidth'),13.44);"
    $player.Bindings.Height.Formula.Expression = "return isnull(`$prop('IRacingRadarPlugin.TrackPlayerLength'),28);"
    $player.Bindings.Left.Formula.Expression = "var w=isnull(`$prop('IRacingRadarPlugin.TrackPlayerWidth'),13.44); return 210-w/2;"
    $player.Bindings.Top.Formula.Expression = "var h=isnull(`$prop('IRacingRadarPlugin.TrackPlayerLength'),28); return 130-h/2;"
}
$leftSideVehicle = @($overlay.Screens[0].Items | Where-Object { $_.Name -eq 'Left opponent marker' }) | Select-Object -First 1
$rightSideVehicle = @($overlay.Screens[0].Items | Where-Object { $_.Name -eq 'Right opponent marker' }) | Select-Object -First 1
foreach ($sideVehicle in @($leftSideVehicle, $rightSideVehicle)) {
    if ($null -eq $sideVehicle) { continue }
    Convert-ToVehicleImage $sideVehicle 'VehicleMarkerRed'
    $sideVehicle.Bindings.Width.Formula.Expression = "return isnull(`$prop('IRacingRadarPlugin.TrackPlayerWidth'),13.44);"
    $sideVehicle.Bindings.Height.Formula.Expression = "return isnull(`$prop('IRacingRadarPlugin.TrackPlayerLength'),28);"
    $sideVehicle.Bindings.Top.Formula.Expression = "var h=isnull(`$prop('IRacingRadarPlugin.TrackPlayerLength'),28); return isnull(`$prop('IRacingRadarPlugin.LeftTop'),109)+21-h/2;"
}
if ($null -ne $leftSideVehicle) {
    $leftSideVehicle.Bindings.Top.Formula.Expression = "var h=isnull(`$prop('IRacingRadarPlugin.TrackPlayerLength'),28); return isnull(`$prop('IRacingRadarPlugin.LeftTop'),109)+21-h/2;"
    $leftSideVehicle.Bindings.Left.Formula.Expression = "var w=isnull(`$prop('IRacingRadarPlugin.TrackPlayerWidth'),13.44); return 176-w/2;"
}
if ($null -ne $rightSideVehicle) {
    $rightSideVehicle.Bindings.Top.Formula.Expression = "var h=isnull(`$prop('IRacingRadarPlugin.TrackPlayerLength'),28); return isnull(`$prop('IRacingRadarPlugin.RightTop'),109)+21-h/2;"
    $rightSideVehicle.Bindings.Left.Formula.Expression = "var w=isnull(`$prop('IRacingRadarPlugin.TrackPlayerWidth'),13.44); return 244-w/2;"
}
$circle = @($overlay.Screens[0].Items | Where-Object { $_.Name -eq 'Radar circle outline' }) | Select-Object -First 1
if ($null -ne $circle) {
    $circle.Left = 84; $circle.Top = 4; $circle.Width = 252; $circle.Height = 252
    $circle.BorderStyle.RadiusTopLeft = 126; $circle.BorderStyle.RadiusTopRight = 126
    $circle.BorderStyle.RadiusBottomLeft = 126; $circle.BorderStyle.RadiusBottomRight = 126
    $circle.BorderStyle.BorderColor = '#00DDE6EE'
    $circle.BorderStyle.BorderTop = 0; $circle.BorderStyle.BorderBottom = 0
    $circle.BorderStyle.BorderLeft = 0; $circle.BorderStyle.BorderRight = 0
}
$items = @($overlay.Screens[0].Items | Where-Object {
    $_.Name -ne 'Predicted overtake point' -and $_.Name -ne 'Front opponent map point' -and
    $_.Name -ne 'Rear opponent map point' -and $_.Name -notlike 'Radar soft edge *' -and $_.Name -ne 'Radar feathered edge'
})
$items += [pscustomobject]@{
    '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.ImageItem, SimHub.Plugins'
    Image = 'RadarEdgeGlow'; AutoSize = $false; BackgroundColor = '#00FFFFFF'; Height = 260; Left = 80; Opacity = 100; Top = 0; Visible = $true
    BlinkPhasisInverted = $false; Width = 260; Name = 'Radar feathered edge'; RenderingSkip = 0; MinimumRefreshIntervalMS = 0
    Bindings = [pscustomobject]@{
        Visible = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.RadarVisible'), false);" }; Mode = 2; TargetPropertyName = 'Visible' }
        Opacity = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92)))*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.RadarVisualOpacity'),0)))/100;" }; Mode = 2; TargetPropertyName = 'Opacity' }
    }
}

$point = [pscustomobject]@{
    '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.RectangleItem, SimHub.Plugins'
    IsRectangleItem = $true
    BackgroundColor = '#FF4DF28B'
    BorderColor = '#00FFFFFF'
    BorderStyle = [pscustomobject]@{
        BorderColor = '#00FFFFFF'
        BorderTop = 0
        BorderBottom = 0
        BorderLeft = 0
        BorderRight = 0
        RadiusTopLeft = 2
        RadiusTopRight = 2
        RadiusBottomLeft = 2
        RadiusBottomRight = 2
    }
    Height = 3
    Left = 199
    Opacity = 90
    Top = 128.5
    Visible = $false
    BlinkPhasisInverted = $false
    Width = 22
    Name = 'Predicted overtake point'
    RenderingSkip = 0
    MinimumRefreshIntervalMS = 0
    Bindings = [pscustomobject]@{
        Visible = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.OvertakePredictionVisible'),false);" }; Mode = 2; TargetPropertyName = 'Visible' }
        Opacity = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return 90*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92)))/100*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.RadarVisualOpacity'),0)))/100;" }; Mode = 2; TargetPropertyName = 'Opacity' }
        Left = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.OvertakePredictionX'),210)-11;" }; Mode = 2; TargetPropertyName = 'Left' }
        Top = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.OvertakePredictionY'),130)-1.5;" }; Mode = 2; TargetPropertyName = 'Top' }
        Rotation = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.OvertakePredictionRotation'),0);" }; Mode = 2; TargetPropertyName = 'Rotation' }
    }
}
$items += $point

function New-OpponentMapPoint([string]$name, [string]$prefix) {
    return [pscustomobject]@{
        '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.ImageItem, SimHub.Plugins'
        Image = 'VehicleMarkerRed'; AutoSize = $false; BackgroundColor = '#00FFFFFF'
        Height = 30; Left = 204; Opacity = 96; Top = 124; Visible = $false
        BlinkPhasisInverted = $false; Width = 15; Name = $name; RenderingSkip = 0; MinimumRefreshIntervalMS = 0
        Bindings = [pscustomobject]@{
            Visible = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.$prefix`Visible'),false);" }; Mode = 2; TargetPropertyName = 'Visible' }
            Opacity = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return 96*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.$prefix`Opacity'),100)))/100*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92)))/100*Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.RadarVisualOpacity'),0)))/100;" }; Mode = 2; TargetPropertyName = 'Opacity' }
            Width = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.TrackPlayerWidth'),13.44);" }; Mode = 2; TargetPropertyName = 'Width' }
            Height = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.TrackPlayerLength'),28);" }; Mode = 2; TargetPropertyName = 'Height' }
            Left = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "var w=isnull(`$prop('IRacingRadarPlugin.TrackPlayerWidth'),13.44);return isnull(`$prop('IRacingRadarPlugin.$prefix`X'),210)-w/2;" }; Mode = 2; TargetPropertyName = 'Left' }
            Top = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "var h=isnull(`$prop('IRacingRadarPlugin.TrackPlayerLength'),28);return isnull(`$prop('IRacingRadarPlugin.$prefix`Y'),130)-h/2;" }; Mode = 2; TargetPropertyName = 'Top' }
            Rotation = [pscustomobject]@{ Formula = [pscustomobject]@{ JSExt = 0; Interpreter = 1; Expression = "return isnull(`$prop('IRacingRadarPlugin.$prefix`Rotation'),0);" }; Mode = 2; TargetPropertyName = 'Rotation' }
        }
    }
}

$items += New-OpponentMapPoint 'Front opponent map point' 'FrontOpponentMap'
$items += New-OpponentMapPoint 'Rear opponent map point' 'RearOpponentMap'
# Draw the player last so a very close opponent moves naturally underneath it
# instead of stopping at an artificial boundary.
$items = @($items | Where-Object { $_.Name -ne 'Player marker' }) + @($player)
$overlay.Screens[0].Items = $items
$overlay.Images = @($overlay.Images | Where-Object {
    $_.Name -ne 'RadarEdgeGlow' -and $_.Name -ne 'VehicleMarkerGray' -and
    $_.Name -ne 'VehicleMarkerPlayer' -and $_.Name -ne 'VehicleMarkerRed'
})
foreach ($resource in @(
    [pscustomobject]@{ Name = 'RadarEdgeGlow'; Width = 260; Height = 260 },
    [pscustomobject]@{ Name = 'VehicleMarkerPlayer'; Width = 96; Height = 200 },
    [pscustomobject]@{ Name = 'VehicleMarkerRed'; Width = 96; Height = 200 }
)) {
    $resourcePath = Join-Path $root ("SimHubPlugin\Overlay\FanResources\" + $resource.Name + '.png')
    $resourceLength = 0
    $resourceHash = ''
    if (Test-Path -LiteralPath $resourcePath) {
        $resourceLength = (Get-Item -LiteralPath $resourcePath).Length
        $resourceHash = (Get-FileHash -LiteralPath $resourcePath -Algorithm MD5).Hash.ToLowerInvariant()
    }
    $overlay.Images += [pscustomobject]@{
        Name = $resource.Name; Extension = '.png'; Modified = $false; Optimized = $true
        Width = $resource.Width; Height = $resource.Height; Length = $resourceLength; MD5 = $resourceHash
    }
}
$json = $overlay | ConvertTo-Json -Depth 100
[IO.File]::WriteAllText($path, $json, [Text.UTF8Encoding]::new($false))
Write-Host 'Added the predicted overtake point to the radar Overlay.'
