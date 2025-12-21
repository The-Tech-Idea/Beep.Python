"""
Identity Server Authentication Service
Handles OAuth2/OIDC authentication via Beep.Foundation.IdentityServer
"""
import os
import logging
from typing import Optional, Tuple, Dict, Any

from flask_jwt_extended import create_access_token
from app import db
from app.models.user import User
from app.clients.identity_server_client import IdentityServerClient, get_identity_server_client
from app.utils.constants import (
    AUTH_MODE_IDENTITY_SERVER,
    ERROR_IDENTITY_SERVER_NOT_CONFIGURED,
    ERROR_TOKEN_VALIDATION_FAILED,
    ERROR_USER_INFO_RETRIEVAL_FAILED,
    ERROR_EMAIL_NOT_FOUND,
    ERROR_USERNAME_NOT_FOUND
)
from app.exceptions.auth_exceptions import (
    IdentityServerError,
    TokenValidationError,
    UserAccessDeniedError
)

logger = logging.getLogger(__name__)


class IdentityServerAuthService:
    """Service for Identity Server OAuth2/OIDC authentication"""
    
    def __init__(self, client: Optional[IdentityServerClient] = None):
        """
        Initialize Identity Server auth service
        
        Args:
            client: Optional Identity Server client instance
        """
        self.client = client or get_identity_server_client()
    
    def validate_token(
        self, 
        access_token: str, 
        client_id: Optional[str] = None
    ) -> Tuple[bool, Optional[Dict[str, Any]], Optional[str]]:
        """
        Validate OAuth token from Identity Server
        
        Args:
            access_token: OAuth access token
            client_id: Optional OAuth client ID
            
        Returns:
            Tuple of (is_valid, user_info, error_message)
        """
        if not self.client.base_url:
            return False, None, ERROR_IDENTITY_SERVER_NOT_CONFIGURED
        
        try:
            is_valid, user_info, error = self.client.validate_token(access_token, client_id)
            if not is_valid:
                raise TokenValidationError(error or ERROR_TOKEN_VALIDATION_FAILED)
            return True, user_info, None
        except TokenValidationError:
            raise
        except Exception as e:
            logger.error(f"Token validation error: {e}")
            raise IdentityServerError(f"Token validation failed: {str(e)}", original_error=e)
    
    def get_or_create_user(self, user_info: Dict[str, Any]) -> Tuple[User, bool]:
        """
        Get or create user from Identity Server user info
        
        Args:
            user_info: User information from Identity Server
            
        Returns:
            Tuple of (user_object, was_created)
        """
        # Extract email (required)
        email = user_info.get('email') or user_info.get('Email')
        if not email:
            raise IdentityServerError(ERROR_EMAIL_NOT_FOUND)
        
        # Extract username (prefer preferred_username, fallback to email)
        username = (
            user_info.get('preferred_username') or 
            user_info.get('username') or 
            user_info.get('sub') or 
            email.split('@')[0]
        )
        
        # Check if user exists
        user = User.query.filter_by(email=email).first()
        if not user:
            user = User.query.filter_by(username=username).first()
        
        if user:
            # Update user info if needed
            if user.email != email:
                user.email = email
            if user.username != username:
                user.username = username
            db.session.commit()
            return user, False
        
        # Create new user (no password for Identity Server users)
        user = User(
            username=username,
            email=email,
            password_hash=None,  # No local password for Identity Server users
            is_active=True
        )
        
        # Set admin if indicated in user info
        user.is_admin = user_info.get('is_admin', False) or user_info.get('IsAdmin', False)
        
        db.session.add(user)
        try:
            db.session.commit()
            logger.info(f"Created user from Identity Server: {username} ({email})")
            return user, True
        except Exception as e:
            db.session.rollback()
            logger.error(f"Error creating user: {e}")
            raise IdentityServerError(f"Failed to create user: {str(e)}", original_error=e)
    
    def login(self, access_token: str, client_id: Optional[str] = None) -> Tuple[Optional[Dict], Optional[str]]:
        """
        Login using Identity Server OAuth token
        
        Args:
            access_token: OAuth access token
            client_id: Optional OAuth client ID
            
        Returns:
            Tuple of (login_result_dict, error_message)
        """
        try:
            # Validate token
            is_valid, user_info, error = self.validate_token(access_token, client_id)
            if not is_valid:
                return None, error or ERROR_TOKEN_VALIDATION_FAILED
            
            # Get or create user
            user, created = self.get_or_create_user(user_info)
            
            # Create JWT token for local session
            jwt_token = create_access_token(identity=user.id)
            
            return {
                'access_token': jwt_token,
                'user': user.to_dict(),
                'oauth_token': access_token,
                'created': created
            }, None
            
        except IdentityServerError as e:
            logger.error(f"Identity Server login error: {e}")
            return None, str(e)
        except Exception as e:
            logger.error(f"Unexpected login error: {e}")
            return None, f"Login failed: {str(e)}"
    
    def get_user_role(self, access_token: str) -> Optional[str]:
        """
        Get user role from Identity Server
        
        Args:
            access_token: OAuth access token
            
        Returns:
            User role or None
        """
        try:
            role, error = self.client.get_user_role(access_token)
            return role
        except Exception as e:
            logger.error(f"Error getting user role: {e}")
            return None
