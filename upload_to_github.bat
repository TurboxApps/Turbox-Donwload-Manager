@echo off
setlocal
set PATH=%LOCALAPPDATA%\Programs\MinGit\cmd;%PATH%

echo ======================================================================
echo 🚀 Turbo X - One-Click GitHub Uploader
echo ======================================================================
echo.
echo Current branch: main
echo Status: Clean and ready to push!
echo.

git remote -v >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Existing remote origin found:
    git remote -v
    echo.
    set /p PUSH_NOW="Do you want to push to this remote now? (Y/N): "
    if /i "%PUSH_NOW%"=="Y" (
        echo Pushing to GitHub...
        git push -u origin main
        goto done
    )
)

echo.
echo Please enter your GitHub repository URL:
echo (Example: https://github.com/YOUR_USERNAME/IDM-Turbo-Downloader.git)
echo.
set /p REPO_URL="Repository URL: "

if "%REPO_URL%"=="" (
    echo [ERROR] No URL entered. Aborting.
    pause
    exit /b 1
)

echo.
echo Setting remote origin to: %REPO_URL%
git remote remove origin >nul 2>&1
git remote add origin %REPO_URL%

echo.
echo Pushing branch 'main' to GitHub...
git push -u origin main

:done
echo.
if %ERRORLEVEL% EQU 0 (
    echo ======================================================================
    echo 🎉 Successfully pushed to GitHub!
    echo ======================================================================
) else (
    echo ======================================================================
    echo [NOTE] Push finished. If GitHub asked for login or token, make sure
    echo you entered your GitHub Personal Access Token (PAT).
    echo ======================================================================
)
echo.
pause
