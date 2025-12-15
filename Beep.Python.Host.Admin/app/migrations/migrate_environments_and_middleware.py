"""
Migration Script: Move Virtual Environments to Providers & Create Middleware Tables

This migration:
1. Updates old environment paths in settings to point to providers directory
2. Creates database tables for RoutingRule and AccessPolicy
3. Migrates existing rules/policies from Setting JSON to new tables
4. Cleans up old environment directories (optional, safe to delete)
"""
import sys
import json
from pathlib import Path
from datetime import datetime

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent.parent))

from app.database import db
from app.models.core import Setting
from flask import Flask
from app import create_app


def migrate_environment_paths():
    """Update environment paths in settings to use providers directory"""
    print("Migrating environment paths to providers directory...")
    
    from app.config_manager import get_app_directory
    app_dir = get_app_directory()
    providers_path = app_dir / 'providers'
    
    # Environment path mappings
    env_mappings = {
        'doc_extraction_env_path': providers_path / 'document_extraction',
        'job_scheduler_env_path': providers_path / 'job_scheduler',
    }
    
    # AI Services paths
    ai_service_types = ['text_to_image', 'text_to_speech', 'speech_to_text', 'voice_to_voice', 
                       'object_detection', 'tabular_time_series']
    for service_type in ai_service_types:
        key = f'ai_service_{service_type}_env_path'
        env_mappings[key] = providers_path / f'ai_services_{service_type}'
    
    updated_count = 0
    for key, new_path in env_mappings.items():
        old_path = Setting.get(key, '')
        if old_path:
            # Check if old path exists and is different from new path
            old_path_obj = Path(old_path)
            new_path_str = str(new_path)
            
            if old_path != new_path_str:
                # Update setting to new path
                Setting.set(key, new_path_str, f'{key.replace("_", " ").title()} virtual environment path')
                updated_count += 1
                print(f"  Updated {key}: {old_path} -> {new_path_str}")
                
                # Check if old directory exists and warn
                if old_path_obj.exists() and old_path_obj.is_dir():
                    print(f"    WARNING: Old directory still exists: {old_path}")
                    print(f"    You can safely delete it after verifying the new environment works.")
    
    print(f"Updated {updated_count} environment paths.")
    return updated_count


def create_middleware_tables():
    """Create database tables for RoutingRule and AccessPolicy"""
    print("\nCreating middleware database tables...")
    
    # Check if tables already exist
    from sqlalchemy import inspect, text
    inspector = inspect(db.engine)
    existing_tables = inspector.get_table_names()
    
    if 'middleware_routing_rules' in existing_tables and 'middleware_access_policies' in existing_tables:
        print("  Middleware tables already exist. Skipping creation.")
        return True
    
    # Create tables
    try:
        # Import models (they should define the tables)
        from app.models.middleware import RoutingRule, AccessPolicy
        
        # Create all tables
        db.create_all()
        print("  Created middleware_routing_rules and middleware_access_policies tables.")
        return True
    except Exception as e:
        print(f"  Error creating tables via models: {e}")
        # If models don't exist yet, create tables manually
        try:
            # Create routing_rules table
            db.session.execute(text("""
                CREATE TABLE IF NOT EXISTS middleware_routing_rules (
                    id VARCHAR(255) PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    rule_type VARCHAR(50) NOT NULL,
                    pattern TEXT NOT NULL,
                    action VARCHAR(50) NOT NULL,
                    target_service VARCHAR(100),
                    target_api VARCHAR(255),
                    priority INTEGER DEFAULT 0,
                    enabled BOOLEAN DEFAULT TRUE,
                    conditions TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )
            """))
            
            # Create access_policies table
            db.session.execute(text("""
                CREATE TABLE IF NOT EXISTS middleware_access_policies (
                    id VARCHAR(255) PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    description TEXT,
                    service VARCHAR(100) NOT NULL,
                    resource VARCHAR(255),
                    user_id VARCHAR(255),
                    user_role VARCHAR(50),
                    allowed BOOLEAN DEFAULT TRUE,
                    conditions TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )
            """))
            
            db.session.commit()
            print("  Created middleware tables manually.")
            return True
        except Exception as e2:
            print(f"  Error creating tables manually: {e2}")
            db.session.rollback()
            return False


def migrate_rules_and_policies():
    """Migrate rules and policies from Setting JSON to database tables"""
    print("\nMigrating rules and policies from Settings to database tables...")
    
        try:
            # Check if tables exist
            from sqlalchemy import inspect, text
            inspector = inspect(db.engine)
            existing_tables = inspector.get_table_names()
            
            if 'middleware_routing_rules' not in existing_tables or 'middleware_access_policies' not in existing_tables:
                print("  Middleware tables don't exist. Run create_middleware_tables() first.")
                return 0, 0
            
            # Use text() for SQL execution
            from sqlalchemy import text
        
        # Load rules from Settings
        rules_json = Setting.get('middleware_routing_rules', '[]')
        rules_data = json.loads(rules_json) if rules_json else []
        
        # Load policies from Settings
        policies_json = Setting.get('middleware_access_policies', '[]')
        policies_data = json.loads(policies_json) if policies_json else []
        
        rules_migrated = 0
        policies_migrated = 0
        
        # Migrate rules
        if rules_data:
            print(f"  Found {len(rules_data)} rules to migrate...")
            for rule in rules_data:
                try:
                    # Check if rule already exists
                    result = db.session.execute(text(
                        "SELECT id FROM middleware_routing_rules WHERE id = :id"
                    ), {"id": rule.get('id')})
                    
                    if result.fetchone():
                        print(f"    Rule {rule.get('id')} already exists, skipping...")
                        continue
                    
                    # Insert rule
                    conditions_json = json.dumps(rule.get('conditions', {}))
                    db.session.execute(text("""
                        INSERT INTO middleware_routing_rules 
                        (id, name, description, rule_type, pattern, action, target_service, 
                         target_api, priority, enabled, conditions, created_at, updated_at)
                        VALUES (:id, :name, :description, :rule_type, :pattern, :action, 
                                :target_service, :target_api, :priority, :enabled, :conditions,
                                :created_at, :updated_at)
                    """), {
                        "id": rule.get('id'),
                        "name": rule.get('name', ''),
                        "description": rule.get('description'),
                        "rule_type": rule.get('rule_type', 'keyword'),
                        "pattern": rule.get('pattern', ''),
                        "action": rule.get('action', 'route'),
                        "target_service": rule.get('target_service'),
                        "target_api": rule.get('target_api'),
                        "priority": rule.get('priority', 0),
                        "enabled": rule.get('enabled', True),
                        "conditions": conditions_json,
                        "created_at": rule.get('created_at', datetime.now().isoformat()),
                        "updated_at": rule.get('updated_at', datetime.now().isoformat())
                    })
                    rules_migrated += 1
                except Exception as e:
                    print(f"    Error migrating rule {rule.get('id')}: {e}")
        
        # Migrate policies
        if policies_data:
            print(f"  Found {len(policies_data)} policies to migrate...")
            for policy in policies_data:
                try:
                    # Check if policy already exists
                    result = db.session.execute(text(
                        "SELECT id FROM middleware_access_policies WHERE id = :id"
                    ), {"id": policy.get('id')})
                    
                    if result.fetchone():
                        print(f"    Policy {policy.get('id')} already exists, skipping...")
                        continue
                    
                    # Insert policy
                    conditions_json = json.dumps(policy.get('conditions', {}))
                    db.session.execute(text("""
                        INSERT INTO middleware_access_policies 
                        (id, name, description, service, resource, user_id, user_role, 
                         allowed, conditions, created_at, updated_at)
                        VALUES (:id, :name, :description, :service, :resource, :user_id, 
                                :user_role, :allowed, :conditions, :created_at, :updated_at)
                    """), {
                        "id": policy.get('id'),
                        "name": policy.get('name', ''),
                        "description": policy.get('description'),
                        "service": policy.get('service', ''),
                        "resource": policy.get('resource'),
                        "user_id": policy.get('user_id'),
                        "user_role": policy.get('user_role'),
                        "allowed": policy.get('allowed', True),
                        "conditions": conditions_json,
                        "created_at": policy.get('created_at', datetime.now().isoformat()),
                        "updated_at": policy.get('updated_at', datetime.now().isoformat())
                    })
                    policies_migrated += 1
                except Exception as e:
                    print(f"    Error migrating policy {policy.get('id')}: {e}")
        
        db.session.commit()
        
        print(f"  Migrated {rules_migrated} rules and {policies_migrated} policies.")
        
        # Optionally clear old Setting entries (keep them as backup for now)
        # Setting.set('middleware_routing_rules', '', 'Migrated to middleware_routing_rules table')
        # Setting.set('middleware_access_policies', '', 'Migrated to middleware_access_policies table')
        
        return rules_migrated, policies_migrated
        
    except Exception as e:
        print(f"  Error migrating rules/policies: {e}")
        db.session.rollback()
        return 0, 0


def update_rag_provider_paths():
    """Update hardcoded RAG provider paths to use providers directory"""
    print("\nUpdating RAG provider paths...")
    print("  NOTE: This requires manual code updates in:")
    print("    - app/services/rag_providers/chromadb_subprocess.py")
    print("    - app/services/rag_providers/faiss_subprocess.py")
    print("    - app/services/rag_providers/chromadb_provider.py")
    print("    - app/services/rag_providers/faiss_provider.py")
    print("    - app/services/rag_providers/subprocess_executor.py")
    print("  These files still reference 'rag_data/venv' and should be updated to use providers/rag")


def main():
    """Run all migrations"""
    print("=" * 60)
    print("Migration: Virtual Environments & Middleware Database")
    print("=" * 60)
    
    app = create_app()
    with app.app_context():
        try:
            # Step 1: Migrate environment paths
            migrate_environment_paths()
            
            # Step 2: Create middleware tables
            tables_created = create_middleware_tables()
            
            # Step 3: Migrate rules and policies
            from sqlalchemy import inspect
            inspector = inspect(db.engine)
            existing_tables = inspector.get_table_names()
            
            if 'middleware_routing_rules' in existing_tables and 'middleware_access_policies' in existing_tables:
                rules_count, policies_count = migrate_rules_and_policies()
            else:
                print("\nSkipping data migration - tables not created.")
                rules_count, policies_count = 0, 0
            
            # Step 4: Warn about RAG provider paths
            update_rag_provider_paths()
            
            print("\n" + "=" * 60)
            print("Migration completed!")
            print("=" * 60)
            print("\nNext steps:")
            print("1. Verify new environments work correctly")
            print("2. Delete old environment directories if everything works:")
            print("   - {app_dir}/doc_extraction_env")
            print("   - {app_dir}/rag_env")
            print("   - {app_dir}/rag_data/venv")
            print("   - {app_dir}/data/ai_services/*/venv")
            print("3. Update RAG provider files to use providers/rag path")
            print("4. Test middleware rules and policies functionality")
            
        except Exception as e:
            print(f"\nERROR: Migration failed: {e}")
            import traceback
            traceback.print_exc()
            db.session.rollback()
            return 1
    
    return 0


if __name__ == '__main__':
    exit(main())
