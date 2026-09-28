param(
    [Parameter(Mandatory = $true)]
    [string] $CurrentMsiPath,

    [Parameter(Mandatory = $true)]
    [string] $CurrentVersion,

    [Parameter(Mandatory = $true)]
    [string] $PreviousMsiPath,

    [Parameter(Mandatory = $true)]
    [string] $PreviousVersion
)

$ErrorActionPreference = 'Stop'
$current = (Resolve-Path -LiteralPath $CurrentMsiPath).Path
$previous = (Resolve-Path -LiteralPath $PreviousMsiPath).Path
$installer = New-Object -ComObject WindowsInstaller.Installer

function Read-MsiIdentity([string] $path) {
    $database = $null
    try {
        $database = $installer.OpenDatabase($path, 0)

        function Get-Property([string] $name) {
            $view = $null
            $record = $null
            try {
                $safe = $name.Replace("'", "''")
                $view = $database.OpenView("SELECT `Value` FROM `Property` WHERE `Property` = '$safe'")
                $view.Execute()
                $record = $view.Fetch()
                if ($null -eq $record) { return $null }
                return ([string]$record.StringData(1)).Trim()
            }
            finally {
                if ($null -ne $record) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($record) }
                if ($null -ne $view) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($view) }
            }
        }

        $summary = $database.SummaryInformation(0)
        try {
            return [pscustomobject]@{
                ProductVersion = Get-Property 'ProductVersion'
                ProductCode = Get-Property 'ProductCode'
                UpgradeCode = Get-Property 'UpgradeCode'
                Template = ([string]$summary.Property(7)).Trim()
                PackageCode = ([string]$summary.Property(9)).Trim()
            }
        }
        finally {
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($summary)
        }
    }
    finally {
        if ($null -ne $database) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($database) }
    }
}

try {
    $currentIdentity = Read-MsiIdentity $current
    $previousIdentity = Read-MsiIdentity $previous

    if ($currentIdentity.ProductVersion -ne $CurrentVersion) {
        throw "Current MSI ProductVersion mismatch. Expected $CurrentVersion, got $($currentIdentity.ProductVersion)."
    }
    if ($previousIdentity.ProductVersion -ne $PreviousVersion) {
        throw "Previous MSI ProductVersion mismatch. Expected $PreviousVersion, got $($previousIdentity.ProductVersion)."
    }
    if ($currentIdentity.ProductCode -ieq $previousIdentity.ProductCode) {
        throw "Major-upgrade pair reused ProductCode $($currentIdentity.ProductCode). Each ProductVersion must have a distinct ProductCode."
    }
    if ($currentIdentity.PackageCode -ieq $previousIdentity.PackageCode) {
        throw "Major-upgrade pair reused PackageCode $($currentIdentity.PackageCode). Each built MSI must have a distinct package identity."
    }
    if ($currentIdentity.UpgradeCode -ine $previousIdentity.UpgradeCode) {
        throw "Major-upgrade pair UpgradeCode mismatch. Current=$($currentIdentity.UpgradeCode), previous=$($previousIdentity.UpgradeCode)."
    }
    if ($currentIdentity.Template -ne $previousIdentity.Template) {
        throw "Major-upgrade pair architecture/template mismatch. Current='$($currentIdentity.Template)', previous='$($previousIdentity.Template)'."
    }

    Write-Host "Validated MSI upgrade pair: $PreviousVersion [$($previousIdentity.ProductCode)] -> $CurrentVersion [$($currentIdentity.ProductCode)], UpgradeCode $($currentIdentity.UpgradeCode), template $($currentIdentity.Template)."
}
finally {
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
}
