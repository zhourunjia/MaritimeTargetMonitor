param(
    [string]$Root = "",
    [switch]$SkipPython
)

$ErrorActionPreference = "Stop"

function Fail($msg) {
    Write-Host "[FAIL] $msg" -ForegroundColor Red
    exit 1
}

function Ok($msg) {
    Write-Host "[OK] $msg" -ForegroundColor Green
}

function Check-Path($path, $label) {
    if (-not (Test-Path -LiteralPath $path)) {
        Fail "$label not found: $path"
    }
    Ok "$label found"
}

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = "C:\Users\15666\Desktop\MaritimeTargetMonitor\dist\MaritimeTargetMonitor_Offline"
}

if (-not (Test-Path -LiteralPath $Root)) {
    Fail "Root not found: $Root"
}

Write-Host "== Offline smoke test =="
Write-Host "Root: $Root"

# Core executables
Check-Path (Join-Path $Root "Maritime.App.exe") "App exe"
Check-Path (Join-Path $Root "config.json") "Config"
Check-Path (Join-Path $Root "mediamtx.exe") "MediaMTX"
Check-Path (Join-Path $Root "ffmpeg-full\bin\ffmpeg.exe") "FFmpeg"

# VLC structure
Check-Path (Join-Path $Root "libvlc\win-x64\libvlc.dll") "libvlc.dll"
Check-Path (Join-Path $Root "libvlc\win-x64\plugins") "libvlc plugins"

# Algorithm files
Check-Path (Join-Path $Root "edgeyolo-main\edgeyolo-main\stream_infer_local.py") "Algorithm script"
Check-Path (Join-Path $Root "edgeyolo-main\edgeyolo-main\output\train\edgeyolo\best.pth") "Weights"

# Runtime python
Check-Path (Join-Path $Root "runtime\python\python.exe") "Python runtime"
Check-Path (Join-Path $Root "runtime\python\python311._pth") "Python _pth"
Check-Path (Join-Path $Root "tools\vc_redist.x64.exe") "VC++ Redistributable"

# Ensure _pth has no BOM
$pthBytes = Get-Content -Path (Join-Path $Root "runtime\python\python311._pth") -Encoding Byte -TotalCount 3
if ($pthBytes.Count -ge 3 -and $pthBytes[0] -eq 239 -and $pthBytes[1] -eq 187 -and $pthBytes[2] -eq 191) {
    Fail "python311._pth has UTF-8 BOM; embeddable Python may fail"
}
Ok "python311._pth has no BOM"

# Config sanity
try {
    $cfg = Get-Content -Raw -Path (Join-Path $Root "config.json") | ConvertFrom-Json
    if (-not $cfg.AlgorithmAutoStart) { Fail "AlgorithmAutoStart is not true in config.json" }
    if (-not $cfg.AlgorithmUseCpu) { Fail "AlgorithmUseCpu is not true in config.json" }
    Ok "Config defaults ok"
} catch {
    Fail "Failed to parse config.json: $($_.Exception.Message)"
}

if (-not $SkipPython) {
    $py = Join-Path $Root "runtime\python\python.exe"
    Write-Host "== Python import test =="
    $cmd = "import torch, torchvision, cv2, numpy, yaml, loguru, tqdm, tabulate; print('python_ok')"
    & $py -c $cmd
    if ($LASTEXITCODE -ne 0) {
        Fail "Python import test failed (exit $LASTEXITCODE)"
    }
    Ok "Python import test ok"
}

Write-Host "All checks passed."
