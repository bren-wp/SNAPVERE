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

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

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

function Test-IsTargetForeground {
    param([Parameter(Mandatory = $true)][IntPtr]$Handle)
    [SnapvereVisualQaNative]::GetForegroundWindow() -eq $Handle
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
    $hasVisualContent = $false

    try {
        for ($attempt = 0; $attempt -lt 3 -and -not $hasVisualContent; $attempt++) {
            foreach ($flag in @(2, 0)) {
                $graphics.Clear([System.Drawing.Color]::Black)
                $hdc = $graphics.GetHdc()
                try {
                    $printed = [SnapvereVisualQaNative]::PrintWindow($Window.Handle, $hdc, [uint32]$flag)
                }
                finally {
                    $graphics.ReleaseHdc($hdc)
                }

                if ($printed -and (Test-BitmapHasVisualContent -Bitmap $bitmap)) {
                    $hasVisualContent = $true
                    $captureMethod = if ($flag -eq 2) { 'PrintWindowFullContent' } else { 'PrintWindow' }
                    break
                }
            }

            if ($hasVisualContent) {
                break
            }

            # Hosted Windows runners may expose a WinUI HWND before DirectComposition
            # has paint available to PrintWindow. A screen-copy fallback is permitted
            # only when Windows confirms that this exact SNAPVERE HWND is foreground.
            # This prevents the runner desktop or another process from being accepted.
            [void][SnapvereVisualQaNative]::SetForegroundWindow($Window.Handle)
            for ($foregroundAttempt = 0; $foregroundAttempt -lt 4; $foregroundAttempt++) {
                if (Test-IsTargetForeground -Handle $Window.Handle) {
                    break
                }
                Start-Sleep -Milliseconds 8
            }

            if (Test-IsTargetForeground -Handle $Window.Handle) {
                $graphics.Clear([System.Drawing.Color]::Black)
                $graphics.CopyFromScreen(
                    $Window.Left,
                    $Window.Top,
                    0,
                    0,
                    [System.Drawing.Size]::new($Window.Width, $Window.Height),
                    [System.Drawing.CopyPixelOperation]::SourceCopy)

                if (Test-BitmapHasVisualContent -Bitmap $bitmap) {
                    $hasVisualContent = $true
                    $captureMethod = 'ForegroundScreenCopy'
                    break
                }
            }

            Start-Sleep -Milliseconds 8
        }

        if (-not $hasVisualContent) {
            $foreground = [SnapvereVisualQaNative]::GetForegroundWindow().ToInt64()
            throw "Rendered SNAPVERE window '$($Window.Title)' did not produce a target-owned visual frame. Target HWND=$($Window.Handle.ToInt64()), foreground HWND=$foreground."
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
        while (-not $process.HasExited -and $stopwatch.ElapsedMilliseconds -lt 12000) {
            $window = Get-CapturableWindows -Process $process |
                Sort-Object { $_.Width * $_.Height } -Descending |
                Select-Object -First 1

            if ($null -ne $window) {
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

            Start-Sleep -Milliseconds 6
        }

        if ($null -eq $snapshot) {
            if ($null -ne $lastCaptureError) {
                throw "No target-owned SNAPVERE window was captured for $EnvironmentVariable. Last capture error: $($lastCaptureError.Message)"
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
    Remove-ProbeMarker -FileName $markerFileName
    $process = Start-ProbeProcess -EnvironmentVariable 'SNAPVERE_SECONDARY_UI_PROBE'
    $expectedNames = @('tray-menu.png', 'options.png', 'language.png', 'about.png')
    $seenWindows = [System.Collections.Generic.HashSet[string]]::new()
    $snapshots = [System.Collections.Generic.List[object]]::new()
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        while (-not $process.HasExited -and $stopwatch.ElapsedMilliseconds -lt 15000 -and $snapshots.Count -lt $expectedNames.Count) {
            foreach ($window in (Get-CapturableWindows -Process $process)) {
                $windowKey = "$($window.Handle.ToInt64())|$($window.Title)|$($window.Width)x$($window.Height)"
                if ($seenWindows.Contains($windowKey)) {
                    continue
                }

                $current = Get-CapturableWindows -Process $process |
                    Where-Object { $_.Handle -eq $window.Handle } |
                    Select-Object -First 1
                if ($null -eq $current) {
                    continue
                }

                $snapshotName = $expectedNames[$snapshots.Count]
                try {
                    $snapshot = Save-WindowSnapshot -Window $current -DestinationPath (Join-Path $resolvedOutputDirectory $snapshotName)
                }
                catch {
                    Remove-Item -LiteralPath (Join-Path $resolvedOutputDirectory $snapshotName) -Force -ErrorAction SilentlyContinue
                    continue
                }

                [void]$seenWindows.Add($windowKey)
                $snapshots.Add($snapshot)
                if ($snapshots.Count -ge $expectedNames.Count) {
                    break
                }
            }

            Start-Sleep -Milliseconds 6
        }

        Wait-ForProcessExit -Process $process
        Assert-ProbeMarker -FileName $markerFileName -ExpectedState 'SECONDARY_UI_READY'

        if ($snapshots.Count -ne $expectedNames.Count) {
            throw "Expected four target-owned rendered secondary UI surfaces but captured $($snapshots.Count)."
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

Write-Host "SNAPVERE visual QA captured six target-owned rendered Windows surfaces:"
foreach ($snapshot in $results) {
    Write-Host "  $($snapshot.File) $($snapshot.Width)x$($snapshot.Height) $([Math]::Round($snapshot.Bytes / 1KB, 1)) KB $($snapshot.CaptureMethod) sha256:$($snapshot.Sha256)"
}
Write-Host "Manifest: $manifestPath"
