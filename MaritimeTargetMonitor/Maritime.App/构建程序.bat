@echo off
chcp 65001 >nul
echo ========================================
echo     Maritime Target Monitor 构建程序
echo ========================================
echo.

cd /d "%~dp0"

echo [1/4] 检查 Visual Studio 安装...
where msbuild >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 MSBuild，请确保已安装 Visual Studio。
    echo.
    echo 安装步骤：
    echo 1. 下载并安装 Visual Studio 2019 或更高版本
    echo 2. 安装时选择 ".NET 桌面开发" 工作负载
    echo 3. 重新运行此脚本
    pause
    exit /b 1
)
echo [成功] MSBuild 检查通过
echo.

echo [2/4] 检查解决方案文件...
if not exist "..\MaritimeTargetMonitor.sln" (
    echo [错误] 解决方案文件 MaritimeTargetMonitor.sln 不存在！
    pause
    exit /b 1
)
echo [成功] 解决方案文件检查通过
echo.

echo [3/4] 清理旧的构建文件...
msbuild "..\MaritimeTargetMonitor.sln" /t:Clean /p:Configuration=Release /p:Platform=x64 /v:minimal
if %errorlevel% neq 0 (
    echo [警告] 清理构建文件时出现错误，继续构建...
)
echo [成功] 清理完成
echo.

echo [4/4] 构建项目...
msbuild "..\MaritimeTargetMonitor.sln" /t:Build /p:Configuration=Release /p:Platform=x64 /v:minimal
if %errorlevel% neq 0 (
    echo [错误] 构建失败！
    echo 请检查错误信息并修复问题后重新构建。
    pause
    exit /b 1
)
echo [成功] 构建完成
echo.

echo ========================================
echo           构建成功！
echo ========================================
echo.
echo 可执行文件位置：
echo %~dp0bin\x64\Release\Maritime.App.exe
echo.
echo 下一步：
echo 1. 将 bin\x64\Release\ 目录下的所有文件复制到当前目录
echo 2. 确保 config.json 文件在当前目录下
echo 3. 双击"启动程序.bat"启动应用程序
echo.
echo 或者直接运行：
echo bin\x64\Release\Maritime.App.exe
echo.
pause
