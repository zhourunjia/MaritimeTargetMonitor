param(
    [string]$Path
)

if (-not (Test-Path $Path)) {
    Write-Host "config not found: $Path"
    exit 1
}

$json = Get-Content -Raw -Path $Path | ConvertFrom-Json
$json.AlgorithmAutoStart = $true
$json.AlgorithmUseCpu = $true
$json.AlgorithmUseTrt = $false
$json | ConvertTo-Json -Depth 10 | Set-Content -Path $Path -Encoding UTF8
