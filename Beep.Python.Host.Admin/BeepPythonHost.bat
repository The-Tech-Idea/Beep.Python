@echo off
REM Beep Python Host Admin Launcher
REM Uses embedded Python - no virtual environment needed

setlocal enabledelayedexpansion

REM Get the directory where this batch file is located
set "APP_DIR=%~dp0"
cd /d "%APP_DIR%"

REM Use embedded Python (required - no fallback to system Python)
if exist "python-embedded\python.exe" (
    set "PYTHON_EXE=%APP_DIR%python-embedded\python.exe"
    set "PYTHONPATH=%APP_DIR%python-embedded;%APP_DIR%python-embedded\Lib;%APP_DIR%python-embedded\Lib\site-packages"
) else (
    echo ERROR: Embedded Python not found!
    echo Please ensure python-embedded folder exists.
    pause
    exit /b 1
)

REM Set environment variables
set PYTHONUNBUFFERED=1
set BEEP_PYTHON_HOME=%APP_DIR%
set BEEP_CONFIG_DIR=%APP_DIR%config
set BEEP_DATA_DIR=%APP_DIR%data

REM Launch the application directly - dependencies are pre-installed in embedded Python
echo Starting Beep Python Host Admin...
"%PYTHON_EXE%" run.py

REM If python exits with error, pause to show message
if errorlevel 1 (
    echo.
    echo Application exited with error
    pause
)

endlocal
