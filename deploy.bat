@echo off
echo ====================================
echo AutoTranslate Deployment Script
echo ====================================

set SOLUTION_DIR=%~dp0
set PROJECT_DIR=%SOLUTION_DIR%AutoTranslate
set OUTPUT_DIR=%SOLUTION_DIR%Deploy
set RELEASE_DIR=%PROJECT_DIR%\bin\Release\net8.0-windows

echo.
echo Cleaning previous builds...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

echo.
echo Building Release version...
dotnet clean --configuration Release
dotnet restore
dotnet build --configuration Release --no-restore

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Creating deployment folders...
mkdir "%OUTPUT_DIR%\Portable"
mkdir "%OUTPUT_DIR%\Installer"

echo.
echo Copying files for portable version...
xcopy "%RELEASE_DIR%\*" "%OUTPUT_DIR%\Portable\" /E /I /Y

echo.
echo Creating portable archive...
powershell -Command "Compress-Archive -Path '%OUTPUT_DIR%\Portable\*' -DestinationPath '%OUTPUT_DIR%\AutoTranslate-Portable.zip' -Force"

echo.
echo Copying installer files...
copy "%RELEASE_DIR%\AutoTranslate.exe" "%OUTPUT_DIR%\Installer\"
copy "%RELEASE_DIR%\*.dll" "%OUTPUT_DIR%\Installer\"
copy "%RELEASE_DIR%\*.json" "%OUTPUT_DIR%\Installer\" 2>nul
copy "%PROJECT_DIR%\icon.ico" "%OUTPUT_DIR%\Installer\"
copy "%SOLUTION_DIR%\README.md" "%OUTPUT_DIR%\"

echo.
echo Creating version info...
for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
echo .NET Version: %DOTNET_VERSION% > "%OUTPUT_DIR%\BuildInfo.txt"
echo Build Date: %DATE% %TIME% >> "%OUTPUT_DIR%\BuildInfo.txt"
echo Build Machine: %COMPUTERNAME% >> "%OUTPUT_DIR%\BuildInfo.txt"

echo.
echo ====================================
echo Deployment completed successfully!
echo ====================================
echo.
echo Files created:
echo - Portable: %OUTPUT_DIR%\AutoTranslate-Portable.zip
echo - Installer files: %OUTPUT_DIR%\Installer\
echo - Documentation: %OUTPUT_DIR%\README.md
echo.
echo Next steps:
echo 1. Test the portable version
echo 2. Create installer using WiX or similar
echo 3. Sign the executable (optional)
echo 4. Upload to release repository
echo.
pause