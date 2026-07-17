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

function New-SideItem([string]$side, [bool]$glow) {
    $left = if ($side -eq 'Left') { if ($glow) { 173 } else { 177 } } else { if ($glow) { 221 } else { 225 } }
    $width = if ($glow) { 26 } else { 18 }
    $height = if ($glow) { 50 } else { 42 }
    $opacity = if ($glow) { 24 } else { 82 }
    $topOffset = if ($glow) { -4 } else { 0 }
    $topOffsetText = if ($topOffset -lt 0) { [string]$topOffset } else { "+" + [string]$topOffset }
    $nameSuffix = if ($glow) { 'glow' } else { 'marker' }

    [pscustomobject][ordered]@{
        '$type' = 'SimHub.Plugins.OutputPlugins.GraphicalDash.Models.RectangleItem, SimHub.Plugins'
        IsRectangleItem = $true
        BackgroundColor = if ($glow) { '#55FF1220' } else { '#D9F51C25' }
        BorderColor = '#00FFFFFF'
        Height = $height
        Left = $left
        Opacity = $opacity
        Top = 109 + $topOffset
        Visible = $false
        BlinkPhasisInverted = $false
        Width = $width
        Name = "$side opponent $nameSuffix"
        RenderingSkip = 0
        MinimumRefreshIntervalMS = 0
        Bindings = [pscustomobject][ordered]@{
            Visible = New-Binding "return isnull(`$prop('IRacingRadarPlugin.RadarVisible'), false) && isnull(`$prop('IRacingRadarPlugin.$side`Visible'), false);" 'Visible'
            Top = New-Binding "return isnull(`$prop('IRacingRadarPlugin.$side`Top'), 109)$topOffsetText;" 'Top'
            Opacity = New-Binding "return $opacity * Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),92))) / 100;" 'Opacity'
        }
    }
}

$overlay.BaseWidth = 420
$overlay.BaseHeight = 260
$overlay.Metadata.Width = 420
$overlay.Metadata.Height = 260
$overlay.DashboardDebugManager.WindowPositionSettings.Position = '100,100,420,260'

$items = [System.Collections.Generic.List[object]]::new()
$sideInserted = $false

foreach ($item in $overlay.Screens[0].Items) {
    if ($item.Name -match '^(Left|Right) opponent fade' -or
        $item.Name -in @('Left opponent marker','Right opponent marker','Left opponent glow','Right opponent glow')) {
        continue
    }

    switch ($item.Name) {
        'Radar circle outline' {
            $item.Left = 81; $item.Top = 1; $item.Width = 258; $item.Height = 258
            $item.BorderStyle.RadiusTopLeft = 129; $item.BorderStyle.RadiusTopRight = 129
            $item.BorderStyle.RadiusBottomLeft = 129; $item.BorderStyle.RadiusBottomRight = 129
        }
        'Front far semicircle' {
            $item.Left = 80; $item.Top = 0; $item.Width = 260; $item.Height = 130
            $item.BorderStyle.RadiusTopLeft = 130; $item.BorderStyle.RadiusTopRight = 130
        }
        'Rear far semicircle' {
            $item.Left = 80; $item.Top = 130; $item.Width = 260; $item.Height = 130
            $item.BorderStyle.RadiusBottomLeft = 130; $item.BorderStyle.RadiusBottomRight = 130
        }
        'Front far distance' { $item.Left = 169; $item.Top = 32 }
        'Rear far distance' { $item.Left = 169; $item.Top = 200 }
        'Center spine' { $item.Left = 209; $item.Top = 8; $item.Height = 244 }
        'Front range tick' { $item.Left = 201; $item.Top = 28; $item.Width = 18 }
        'Center range tick' { $item.Left = 199; $item.Top = 129; $item.Width = 22 }
        'Rear range tick' { $item.Left = 201; $item.Top = 230; $item.Width = 18 }
        'Left distance' { $item.Left = 93 }
        'Right distance' { $item.Left = 255 }
        'Front opponent glow' { $item.Left = 196; $item.Width = 28; $item.Height = 52 }
        'Rear opponent glow' { $item.Left = 196; $item.Width = 28; $item.Height = 52 }
        'Nearest car ahead' { $item.Left = 201; $item.Width = 18; $item.Height = 42 }
        'Nearest car behind' { $item.Left = 201; $item.Width = 18; $item.Height = 42 }
        'Front distance' { $item.Left = 228; $item.Width = 82; $item.Text = 'F 0m' }
        'Rear distance' { $item.Left = 228; $item.Width = 82; $item.Text = 'B 0m' }
        'Player marker' { $item.Left = 201; $item.Top = 109; $item.Width = 18; $item.Height = 42 }
    }

    foreach ($binding in $item.Bindings.PSObject.Properties) {
        $expression = [string]$binding.Value.Formula.Expression
        if ($expression.Length -gt 0) {
            $binding.Value.Formula.Expression = $expression.Replace(', 86)', ', 109)').Replace("'),85)", "'),92)")
        }
    }

    if ($item.Name -eq 'Front opponent glow' -or $item.Name -eq 'Rear opponent glow') {
        $item.Bindings.Top.Formula.Expression = $item.Bindings.Top.Formula.Expression.Replace('-4;', '-5;')
    }

    if ($item.Name -eq 'Front distance') {
        $item.Bindings.Text.Formula.Expression = "return 'F ' + format(Math.abs(isnull(`$prop('IRacingRadarPlugin.FrontRelativeMeters'),0)),'0') + 'm';"
    }
    elseif ($item.Name -eq 'Rear distance') {
        $item.Bindings.Text.Formula.Expression = "return 'B ' + format(Math.abs(isnull(`$prop('IRacingRadarPlugin.RearRelativeMeters'),0)),'0') + 'm';"
    }

    if (-not $sideInserted -and $item.Name -eq 'Left distance') {
        $items.Add((New-SideItem 'Left' $true))
        $items.Add((New-SideItem 'Right' $true))
        $items.Add((New-SideItem 'Left' $false))
        $items.Add((New-SideItem 'Right' $false))
        $sideInserted = $true
    }

    $items.Add($item)
}

$overlay.Screens[0].Items = $items.ToArray()
$json = $overlay | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path $overlayPath), $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
