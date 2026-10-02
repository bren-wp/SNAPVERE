function Normalize-SnapvereMsiText {
    param(
        [AllowNull()]
        [string] $Value
    )

    if ($null -eq $Value) {
        return $null
    }

    # Windows Installer COM can surface boundary formatting characters that are
    # visually indistinguishable from whitespace. Strip only boundary Unicode
    # whitespace/format controls; embedded characters remain significant.
    return [regex]::Replace([string]$Value, '^[\s\p{Cf}]+|[\s\p{Cf}]+$', '')
}

function ConvertTo-SnapvereCanonicalMsiGuid {
    param(
        [AllowNull()]
        [string] $Value,

        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    $candidate = Normalize-SnapvereMsiText -Value $Value
    if ([string]::IsNullOrEmpty($candidate)) {
        throw "MSI $Name is missing."
    }

    $parsed = [Guid]::Empty
    if (-not [Guid]::TryParseExact($candidate, 'B', [ref]$parsed)) {
        throw "MSI $Name is not a canonical braced GUID: '$candidate'."
    }

    return $parsed.ToString('B').ToUpperInvariant()
}
