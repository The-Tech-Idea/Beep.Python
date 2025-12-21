@echo off
REM Reset MLStudio Database - Windows Batch Script
echo.
echo ========================================
echo   MLStudio Database Reset Script
echo ========================================
echo.

REM Check if Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python is not installed or not in PATH
    echo Please install Python or add it to your PATH
    pause
    exit /b 1
)

REM Check if we should skip confirmation
if "%1"=="--force" goto force
if "%1"=="--yes" goto force

echo WARNING: This will delete ALL data in the database!
echo All tables will be dropped and recreated from scratch.
echo.
set /p confirm="Are you sure you want to continue? (yes/no): "
if /i not "%confirm%"=="yes" (
    if /i not "%confirm%"=="y" (
        echo Database reset cancelled.
        pause
        exit /b 0
    )
)

:force
echo.
echo Resetting database...
python reset_database.py --force %*
if errorlevel 1 (
    echo.
    echo [ERROR] Database reset failed!
    pause
    exit /b 1
)

echo.
echo Database reset completed successfully!
pause
