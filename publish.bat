@echo off
setlocal

echo ======================================================================
echo Publishing Reframe as a self-contained single-file executable (win-x64)...
echo ======================================================================

dotnet publish "%~dp0Reframe\Reframe.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true %*

if %ERRORLEVEL% equ 0 (
    echo.
    echo ======================================================================
    echo Publish succeeded!
    echo Output executable:
    echo   %~dp0Reframe\bin\Release\net9.0-windows\win-x64\publish\Reframe.exe
    echo ======================================================================
) else (
    echo.
    echo ======================================================================
    echo Publish failed with error code %ERRORLEVEL%.
    echo ======================================================================
    exit /b %ERRORLEVEL%
)

endlocal
