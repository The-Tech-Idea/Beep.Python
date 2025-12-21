"""
Migration: Add competition_id column to ml_projects table

This migration adds a competition_id field to link projects to Community competitions.
"""
import sqlite3
import os
from pathlib import Path

def migrate():
    """Add competition_id column to ml_projects table"""
    # Get database path
    db_path = Path(__file__).parent.parent / 'mlstudio.db'
    
    if not db_path.exists():
        print(f"Database not found at {db_path}")
        return False
    
    try:
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # Check if column already exists
        cursor.execute("PRAGMA table_info(ml_projects)")
        columns = [row[1] for row in cursor.fetchall()]
        
        if 'competition_id' not in columns:
            print("Adding competition_id column...")
            cursor.execute("ALTER TABLE ml_projects ADD COLUMN competition_id INTEGER")
            print("✓ Added competition_id column")
        else:
            print("competition_id column already exists")
        
        conn.commit()
        conn.close()
        
        print("Migration completed successfully!")
        return True
        
    except Exception as e:
        print(f"Migration failed: {e}")
        return False

if __name__ == '__main__':
    migrate()
