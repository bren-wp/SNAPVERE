[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$AppPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$resolvedAppPath = (Resolve-Path -LiteralPath $AppPath).Path
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $resolvedOutputDirectory | Out-Null
Get-ChildItem -LiteralPath $resolvedOutputDirectory -File -ErrorAction SilentlyContinue | Remove-Item -Force

try {
    Add-Type -AssemblyName System.Drawing.Common -ErrorAction Stop
}
catch {
    Add-Type -AssemblyName System.Drawing -ErrorAction Stop
}

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public sealed class SnapvereVisualQaWindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public static class SnapvereVisualQaNative
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint flags);

    public static SnapvereVisualQaWindowInfo[] GetVisibleWindows(int processId)
    {
        var windows = new List<SnapvereVisualQaWindowInfo>();
        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out var ownerProcessId);
            if (ownerProcessId != (uint)processId || !IsWindowVisible(hWnd))
            {
                return true;
            }

            if (!GetWindowRect(hWnd, out var rect))
            {
                return true;
            }

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width < 120 || height < 100)
            {
                return true;
            }

            var length = Math.Max(0, GetWindowTextLength(hWnd));
            var titleBuffer = new StringBuilder(length + 1);
            _ = GetWindowText(hWnd, titleBuffer, titleBuffer.Capacity);

            windows.Add(new SnapvereVisualQaWindowInfo
            {
                Handle = hWnd,
                Title = titleBuffer.ToString(),
                Left = rect.Left,
                Top = rect.Top,
                Width = width,
                Height = height
            });
            return true;
        }, IntPtr.Zero);

        return windows.ToArray();
    }
}
'@

function Get-ProbeMarkerPath {
    param([Parameter(Mandatory = $true)][string]$FileName)
    Join-Path (Join-Path ([System.IO.Path]::GetTempPath()) 'SNAPVERE') $FileName
}

function Remove-ProbeMarker {
    param([Parameter(Mandatory = $true)][string]$FileName)
    Remove-Item -LiteralPath (Get-ProbeMarkerPath -FileName $FileName) -Force -ErrorAction SilentlyContinue
}

function Get-CapturableWindows {
    param([Parameter(Mandatory = $true)][System.Diagnostics.Process]$Process)

    if ($Process.HasExited) {
        return @()
    }

    @([SnapvereVisualQaNative]::GetVisibleWindows($Process.Id) | Where-Object {
        $_.Title -ne 'SNAPVERE Runtime Host'
    })
}

function Get-BitmapSampleFingerprint {
    param([Parameter(Mandatory = $true)][System.Drawing.Bitmap]$Bitmap)

    $builder = [System.Text.StringBuilder]::new()
    $stepX = [Math]::Max(1, [int][Math]::Floor($Bitmap.Width / 18.0))
    $stepY = [Math]::Max(1, [int][Math]::Floor($Bitmap.Height / 18.0))

    for ($y = 0; $y -lt $Bitmap.Height; $y += $stepY) {
        for ($x = 0; $x -lt $Bitmap.Width; $x += $stepX) {
            [void]$builder.Append($Bitmap.GetPixel($x, $y).ToArgb())
            [void]$builder.Append('|')
        }
    }

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($builder.ToString())
    $hash = [System.Security.Cryptography.SHA256]::HashData($bytes)
    [Convert]::ToHexString($hash)
}

function Test-BitmapHasVisualContent {
    param([Parameter(Mandatory = $true)][System.Drawing.Bitmap]$Bitmap)

    $unique = [System.Collections.Generic.HashSet[int]]::new()
    $stepX = [Math]::Max(1, [int][Math]::Floor($Bitmap.Width / 18.0))
    $stepY = [Math]::Max(1, [int][Math]::Floor($Bitmap.Height / 18.0))

    for ($y = 0; $y -lt $Bitmap.Height; $y += $stepY) {
        for ($x = 0; $x -lt $Bitmap.Width; $x += $stepX) {
            [void]$unique.Add($Bitmap.GetPixel($x, $y).ToArgb())
            if ($unique.Count -ge 12) {
                return $true
            }
        }
    }

    return $false
}

function Save-WindowSnapshot {
    param(
        [Parameter(Mandatory = $true)]$Window,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    $bitmap = [System.Drawing.Bitmap]::new(
        $Window.Width,
        $Window.Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $captureMethod = $null
    $hasStableVisualContent = $false

    try {
        # Use HWND-owned PrintWindow pixels only. A rendered frame is accepted only
        # after the same sampled visual fingerprint appears on three consecutive
        # captures, separated by a frame-scale delay. This avoids accepting the
        # first non-empty WinUI chrome before DirectComposition content settles.
        foreach ($flag in @(2, 0)) {
            $previousFingerprint = $null
            $stableCount = 0
            $maxAttempts = if ($flag -eq 2) { 6 } else { 4 }

            for ($attempt = 0; $attempt -lt $maxAttempts; $attempt++) {
                $graphics.Clear([System.Drawing.Color]::Black)
                $hdc = $graphics.GetHdc()
                try {
                    $printed = [SnapvereVisualQaNative]::PrintWindow($Window.Handle, $hdc, [uint32]$flag)
                }
                finally {
                    $graphics.ReleaseHdc($hdc)
                }

                if ($printed -and (Test-BitmapHasVisualContent -Bitmap $bitmap)) {
                    $fingerprint = Get-BitmapSampleFingerprint -Bitmap $bitmap
                    if ($fingerprint -eq $previousFingerprint) {
                        $stableCount++
                    }
                    else {
                        $previousFingerprint = $fingerprint
                        $stableCount = 1
                    }

                    if ($stableCount -ge 3) {
                        $hasStableVisualContent = $true
                        $captureMethod = if ($flag -eq 2) { 'StablePrintWindowFullContent' } else { 'StablePrintWindow' }
                        break
                    }
                }
                else {
                    $previousFingerprint = $null
                    $stableCount = 0
                }

                Start-Sleep -Milliseconds 20
            }

            if ($hasStableVisualContent) {
                break
            }
        }

        if (-not $hasStableVisualContent) {
            throw "Rendered SNAPVERE window '$($Window.Title)' did not produce three consecutive stable target-owned PrintWindow frames. Target HWND=$($Window.Handle.ToInt64())."
        }

        $bitmap.Save($DestinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }

    $file = Get-Item -LiteralPath $DestinationPath
    if ($file.Length -lt 4096) {
        throw "Visual QA snapshot '$($file.Name)' is unexpectedly small: $($file.Length) bytes."
    }

    [pscustomobject]@{
        File = $file.Name
        Title = $Window.Title
        Width = $Window.Width
        Height = $Window.Height
        Bytes = $file.Length
        Sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        CaptureMethod = $captureMethod
        TargetOwned = $true
    }
}

function Start-ProbeProcess {
    param([Parameter(Mandatory = $true)][string]$EnvironmentVariable)

    $previous = [Environment]::GetEnvironmentVariable($EnvironmentVariable, 'Process')
    try {
        [Environment]::SetEnvironmentVariable($EnvironmentVariable, '1', 'Process')
        Start-Process -FilePath $resolvedAppPath -WorkingDirectory (Split-Path -Parent $resolvedAppPath) -PassThru
    }
    finally {
        [Environment]::SetEnvironmentVariable($EnvironmentVariable, $previous, 'Process')
    }
}

function Wait-ForProcessExit {
    param(
        [Parameter(Mandatory = $true)][System.Diagnostics.Process]$Process,
        [int]$TimeoutMilliseconds = 15000
    )

    if (-not $Process.WaitForExit($TimeoutMilliseconds)) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
        throw "Visual QA probe process $($Process.Id) did not exit within $TimeoutMilliseconds ms."
    }

    if ($Process.ExitCode -ne 0) {
        throw "Visual QA probe process $($Process.Id) exited with code $($Process.ExitCode)."
    }
}

function Assert-ProbeMarker {
    param(
        [Parameter(Mandatory = $true)][string]$FileName,
        [Parameter(Mandatory = $true)][string]$ExpectedState
    )

    $markerPath = Get-ProbeMarkerPath -FileName $FileName
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw "Visual QA probe marker '$FileName' was not created."
    }

    $marker = Get-Content -Raw -LiteralPath $markerPath
    if ($marker -notmatch [Regex]::Escape($ExpectedState)) {
        throw "Visual QA probe marker '$FileName' does not contain '$ExpectedState'. Actual: $marker"
    }
}

function Invoke-SingleSurfaceProbe {
    param(
        [Parameter(Mandatory = $true)][string]$EnvironmentVariable,
        [Parameter(Mandatory = $true)][string]$MarkerFileName,
        [Parameter(Mandatory = $true)][string]$ExpectedMarkerState,
        [Parameter(Mandatory = $true)][string]$SnapshotFileName
    )

    Remove-ProbeMarker -FileName $MarkerFileName
    $process = Start-ProbeProcess -EnvironmentVariable $EnvironmentVariable
    $snapshot = $null
    $lastCaptureError = $null
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $markerPath = Get-ProbeMarkerPath -FileName $MarkerFileName
        while (-not $process.HasExited -and $stopwatch.ElapsedMilliseconds -lt 10000 -and -not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
            Start-Sleep -Milliseconds 10
        }

        if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
            throw "SNAPVERE render-ready marker '$MarkerFileName' was not created before capture."
        }

        Assert-ProbeMarker -FileName $MarkerFileName -ExpectedState $ExpectedMarkerState

        while (-not $process.HasExited -and $stopwatch.ElapsedMilliseconds -lt 12000) {
            $window = Get-CapturableWindows -Process $process |
                Sort-Object { $_.Width * $_.Height } -Descending |
                Select-Object -First 1

            if ($null -ne $window) {
                Start-Sleep -Milliseconds 25
                $current = Get-CapturableWindows -Process $process |
                    Where-Object { $_.Handle -eq $window.Handle } |
                    Select-Object -First 1

                if ($null -ne $current) {
                    try {
                        $snapshot = Save-WindowSnapshot -Window $current -DestinationPath (Join-Path $resolvedOutputDirectory $SnapshotFileName)
                        break
                    }
                    catch {
                        $lastCaptureError = $_.Exception
                        Remove-Item -LiteralPath (Join-Path $resolvedOutputDirectory $SnapshotFileName) -Force -ErrorAction SilentlyContinue
                    }
                }
            }

            Start-Sleep -Milliseconds 8
        }

        if ($null -eq $snapshot) {
            if ($null -ne $lastCaptureError) {
                throw "No stable target-owned SNAPVERE window was captured for $EnvironmentVariable. Last capture error: $($lastCaptureError.Message)"
            }
            throw "No rendered SNAPVERE window was captured for $EnvironmentVariable."
        }

        Wait-ForProcessExit -Process $process
        Assert-ProbeMarker -FileName $MarkerFileName -ExpectedState $ExpectedMarkerState
        return $snapshot
    }
    finally {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
        $process.Dispose()
    }
}

function Invoke-SecondarySurfaceProbe {
    $markerFileName = 'secondary-ui-probe.ready'
    $surfaces = @(
        [pscustomobject]@{ Snapshot = 'tray-menu.png'; Ready = 'secondary-tray.ready'; State = 'SECONDARY_TRAY_READY'; Acknowledgement = 'secondary-tray.captured' },
        [pscustomobject]@{ Snapshot = 'options.png'; Ready = 'secondary-options.ready'; State = 'SECONDARY_OPTIONS_READY'; Acknowledgement = 'secondary-options.captured' },
        [pscustomobject]@{ Snapshot = 'language.png'; Ready = 'secondary-language.ready'; State = 'SECONDARY_LANGUAGE_READY'; Acknowledgement = 'secondary-language.captured' },
        [pscustomobject]@{ Snapshot = 'about.png'; Ready = 'secondary-about.ready'; State = 'SECONDARY_ABOUT_READY'; Acknowledgement = 'secondary-about.captured' }
    )

    Remove-ProbeMarker -FileName $markerFileName
    foreach ($surface in $surfaces) {
        Remove-ProbeMarker -FileName $surface.Ready
        Remove-ProbeMarker -FileName $surface.Acknowledgement
    }

    $process = Start-ProbeProcess -EnvironmentVariable 'SNAPVERE_SECONDARY_UI_PROBE'
    $snapshots = [System.Collections.Generic.List[object]]::new()

    try {
        foreach ($surface in $surfaces) {
            $readyPath = Get-ProbeMarkerPath -FileName $surface.Ready
            $readyWait = [System.Diagnostics.Stopwatch]::StartNew()
            while (-not $process.HasExited -and $readyWait.ElapsedMilliseconds -lt 6000 -and -not (Test-Path -LiteralPath $readyPath -PathType Leaf)) {
                Start-Sleep -Milliseconds 10
            }

            if (-not (Test-Path -LiteralPath $readyPath -PathType Leaf)) {
                throw "Secondary UI surface '$($surface.Snapshot)' did not publish render-ready marker '$($surface.Ready)'."
            }
            Assert-ProbeMarker -FileName $surface.Ready -ExpectedState $surface.State

            $snapshot = $null
            $lastCaptureError = $null
            $captureWait = [System.Diagnostics.Stopwatch]::StartNew()
            while (-not $process.HasExited -and $captureWait.ElapsedMilliseconds -lt 5000 -and $null -eq $snapshot) {
                $windows = @(Get-CapturableWindows -Process $process | Sort-Object { $_.Width * $_.Height } -Descending)
                foreach ($window in $windows) {
                    $current = Get-CapturableWindows -Process $process |
                        Where-Object { $_.Handle -eq $window.Handle } |
                        Select-Object -First 1
                    if ($null -eq $current) {
                        continue
                    }

                    try {
                        $snapshot = Save-WindowSnapshot -Window $current -DestinationPath (Join-Path $resolvedOutputDirectory $surface.Snapshot)
                        break
                    }
                    catch {
                        $lastCaptureError = $_.Exception
                        Remove-Item -LiteralPath (Join-Path $resolvedOutputDirectory $surface.Snapshot) -Force -ErrorAction SilentlyContinue
                    }
                }

                if ($null -eq $snapshot) {
                    Start-Sleep -Milliseconds 10
                }
            }

            if ($null -eq $snapshot) {
                if ($null -ne $lastCaptureError) {
                    throw "Secondary UI surface '$($surface.Snapshot)' did not produce a stable target-owned frame. Last capture error: $($lastCaptureError.Message)"
                }
                throw "Secondary UI surface '$($surface.Snapshot)' did not expose a capturable SNAPVERE window."
            }

            $snapshots.Add($snapshot)
            $acknowledgementPath = Get-ProbeMarkerPath -FileName $surface.Acknowledgement
            $acknowledgementDirectory = Split-Path -Parent $acknowledgementPath
            New-Item -ItemType Directory -Force -Path $acknowledgementDirectory | Out-Null
            Set-Content -LiteralPath $acknowledgementPath -Value "CAPTURED $($surface.Snapshot)" -Encoding utf8
        }

        Wait-ForProcessExit -Process $process
        Assert-ProbeMarker -FileName $markerFileName -ExpectedState 'SECONDARY_UI_READY'

        if ($snapshots.Count -ne $surfaces.Count) {
            throw "Expected four stable target-owned rendered secondary UI surfaces but captured $($snapshots.Count)."
        }

        return $snapshots.ToArray()
    }
    finally {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
        $process.Dispose()
    }
}

$results = [System.Collections.Generic.List[object]]::new()
$results.Add((Invoke-SingleSurfaceProbe `
    -EnvironmentVariable 'SNAPVERE_REGION_OVERLAY_PROBE' `
    -MarkerFileName 'region-overlay-probe.ready' `
    -ExpectedMarkerState 'REGION_OVERLAY_READY' `
    -SnapshotFileName 'region-capture.png'))
$results.Add((Invoke-SingleSurfaceProbe `
    -EnvironmentVariable 'SNAPVERE_WINDOW_OVERLAY_PROBE' `
    -MarkerFileName 'window-overlay-probe.ready' `
    -ExpectedMarkerState 'WINDOW_OVERLAY_READY' `
    -SnapshotFileName 'window-capture.png'))
foreach ($snapshot in (Invoke-SecondarySurfaceProbe)) {
    $results.Add($snapshot)
}

$expectedFiles = @(
    'region-capture.png',
    'window-capture.png',
    'tray-menu.png',
    'options.png',
    'language.png',
    'about.png'
)
$actualFiles = @(Get-ChildItem -LiteralPath $resolvedOutputDirectory -Filter '*.png' -File | Select-Object -ExpandProperty Name | Sort-Object)
if ($actualFiles.Count -ne $expectedFiles.Count -or @(Compare-Object ($expectedFiles | Sort-Object) $actualFiles).Count -ne 0) {
    throw "Visual QA must produce exactly six PNG files. Actual: $($actualFiles -join ', ')"
}

$manifest = [pscustomobject]@{
    GeneratedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    App = $resolvedAppPath
    ProcessArchitecture = 'x64'
    CaptureContractVersion = 2
    Surfaces = @($results)
}
$manifestPath = Join-Path $resolvedOutputDirectory 'manifest.json'
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $manifestPath -Encoding utf8

Write-Host "SNAPVERE visual QA captured six stable target-owned rendered Windows surfaces:"
foreach ($snapshot in $results) {
    Write-Host "  $($snapshot.File) $($snapshot.Width)x$($snapshot.Height) $([Math]::Round($snapshot.Bytes / 1KB, 1)) KB $($snapshot.CaptureMethod) sha256:$($snapshot.Sha256)"
}
Write-Host "Manifest: $manifestPath"
