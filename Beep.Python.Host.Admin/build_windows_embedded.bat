@echo off
setlocal

REM Build Beep.Python Host Admin on Windows using the embedded Python runtime.
REM Prereqs: PowerShell available (for setup script), internet to download embedded Python on first run.

set "ROOT=%~dp0"
set "EMBED_DIR=%ROOT%python-embedded"
set "PYTHON_EXE=%EMBED_DIR%\python.exe"

echo === Windows Embedded Build ===

REM Ensure embedded Python exists
if not exist "%PYTHON_EXE%" (
    echo Embedded Python not found. Running setup_embedded_python.bat...
    call "%ROOT%setup_embedded_python.bat"
)

if not exist "%PYTHON_EXE%" (
    echo ERROR: Embedded Python was not installed. Aborting.
    exit /b 1
)

REM Upgrade pip and install pyinstaller inside embedded runtime
echo Installing/Updating PyInstaller in embedded runtime...
"%PYTHON_EXE%" -m pip install --upgrade pip pyinstaller
if errorlevel 1 (
    echo ERROR: Failed to install PyInstaller.
    exit /b 1
)

REM Run PyInstaller with the project spec
echo Running PyInstaller...
"%PYTHON_EXE%" -m PyInstaller --clean "%ROOT%beep_python_host.spec"
if errorlevel 1 (
    echo ERROR: PyInstaller build failed.
    exit /b 1
)

echo.
echo Build complete. Output folder:
echo   %ROOT%dist\BeepPythonHost
echo Use launch_beep_python_admin.cmd inside that folder to start the app.
echo.
endlocal
