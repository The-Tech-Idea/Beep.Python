"""
API Token Manager

Manages API tokens for user authentication in middleware.
"""
import secrets
import hashlib
import logging
from typing import Optional, Dict, Any, List, TYPE_CHECKING
from datetime import datetime, timedelta
from flask import has_app_context

if TYPE_CHECKING:
    from app.models.core import APIToken

logger = logging.getLogger(__name__)


class APITokenManager:
    """Manages API token generation and validation"""
    
    @staticmethod
    def generate_token(length: int = 32) -> str:
        """Generate a secure random token"""
        # Generate URL-safe token
        token = secrets.token_urlsafe(length)
        return token
    
    @staticmethod
    def hash_token(token: str) -> str:
        """Hash a token for storage (SHA-256)"""
        return hashlib.sha256(token.encode()).hexdigest()
    
    @staticmethod
    def create_token(user_id: int, name: Optional[str] = None,
                    expires_in_days: Optional[int] = None,
                    scopes: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        Create a new API token for a user
        
        Args:
            user_id: User ID
            name: Token name/description
            expires_in_days: Days until expiration (None = no expiration)
            scopes: List of allowed scopes
        
        Returns:
            Dict with 'token' (plain text) and 'token_record' (database record)
        """
        if not has_app_context():
            raise RuntimeError("No Flask app context available")
        
        from app.models.core import APIToken, User
        from app.database import db
        
        # Check if user exists
        user = db.session.get(User, user_id)
        if not user:
            raise ValueError(f"User {user_id} not found")
        
        # Generate token
        plain_token = APITokenManager.generate_token()
        token_hash = APITokenManager.hash_token(plain_token)
        
        # Calculate expiration
        expires_at = None
        if expires_in_days:
            expires_at = datetime.utcnow() + timedelta(days=expires_in_days)
        
        # Create token record
        token_record = APIToken(
            user_id=user_id,
            token=token_hash,  # Store hashed token
            name=name or f"API Token for {user.username}",
            expires_at=expires_at,
            is_active=True
        )
        
        if scopes:
            token_record.set_scopes(scopes)
        
        db.session.add(token_record)
        db.session.commit()
        
        logger.info(f"Created API token for user {user_id} ({user.username})")
        
        return {
            'token': plain_token,  # Return plain token (only shown once)
            'token_record': token_record,
            'user': user
        }
    
    @staticmethod
    def validate_token(token: str) -> Optional[Dict[str, Any]]:
        """
        Validate an API token and return user info
        
        Args:
            token: Plain text token
        
        Returns:
            Dict with user info if valid, None if invalid
        """
        if not has_app_context():
            return None
        
        from app.models.core import APIToken, User
        from app.database import db
        
        # Hash the provided token
        token_hash = APITokenManager.hash_token(token)
        
        # Find token
        token_record = APIToken.query.filter_by(token=token_hash, is_active=True).first()
        
        if not token_record:
            return None
        
        # Check expiration
        if token_record.is_expired():
            return None
        
        # Update last used
        token_record.last_used_at = datetime.utcnow()
        db.session.commit()
        
        # Get user
        user = db.session.get(User, token_record.user_id)
        if not user or not user.is_active:
            return None
        
        # Get user role
        user_role = user.role.name if user.role else None
        
        return {
            'user_id': str(user.id),
            'username': user.username,
            'user_role': user_role,
            'is_admin': user.is_admin,
            'token_id': token_record.id,
            'scopes': token_record.get_scopes()
        }
    
    @staticmethod
    def revoke_token(token_id: int) -> bool:
        """Revoke (deactivate) a token"""
        if not has_app_context():
            return False
        
        from app.models.core import APIToken
        from app.database import db
        
        token = db.session.get(APIToken, token_id)
        if not token:
            return False
        
        token.is_active = False
        db.session.commit()
        
        logger.info(f"Revoked API token {token_id}")
        return True
    
    @staticmethod
    def revoke_all_user_tokens(user_id: int) -> int:
        """Revoke all tokens for a user"""
        if not has_app_context():
            return 0
        
        from app.models.core import APIToken
        from app.database import db
        
        tokens = APIToken.query.filter_by(user_id=user_id, is_active=True).all()
        count = len(tokens)
        
        for token in tokens:
            token.is_active = False
        
        db.session.commit()
        
        logger.info(f"Revoked {count} API tokens for user {user_id}")
        return count
    
    @staticmethod
    def get_user_tokens(user_id: int) -> List['APIToken']:
        """Get all tokens for a user"""
        if not has_app_context():
            return []
        
        from app.models.core import APIToken
        
        return APIToken.query.filter_by(user_id=user_id).order_by(APIToken.created_at.desc()).all()
    
    @staticmethod
    def get_token_by_id(token_id: int) -> Optional['APIToken']:
        """Get token by ID"""
        if not has_app_context():
            return None
        
        from app.models.core import APIToken
        
        return APIToken.query.get(token_id)


def get_api_token_manager() -> APITokenManager:
    """Get API token manager instance"""
    return APITokenManager()
