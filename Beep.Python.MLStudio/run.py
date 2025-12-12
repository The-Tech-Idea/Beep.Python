#!/usr/bin/env python3
"""
Beep.Python.MLStudio
User-friendly ML Model Development Environment

Run with: python run_mlstudio.py (recommended - handles setup automatically)
Or: python run.py (if already set up)
"""
import os
import sys
import signal
import logging
import webbrowser
import threading
import time
import platform
from pathlib import Path

# Check if setup is needed - ALL REQUIREMENTS MUST BE MET
def check_setup():
    """Check if initial setup is needed - ALL REQUIREMENTS ARE MANDATORY"""
    issues = []
    
    # Check virtual environment - REQUIRED
    venv_path = Path('.venv')
    if not venv_path.exists():
        issues.append("Virtual environment not found (.venv) - REQUIRED")
    
    # Check .env file - REQUIRED
    env_file = Path('.env')
    if not env_file.exists():
        issues.append(".env file not found - REQUIRED")
    
    # Check database - REQUIRED (should exist after initialization)
    db_file = Path('mlstudio.db')
    if not db_file.exists():
        issues.append("Database not initialized (mlstudio.db) - REQUIRED")
    
    # Check embedded Python - REQUIRED
    embedded_path = Path('python-embedded')
    if platform.system() == 'Windows':
        embedded_python = embedded_path / 'python.exe'
    else:
        embedded_python = embedded_path / 'bin' / 'python3'
    if not embedded_python.exists():
        issues.append("Embedded Python not found - REQUIRED")
    
    # Check if Flask can be imported - REQUIRED
    try:
        import flask
    except ImportError:
        issues.append("Dependencies not installed (Flask not found) - REQUIRED")
        issues.append("Make sure you're running from the virtual environment (.venv)")
    
    # ALL issues are critical - exit if any found
    if issues:
        print("\n" + "=" * 70)
        print("❌ SETUP REQUIRED - ALL REQUIREMENTS MUST BE MET")
        print("=" * 70)
        print("\nThe following REQUIRED components are missing:")
        for issue in issues:
            print(f"  ❌ {issue}")
        print("\n" + "=" * 70)
        print("💡 SOLUTION: Use the launcher to set up everything:")
        print("=" * 70)
        print("\n  Windows: run.bat")
        print("  Linux/macOS: ./run.sh")
        print("  Cross-platform: python run_mlstudio.py")
        print("\n" + "=" * 70)
        print("\nThe launcher will automatically:")
        print("  ✅ Set up embedded Python")
        print("  ✅ Create virtual environment")
        print("  ✅ Install dependencies")
        print("  ✅ Create .env file")
        print("  ✅ Initialize database (REQUIRED)")
        print("  ✅ Start MLStudio")
        print("\n" + "=" * 70 + "\n")
        sys.exit(1)

# Check setup - ALL REQUIREMENTS MUST BE MET
# Note: run.py should be called from the venv Python, not system Python
# The launcher (run_mlstudio.py) handles this automatically
check_setup()

# Signal handler for graceful shutdown
def signal_handler(signum, frame):
    """Handle shutdown signals gracefully"""
    print("\n[MLStudio] Shutting down gracefully...")
    sys.exit(0)

# Register signal handlers
if sys.platform != 'win32':
    signal.signal(signal.SIGTERM, signal_handler)
    signal.signal(signal.SIGINT, signal_handler)
else:
    signal.signal(signal.SIGINT, signal_handler)

# Suppress Flask/Werkzeug development server warning
import warnings
warnings.filterwarnings('ignore', message='.*development server.*')
warnings.filterwarnings('ignore', message='.*WSGI server.*')

logging.getLogger('werkzeug').setLevel(logging.ERROR)

# Ensure we're in the correct directory
script_dir = Path(__file__).parent.absolute()
os.chdir(script_dir)

from app import create_app, socketio

app = create_app()

if __name__ == '__main__':
    host = os.environ.get('HOST', '127.0.0.1')
    port = int(os.environ.get('PORT', 5001))
    debug = os.environ.get('DEBUG', 'true').lower() == 'true'
    open_browser = os.environ.get('OPEN_BROWSER', 'true').lower() == 'true'
    
    mode = 'Development' if debug else 'Production'
    print(f"""
+==============================================================+
|           Beep.Python.MLStudio                                 |
|           ML Model Development Environment                     |
+--------------------------------------------------------------+
|  Server running at: http://{host}:{port:<5}                      |
|  Mode: {mode:<12}                                         |
|  Press Ctrl+C to stop the server                             |
+==============================================================+
    """)
    
    # Open browser after short delay
    if open_browser and not os.environ.get('MLSTUDIO_NO_BROWSER'):
        def open_browser_delayed():
            time.sleep(1.5)
            url = f'http://localhost:{port}' if host in ('0.0.0.0', '127.0.0.1') else f'http://{host}:{port}'
            webbrowser.open(url)
        
        browser_thread = threading.Thread(target=open_browser_delayed, daemon=True)
        browser_thread.start()
    
    log = logging.getLogger('werkzeug')
    log.setLevel(logging.ERROR)
    
    socketio.run(app, host=host, port=port, debug=debug, allow_unsafe_werkzeug=True, log_output=False)

