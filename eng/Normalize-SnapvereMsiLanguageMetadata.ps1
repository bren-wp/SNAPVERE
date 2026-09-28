param(
    [Parameter(Mandatory = $true)]
    [string] $MsiPath,

    [string] $ReportPath
)

$ErrorActionPreference = 'Stop'
$msi = (Resolve-Path -LiteralPath $MsiPath).Path


$script:LocaleSupported = [uint32]0x00000002
$script:LocaleValidatorType = Add-Type -TypeDefinition @"
using System.Runtime.InteropServices;

public static class SnapvereLocaleValidation
{
    [DllImport("kernel32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsValidLocale(uint locale, uint flags);
}
"@ -PassThru

$script:IsValidLocaleMethod = $script:LocaleValidatorType.GetMethod(
    "IsValidLocale",
    [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::Static)

if ($null -eq $script:IsValidLocaleMethod) {
    throw 'Could not resolve kernel32 IsValidLocale validator.'
}

$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $null

function Test-MsiLanguage([string] $value) {
    if ([string]::IsNullOrWhiteSpace($value)) {
        return $true
    }

    if ($value.Length -gt 20 -or $value -notmatch '^\d{1,5}(,\d{1,5})*$') {
        return $false
    }

    foreach ($token in $value.Split(',')) {
        $languageId = 0
        if (-not [int]::TryParse($token, [ref]$languageId)) {
            return $false
        }
        if ($languageId -eq 0) {
            continue
        }
        if ($languageId -lt 0 -or $languageId -gt 0xFFFF) {
            return $false
        }
        $arguments = [object[]]@([uint32]$languageId, $script:LocaleSupported)
        $isValid = [bool]$script:IsValidLocaleMethod.Invoke($null, $arguments)
        if (-not $isValid) {
            return $false
        }
    }

    return $true
}

function Get-FileLanguageRows($db) {
    $view = $null
    try {
        $view = $db.OpenView('SELECT `File`, `FileName`, `Language` FROM `File`')
        $view.Execute()
        $rows = [System.Collections.Generic.List[object]]::new()
        while ($true) {
            $record = $view.Fetch()
            if ($null -eq $record) {
                break
            }
            try {
                $rows.Add([pscustomobject]@{
                    File = [string]$record.StringData(1)
                    FileName = [string]$record.StringData(2)
                    Language = [string]$record.StringData(3)
                })
            }
            finally {
                [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($record)
            }
        }
        return $rows
    }
    finally {
        if ($null -ne $view) {
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($view)
        }
    }
}

try {
    # Transacted mode lets us correct only File.Language metadata before the
    # final full ICE pass. File hashes, versions, cabinet data and component
    # authoring remain untouched.
    $database = $installer.OpenDatabase($msi, 1)
    $rows = @(Get-FileLanguageRows $database)
    $invalid = @($rows | Where-Object { -not (Test-MsiLanguage $_.Language) })

    $report = [System.Collections.Generic.List[object]]::new()
    foreach ($row in $invalid) {
        $safeId = $row.File.Replace("'", "''")
        $view = $null
        try {
            $view = $database.OpenView("UPDATE `File` SET `Language` = '0' WHERE `File` = '$safeId'")
            $view.Execute()
        }
        finally {
            if ($null -ne $view) {
                [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($view)
            }
        }

        $report.Add([pscustomobject]@{
            File = $row.File
            FileName = $row.FileName
            OriginalLanguage = $row.Language
            NormalizedLanguage = '0'
        })
    }

    if ($invalid.Count -gt 0) {
        $database.Commit()
    }

    $remaining = @((Get-FileLanguageRows $database) | Where-Object { -not (Test-MsiLanguage $_.Language) })
    if ($remaining.Count -ne 0) {
        throw "MSI still contains $($remaining.Count) invalid File.Language value(s) after normalization."
    }

    if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
        $reportDirectory = Split-Path -Parent $ReportPath
        if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
            New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
        }
        [pscustomobject]@{
            FormatVersion = 1
            Msi = [IO.Path]::GetFileName($msi)
            NormalizedCount = $report.Count
            Files = @($report)
        } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $ReportPath -Encoding utf8NoBOM
    }

    if ($report.Count -eq 0) {
        Write-Host 'MSI File.Language metadata was already valid; no normalization was required.'
    }
    else {
        Write-Host "Normalized $($report.Count) invalid File.Language value(s) to language-neutral 0 before final ICE validation."
        foreach ($entry in $report) {
            Write-Host "  $($entry.FileName): '$($entry.OriginalLanguage)' -> 0"
        }
    }
}
finally {
    if ($null -ne $database) {
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($database)
    }
    [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
}
