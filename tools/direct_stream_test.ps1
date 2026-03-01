param(
    [string]$InputFile = "",
    [string]$Ffmpeg = "",
    [string]$Mediamtx = "",
    [string]$MediamtxConfig = "",
    [int]$WarmupSec = 8
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
    Ffmpeg        = Join-Path $repoRoot "MaritimeTargetMonitor\\Maritime.App\\bin\\x64\\Release\\net48\\ffmpeg-full\\bin\\ffmpeg.exe"
    Mediamtx      = Join-Path $repoRoot "MaritimeTargetMonitor\\Maritime.App\\bin\\x64\\Release\\net48\\mediamtx.exe"
    MediamtxConfig= Join-Path $repoRoot "MaritimeTargetMonitor\\Maritime.App\\bin\\x64\\Release\\net48\\mediamtx.yml"
}

if (-not $InputFile) {
    $mp4 = Get-ChildItem -Path (Join-Path $repoRoot "edgeyolo-main\\edgeyolo-main") -Filter *.mp4 -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($mp4) { $InputFile = $mp4.FullName }
}
if (-not $Ffmpeg) { $Ffmpeg = $defaults.Ffmpeg }
if (-not $Mediamtx) { $Mediamtx = $defaults.Mediamtx }
if (-not $MediamtxConfig) { $MediamtxConfig = $defaults.MediamtxConfig }

$InputFile = Resolve-DefaultPath $InputFile
$Ffmpeg = Resolve-DefaultPath $Ffmpeg
$Mediamtx = Resolve-DefaultPath $Mediamtx
$MediamtxConfig = Resolve-DefaultPath $MediamtxConfig

$missing = @()
if (-not $InputFile) { $missing += "InputFile" }
if (-not $Ffmpeg) { $missing += "Ffmpeg" }
if (-not $Mediamtx) { $missing += "Mediamtx" }
if (-not $MediamtxConfig) { $missing += "MediamtxConfig" }

if ($missing.Count -gt 0) {
    Write-Host "Missing required paths: $($missing -join ', ')"
    exit 2
}

Write-Host "InputFile: $InputFile"
Write-Host "FFmpeg: $Ffmpeg"
Write-Host "MediaMTX: $Mediamtx"
Write-Host "MediaMTX Config: $MediamtxConfig"

$relayProc = $null
$pushProc = $null
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
    $pushOut = Join-Path $logDir "direct.push.out.log"
    $pushErr = Join-Path $logDir "direct.push.err.log"

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

    Write-Step "Verify RTSP direct output (live/raw)"
    $outArgs = @(
        "-v", "error",
        "-rtsp_transport", "tcp",
        "-t", "2",
        "-i", "rtsp://127.0.0.1:8554/live/raw",
        "-map", "0:v:0",
        "-c", "copy",
        "-f", "null",
        "-"
    )
    $outOk = $false
    $deadline = (Get-Date).AddSeconds(20)
    while ((Get-Date) -lt $deadline) {
        & $Ffmpeg @outArgs | Out-Host
        if ($LASTEXITCODE -eq 0) {
            $outOk = $true
            break
        }
        Start-Sleep -Seconds 2
    }
    if (-not $outOk) {
        throw "Direct RTSP check failed (live/raw)."
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
    if ($pushProc -and -not $pushProc.HasExited) {
        $pushProc.Kill()
        $pushProc.WaitForExit(2000) | Out-Null
    }
    if ($startedRelay -and $relayProc -and -not $relayProc.HasExited) {
        $relayProc.Kill()
        $relayProc.WaitForExit(2000) | Out-Null
    }
}
