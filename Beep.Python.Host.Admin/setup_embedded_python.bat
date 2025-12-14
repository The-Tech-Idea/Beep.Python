@echo off
REM ============================================
REM Beep.Python - Embedded Python Setup
REM ============================================

echo ============================================
echo Beep.Python Embedded Python Setup
echo ============================================
echo.

REM Check if embedded Python already exists
if exist "python-embedded\python.exe" (
    echo Embedded Python already installed.
    echo Location: python-embedded\
    echo.
    REM Skip prompt in CI/CD environments
    if defined CI (
        echo CI environment detected. Using existing embedded Python.
        goto :end
    )
    choice /C YN /M "Do you want to re-download and reinstall"
    if errorlevel 2 goto :skip_download
    if errorlevel 1 goto :download
) else (
    goto :download
)

:download
echo.
echo Downloading Python 3.11.7 Embedded (64-bit)...
echo This will download approximately 10MB...
echo.

REM Create directory
if not exist "python-embedded" mkdir python-embedded

REM Download embedded Python using PowerShell
powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://www.python.org/ftp/python/3.11.7/python-3.11.7-embed-amd64.zip' -OutFile 'python-embedded.zip'}"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Failed to download Python!
    echo Please check your internet connection.
    pause
    exit /b 1
)

echo.
echo Extracting Python...
powershell -Command "Expand-Archive -Path 'python-embedded.zip' -DestinationPath 'python-embedded' -Force"

REM Clean up zip file
del python-embedded.zip

echo.
echo Configuring embedded Python...

REM Enable site-packages by uncommenting import site in python311._pth
powershell -Command "(Get-Content 'python-embedded\python311._pth') -replace '#import site', 'import site' | Set-Content 'python-embedded\python311._pth'"

REM Download get-pip.py
echo Downloading pip installer...
powershell -Command "Invoke-WebRequest -Uri 'https://bootstrap.pypa.io/get-pip.py' -OutFile 'python-embedded\get-pip.py'"

REM Install pip
echo Installing pip...
python-embedded\python.exe python-embedded\get-pip.py

REM Clean up get-pip.py
del python-embedded\get-pip.py

echo.
echo Installing application dependencies...
python-embedded\python.exe -m pip install -r requirements.txt

REM Create protection marker file
echo CRITICAL_SYSTEM_COMPONENT > python-embedded\.beep_protected
echo This directory contains the embedded Python runtime required for Beep.Python to function. >> python-embedded\.beep_protected
echo DO NOT DELETE this directory unless you are uninstalling the application. >> python-embedded\.beep_protected
echo Deletion will prevent the application from starting. >> python-embedded\.beep_protected

echo.
echo ============================================
echo Setup Complete!
echo ============================================
echo.
echo Embedded Python installed to: python-embedded\
echo Python version: 3.11.7
echo.
echo You can now run the application using start.bat
echo.
goto :end

:skip_download
echo.
echo Skipping download. Using existing embedded Python.
echo.

:end
REM Skip pause in CI/CD environments
if not defined CI (
    pause
)
