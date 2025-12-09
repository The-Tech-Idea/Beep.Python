@echo off
setlocal
set PORT=5000

:: Beep.Python Host Admin - Windows Startup Script
:: Checks for Python, creates venv, installs requirements, and runs the app.

echo ===================================================
echo   Beep.Python Host Admin - Startup
echo ===================================================

:: Check if Python is available
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Python not found! Please install Python 3.10+ and add it to your PATH.
    pause
    exit /b 1
)

:: Check for virtual environment folder
if not exist ".venv" (
    echo [INFO] Virtual environment not found. Creating one...
    python -m venv .venv
    if %errorlevel% neq 0 (
        echo [ERROR] Failed to create virtual environment.
        pause
        exit /b 1
    )
    echo [INFO] Virtual environment created.
)

:: Activate environment
call .venv\Scripts\activate.bat

:: Install requirements if not fully installed (simple check)
:: For robustness, we just attempt install/upgrade every time, or we could check pip freeze
echo [INFO] Checking dependencies...
pip install -r requirements.txt
if %errorlevel% neq 0 (
    echo [ERROR] Failed to install dependencies.
    pause
    exit /b 1
)

:: Run the application
echo [INFO] Starting application...
echo [INFO] Open your browser to http://127.0.0.1:%PORT%
set PORT=%PORT%
python run.py

pause
