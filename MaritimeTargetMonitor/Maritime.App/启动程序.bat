@echo off
chcp 65001 >nul
echo ========================================
echo     Maritime Target Monitor 启动程序
echo ========================================
echo.

cd /d "%~dp0"

echo [1/4] 检查配置文件...
if not exist "config.json" (
    echo [错误] 配置文件 config.json 不存在！
    echo 请确保 config.json 文件在程序根目录下。
    pause
    exit /b 1
)
echo [成功] 配置文件检查通过
echo.

echo [2/4] 检查可执行文件...
if exist "Maritime.App.exe" (
    echo [成功] 找到可执行文件，准备启动...
    goto :RUN_APP
)

echo [提示] 未找到可执行文件，尝试构建项目...
echo.

:: 检查 .NET SDK 是否安装
set "DOTNET_FOUND=false"
where dotnet >nul 2>&1
if %errorlevel% equ 0 (
    set "DOTNET_FOUND=true"
    echo [成功] 找到 .NET SDK
    
    :: 验证 .NET SDK 版本
    for /f "tokens=*" %%i in ('dotnet --version') do set "DOTNET_VERSION=%%i"
    echo [信息] .NET SDK 版本: %DOTNET_VERSION%
    
    :: 尝试使用 .NET SDK 构建
    echo [3/4] 使用 .NET SDK 构建项目...
    echo 正在构建项目，请稍候...
    echo.
    
    set "SOLUTION_PATH=%~dp0..\MaritimeTargetMonitor.sln"
    if not exist "%SOLUTION_PATH%" (
        echo [错误] 解决方案文件不存在: %SOLUTION_PATH%
        pause
        exit /b 1
    )
    
    dotnet build "%SOLUTION_PATH%" -c Debug -r win-x64
    if %errorlevel% neq 0 (
        echo [错误] 构建失败！
        echo 请查看上面的错误信息。
        pause
        exit /b 1
    )
    
    echo [成功] 构建完成！
    echo.
    
    echo [4/4] 复制可执行文件...
    set "BUILD_DIR=%~dp0..\Maritime.App\bin\Debug\net48\win-x64"
    if not exist "%BUILD_DIR%\Maritime.App.exe" (
        :: 尝试其他可能的输出目录
        set "BUILD_DIR=%~dp0..\Maritime.App\bin\Debug\net48"
        if not exist "%BUILD_DIR%\Maritime.App.exe" (
            echo [错误] 构建文件不存在: %BUILD_DIR%\Maritime.App.exe
            echo 请检查构建输出目录。
            pause
            exit /b 1
        )
    )
    
    copy /Y "%BUILD_DIR%\Maritime.App.exe" "%~dp0" >nul
    copy /Y "%BUILD_DIR%\Maritime.App.dll" "%~dp0" >nul
    copy /Y "%BUILD_DIR%\Maritime.Core.dll" "%~dp0" >nul
    copy /Y "%BUILD_DIR%\Maritime.Infrastructure.dll" "%~dp0" >nul
    copy /Y "%BUILD_DIR%\Newtonsoft.Json.dll" "%~dp0" >nul
    
    echo [成功] 文件复制完成
    echo.
    
    goto :RUN_APP
)

echo [提示] 未找到 .NET SDK，尝试使用 Visual Studio 构建...
echo.

echo [3/4] 查找 Visual Studio...
set "VS_PATH="
for %%v in (2022 2019 2017) do (
    if exist "C:\Program Files\Microsoft Visual Studio\%%v\Community\Common7\IDE\devenv.exe" (
        set "VS_PATH=C:\Program Files\Microsoft Visual Studio\%%v\Community\Common7\IDE\devenv.exe"
        goto :VS_FOUND
    )
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\%%v\Community\Common7\IDE\devenv.exe" (
        set "VS_PATH=C:\Program Files (x86)\Microsoft Visual Studio\%%v\Community\Common7\IDE\devenv.exe"
        goto :VS_FOUND
    )
)

:VS_FOUND
if not defined VS_PATH (
    echo [错误] 未找到 Visual Studio 和 .NET SDK！
    echo.
    echo 请按照以下步骤操作：
    echo 1. 安装 .NET SDK 6.0
    echo    参考：.NET SDK 安装指南.txt
    echo 2. 或者安装 Visual Studio 2017 或更高版本
    echo    安装时选择 ".NET 桌面开发" 工作负载
    echo 3. 重新运行此脚本
    echo.
    echo 或者手动构建：
    echo 1. 打开 MaritimeTargetMonitor.sln
    echo 2. 右键点击 Maritime.App 项目，选择"生成"
    echo 3. 将 bin\Debug\net48\Maritime.App.exe 复制到当前目录
    pause
    exit /b 1
)

echo [成功] 找到 Visual Studio: %VS_PATH%
echo.

echo [4/4] 使用 Visual Studio 构建项目...
echo 正在构建项目，请稍候...
echo.

set "SOLUTION_PATH=%~dp0..\MaritimeTargetMonitor.sln"
if not exist "%SOLUTION_PATH%" (
    echo [错误] 解决方案文件不存在: %SOLUTION_PATH%
    pause
    exit /b 1
)

"%VS_PATH%" "%SOLUTION_PATH%" /build Debug /out "%~dp0build.log"
if %errorlevel% neq 0 (
    echo [错误] 构建失败！
    echo 请查看 build.log 文件获取详细信息。
    notepad "%~dp0build.log"
    pause
    exit /b 1
)

echo [成功] 构建完成！
echo.

echo [5/5] 复制可执行文件...
set "BUILD_DIR=%~dp0..\Maritime.App\bin\Debug\net48"
if not exist "%BUILD_DIR%\Maritime.App.exe" (
    echo [错误] 构建文件不存在: %BUILD_DIR%\Maritime.App.exe
    echo 请检查构建输出目录。
    pause
    exit /b 1
)

copy /Y "%BUILD_DIR%\Maritime.App.exe" "%~dp0" >nul
copy /Y "%BUILD_DIR%\Maritime.App.dll" "%~dp0" >nul
copy /Y "%BUILD_DIR%\Maritime.Core.dll" "%~dp0" >nul
copy /Y "%BUILD_DIR%\Maritime.Infrastructure.dll" "%~dp0" >nul
copy /Y "%BUILD_DIR%\Newtonsoft.Json.dll" "%~dp0" >nul

echo [成功] 文件复制完成
echo.

:RUN_APP
echo [6/6] 启动应用程序...
echo 正在启动 Maritime Target Monitor...
echo.

start "" "Maritime.App.exe"

echo [成功] 应用程序已启动！
echo 如果应用程序没有正常启动，请检查日志文件。
echo.
timeout /t 2 >nul
