#!/usr/bin/env python3
"""
Admin Database Initialization
Uses embedded Python only - no venv dependencies
"""
import sys
import os
from pathlib import Path

# Add current directory to path so we can import app
script_dir = Path(__file__).parent.absolute()
sys.path.insert(0, str(script_dir))

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
        message = message.replace('[OK]', '[OK]')
        message = message.replace('[WARN]', '[WARN]')
        message = message.replace('[ERROR]', '[ERROR]')
        message = message.replace('[INFO]', '[INFO]')
        print(message)

try:
    # Verify we're running from embedded Python
    python_exe = Path(sys.executable)
    is_embedded = 'python-embedded' in str(python_exe) or python_exe.parent.name == 'python-embedded'
    
    if not is_embedded:
        safe_print("[WARN] Warning: Not running from embedded Python!")
        safe_print("[WARN] Admin operations should use embedded Python only.")
        safe_print(f"[INFO] Current Python: {python_exe}")
    
    from app import create_app, db
    
    app = create_app()
    
    # Update database URI to use absolute path before creating context
    db_uri = app.config.get('SQLALCHEMY_DATABASE_URI', 'sqlite:///mlstudio.db')
    if db_uri.startswith('sqlite:///') and not db_uri.replace('sqlite:///', '').startswith('/'):
        # Make it absolute
        db_file = script_dir / db_uri.replace('sqlite:///', '')
        app.config['SQLALCHEMY_DATABASE_URI'] = f'sqlite:///{db_file}'
        safe_print(f"[INFO] Database will be created at: {db_file}")
    
    with app.app_context():
        try:
            # Get database URI to know where file will be created
            db_uri = app.config.get('SQLALCHEMY_DATABASE_URI', 'sqlite:///mlstudio.db')
            safe_print(f"[INFO] Database URI: {db_uri}")
            
            # Get database path - use absolute path to project root
            db_uri = app.config.get('SQLALCHEMY_DATABASE_URI', 'sqlite:///mlstudio.db')
            
            # Ensure database is in project root, not instance folder
            if db_uri.startswith('sqlite:///'):
                db_file = db_uri.replace('sqlite:///', '')
                # Make absolute path to project root
                if not Path(db_file).is_absolute():
                    db_file_abs = script_dir / db_file
                    # Update URI to use absolute path
                    app.config['SQLALCHEMY_DATABASE_URI'] = f'sqlite:///{db_file_abs}'
                    # Need to recreate app context with new config
                    safe_print(f"[INFO] Using database path: {db_file_abs}")
            
            # Get final database path
            final_uri = app.config.get('SQLALCHEMY_DATABASE_URI', 'sqlite:///mlstudio.db')
            db_file = final_uri.replace('sqlite:///', '')
            db_path = Path(db_file)
            
            # Create all tables
            db.create_all()
            safe_print("[OK] Database tables created successfully!")
            
            # CRITICAL: Force database file creation by writing actual data
            # SQLite only creates the file when data is written, not just schema
            from app.models.settings import Settings
            
            safe_print("[INFO] Writing initial data to create database file...")
            
            # Create at least one setting to force file creation
            try:
                # Check if settings table has any data
                existing_count = db.session.query(Settings).count()
                
                if existing_count == 0:
                    # Create a real setting to force file creation
                    init_setting = Settings(
                        key='app_initialized',
                        category='general',
                        value_type='boolean',
                        description='Application initialization marker'
                    )
                    init_setting.set_value('true')
                    db.session.add(init_setting)
                    db.session.commit()
                    safe_print("[OK] Initial data written to database")
                else:
                    safe_print(f"[OK] Database already has {existing_count} records")
                
                # Force connection close and reopen to ensure file is written
                db.session.close()
                db.engine.dispose()
                
                # Reconnect to verify file exists
                import time
                time.sleep(0.2)  # Give filesystem time to sync
                
                # Check actual database file location from engine
                actual_db_path = None
                try:
                    # Get the actual file path from SQLite connection
                    from sqlalchemy import inspect as sql_inspect
                    engine = db.engine
                    if hasattr(engine, 'url') and engine.url.database:
                        actual_db_path = Path(engine.url.database)
                        if not actual_db_path.is_absolute():
                            # Resolve relative path
                            actual_db_path = Path.cwd() / actual_db_path
                        safe_print(f"[INFO] Database path from engine: {actual_db_path}")
                except Exception as e:
                    safe_print(f"[WARN] Could not get engine path: {e}")
                
                # Verify file exists - check both expected and actual paths
                file_found = False
                final_db_path = None
                
                if db_path.exists():
                    file_size = db_path.stat().st_size
                    safe_print(f"[OK] Database file verified: {db_path.absolute()} ({file_size} bytes)")
                    file_found = True
                    final_db_path = db_path
                elif actual_db_path and actual_db_path.exists():
                    file_size = actual_db_path.stat().st_size
                    safe_print(f"[INFO] Database file found at: {actual_db_path.absolute()} ({file_size} bytes)")
                    # Move from instance folder to root if needed
                    if 'instance' in str(actual_db_path) and db_path != actual_db_path:
                        safe_print(f"[INFO] Moving database from instance folder to root...")
                        try:
                            import shutil
                            # Close all connections first
                            db.session.close()
                            db.engine.dispose()
                            # Move the file
                            shutil.move(str(actual_db_path), str(db_path))
                            safe_print(f"[OK] Database moved to: {db_path.absolute()}")
                            # Update app config to point to new location
                            app.config['SQLALCHEMY_DATABASE_URI'] = f'sqlite:///{db_path}'
                            # Recreate tables in new location
                            db.create_all()
                            # Reinitialize settings
                            from app.services.settings_manager import get_settings_manager
                            settings_mgr = get_settings_manager()
                            safe_print(f"[OK] Database reinitialized in root location")
                            file_found = True
                            final_db_path = db_path
                        except Exception as e:
                            safe_print(f"[ERROR] Could not move database file: {e}")
                            import traceback
                            safe_print(traceback.format_exc())
                            sys.exit(1)
                    else:
                        file_found = True
                        final_db_path = actual_db_path
                
                if not file_found:
                    safe_print(f"[ERROR] CRITICAL: Database file not found!")
                    safe_print(f"[ERROR] Expected: {db_path.absolute()}")
                    if actual_db_path:
                        safe_print(f"[ERROR] Engine path: {actual_db_path.absolute()}")
                    safe_print(f"[ERROR] Current dir: {Path.cwd()}")
                    safe_print(f"[ERROR] Database URI: {db_uri}")
                    sys.exit(1)
                
                # Verify final location
                if final_db_path and final_db_path.exists():
                    file_size = final_db_path.stat().st_size
                    safe_print(f"[OK] Database file confirmed: {final_db_path.absolute()} ({file_size} bytes)")
                else:
                    safe_print(f"[ERROR] Database file verification failed!")
                    sys.exit(1)
                
                # Now initialize all default settings
                from app.services.settings_manager import get_settings_manager
                settings_mgr = get_settings_manager()
                settings_count = len(settings_mgr.get_all())
                
                if settings_count > 0:
                    safe_print(f"[OK] Settings initialized: {settings_count} settings available")
                else:
                    safe_print("[OK] Settings system ready")
                    
            except Exception as e:
                safe_print(f"[ERROR] Failed to create database file: {e}")
                import traceback
                safe_print(traceback.format_exc())
                sys.exit(1)
            
            # Verify database file exists
            if 'sqlite' in db_uri:
                # Extract filename from sqlite:///filename.db
                db_file = db_uri.replace('sqlite:///', '')
                db_path = Path(db_file)
                if db_path.exists():
                    safe_print(f"[OK] Database file created: {db_path.absolute()}")
                else:
                    # SQLite creates file on first write, so it's OK if it doesn't exist yet
                    safe_print(f"[INFO] Database file will be created on first use: {db_path.absolute()}")
            
            # Initialize settings (within app context)
            try:
                from app.services.settings_manager import get_settings_manager
                # Force initialization by getting the manager
                settings_mgr = get_settings_manager()
                # Settings are initialized in __init__, but we can verify
                settings_count = len(settings_mgr.get_all())
                if settings_count > 0:
                    safe_print(f"[OK] Settings initialized: {settings_count} settings available")
                else:
                    safe_print("[OK] Settings system ready (will initialize on first use)")
            except Exception as e:
                safe_print(f"[WARN] Warning: Settings initialization had issues: {e}")
                if '--verbose' in sys.argv:
                    import traceback
                    traceback.print_exc()
            
            safe_print("[OK] Database initialized successfully!")
            safe_print("[OK] Tables created: ml_projects, experiments, workflows, settings")
            
        except Exception as e:
            safe_print(f"[ERROR] Error creating database tables: {e}")
            if '--verbose' in sys.argv:
                import traceback
                traceback.print_exc()
            sys.exit(1)
            
except ImportError as e:
    safe_print(f"[ERROR] Import error: {e}")
    safe_print("[INFO] Make sure embedded Python has required packages installed:")
    safe_print("   python-embedded\\python.exe -m pip install flask flask-sqlalchemy python-dotenv")
    safe_print(f"[INFO] Current Python: {sys.executable}")
    safe_print(f"[INFO] Python path: {sys.path}")
    sys.exit(1)
except Exception as e:
    safe_print(f"[ERROR] Unexpected error: {e}")
    if '--verbose' in sys.argv:
        import traceback
        traceback.print_exc()
    sys.exit(1)

