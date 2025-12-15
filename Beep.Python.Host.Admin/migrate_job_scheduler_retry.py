"""
Database Migration: Add Retry/Failover Support to Job Scheduler

Adds new columns to scheduled_jobs table for retry and failover functionality.
This migration is safe and preserves existing data.
"""
import os
import sys
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent))

from app.database import db
from app import create_app
from sqlalchemy import text

def migrate():
    """Add retry/failover columns to scheduled_jobs table"""
    app = create_app()
    
    with app.app_context():
        print("=" * 70)
        print("Job Scheduler Retry/Failover Migration")
        print("=" * 70)
        
        try:
            # Check if table exists
            inspector = db.inspect(db.engine)
            tables = inspector.get_table_names()
            
            if 'scheduled_jobs' not in tables:
                print("\n⚠️  scheduled_jobs table does not exist.")
                print("Creating all tables...")
                db.create_all()
                print("✓ Tables created. Migration complete.")
                return True
            
            print("\n[1/6] Checking existing columns...")
            print("-" * 70)
            
            # Get existing columns
            columns = {col['name']: col for col in inspector.get_columns('scheduled_jobs')}
            existing_cols = set(columns.keys())
            
            print(f"Found {len(existing_cols)} existing columns")
            
            # Columns to add
            columns_to_add = {
                'retry_enabled': ('BOOLEAN', 'DEFAULT 0'),
                'max_retries': ('INTEGER', 'DEFAULT 3'),
                'retry_delay_seconds': ('INTEGER', 'DEFAULT 60'),
                'retry_backoff': ('BOOLEAN', 'DEFAULT 1'),
                'failover_enabled': ('BOOLEAN', 'DEFAULT 0'),
                'retry_count': ('INTEGER', 'DEFAULT 0'),
                'is_running': ('BOOLEAN', 'DEFAULT 0'),
                'current_execution_id': ('INTEGER', 'NULL')
            }
            
            # Check which columns need to be added
            missing_cols = {col: defn for col, defn in columns_to_add.items() 
                          if col not in existing_cols}
            
            if not missing_cols:
                print("\n✓ All columns already exist. No migration needed.")
                return True
            
            print(f"\n[2/6] Adding {len(missing_cols)} new columns...")
            print("-" * 70)
            
            # Determine database type
            db_url = str(db.engine.url)
            is_sqlite = 'sqlite' in db_url.lower()
            
            for col_name, (col_type, default) in missing_cols.items():
                try:
                    if is_sqlite:
                        # SQLite doesn't support ALTER TABLE ADD COLUMN with DEFAULT easily
                        # So we add without default, then update existing rows
                        sql = f"ALTER TABLE scheduled_jobs ADD COLUMN {col_name} {col_type}"
                        db.session.execute(text(sql))
                        db.session.commit()
                        
                        # Set default values for existing rows
                        if default and 'DEFAULT' in default:
                            default_value = default.split('DEFAULT')[1].strip()
                            if col_type == 'BOOLEAN':
                                # SQLite uses 0/1 for boolean
                                default_value = '1' if default_value == '1' else '0'
                            elif col_type == 'INTEGER':
                                default_value = default_value
                            
                            update_sql = f"UPDATE scheduled_jobs SET {col_name} = {default_value} WHERE {col_name} IS NULL"
                            db.session.execute(text(update_sql))
                            db.session.commit()
                    else:
                        # PostgreSQL, MySQL, etc.
                        sql = f"ALTER TABLE scheduled_jobs ADD COLUMN {col_name} {col_type} {default}"
                        db.session.execute(text(sql))
                        db.session.commit()
                    
                    print(f"  ✓ Added column: {col_name}")
                except Exception as e:
                    print(f"  ✗ Failed to add column {col_name}: {e}")
                    db.session.rollback()
                    # Check if column was added anyway (might have failed due to already existing)
                    inspector = db.inspect(db.engine)
                    new_columns = {col['name'] for col in inspector.get_columns('scheduled_jobs')}
                    if col_name in new_columns:
                        print(f"    (Column exists, continuing...)")
                    else:
                        return False
            
            print("\n[3/6] Adding foreign key constraint for current_execution_id...")
            print("-" * 70)
            
            # Check if foreign key already exists
            fks = inspector.get_foreign_keys('scheduled_jobs')
            has_fk = any(fk['constrained_columns'] == ['current_execution_id'] 
                        for fk in fks)
            
            if not has_fk and 'current_execution_id' in missing_cols:
                try:
                    if not is_sqlite:
                        # SQLite doesn't support adding foreign keys via ALTER TABLE easily
                        fk_sql = """
                        ALTER TABLE scheduled_jobs 
                        ADD CONSTRAINT fk_scheduled_jobs_current_execution 
                        FOREIGN KEY (current_execution_id) 
                        REFERENCES job_executions(id)
                        """
                        db.session.execute(text(fk_sql))
                        db.session.commit()
                        print("  ✓ Added foreign key constraint")
                    else:
                        print("  ⚠️  SQLite: Foreign key constraint will be enforced by application")
                except Exception as e:
                    print(f"  ⚠️  Could not add foreign key: {e}")
                    print("    (This is usually safe - constraint may already exist)")
                    db.session.rollback()
            
            print("\n[4/6] Verifying migration...")
            print("-" * 70)
            
            # Verify all columns exist
            inspector = db.inspect(db.engine)
            final_columns = {col['name'] for col in inspector.get_columns('scheduled_jobs')}
            
            all_added = all(col in final_columns for col in columns_to_add.keys())
            
            if all_added:
                print("✓ All columns verified")
            else:
                missing = set(columns_to_add.keys()) - final_columns
                print(f"⚠️  Some columns missing: {missing}")
                return False
            
            print("\n[5/6] Setting default values for existing jobs...")
            print("-" * 70)
            
            # Ensure all existing jobs have proper defaults
            try:
                update_sql = """
                UPDATE scheduled_jobs 
                SET retry_enabled = 0,
                    max_retries = 3,
                    retry_delay_seconds = 60,
                    retry_backoff = 1,
                    failover_enabled = 0,
                    retry_count = 0,
                    is_running = 0
                WHERE retry_enabled IS NULL
                """
                result = db.session.execute(text(update_sql))
                db.session.commit()
                print(f"✓ Updated {result.rowcount} existing jobs with defaults")
            except Exception as e:
                print(f"⚠️  Could not update defaults: {e}")
                db.session.rollback()
            
            print("\n[6/6] Migration Summary")
            print("-" * 70)
            print(f"✓ Added {len(missing_cols)} columns to scheduled_jobs table:")
            for col in missing_cols.keys():
                print(f"  - {col}")
            
            print("\n" + "=" * 70)
            print("Migration Complete!")
            print("=" * 70)
            print("\nNew columns added:")
            print("  - retry_enabled: Enable/disable retry on failure")
            print("  - max_retries: Maximum number of retry attempts")
            print("  - retry_delay_seconds: Delay between retries")
            print("  - retry_backoff: Use exponential backoff")
            print("  - failover_enabled: Enable failover (future feature)")
            print("  - retry_count: Current retry attempt counter")
            print("  - is_running: Job execution status")
            print("  - current_execution_id: Link to current execution record")
            print()
            
            return True
            
        except Exception as e:
            print(f"\n✗ Migration failed: {e}")
            import traceback
            traceback.print_exc()
            db.session.rollback()
            return False


if __name__ == '__main__':
    success = migrate()
    sys.exit(0 if success else 1)
