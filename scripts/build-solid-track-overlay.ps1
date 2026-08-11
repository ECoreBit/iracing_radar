$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$path = Join-Path $root 'SimHubPlugin\Overlay\iRacing Radar.djson'
$overlay = Get-Content -Raw -LiteralPath $path -Encoding UTF8 | ConvertFrom-Json
$items = @($overlay.Screens[0].Items)

for ($number = 1; $number -lt 48; $number++) {
    $index = $number.ToString('00')
    $next = ($number + 1).ToString('00')
    $item = $items | Where-Object {
        $_.Name -eq "Local track point $index" -or $_.Name -eq "Local track segment $index"
    } | Select-Object -First 1
    if (-not $item) { throw "Local track item $index was not found." }

    $x1 = "IRacingRadarPlugin.TrackPoint${index}X"
    $y1 = "IRacingRadarPlugin.TrackPoint${index}Y"
    $x2 = "IRacingRadarPlugin.TrackPoint${next}X"
    $y2 = "IRacingRadarPlugin.TrackPoint${next}Y"
    $v1 = "IRacingRadarPlugin.TrackPoint${index}Visible"
    $v2 = "IRacingRadarPlugin.TrackPoint${next}Visible"
    $width = "IRacingRadarPlugin.TrackRoadWidth"

    $item.Name = "Local track segment $index"
    $item.Left = 164
    $item.Top = 121
    $item.Width = 92
    $item.Height = 18
    $item.BorderStyle.RadiusTopLeft = 20
    $item.BorderStyle.RadiusTopRight = 20
    $item.BorderStyle.RadiusBottomLeft = 20
    $item.BorderStyle.RadiusBottomRight = 20
    $item | Add-Member -NotePropertyName Rotation -NotePropertyValue 0.0 -Force

    $item.Bindings.Visible.Formula.Expression =
        "return isnull(`$prop('$v1'),false) && isnull(`$prop('$v2'),false);"
    $item.Bindings.Left.Formula.Expression =
        "var x1=isnull(`$prop('$x1'),210),y1=isnull(`$prop('$y1'),130),x2=isnull(`$prop('$x2'),210),y2=isnull(`$prop('$y2'),130),w=Math.max(2,isnull(`$prop('$width'),18)),l=Math.sqrt((x2-x1)*(x2-x1)+(y2-y1)*(y2-y1));return (x1+x2)/2-(l+w)/2;"
    $item.Bindings.Top.Formula.Expression =
        "var y1=isnull(`$prop('$y1'),130),y2=isnull(`$prop('$y2'),130),w=Math.max(2,isnull(`$prop('$width'),18));return (y1+y2)/2-w/2;"
    $item.Bindings.Width.Formula.Expression =
        "var x1=isnull(`$prop('$x1'),210),y1=isnull(`$prop('$y1'),130),x2=isnull(`$prop('$x2'),210),y2=isnull(`$prop('$y2'),130),w=Math.max(2,isnull(`$prop('$width'),18));return Math.sqrt((x2-x1)*(x2-x1)+(y2-y1)*(y2-y1))+w;"
    $item.Bindings.Height.Formula.Expression =
        "return Math.max(2,isnull(`$prop('$width'),18));"
    $rotation = [pscustomobject]@{
        Formula = [pscustomobject]@{
            JSExt = 0
            Interpreter = 1
            Expression = "var x1=isnull(`$prop('$x1'),210),y1=isnull(`$prop('$y1'),130),x2=isnull(`$prop('$x2'),210),y2=isnull(`$prop('$y2'),130);return Math.atan2(y2-y1,x2-x1)*180/Math.PI;"
        }
        Mode = 2
        TargetPropertyName = 'Rotation'
    }
    $item.Bindings | Add-Member -NotePropertyName Rotation -NotePropertyValue $rotation -Force
}

$overlay.Screens[0].Items = @($items | Where-Object {
    $_.Name -ne 'Local track point 48' -and $_.Name -ne 'Local track segment 48'
})
$json = $overlay | ConvertTo-Json -Depth 100
[IO.File]::WriteAllText($path, $json, [Text.UTF8Encoding]::new($false))
Write-Host 'Built 47 continuous solid local-track segments.'
