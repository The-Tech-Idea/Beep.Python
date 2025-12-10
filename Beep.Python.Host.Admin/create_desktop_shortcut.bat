@echo off
REM Create Desktop Shortcut - Simple batch wrapper
echo Creating desktop shortcut...
powershell -ExecutionPolicy Bypass -File "%~dp0create_desktop_shortcut.ps1"
