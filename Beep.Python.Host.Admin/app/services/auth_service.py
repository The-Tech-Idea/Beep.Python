"""
Authorization Service

Handles permission checks and access control for resources.
"""
from typing import Optional, List
from app.models.core import User, Role, Group
from app.database import db


# Permission constants
class Permissions:
    """Permission constants for RBAC"""
    # LLM Permissions
    LLM_VIEW = 'llm:view'
    LLM_LOAD = 'llm:load'
    LLM_DELETE = 'llm:delete'
    LLM_MANAGE_ENV = 'llm:manage_env'
    
    # RAG Permissions
    RAG_READ = 'rag:read'
    RAG_WRITE = 'rag:write'
    RAG_ADMIN = 'rag:admin'
    
    # System Permissions
    SYSTEM_ADMIN = 'system:admin'
    USER_MANAGE = 'user:manage'
    GROUP_MANAGE = 'group:manage'
    
    @classmethod
    def all_permissions(cls) -> List[str]:
        """Get all available permissions"""
        return [
            cls.LLM_VIEW, cls.LLM_LOAD, cls.LLM_DELETE, cls.LLM_MANAGE_ENV,
            cls.RAG_READ, cls.RAG_WRITE, cls.RAG_ADMIN,
            cls.SYSTEM_ADMIN, cls.USER_MANAGE, cls.GROUP_MANAGE
        ]


class AuthService:
    """Service for authorization and permission checks"""
    
    @staticmethod
    def check_permission(user: User, permission: str) -> bool:
        """
        Check if user has a specific permission
        
        Args:
            user: User object
            permission: Permission string (e.g., 'llm:view')
        
        Returns:
            True if user has permission, False otherwise
        """
        if not user or not user.is_active:
            return False
        
        # Admins have all permissions
        if user.is_admin:
            return True
        
        # Check user's role permissions
        if user.role:
            return permission in user.role.get_permissions()
        
        return False
    
    @staticmethod
    def check_any_permission(user: User, permissions: List[str]) -> bool:
        """Check if user has any of the specified permissions"""
        if not user or not user.is_active:
            return False
        
        if user.is_admin:
            return True
        
        for perm in permissions:
            if AuthService.check_permission(user, perm):
                return True
        
        return False
    
    @staticmethod
    def check_all_permissions(user: User, permissions: List[str]) -> bool:
        """Check if user has all of the specified permissions"""
        if not user or not user.is_active:
            return False
        
        if user.is_admin:
            return True
        
        for perm in permissions:
            if not AuthService.check_permission(user, perm):
                return False
        
        return True
    
    @staticmethod
    def can_access_llm(user: User, model_id: str, action: str = 'view') -> bool:
        """
        Check if user can access an LLM model
        
        Args:
            user: User object
            model_id: Model ID
            action: Action to perform ('view', 'load', 'delete', 'manage_env')
        
        Returns:
            True if user has access, False otherwise
        """
        permission_map = {
            'view': Permissions.LLM_VIEW,
            'load': Permissions.LLM_LOAD,
            'delete': Permissions.LLM_DELETE,
            'manage_env': Permissions.LLM_MANAGE_ENV
        }
        
        required_perm = permission_map.get(action, Permissions.LLM_VIEW)
        return AuthService.check_permission(user, required_perm)
    
    @staticmethod
    def can_access_rag(user: User, collection_id: str, access_level: str = 'read') -> bool:
        """
        Check if user can access a RAG collection
        
        Args:
            user: User object
            collection_id: Collection ID
            access_level: Access level ('read', 'write', 'admin')
        
        Returns:
            True if user has access, False otherwise
        """
        permission_map = {
            'read': Permissions.RAG_READ,
            'write': Permissions.RAG_WRITE,
            'admin': Permissions.RAG_ADMIN
        }
        
        required_perm = permission_map.get(access_level, Permissions.RAG_READ)
        return AuthService.check_permission(user, required_perm)
    
    @staticmethod
    def get_user_permissions(user: User) -> List[str]:
        """Get list of all permissions for a user"""
        if not user or not user.is_active:
            return []
        
        if user.is_admin:
            return Permissions.all_permissions()
        
        if user.role:
            return user.role.get_permissions()
        
        return []
    
    @staticmethod
    def create_default_roles():
        """Create default system roles"""
        default_roles = [
            {
                'name': 'Admin',
                'description': 'Full system access',
                'permissions': Permissions.all_permissions(),
                'is_system': True
            },
            {
                'name': 'Power User',
                'description': 'Can create and manage own resources',
                'permissions': [
                    Permissions.LLM_VIEW, Permissions.LLM_LOAD, Permissions.LLM_MANAGE_ENV,
                    Permissions.RAG_READ, Permissions.RAG_WRITE
                ],
                'is_system': True
            },
            {
                'name': 'User',
                'description': 'Standard user with limited access',
                'permissions': [
                    Permissions.LLM_VIEW, Permissions.LLM_LOAD,
                    Permissions.RAG_READ
                ],
                'is_system': True
            },
            {
                'name': 'Guest',
                'description': 'Read-only access',
                'permissions': [
                    Permissions.LLM_VIEW,
                    Permissions.RAG_READ
                ],
                'is_system': True
            }
        ]
        
        for role_data in default_roles:
            existing = Role.query.filter_by(name=role_data['name']).first()
            if not existing:
                role = Role(
                    name=role_data['name'],
                    description=role_data['description'],
                    is_system=role_data['is_system']
                )
                role.set_permissions(role_data['permissions'])
                db.session.add(role)
        
        db.session.commit()


def get_auth_service() -> AuthService:
    """Get singleton auth service instance"""
    return AuthService()
