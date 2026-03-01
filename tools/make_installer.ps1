$repoRoot = "C:\Users\15666\Desktop\MaritimeTargetMonitor"
$solutionRoot = Join-Path $repoRoot "MaritimeTargetMonitor"
$releaseDir = Join-Path $solutionRoot "Maritime.App\bin\x64\Release\net48"
$distRoot = Join-Path $repoRoot "dist\MaritimeTargetMonitor"

if (-not (Test-Path $releaseDir)) { throw "Release output not found: $releaseDir" }

if (Test-Path $distRoot) { Remove-Item -Recurse -Force $distRoot }
New-Item -ItemType Directory -Force -Path $distRoot | Out-Null

Copy-Item -Recurse -Force (Join-Path $releaseDir '*') $distRoot

$edgeSrc = Join-Path $repoRoot "edgeyolo-main"
if (Test-Path $edgeSrc) {
    Copy-Item -Recurse -Force $edgeSrc (Join-Path $distRoot "edgeyolo-main")
}

$toolsSrc = Join-Path $repoRoot "tools"
if (Test-Path $toolsSrc) {
    Copy-Item -Recurse -Force $toolsSrc (Join-Path $distRoot "tools")
}

$runtimeSrc = Join-Path $repoRoot "runtime"
if (Test-Path $runtimeSrc) {
    Copy-Item -Recurse -Force $runtimeSrc (Join-Path $distRoot "runtime")
}

$bat = Join-Path $repoRoot "一键部署.bat"
if (Test-Path $bat) { Copy-Item -Force $bat $distRoot }
$guide = Join-Path $repoRoot "推流使用说明"
if (Test-Path $guide) { Copy-Item -Force $guide $distRoot }

# Create zip package
$zipPath = Join-Path $repoRoot "dist\MaritimeTargetMonitor_Package.zip"
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
Compress-Archive -Path (Join-Path $distRoot '*') -DestinationPath $zipPath -Force

# Build IExpress SED
$sedPath = Join-Path $repoRoot "dist\MaritimeTargetMonitor_Setup.sed"
$setupExe = Join-Path $repoRoot "dist\MaritimeTargetMonitor_Setup.exe"
if (Test-Path $setupExe) { Remove-Item -Force $setupExe }

$files = Get-ChildItem -Path $distRoot -Recurse -File
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('[Version]')
$lines.Add('Class=IEXPRESS')
$lines.Add('SEDVersion=3')
$lines.Add('')
$lines.Add('[Options]')
$lines.Add('PackagePurpose=InstallApp')
$lines.Add('ShowInstallProgramWindow=1')
$lines.Add('HideExtractAnimation=0')
$lines.Add('UseLongFileName=1')
$lines.Add('InsideCompressed=1')
$lines.Add('CAB_FixedSize=0')
$lines.Add('CAB_ResvCodeSigning=0')
$lines.Add('RebootMode=N')
$lines.Add('InstallPrompt=请选择安装目录')
$lines.Add('DisplayLicense=')
$lines.Add('FinishMessage=安装完成')
$lines.Add('TargetName=' + $setupExe)
$lines.Add('FriendlyName=MaritimeTargetMonitor')
$lines.Add('AppLaunched=Maritime.App.exe')
$lines.Add('PostInstallCmd=')
$lines.Add('AdminQuietInstCmd=')
$lines.Add('UserQuietInstCmd=')
$lines.Add('SourceFiles=SourceFiles')
$lines.Add('')
$lines.Add('[SourceFiles]')
$lines.Add('SourceFiles0=' + $distRoot)
$lines.Add('')
$lines.Add('[SourceFiles0]')

for ($i = 0; $i -lt $files.Count; $i++) {
    $lines.Add("%FILE$i%=")
}

$lines.Add('')
$lines.Add('[Strings]')
for ($i = 0; $i -lt $files.Count; $i++) {
    $rel = $files[$i].FullName.Substring($distRoot.Length + 1)
    $lines.Add("FILE$i=$rel")
}

$lines | Set-Content -Path $sedPath -Encoding ASCII

$iexpress = Join-Path $env:SystemRoot "System32\iexpress.exe"
if (Test-Path $iexpress) {
    & $iexpress /N /Q $sedPath | Out-Null
} else {
    Write-Host "IExpress not found, only zip created."
}

Write-Host "Package ready:"
Write-Host "- $zipPath"
Write-Host "- $setupExe"
