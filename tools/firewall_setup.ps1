param(
    [string]$Ports = ""
)

function Test-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    $args = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Ports `"$Ports`""
    Start-Process -FilePath "powershell" -ArgumentList $args -Verb RunAs
    exit
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$marker = Join-Path $repoRoot "firewall.ok"

if ([string]::IsNullOrWhiteSpace($Ports)) {
    $Ports = "1935,8554,60800"
}

$portList = $Ports -split "," | ForEach-Object { $_.Trim() } | Where-Object { $_ -match "^\d+$" } | Select-Object -Unique

foreach ($p in $portList) {
    $nameIn = "MaritimeTargetMonitor-Port-$p-In"
    $nameOut = "MaritimeTargetMonitor-Port-$p-Out"
    netsh advfirewall firewall add rule name=$nameIn dir=in action=allow protocol=TCP localport=$p > $null 2>&1
    netsh advfirewall firewall add rule name=$nameOut dir=out action=allow protocol=TCP localport=$p > $null 2>&1
}

"ok" | Set-Content -Path $marker -Encoding ASCII
Write-Host "Firewall rules configured for ports: $Ports"
