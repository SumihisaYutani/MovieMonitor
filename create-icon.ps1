# PowerShell script to create ICO file from SVG

# Check if we have ImageMagick or other conversion tool
$svgPath = "src/Resources/Icons/app-icon.svg"
$icoPath = "src/Resources/Icons/app-icon.ico"

# For now, we'll create a simple ico file using .NET
Add-Type -AssemblyName System.Drawing

# Create a simple icon using .NET Graphics
$bitmap = New-Object System.Drawing.Bitmap(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Set high quality rendering
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# Create gradient background
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush([System.Drawing.Point]::new(0,0), [System.Drawing.Point]::new(256,256), [System.Drawing.Color]::FromArgb(33,150,243), [System.Drawing.Color]::FromArgb(25,118,210))
$graphics.FillEllipse($brush, 8, 8, 240, 240)

# Draw film strip
$filmBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(66,66,66))
$graphics.FillRectangle($filmBrush, 64, 80, 128, 96)

# Draw film holes
$holeBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(102,102,102))
$graphics.FillEllipse($holeBrush, 74, 90, 12, 12)
$graphics.FillEllipse($holeBrush, 74, 122, 12, 12)
$graphics.FillEllipse($holeBrush, 74, 154, 12, 12)
$graphics.FillEllipse($holeBrush, 170, 90, 12, 12)
$graphics.FillEllipse($holeBrush, 170, 122, 12, 12)
$graphics.FillEllipse($holeBrush, 170, 154, 12, 12)

# Draw screen area
$screenBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.FillRectangle($screenBrush, 100, 96, 56, 64)

# Draw play button
$playBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(33,150,243))
$playPoints = @(
    [System.Drawing.Point]::new(118, 120),
    [System.Drawing.Point]::new(118, 136),
    [System.Drawing.Point]::new(142, 128)
)
$graphics.FillPolygon($playBrush, $playPoints)

# Save as ICO
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
$stream = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
$icon.Save($stream)
$stream.Close()

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$brush.Dispose()
$filmBrush.Dispose()
$holeBrush.Dispose()
$screenBrush.Dispose()
$playBrush.Dispose()

Write-Host "Icon created: $icoPath"