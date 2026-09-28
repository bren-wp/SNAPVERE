param(
    [Parameter(Mandatory = $true)]
    [string] $MsiPath,

    [Parameter(Mandatory = $true)]
    [string] $Version,

    [string] $PreviousMsiPath,

    [string] $PreviousVersion,

    [string] $LogDirectory = (Join-Path $env:TEMP 'SNAPVERE-msi-qa')
)

$ErrorActionPreference = 'Stop'
$msi = (Resolve-Path -LiteralPath $MsiPath).Path
$previousMsi = if ([string]::IsNullOrWhiteSpace($PreviousMsiPath)) { $null } else { (Resolve-Path -LiteralPath $PreviousMsiPath).Path }
$msiexec = Join-Path $env:SystemRoot 'System32\msiexec.exe'
$installRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)) 'Brendigo\SNAPVERE'
$appPath = Join-Path $installRoot 'Snapvere.exe'
$shortcutPath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::CommonPrograms)) 'SNAPVERE\SNAPVERE.lnk'
$uninstallRoot = 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall'

New-Item -ItemType Directory -Force -Path $LogDirectory | Out-Null

function Invoke-Msi(
    [Parameter(Mandatory = $true)][string[]] $Arguments,
    [Parameter(Mandatory = $true)][string] $Name
) {
    $process = Start-Process -FilePath $msiexec -ArgumentList $Arguments -Wait -PassThru
    try {
        if ($process.ExitCode -notin @(0, 3010)) {
            throw "$Name failed with Windows Installer exit code $($process.ExitCode)."
        }
        if ($process.ExitCode -eq 3010) {
            Write-Host "$Name succeeded and requested a restart (3010); /norestart kept the runner active."
        }
    }
    finally {
        $process.Dispose()
    }
}

function Get-SnapvereArpEntries {
    if (-not (Test-Path -LiteralPath $uninstallRoot)) { return @() }
    return @(
        Get-ChildItem -LiteralPath $uninstallRoot |
            ForEach-Object {
                try { Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction Stop } catch { $null }
            } |
            Where-Object { $_.DisplayName -eq 'SNAPVERE' }
    )
}

function Assert-Installed([string] $ExpectedVersion) {
    if (-not (Test-Path -LiteralPath $appPath -PathType Leaf)) {
        throw "Installed application is missing: $appPath"
    }

    $fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($appPath).FileVersion
    if (-not $fileVersion.StartsWith("$Version.", [StringComparison]::Ordinal) -and $ExpectedVersion -eq $Version) {
        throw "Installed Snapvere.exe file version '$fileVersion' does not match release $Version."
    }

    if (-not (Test-Path -LiteralPath $shortcutPath -PathType Leaf)) {
        throw "Start Menu shortcut is missing: $shortcutPath"
    }

    $entries = @(Get-SnapvereArpEntries)
    if ($entries.Count -ne 1) {
        throw "Expected exactly one SNAPVERE Installed Apps entry, found $($entries.Count)."
    }
    if ([string]$entries[0].DisplayVersion -ne $ExpectedVersion) {
        throw "Installed Apps version mismatch. Expected $ExpectedVersion, got $($entries[0].DisplayVersion)."
    }
}

function Assert-Uninstalled {
    if (Test-Path -LiteralPath $appPath -PathType Leaf) {
        throw "Snapvere.exe remains after MSI uninstall: $appPath"
    }
    if (Test-Path -LiteralPath $shortcutPath -PathType Leaf) {
        throw "Start Menu shortcut remains after MSI uninstall: $shortcutPath"
    }
    if (@(Get-SnapvereArpEntries).Count -ne 0) {
        throw 'SNAPVERE Installed Apps registration remains after MSI uninstall.'
    }
}

function Install-Package([string] $Path, [string] $LogName) {
    Invoke-Msi @('/i', $Path, '/qn', '/norestart', '/L*v', (Join-Path $LogDirectory $LogName)) "MSI install $LogName"
}

function Uninstall-Package([string] $Path, [string] $LogName) {
    Invoke-Msi @('/x', $Path, '/qn', '/norestart', '/L*v', (Join-Path $LogDirectory $LogName)) "MSI uninstall $LogName"
}

try {
    Write-Host 'MSI clean-install scenario'
    Install-Package $msi 'clean-install.log'
    Assert-Installed $Version

    Write-Host 'MSI repair scenario'
    Invoke-Msi @('/fa', $msi, '/qn', '/norestart', '/L*v', (Join-Path $LogDirectory 'repair.log')) 'MSI repair'
    Assert-Installed $Version

    Write-Host 'MSI silent-uninstall scenario'
    Uninstall-Package $msi 'clean-uninstall.log'
    Assert-Uninstalled

    if ($null -ne $previousMsi) {
        if ([string]::IsNullOrWhiteSpace($PreviousVersion)) {
            throw 'PreviousVersion is required when PreviousMsiPath is supplied.'
        }

        Write-Host "MSI major-upgrade scenario: $PreviousVersion -> $Version"
        Install-Package $previousMsi 'upgrade-baseline-install.log'
        Assert-Installed $PreviousVersion

        Install-Package $msi 'upgrade-current-install.log'
        Assert-Installed $Version

        Uninstall-Package $msi 'upgrade-uninstall.log'
        Assert-Uninstalled
    }

    Write-Host 'SNAPVERE MSI lifecycle QA completed successfully.'
}
finally {
    if (@(Get-SnapvereArpEntries).Count -gt 0) {
        try { Uninstall-Package $msi 'emergency-cleanup-current.log' } catch {}
        if ($null -ne $previousMsi -and @(Get-SnapvereArpEntries).Count -gt 0) {
            try { Uninstall-Package $previousMsi 'emergency-cleanup-previous.log' } catch {}
        }
    }
}
