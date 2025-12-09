"""
Database Migration Script for RBAC Tables

This script creates the necessary tables for Role-Based Access Control:
- roles
- groups
- user_groups (association table)
- Updates users table with role_id

Run this script to migrate the database to support RBAC.
"""
import os
import sys
from pathlib import Path

# Add parent directory to path to import app modules
sys.path.insert(0, str(Path(__file__).parent.parent))

from app.database import db
from app.models.core import Role, Group, User, user_groups
from app.services.auth_service import AuthService
from flask import Flask


def create_app():
    """Create Flask app for migration"""
    app = Flask(__name__)
    
    # Load configuration
    app.config['SQLALCHEMY_DATABASE_URI'] = os.environ.get(
        'DATABASE_URL',
        'sqlite:///beep_python.db'
    )
    app.config['SQLALCHEMY_TRACK_MODIFICATIONS'] = False
    
    db.init_app(app)
    return app


def run_migration():
    """Run the RBAC migration"""
    print("=" * 60)
    print("RBAC Database Migration")
    print("=" * 60)
    
    app = create_app()
    
    with app.app_context():
        print("\n1. Creating database tables...")
        
        try:
            # Create all tables (will skip existing ones)
            db.create_all()
            print("   ✓ Tables created successfully")
        except Exception as e:
            print(f"   ✗ Error creating tables: {e}")
            return False
        
        print("\n2. Creating default roles...")
        
        try:
            AuthService.create_default_roles()
            print("   ✓ Default roles created:")
            
            roles = Role.query.all()
            for role in roles:
                print(f"      - {role.name}: {len(role.get_permissions())} permissions")
        except Exception as e:
            print(f"   ✗ Error creating roles: {e}")
            return False
        
        print("\n3. Checking existing users...")
        
        try:
            users = User.query.all()
            print(f"   Found {len(users)} existing user(s)")
            
            # Assign default role to users without roles
            default_role = Role.query.filter_by(name='User').first()
            admin_role = Role.query.filter_by(name='Admin').first()
            
            updated_count = 0
            for user in users:
                if not user.role_id:
                    # Assign Admin role to admin users, User role to others
                    if user.is_admin:
                        user.role_id = admin_role.id
                        print(f"      - Assigned 'Admin' role to {user.username}")
                    else:
                        user.role_id = default_role.id
                        print(f"      - Assigned 'User' role to {user.username}")
                    updated_count += 1
            
            if updated_count > 0:
                db.session.commit()
                print(f"   ✓ Updated {updated_count} user(s) with default roles")
            else:
                print("   ✓ All users already have roles assigned")
                
        except Exception as e:
            print(f"   ✗ Error updating users: {e}")
            db.session.rollback()
            return False
        
        print("\n4. Verifying migration...")
        
        try:
            # Verify tables exist
            tables = db.engine.table_names()
            required_tables = ['roles', 'groups', 'user_groups']
            
            for table in required_tables:
                if table in tables:
                    print(f"   ✓ Table '{table}' exists")
                else:
                    print(f"   ✗ Table '{table}' missing")
                    return False
            
            # Verify role count
            role_count = Role.query.count()
            if role_count >= 4:
                print(f"   ✓ {role_count} roles created")
            else:
                print(f"   ⚠ Only {role_count} roles found (expected 4+)")
            
        except Exception as e:
            print(f"   ✗ Error verifying migration: {e}")
            return False
        
        print("\n" + "=" * 60)
        print("Migration completed successfully!")
        print("=" * 60)
        print("\nNext steps:")
        print("1. Restart your application")
        print("2. Users can now be assigned to roles and groups")
        print("3. Use the admin interface to manage permissions")
        print()
        
        return True


def rollback_migration():
    """Rollback the RBAC migration (use with caution!)"""
    print("=" * 60)
    print("RBAC Migration Rollback")
    print("=" * 60)
    print("\n⚠️  WARNING: This will delete all RBAC data!")
    print("This includes:")
    print("  - All roles")
    print("  - All groups")
    print("  - User-group associations")
    print("  - Role assignments from users")
    print()
    
    confirm = input("Type 'ROLLBACK' to confirm: ")
    
    if confirm != 'ROLLBACK':
        print("Rollback cancelled.")
        return
    
    app = create_app()
    
    with app.app_context():
        try:
            print("\n1. Removing role assignments from users...")
            User.query.update({User.role_id: None})
            db.session.commit()
            print("   ✓ Role assignments removed")
            
            print("\n2. Dropping RBAC tables...")
            
            # Drop tables in correct order (respecting foreign keys)
            db.session.execute('DROP TABLE IF EXISTS user_groups')
            db.session.execute('DROP TABLE IF EXISTS groups')
            db.session.execute('DROP TABLE IF EXISTS roles')
            db.session.commit()
            
            print("   ✓ RBAC tables dropped")
            print("\nRollback completed successfully!")
            
        except Exception as e:
            print(f"   ✗ Error during rollback: {e}")
            db.session.rollback()


if __name__ == '__main__':
    import argparse
    
    parser = argparse.ArgumentParser(description='RBAC Database Migration')
    parser.add_argument(
        '--rollback',
        action='store_true',
        help='Rollback the migration (WARNING: deletes all RBAC data)'
    )
    
    args = parser.parse_args()
    
    if args.rollback:
        rollback_migration()
    else:
        success = run_migration()
        sys.exit(0 if success else 1)
