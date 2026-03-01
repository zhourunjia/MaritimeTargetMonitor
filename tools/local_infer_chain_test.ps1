param(
    [string]$Python = "",
    [string]$Weights = "",
    [string]$InputFile = "",
    [string]$Ffmpeg = "",
    [string]$Mediamtx = "",
    [string]$MediamtxConfig = "",
    [int]$WarmupSec = 10,
    [int]$InferWarmupSec = 12,
    [int]$CapFps = 5,
    [string]$InputSize = "256,256",
    [string]$OutputSize = "960x540",
    [string]$SnapshotPath = "",
    [switch]$UseGpu
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-DefaultPath {
    param([string]$Candidate)
    if ([string]::IsNullOrWhiteSpace($Candidate)) { return $null }
    $full = [System.IO.Path]::GetFullPath($Candidate)
    if (Test-Path -LiteralPath $full) { return $full }
    return $null
}

function Write-Step {
    param([string]$Text)
    Write-Host ("`n== {0} ==" -f $Text)
}

$repoRoot = Split-Path -Parent $PSScriptRoot

$defaults = @{
    Python         = Join-Path $repoRoot "edgeyolo-main\\edgeyolo-main\\.venv\\Scripts\\python.exe"
    Weights        = Join-Path $repoRoot "edgeyolo-main\\edgeyolo-main\\output\\train\\edgeyolo\\best.pth"
    Ffmpeg         = Join-Path $repoRoot "MaritimeTargetMonitor\\Maritime.App\\bin\\x64\\Release\\net48\\ffmpeg-full\\bin\\ffmpeg.exe"
    Mediamtx       = Join-Path $repoRoot "MaritimeTargetMonitor\\Maritime.App\\bin\\x64\\Release\\net48\\mediamtx.exe"
    MediamtxConfig = Join-Path $repoRoot "MaritimeTargetMonitor\\Maritime.App\\bin\\x64\\Release\\net48\\mediamtx.yml"
    WorkDir        = Join-Path $repoRoot "edgeyolo-main\\edgeyolo-main"
}

if (-not $Python) { $Python = $defaults.Python }
if (-not $Weights) { $Weights = $defaults.Weights }
if (-not $InputFile) {
    $mp4 = Get-ChildItem -Path (Join-Path $repoRoot "edgeyolo-main\\edgeyolo-main") -Filter *.mp4 -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($mp4) { $InputFile = $mp4.FullName }
}
if (-not $Ffmpeg) { $Ffmpeg = $defaults.Ffmpeg }
if (-not $Mediamtx) { $Mediamtx = $defaults.Mediamtx }
if (-not $MediamtxConfig) { $MediamtxConfig = $defaults.MediamtxConfig }
if (-not $SnapshotPath) { $SnapshotPath = Join-Path $repoRoot "tools\\logs\\infer_snapshot.jpg" }

$Python = Resolve-DefaultPath $Python
$Weights = Resolve-DefaultPath $Weights
$InputFile = Resolve-DefaultPath $InputFile
$Ffmpeg = Resolve-DefaultPath $Ffmpeg
$Mediamtx = Resolve-DefaultPath $Mediamtx
$MediamtxConfig = Resolve-DefaultPath $MediamtxConfig

$missing = @()
if (-not $Python) { $missing += "Python" }
if (-not $Weights) { $missing += "Weights" }
if (-not $InputFile) { $missing += "InputFile" }
if (-not $Ffmpeg) { $missing += "Ffmpeg" }
if (-not $Mediamtx) { $missing += "Mediamtx" }
if (-not $MediamtxConfig) { $missing += "MediamtxConfig" }

if ($missing.Count -gt 0) {
    Write-Host "Missing required paths: $($missing -join ', ')"
    exit 2
}

Write-Host "Python: $Python"
Write-Host "Weights: $Weights"
Write-Host "InputFile: $InputFile"
Write-Host "FFmpeg: $Ffmpeg"
Write-Host "MediaMTX: $Mediamtx"
Write-Host "MediaMTX Config: $MediamtxConfig"
Write-Host "InputSize: $InputSize  OutputSize: $OutputSize  CapFps: $CapFps"
Write-Host "Snapshot: $SnapshotPath"

$relayProc = $null
$pushProc = $null
$algoProc = $null
$startedRelay = $false

try {
    Write-Step "Start MediaMTX (if not running)"
    $existingRelay = Get-Process -Name "mediamtx" -ErrorAction SilentlyContinue
    if ($existingRelay) {
        Write-Host "MediaMTX already running (pid(s): $($existingRelay.Id -join ', '))."
    } else {
        $relayProc = Start-Process -FilePath $Mediamtx -ArgumentList @($MediamtxConfig) -WorkingDirectory (Split-Path $Mediamtx) -PassThru -WindowStyle Minimized
        $startedRelay = $true
        Start-Sleep -Seconds 2
        Write-Host "MediaMTX started (pid: $($relayProc.Id))."
    }

    Write-Step "Push test video to RTMP (live/raw)"
    $logDir = Join-Path $repoRoot "tools\\logs"
    if (-not (Test-Path -LiteralPath $logDir)) {
        New-Item -ItemType Directory -Path $logDir | Out-Null
    }
    $pushOut = Join-Path $logDir "infer.push.out.log"
    $pushErr = Join-Path $logDir "infer.push.err.log"
    $algoOut = Join-Path $logDir "infer.algo.out.log"
    $algoErr = Join-Path $logDir "infer.algo.err.log"

    $pushArgs = @(
        "-re",
        "-stream_loop", "-1",
        "-i", $InputFile,
        "-c:v", "copy",
        "-an",
        "-f", "flv",
        "rtmp://127.0.0.1:1935/live/raw"
    )
    $pushProc = Start-Process -FilePath $Ffmpeg -ArgumentList $pushArgs -PassThru -WindowStyle Minimized -RedirectStandardError $pushErr -RedirectStandardOutput $pushOut
    Write-Host "Pushing stream (pid: $($pushProc.Id))."
    Start-Sleep -Seconds $WarmupSec

    Write-Step "Run local algorithm inference (EdgeYOLO)"
    $streamDetect = Join-Path $defaults.WorkDir "stream_infer_local.py"
    $inputParts = $InputSize -split '[,xX ]+' | Where-Object { $_ -and $_.Trim().Length -gt 0 }
    $outputParts = $OutputSize -split '[,xX ]+' | Where-Object { $_ -and $_.Trim().Length -gt 0 }
    if ($inputParts.Count -lt 2) { throw "InputSize must have 2 numbers, e.g. 256,256" }
    if ($outputParts.Count -lt 2) { throw "OutputSize must have 2 numbers, e.g. 960x540" }

    $algoArgs = @(
        "-u", $streamDetect,
        "-w", $Weights,
        "-s", "rtmp://127.0.0.1:1935/live/raw",
        "-o", "rtmp://127.0.0.1:1935/live/m3t",
        "--ffmpeg", $Ffmpeg,
        "--input-size", $inputParts[0], $inputParts[1],
        "--out-size", $outputParts[0], $outputParts[1],
        "--cap-fps", $CapFps
    )
    if (-not $UseGpu) {
        $algoArgs += "--cpu"
    }
    $algoProc = Start-Process -FilePath $Python -ArgumentList $algoArgs -WorkingDirectory $defaults.WorkDir -PassThru -WindowStyle Minimized -RedirectStandardError $algoErr -RedirectStandardOutput $algoOut
    Write-Host "Algorithm started (pid: $($algoProc.Id))."
    Start-Sleep -Seconds $InferWarmupSec

    Write-Step "Verify algorithm output (RTSP live/m3t)"
    $outArgs = @(
        "-v", "error",
        "-rtsp_transport", "tcp",
        "-t", "2",
        "-i", "rtsp://127.0.0.1:8554/live/m3t",
        "-map", "0:v:0",
        "-c", "copy",
        "-f", "null",
        "-"
    )
    $outOk = $false
    $deadline = (Get-Date).AddSeconds(40)
    while ((Get-Date) -lt $deadline) {
        & $Ffmpeg @outArgs | Out-Host
        if ($LASTEXITCODE -eq 0) {
            $outOk = $true
            break
        }
        Start-Sleep -Seconds 2
    }
    if (-not $outOk) {
        throw "Output check failed (live/m3t)."
    }

    Write-Step "Snapshot a frame from algorithm output"
    $snapArgs = @(
        "-y",
        "-rtsp_transport", "tcp",
        "-i", "rtsp://127.0.0.1:8554/live/m3t",
        "-frames:v", "1",
        "-q:v", "2",
        $SnapshotPath
    )
    & $Ffmpeg @snapArgs | Out-Host
    Write-Host "Snapshot saved: $SnapshotPath"

    Write-Host "`nTEST RESULT: PASS"
    exit 0
}
catch {
    Write-Host "`nTEST RESULT: FAIL"
    Write-Host $_.Exception.Message
    exit 1
}
finally {
    if ($algoProc -and -not $algoProc.HasExited) {
        $algoProc.Kill()
        $algoProc.WaitForExit(2000) | Out-Null
    }
    if ($pushProc -and -not $pushProc.HasExited) {
        $pushProc.Kill()
        $pushProc.WaitForExit(2000) | Out-Null
    }
    if ($startedRelay -and $relayProc -and -not $relayProc.HasExited) {
        $relayProc.Kill()
        $relayProc.WaitForExit(2000) | Out-Null
    }
}
