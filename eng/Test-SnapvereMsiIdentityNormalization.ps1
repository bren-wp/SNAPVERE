$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'MsiIdentityNormalization.ps1')

$expected = '{A680451A-7E71-4E13-9814-8517D3BC6A89}'
$cases = @(
    @{ Name = 'plain'; Value = $expected },
    @{ Name = 'ASCII whitespace'; Value = "  `t$expected`r`n" },
    @{ Name = 'BOM boundary'; Value = ([string][char]0xFEFF) + $expected + ([string][char]0xFEFF) },
    @{ Name = 'zero-width boundary'; Value = ([string][char]0x200B) + $expected + ([string][char]0x200B) },
    @{ Name = 'directional boundary'; Value = ([string][char]0x200E) + $expected + ([string][char]0x200F) },
    @{ Name = 'lowercase canonical'; Value = ' {a680451a-7e71-4e13-9814-8517d3bc6a89} ' }
)

foreach ($case in $cases) {
    $actual = ConvertTo-SnapvereCanonicalMsiGuid -Value $case.Value -Name "UpgradeCode/$($case.Name)"
    if ($actual -cne $expected) {
        throw "GUID normalization regression for '$($case.Name)'. Expected '$expected', got '$actual'."
    }
}

$invalidCases = @(
    'A680451A-7E71-4E13-9814-8517D3BC6A89',
    '{A680451A7E714E1398148517D3BC6A89}',
    'prefix{A680451A-7E71-4E13-9814-8517D3BC6A89}',
    '{NOT-A-GUID}'
)

foreach ($invalid in $invalidCases) {
    $threw = $false
    try {
        [void](ConvertTo-SnapvereCanonicalMsiGuid -Value $invalid -Name 'UpgradeCode/invalid-regression')
    }
    catch {
        $threw = $true
    }

    if (-not $threw) {
        throw "Invalid GUID regression case was accepted: '$invalid'."
    }
}

$templateRaw = ([string][char]0xFEFF) + " `tArm64;1033`r`n" + ([string][char]0x200B)
$template = Normalize-SnapvereMsiText -Value $templateRaw
if ($template -cne 'Arm64;1033') {
    throw "MSI text boundary normalization regression. Got '$template'."
}

Write-Host 'Validated MSI identity normalization regression cases.'
