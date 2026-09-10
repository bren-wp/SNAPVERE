param(
    [Parameter(Mandatory = $true)]
    [string] $SetupPath,

    [Parameter(Mandatory = $true)]
    [string] $PortablePath,

    [Parameter(Mandatory = $true)]
    [string] $Version,

    [Parameter(Mandatory = $true)]
    [ValidateSet('x64', 'x86')]
    [string] $Arch
)

$ErrorActionPreference = 'Stop'
$setup = (Resolve-Path -LiteralPath $SetupPath).Path
$portable = (Resolve-Path -LiteralPath $PortablePath).Path
$install = Join-Path $env:LOCALAPPDATA 'Programs/SNAPVERE'
$startupLog = Join-Path $env:LOCALAPPDATA 'SNAPVERE/Logs/startup.log'
$setupUiMarker = Join-Path $env:TEMP 'SNAPVERE/setup-ui-probe.ready'
$trayMarker = Join-Path $env:TEMP 'SNAPVERE/tray-startup-probe.ready'

function Write-StartupLogIfPresent {
    if (Test-Path -LiteralPath $startupLog) {
        Write-Host '----- SNAPVERE startup.log -----'
        Get-Content -LiteralPath $startupLog | Write-Host
        Write-Host '--------------------------------'
    }
}

function Stop-SnapvereProcesses {
    Get-Process -Name Snapvere -ErrorAction SilentlyContinue | ForEach-Object {
        try { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue } catch {}
    }
    Start-Sleep -Milliseconds 350
}

function Assert-TrayOnlyProcess([System.Diagnostics.Process] $Process, [string] $Name) {
    Start-Sleep -Seconds 3
    if ($Process.HasExited) {
        Write-StartupLogIfPresent
        throw "$Name exited during tray-first startup. Exit code: $($Process.ExitCode)."
    }

    $Process.Refresh()
    if ($Process.MainWindowHandle -ne 0) {
        throw "$Name opened a top-level main window during normal tray-first startup. HWND=$($Process.MainWindowHandle)."
    }

    Write-Host "$Name stayed alive without a visible main window. PID=$($Process.Id)."
}

function Get-SingleSnapvereProcess([string] $Name) {
    $deadline = (Get-Date).AddSeconds(8)
    do {
        $processes = @(Get-Process -Name Snapvere -ErrorAction SilentlyContinue)
        if ($processes.Count -eq 1) {
            return $processes[0]
        }
        if ($processes.Count -gt 1) {
            throw "$Name left multiple SNAPVERE app processes running."
        }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    Write-StartupLogIfPresent
    throw "$Name did not leave a SNAPVERE app process running."
}

function Invoke-TrayProbe([string] $FilePath, [string] $Name) {
    Remove-Item -LiteralPath $trayMarker -Force -ErrorAction SilentlyContinue
    $env:SNAPVERE_TRAY_STARTUP_PROBE = '1'
    $process = $null
    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList @('--tray-startup-probe') -PassThru
        if (-not $process.WaitForExit(20000)) {
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
            Write-StartupLogIfPresent
            throw "$Name tray startup probe timed out."
        }
        if ($process.ExitCode -ne 0) {
            Write-StartupLogIfPresent
            throw "$Name tray startup probe failed with exit code $($process.ExitCode)."
        }
        if (-not (Test-Path -LiteralPath $trayMarker)) {
            Write-StartupLogIfPresent
            throw "$Name did not confirm that its native tray host initialized."
        }
        $markerText = Get-Content -LiteralPath $trayMarker -Raw
        $escapedVersion = [Regex]::Escape($Version)
        if ($markerText -notmatch "SNAPVERE $escapedVersion TRAY_READY") {
            throw "$Name tray startup marker is invalid: $markerText"
        }
        Write-Host "$Name tray startup probe: $markerText"
    }
    finally {
        Remove-Item Env:SNAPVERE_TRAY_STARTUP_PROBE -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $trayMarker -Force -ErrorAction SilentlyContinue
        if ($null -ne $process) { $process.Dispose() }
    }
}

Stop-SnapvereProcesses
Remove-Item -LiteralPath $setupUiMarker -Force -ErrorAction SilentlyContinue

$setupProbe = Start-Process -FilePath $setup -ArgumentList @('--ui-probe') -PassThru
if (-not $setupProbe.WaitForExit(20000)) {
    try { Stop-Process -Id $setupProbe.Id -Force -ErrorAction SilentlyContinue } catch {}
    throw "$Arch Setup UI probe timed out."
}
if ($setupProbe.ExitCode -ne 0) {
    throw "$Arch Setup UI probe failed with exit code $($setupProbe.ExitCode)."
}
if (-not (Test-Path -LiteralPath $setupUiMarker)) {
    throw "$Arch Setup exited without materializing its UI probe form."
}
Write-Host "$Arch Setup UI materialized successfully."
Remove-Item -LiteralPath $setupUiMarker -Force -ErrorAction SilentlyContinue
$setupProbe.Dispose()

$installProcess = Start-Process -FilePath $setup -ArgumentList @('--silent', '--accept-license') -Wait -PassThru
if ($installProcess.ExitCode -ne 0) {
    throw "$Arch Setup failed to install for tray-first validation: $($installProcess.ExitCode)."
}
$installProcess.Dispose()

$installedApp = Join-Path $install 'Snapvere.exe'
$installedSetup = Join-Path $install 'SNAPVERE-Setup.exe'
if (-not (Test-Path -LiteralPath $installedApp -PathType Leaf)) {
    throw "$Arch installed application is missing: $installedApp"
}

Invoke-TrayProbe $installedApp "Installed SNAPVERE $Arch"
Stop-SnapvereProcesses

$installed = Start-Process -FilePath $installedApp -PassThru
try {
    Assert-TrayOnlyProcess $installed "Installed SNAPVERE $Arch normal launch"
}
finally {
    try { if (-not $installed.HasExited) { Stop-Process -Id $installed.Id -Force -ErrorAction SilentlyContinue } } catch {}
    $installed.Dispose()
}
Stop-SnapvereProcesses

$uninstallProcess = Start-Process -FilePath $installedSetup -ArgumentList @('--uninstall', '--silent') -Wait -PassThru
if ($uninstallProcess.ExitCode -ne 0) {
    throw "$Arch Setup failed to uninstall after tray-first validation: $($uninstallProcess.ExitCode)."
}
$uninstallProcess.Dispose()

Invoke-TrayProbe $portable "Portable SNAPVERE $Arch"
Stop-SnapvereProcesses

$portableLauncher = Start-Process -FilePath $portable -PassThru
if (-not $portableLauncher.WaitForExit(15000)) {
    try { Stop-Process -Id $portableLauncher.Id -Force -ErrorAction SilentlyContinue } catch {}
    throw "$Arch Portable launcher did not return after starting the tray-only app."
}
if ($portableLauncher.ExitCode -ne 0) {
    Write-StartupLogIfPresent
    throw "$Arch Portable launcher failed normal startup: $($portableLauncher.ExitCode)."
}
$portableLauncher.Dispose()

$portableApp = Get-SingleSnapvereProcess "Portable SNAPVERE $Arch normal launch"
try {
    Assert-TrayOnlyProcess $portableApp "Portable SNAPVERE $Arch normal launch"
}
finally {
    try { if (-not $portableApp.HasExited) { Stop-Process -Id $portableApp.Id -Force -ErrorAction SilentlyContinue } } catch {}
    $portableApp.Dispose()
}
Stop-SnapvereProcesses

Write-Host "SNAPVERE $Version $Arch tray-first EXE startup validation passed."
