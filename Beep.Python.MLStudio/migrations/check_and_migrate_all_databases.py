"""
Check and migrate all mlstudio.db files
"""
import sqlite3
import os
from pathlib import Path

def check_and_migrate_db(db_path):
    """Check and migrate a database file"""
    if not os.path.exists(db_path):
        print(f"Database not found: {db_path}")
        return False
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Check if columns already exist
        cursor.execute("PRAGMA table_info(experiments)")
        columns = [row[1] for row in cursor.fetchall()]
        
        has_stdout = 'stdout' in columns
        has_stderr = 'stderr' in columns
        
        print(f"\n=== {db_path} ===")
        print(f"Has stdout: {has_stdout}")
        print(f"Has stderr: {has_stderr}")
        
        if not has_stdout:
            print("Adding stdout column...")
            cursor.execute("ALTER TABLE experiments ADD COLUMN stdout TEXT")
            print("✓ Added stdout column")
        
        if not has_stderr:
            print("Adding stderr column...")
            cursor.execute("ALTER TABLE experiments ADD COLUMN stderr TEXT")
            print("✓ Added stderr column")
        
        if has_stdout and has_stderr:
            print("✓ Database already has required columns")
        
        conn.commit()
        conn.close()
        
        return True
        
    except Exception as e:
        print(f"Error with {db_path}: {e}")
        return False

if __name__ == '__main__':
    # Find all mlstudio.db files
    project_root = Path(__file__).parent.parent
    
    db_files = [
        project_root / 'mlstudio.db',
        project_root / 'instance' / 'mlstudio.db'
    ]
    
    for db_file in db_files:
        if db_file.exists():
            check_and_migrate_db(str(db_file))
        else:
            print(f"\n=== {db_file} ===")
            print("File does not exist")

