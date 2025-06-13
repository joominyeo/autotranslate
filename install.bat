@echo off
echo Installing AutoTranslate...

REM Check if .NET 8 is installed
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo .NET 8 is not installed. Please install .NET 8 Desktop Runtime from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

REM Build the application
echo Building application...
call build.bat

REM Create shortcut (optional)
echo.
echo Installation complete!
echo.
echo To run AutoTranslate:
echo 1. Navigate to AutoTranslate\bin\Release\net8.0-windows\
echo 2. Run AutoTranslate.exe
echo.
echo Or create a desktop shortcut to the executable file.
echo.
pause