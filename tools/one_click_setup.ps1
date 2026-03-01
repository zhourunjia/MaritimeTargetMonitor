param(
    [string]$Python = "",
    [string]$RuntimeDir = "",
    [switch]$Full
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Text)
    Write-Host ("`n== {0} ==" -f $Text)
}

function Download-File {
    param(
        [string]$Url,
        [string]$OutFile
    )
    Write-Host "下载: $Url"
    Write-Host "保存到: $OutFile"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    } catch {
        # ignore
    }
    Invoke-WebRequest -Uri $Url -OutFile $OutFile -UseBasicParsing
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$edgeRoot = Join-Path $repoRoot "edgeyolo-main\\edgeyolo-main"
if (-not (Test-Path -LiteralPath $edgeRoot)) {
    Write-Host "未找到 EdgeYOLO 目录: $edgeRoot"
    exit 1
}

$runtimeRoot = if ([string]::IsNullOrWhiteSpace($RuntimeDir)) {
    Join-Path $repoRoot "runtime\\python"
} else {
    $RuntimeDir
}

$cacheDir = Join-Path $repoRoot "tools\\cache"
if (-not (Test-Path -LiteralPath $cacheDir)) {
    New-Item -ItemType Directory -Path $cacheDir | Out-Null
}

# Use inference-only requirements by default
$reqInfer = Join-Path $edgeRoot "requirements_infer.txt"
$reqFull = Join-Path $edgeRoot "requirements.txt"
if ($Full -or -not (Test-Path -LiteralPath $reqInfer)) {
    $req = $reqFull
} else {
    $req = $reqInfer
}

if (-not (Test-Path -LiteralPath $req)) {
    Write-Host "requirements 文件不存在: $req"
    exit 2
}

$pythonExe = Join-Path $runtimeRoot "python.exe"
if (-not (Test-Path -LiteralPath $pythonExe)) {
    Write-Step "下载并解压便携式 Python"
    if (-not (Test-Path -LiteralPath $runtimeRoot)) {
        New-Item -ItemType Directory -Path $runtimeRoot | Out-Null
    }

    $pyVersion = "3.11.9"
    $pyZip = "python-$pyVersion-embed-amd64.zip"
    $pyUrl = "https://www.python.org/ftp/python/$pyVersion/$pyZip"
    $zipPath = Join-Path $cacheDir $pyZip

    if (-not (Test-Path -LiteralPath $zipPath)) {
        Download-File -Url $pyUrl -OutFile $zipPath
    }

    Expand-Archive -Path $zipPath -DestinationPath $runtimeRoot -Force

    # Enable site-packages in embeddable Python
    $pth = Get-ChildItem -Path $runtimeRoot -Filter "python*._pth" | Select-Object -First 1
    if ($pth) {
        $lines = Get-Content -LiteralPath $pth.FullName
        $updated = @()
        $hasSite = $false
        $hasLib = $false
        foreach ($line in $lines) {
            $text = $line
            if ($text -match "^#\s*import\s+site") {
                $text = "import site"
                $hasSite = $true
            } elseif ($text -match "^import\s+site") {
                $hasSite = $true
            }
            if ($text -match "^Lib\\\\site-packages") {
                $hasLib = $true
            }
            $updated += $text
        }
        if (-not $hasLib) {
            $updated += "Lib\\site-packages"
        }
        if (-not $hasSite) {
            $updated += "import site"
        }
        # Use ASCII to avoid BOM issues in embeddable Python ._pth
        Set-Content -LiteralPath $pth.FullName -Value $updated -Encoding ASCII
    }
}

if (-not (Test-Path -LiteralPath $pythonExe)) {
    Write-Host "Python 未就绪: $pythonExe"
    exit 3
}

Write-Step "安装/升级 pip"
$getPip = Join-Path $cacheDir "get-pip.py"
if (-not (Test-Path -LiteralPath $getPip)) {
    Download-File -Url "https://bootstrap.pypa.io/get-pip.py" -OutFile $getPip
}
& $pythonExe $getPip
& $pythonExe -m pip install --upgrade pip

Write-Step "安装依赖"
Write-Host "使用 requirements: $req"
& $pythonExe -m pip install -r $req

Write-Host "`n完成。便携式 Python 路径: $pythonExe"
