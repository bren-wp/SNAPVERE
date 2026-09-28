param(
    [Parameter(Mandatory = $true)]
    [string] $MsiPath,

    [Parameter(Mandatory = $true)]
    [string] $Version,

    [ValidateSet('x64', 'arm64', 'x86')]
    [string] $Architecture = 'x64'
)

$ErrorActionPreference = 'Stop'
$msi = (Resolve-Path -LiteralPath $MsiPath).Path
$file = Get-Item -LiteralPath $msi

if ($file.Extension -ine '.msi') {
    throw "Expected an .msi package, got $($file.Name)."
}
if ($file.Length -lt 1MB) {
    throw "MSI package is unexpectedly small: $($file.Length) bytes."
}

$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $null

function Get-MsiScalar([string] $query) {
    $view = $null
    $record = $null
    try {
        $view = $database.OpenView($query)
        $view.Execute()
        $record = $view.Fetch()
        if ($null -eq $record) { return $null }
        return [string]$record.StringData(1)
    }
    finally {
        if ($null -ne $record) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($record) }
        if ($null -ne $view) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($view) }
    }
}

function Get-MsiProperty([string] $name) {
    $safe = $name.Replace("'", "''")
    return Get-MsiScalar "SELECT `Value` FROM `Property` WHERE `Property` = '$safe'"
}

try {
    $database = $installer.OpenDatabase($msi, 0)

    $expected = @{
        ProductName = 'SNAPVERE'
        Manufacturer = 'Brendigo'
        ProductVersion = $Version
        UpgradeCode = '{A680451A-7E71-4E13-9814-8517D3BC6A89}'
        ALLUSERS = '1'
        ARPPRODUCTICON = 'Snapvere.exe'
    }

    foreach ($entry in $expected.GetEnumerator()) {
        $actual = Get-MsiProperty $entry.Key
        if ($actual -ne $entry.Value) {
            throw "MSI property $($entry.Key) mismatch. Expected '$($entry.Value)', got '$actual'."
        }
    }

    $productCode = Get-MsiProperty 'ProductCode'
    if ($productCode -notmatch '^\{[0-9A-Fa-f-]{36}\}$') {
        throw "MSI ProductCode is missing or invalid: '$productCode'."
    }
    if ($productCode -ieq $expected.UpgradeCode) {
        throw 'MSI ProductCode must not reuse the stable UpgradeCode.'
    }

    $fileView = $null
    $snapvereFileFound = $false
    try {
        $fileView = $database.OpenView("SELECT `FileName` FROM `File`")
        $fileView.Execute()
        while ($true) {
            $fileRecord = $fileView.Fetch()
            if ($null -eq $fileRecord) { break }
            try {
                $fileName = [string]$fileRecord.StringData(1)
                $longName = ($fileName -split '\\|')[-1]
                if ([string]::Equals($longName, 'Snapvere.exe', [StringComparison]::OrdinalIgnoreCase)) {
                    $snapvereFileFound = $true
                    break
                }
            }
            finally {
                [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($fileRecord)
            }
        }
    }
    finally {
        if ($null -ne $fileView) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($fileView) }
    }
    if (-not $snapvereFileFound) {
        throw 'MSI File table does not contain Snapvere.exe.'
    }

    $shortcutDirectory = Get-MsiScalar "SELECT `Directory_` FROM `Shortcut` WHERE `Shortcut` = 'StartMenuShortcut'"
    if ($shortcutDirectory -ne 'ApplicationProgramsFolder') {
        throw "MSI Start Menu shortcut directory is invalid: '$shortcutDirectory'."
    }

    $shortcutComponent = Get-MsiScalar "SELECT `Component_` FROM `Shortcut` WHERE `Shortcut` = 'StartMenuShortcut'"
    if ($shortcutComponent -ne 'MainExecutable') {
        throw "MSI Start Menu shortcut is not owned by MainExecutable: '$shortcutComponent'."
    }

    $shortcutTarget = Get-MsiScalar "SELECT `Target` FROM `Shortcut` WHERE `Shortcut` = 'StartMenuShortcut'"
    if ([string]::IsNullOrWhiteSpace($shortcutTarget) -or $shortcutTarget -eq '[INSTALLFOLDER]Snapvere.exe') {
        throw "MSI Start Menu shortcut is not authored as an advertised application shortcut: '$shortcutTarget'."
    }

    $upgradeVersionMax = Get-MsiScalar "SELECT `VersionMax` FROM `Upgrade` WHERE `UpgradeCode` = '$($expected.UpgradeCode)'"
    if ($null -eq $upgradeVersionMax) {
        throw 'MSI does not contain the expected major-upgrade relationship.'
    }

    $summary = $database.SummaryInformation(0)
    try {
        $template = [string]$summary.Property(7)
        $packageCode = [string]$summary.Property(9)

        $expectedTemplatePrefix = switch ($Architecture) {
            'x64' { 'x64' }
            'arm64' { 'Arm64' }
            default { 'Intel' }
        }
        if (-not $template.StartsWith($expectedTemplatePrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "MSI architecture template mismatch. Expected $expectedTemplatePrefix, got '$template'."
        }
        if ($packageCode -notmatch '^\{[0-9A-Fa-f-]{36}\}$') {
            throw "MSI PackageCode is missing or invalid: '$packageCode'."
        }
    }
    finally {
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($summary)
    }

    Write-Host "Validated $($file.Name): version $Version, architecture $Architecture, ProductCode $productCode, size $([Math]::Round($file.Length / 1MB, 1)) MB."
}
finally {
    if ($null -ne $database) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($database) }
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
}
