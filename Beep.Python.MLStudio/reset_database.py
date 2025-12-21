"""
Reset database and setup state for Beep.Python.MLStudio
This script deletes the database file(s) and setup completion marker
"""
import os
import sys
from pathlib import Path

def reset_database():
    """Reset database and setup state"""
    # Get project root directory
    project_root = Path(__file__).parent
    instance_dir = project_root / 'instance'
    
    print("=" * 60)
    print("Database Reset Script")
    print("=" * 60)
    print()
    
    # First, try to use Flask's database connection to drop all tables
    try:
        print("[INFO] Attempting to reset database via Flask...")
        from app import create_app, db
        app = create_app()
        with app.app_context():
            db.drop_all()
            print("[OK] Dropped all database tables via Flask")
    except Exception as e:
        print(f"[INFO] Could not reset via Flask (this is OK if database doesn't exist): {e}")
    
    # Find and delete database files
    db_files_deleted = []
    
    # Check for SQLite database files in common locations
    db_locations = [
        project_root / 'mlstudio.db',
        instance_dir / 'mlstudio.db',
    ]
    
    # Search for all .db files in project root
    if project_root.exists():
        for db_file in project_root.glob('*.db'):
            db_files_deleted.append(str(db_file))
            try:
                db_file.unlink()
                print(f"[OK] Deleted database file: {db_file.name}")
            except Exception as e:
                print(f"[ERROR] Error deleting {db_file.name}: {e}")
    
    # Search for all .db files in instance directory
    if instance_dir.exists():
        for db_file in instance_dir.glob('*.db'):
            if str(db_file) not in db_files_deleted:
                db_files_deleted.append(str(db_file))
                try:
                    db_file.unlink()
                    print(f"[OK] Deleted database file: {instance_dir.name}/{db_file.name}")
                except Exception as e:
                    print(f"[ERROR] Error deleting {db_file.name}: {e}")
    
    # Also check for database files with .sqlite extension
    if project_root.exists():
        for db_file in project_root.glob('*.sqlite'):
            db_files_deleted.append(str(db_file))
            try:
                db_file.unlink()
                print(f"[OK] Deleted SQLite file: {db_file.name}")
            except Exception as e:
                print(f"[ERROR] Error deleting {db_file.name}: {e}")
    
    if instance_dir.exists():
        for db_file in instance_dir.glob('*.sqlite'):
            if str(db_file) not in db_files_deleted:
                db_files_deleted.append(str(db_file))
                try:
                    db_file.unlink()
                    print(f"[OK] Deleted SQLite file: {instance_dir.name}/{db_file.name}")
                except Exception as e:
                    print(f"[ERROR] Error deleting {db_file.name}: {e}")
    
    # Delete setup completion marker (if exists)
    setup_complete_file = instance_dir / 'setup_complete.json'
    if setup_complete_file.exists():
        try:
            setup_complete_file.unlink()
            print(f"[OK] Deleted setup completion marker: {setup_complete_file}")
        except Exception as e:
            print(f"[ERROR] Error deleting setup marker: {e}")
    else:
        print(f"[INFO] Setup completion marker not found (already reset or never completed)")
    
    # Delete SQLite journal files if they exist
    journal_files = []
    if project_root.exists():
        for journal_file in project_root.glob('*.db-journal'):
            journal_files.append(str(journal_file))
            try:
                journal_file.unlink()
                print(f"[OK] Deleted journal file: {journal_file.name}")
            except Exception as e:
                print(f"[ERROR] Error deleting journal file {journal_file.name}: {e}")
    
    if instance_dir.exists():
        for journal_file in instance_dir.glob('*.db-journal'):
            if str(journal_file) not in journal_files:
                try:
                    journal_file.unlink()
                    print(f"[OK] Deleted journal file: {instance_dir.name}/{journal_file.name}")
                except Exception as e:
                    print(f"[ERROR] Error deleting journal file {journal_file.name}: {e}")
    
    print()
    print("=" * 60)
    
    # Check if there were any locked files
    locked_files = [f for f in db_files_deleted if "being used by another process" in str(f)]
    
    if db_files_deleted:
        if locked_files:
            print(f"[WARN] Found database file(s) but could not delete (application is running):")
            for f in locked_files:
                print(f"       - {f}")
            print()
            print("[INFO] Please STOP the Flask application first, then run this script again.")
            print("       Or manually delete the database file after stopping the app.")
        else:
            print(f"[OK] Database reset complete! Deleted {len(db_files_deleted)} database file(s).")
            print()
            print("You can now:")
            print("  1. Restart the application")
            print("  2. Go through the setup wizard again")
    else:
        print("[OK] Database reset complete! No database files found.")
        print()
        print("You can now:")
        print("  1. Restart the application")
        print("  2. Go through the setup wizard again")
    
    print("=" * 60)

if __name__ == '__main__':
    try:
        reset_database()
    except KeyboardInterrupt:
        print("\n\n[WARN] Reset cancelled by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n\n[ERROR] Error during reset: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
