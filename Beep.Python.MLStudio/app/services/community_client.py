"""
MLStudio Community Client - Publish projects to Beep.AI.Community
"""
import os
import requests
import logging
from typing import Dict, Optional, Any
from pathlib import Path

logger = logging.getLogger(__name__)


class CommunityClient:
    """Client for publishing to Beep.AI.Community platform"""
    
    def __init__(self, base_url: str = None, api_key: Optional[str] = None):
        """
        Initialize Community client
        
        Args:
            base_url: Base URL of Community platform (e.g., http://127.0.0.1:5002)
            api_key: API key for authentication (from Community platform)
        """
        # Default to localhost if not specified
        self.base_url = (base_url or os.getenv('COMMUNITY_URL', 'http://127.0.0.1:5002')).rstrip('/')
        self.api_key = api_key or os.getenv('COMMUNITY_API_KEY')
        self.session = requests.Session()
        self.session.headers.update({'User-Agent': 'BeepMLStudio/1.0'})
        
        if self.api_key:
            # Support both Bearer token and X-API-Key header
            self.session.headers.update({
                'Authorization': f'Bearer {self.api_key}',
                'X-API-Key': self.api_key
            })
    
    def _request(self, method: str, endpoint: str, **kwargs) -> Dict[str, Any]:
        """Make HTTP request to Community API"""
        url = f"{self.base_url}{endpoint}"
        timeout = kwargs.pop('timeout', 60)
        
        try:
            response = self.session.request(method, url, timeout=timeout, **kwargs)
            
            # Try to get JSON response
            try:
                result = response.json()
            except:
                result = {'success': False, 'error': response.text}
            
            # Check for errors
            if response.status_code >= 400:
                error = result.get('error', f'HTTP {response.status_code}')
                result['success'] = False
                result['error'] = error
            
            return result
            
        except requests.exceptions.ConnectionError:
            return {'success': False, 'error': f"Cannot connect to Community platform at {self.base_url}. Is it running?"}
        except requests.exceptions.Timeout:
            return {'success': False, 'error': "Request timed out"}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def health_check(self) -> bool:
        """Check if Community platform is accessible"""
        try:
            result = self._request('GET', '/api/v1/health')
            return result.get('status') == 'ok' or 'error' not in result
        except:
            return False
    
    def publish_project(
        self,
        project_id: int,
        title: str,
        description: str = '',
        code_content: str = '',
        output_content: str = '',
        language: str = 'python',
        tags: list = None,
        category: str = None,
        is_public: bool = True
    ) -> Dict[str, Any]:
        """
        Publish a project from MLStudio to Community platform
        
        Args:
            project_id: MLStudio project ID
            title: Project title
            description: Project description
            code_content: Code content (from files or notebook)
            output_content: Output/results (if any)
            language: Programming language
            tags: List of tags
            category: Project category
            is_public: Whether project is public
        
        Returns:
            Dict with success status and project info or error
        """
        payload = {
            'title': title,
            'description': description,
            'project_id': project_id,
            'code_content': code_content,
            'output_content': output_content,
            'language': language,
            'kernel_type': 'notebook',
            'tags': tags or [],
            'category': category,
            'is_public': is_public
        }
        
        result = self._request(
            'POST',
            '/api/v1/notebooks',
            json=payload,
            timeout=60
        )
        
        return result
    
    def publish_dataset(
        self,
        dataset_path: str,
        title: str,
        description: str = '',
        tags: list = None,
        category: str = None,
        license: str = 'MIT',
        is_public: bool = True
    ) -> Dict[str, Any]:
        """
        Publish a dataset to Community platform
        
        Args:
            dataset_path: Path to dataset file
            title: Dataset title
            description: Dataset description
            tags: List of tags
            category: Dataset category
            license: License type
            is_public: Whether dataset is public
        
        Returns:
            Dict with success status and dataset info or error
        """
        file_path = Path(dataset_path)
        
        if not file_path.exists():
            return {'success': False, 'error': f"Dataset file not found: {dataset_path}"}
        
        try:
            with open(file_path, 'rb') as f:
                files = {'file': (file_path.name, f, 'application/octet-stream')}
                
                data = {
                    'title': title,
                    'description': description,
                    'tags': ','.join(tags) if tags else '',
                    'category': category or '',
                    'license': license,
                    'is_public': str(is_public).lower()
                }
                
                result = self._request(
                    'POST',
                    '/api/v1/datasets',
                    data=data,
                    files=files,
                    timeout=300  # 5 min timeout for upload
                )
            
            return result
                
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def submit_to_competition(
        self,
        competition_id: int,
        submission_file_path: str,
        model_id: Optional[int] = None
    ) -> Dict[str, Any]:
        """
        Submit to a competition
        
        Args:
            competition_id: Competition ID
            submission_file_path: Path to submission file (predictions CSV or model file)
            model_id: Optional model ID from MLStudio
        
        Returns:
            Dict with success status and submission info or error
        """
        file_path = Path(submission_file_path)
        
        if not file_path.exists():
            return {'success': False, 'error': f"Submission file not found: {submission_file_path}"}
        
        try:
            with open(file_path, 'rb') as f:
                files = {'file': (file_path.name, f, 'application/octet-stream')}
                
                data = {
                    'model_id': model_id
                } if model_id else {}
                
                result = self._request(
                    'POST',
                    f'/api/v1/competitions/{competition_id}/submit',
                    data=data,
                    files=files,
                    timeout=300
                )
            
            return result
                
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def get_competitions(self, active_only: bool = True) -> Dict[str, Any]:
        """Get list of competitions"""
        endpoint = '/api/v1/competitions'
        params = {}
        if active_only:
            params['is_active'] = 'true'
        
        return self._request('GET', endpoint, params=params)
    
    def get_datasets(self, search: str = None, category: str = None) -> Dict[str, Any]:
        """Get list of datasets"""
        params = {}
        if search:
            params['search'] = search
        if category:
            params['category'] = category
        
        return self._request('GET', '/api/v1/datasets', params=params)
    
    def get_user_competitions(self, user_id: int) -> Dict[str, Any]:
        """
        Get competitions that a user has joined
        
        Args:
            user_id: User ID in Community platform
            
        Returns:
            Dict with competitions list or error
        """
        return self._request('GET', f'/api/v1/user-participation/users/{user_id}/competitions')
    
    def get_user_submissions(
        self, 
        user_id: int, 
        competition_id: Optional[int] = None
    ) -> Dict[str, Any]:
        """
        Get user's submissions
        
        Args:
            user_id: User ID in Community platform
            competition_id: Optional competition ID to filter submissions
            
        Returns:
            Dict with submissions list or error
        """
        endpoint = f'/api/v1/users/{user_id}/submissions'
        if competition_id:
            endpoint = f'/api/v1/users/{user_id}/submissions/{competition_id}'
        
        return self._request('GET', endpoint)
    
    def get_user_rankings(self, user_id: int) -> Dict[str, Any]:
        """
        Get user's rankings across all competitions
        
        Args:
            user_id: User ID in Community platform
            
        Returns:
            Dict with rankings list or error
        """
        return self._request('GET', f'/api/v1/user-participation/users/{user_id}/rankings')
    
    def get_user_activity(self, user_id: int, limit: int = 10) -> Dict[str, Any]:
        """
        Get recent user activity
        
        Args:
            user_id: User ID in Community platform
            limit: Maximum number of activities to return
            
        Returns:
            Dict with activity list or error
        """
        params = {'limit': limit}
        return self._request('GET', f'/api/v1/user-participation/users/{user_id}/activity', params=params)
    
    def get_user_stats(self, user_id: int) -> Dict[str, Any]:
        """
        Get user participation statistics
        
        Args:
            user_id: User ID in Community platform
            
        Returns:
            Dict with statistics or error
        """
        return self._request('GET', f'/api/v1/user-participation/users/{user_id}/competitions/stats')
    
    def get_competition_detail(self, competition_id: int) -> Dict[str, Any]:
        """
        Get detailed competition information including user's status
        
        Args:
            competition_id: Competition ID
        
        Returns:
            Dict with competition details or error
        """
        return self._request('GET', f'/api/v1/competitions/{competition_id}')
    
    def join_competition(self, competition_id: int, user_id: int) -> Dict[str, Any]:
        """
        Join a competition in Community
        
        Args:
            competition_id: Competition ID
            user_id: User ID in Community platform
        
        Returns:
            Dict with join status and participant info or error
        """
        result = self._request(
            'POST',
            f'/api/v1/competitions/{competition_id}/join',
            json={'user_id': user_id},  # May not be needed if JWT is used, but included for API key auth
            timeout=30
        )
        return result
    
    def download_training_data(self, competition_id: int, save_path: Path) -> Tuple[bool, Optional[str]]:
        """
        Download training dataset from a competition
        
        Args:
            competition_id: Competition ID
            save_path: Path where the file should be saved
        
        Returns:
            Tuple of (success, error_message)
        """
        try:
            # First get competition details to find training_data_path
            competition = self.get_competition_detail(competition_id)
            
            # Check for error response
            if competition.get('error') or competition.get('success') is False:
                return False, competition.get('error', 'Competition not found or training data not available')
            
            # Competition detail response structure may vary - handle both dict and direct access
            training_data_path = None
            if isinstance(competition, dict):
                training_data_path = competition.get('training_data_path')
            
            if not training_data_path:
                return False, "Training data not available for this competition"
            
            # Use API endpoint for authenticated download
            base_url = self.base_url.rstrip('/')
            download_url = f"{base_url}/api/v1/competitions/{competition_id}/training-data"
            
            # Download the file using session (includes auth headers)
            response = self.session.get(
                download_url,
                timeout=300,  # 5 min timeout for large files
                stream=True
            )
            
            if response.status_code != 200:
                return False, f"Failed to download training data: HTTP {response.status_code} - {response.text[:200]}"
            
            # Ensure save directory exists
            save_path.parent.mkdir(parents=True, exist_ok=True)
            
            # Save file
            with open(save_path, 'wb') as f:
                for chunk in response.iter_content(chunk_size=8192):
                    f.write(chunk)
            
            return True, None
            
        except requests.exceptions.RequestException as e:
            return False, f"Network error downloading training data: {str(e)}"
        except Exception as e:
            return False, f"Error downloading training data: {str(e)}"
    
    def get_competition_leaderboard(self, competition_id: int, limit: int = 100) -> Dict[str, Any]:
        """
        Get leaderboard for a competition
        
        Args:
            competition_id: Competition ID
            limit: Maximum number of entries to return
        
        Returns:
            Dict with leaderboard entries or error
        """
        params = {'limit': limit}
        return self._request('GET', f'/api/v1/competitions/{competition_id}/leaderboard', params=params)
    
    def submit_model_to_competition(
        self,
        competition_id: int,
        model_file_path: str,
        model_name: str,
        user_id: int,
        model_type: Optional[str] = None,
        framework: Optional[str] = None,
        metrics: Optional[Dict] = None,
        description: Optional[str] = None,
        input_schema: Optional[Dict] = None,
        output_schema: Optional[Dict] = None,
        mlstudio_source_id: Optional[str] = None
    ) -> Dict[str, Any]:
        """
        Submit a trained model to a Community competition
        
        The model will be validated against competition test data before acceptance.
        If validation fails, detailed error information is returned.
        
        Args:
            competition_id: Competition ID
            model_file_path: Path to model file (will be base64 encoded)
            model_name: Name of the model
            user_id: User ID in Community platform
            model_type: Type of model (optional)
            framework: Framework used (optional)
            metrics: Model metrics dictionary (optional)
            description: Model description (optional)
            input_schema: Input schema dictionary (optional)
            output_schema: Output schema dictionary (optional)
            mlstudio_source_id: Original MLStudio model ID (optional)
        
        Returns:
            Dict with success status, submission info, or validation errors
        """
        model_path = Path(model_file_path)
        
        if not model_path.exists():
            return {'success': False, 'error': f"Model file not found: {model_file_path}"}
        
        try:
            # Read model file and encode as base64
            import base64
            with open(model_path, 'rb') as f:
                model_data = base64.b64encode(f.read()).decode('utf-8')
            
            # Prepare payload
            payload = {
                'model_name': model_name,
                'model_file_data': model_data,
                'user_id': user_id,
                'model_type': model_type,
                'framework': framework,
                'metrics': metrics,
                'description': description,
                'input_schema': input_schema,
                'output_schema': output_schema,
                'mlstudio_source_id': mlstudio_source_id
            }
            
            # Remove None values
            payload = {k: v for k, v in payload.items() if v is not None}
            
            # Submit to Community API
            result = self._request(
                'POST',
                f'/api/v1/competitions/{competition_id}/submit-model',
                json=payload,
                timeout=300  # 5 min timeout for validation and submission
            )
            
            return result
                
        except Exception as e:
            return {'success': False, 'error': str(e)}


# Singleton instance
_community_client: Optional[CommunityClient] = None


def get_community_client(base_url: str = None, api_key: str = None) -> CommunityClient:
    """Get or create Community client instance"""
    global _community_client
    
    if _community_client is None or base_url:
        # Get URL from settings if not provided
        if not base_url:
            try:
                from app.services.settings_manager import SettingsManager
                settings = SettingsManager()
                base_url = settings.get('community_url', 'http://127.0.0.1:5002')
                api_key = settings.get('community_api_key')
            except:
                base_url = 'http://127.0.0.1:5002'
        
        _community_client = CommunityClient(base_url, api_key)
    
    return _community_client


def reset_community_client():
    """Reset the client (for testing or reconnection)"""
    global _community_client
    _community_client = None
