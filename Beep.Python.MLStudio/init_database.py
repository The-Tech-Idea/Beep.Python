#!/usr/bin/env python3
"""
Initialize MLStudio Database
"""
import sys
import traceback
import os
from pathlib import Path

# Fix Unicode encoding for Windows console
if sys.platform == 'win32':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
        sys.stderr.reconfigure(encoding='utf-8')
    except:
        pass

def safe_print(message):
    """Print message safely, handling Unicode issues"""
    try:
        print(message)
    except UnicodeEncodeError:
        # Fallback for systems that can't handle Unicode
        message = message.replace('✅', '[OK]')
        message = message.replace('⚠️', '[WARN]')
        message = message.replace('❌', '[ERROR]')
        message = message.replace('💡', '[INFO]')
        print(message)

try:
    # Verify we're running from embedded Python (check for embedded Python path)
    import sys
    python_exe = Path(sys.executable)
    is_embedded = 'python-embedded' in str(python_exe) or python_exe.parent.name == 'python-embedded'
    
    if not is_embedded:
        safe_print("[WARN] Warning: Not running from embedded Python!")
        safe_print("[WARN] Database initialization should use embedded Python.")
        safe_print(f"[INFO] Current Python: {python_exe}")
        # Continue anyway, but warn
    
    from app import create_app, db
    
    app = create_app()
    
    with app.app_context():
        try:
            # Create all tables
            db.create_all()
            safe_print("[OK] Database tables created successfully!")
            
            # Initialize settings (only if we're in proper context)
            try:
                from app.services.settings_manager import get_settings_manager
                # Settings manager will check for app context internally
                settings_mgr = get_settings_manager()
                safe_print("[OK] Settings initialized with default values")
            except Exception as e:
                safe_print(f"[WARN] Warning: Settings initialization had issues: {e}")
                if '--verbose' in sys.argv:
                    traceback.print_exc()
            
            safe_print("[OK] Database initialized successfully!")
            safe_print("[OK] Tables created: ml_projects, experiments, workflows, settings")
            
        except Exception as e:
            safe_print(f"[ERROR] Error creating database tables: {e}")
            if '--verbose' in sys.argv:
                traceback.print_exc()
            sys.exit(1)
            
except ImportError as e:
    safe_print(f"[ERROR] Import error: {e}")
    safe_print("[INFO] Make sure you're running this from the virtual environment:")
    safe_print("   .venv\\Scripts\\python.exe init_database.py (Windows)")
    safe_print("   .venv/bin/python init_database.py (Linux/macOS)")
    sys.exit(1)
except Exception as e:
    safe_print(f"[ERROR] Unexpected error: {e}")
    if '--verbose' in sys.argv:
        traceback.print_exc()
    sys.exit(1)

