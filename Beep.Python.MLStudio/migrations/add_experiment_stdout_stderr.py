"""
Migration: Add stdout and stderr columns to experiments table
"""
import sqlite3
import os
from pathlib import Path

def migrate():
    """Add stdout and stderr columns to experiments table"""
    # Get database path
    db_path = Path(__file__).parent.parent / 'mlstudio.db'
    
    if not db_path.exists():
        print(f"Database not found at {db_path}")
        return False
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # Check if columns already exist
        cursor.execute("PRAGMA table_info(experiments)")
        columns = [row[1] for row in cursor.fetchall()]
        
        if 'stdout' not in columns:
            print("Adding stdout column...")
            cursor.execute("ALTER TABLE experiments ADD COLUMN stdout TEXT")
            print("✓ Added stdout column")
        else:
            print("stdout column already exists")
        
        if 'stderr' not in columns:
            print("Adding stderr column...")
            cursor.execute("ALTER TABLE experiments ADD COLUMN stderr TEXT")
            print("✓ Added stderr column")
        else:
            print("stderr column already exists")
        
        conn.commit()
        conn.close()
        
        print("Migration completed successfully!")
        return True
        
    except Exception as e:
        print(f"Migration failed: {e}")
        return False

if __name__ == '__main__':
    migrate()

