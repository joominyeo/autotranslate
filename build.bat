@echo off
echo Building AutoTranslate...
dotnet restore
dotnet build --configuration Release
if %ERRORLEVEL% EQU 0 (
    echo Build completed successfully!
    echo You can run the application from: AutoTranslate\bin\Release\net8.0-windows\AutoTranslate.exe
) else (
    echo Build failed!
)
pause