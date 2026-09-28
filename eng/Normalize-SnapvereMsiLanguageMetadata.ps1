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
$diagnosticPattern = '\b(?:error|warning)\s+WIX\d+:'

$targetKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$unexpectedDiagnostics = [System.Collections.Generic.List[string]]::new()

foreach ($line in $initial.Lines) {
    if ($line -notmatch $diagnosticPattern) {
        continue
    }

    $match = [Regex]::Match($line, $languageErrorPattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success) {
        [void]$targetKeys.Add($match.Groups['key'].Value)
        continue
    }

    $unexpectedDiagnostics.Add($line)
}

if ($unexpectedDiagnostics.Count -gt 0) {
    Write-ValidationFailure -Validation $initial
    throw "MSI validation reported $($unexpectedDiagnostics.Count) non-language ICE diagnostic(s); refusing metadata normalization."
}
if ($initial.ExitCode -ne 0 -and $targetKeys.Count -eq 0) {
    Write-ValidationFailure -Validation $initial
    throw 'MSI validation failed without a recognized File.Language ICE03 diagnostic; refusing metadata normalization.'
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
$finalDiagnostics = @($final.Lines | Where-Object { $_ -match $diagnosticPattern })
if ($final.ExitCode -ne 0 -or $finalDiagnostics.Count -ne 0) {
    Write-ValidationFailure -Validation $final
    throw "MSI final WiX/Windows Installer validation is not clean after targeted File.Language normalization (exit=$($final.ExitCode), diagnostics=$($finalDiagnostics.Count))."
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
        InitialDiagnosticCount = @($initial.Lines | Where-Object { $_ -match $diagnosticPattern }).Count
        FinalValidationExitCode = $final.ExitCode
        FinalDiagnosticCount = $finalDiagnostics.Count
        NormalizedCount = $report.Count
        Files = @($report)
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8NoBOM
}

if ($report.Count -eq 0) {
    Write-Host 'MSI passed full WiX/Windows Installer validation with zero diagnostics and without File.Language normalization.'
}
else {
    Write-Host "Normalized $($report.Count) File.Language value(s) reported by ICE03; full validation now passes with zero diagnostics."
    foreach ($entry in $report) {
        Write-Host "  $($entry.FileName): '$($entry.OriginalLanguage)' -> 0"
    }
}
