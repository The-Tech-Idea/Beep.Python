@echo off
setlocal

rem Simple launcher for the packaged BeepPythonHost.exe
set "APP_DIR=%~dp0"
if "%BEEP_PYTHON_HOME%"=="" set "BEEP_PYTHON_HOME=%LOCALAPPDATA%\BeepPython"
if not exist "%BEEP_PYTHON_HOME%" mkdir "%BEEP_PYTHON_HOME%" >nul 2>&1

rem Production mode by default
set "FLASK_ENV=production"
set "DEBUG=false"
set "OPEN_BROWSER=true"

set "HOST=%HOST%"
if "%HOST%"=="" set "HOST=127.0.0.1"
set "PORT=%PORT%"
if "%PORT%"=="" set "PORT=5000"

"%APP_DIR%BeepPythonHost.exe" %*
