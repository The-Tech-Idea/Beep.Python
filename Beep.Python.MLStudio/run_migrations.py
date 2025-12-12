#!/usr/bin/env python
"""
Run Database Migrations
Utility script to manually run database migrations.

Usage:
    python run_migrations.py           # Run all pending migrations
    python run_migrations.py --status  # Show migration status only
    python run_migrations.py --check   # Check schema status
"""
import sys
import argparse
from pathlib import Path

# Add app to path
sys.path.insert(0, str(Path(__file__).parent))


def run_migrations():
    """Run pending migrations"""
    from app.services.database_migration_manager import get_migration_manager
    
    print("=" * 60)
    print("MLStudio Database Migration Manager")
    print("=" * 60)
    
    mgr = get_migration_manager()
    
    print(f"\nDatabase: {mgr.db_path}")
    print(f"Database exists: {mgr.db_path.exists()}")
    
    if not mgr.db_path.exists():
        print("\n[WARNING] Database file not found!")
        print("The database will be created when you run the application.")
        return
    
    print("\nRunning migrations...")
    applied = mgr.check_and_apply_migrations()
    
    if applied:
        print(f"\n[OK] Applied {len(applied)} migration(s):")
        for m in applied:
            print(f"   - {m}")
    else:
        print("\n[OK] No pending migrations - database is up to date!")
    
    print("\n" + "=" * 60)


def show_status():
    """Show migration status"""
    from app.services.database_migration_manager import get_migration_manager
    
    print("=" * 60)
    print("MLStudio Database Migration Status")
    print("=" * 60)
    
    mgr = get_migration_manager()
    status = mgr.get_schema_status()
    
    print(f"\nDatabase: {status['database_path']}")
    print(f"Exists: {status['database_exists']}")
    
    if status['database_exists']:
        print(f"\nTables ({len(status['tables'])}):")
        for table in status['tables']:
            print(f"   - {table}")
        
        print(f"\nApplied Migrations ({len(status['applied_migrations'])}):")
        if status['applied_migrations']:
            for m in status['applied_migrations']:
                applied_at = m['applied_at'][:19] if m['applied_at'] else 'Unknown'
                print(f"   [OK] {m['version']}: {m['name']} ({applied_at})")
        else:
            print("   (none)")
    
    print("\n" + "=" * 60)


def check_schema():
    """Check schema and show details"""
    from app.services.database_migration_manager import get_migration_manager
    
    print("=" * 60)
    print("MLStudio Database Schema Check")
    print("=" * 60)
    
    mgr = get_migration_manager()
    
    if not mgr.db_path.exists():
        print("\n[WARNING] Database file not found!")
        return
    
    # Check key tables
    key_tables = ['ml_projects', 'experiments', 'workflows', 'settings', 
                  'industry_scenario_progress', '_schema_migrations']
    
    print("\nTable Status:")
    for table in key_tables:
        exists = mgr.table_exists(table)
        status_icon = "[OK]" if exists else "[MISSING]"
        print(f"   {status_icon} {table}")
        
        if exists:
            columns = mgr.get_table_columns(table)
            for col in columns:
                print(f"      - {col['name']}: {col['type']}")
    
    print("\n" + "=" * 60)


def main():
    parser = argparse.ArgumentParser(description='MLStudio Database Migration Tool')
    parser.add_argument('--status', action='store_true', help='Show migration status only')
    parser.add_argument('--check', action='store_true', help='Check schema details')
    
    args = parser.parse_args()
    
    if args.status:
        show_status()
    elif args.check:
        check_schema()
    else:
        run_migrations()


if __name__ == '__main__':
    main()

