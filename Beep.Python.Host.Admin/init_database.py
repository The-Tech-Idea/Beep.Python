"""
Unified Database Schema Creation Script

Creates all database tables for Beep.Python in one go:
- Core tables (User, Role, Group, Settings, AuditLog)
- RAG tables (Collections, Documents, AccessPrivileges)
- Association tables (user_groups)

Run this script to initialize the complete database schema.
"""
import os
import sys
from pathlib import Path

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from app.database import db
from app.models.core import User, Role, Group, Setting, AuditLog, user_groups
from app.models.rag_metadata import Collection, Document, AccessPrivilege
from app.services.auth_service import AuthService
from flask import Flask
from werkzeug.security import generate_password_hash


def create_app():
    """Create Flask app for database initialization"""
    app = Flask(__name__)
    
    # Load configuration
    app.config['SQLALCHEMY_DATABASE_URI'] = os.environ.get(
        'DATABASE_URL',
        'sqlite:///beep_python.db'
    )
    app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
    
    db.init_app(app)
    return app


def init_database():
    """Initialize complete database schema"""
    print("=" * 70)
    print("Beep.Python - Unified Database Initialization")
    print("=" * 70)
    
    app = create_app()
    
    with app.app_context():
        print("\n[1/5] Creating database tables...")
        print("-" * 70)
        
        try:
            # Drop all tables (WARNING: This deletes all data!)
            print("⚠️  Dropping existing tables...")
            db.drop_all()
            
            # Create all tables
            print("Creating tables...")
            db.create_all()
            
            # Verify tables created
            inspector = db.inspect(db.engine)
            tables = inspector.get_table_names()
            
            print(f"\n✓ Created {len(tables)} tables:")
            for table in sorted(tables):
                print(f"  - {table}")
            
        except Exception as e:
            print(f"\n✗ Error creating tables: {e}")
            return False
        
        print("\n[2/5] Creating default roles...")
        print("-" * 70)
        
        try:
            AuthService.create_default_roles()
            
            roles = Role.query.all()
            print(f"\n✓ Created {len(roles)} roles:")
            for role in roles:
                perms = role.get_permissions()
                print(f"  - {role.name}: {len(perms)} permissions")
            
        except Exception as e:
            print(f"\n✗ Error creating roles: {e}")
            db.session.rollback()
            return False
        
        print("\n[3/5] Creating default admin user...")
        print("-" * 70)
        
        try:
            # Check if admin exists
            admin = User.query.filter_by(username='admin').first()
            
            if not admin:
                admin_role = Role.query.filter_by(name='Admin').first()
                
                admin = User(
                    username='admin',
                    email='admin@beep-python.local',
                    display_name='Administrator',
                    is_admin=True,
                    is_active=True,
                    role_id=admin_role.id if admin_role else None,
                    password_hash=generate_password_hash('admin123')
                )
                
                db.session.add(admin)
                db.session.commit()
                
                print("\n✓ Created admin user:")
                print(f"  Username: admin")
                print(f"  Password: admin123")
                print(f"  ⚠️  CHANGE THIS PASSWORD IMMEDIATELY!")
            else:
                print("\n✓ Admin user already exists")
            
        except Exception as e:
            print(f"\n✗ Error creating admin user: {e}")
            db.session.rollback()
            return False
        
        print("\n[4/5] Creating default group...")
        print("-" * 70)
        
        try:
            # Check if default group exists
            default_group = Group.query.filter_by(name='Default').first()
            
            if not default_group:
                default_group = Group(
                    name='Default',
                    description='Default user group',
                    created_by=admin.id
                )
                
                db.session.add(default_group)
                db.session.commit()
                
                print("\n✓ Created default group")
            else:
                print("\n✓ Default group already exists")
            
        except Exception as e:
            print(f"\n✗ Error creating default group: {e}")
            db.session.rollback()
            return False
        
        print("\n[5/5] Creating default settings...")
        print("-" * 70)
        
        try:
            # Create default settings
            default_settings = [
                ('app_name', 'Beep.Python LLM Management', 'Application name'),
                ('app_version', '1.0.0', 'Application version'),
                ('max_upload_size', '100', 'Maximum upload size in MB'),
                ('session_timeout', '3600', 'Session timeout in seconds'),
            ]
            
            for key, value, description in default_settings:
                existing = Setting.query.get(key)
                if not existing:
                    Setting.set(key, value, description)
            
            print(f"\n✓ Created {len(default_settings)} default settings")
            
        except Exception as e:
            print(f"\n✗ Error creating settings: {e}")
            db.session.rollback()
            return False
        
        print("\n" + "=" * 70)
        print("Database Initialization Complete!")
        print("=" * 70)
        
        print("\n📊 Summary:")
        print(f"  Tables: {len(tables)}")
        print(f"  Roles: {Role.query.count()}")
        print(f"  Users: {User.query.count()}")
        print(f"  Groups: {Group.query.count()}")
        print(f"  Settings: {Setting.query.count()}")
        
        print("\n🔐 Default Credentials:")
        print("  Username: admin")
        print("  Password: admin123")
        print("  ⚠️  CHANGE THIS PASSWORD AFTER FIRST LOGIN!")
        
        print("\n✅ Database is ready to use!")
        print()
        
        return True


def show_schema():
    """Display database schema information"""
    print("=" * 70)
    print("Database Schema Information")
    print("=" * 70)
    
    app = create_app()
    
    with app.app_context():
        inspector = db.inspect(db.engine)
        tables = inspector.get_table_names()
        
        for table in sorted(tables):
            print(f"\n📋 Table: {table}")
            print("-" * 70)
            
            columns = inspector.get_columns(table)
            for col in columns:
                col_type = str(col['type'])
                nullable = "NULL" if col['nullable'] else "NOT NULL"
                primary = "PRIMARY KEY" if col.get('primary_key') else ""
                
                print(f"  {col['name']:20} {col_type:15} {nullable:10} {primary}")
            
            # Show foreign keys
            fks = inspector.get_foreign_keys(table)
            if fks:
                print("\n  Foreign Keys:")
                for fk in fks:
                    print(f"    {fk['constrained_columns']} -> {fk['referred_table']}.{fk['referred_columns']}")


if __name__ == '__main__':
    import argparse
    
    parser = argparse.ArgumentParser(description='Beep.Python Database Management')
    parser.add_argument(
        '--init',
        action='store_true',
        help='Initialize database (WARNING: Drops existing tables!)'
    )
    parser.add_argument(
        '--schema',
        action='store_true',
        help='Show database schema'
    )
    
    args = parser.parse_args()
    
    if args.schema:
        show_schema()
    elif args.init:
        print("\n⚠️  WARNING: This will DROP all existing tables and data!")
        print("Are you sure you want to continue?")
        confirm = input("Type 'YES' to confirm: ")
        
        if confirm == 'YES':
            success = init_database()
            sys.exit(0 if success else 1)
        else:
            print("Cancelled.")
            sys.exit(0)
    else:
        # Default: initialize database
        success = init_database()
        sys.exit(0 if success else 1)
