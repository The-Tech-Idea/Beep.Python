@echo off
REM Beep.Python.MLStudio - Windows Launcher
REM This script automatically sets up and runs MLStudio

cd /d "%~dp0"

echo.
echo ============================================================
echo   Beep.Python.MLStudio - Windows Launcher
echo ============================================================
echo.

REM Check if Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python is not installed or not in PATH
    echo Please install Python 3.8 or higher from https://www.python.org/
    pause
    exit /b 1
)

REM Run the Python launcher
python run_mlstudio.py

if errorlevel 1 (
    echo.
    echo [ERROR] Failed to start MLStudio
    pause
    exit /b 1
)

pause

