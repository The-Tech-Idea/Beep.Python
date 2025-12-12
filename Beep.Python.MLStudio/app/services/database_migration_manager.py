"""
Database Migration Manager for Beep ML Studio
Handles automatic database schema upgrades when new modules are added.
By TheTechIdea
"""
import os
import sqlite3
import logging
from pathlib import Path
from typing import Dict, List, Optional, Tuple
from datetime import datetime

logger = logging.getLogger(__name__)


class MigrationInfo:
    """Information about a migration"""
    def __init__(self, version: str, name: str, applied: bool = False, applied_at: datetime = None):
        self.version = version
        self.name = name
        self.applied = applied
        self.applied_at = applied_at
    
    def to_dict(self) -> Dict:
        return {
            'version': self.version,
            'name': self.name,
            'applied': self.applied,
            'applied_at': self.applied_at.isoformat() if self.applied_at else None
        }


class DatabaseMigrationManager:
    """
    Manages database schema migrations for MLStudio.
    
    Features:
    - Automatic detection of required schema changes
    - Version tracking for applied migrations
    - Safe rollback support
    - Module-specific migrations
    """
    
    # Migration tracking table
    MIGRATIONS_TABLE = '_schema_migrations'
    
    def __init__(self, db_path: Path = None):
        """Initialize the migration manager"""
        if db_path is None:
            # Try to get from Flask app context first
            db_path = self._get_db_path_from_flask()
        if db_path is None:
            # Fallback to default location in project root
            db_path = Path(__file__).parent.parent.parent / 'mlstudio.db'
        self.db_path = Path(db_path)
        self._ensure_migrations_table()
    
    @staticmethod
    def _get_db_path_from_flask() -> Optional[Path]:
        """Get database path from Flask app configuration"""
        try:
            from flask import current_app
            db_uri = current_app.config.get('SQLALCHEMY_DATABASE_URI', '')
            if db_uri.startswith('sqlite:///'):
                return Path(db_uri.replace('sqlite:///', ''))
        except RuntimeError:
            # No Flask app context available
            pass
        return None
    
    def _get_connection(self) -> sqlite3.Connection:
        """Get a database connection"""
        return sqlite3.connect(str(self.db_path))
    
    def _ensure_migrations_table(self):
        """Ensure the migrations tracking table exists"""
        if not self.db_path.exists():
            logger.warning(f"Database not found at {self.db_path}")
            return
        
        conn = self._get_connection()
        cursor = conn.cursor()
        
        cursor.execute(f'''
            CREATE TABLE IF NOT EXISTS {self.MIGRATIONS_TABLE} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                version VARCHAR(50) NOT NULL UNIQUE,
                name VARCHAR(200) NOT NULL,
                module VARCHAR(100),
                applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                checksum VARCHAR(64)
            )
        ''')
        conn.commit()
        conn.close()
    
    def get_applied_migrations(self) -> List[MigrationInfo]:
        """Get list of applied migrations"""
        if not self.db_path.exists():
            return []
        
        conn = self._get_connection()
        cursor = conn.cursor()
        
        cursor.execute(f'''
            SELECT version, name, applied_at 
            FROM {self.MIGRATIONS_TABLE} 
            ORDER BY version
        ''')
        
        migrations = []
        for row in cursor.fetchall():
            migrations.append(MigrationInfo(
                version=row[0],
                name=row[1],
                applied=True,
                applied_at=datetime.fromisoformat(row[2]) if row[2] else None
            ))
        
        conn.close()
        return migrations
    
    def is_migration_applied(self, version: str) -> bool:
        """Check if a specific migration has been applied"""
        if not self.db_path.exists():
            return False
        
        conn = self._get_connection()
        cursor = conn.cursor()
        
        cursor.execute(f'''
            SELECT 1 FROM {self.MIGRATIONS_TABLE} WHERE version = ?
        ''', (version,))
        
        result = cursor.fetchone() is not None
        conn.close()
        return result
    
    def record_migration(self, version: str, name: str, module: str = None, checksum: str = None):
        """Record that a migration has been applied"""
        conn = self._get_connection()
        cursor = conn.cursor()
        
        cursor.execute(f'''
            INSERT OR REPLACE INTO {self.MIGRATIONS_TABLE} 
            (version, name, module, checksum, applied_at)
            VALUES (?, ?, ?, ?, CURRENT_TIMESTAMP)
        ''', (version, name, module, checksum))
        
        conn.commit()
        conn.close()
        logger.info(f"Recorded migration: {version} - {name}")
    
    def get_table_columns(self, table_name: str) -> List[Dict]:
        """Get columns for a specific table"""
        if not self.db_path.exists():
            return []
        
        conn = self._get_connection()
        cursor = conn.cursor()
        
        cursor.execute(f"PRAGMA table_info({table_name})")
        columns = []
        for row in cursor.fetchall():
            columns.append({
                'cid': row[0],
                'name': row[1],
                'type': row[2],
                'notnull': row[3],
                'default': row[4],
                'pk': row[5]
            })
        
        conn.close()
        return columns
    
    def table_exists(self, table_name: str) -> bool:
        """Check if a table exists"""
        if not self.db_path.exists():
            return False
        
        conn = self._get_connection()
        cursor = conn.cursor()
        
        cursor.execute('''
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name=?
        ''', (table_name,))
        
        result = cursor.fetchone() is not None
        conn.close()
        return result
    
    def column_exists(self, table_name: str, column_name: str) -> bool:
        """Check if a column exists in a table"""
        columns = self.get_table_columns(table_name)
        return any(col['name'] == column_name for col in columns)
    
    def add_column(self, table_name: str, column_name: str, column_type: str, 
                   nullable: bool = True, default: any = None) -> bool:
        """Add a column to a table if it doesn't exist"""
        if self.column_exists(table_name, column_name):
            logger.info(f"Column {column_name} already exists in {table_name}")
            return False
        
        conn = self._get_connection()
        cursor = conn.cursor()
        
        sql = f"ALTER TABLE {table_name} ADD COLUMN {column_name} {column_type}"
        if default is not None:
            if isinstance(default, str):
                sql += f" DEFAULT '{default}'"
            else:
                sql += f" DEFAULT {default}"
        
        cursor.execute(sql)
        conn.commit()
        conn.close()
        
        logger.info(f"Added column {column_name} to {table_name}")
        return True
    
    def create_table(self, table_name: str, columns: Dict[str, str], 
                     primary_key: str = 'id', foreign_keys: List[Tuple[str, str, str]] = None) -> bool:
        """
        Create a table if it doesn't exist
        
        Args:
            table_name: Name of the table
            columns: Dict of column_name -> column_definition
            primary_key: Primary key column name
            foreign_keys: List of (column, ref_table, ref_column) tuples
        """
        if self.table_exists(table_name):
            logger.info(f"Table {table_name} already exists")
            return False
        
        conn = self._get_connection()
        cursor = conn.cursor()
        
        # Build column definitions
        col_defs = []
        for col_name, col_def in columns.items():
            if col_name == primary_key:
                # SQLite requires: INTEGER PRIMARY KEY AUTOINCREMENT (not INTEGER AUTOINCREMENT PRIMARY KEY)
                if 'AUTOINCREMENT' in col_def.upper():
                    col_defs.append(f"{col_name} INTEGER PRIMARY KEY AUTOINCREMENT")
                else:
                    col_defs.append(f"{col_name} {col_def} PRIMARY KEY")
            else:
                col_defs.append(f"{col_name} {col_def}")
        
        # Add foreign keys
        if foreign_keys:
            for col, ref_table, ref_col in foreign_keys:
                col_defs.append(f"FOREIGN KEY ({col}) REFERENCES {ref_table}({ref_col})")
        
        sql = f"CREATE TABLE {table_name} ({', '.join(col_defs)})"
        try:
            cursor.execute(sql)
            conn.commit()
            logger.info(f"Created table {table_name}")
            return True
        except Exception as e:
            logger.error(f"Failed to create table {table_name}: {e}")
            return False
        finally:
            conn.close()
    
    def run_sql(self, sql: str, params: tuple = None) -> bool:
        """Execute arbitrary SQL"""
        conn = self._get_connection()
        cursor = conn.cursor()
        
        try:
            if params:
                cursor.execute(sql, params)
            else:
                cursor.execute(sql)
            conn.commit()
            return True
        except Exception as e:
            logger.error(f"SQL error: {e}")
            return False
        finally:
            conn.close()
    
    def check_and_apply_migrations(self) -> List[str]:
        """
        Check for pending migrations and apply them.
        Returns list of applied migration names.
        """
        applied = []
        
        # Core migrations
        applied.extend(self._apply_core_migrations())
        
        # Module-specific migrations
        applied.extend(self._apply_industry_migrations())
        
        return applied
    
    def _apply_core_migrations(self) -> List[str]:
        """Apply core schema migrations"""
        applied = []
        
        # Migration 001: Add stdout/stderr to experiments
        if not self.is_migration_applied('001'):
            if self.table_exists('experiments'):
                self.add_column('experiments', 'stdout', 'TEXT')
                self.add_column('experiments', 'stderr', 'TEXT')
            self.record_migration('001', 'add_experiment_stdout_stderr', 'core')
            applied.append('001_add_experiment_stdout_stderr')
        
        # Migration 002: Ensure workflows table exists
        if not self.is_migration_applied('002'):
            if not self.table_exists('workflows'):
                self.create_table('workflows', {
                    'id': 'INTEGER AUTOINCREMENT',
                    'project_id': 'INTEGER NOT NULL',
                    'name': 'VARCHAR(200) NOT NULL',
                    'description': 'TEXT',
                    'workflow_data': 'TEXT',
                    'generated_code': 'TEXT',
                    'status': "VARCHAR(50) DEFAULT 'draft'",
                    'created_at': 'TIMESTAMP DEFAULT CURRENT_TIMESTAMP',
                    'updated_at': 'TIMESTAMP DEFAULT CURRENT_TIMESTAMP',
                    'last_executed_at': 'TIMESTAMP'
                }, foreign_keys=[('project_id', 'ml_projects', 'id')])
            self.record_migration('002', 'ensure_workflows_table', 'core')
            applied.append('002_ensure_workflows_table')
        
        # Migration 003: Ensure settings table exists
        if not self.is_migration_applied('003'):
            if not self.table_exists('settings'):
                self.create_table('settings', {
                    'id': 'INTEGER AUTOINCREMENT',
                    'key': 'VARCHAR(200) NOT NULL UNIQUE',
                    'category': 'VARCHAR(100) NOT NULL',
                    'value': 'TEXT',
                    'value_type': "VARCHAR(50) DEFAULT 'string'",
                    'description': 'TEXT',
                    'is_encrypted': 'BOOLEAN DEFAULT 0',
                    'created_at': 'TIMESTAMP DEFAULT CURRENT_TIMESTAMP',
                    'updated_at': 'TIMESTAMP DEFAULT CURRENT_TIMESTAMP'
                })
            self.record_migration('003', 'ensure_settings_table', 'core')
            applied.append('003_ensure_settings_table')
        
        return applied
    
    def _apply_industry_migrations(self) -> List[str]:
        """Apply industry module migrations"""
        applied = []
        
        # Migration 100: Add industry profile fields to ml_projects
        if not self.is_migration_applied('100'):
            if self.table_exists('ml_projects'):
                self.add_column('ml_projects', 'industry_profile', 'VARCHAR(100)')
                self.add_column('ml_projects', 'scenario_id', 'VARCHAR(100)')
                self.add_column('ml_projects', 'industry_config', 'TEXT')
            self.record_migration('100', 'add_industry_profile_to_projects', 'industry')
            applied.append('100_add_industry_profile_to_projects')
        
        # Migration 101: Create industry_scenarios table for tracking scenario progress
        if not self.is_migration_applied('101'):
            self.create_table('industry_scenario_progress', {
                'id': 'INTEGER AUTOINCREMENT',
                'project_id': 'INTEGER NOT NULL',
                'scenario_id': 'VARCHAR(100) NOT NULL',
                'current_step': 'INTEGER DEFAULT 1',
                'completed_steps': 'TEXT',  # JSON array of completed step numbers
                'step_data': 'TEXT',  # JSON object with data from each step
                'status': "VARCHAR(50) DEFAULT 'in_progress'",  # in_progress, completed, abandoned
                'created_at': 'TIMESTAMP DEFAULT CURRENT_TIMESTAMP',
                'updated_at': 'TIMESTAMP DEFAULT CURRENT_TIMESTAMP'
            }, foreign_keys=[('project_id', 'ml_projects', 'id')])
            self.record_migration('101', 'create_industry_scenario_progress_table', 'industry')
            applied.append('101_create_industry_scenario_progress_table')
        
        return applied
    
    def get_schema_status(self) -> Dict:
        """Get current schema status information"""
        tables = []
        if self.db_path.exists():
            conn = self._get_connection()
            cursor = conn.cursor()
            cursor.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
            tables = [row[0] for row in cursor.fetchall()]
            conn.close()
        
        return {
            'database_path': str(self.db_path),
            'database_exists': self.db_path.exists(),
            'tables': tables,
            'applied_migrations': [m.to_dict() for m in self.get_applied_migrations()]
        }


# Singleton instance
_migration_manager = None


def get_migration_manager(db_path: Path = None) -> DatabaseMigrationManager:
    """Get or create the migration manager singleton"""
    global _migration_manager
    if _migration_manager is None or db_path is not None:
        _migration_manager = DatabaseMigrationManager(db_path)
    return _migration_manager


def run_migrations_on_startup():
    """
    Run pending migrations on application startup.
    Call this from create_app() after db.init_app(app).
    """
    try:
        mgr = get_migration_manager()
        applied = mgr.check_and_apply_migrations()
        if applied:
            logger.info(f"Applied {len(applied)} migrations: {applied}")
        else:
            logger.debug("No pending migrations")
    except Exception as e:
        logger.error(f"Migration error: {e}")

