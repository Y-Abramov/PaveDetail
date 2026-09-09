# Иконки ленты PaveDetail. Формат Robur: {name}_{16|32}dp_{1|1.5|2|2.5|3}x.png
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot 'icons'
New-Item -ItemType Directory -Force $out | Out-Null

$accent = [System.Drawing.ColorTranslator]::FromHtml('#1064AF')
$dark   = [System.Drawing.ColorTranslator]::FromHtml('#334155')
$scales = @{ '1x' = 1.0; '1.5x' = 1.5; '2x' = 2.0; '2.5x' = 2.5; '3x' = 3.0 }

function Draw-Icon([System.Drawing.Graphics]$g, [string]$name, [double]$s) {
    $accentBrush = New-Object System.Drawing.SolidBrush $accent
    $darkBrush   = New-Object System.Drawing.SolidBrush $dark
    $pen         = New-Object System.Drawing.Pen $dark, ([float](1.0 * $s))

    switch ($name) {
        'pd_module' {
            # стопка слоёв
            $g.FillRectangle($accentBrush, [float](2*$s), [float](4*$s), [float](12*$s), [float](2.5*$s))
            $g.FillRectangle($darkBrush,   [float](2*$s), [float](7.5*$s), [float](12*$s), [float](2*$s))
            $g.FillRectangle($accentBrush, [float](2*$s), [float](10.5*$s), [float](12*$s), [float](3*$s))
        }
        'pd_node' {
            # разрез с выноской
            $g.FillRectangle($accentBrush, [float](2*$s), [float](6*$s), [float](9*$s), [float](2*$s))
            $g.FillRectangle($darkBrush,   [float](2*$s), [float](8*$s), [float](9*$s), [float](4*$s))
            $g.DrawLine($pen, [float](6*$s), [float](7*$s), [float](13*$s), [float](3*$s))
            $g.DrawLine($pen, [float](13*$s), [float](3*$s), [float](15*$s), [float](3*$s))
        }
        'pd_batch' {
            # три разреза в ряд
            foreach ($x in 2, 7, 12) {
                $g.FillRectangle($accentBrush, [float]($x*$s), [float](5*$s), [float](3*$s), [float](2*$s))
                $g.FillRectangle($darkBrush,   [float]($x*$s), [float](7*$s), [float](3*$s), [float](4*$s))
            }
        }
        'pd_report' {
            # таблица
            $g.FillRectangle($accentBrush, [float](2*$s), [float](3*$s), [float](12*$s), [float](2.5*$s))
            for ($i = 0; $i -lt 3; $i++) {
                $y = (6.5 + $i * 2.5) * $s
                $g.FillRectangle($darkBrush, [float](2*$s), [float]$y, [float](12*$s), [float](1.5*$s))
            }
        }
        'pd_table' {
            # таблица с мини-разрезом в первой графе
            $g.FillRectangle($accentBrush, [float](1.5*$s), [float](3*$s), [float](13*$s), [float](2*$s))
            $g.FillRectangle($darkBrush,   [float](2*$s), [float](6.5*$s), [float](3.5*$s), [float](1.5*$s))
            $g.FillRectangle($accentBrush, [float](2*$s), [float](8*$s), [float](3.5*$s), [float](2.5*$s))
            for ($i = 0; $i -lt 3; $i++) {
                $y = (6.5 + $i * 2) * $s
                $g.FillRectangle($darkBrush, [float](6.5*$s), [float]$y, [float](8*$s), [float](1.2*$s))
            }
        }
        'pd_catalog' {
            # книга со штриховкой
            $g.FillRectangle($darkBrush, [float](2*$s), [float](3*$s), [float](12*$s), [float](10*$s))
            $g.FillRectangle($accentBrush, [float](3.5*$s), [float](4.5*$s), [float](9*$s), [float](7*$s))
            $hatch = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([float](0.8*$s))
            for ($x = 4; $x -lt 12; $x += 2) {
                $g.DrawLine($hatch, [float]($x*$s), [float](11.5*$s), [float](($x+2)*$s), [float](4.5*$s))
            }
            $hatch.Dispose()
        }
        'pd_export' {
            # лист со стрелкой вниз
            $g.FillRectangle($darkBrush, [float](2*$s), [float](2*$s), [float](8*$s), [float](10*$s))
            $g.FillRectangle($accentBrush, [float](3.5*$s), [float](3.5*$s), [float](5*$s), [float](1.2*$s))
            $g.FillRectangle($accentBrush, [float](3.5*$s), [float](5.5*$s), [float](5*$s), [float](1.2*$s))
            $arrow = New-Object System.Drawing.Pen $accent, ([float](1.6*$s))
            $g.DrawLine($arrow, [float](12*$s), [float](7*$s), [float](12*$s), [float](14*$s))
            $g.DrawLine($arrow, [float](9.5*$s), [float](11.5*$s), [float](12*$s), [float](14*$s))
            $g.DrawLine($arrow, [float](14.5*$s), [float](11.5*$s), [float](12*$s), [float](14*$s))
            $arrow.Dispose()
        }
    }

    $accentBrush.Dispose(); $darkBrush.Dispose(); $pen.Dispose()
}

foreach ($name in 'pd_module', 'pd_node', 'pd_batch', 'pd_report', 'pd_table', 'pd_catalog', 'pd_export') {
    foreach ($dp in 16, 32) {
        foreach ($sk in $scales.Keys) {
            $px = [int][math]::Round($dp * $scales[$sk])
            $bmp = New-Object System.Drawing.Bitmap $px, $px
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $g.Clear([System.Drawing.Color]::Transparent)
            # рисуем в сетке 16x16, масштабируя под фактический размер
            Draw-Icon $g $name ($px / 16.0)
            $g.Dispose()
            $bmp.Save((Join-Path $out "$($name)_$($dp)dp_$($sk).png"), [System.Drawing.Imaging.ImageFormat]::Png)
            $bmp.Dispose()
        }
    }
}

Write-Host "Готово: $out"
