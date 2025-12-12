@echo off
REM Beep.Python Host Admin - Windows Launcher
REM This script automatically sets up and runs Host Admin

cd /d "%~dp0"

echo.
echo ============================================================
echo   Beep AI Server - Windows Launcher
echo ============================================================
echo.

REM Check if embedded Python exists
if exist "python-embedded\python.exe" (
    echo [INFO] Embedded Python found
    goto :run_launcher
)

REM Embedded Python not found - download it automatically
echo [INFO] Embedded Python not found. Downloading automatically...
echo.

REM Create directory
if not exist "python-embedded" mkdir python-embedded

REM Download embedded Python using PowerShell
echo [INFO] Downloading Python 3.11.7 Embedded (64-bit)...
echo        This may take a few minutes...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
    $ProgressPreference = 'SilentlyContinue'; ^
    Invoke-WebRequest -Uri 'https://www.python.org/ftp/python/3.11.7/python-3.11.7-embed-amd64.zip' -OutFile 'python-embedded.zip'"

if not exist "python-embedded.zip" (
    echo [ERROR] Failed to download Python!
    echo Please check your internet connection and try again.
    pause
    exit /b 1
)

REM Extract using PowerShell
echo [INFO] Extracting Python...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "Expand-Archive -Path 'python-embedded.zip' -DestinationPath 'python-embedded' -Force"

REM Clean up zip file
del python-embedded.zip

REM Configure embedded Python
echo [INFO] Configuring embedded Python...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "(Get-Content 'python-embedded\python311._pth') -replace '#import site', 'import site' | Set-Content 'python-embedded\python311._pth'"

REM Download and install pip
echo [INFO] Installing pip...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; ^
    $ProgressPreference = 'SilentlyContinue'; ^
    Invoke-WebRequest -Uri 'https://bootstrap.pypa.io/get-pip.py' -OutFile 'python-embedded\get-pip.py'"

python-embedded\python.exe python-embedded\get-pip.py
del python-embedded\get-pip.py

REM Verify installation
if not exist "python-embedded\python.exe" (
    echo [ERROR] Embedded Python installation failed!
    pause
    exit /b 1
)

echo [INFO] Embedded Python installed successfully!
echo.

:run_launcher
REM Install/upgrade pip and install requirements
echo [INFO] Installing required packages...
python-embedded\python.exe -m pip install --upgrade pip --quiet --no-warn-script-location
python-embedded\python.exe -m pip install -r requirements.txt --quiet --no-warn-script-location

REM Run the launcher with embedded Python
python-embedded\python.exe run_hostadmin.py %*

if errorlevel 1 (
    echo.
    echo [ERROR] Failed to start Host Admin
    pause
    exit /b 1
)

pause
