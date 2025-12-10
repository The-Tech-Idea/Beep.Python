#!/usr/bin/env python3
"""
Beep.Python Host Admin
Professional Python Environment Management Web Application

Run with: python run.py
Or: flask run
"""
import os
import sys
import signal
import logging
import multiprocessing


# Signal handler for graceful shutdown
def signal_handler(signum, frame):
    """Handle shutdown signals gracefully"""
    print("\n[BeepPython] Shutting down gracefully...")
    sys.exit(0)

# Register signal handlers
if sys.platform != 'win32':
    signal.signal(signal.SIGTERM, signal_handler)
    signal.signal(signal.SIGINT, signal_handler)
else:
    # Windows: SIGINT (Ctrl+C) still works
    signal.signal(signal.SIGINT, signal_handler)

# Suppress Flask/Werkzeug development server warning for desktop app
# This is expected behavior for a local desktop application
import warnings
warnings.filterwarnings('ignore', message='.*development server.*')
warnings.filterwarnings('ignore', message='.*WSGI server.*')

# Also suppress via logging
logging.getLogger('werkzeug').setLevel(logging.ERROR)


def get_base_path():
    """Get the base path for the application, handling frozen (PyInstaller) builds."""
    if getattr(sys, 'frozen', False) and hasattr(sys, '_MEIPASS'):
        # Running as a PyInstaller bundle
        return sys._MEIPASS
    else:
        # Running as a normal script
        return os.path.dirname(os.path.abspath(__file__))


# Set up paths for frozen builds
base_path = get_base_path()
sys.path.insert(0, base_path)

# For frozen builds, set working directory context
if getattr(sys, 'frozen', False):
    os.chdir(base_path)
    # Set UTF-8 encoding for console output on Windows
    if sys.platform == 'win32':
        import io
        sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
        sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from app import create_app, socketio

app = create_app()

if __name__ == '__main__':
    # CRITICAL: Required for PyInstaller to handle multiprocessing correctly
    import multiprocessing
    multiprocessing.freeze_support()

    import webbrowser
    import threading
    import click
    
    # Suppress click echo for cleaner output
    def secho_noop(*args, **kwargs):
        pass
    click.echo = secho_noop
    click.secho = secho_noop
    
    host = os.environ.get('HOST', '127.0.0.1')
    port = int(os.environ.get('PORT', 5000))
    # Default to production mode (debug=false) unless explicitly set
    debug = os.environ.get('DEBUG', 'false').lower() == 'true'
    # Auto-open browser (default true, can be disabled)
    open_browser = os.environ.get('OPEN_BROWSER', 'true').lower() == 'true'
    
    # Use ASCII-safe banner for compatibility with all console encodings
    mode = 'Development' if debug else 'Production'
    print(f"""
+==============================================================+
|           Beep.Python Host Admin                             |
|           Python Environment Management                      |
+--------------------------------------------------------------+
|  Server running at: http://{host}:{port:<5}                      |
|  Mode: {mode:<12}                                         |
|  Press Ctrl+C to stop the server                             |
+==============================================================+
    """)
    
    # Open browser after short delay to allow server to start
    if open_browser and not os.environ.get('BEEP_NO_BROWSER'):
        def open_browser_delayed():
            import time
            time.sleep(1.5)
            url = f'http://localhost:{port}' if host in ('0.0.0.0', '127.0.0.1') else f'http://{host}:{port}'
            webbrowser.open(url)
        
        browser_thread = threading.Thread(target=open_browser_delayed, daemon=True)
        browser_thread.start()
    
    # Run with logging suppressed for cleaner output
    log = logging.getLogger('werkzeug')
    log.setLevel(logging.ERROR)
    
    socketio.run(app, host=host, port=port, debug=debug, allow_unsafe_werkzeug=True, log_output=False)
