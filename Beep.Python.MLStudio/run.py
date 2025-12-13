#!/usr/bin/env python3
"""
Beep.Python.MLStudio
User-friendly ML Model Development Environment

Run with: python run_mlstudio.py (recommended - handles setup automatically)
Or: python run.py (if already set up)

Industry-specific mode:
  python run.py --industry=pet
  python run.py --industry=health
  python run.py --industry=oilandgas
"""
import os
import sys
import signal
import logging
import webbrowser
import threading
import time
import platform
import argparse
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
warnings.filterwarnings('ignore', category=DeprecationWarning, message='.*Eventlet is deprecated.*')
warnings.filterwarnings('ignore', category=DeprecationWarning, module='.*eventlet.*')

# Suppress harmless connection reset errors from eventlet
logging.getLogger('werkzeug').setLevel(logging.ERROR)
logging.getLogger('eventlet').setLevel(logging.CRITICAL)
logging.getLogger('eventlet.wsgi').setLevel(logging.CRITICAL)
logging.getLogger('eventlet.hubs').setLevel(logging.CRITICAL)
logging.getLogger('eventlet.greenthread').setLevel(logging.CRITICAL)

# Suppress ConnectionResetError tracebacks (harmless - client closed connection early)
# These occur when browsers close connections before the server finishes sending the response
class QuietConnectionErrorHandler(logging.Filter):
    """Filter to suppress ConnectionResetError logs"""
    def filter(self, record):
        # Suppress ConnectionResetError messages
        msg = str(record.getMessage())
        if 'ConnectionResetError' in msg or 'WinError 10054' in msg:
            return False
        if 'forcibly closed by the remote host' in msg:
            return False
        if 'Removing descriptor' in msg:
            return False
        return True

# Apply filter to eventlet loggers
quiet_filter = QuietConnectionErrorHandler()
logging.getLogger('eventlet').addFilter(quiet_filter)
logging.getLogger('eventlet.wsgi').addFilter(quiet_filter)
logging.getLogger('eventlet.hubs').addFilter(quiet_filter)
logging.getLogger('eventlet.greenthread').addFilter(quiet_filter)

# Patch eventlet's greenio.shutdown_safe to suppress ConnectionResetError
try:
    import eventlet.greenio.base as greenio_base
    _original_shutdown_safe = greenio_base.shutdown_safe
    
    def _quiet_shutdown_safe(sock):
        """Wrapper to suppress ConnectionResetError during socket shutdown"""
        try:
            return _original_shutdown_safe(sock)
        except (ConnectionResetError, OSError) as e:
            # Suppress these harmless errors - client closed connection early
            error_code = getattr(e, 'winerror', None) or getattr(e, 'errno', None)
            if error_code in (10054, 104):  # Connection reset by peer (Windows/Linux)
                return
            raise
    
    greenio_base.shutdown_safe = _quiet_shutdown_safe
except Exception:
    pass  # If patching fails, continue anyway

# Suppress ConnectionResetError exceptions in eventlet greenlets
# These are printed directly to stderr, not through logging
_original_excepthook = sys.excepthook

def _quiet_excepthook(exc_type, exc_value, exc_traceback):
    """Suppress ConnectionResetError tracebacks from eventlet"""
    if exc_type == ConnectionResetError:
        # Check if it's the harmless "connection closed by remote host" error
        error_msg = str(exc_value)
        if '10054' in error_msg or 'forcibly closed by the remote host' in error_msg:
            # Suppress this error - it's harmless
            return
    # For all other exceptions, use the original handler
    _original_excepthook(exc_type, exc_value, exc_traceback)

# Only override excepthook if we're in the main thread
if threading.current_thread() is threading.main_thread():
    sys.excepthook = _quiet_excepthook

# Ensure we're in the correct directory
script_dir = Path(__file__).parent.absolute()
os.chdir(script_dir)

from app import create_app, socketio

# Parse command-line arguments
parser = argparse.ArgumentParser(description='Beep ML Studio - ML Model Development Environment')
parser.add_argument('--industry', type=str, help='Force industry mode (pet, health, oilandgas, etc.)')
parser.add_argument('--host', type=str, default=None, help='Host address (default: 127.0.0.1)')
parser.add_argument('--port', type=int, default=None, help='Port number (default: 5002)')
parser.add_argument('--debug', action='store_true', help='Enable debug mode')
parser.add_argument('--no-browser', action='store_true', help='Do not open browser automatically')
args = parser.parse_args()

# Store forced industry mode in environment variable
if args.industry:
    os.environ['MLSTUDIO_FORCED_INDUSTRY'] = args.industry.lower()

app = create_app()

if __name__ == '__main__':
    host = args.host or os.environ.get('HOST', '127.0.0.1')
    port = args.port or int(os.environ.get('PORT', 5002))
    debug = args.debug or os.environ.get('DEBUG', 'true').lower() == 'true'
    open_browser = not args.no_browser and os.environ.get('OPEN_BROWSER', 'true').lower() == 'true'
    
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

