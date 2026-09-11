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

function Invoke-StartupProbe(
    [string] $FilePath,
    [string] $Name,
    [int] $TimeoutMilliseconds = 20000
) {
    $marker = Join-Path $env:TEMP 'SNAPVERE/startup-probe.ready'
    Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
    $env:SNAPVERE_STARTUP_PROBE = '1'
    $process = $null

    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList @('--startup-probe') -PassThru
        if (-not $process.WaitForExit($TimeoutMilliseconds)) {
            Write-StartupLogIfPresent
            try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch {}
            $timeoutSeconds = [Math]::Ceiling($TimeoutMilliseconds / 1000.0)
            throw "$Name startup probe timed out after $timeoutSeconds seconds."
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

function Invoke-SecondaryUiProbe([string] $FilePath, [string] $Name) {
    $sessionId = [Guid]::NewGuid().ToString('N')
    $markerFileName = "secondary-ui-probe.$sessionId.ready"
    $marker = Join-Path $env:TEMP "SNAPVERE/$markerFileName"
    $surfaces = @(
        [pscustomobject]@{ Stem = 'secondary-tray'; State = 'SECONDARY_TRAY_READY' },
        [pscustomobject]@{ Stem = 'secondary-options'; State = 'SECONDARY_OPTIONS_READY' },
        [pscustomobject]@{ Stem = 'secondary-language'; State = 'SECONDARY_LANGUAGE_READY' },
        [pscustomobject]@{ Stem = 'secondary-about'; State = 'SECONDARY_ABOUT_READY' }
    )
    Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
    foreach ($surface in $surfaces) {
        Remove-Item -LiteralPath (Join-Path $env:TEMP "SNAPVERE/$($surface.Stem).$sessionId.ready") -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath (Join-Path $env:TEMP "SNAPVERE/$($surface.Stem).$sessionId.captured") -Force -ErrorAction SilentlyContinue
    }
    $env:SNAPVERE_SECONDARY_UI_PROBE = '1'
    $env:SNAPVERE_PROBE_SESSION_ID = $sessionId
    $process = $null

    try {
        $process = Start-Process -FilePath $FilePath -ArgumentList @('--secondary-ui-probe') -PassThru
        $escapedVersion = [Regex]::Escape($Version)

        foreach ($surface in $surfaces) {
            $readyFileName = "$($surface.Stem).$sessionId.ready"
            $readyPath = Join-Path $env:TEMP "SNAPVERE/$readyFileName"
            $deadline = (Get-Date).AddSeconds(15)
            while (-not (Test-Path -LiteralPath $readyPath -PathType Leaf) -and (Get-Date) -lt $deadline) {
                if ($process.HasExited) {
                    Write-StartupLogIfPresent
                    throw "$Name secondary-UI probe exited before $readyFileName was created (code $($process.ExitCode))."
                }
                Start-Sleep -Milliseconds 20
            }

            if (-not (Test-Path -LiteralPath $readyPath -PathType Leaf)) {
                Write-StartupLogIfPresent
                throw "$Name secondary-UI probe did not publish $readyFileName within 15 seconds."
            }

            $readyText = Get-Content -LiteralPath $readyPath -Raw
            if ($readyText -notmatch "SNAPVERE $escapedVersion $([Regex]::Escape($surface.State))") {
                throw "$Name secondary-UI marker is invalid: $readyText"
            }

            $acknowledgementPath = Join-Path $env:TEMP "SNAPVERE/$($surface.Stem).$sessionId.captured"
            Set-Content -LiteralPath $acknowledgementPath -Value "LIFECYCLE_ACK session=$sessionId" -Encoding utf8
        }

        if (-not $process.WaitForExit(10000)) {
            Write-StartupLogIfPresent
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            throw "$Name secondary-UI probe did not exit after all four acknowledgements."
        }

        if ($process.ExitCode -ne 0) {
            Write-StartupLogIfPresent
            throw "$Name secondary-UI probe failed with exit code $($process.ExitCode)."
        }

        if (-not (Test-Path -LiteralPath $marker)) {
            Write-StartupLogIfPresent
            throw "$Name exited successfully but did not complete all secondary UI surfaces."
        }

        $markerText = Get-Content -LiteralPath $marker -Raw
        if ($markerText -notmatch "SNAPVERE $escapedVersion SECONDARY_UI_READY") {
            throw "$Name secondary-UI marker is invalid: $markerText"
        }

        Write-Host "$Name secondary-UI marker: $markerText"
    }
    finally {
        Remove-Item Env:SNAPVERE_SECONDARY_UI_PROBE -ErrorAction SilentlyContinue
        Remove-Item Env:SNAPVERE_PROBE_SESSION_ID -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $marker -Force -ErrorAction SilentlyContinue
        foreach ($surface in $surfaces) {
            Remove-Item -LiteralPath (Join-Path $env:TEMP "SNAPVERE/$($surface.Stem).$sessionId.ready") -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath (Join-Path $env:TEMP "SNAPVERE/$($surface.Stem).$sessionId.captured") -Force -ErrorAction SilentlyContinue
        }
        if ($null -ne $process) { $process.Dispose() }
    }
}

function Clear-ProbeEnvironment {
    Remove-Item Env:SNAPVERE_STARTUP_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_TRAY_STARTUP_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_REGION_OVERLAY_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_WINDOW_OVERLAY_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_SECONDARY_UI_PROBE -ErrorAction SilentlyContinue
    Remove-Item Env:SNAPVERE_PROBE_SESSION_ID -ErrorAction SilentlyContinue
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

# This contract deliberately checks the real Setup defaults before any test code
# touches Windows startup registration: Desktop shortcut and Start with Windows
# must both have been created by Setup itself.
& $contractScript -State Installed -InstallDirectory $install -ExpectedVersion $Version

$installedApp = Join-Path $install 'Snapvere.exe'
$installedSetup = Join-Path $install 'SNAPVERE-Setup.exe'
Invoke-StartupProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-TrayStartupProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-RegionOverlayProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-WindowOverlayProbe $installedApp "Installed SNAPVERE $Arch"
Invoke-SecondaryUiProbe $installedApp "Installed SNAPVERE $Arch"
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

# The first Portable probe includes cold embedded-payload extraction before the
# child WinUI probe can start. Keep installed probes strict at 20 seconds, but
# allow a realistic cold-start budget for the Portable wrapper on hosted CI.
Invoke-StartupProbe $portable "Portable SNAPVERE $Arch" 60000
Invoke-TrayStartupProbe $portable "Portable SNAPVERE $Arch"
Invoke-RegionOverlayProbe $portable "Portable SNAPVERE $Arch"
Invoke-WindowOverlayProbe $portable "Portable SNAPVERE $Arch"
Invoke-SecondaryUiProbe $portable "Portable SNAPVERE $Arch"
Invoke-NormalPortableLaunch $portable "Portable SNAPVERE $Arch"

Clear-ProbeEnvironment
Write-Host "SNAPVERE $Version $Arch package lifecycle passed."
