param()

function Test-VCRedist {
    $paths = @(
        "HKLM:\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64"
    )
    foreach ($p in $paths) {
        try {
            $v = Get-ItemProperty -Path $p -ErrorAction Stop
            if ($v.Installed -eq 1) { return $true }
        } catch { }
    }
    return $false
}

function Ensure-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$marker = Join-Path $repoRoot "vcredist.ok"

if (Test-Path $marker) { exit }

if (Test-VCRedist) {
    "ok" | Set-Content -Path $marker -Encoding ASCII
    exit
}

$installer = Join-Path $PSScriptRoot "vc_redist.x64.exe"
if (-not (Test-Path $installer)) {
    Write-Host "vc_redist.x64.exe not found: $installer"
    exit 1
}

if (-not (Ensure-Admin)) {
    $args = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    Start-Process -FilePath "powershell" -ArgumentList $args -Verb RunAs
    exit
}

Start-Process -FilePath $installer -ArgumentList "/install /quiet /norestart" -Wait
Start-Sleep -Seconds 2

if (Test-VCRedist) {
    "ok" | Set-Content -Path $marker -Encoding ASCII
    exit 0
}

Write-Host "VCRedist install failed or not detected."
exit 2
