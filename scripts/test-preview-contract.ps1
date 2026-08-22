$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$overlayPath = Join-Path $root 'SimHubPlugin\Overlay\iRacing Radar.djson'
$previewPath = Join-Path $root 'Configurator\OverlayRadarPreviewControl.cs'
$overlay = Get-Content -Raw -LiteralPath $overlayPath -Encoding UTF8 | ConvertFrom-Json
$preview = Get-Content -Raw -LiteralPath $previewPath -Encoding UTF8

if ($overlay.BaseWidth -ne 420 -or $overlay.BaseHeight -ne 260) {
    throw 'Overlay canvas no longer matches the 420 x 260 configurator preview canvas.'
}

$items = @{
    'Radar circle outline' = '84,4,252,252,#52101620'
    'Center spine' = '209,8,2,244,#66D8E1E9'
    'Front range tick' = '201,28,18,2,#5CD8E1E9'
    'Center range tick' = '199,129,22,2,#78E4EBF2'
    'Rear range tick' = '201,230,18,2,#5CD8E1E9'
    'Left position rail' = '175,34,2,192,#80D51B2A'
    'Right position rail' = '243,34,2,192,#80D51B2A'
}
$trackSegments = @($overlay.Screens[0].Items | Where-Object Name -like 'Local track segment *')
if ($trackSegments.Count -ne 47) { throw 'Overlay must contain 47 continuous local-track segments.' }
foreach ($segment in $trackSegments) {
    if (-not $segment.Bindings.Rotation -or $segment.BorderStyle.RadiusTopLeft -lt 9) {
        throw "Local-track segment is not a rounded solid connection: $($segment.Name)"
    }
}
if ($preview -notmatch 'DrawReferenceTrack') { throw 'Preview local-track rendering is missing.' }

foreach ($name in $items.Keys) {
    $item = $overlay.Screens[0].Items | Where-Object Name -eq $name | Select-Object -First 1
    if (-not $item) { throw "Missing Overlay item: $name" }
    $actual = "$($item.Left),$($item.Top),$($item.Width),$($item.Height),$($item.BackgroundColor)"
    if ($actual -ne $items[$name]) { throw "Overlay contract changed for ${name}: $actual" }
}

$circle = $overlay.Screens[0].Items | Where-Object Name -eq 'Radar circle outline' | Select-Object -First 1
if ($circle.BorderStyle.BorderTop -ne 0 -or $circle.BorderStyle.BorderColor -ne '#00DDE6EE') {
    throw 'Radar circle base must not use a hard border.'
}
$softEdge = $overlay.Screens[0].Items | Where-Object Name -eq 'Radar feathered edge' | Select-Object -First 1
if (-not $softEdge -or $softEdge.Image -ne 'RadarEdgeGlow' -or
    $softEdge.Bindings.Visible.Formula.Expression -notmatch 'RadarVisible') {
    throw 'Radar feathered edge is incomplete.'
}
$player = $overlay.Screens[0].Items | Where-Object Name -eq 'Player marker' | Select-Object -First 1
$prediction = $overlay.Screens[0].Items | Where-Object Name -eq 'Predicted overtake point' | Select-Object -First 1
if (-not $prediction -or $prediction.BackgroundColor -ne '#FF4DF28B' -or
    $prediction.Width -ne 22 -or $prediction.Height -ne 3 -or
    $prediction.Bindings.Visible.Formula.Expression -notmatch 'OvertakePredictionVisible') {
    throw 'Predicted overtake point is missing from the Overlay.'
}
foreach ($name in @('Front opponent map point', 'Rear opponent map point')) {
    $mapPoint = $overlay.Screens[0].Items | Where-Object Name -eq $name | Select-Object -First 1
    if (-not $mapPoint -or $mapPoint.Image -ne 'VehicleMarkerRed' -or
        $mapPoint.Bindings.Visible.Formula.Expression -notmatch 'OpponentMapVisible' -or
        $mapPoint.Bindings.Rotation.Formula.Expression -notmatch 'OpponentMapRotation' -or
        $mapPoint.Bindings.Opacity.Formula.Expression -notmatch 'OpponentMapOpacity') {
        throw "Opponent map point is missing from the Overlay: $name"
    }
}
if ($player.Image -ne 'VehicleMarkerPlayer' -or
    $player.Bindings.Width.Formula.Expression -notmatch 'TrackPlayerWidth' -or
    $player.Bindings.Width.Formula.Expression -match 'PlayerMarkerScalePercent' -or
    $player.Bindings.Height.Formula.Expression -notmatch 'TrackPlayerLength') {
    throw 'Player marker does not use the shared final dimensions and player resource.'
}
$opponent = $overlay.Screens[0].Items | Where-Object Name -eq 'Left opponent marker' | Select-Object -First 1
foreach ($name in @('Left opponent marker', 'Right opponent marker')) {
    $opponent = $overlay.Screens[0].Items | Where-Object Name -eq $name | Select-Object -First 1
    if ($opponent.Image -ne 'VehicleMarkerRed' -or
        $opponent.Bindings.Width.Formula.Expression -notmatch 'TrackPlayerWidth' -or
        $opponent.Bindings.Width.Formula.Expression -match 'PlayerMarkerScalePercent' -or
        $opponent.Bindings.Height.Formula.Expression -notmatch 'TrackPlayerLength') {
        throw "${name} does not use the shared final dimensions."
    }
}
$playerIndex = [Array]::IndexOf(@($overlay.Screens[0].Items), $player)
$frontMap = $overlay.Screens[0].Items | Where-Object Name -eq 'Front opponent map point' | Select-Object -First 1
if ($playerIndex -le [Array]::IndexOf(@($overlay.Screens[0].Items), $frontMap)) {
    throw 'Player marker must render above close map opponents.'
}

$patterns = @(
    'RectangleF\(84, 4, 252, 252\), "#52101620"',
    'RectangleF\(209, 8, 2, 244\), "#66D8E1E9"',
    'RectangleF\(199, 129, 22, 2\), "#78E4EBF2"',
    'VehicleMarkerSize\(',
    'DrawVehicleMarker\(',
    'DrawReferenceTrack\(g, radarOpacity, front && farVisible && !nearVisible\)',
    'showPrediction && Settings.CatchEstimateEnabled',
    'OvertakePredictionEnabled',
    'DrawRadarBackground\(g, radarOpacity\)',
    'DrawResource\(g, "RadarEdgeGlow", new RectangleF\(80, 0, 260, 260\)',
    'farTextOpacity = \(100 - blend\) \* proximity',
    'textOpacity = blend \*'
)
foreach ($pattern in $patterns) {
    if ($preview -notmatch $pattern) { throw "Preview is missing an Overlay item or formula: $pattern" }
}
if ($preview -match 'DrawInactiveMessage') {
    throw 'The preview must be empty when the actual Overlay is hidden.'
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead("$overlayPath.ressources")
try {
    $frames = @($archive.Entries | Where-Object Name -match '^(Front|Rear)(Fan|GreenArc)\d+\.png$')
    if ($frames.Count -ne 240) { throw "Expected 240 original Overlay frames, found $($frames.Count)." }
}
finally { $archive.Dispose() }

Write-Host 'PASS preview matches the SimHub Overlay visual contract'
