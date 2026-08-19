<#
.SYNOPSIS
    Regenerates the multi-resolution .ico files from assets/logo.png.

.DESCRIPTION
    Windows picks the closest available size out of an .ico and scales it to fit.
    An icon that contains only one small image therefore looks blurry everywhere
    it is shown larger, so the generated icons carry every size the shell asks
    for: 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixels.

    Sizes up to 48px are stored as 32-bit BGRA DIBs, which every version of the
    shell reads. Larger sizes are stored as PNG to keep the file small; PNG
    entries are supported from Windows Vista onwards.

    Run this after changing assets/logo.png:
        pwsh scripts/generate-icons.ps1
#>
[CmdletBinding()]
param(
    [string]$Source,
    [string[]]$Destination
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Source) { $Source = Join-Path $root '..\assets\logo.png' }
if (-not $Destination) {
    $Destination = @(
        (Join-Path $root '..\assets\logo.ico'),
        (Join-Path $root '..\md2loop\Assets\AppIcon.ico')
    )
}

# Sizes below this are written as DIB, the rest as PNG.
$PngThreshold = 64
$Sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

function Resize-Icon {
    param([System.Drawing.Image]$Image, [int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

        # Clamp sampling at the edges so downscaling does not bleed transparent
        # pixels from outside the image into the border.
        $attributes = New-Object System.Drawing.Imaging.ImageAttributes
        try {
            $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
            $rect = New-Object System.Drawing.Rectangle 0, 0, $Size, $Size
            $graphics.DrawImage($Image, $rect, 0, 0, $Image.Width, $Image.Height,
                [System.Drawing.GraphicsUnit]::Pixel, $attributes)
        }
        finally { $attributes.Dispose() }
    }
    finally { $graphics.Dispose() }

    return $bitmap
}

function ConvertTo-PngBytes {
    param([System.Drawing.Bitmap]$Bitmap)

    $stream = New-Object System.IO.MemoryStream
    try {
        $Bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        # The comma stops PowerShell from unrolling the array into the pipeline.
        return , $stream.ToArray()
    }
    finally { $stream.Dispose() }
}

function ConvertTo-DibBytes {
    param([System.Drawing.Bitmap]$Bitmap)

    $width = $Bitmap.Width
    $height = $Bitmap.Height

    $rect = New-Object System.Drawing.Rectangle 0, 0, $width, $height
    $data = $Bitmap.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $stride = $data.Stride
        $pixels = New-Object byte[] ($stride * $height)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
    }
    finally { $Bitmap.UnlockBits($data) }

    # An AND mask row is 1 bit per pixel, padded to a 4-byte boundary.
    $maskStride = [int][Math]::Floor(($width + 31) / 32) * 4
    $xorSize = $width * 4 * $height
    $andSize = $maskStride * $height

    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter $stream
    try {
        # BITMAPINFOHEADER. Height covers the XOR image plus the AND mask.
        $writer.Write([uint32]40)
        $writer.Write([int32]$width)
        $writer.Write([int32]($height * 2))
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]0)       # BI_RGB
        $writer.Write([uint32]($xorSize + $andSize))
        $writer.Write([int32]0); $writer.Write([int32]0)
        $writer.Write([uint32]0); $writer.Write([uint32]0)

        # XOR image, bottom-up. GDI+ already gives us BGRA.
        for ($y = $height - 1; $y -ge 0; $y--) {
            $writer.Write($pixels, $y * $stride, $width * 4)
        }

        # AND mask. The alpha channel carries transparency for 32bpp icons, so
        # the mask is all zeroes, but it must still be present.
        $writer.Write((New-Object byte[] $andSize))

        $writer.Flush()
        # The comma stops PowerShell from unrolling the array into the pipeline.
        return , $stream.ToArray()
    }
    finally { $writer.Dispose() }
}

function Write-IconFile {
    param([string]$Path, [hashtable]$Images)

    $ordered = @($Sizes | ForEach-Object { [pscustomobject]@{ Size = $_; Data = [byte[]]$Images[$_] } })

    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter $stream
    try {
        # ICONDIR
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$ordered.Count)

        # ICONDIRENTRY records are fixed width, so the first image starts after all of them.
        $offset = 6 + (16 * $ordered.Count)
        foreach ($image in $ordered) {
            # 256 is encoded as 0 because the field is a single byte.
            $dimension = if ($image.Size -ge 256) { 0 } else { $image.Size }
            $writer.Write([byte]$dimension)
            $writer.Write([byte]$dimension)
            $writer.Write([byte]0)          # palette size, 0 for truecolour
            $writer.Write([byte]0)          # reserved
            $writer.Write([uint16]1)        # colour planes
            $writer.Write([uint16]32)       # bits per pixel
            $writer.Write([uint32]$image.Data.Length)
            $writer.Write([uint32]$offset)
            $offset += $image.Data.Length
        }

        foreach ($image in $ordered) { $writer.Write($image.Data) }

        $writer.Flush()
        $full = [System.IO.Path]::GetFullPath($Path)
        [System.IO.File]::WriteAllBytes($full, $stream.ToArray())
        Write-Host ("  wrote {0} ({1:N0} bytes, {2} sizes)" -f $full, $stream.Length, $ordered.Count)
    }
    finally { $writer.Dispose() }
}

$sourcePath = [System.IO.Path]::GetFullPath($Source)
if (-not (Test-Path $sourcePath)) { throw "Source image not found: $sourcePath" }

Write-Host "Generating icons from $sourcePath"
$sourceImage = [System.Drawing.Image]::FromFile($sourcePath)
try {
    if ($sourceImage.Width -lt 256) {
        throw "Source image is only $($sourceImage.Width)px wide; a 256px icon cannot be generated from it."
    }

    $images = @{}
    foreach ($size in $Sizes) {
        $bitmap = Resize-Icon -Image $sourceImage -Size $size
        try {
            $images[$size] = if ($size -ge $PngThreshold) {
                ConvertTo-PngBytes -Bitmap $bitmap
            }
            else {
                ConvertTo-DibBytes -Bitmap $bitmap
            }
        }
        finally { $bitmap.Dispose() }
    }
}
finally { $sourceImage.Dispose() }

foreach ($path in $Destination) { Write-IconFile -Path $path -Images $images }

Write-Host "Done."
