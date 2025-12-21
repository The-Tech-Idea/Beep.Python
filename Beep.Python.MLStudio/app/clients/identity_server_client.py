"""
Identity Server Client - Integration with Beep.Foundation.IdentityServer
"""
import requests
import logging
from typing import Dict, Optional, Any, List, Tuple
import os

from app.utils.constants import (
    ID_SERVER_ENDPOINT_HEALTH,
    ID_SERVER_ENDPOINT_TOKEN_VALIDATE,
    ID_SERVER_ENDPOINT_USER_ACCESS_CHECK,
    ID_SERVER_ENDPOINT_USER_ROLE,
    ID_SERVER_ENDPOINT_USER_APPLICATIONS,
    ID_SERVER_ENDPOINT_TOKEN,
    ID_SERVER_ENDPOINT_USERINFO,
    HTTP_TIMEOUT,
    MSG_UNAUTHORIZED,
    MSG_FORBIDDEN,
    MSG_CONNECTION_ERROR,
    MSG_TIMEOUT_ERROR
)

logger = logging.getLogger(__name__)


class IdentityServerClient:
    """Client for Beep.Foundation.IdentityServer API"""
    
    def __init__(self, base_url: str = None, client_id: str = None, client_secret: str = None):
        """
        Initialize Identity Server client
        
        Args:
            base_url: Identity Server base URL (e.g., https://identityserver.com)
            client_id: OAuth client ID
            client_secret: OAuth client secret (for confidential clients)
        """
        self.base_url = (base_url or os.getenv('IDENTITY_SERVER_URL', '')).rstrip('/')
        self.client_id = client_id or os.getenv('IDENTITY_SERVER_CLIENT_ID', '')
        self.client_secret = client_secret or os.getenv('IDENTITY_SERVER_CLIENT_SECRET', '')
        self.session = requests.Session()
    
    def _request(self, method: str, endpoint: str, access_token: str = None, **kwargs) -> Dict[str, Any]:
        """
        Make HTTP request to Identity Server
        
        Args:
            method: HTTP method (GET, POST, etc.)
            endpoint: API endpoint path
            access_token: Optional OAuth access token
            **kwargs: Additional arguments for requests
            
        Returns:
            Response data as dictionary
        """
        url = f"{self.base_url}{endpoint}"
        headers = kwargs.pop('headers', {})
        
        if access_token:
            headers['Authorization'] = f'Bearer {access_token}'
        
        timeout = kwargs.pop('timeout', HTTP_TIMEOUT)
        
        try:
            response = self.session.request(
                method, 
                url, 
                headers=headers, 
                timeout=timeout, 
                **kwargs
            )
            
            # Try to parse JSON response
            try:
                result = response.json()
            except ValueError:
                result = {'success': False, 'error': response.text}
            
            # Check for errors
            if response.status_code >= 400:
                error_msg = result.get('error', f'HTTP {response.status_code}')
                result['success'] = False
                result['error'] = error_msg
            
            return result
            
        except requests.exceptions.ConnectionError:
            logger.error(f"Connection error to Identity Server: {url}")
            return {
                'success': False,
                'error': MSG_CONNECTION_ERROR
            }
        except requests.exceptions.Timeout:
            logger.error(f"Timeout connecting to Identity Server: {url}")
            return {
                'success': False,
                'error': MSG_TIMEOUT_ERROR
            }
        except Exception as e:
            logger.error(f"Error requesting Identity Server: {e}")
            return {
                'success': False,
                'error': str(e)
            }
    
    def health_check(self) -> Tuple[bool, Optional[str]]:
        """
        Check if Identity Server is accessible
        
        Returns:
            Tuple of (is_healthy, error_message)
        """
        if not self.base_url:
            return False, "Identity Server URL not configured"
        
        result = self._request('GET', ID_SERVER_ENDPOINT_HEALTH)
        if result.get('success', False) or 'error' not in result:
            return True, None
        return False, result.get('error', 'Health check failed')
    
    def validate_token(self, access_token: str, client_id: str = None) -> Tuple[bool, Optional[Dict], Optional[str]]:
        """
        Validate OAuth access token
        
        Args:
            access_token: OAuth access token
            client_id: OAuth client ID (uses instance client_id if not provided)
            
        Returns:
            Tuple of (is_valid, user_info_dict, error_message)
        """
        client_id = client_id or self.client_id
        if not client_id:
            return False, None, "Client ID not configured"
        
        result = self._request(
            'POST',
            ID_SERVER_ENDPOINT_TOKEN_VALIDATE,
            data={
                'token': access_token,
                'client_id': client_id
            }
        )
        
        if result.get('success') and result.get('valid'):
            return True, result.get('user_info'), None
        return False, None, result.get('error', 'Token validation failed')
    
    def get_user_info(self, access_token: str) -> Tuple[bool, Optional[Dict], Optional[str]]:
        """
        Get user information from Identity Server
        
        Args:
            access_token: OAuth access token
            
        Returns:
            Tuple of (success, user_info_dict, error_message)
        """
        result = self._request('GET', ID_SERVER_ENDPOINT_USERINFO, access_token=access_token)
        
        if result.get('success') or 'error' not in result:
            return True, result, None
        return False, None, result.get('error', 'Failed to retrieve user info')
    
    def check_user_access(self, access_token: str, application: str = None) -> Tuple[bool, Optional[str]]:
        """
        Check if user has access to application
        
        Args:
            access_token: OAuth access token
            application: Application name (optional)
            
        Returns:
            Tuple of (has_access, error_message)
        """
        data = {}
        if application:
            data['application'] = application
        
        result = self._request(
            'POST',
            ID_SERVER_ENDPOINT_USER_ACCESS_CHECK,
            access_token=access_token,
            json=data
        )
        
        if result.get('success') and result.get('has_access'):
            return True, None
        return False, result.get('error', 'Access denied')
    
    def get_user_role(self, access_token: str) -> Tuple[Optional[str], Optional[str]]:
        """
        Get user role from Identity Server
        
        Args:
            access_token: OAuth access token
            
        Returns:
            Tuple of (role, error_message)
        """
        result = self._request('GET', ID_SERVER_ENDPOINT_USER_ROLE, access_token=access_token)
        
        if result.get('success') or 'error' not in result:
            role = result.get('role') or result.get('user_role')
            return role, None
        return None, result.get('error', 'Failed to retrieve user role')


# Singleton instance
_identity_server_client: Optional[IdentityServerClient] = None


def get_identity_server_client(
    base_url: str = None,
    client_id: str = None,
    client_secret: str = None
) -> IdentityServerClient:
    """
    Get or create Identity Server client singleton instance
    
    Args:
        base_url: Optional base URL override
        client_id: Optional client ID override
        client_secret: Optional client secret override
        
    Returns:
        IdentityServerClient instance
    """
    global _identity_server_client
    
    if _identity_server_client is None or base_url or client_id:
        _identity_server_client = IdentityServerClient(base_url, client_id, client_secret)
    
    return _identity_server_client
