@echo off
chcp 65001 >nul
echo ========================================
echo     Maritime Target Monitor 发布程序
echo ========================================
echo.

cd /d "%~dp0"

echo [1/5] 检查构建文件...
set "BUILD_DIR=bin\x64\Release"
if not exist "%BUILD_DIR%\Maritime.App.exe" (
    echo [错误] 构建文件不存在！
    echo 请先运行"构建程序.bat"来构建项目。
    pause
    exit /b 1
)
echo [成功] 构建文件检查通过
echo.

echo [2/5] 清理旧的发布文件...
if exist "Publish" (
    echo 正在删除旧的发布文件...
    rmdir /s /q "Publish"
)
echo [成功] 清理完成
echo.

echo [3/5] 创建发布目录...
mkdir "Publish"
mkdir "Publish\data"
mkdir "Publish\Logs"
mkdir "Publish\Backup"
echo [成功] 发布目录创建完成
echo.

echo [4/5] 复制文件到发布目录...
echo 正在复制可执行文件...
copy "%BUILD_DIR%\Maritime.App.exe" "Publish\" >nul
copy "%BUILD_DIR%\Maritime.App.dll" "Publish\" >nul
copy "%BUILD_DIR%\Maritime.Core.dll" "Publish\" >nul
copy "%BUILD_DIR%\Maritime.Infrastructure.dll" "Publish\" >nul
copy "%BUILD_DIR%\Newtonsoft.Json.dll" "Publish\" >nul

echo 正在复制配置文件...
copy "config.json" "Publish\" >nul

echo 正在复制启动脚本...
copy "启动程序.bat" "Publish\" >nul
copy "使用说明.txt" "Publish\" >nul

echo [成功] 文件复制完成
echo.

echo [5/5] 创建发布包...
set "VERSION=1.0.0"
set "ZIP_FILE=MaritimeTargetMonitor_v%VERSION%.zip"

if exist "%ZIP_FILE%" del "%ZIP_FILE%"

echo 正在创建发布包...
powershell -Command "Compress-Archive -Path 'Publish\*' -DestinationPath '%ZIP_FILE%' -Force"
if %errorlevel% neq 0 (
    echo [警告] 创建发布包失败，但文件已复制到 Publish 目录。
) else (
    echo [成功] 发布包创建完成：%ZIP_FILE%
)
echo.

echo ========================================
echo           发布完成！
echo ========================================
echo.
echo 发布文件位置：
echo %~dp0Publish\
echo.
echo 发布包位置：
echo %~dp0%ZIP_FILE%
echo.
echo 使用说明：
echo 1. 将 Publish 目录下的所有文件复制到目标计算机
echo 2. 双击"启动程序.bat"启动应用程序
echo 3. 或者直接运行 Maritime.App.exe
echo.
pause
