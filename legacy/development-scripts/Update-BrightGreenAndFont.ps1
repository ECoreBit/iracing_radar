$ErrorActionPreference = 'Stop'

$overlayPath = Join-Path $PSScriptRoot '..\SimHubPlugin\Overlay\iRacing Radar.djson'
$overlay = Get-Content -Raw -Encoding UTF8 $overlayPath | ConvertFrom-Json

foreach ($item in $overlay.Screens[0].Items) {
    if ($item.Name -in @('Front far semicircle', 'Rear far semicircle')) {
        $item.BackgroundColor = '#3355FF88'
        $item.BorderStyle.BorderColor = '#FF55FF88'
    }

    if ($item.Name -in @('Front far distance', 'Rear far distance')) {
        $item.TextColor = '#FF9CFFC2'
    }

    if ($item.Name -in @('Front far distance', 'Rear far distance', 'Front distance', 'Rear distance')) {
        $item.FontSize = 22
        $item.Left = 110
        $item.Width = 200
        $item.Height = 40
        $item.Top = if ($item.Name.StartsWith('Front')) { 42 } else { 178 }
        $item.Bindings.FontSize.Formula.Expression =
            "return isnull(`$prop('IRacingRadarPlugin.LabelFontSize'),22);"
    }
}

$json = $overlay | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path $overlayPath), $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
