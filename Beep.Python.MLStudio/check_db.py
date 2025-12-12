import sqlite3
from pathlib import Path

db_path = Path('instance/mlstudio.db')
if db_path.exists():
    conn = sqlite3.connect(str(db_path))
    cursor = conn.cursor()
    cursor.execute('SELECT name FROM sqlite_master WHERE type="table"')
    tables = [row[0] for row in cursor.fetchall()]
    print('Tables:', tables)
    
    if 'ml_projects' in tables:
        cursor.execute('SELECT COUNT(*) FROM ml_projects')
        count = cursor.fetchone()[0]
        print(f'Projects count: {count}')
    
    if 'experiments' in tables:
        cursor.execute('PRAGMA table_info(experiments)')
        cols = [row[1] for row in cursor.fetchall()]
        print('Experiments columns:', cols)
    
    conn.close()
    print('Database is ready!')
else:
    print('Database does not exist!')

