param(
    [Parameter(Mandatory = $true)]
    [string] $ArtifactsRoot
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ArtifactsRoot).Path

$budgets = @(
    [pscustomobject]@{ Name = 'x86 payload'; RelativePath = 'SNAPVERE-payload-x86.zip'; MaxMb = 85.0 },
    [pscustomobject]@{ Name = 'x64 payload'; RelativePath = 'SNAPVERE-payload-x64.zip'; MaxMb = 115.0 },
    [pscustomobject]@{ Name = 'ARM64 payload'; RelativePath = 'SNAPVERE-payload-arm64.zip'; MaxMb = 110.0 },
    [pscustomobject]@{ Name = 'Portable'; RelativePath = 'universal/SNAPVERE-Portable.exe'; MaxMb = 350.0 },
    [pscustomobject]@{ Name = 'Setup'; RelativePath = 'universal/SNAPVERE-Setup.exe'; MaxMb = 350.0 }
)

$results = foreach ($budget in $budgets) {
    $path = Join-Path $root $budget.RelativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Package-size input is missing: $($budget.RelativePath)"
    }

    $file = Get-Item -LiteralPath $path
    $sizeMb = [Math]::Round($file.Length / 1MB, 2)
    $headroomMb = [Math]::Round($budget.MaxMb - $sizeMb, 2)
    if ($file.Length -gt ($budget.MaxMb * 1MB)) {
        throw "$($budget.Name) is $sizeMb MB, above the $($budget.MaxMb) MB regression budget."
    }

    Write-Host "$($budget.Name): $sizeMb MB / $($budget.MaxMb) MB budget ($headroomMb MB headroom)"
    [pscustomobject]@{
        Name = $budget.Name
        Path = $budget.RelativePath.Replace('\\', '/')
        SizeBytes = $file.Length
        SizeMb = $sizeMb
        MaxMb = $budget.MaxMb
        HeadroomMb = $headroomMb
    }
}

$reportPath = Join-Path $root 'package-size.json'
$results | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $reportPath -Encoding utf8NoBOM
Write-Host "Package-size regression report: $reportPath"
