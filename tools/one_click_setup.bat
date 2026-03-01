@echo off
setlocal
chcp 65001 >nul

set "MODE=%1"
set "ARGS="
if /I "%MODE%"=="full" set "ARGS=-Full"

echo ========================================
echo   EdgeYOLO 一键部署（便携式 Python + 依赖）
echo ========================================
echo.

set "SCRIPT=%~dp0one_click_setup.ps1"
if not exist "%SCRIPT%" (
  echo [错误] 未找到脚本: %SCRIPT%
  pause
  exit /b 1
)

powershell -ExecutionPolicy Bypass -File "%SCRIPT%" %ARGS%
echo.
echo 完成。
pause
