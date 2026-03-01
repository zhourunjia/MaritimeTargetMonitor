param(
    [string]$Python = "",
    [string]$Weights = "",
    [string]$InputFile = "",
    [string]$Ffmpeg = "",
    [string]$Mediamtx = "",
    [string]$MediamtxConfig = "",
    [int]$WarmupSec = 12,
    [switch]$FullInfer,
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

$ffVer = & $Ffmpeg -version 2>&1
if ($ffVer -match "--disable-encoders") {
    Write-Host "`nFFmpeg build has encoders disabled. This build cannot output H264, algorithm streaming will fail."
    Write-Host "Use a full FFmpeg build (with libx264) and update AlgorithmFfmpegPath."
    exit 3
}

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
    $pushOut = Join-Path $logDir "push.out.log"
    $pushErr = Join-Path $logDir "push.err.log"
    $algoOut = Join-Path $logDir "algo.out.log"
    $algoErr = Join-Path $logDir "algo.err.log"

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

    Write-Step "Verify RTMP ingest (live/raw)"
    $ingestArgs = @(
        "-v", "error",
        "-t", "2",
        "-i", "rtmp://127.0.0.1:1935/live/raw",
        "-map", "0:v:0",
        "-c", "copy",
        "-f", "null",
        "-"
    )
    & $Ffmpeg @ingestArgs | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "RTMP ingest check failed (live/raw)."
    }
    Write-Host "RTMP ingest ok."

    Write-Step "Run algorithm (no-infer pipeline check)"
    $streamDetect = Join-Path $defaults.WorkDir "stream_infer_local.py"
    $algoArgs = @(
        "-u", $streamDetect,
        "-w", $Weights,
        "-s", "rtmp://127.0.0.1:1935/live/raw",
        "-o", "rtmp://127.0.0.1:1935/live/m3t",
        "--ffmpeg", $Ffmpeg,
        "--no-infer",
        "--max-frames", "200"
    )
    $algoProc = Start-Process -FilePath $Python -ArgumentList $algoArgs -WorkingDirectory $defaults.WorkDir -PassThru -WindowStyle Minimized -RedirectStandardError $algoErr -RedirectStandardOutput $algoOut
    Write-Host "Algorithm started (pid: $($algoProc.Id))."
    Start-Sleep -Seconds $WarmupSec
    if ($algoProc.HasExited) {
        Write-Host "Algorithm exited early with code $($algoProc.ExitCode)."
        if (Test-Path $algoErr) { Get-Content -Path $algoErr -Tail 200 | Out-Host }
        if (Test-Path $algoOut) { Get-Content -Path $algoOut -Tail 200 | Out-Host }
        throw "Algorithm exited before output was available."
    }

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
    $deadline = (Get-Date).AddSeconds(30)
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
    Write-Host "Algorithm output ok."

    if ($FullInfer) {
        Write-Step "Run algorithm (full inference quick check)"
        if ($algoProc -and -not $algoProc.HasExited) {
            $algoProc.Kill()
            $algoProc.WaitForExit(2000) | Out-Null
        }
        $algoArgs = @(
            "-u", $streamDetect,
            "-w", $Weights,
            "-s", "rtmp://127.0.0.1:1935/live/raw",
            "-o", "rtmp://127.0.0.1:1935/live/m3t",
            "--ffmpeg", $Ffmpeg
        )
        if (-not $UseGpu) {
            $algoArgs += "--cpu"
        }
        $algoProc = Start-Process -FilePath $Python -ArgumentList $algoArgs -WorkingDirectory $defaults.WorkDir -PassThru -WindowStyle Minimized -RedirectStandardError $algoErr -RedirectStandardOutput $algoOut
        Start-Sleep -Seconds $WarmupSec
        $outOk = $false
        $deadline = (Get-Date).AddSeconds(60)
        while ((Get-Date) -lt $deadline) {
            & $Ffmpeg @outArgs | Out-Host
            if ($LASTEXITCODE -eq 0) {
                $outOk = $true
                break
            }
            Start-Sleep -Seconds 2
        }
        if (-not $outOk) {
            throw "Full inference output check failed (live/m3t)."
        }
        Write-Host "Full inference output ok."
    }

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
