@echo off
setlocal
chcp 65001 >nul

echo ========================================
echo   Maritime Target Monitor 一键部署
echo ========================================
echo.

call "%~dp0tools\one_click_setup.bat" %*
