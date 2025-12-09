@echo off
REM ============================================
REM Beep.Python - Application Launcher
REM Uses embedded Python if available
REM ============================================

title Beep.Python LLM Management

REM Check for embedded Python
if exist "python-embedded\python.exe" (
    echo Using embedded Python...
    set PYTHON_EXE=python-embedded\python.exe
) else (
    echo Embedded Python not found. Checking system Python...
    python --version >nul 2>&1
    if %errorlevel% neq 0 (
        echo.
        echo ERROR: Python not found!
        echo.
        echo Please run setup_embedded_python.bat first to install embedded Python.
        echo.
        pause
        exit /b 1
    )
    set PYTHON_EXE=python
)

echo Starting Beep.Python LLM Management...
echo.

REM Set environment variable for app to detect embedded mode
if exist "python-embedded\python.exe" (
    set BEEP_EMBEDDED_PYTHON=1
)

REM Run the application
%PYTHON_EXE% app.py

if %errorlevel% neq 0 (
    echo.
    echo Application exited with error code: %errorlevel%
    pause
)
