$ErrorActionPreference = 'Stop'

$overlayPath = Join-Path $PSScriptRoot '..\SimHubPlugin\Overlay\iRacing Radar.djson'
$overlay = Get-Content -Raw -Encoding UTF8 $overlayPath | ConvertFrom-Json

function New-FormulaBinding([string]$expression, [string]$target) {
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

foreach ($item in $overlay.Screens[0].Items) {
    if ($null -eq $item.PSObject.Properties['Bindings'] -or $null -eq $item.Bindings) {
        $item | Add-Member -NotePropertyName Bindings -NotePropertyValue ([pscustomobject][ordered]@{})
    }

    $visibleProperty = $item.Bindings.PSObject.Properties['Visible']
    if ($null -eq $visibleProperty) {
        $item.Bindings | Add-Member -NotePropertyName Visible -NotePropertyValue (
            New-FormulaBinding "return isnull(`$prop('IRacingRadarPlugin.RadarVisible'), false);" 'Visible'
        )
    }
    else {
        $expression = [string]$item.Bindings.Visible.Formula.Expression
        if ($expression -notmatch 'IRacingRadarPlugin\.RadarVisible') {
            $body = $expression.Trim()
            $body = [regex]::Replace($body, '^return\s+', '')
            $body = [regex]::Replace($body, ';\s*$', '')
            $item.Bindings.Visible.Formula.Expression =
                "return isnull(`$prop('IRacingRadarPlugin.RadarVisible'), false) && ($body);"
        }
    }

    $opacityProperty = $item.PSObject.Properties['Opacity']
    if ($null -eq $opacityProperty) {
        $item | Add-Member -NotePropertyName Opacity -NotePropertyValue 100
        $baseOpacity = 100
    }
    else {
        $baseOpacity = [double]$item.Opacity
    }

    if ($null -eq $item.Bindings.PSObject.Properties['Opacity']) {
        $opacityExpression = "return $baseOpacity * Math.max(0,Math.min(100,isnull(`$prop('IRacingRadarPlugin.OverlayOpacity'),85))) / 100;"
        $item.Bindings | Add-Member -NotePropertyName Opacity -NotePropertyValue (
            New-FormulaBinding $opacityExpression 'Opacity'
        )
    }
}

$json = $overlay | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path $overlayPath), $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
