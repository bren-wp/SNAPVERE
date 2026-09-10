param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Installed', 'Removed')]
    [string] $State,

    [Parameter(Mandatory = $true)]
    [string] $InstallDirectory,

    [Parameter(Mandatory = $true)]
    [string] $ExpectedVersion
)

$ErrorActionPreference = 'Stop'
$install = [System.IO.Path]::GetFullPath($InstallDirectory)
$setup = Join-Path $install 'SNAPVERE-Setup.exe'
$app = Join-Path $install 'Snapvere.exe'
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\SNAPVERE'

if ($State -eq 'Installed') {
    if (-not (Test-Path -LiteralPath $app -PathType Leaf)) {
        throw "Installed application is missing: $app"
    }

    if (-not (Test-Path -LiteralPath $setup -PathType Leaf)) {
        throw "Installed maintenance Setup is missing: $setup"
    }

    $forbidden = @(
        Get-ChildItem -LiteralPath $install -File -ErrorAction Stop |
            Where-Object { $_.Name -match '^(?i:uninstall|unins).*\.exe$' }
    )
    if ($forbidden.Count -ne 0) {
        throw "SNAPVERE must not install a separate uninstaller executable: $($forbidden.Name -join ', ')"
    }

    if (-not (Test-Path -LiteralPath $uninstallKey)) {
        throw 'Windows Installed apps registration is missing.'
    }

    $registration = Get-ItemProperty -LiteralPath $uninstallKey
    if ($registration.DisplayName -ne 'SNAPVERE') {
        throw "Unexpected uninstall DisplayName: $($registration.DisplayName)"
    }

    if ($registration.DisplayVersion -ne $ExpectedVersion) {
        throw "Unexpected uninstall DisplayVersion: $($registration.DisplayVersion); expected $ExpectedVersion"
    }

    if ([System.IO.Path]::GetFullPath([string]$registration.InstallLocation) -ne $install) {
        throw "Unexpected InstallLocation: $($registration.InstallLocation)"
    }

    $expectedUninstall = '"' + $setup + '" --uninstall'
    $expectedQuietUninstall = '"' + $setup + '" --uninstall --silent'
    if ($registration.UninstallString -ne $expectedUninstall) {
        throw "UninstallString must use the installed SNAPVERE-Setup.exe. Actual: $($registration.UninstallString)"
    }

    if ($registration.QuietUninstallString -ne $expectedQuietUninstall) {
        throw "QuietUninstallString must use the installed SNAPVERE-Setup.exe. Actual: $($registration.QuietUninstallString)"
    }

    Write-Host 'SNAPVERE install contract verified: one Setup executable handles install/update/remove.'
    return
}

if (Test-Path -LiteralPath $app) {
    throw "Snapvere.exe remains after uninstall: $app"
}

if (Test-Path -LiteralPath $uninstallKey) {
    throw 'Windows Installed apps registration remains after uninstall.'
}

Write-Host 'SNAPVERE uninstall contract verified: application and registration removed without a separate uninstaller.'
