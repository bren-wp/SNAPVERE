[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaselineDirectory,

    [Parameter(Mandatory = $true)]
    [string]$CurrentDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [ValidateRange(16, 256)]
    [int]$SampleSize = 64,

    [ValidateRange(0.0, 1.0)]
    [double]$MaxDimensionDeltaRatio = 0.08,

    [ValidateRange(0.0, 1.0)]
    [double]$MaxMeanRgbDifference = 0.18,

    [ValidateRange(0.0, 1.0)]
    [double]$SignificantPixelThreshold = 0.20,

    [ValidateRange(0.0, 1.0)]
    [double]$MaxSignificantPixelRatio = 0.70,

    [ValidateRange(0.01, 1.0)]
    [double]$MinByteSizeRatio = 0.35,

    [ValidateRange(1.0, 10.0)]
    [double]$MaxByteSizeRatio = 3.0
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$baselineRoot = (Resolve-Path -LiteralPath $BaselineDirectory).Path
$currentRoot = (Resolve-Path -LiteralPath $CurrentDirectory).Path
$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputParent = Split-Path -Parent $outputFullPath
if (-not [string]::IsNullOrWhiteSpace($outputParent)) {
    New-Item -ItemType Directory -Force -Path $outputParent | Out-Null
}

try {
    Add-Type -AssemblyName System.Drawing.Common -ErrorAction Stop
}
catch {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
}

$expectedFiles = @(
    'region-capture.png',
    'window-capture.png',
    'tray-menu.png',
    'options.png',
    'language.png',
    'about.png'
)

function Assert-VisualQaArtifactContract {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $manifestPath = Join-Path $Directory 'manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "$Label visual-QA artifact is missing manifest.json."
    }

    $actual = @(Get-ChildItem -LiteralPath $Directory -Filter '*.png' -File |
        Select-Object -ExpandProperty Name |
        Sort-Object)
    $expected = @($expectedFiles | Sort-Object)
    if ($actual.Count -ne $expected.Count -or @(Compare-Object $expected $actual).Count -ne 0) {
        throw "$Label visual-QA artifact must contain exactly the six expected PNG files. Actual: $($actual -join ', ')"
    }

    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    $manifestFiles = @($manifest.Surfaces | ForEach-Object { $_.File } | Sort-Object)
    if ($manifestFiles.Count -ne $expected.Count -or @(Compare-Object $expected $manifestFiles).Count -ne 0) {
        throw "$Label visual-QA manifest does not describe exactly the six expected surfaces."
    }

    return $manifest
}

function New-NormalizedBitmap {
    param(
        [Parameter(Mandatory = $true)][System.Drawing.Bitmap]$Source,
        [Parameter(Mandatory = $true)][int]$Size
    )

    $target = [System.Drawing.Bitmap]::new(
        $Size,
        $Size,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($target)
    try {
        $graphics.Clear([System.Drawing.Color]::Black)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($Source, 0, 0, $Size, $Size)
    }
    finally {
        $graphics.Dispose()
    }

    return $target
}

function Compare-VisualSurface {
    param(
        [Parameter(Mandatory = $true)][string]$FileName
    )

    $baselinePath = Join-Path $baselineRoot $FileName
    $currentPath = Join-Path $currentRoot $FileName
    $baselineFile = Get-Item -LiteralPath $baselinePath
    $currentFile = Get-Item -LiteralPath $currentPath
    $baselineBitmap = [System.Drawing.Bitmap]::new($baselinePath)
    $currentBitmap = [System.Drawing.Bitmap]::new($currentPath)
    $baselineSample = $null
    $currentSample = $null

    try {
        $widthDeltaRatio = [Math]::Abs($currentBitmap.Width - $baselineBitmap.Width) / [double][Math]::Max(1, $baselineBitmap.Width)
        $heightDeltaRatio = [Math]::Abs($currentBitmap.Height - $baselineBitmap.Height) / [double][Math]::Max(1, $baselineBitmap.Height)
        $byteSizeRatio = $currentFile.Length / [double][Math]::Max(1L, $baselineFile.Length)

        $baselineSample = New-NormalizedBitmap -Source $baselineBitmap -Size $SampleSize
        $currentSample = New-NormalizedBitmap -Source $currentBitmap -Size $SampleSize

        $differenceSum = 0.0
        $significantPixels = 0
        $samplePixels = $SampleSize * $SampleSize

        for ($y = 0; $y -lt $SampleSize; $y++) {
            for ($x = 0; $x -lt $SampleSize; $x++) {
                $left = $baselineSample.GetPixel($x, $y)
                $right = $currentSample.GetPixel($x, $y)
                $pixelDifference = (
                    [Math]::Abs([int]$left.R - [int]$right.R) +
                    [Math]::Abs([int]$left.G - [int]$right.G) +
                    [Math]::Abs([int]$left.B - [int]$right.B)) / 765.0
                $differenceSum += $pixelDifference
                if ($pixelDifference -gt $SignificantPixelThreshold) {
                    $significantPixels++
                }
            }
        }

        $meanRgbDifference = $differenceSum / $samplePixels
        $significantPixelRatio = $significantPixels / [double]$samplePixels
        $failures = [System.Collections.Generic.List[string]]::new()

        if ($widthDeltaRatio -gt $MaxDimensionDeltaRatio) {
            $failures.Add("width delta $([Math]::Round($widthDeltaRatio * 100, 2))% exceeds $([Math]::Round($MaxDimensionDeltaRatio * 100, 2))%")
        }
        if ($heightDeltaRatio -gt $MaxDimensionDeltaRatio) {
            $failures.Add("height delta $([Math]::Round($heightDeltaRatio * 100, 2))% exceeds $([Math]::Round($MaxDimensionDeltaRatio * 100, 2))%")
        }
        if ($meanRgbDifference -gt $MaxMeanRgbDifference) {
            $failures.Add("mean RGB difference $([Math]::Round($meanRgbDifference, 4)) exceeds $MaxMeanRgbDifference")
        }
        if ($significantPixelRatio -gt $MaxSignificantPixelRatio) {
            $failures.Add("significant-pixel ratio $([Math]::Round($significantPixelRatio * 100, 2))% exceeds $([Math]::Round($MaxSignificantPixelRatio * 100, 2))%")
        }
        if ($byteSizeRatio -lt $MinByteSizeRatio -or $byteSizeRatio -gt $MaxByteSizeRatio) {
            $failures.Add("PNG byte-size ratio $([Math]::Round($byteSizeRatio, 3)) is outside [$MinByteSizeRatio, $MaxByteSizeRatio]")
        }

        return [pscustomobject]@{
            File = $FileName
            BaselineWidth = $baselineBitmap.Width
            BaselineHeight = $baselineBitmap.Height
            CurrentWidth = $currentBitmap.Width
            CurrentHeight = $currentBitmap.Height
            WidthDeltaRatio = [Math]::Round($widthDeltaRatio, 6)
            HeightDeltaRatio = [Math]::Round($heightDeltaRatio, 6)
            BaselineBytes = $baselineFile.Length
            CurrentBytes = $currentFile.Length
            ByteSizeRatio = [Math]::Round($byteSizeRatio, 6)
            MeanRgbDifference = [Math]::Round($meanRgbDifference, 6)
            SignificantPixelRatio = [Math]::Round($significantPixelRatio, 6)
            Passed = $failures.Count -eq 0
            Failures = @($failures)
        }
    }
    finally {
        if ($null -ne $baselineSample) { $baselineSample.Dispose() }
        if ($null -ne $currentSample) { $currentSample.Dispose() }
        $baselineBitmap.Dispose()
        $currentBitmap.Dispose()
    }
}

$baselineManifest = Assert-VisualQaArtifactContract -Directory $baselineRoot -Label 'Baseline'
$currentManifest = Assert-VisualQaArtifactContract -Directory $currentRoot -Label 'Current'
$comparisons = @($expectedFiles | ForEach-Object { Compare-VisualSurface -FileName $_ })
$failed = @($comparisons | Where-Object { -not $_.Passed })

$report = [pscustomobject]@{
    GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    Baseline = [pscustomobject]@{
        Directory = $baselineRoot
        ProcessArchitecture = $baselineManifest.ProcessArchitecture
    }
    Current = [pscustomobject]@{
        Directory = $currentRoot
        ProcessArchitecture = $currentManifest.ProcessArchitecture
    }
    Policy = [pscustomobject]@{
        SampleSize = $SampleSize
        MaxDimensionDeltaRatio = $MaxDimensionDeltaRatio
        MaxMeanRgbDifference = $MaxMeanRgbDifference
        SignificantPixelThreshold = $SignificantPixelThreshold
        MaxSignificantPixelRatio = $MaxSignificantPixelRatio
        MinByteSizeRatio = $MinByteSizeRatio
        MaxByteSizeRatio = $MaxByteSizeRatio
    }
    Passed = $failed.Count -eq 0
    Surfaces = $comparisons
}
$report | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $outputFullPath -Encoding utf8

Write-Host 'SNAPVERE rendered UI regression comparison:'
foreach ($comparison in $comparisons) {
    $state = if ($comparison.Passed) { 'PASS' } else { 'FAIL' }
    Write-Host ("  {0,-20} {1} mean={2:N4} significant={3:N1}% size={4:N3} dimensions={5}x{6}->{7}x{8}" -f `
        $comparison.File,
        $state,
        $comparison.MeanRgbDifference,
        ($comparison.SignificantPixelRatio * 100),
        $comparison.ByteSizeRatio,
        $comparison.BaselineWidth,
        $comparison.BaselineHeight,
        $comparison.CurrentWidth,
        $comparison.CurrentHeight)
    foreach ($failure in $comparison.Failures) {
        Write-Host "    - $failure"
    }
}
Write-Host "Comparison report: $outputFullPath"

if ($failed.Count -gt 0) {
    throw "Rendered UI regression gate failed for $($failed.Count) surface(s): $($failed.File -join ', ')"
}
