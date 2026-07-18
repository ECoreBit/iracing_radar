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
    'Radar circle outline' = '81,1,258,258,#52101620'
    'Center spine' = '209,8,2,244,#66D8E1E9'
    'Front range tick' = '201,28,18,2,#5CD8E1E9'
    'Center range tick' = '199,129,22,2,#78E4EBF2'
    'Rear range tick' = '201,230,18,2,#5CD8E1E9'
    'Player marker' = '201,109,18,42,#FF727E8A'
    'Left opponent marker' = '167,109,18,42,#F0E31B2C'
    'Right opponent marker' = '235,109,18,42,#F0E31B2C'
    'Left position rail' = '175,34,2,192,#80D51B2A'
    'Right position rail' = '243,34,2,192,#80D51B2A'
}
foreach ($name in $items.Keys) {
    $item = $overlay.Screens[0].Items | Where-Object Name -eq $name | Select-Object -First 1
    if (-not $item) { throw "Missing Overlay item: $name" }
    $actual = "$($item.Left),$($item.Top),$($item.Width),$($item.Height),$($item.BackgroundColor)"
    if ($actual -ne $items[$name]) { throw "Overlay contract changed for ${name}: $actual" }
}

$circle = $overlay.Screens[0].Items | Where-Object Name -eq 'Radar circle outline' | Select-Object -First 1
if ($circle.BorderStyle.RadiusTopLeft -ne 129 -or $circle.BorderStyle.BorderTop -ne 2 -or
    $circle.BorderStyle.BorderColor -ne '#88DDE6EE') { throw 'Radar circle style contract changed.' }
$player = $overlay.Screens[0].Items | Where-Object Name -eq 'Player marker' | Select-Object -First 1
if ($player.BorderStyle.RadiusTopLeft -ne 7 -or $player.BorderStyle.BorderColor -ne '#B8E8EDF2') {
    throw 'Player marker style contract changed.'
}
$opponent = $overlay.Screens[0].Items | Where-Object Name -eq 'Left opponent marker' | Select-Object -First 1
if ($opponent.BorderStyle.RadiusTopLeft -ne 7 -or $opponent.BorderStyle.BorderColor -ne '#B8FF7A82') {
    throw 'Opponent marker style contract changed.'
}

$patterns = @(
    'RectangleF\(81, 1, 258, 258\), "#52101620"',
    'RectangleF\(209, 8, 2, 244\), "#66D8E1E9"',
    'RectangleF\(199, 129, 22, 2\), "#78E4EBF2"',
    'RectangleF\(201, 109, 18, 42\), "#FF727E8A"',
    '129, "#88DDE6EE", 2',
    '7, "#B8E8EDF2", 1',
    '7, "#B8FF7A82", 1',
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
