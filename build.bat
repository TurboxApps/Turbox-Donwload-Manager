@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] C# Compiler csc.exe not found!
    pause
    exit /b 1
)

echo ======================================================================
echo [1/4] Compiling IDM Turbo Downloader v2.5...
echo ======================================================================
"%CSC%" /nologo /target:winexe /optimize+ /win32manifest:app.manifest /win32icon:app.ico /out:IDMTurboDownloader.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,Microsoft.Web.WebView2.Core.dll,Microsoft.Web.WebView2.WinForms.dll AssemblyInfo.cs IconFactory.cs ThemeManager.cs SecurityGuard.cs AboutDialog.cs SchedulerDialog.cs OptionsDialog.cs AddUrlDialog.cs MediaCatchDialog.cs IDMBrowserSuite.cs SpeedometerGauge.cs SpeedTestDialog.cs DownloadProgressDialog.cs ClipboardNotificationPopup.cs DownloadEngine.cs VideoEngine.cs TorrentEngine.cs MainForm.cs Program.cs

if %ERRORLEVEL% NEQ 0 (
    echo [FAILED] Compilation failed!
    if "%1"=="" pause
    exit /b 1
)

echo [SUCCESS] IDMTurboDownloader.exe compiled!

echo.
echo ======================================================================
echo [2/4] Applying Anti-Tamper Code Obfuscation (Obfuscar)...
echo ======================================================================
if exist "obfuscar_tools\tools\Obfuscar.Console.exe" (
    ".\obfuscar_tools\tools\Obfuscar.Console.exe" obfuscar.xml
    if exist "Obfuscated\IDMTurboDownloader.exe" (
        copy /y "Obfuscated\IDMTurboDownloader.exe" "IDMTurboDownloader.exe" >nul
        echo [PROTECTED] Anti-tamper obfuscation applied successfully!
    )
) else (
    echo [NOTICE] Obfuscar tool not found, skipping obfuscation.
)

echo.
echo ======================================================================
echo [3/4] Packaging Standalone Setup Installer (IDMTurbo_Setup.exe)...
echo ======================================================================
powershell -NoProfile -Command ^
    "if (Test-Path 'staging_payload') { Remove-Item -Recurse -Force 'staging_payload' }; " ^
    "New-Item -ItemType Directory -Path 'staging_payload' | Out-Null; " ^
    "Copy-Item -Force 'IDMTurboDownloader.exe', 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'app.manifest', 'app.ico' 'staging_payload\'; " ^
    "Copy-Item -Recurse -Force 'runtimes' 'staging_payload\runtimes'; " ^
    "Copy-Item -Recurse -Force 'extension_brave' 'staging_payload\extension_brave'; " ^
    "Copy-Item -Recurse -Force 'extension_chrome' 'staging_payload\extension_chrome'; " ^
    "Copy-Item -Recurse -Force 'extension_edge' 'staging_payload\extension_edge'; " ^
    "Copy-Item -Recurse -Force 'extension_firefox' 'staging_payload\extension_firefox'; " ^
    "Copy-Item -Recurse -Force 'extension' 'staging_payload\extension'; " ^
    "if (Test-Path 'payload.zip') { Remove-Item -Force 'payload.zip' }; " ^
    "Add-Type -AssemblyName System.IO.Compression.FileSystem; " ^
    "[System.IO.Compression.ZipFile]::CreateFromDirectory('staging_payload', 'payload.zip', [System.IO.Compression.CompressionLevel]::Optimal, $false); " ^
    "Remove-Item -Recurse -Force 'staging_payload'"

"%CSC%" /nologo /target:winexe /optimize+ /win32icon:app.ico /resource:payload.zip,payload.zip /out:IDMTurbo_Setup.exe /r:System.dll,System.Core.dll,System.Drawing.dll,System.Windows.Forms.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll SetupWizard.cs

if %ERRORLEVEL% EQU 0 (
    echo [SUCCESS] IDMTurbo_Setup.exe compiled!
) else (
    echo [WARNING] Failed to compile standalone installer.
)

echo.
echo ======================================================================
echo [4/4] Updating 'update' Distribution Folder...
echo ======================================================================
powershell -NoProfile -Command ^
    "if (-not (Test-Path 'update')) { New-Item -ItemType Directory -Path 'update' | Out-Null }; " ^
    "Copy-Item -Force 'IDMTurboDownloader.exe', 'IDMTurbo_Setup.exe', 'Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'app.manifest', 'app.ico' 'update\'; " ^
    "Copy-Item -Recurse -Force 'runtimes' 'update\runtimes'; " ^
    "Copy-Item -Recurse -Force 'extension_brave' 'update\extension_brave'; " ^
    "Copy-Item -Recurse -Force 'extension_chrome' 'update\extension_chrome'; " ^
    "Copy-Item -Recurse -Force 'extension_edge' 'update\extension_edge'; " ^
    "Copy-Item -Recurse -Force 'extension_firefox' 'update\extension_firefox'; " ^
    "Copy-Item -Recurse -Force 'extension' 'update\extension'"

echo [ALL DONE] Build, Obfuscation, and Update Folder are completely ready!
if "%1"=="" pause
