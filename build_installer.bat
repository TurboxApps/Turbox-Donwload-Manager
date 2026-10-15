@echo off
set ISCC="C:\Users\Ai\AppData\Local\Programs\Inno Setup 6\ISCC.exe"

if not exist %ISCC% (
    set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

if not exist %ISCC% (
    echo [ERROR] Inno Setup Compiler ISCC.exe not found!
    pause
    exit /b 1
)

echo Compiling Turbo X Setup (TurboX_Setup.exe) with LZMA2 ultra compression...
%ISCC% "TurboX_Installer.iss"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================================
    echo [SUCCESS] TurboX_Setup.exe successfully generated in Output folder!
    echo ============================================================
) else (
    echo.
    echo [FAILED] Setup compilation failed.
)
if "%1"=="" pause
