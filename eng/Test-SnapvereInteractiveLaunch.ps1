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
    Start-Sleep -Milliseconds 300
}

function Wait-ForVisibleMainWindow([System.Diagnostics.Process] $Process, [string] $Name) {
    $deadline = (Get-Date).AddSeconds(12)
    while ((Get-Date) -lt $deadline) {
        if ($Process.HasExited) {
            Write-StartupLogIfPresent
            throw "$Name exited before showing a window. Exit code: $($Process.ExitCode)."
        }

        $Process.Refresh()
        if ($Process.MainWindowHandle -ne 0) {
            Write-Host "$Name showed a visible top-level window. PID=$($Process.Id), HWND=$($Process.MainWindowHandle)."
            return
        }

        Start-Sleep -Milliseconds 250
    }

    Write-StartupLogIfPresent
    throw "$Name stayed alive but never showed a visible top-level window."
}

function Assert-BackgroundOnly([System.Diagnostics.Process] $Process, [string] $Name) {
    Start-Sleep -Seconds 3
    if ($Process.HasExited) {
        Write-StartupLogIfPresent
        throw "$Name exited during background startup. Exit code: $($Process.ExitCode)."
    }

    $Process.Refresh()
    if ($Process.MainWindowHandle -ne 0) {
        throw "$Name unexpectedly showed a window during --background startup."
    }

    Write-Host "$Name remained alive without a visible main window during --background startup."
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
    throw "$Arch Setup exited without showing its UI probe form."
}
Write-Host "$Arch Setup UI opened successfully."
Remove-Item -LiteralPath $setupUiMarker -Force -ErrorAction SilentlyContinue
$setupProbe.Dispose()

$installProcess = Start-Process -FilePath $setup -ArgumentList @('--silent', '--accept-license') -Wait -PassThru
if ($installProcess.ExitCode -ne 0) {
    throw "$Arch Setup failed to install for interactive launch validation: $($installProcess.ExitCode)."
}
$installProcess.Dispose()

$installedApp = Join-Path $install 'Snapvere.exe'
$installedSetup = Join-Path $install 'SNAPVERE-Setup.exe'
if (-not (Test-Path -LiteralPath $installedApp -PathType Leaf)) {
    throw "$Arch installed application is missing: $installedApp"
}

$manual = Start-Process -FilePath $installedApp -PassThru
try {
    Wait-ForVisibleMainWindow $manual "Installed SNAPVERE $Arch manual launch"
}
finally {
    try { if (-not $manual.HasExited) { Stop-Process -Id $manual.Id -Force -ErrorAction SilentlyContinue } } catch {}
    $manual.Dispose()
}
Stop-SnapvereProcesses

$background = Start-Process -FilePath $installedApp -ArgumentList @('--background') -PassThru
try {
    Assert-BackgroundOnly $background "Installed SNAPVERE $Arch"
}
finally {
    try { if (-not $background.HasExited) { Stop-Process -Id $background.Id -Force -ErrorAction SilentlyContinue } } catch {}
    $background.Dispose()
}
Stop-SnapvereProcesses

$uninstallProcess = Start-Process -FilePath $installedSetup -ArgumentList @('--uninstall', '--silent') -Wait -PassThru
if ($uninstallProcess.ExitCode -ne 0) {
    throw "$Arch Setup failed to uninstall after interactive launch validation: $($uninstallProcess.ExitCode)."
}
$uninstallProcess.Dispose()

$portableLauncher = Start-Process -FilePath $portable -PassThru
if (-not $portableLauncher.WaitForExit(15000)) {
    try { Stop-Process -Id $portableLauncher.Id -Force -ErrorAction SilentlyContinue } catch {}
    throw "$Arch Portable launcher did not return after manual startup."
}
if ($portableLauncher.ExitCode -ne 0) {
    Write-StartupLogIfPresent
    throw "$Arch Portable launcher failed manual startup: $($portableLauncher.ExitCode)."
}
$portableLauncher.Dispose()
$portableApp = Get-SingleSnapvereProcess "Portable SNAPVERE $Arch manual launch"
try {
    Wait-ForVisibleMainWindow $portableApp "Portable SNAPVERE $Arch manual launch"
}
finally {
    try { if (-not $portableApp.HasExited) { Stop-Process -Id $portableApp.Id -Force -ErrorAction SilentlyContinue } } catch {}
    $portableApp.Dispose()
}
Stop-SnapvereProcesses

$portableBackgroundLauncher = Start-Process -FilePath $portable -ArgumentList @('--background') -PassThru
if (-not $portableBackgroundLauncher.WaitForExit(15000)) {
    try { Stop-Process -Id $portableBackgroundLauncher.Id -Force -ErrorAction SilentlyContinue } catch {}
    throw "$Arch Portable launcher did not return after background startup."
}
if ($portableBackgroundLauncher.ExitCode -ne 0) {
    Write-StartupLogIfPresent
    throw "$Arch Portable launcher failed background startup: $($portableBackgroundLauncher.ExitCode)."
}
$portableBackgroundLauncher.Dispose()
$portableBackgroundApp = Get-SingleSnapvereProcess "Portable SNAPVERE $Arch background launch"
try {
    Assert-BackgroundOnly $portableBackgroundApp "Portable SNAPVERE $Arch"
}
finally {
    try { if (-not $portableBackgroundApp.HasExited) { Stop-Process -Id $portableBackgroundApp.Id -Force -ErrorAction SilentlyContinue } } catch {}
    $portableBackgroundApp.Dispose()
}
Stop-SnapvereProcesses

Write-Host "SNAPVERE $Version $Arch interactive EXE launch validation passed."
