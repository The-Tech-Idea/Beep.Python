"""
Host Admin API Client
DEPRECATED: This module is deprecated. MLStudio now uses its own embedded Python
and EnvironmentManager instead of relying on Beep.Python.Host.Admin.

This file is kept for backward compatibility but should not be used in new code.
Use app.services.environment_manager.EnvironmentManager instead.
"""
import requests
import logging
from typing import Dict, List, Optional, Any

logger = logging.getLogger(__name__)


class HostAdminClient:
    """Client for communicating with Beep.Python.Host.Admin API"""
    
    def __init__(self, base_url: str, api_key: Optional[str] = None):
        """
        Initialize Host Admin client
        
        Args:
            base_url: Base URL of Host Admin (e.g., http://127.0.0.1:5000)
            api_key: Optional API key for authentication
        """
        self.base_url = base_url.rstrip('/')
        self.api_key = api_key
        self.session = requests.Session()
        
        if api_key:
            self.session.headers.update({'Authorization': f'Bearer {api_key}'})
    
    def _request(self, method: str, endpoint: str, **kwargs) -> Dict[str, Any]:
        """
        Make HTTP request to Host Admin API
        
        Args:
            method: HTTP method (GET, POST, DELETE, etc.)
            endpoint: API endpoint (e.g., '/api/v1/environments')
            **kwargs: Additional arguments for requests
            
        Returns:
            Response JSON as dictionary
            
        Raises:
            requests.RequestException: If request fails
        """
        url = f"{self.base_url}{endpoint}"
        
        try:
            response = self.session.request(method, url, timeout=30, **kwargs)
            response.raise_for_status()
            return response.json()
        except requests.exceptions.ConnectionError:
            raise ConnectionError(f"Cannot connect to Host Admin at {self.base_url}. Is it running?")
        except requests.exceptions.HTTPError as e:
            error_msg = f"Host Admin API error: {e}"
            try:
                error_detail = response.json().get('error', str(e))
                error_msg = f"Host Admin API error: {error_detail}"
            except:
                pass
            raise RuntimeError(error_msg)
        except Exception as e:
            raise RuntimeError(f"Unexpected error communicating with Host Admin: {e}")
    
    def health_check(self) -> bool:
        """Check if Host Admin is accessible"""
        try:
            response = self._request('GET', '/api/v1/health')
            # Host Admin returns {'status': 'ok'} or {'success': True}
            return response.get('status') == 'ok' or response.get('success', False)
        except:
            return False
    
    def list_environments(self) -> List[Dict[str, Any]]:
        """List all virtual environments"""
        response = self._request('GET', '/api/v1/environments')
        return response.get('data', [])
    
    def get_environment(self, env_name: str) -> Optional[Dict[str, Any]]:
        """Get environment details"""
        try:
            response = self._request('GET', f'/api/v1/environments/{env_name}')
            return response.get('data')
        except Exception as e:
            logger.debug(f"Environment {env_name} not found: {e}")
            return None
    
    def create_environment(self, name: str, python_executable: Optional[str] = None, 
                          packages: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        Create a new virtual environment
        
        Args:
            name: Environment name
            python_executable: Python executable path (optional)
            packages: List of packages to install initially (optional)
            
        Returns:
            Environment information dictionary
        """
        data = {'name': name}
        if python_executable:
            data['python_executable'] = python_executable
        if packages:
            data['packages'] = packages
        
        response = self._request('POST', '/api/v1/environments', json=data)
        return response.get('data', {})
    
    def delete_environment(self, name: str) -> bool:
        """Delete a virtual environment"""
        try:
            response = self._request('DELETE', f'/api/v1/environments/{name}')
            return response.get('success', False)
        except:
            return False
    
    def install_packages(self, env_name: str, packages: List[str]) -> Dict[str, Any]:
        """
        Install packages in an environment
        
        Args:
            env_name: Environment name
            packages: List of package names (e.g., ['numpy', 'pandas', 'scikit-learn'])
            
        Returns:
            Installation result dictionary
        """
        data = {'packages': packages}
        response = self._request('POST', f'/api/v1/environments/{env_name}/packages', json=data)
        return response.get('data', {})
    
    def get_packages(self, env_name: str) -> List[Dict[str, Any]]:
        """Get list of installed packages in an environment"""
        try:
            response = self._request('GET', f'/api/v1/environments/{env_name}/packages')
            return response.get('data', [])
        except Exception as e:
            logger.warning(f"Failed to get packages for {env_name}: {e}")
            return []
    
    def get_python_executable(self, env_name: str) -> Optional[str]:
        """Get Python executable path for an environment"""
        env = self.get_environment(env_name)
        if env:
            return env.get('python_executable')
        return None

