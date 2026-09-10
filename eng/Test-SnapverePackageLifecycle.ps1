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
$contractScript = Join-Path $PSScriptRoot 'Assert-SnapvereInstallContract.ps1'

function Write-StartupLogIfPresent {
    if (Test-Path -LiteralPath $startupLog) {
        Write-Host '----- SNAPVERE startup.log -----'
        Get-Content -LiteralPath $startupLog | Write-Host
        Write-Host '--------------------------------'
    }
}

function Invoke-StartupProbe([string] $FilePath, [string] $Name) {
    $marker = Join-Path $env:TEMP 'SNAPVERE/startup-probe.ready'
    Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
    $env:SNAPVERE_STARTUP_PROBE = '1'
    $process = $null

    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList @('--startup-probe') -PassThru
        if (-not $process.WaitForExit(20000)) {
            Write-StartupLogIfPresent
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
            throw "$Name startup probe timed out after 20 seconds."
        }

        if ($process.ExitCode -ne 0) {
            Write-StartupLogIfPresent
            throw "$Name startup probe failed with exit code $($process.ExitCode)."
        }

        if (-not (Test-Path -LiteralPath $marker)) {
            Write-StartupLogIfPresent
            throw "$Name exited successfully but never reported an activated WinUI main window."
        }

        $markerText = Get-Content -LiteralPath $marker -Raw
        $escapedVersion = [Regex]::Escape($Version)
        if ($markerText -notmatch "SNAPVERE $escapedVersion READY") {
            throw "$Name startup marker is invalid: $markerText"
        }

        Write-Host "$Name startup marker: $markerText"
    }
    finally {
        Remove-Item Env:SNAPVERE_STARTUP_PROBE -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
        if ($null -ne $process) { $process.Dispose() }
    }
}

function Invoke-RegionOverlayProbe([string] $FilePath, [string] $Name) {
    $marker = Join-Path $env:TEMP 'SNAPVERE/region-overlay-probe.ready'
    Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
    $env:SNAPVERE_REGION_OVERLAY_PROBE = '1'
    $process = $null

    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList @('--region-overlay-probe') -PassThru
        if (-not $process.WaitForExit(20000)) {
            Write-StartupLogIfPresent
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
            throw "$Name region-overlay probe timed out after 20 seconds."
        }

        if ($process.ExitCode -ne 0) {
            Write-StartupLogIfPresent
            throw "$Name region-overlay probe failed with exit code $($process.ExitCode)."
        }

        if (-not (Test-Path -LiteralPath $marker)) {
            Write-StartupLogIfPresent
            throw "$Name exited successfully but never loaded the Region Capture editor."
        }

        $markerText = Get-Content -LiteralPath $marker -Raw
        $escapedVersion = [Regex]::Escape($Version)
        if ($markerText -notmatch "SNAPVERE $escapedVersion REGION_OVERLAY_READY") {
            throw "$Name region-overlay marker is invalid: $markerText"
        }

        Write-Host "$Name region-overlay marker: $markerText"
    }
    finally {
        Remove-Item Env:SNAPVERE_REGION_OVERLAY_PROBE -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
        if ($null -ne $process) { $process.Dispose() }
    }
}

function Invoke-NormalAppLaunch([string] $FilePath, [string] $Name) {
    Remove-Item Env:SNAPVERE_STARTUP_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_REGION_OVERLAY_PROBE -ErrorAction SilentlyContinue
    $process = Start-Process -FilePath $FilePath -PassThru
    try {
        if ($process.WaitForExit(5000)) {
            Write-StartupLogIfPresent
            throw "$Name exited during normal startup with code $($process.ExitCode)."
        }

        Write-Host "$Name remained alive through the normal startup window (PID $($process.Id))."
    }
    finally {
        try {
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                $null = $process.WaitForExit(5000)
            }
        }
        catch {}
        $process.Dispose()
    }
}

function Invoke-NormalPortableLaunch([string] $FilePath, [string] $Name) {
    Remove-Item Env:SNAPVERE_STARTUP_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_REGION_OVERLAY_PROBE -ErrorAction SilentlyContinue
    $launcher = Start-Process -FilePath $FilePath -PassThru
    try {
        if (-not $launcher.WaitForExit(15000)) {
            try { Stop-Process -Id $launcher.Id -Force -ErrorAction SilentlyContinue } catch {}
            throw "$Name launcher did not return after its startup validation window."
        }

        if ($launcher.ExitCode -ne 0) {
            Write-StartupLogIfPresent
            throw "$Name launcher failed normal startup with code $($launcher.ExitCode)."
        }

        Start-Sleep -Milliseconds 500
        $appProcesses = @(Get-Process -Name Snapvere -ErrorAction SilentlyContinue)
        if ($appProcesses.Count -eq 0) {
            Write-StartupLogIfPresent
            throw "$Name launcher returned success but no SNAPVERE app process remained alive."
        }

        Write-Host "$Name normal startup left $($appProcesses.Count) SNAPVERE process(es) alive."
        foreach ($app in $appProcesses) {
            try { Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue } catch {}
        }
    }
    finally {
        $launcher.Dispose()
    }
}

Remove-Item -LiteralPath $startupLog -Force -ErrorAction SilentlyContinue

$licenseProbe = Start-Process -FilePath $setup -ArgumentList @('--silent') -Wait -PassThru
if ($licenseProbe.ExitCode -ne 2) {
    throw "$Arch Setup without --accept-license must exit with code 2; got $($licenseProbe.ExitCode)."
}

$installProcess = Start-Process -FilePath $setup -ArgumentList @('--silent', '--accept-license') -Wait -PassThru
if ($installProcess.ExitCode -ne 0) {
    throw "$Arch Setup failed to install with code $($installProcess.ExitCode)."
}

& $contractScript -State Installed -InstallDirectory $install -ExpectedVersion $Version

$installedApp = Join-Path $install 'Snapvere.exe'
$installedSetup = Join-Path $install 'SNAPVERE-Setup.exe'
Invoke-StartupProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-RegionOverlayProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-NormalAppLaunch $installedApp "Installed SNAPVERE $Arch"

$uninstallProcess = Start-Process -FilePath $installedSetup -ArgumentList @('--uninstall', '--silent') -Wait -PassThru
if ($uninstallProcess.ExitCode -ne 0) {
    throw "$Arch Setup failed to uninstall with code $($uninstallProcess.ExitCode)."
}

$deadline = (Get-Date).AddSeconds(15)
while ((Test-Path -LiteralPath $installedApp) -and (Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 250
}

& $contractScript -State Removed -InstallDirectory $install -ExpectedVersion $Version

Invoke-StartupProbe $portable "Portable SNAPVERE $Arch"
Invoke-RegionOverlayProbe $portable "Portable SNAPVERE $Arch"
Invoke-NormalPortableLaunch $portable "Portable SNAPVERE $Arch"

Write-Host "SNAPVERE $Version $Arch package lifecycle passed."
