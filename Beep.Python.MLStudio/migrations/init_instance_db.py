"""
Initialize database in instance folder
"""
import sqlite3
from pathlib import Path

def init_instance_db():
    """Create and initialize database in instance folder"""
    instance_folder = Path('instance')
    instance_folder.mkdir(exist_ok=True)
    
    db_path = instance_folder / 'mlstudio.db'
    
    if db_path.exists():
        print(f"Database already exists at {db_path}")
        # Just ensure it has the right schema
        conn = sqlite3.connect(str(db_path))
        cursor = conn.cursor()
        
        # Check if experiments table has stdout/stderr
        cursor.execute("PRAGMA table_info(experiments)")
        columns = [row[1] for row in cursor.fetchall()]
        
        if 'stdout' not in columns:
            print("Adding stdout column...")
            cursor.execute("ALTER TABLE experiments ADD COLUMN stdout TEXT")
        
        if 'stderr' not in columns:
            print("Adding stderr column...")
            cursor.execute("ALTER TABLE experiments ADD COLUMN stderr TEXT")
        
        conn.commit()
        conn.close()
        print("Database schema updated")
        return
    
    print(f"Creating new database at {db_path}")
    
    # Create database file
    conn = sqlite3.connect(str(db_path))
    cursor = conn.cursor()
    
    # Create tables (basic schema)
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS ml_projects (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name VARCHAR(200) NOT NULL UNIQUE,
            description TEXT,
            framework VARCHAR(50),
            status VARCHAR(50) DEFAULT 'active',
            environment_name VARCHAR(200),
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS experiments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            project_id INTEGER NOT NULL,
            name VARCHAR(200) NOT NULL,
            description TEXT,
            model_type VARCHAR(100),
            model_config TEXT,
            dataset_path VARCHAR(500),
            train_size REAL,
            metrics TEXT,
            model_path VARCHAR(500),
            status VARCHAR(50) DEFAULT 'pending',
            error_message TEXT,
            stdout TEXT,
            stderr TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            started_at DATETIME,
            completed_at DATETIME,
            FOREIGN KEY (project_id) REFERENCES ml_projects(id)
        )
    """)
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS workflows (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            project_id INTEGER NOT NULL,
            name VARCHAR(200) NOT NULL,
            description TEXT,
            workflow_data TEXT,
            generated_code TEXT,
            status VARCHAR(50) DEFAULT 'draft',
            last_executed_at DATETIME,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (project_id) REFERENCES ml_projects(id)
        )
    """)
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS settings (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            key VARCHAR(200) NOT NULL UNIQUE,
            category VARCHAR(100) NOT NULL,
            value TEXT,
            value_type VARCHAR(50) DEFAULT 'string',
            description TEXT,
            is_encrypted BOOLEAN DEFAULT 0,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
    conn.commit()
    conn.close()
    
    print(f"Database created successfully at {db_path}")

if __name__ == '__main__':
    init_instance_db()

