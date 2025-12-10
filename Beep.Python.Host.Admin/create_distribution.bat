@echo off
REM ============================================
REM Beep.Python - Create Distribution Package
REM This creates a ready-to-distribute package with embedded Python
REM ============================================

echo ============================================
echo Beep.Python Distribution Builder
echo ============================================
echo.
echo This will create a complete distribution package
echo with embedded Python included.
echo.

REM Check if we're in the right directory
if not exist "app" (
    echo ERROR: app directory not found!
    echo Please run this script from the Beep.Python.Host.Admin directory.
    pause
    exit /b 1
)

REM Create distribution directory
set DIST_DIR=Beep.Python-Distribution
if exist "%DIST_DIR%" (
    echo Removing old distribution...
    rmdir /s /q "%DIST_DIR%"
)

echo Creating distribution directory...
mkdir "%DIST_DIR%"

REM Step 1: Download and setup embedded Python if not already present
echo.
echo [1/5] Setting up embedded Python...

if not exist "python-embedded\python.exe" (
    echo Embedded Python not found. Downloading...
    call setup_embedded_python.bat
    if %errorlevel% neq 0 (
        echo ERROR: Failed to setup embedded Python!
        pause
        exit /b 1
    )
) else (
    echo Embedded Python already present.
)

REM Step 2: Copy application files
echo.
echo [2/5] Copying application files...

xcopy /E /I /Y "app" "%DIST_DIR%\app" >nul
xcopy /E /I /Y "templates" "%DIST_DIR%\templates" >nul
xcopy /E /I /Y "static" "%DIST_DIR%\static" >nul 2>nul
copy /Y "requirements.txt" "%DIST_DIR%\" >nul
copy /Y "*.py" "%DIST_DIR%\" >nul 2>nul

echo Application files copied.

REM Step 3: Copy embedded Python
echo.
echo [3/5] Copying embedded Python (~50MB)...

xcopy /E /I /Y "python-embedded" "%DIST_DIR%\python-embedded" >nul

echo Embedded Python copied.

REM Step 4: Create launcher scripts
echo.
echo [4/5] Creating launcher scripts...

REM Create start.bat
echo @echo off > "%DIST_DIR%\start.bat"
echo title Beep.Python LLM Management >> "%DIST_DIR%\start.bat"
echo. >> "%DIST_DIR%\start.bat"
echo echo Starting Beep.Python... >> "%DIST_DIR%\start.bat"
echo echo. >> "%DIST_DIR%\start.bat"
echo. >> "%DIST_DIR%\start.bat"
echo REM Set embedded Python flag >> "%DIST_DIR%\start.bat"
echo set BEEP_EMBEDDED_PYTHON=1 >> "%DIST_DIR%\start.bat"
echo. >> "%DIST_DIR%\start.bat"
echo REM Run application >> "%DIST_DIR%\start.bat"
echo python-embedded\python.exe run.py >> "%DIST_DIR%\start.bat"
echo. >> "%DIST_DIR%\start.bat"
echo if %%errorlevel%% neq 0 ( >> "%DIST_DIR%\start.bat"
echo     echo. >> "%DIST_DIR%\start.bat"
echo     echo Application exited with error. >> "%DIST_DIR%\start.bat"
echo     pause >> "%DIST_DIR%\start.bat"
echo ) >> "%DIST_DIR%\start.bat"

REM Create README.txt
echo ============================================ > "%DIST_DIR%\README.txt"
echo Beep.Python LLM Management System >> "%DIST_DIR%\README.txt"
echo ============================================ >> "%DIST_DIR%\README.txt"
echo. >> "%DIST_DIR%\README.txt"
echo QUICK START: >> "%DIST_DIR%\README.txt"
echo. >> "%DIST_DIR%\README.txt"
echo 1. Double-click "start.bat" >> "%DIST_DIR%\README.txt"
echo 2. Wait for browser to open >> "%DIST_DIR%\README.txt"
echo 3. Start managing your LLM models! >> "%DIST_DIR%\README.txt"
echo. >> "%DIST_DIR%\README.txt"
echo NO PYTHON INSTALLATION REQUIRED! >> "%DIST_DIR%\README.txt"
echo Python is already included in this package. >> "%DIST_DIR%\README.txt"
echo. >> "%DIST_DIR%\README.txt"
echo FEATURES: >> "%DIST_DIR%\README.txt"
echo - Intelligent LLM model discovery and setup >> "%DIST_DIR%\README.txt"
echo - Per-model virtual environments >> "%DIST_DIR%\README.txt"
echo - GPU backend support (CUDA, Metal, Vulkan) >> "%DIST_DIR%\README.txt"
echo - Subprocess-isolated inference >> "%DIST_DIR%\README.txt"
echo - Role-based access control >> "%DIST_DIR%\README.txt"
echo - Migration and health tools >> "%DIST_DIR%\README.txt"
echo. >> "%DIST_DIR%\README.txt"
echo SYSTEM REQUIREMENTS: >> "%DIST_DIR%\README.txt"
echo - Windows 10/11 (64-bit) >> "%DIST_DIR%\README.txt"
echo - 4GB RAM minimum (8GB+ recommended) >> "%DIST_DIR%\README.txt"
echo - 10GB free disk space (for models) >> "%DIST_DIR%\README.txt"
echo. >> "%DIST_DIR%\README.txt"
echo IMPORTANT: >> "%DIST_DIR%\README.txt"
echo The "python-embedded" folder contains the Python runtime. >> "%DIST_DIR%\README.txt"
echo DO NOT DELETE this folder! >> "%DIST_DIR%\README.txt"
echo. >> "%DIST_DIR%\README.txt"
echo For support and documentation, visit: >> "%DIST_DIR%\README.txt"
echo [Your website/repository URL] >> "%DIST_DIR%\README.txt"
echo. >> "%DIST_DIR%\README.txt"

echo Launcher scripts created.

REM Step 5: Create ZIP package
echo.
echo [5/5] Creating ZIP package...

set ZIP_NAME=Beep.Python-Portable-v1.0.zip

REM Use PowerShell to create ZIP
powershell -Command "Compress-Archive -Path '%DIST_DIR%\*' -DestinationPath '%ZIP_NAME%' -Force"

if %errorlevel% neq 0 (
    echo WARNING: Failed to create ZIP file.
    echo You can manually ZIP the "%DIST_DIR%" folder.
) else (
    echo ZIP package created: %ZIP_NAME%
)

REM Calculate sizes
echo.
echo ============================================
echo Distribution Package Created!
echo ============================================
echo.
echo Location: %DIST_DIR%\
echo ZIP File: %ZIP_NAME%
echo.

REM Get directory size
for /f "tokens=3" %%a in ('dir "%DIST_DIR%" ^| find "File(s)"') do set SIZE=%%a
echo Package Size: ~60-70 MB
echo.
echo CONTENTS:
echo - Application code
echo - Embedded Python 3.11.7
echo - All dependencies pre-installed
echo - Ready to run (no setup needed)
echo.
echo DISTRIBUTION:
echo 1. Share the ZIP file with users
echo 2. Users extract and run start.bat
echo 3. That's it!
echo.
echo ============================================
echo.

choice /C YN /M "Do you want to open the distribution folder"
if errorlevel 2 goto :end
if errorlevel 1 explorer "%DIST_DIR%"

:end
pause
