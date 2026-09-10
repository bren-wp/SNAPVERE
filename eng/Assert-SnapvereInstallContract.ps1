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
$startupKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$startupValueName = 'SNAPVERE'
$desktop = [Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory)
$desktopShortcut = Join-Path $desktop 'SNAPVERE.lnk'
$expectedStartup = '"' + $app + '"'

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

    if (-not (Test-Path -LiteralPath $desktopShortcut -PathType Leaf)) {
        throw "Default Desktop shortcut is missing: $desktopShortcut"
    }

    if (-not (Test-Path -LiteralPath $startupKey)) {
        throw 'Windows startup registry key is unavailable after install.'
    }

    $startup = Get-ItemProperty -LiteralPath $startupKey -Name $startupValueName -ErrorAction SilentlyContinue
    if ($null -eq $startup -or $startup.$startupValueName -ne $expectedStartup) {
        $actual = if ($null -eq $startup) { '<missing>' } else { [string]$startup.$startupValueName }
        throw "Default Start with Windows registration mismatch. Expected: $expectedStartup Actual: $actual"
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

    Write-Host 'SNAPVERE install contract verified: Setup, Desktop shortcut, startup registration and Installed apps metadata are present.'
    return
}

if (Test-Path -LiteralPath $app) {
    throw "Snapvere.exe remains after uninstall: $app"
}

if (Test-Path -LiteralPath $desktopShortcut) {
    throw "Desktop shortcut remains after uninstall: $desktopShortcut"
}

$startupAfterRemoval = Get-ItemProperty -LiteralPath $startupKey -Name $startupValueName -ErrorAction SilentlyContinue
if ($null -ne $startupAfterRemoval -and $null -ne $startupAfterRemoval.$startupValueName) {
    throw "SNAPVERE startup registration remains after uninstall: $($startupAfterRemoval.$startupValueName)"
}

if (Test-Path -LiteralPath $uninstallKey) {
    throw 'Windows Installed apps registration remains after uninstall.'
}

Write-Host 'SNAPVERE uninstall contract verified: app, Desktop shortcut, startup registration and Installed apps registration were removed.'
