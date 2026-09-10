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
$startupRunKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$startupRunValueName = 'SNAPVERE'

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

function Invoke-TrayStartupProbe([string] $FilePath, [string] $Name) {
    $marker = Join-Path $env:TEMP 'SNAPVERE/tray-startup-probe.ready'
    Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
    $env:SNAPVERE_TRAY_STARTUP_PROBE = '1'
    $process = $null

    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList @('--tray-startup-probe') -PassThru
        if (-not $process.WaitForExit(20000)) {
            Write-StartupLogIfPresent
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
            throw "$Name tray-startup probe timed out after 20 seconds."
        }

        if ($process.ExitCode -ne 0) {
            Write-StartupLogIfPresent
            throw "$Name tray-startup probe failed with exit code $($process.ExitCode)."
        }

        if (-not (Test-Path -LiteralPath $marker)) {
            Write-StartupLogIfPresent
            throw "$Name exited successfully but never initialized the tray-first startup path."
        }

        $markerText = Get-Content -LiteralPath $marker -Raw
        $escapedVersion = [Regex]::Escape($Version)
        if ($markerText -notmatch "SNAPVERE $escapedVersion TRAY_READY") {
            throw "$Name tray-startup marker is invalid: $markerText"
        }

        Write-Host "$Name tray-startup marker: $markerText"
    }
    finally {
        Remove-Item Env:SNAPVERE_TRAY_STARTUP_PROBE -ErrorAction SilentlyContinue
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

function Invoke-WindowOverlayProbe([string] $FilePath, [string] $Name) {
    $marker = Join-Path $env:TEMP 'SNAPVERE/window-overlay-probe.ready'
    Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
    $env:SNAPVERE_WINDOW_OVERLAY_PROBE = '1'
    $process = $null

    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList @('--window-overlay-probe') -PassThru
        if (-not $process.WaitForExit(20000)) {
            Write-StartupLogIfPresent
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
            throw "$Name window-overlay probe timed out after 20 seconds."
        }

        if ($process.ExitCode -ne 0) {
            Write-StartupLogIfPresent
            throw "$Name window-overlay probe failed with exit code $($process.ExitCode)."
        }

        if (-not (Test-Path -LiteralPath $marker)) {
            Write-StartupLogIfPresent
            throw "$Name exited successfully but never loaded the Window Capture picker."
        }

        $markerText = Get-Content -LiteralPath $marker -Raw
        $escapedVersion = [Regex]::Escape($Version)
        if ($markerText -notmatch "SNAPVERE $escapedVersion WINDOW_OVERLAY_READY") {
            throw "$Name window-overlay marker is invalid: $markerText"
        }

        Write-Host "$Name window-overlay marker: $markerText"
    }
    finally {
        Remove-Item Env:SNAPVERE_WINDOW_OVERLAY_PROBE -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
        if ($null -ne $process) { $process.Dispose() }
    }
}

function Clear-ProbeEnvironment {
    Remove-Item Env:SNAPVERE_STARTUP_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_TRAY_STARTUP_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_REGION_OVERLAY_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_WINDOW_OVERLAY_PROBE -ErrorAction SilentlyContinue
}

function Invoke-NormalAppLaunch([string] $FilePath, [string] $Name) {
    Clear-ProbeEnvironment
    $process = Start-Process -FilePath $FilePath -PassThru
    try {
        if ($process.WaitForExit(5000)) {
            Write-StartupLogIfPresent
            throw "$Name exited during normal tray-first startup with code $($process.ExitCode)."
        }

        Write-Host "$Name remained alive through the normal tray-first startup window (PID $($process.Id))."
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
    Clear-ProbeEnvironment
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

        Write-Host "$Name normal tray-first startup left $($appProcesses.Count) SNAPVERE process(es) alive."
        foreach ($app in $appProcesses) {
            try { Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue } catch {}
        }
    }
    finally {
        $launcher.Dispose()
    }
}

Clear-ProbeEnvironment
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
Invoke-TrayStartupProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-RegionOverlayProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-WindowOverlayProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-NormalAppLaunch $installedApp "Installed SNAPVERE $Arch"

if (-not (Test-Path -LiteralPath $startupRunKey)) {
    $null = New-Item -Path $startupRunKey -Force
}
$expectedInstalledStartupCommand = '"' + $installedApp + '"'
Set-ItemProperty -LiteralPath $startupRunKey -Name $startupRunValueName -Value $expectedInstalledStartupCommand -Type String
$registeredStartupCommand = (Get-ItemProperty -LiteralPath $startupRunKey -Name $startupRunValueName).$startupRunValueName
if ($registeredStartupCommand -ne $expectedInstalledStartupCommand) {
    throw "$Arch lifecycle could not establish the installed SNAPVERE startup registration test precondition."
}

$uninstallProcess = Start-Process -FilePath $installedSetup -ArgumentList @('--uninstall', '--silent') -Wait -PassThru
if ($uninstallProcess.ExitCode -ne 0) {
    throw "$Arch Setup failed to uninstall with code $($uninstallProcess.ExitCode)."
}

$deadline = (Get-Date).AddSeconds(15)
while ((Test-Path -LiteralPath $installedApp) -and (Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 250
}

& $contractScript -State Removed -InstallDirectory $install -ExpectedVersion $Version

$startupAfterUninstall = Get-ItemProperty -LiteralPath $startupRunKey -Name $startupRunValueName -ErrorAction SilentlyContinue
if ($null -ne $startupAfterUninstall -and $null -ne $startupAfterUninstall.$startupRunValueName) {
    throw "$Arch uninstall left the installed SNAPVERE startup registration behind."
}

Invoke-StartupProbe $portable "Portable SNAPVERE $Arch"
Invoke-TrayStartupProbe $portable "Portable SNAPVERE $Arch"
Invoke-RegionOverlayProbe $portable "Portable SNAPVERE $Arch"
Invoke-WindowOverlayProbe $portable "Portable SNAPVERE $Arch"
Invoke-NormalPortableLaunch $portable "Portable SNAPVERE $Arch"

Clear-ProbeEnvironment
Write-Host "SNAPVERE $Version $Arch package lifecycle passed."
