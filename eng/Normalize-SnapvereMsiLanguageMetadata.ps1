param(
    [Parameter(Mandatory = $true)]
    [string] $MsiPath,

    [Parameter(Mandatory = $true)]
    [string] $WixExePath,

    [string] $ReportPath
)

$ErrorActionPreference = 'Stop'
$msi = (Resolve-Path -LiteralPath $MsiPath).Path
$wix = (Resolve-Path -LiteralPath $WixExePath).Path

function Invoke-WixMsiValidation {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    & $wix msi validate $Path 2>&1 | ForEach-Object {
        $lines.Add($_.ToString())
    }
    $exitCode = $LASTEXITCODE

    return [pscustomobject]@{
        ExitCode = $exitCode
        Lines = @($lines)
    }
}

function Get-FileLanguageRow(
    [Parameter(Mandatory = $true)]
    $Database,

    [Parameter(Mandatory = $true)]
    [string] $FileKey
) {
    $safeKey = $FileKey.Replace("'", "''")
    $view = $null
    $record = $null
    try {
        $view = $Database.OpenView("SELECT `FileName`, `Language` FROM `File` WHERE `File` = '$safeKey'")
        $view.Execute()
        $record = $view.Fetch()
        if ($null -eq $record) {
            return $null
        }

        return [pscustomobject]@{
            File = $FileKey
            FileName = [string]$record.StringData(1)
            Language = [string]$record.StringData(2)
        }
    }
    finally {
        if ($null -ne $record) {
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($record)
        }
        if ($null -ne $view) {
            [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($view)
        }
    }
}

function Write-ValidationFailure {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Validation
    )

    foreach ($line in $Validation.Lines) {
        Write-Host $line
    }
}

$initial = Invoke-WixMsiValidation -Path $msi
$languageErrorPattern = 'ICE03:\s+(?:Invalid Language Id|String overflow .*?);\s*Table:\s*File,\s*Column:\s*Language,\s*Key\(s\):\s*(?<key>\S+)\s*$'
$errorPattern = '\berror\s+WIX\d+:'

$targetKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$unexpectedErrors = [System.Collections.Generic.List[string]]::new()

foreach ($line in $initial.Lines) {
    if ($line -notmatch $errorPattern) {
        continue
    }

    $match = [Regex]::Match($line, $languageErrorPattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success) {
        [void]$targetKeys.Add($match.Groups['key'].Value)
        continue
    }

    $unexpectedErrors.Add($line)
}

if ($initial.ExitCode -eq 0) {
    if ($targetKeys.Count -ne 0) {
        throw 'WiX validation returned success while reporting File.Language ICE03 errors.'
    }
}
elseif ($unexpectedErrors.Count -gt 0 -or $targetKeys.Count -eq 0) {
    Write-ValidationFailure -Validation $initial
    if ($unexpectedErrors.Count -gt 0) {
        throw "MSI validation failed with $($unexpectedErrors.Count) non-language ICE error(s); refusing metadata normalization."
    }
    throw 'MSI validation failed without a recognized File.Language ICE03 error; refusing metadata normalization.'
}

$report = [System.Collections.Generic.List[object]]::new()
$installer = $null
$database = $null

try {
    if ($targetKeys.Count -gt 0) {
        $installer = New-Object -ComObject WindowsInstaller.Installer

        # Transacted mode changes only the File.Language cells explicitly
        # identified by Windows Installer ICE03. File hashes, versions,
        # cabinet data, component authoring and all other tables remain intact.
        $database = $installer.OpenDatabase($msi, 1)

        foreach ($key in ($targetKeys | Sort-Object)) {
            if ([string]::IsNullOrWhiteSpace($key)) {
                throw 'WiX ICE03 returned an empty File-table key; refusing metadata normalization.'
            }

            $row = Get-FileLanguageRow -Database $database -FileKey $key
            if ($null -eq $row) {
                throw "WiX ICE03 reported File key '$key' but the File table row could not be resolved."
            }

            $safeId = $key.Replace("'", "''")
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
                Reason = 'Reported by WiX/Windows Installer ICE03 as invalid File.Language metadata'
            })
        }

        $database.Commit()
    }
}
finally {
    if ($null -ne $database) {
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($database)
    }
    if ($null -ne $installer) {
        [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($installer)
    }
}

$final = Invoke-WixMsiValidation -Path $msi
if ($final.ExitCode -ne 0) {
    Write-ValidationFailure -Validation $final
    throw 'MSI still fails full WiX/Windows Installer validation after targeted File.Language normalization.'
}

if (-not [string]::IsNullOrWhiteSpace($ReportPath)) {
    $reportDirectory = Split-Path -Parent $ReportPath
    if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
        New-Item -ItemType Directory -Force -Path $reportDirectory | Out-Null
    }

    [pscustomobject]@{
        FormatVersion = 2
        Msi = [IO.Path]::GetFileName($msi)
        Validator = 'WiX 5.0.2 / Windows Installer ICE03'
        InitialValidationExitCode = $initial.ExitCode
        FinalValidationExitCode = $final.ExitCode
        NormalizedCount = $report.Count
        Files = @($report)
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8NoBOM
}

if ($report.Count -eq 0) {
    Write-Host 'MSI passed full WiX/Windows Installer validation without File.Language normalization.'
}
else {
    Write-Host "Normalized $($report.Count) File.Language value(s) reported invalid by ICE03; full validation now passes."
    foreach ($entry in $report) {
        Write-Host "  $($entry.FileName): '$($entry.OriginalLanguage)' -> 0"
    }
}
