"""
Community Connection Service
Manages connection to Beep.AI.Community platform
"""
import os
import logging
from typing import Optional, Tuple, Dict
from pathlib import Path

from app.services.settings_manager import get_settings_manager
from app.services.community_client import CommunityClient, get_community_client
from app.utils.constants import (
    DEFAULT_COMMUNITY_URL,
    ERROR_COMMUNITY_CONNECTION_FAILED,
    ERROR_COMMUNITY_AUTH_FAILED,
    ERROR_COMMUNITY_NOT_CONFIGURED,
    MSG_COMMUNITY_CONNECTED
)
from app.exceptions.community_exceptions import (
    CommunityConnectionError,
    CommunityAuthError
)

logger = logging.getLogger(__name__)


class CommunityConnectionService:
    """Service for managing Community server connection"""
    
    SETTING_COMMUNITY_URL = 'community_url'
    SETTING_COMMUNITY_API_KEY = 'community_api_key'
    SETTING_COMMUNITY_CONNECTED = 'community_connected'
    
    def __init__(self):
        self.settings_mgr = get_settings_manager()
    
    def configure_connection(self, url: str, api_key: str) -> Tuple[bool, Optional[str]]:
        """
        Configure Community server connection
        
        Args:
            url: Community server URL
            api_key: API key for authentication
            
        Returns:
            Tuple of (success, error_message)
        """
        try:
            # Validate URL
            if not url or not url.strip():
                return False, "Community URL is required"
            
            # Normalize URL
            url = url.strip().rstrip('/')
            if not url.startswith('http://') and not url.startswith('https://'):
                url = f'http://{url}'
            
            # Store configuration
            self.settings_mgr.set(self.SETTING_COMMUNITY_URL, url)
            self.settings_mgr.set(self.SETTING_COMMUNITY_API_KEY, api_key)
            
            # Test connection
            success, error = self.test_connection()
            if success:
                self.settings_mgr.set(self.SETTING_COMMUNITY_CONNECTED, 'true')
                logger.info(f"Connected to Community server: {url}")
                return True, None
            else:
                self.settings_mgr.set(self.SETTING_COMMUNITY_CONNECTED, 'false')
                return False, error or ERROR_COMMUNITY_CONNECTION_FAILED
                
        except Exception as e:
            logger.error(f"Error configuring Community connection: {e}")
            return False, str(e)
    
    def test_connection(self) -> Tuple[bool, Optional[str]]:
        """
        Test connection to Community server
        
        Returns:
            Tuple of (success, error_message)
        """
        try:
            url = self.get_community_url()
            api_key = self.get_api_key()
            
            if not url:
                return False, ERROR_COMMUNITY_NOT_CONFIGURED
            
            # Create client and test connection
            client = CommunityClient(base_url=url, api_key=api_key)
            is_healthy = client.health_check()
            
            if is_healthy:
                return True, None
            else:
                # Try to get more specific error from health check
                return False, "Cannot connect to Community server. Please check the URL and ensure the server is running."
                
        except CommunityConnectionError as e:
            return False, str(e)
        except Exception as e:
            logger.error(f"Error testing Community connection: {e}")
            return False, f"Connection test failed: {str(e)}"
    
    def is_connected(self) -> bool:
        """
        Check if Community is currently connected
        
        Returns:
            True if connected and connection is valid
        """
        try:
            connected = self.settings_mgr.get(self.SETTING_COMMUNITY_CONNECTED, 'false')
            if connected.lower() != 'true':
                return False
            
            # Test connection to verify it's still valid
            success, _ = self.test_connection()
            if not success:
                # Update status
                self.settings_mgr.set(self.SETTING_COMMUNITY_CONNECTED, 'false')
            
            return success
        except Exception as e:
            logger.error(f"Error checking connection status: {e}")
            return False
    
    def get_connection_config(self) -> Dict:
        """
        Get current connection configuration
        
        Returns:
            Dictionary with connection config
        """
        return {
            'url': self.get_community_url(),
            'api_key': self.get_api_key()[:10] + '...' if self.get_api_key() else None,  # Mask API key
            'connected': self.is_connected()
        }
    
    def get_community_url(self) -> Optional[str]:
        """Get configured Community URL"""
        url = self.settings_mgr.get(self.SETTING_COMMUNITY_URL)
        if not url:
            # Fallback to environment variable
            url = os.getenv('COMMUNITY_URL', DEFAULT_COMMUNITY_URL)
        return url
    
    def get_api_key(self) -> Optional[str]:
        """Get configured API key"""
        api_key = self.settings_mgr.get(self.SETTING_COMMUNITY_API_KEY)
        if not api_key:
            # Fallback to environment variable
            api_key = os.getenv('COMMUNITY_API_KEY')
        return api_key
    
    def disconnect(self):
        """Disconnect from Community server"""
        self.settings_mgr.set(self.SETTING_COMMUNITY_CONNECTED, 'false')
        logger.info("Disconnected from Community server")
    
    def get_client(self) -> Optional[CommunityClient]:
        """
        Get configured Community client instance
        
        Returns:
            CommunityClient instance or None if not configured
        """
        url = self.get_community_url()
        api_key = self.get_api_key()
        
        if not url:
            return None
        
        return CommunityClient(base_url=url, api_key=api_key)


# Singleton instance
_connection_service: Optional[CommunityConnectionService] = None


def get_community_connection_service() -> CommunityConnectionService:
    """Get or create Community connection service singleton"""
    global _connection_service
    
    if _connection_service is None:
        _connection_service = CommunityConnectionService()
    
    return _connection_service
