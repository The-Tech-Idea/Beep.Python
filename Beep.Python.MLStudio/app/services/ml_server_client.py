"""
ML Server Client - Publishes models from ML Studio to Host Admin ML Server

This service allows ML Studio to:
1. Connect to Beep AI Server (Host Admin)
2. Upload/publish trained models
3. Check model status
4. Make predictions via the server
"""
import os
import json
import requests
import logging
from pathlib import Path
from typing import Dict, List, Optional, Any
from dataclasses import dataclass

logger = logging.getLogger(__name__)


@dataclass
class PublishResult:
    """Result of model publishing"""
    success: bool
    model_id: Optional[str] = None
    version_id: Optional[str] = None
    api_endpoint: Optional[str] = None
    error: Optional[str] = None
    message: Optional[str] = None


class MLServerClient:
    """
    Client for communicating with Beep AI Server (Host Admin) ML API
    
    Allows ML Studio to publish trained models to the server for
    production use by other applications.
    """
    
    def __init__(self, base_url: str = None, api_key: Optional[str] = None):
        """
        Initialize ML Server client
        
        Args:
            base_url: Base URL of Host Admin (e.g., http://127.0.0.1:5000)
            api_key: Optional API key for authentication
        """
        # Default to localhost if not specified
        self.base_url = (base_url or "http://127.0.0.1:5000").rstrip('/')
        self.api_key = api_key
        self.session = requests.Session()
        self.session.headers.update({'User-Agent': 'BeepMLStudio/1.0'})
        
        if api_key:
            self.session.headers.update({'Authorization': f'Bearer {api_key}'})
    
    def _request(self, method: str, endpoint: str, **kwargs) -> Dict[str, Any]:
        """Make HTTP request to Host Admin ML API"""
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
            return {'success': False, 'error': f"Cannot connect to ML Server at {self.base_url}. Is it running?"}
        except requests.exceptions.Timeout:
            return {'success': False, 'error': "Request timed out"}
        except Exception as e:
            return {'success': False, 'error': str(e)}
    
    def health_check(self) -> bool:
        """Check if ML Server is accessible"""
        try:
            result = self._request('GET', '/api/v1/health')
            return result.get('status') == 'ok' or result.get('success', False)
        except:
            return False
    
    def get_server_info(self) -> Dict[str, Any]:
        """Get ML Server information and status"""
        result = self._request('GET', '/api/v1/ml-models/env/status')
        return result
    
    def list_models(self, is_public: bool = None) -> Dict[str, Any]:
        """
        List models on the server
        
        Args:
            is_public: Filter by public/private status
        
        Returns:
            Dict with models list
        """
        params = {}
        if is_public is not None:
            params['is_public'] = str(is_public).lower()
        
        return self._request('GET', '/api/v1/ml-models', params=params)
    
    def get_model(self, model_id: str) -> Dict[str, Any]:
        """Get model details"""
        return self._request('GET', f'/api/v1/ml-models/{model_id}')
    
    def publish_model(
        self,
        model_file_path: str,
        name: str,
        model_type: str = 'sklearn',
        framework: str = None,
        description: str = None,
        category: str = None,
        tags: List[str] = None,
        is_public: bool = True,
        requirements: List[str] = None,
        python_version: str = None,
        input_schema: Dict = None,
        output_schema: Dict = None
    ) -> PublishResult:
        """
        Publish a trained model to the ML Server
        
        Args:
            model_file_path: Path to the model file (.pkl, .joblib, .h5, etc.)
            name: Display name for the model
            model_type: Type of model (sklearn, tensorflow, pytorch, xgboost, onnx)
            framework: Framework used (scikit-learn, tensorflow, pytorch, etc.)
            description: Model description
            category: Model category (classification, regression, clustering, etc.)
            tags: List of tags
            is_public: Whether model is public
            requirements: List of pip requirements
            python_version: Python version requirement
            input_schema: JSON schema for input data
            output_schema: JSON schema for output data
        
        Returns:
            PublishResult with model_id and api_endpoint
        """
        model_path = Path(model_file_path)
        
        if not model_path.exists():
            return PublishResult(success=False, error=f"Model file not found: {model_file_path}")
        
        # Prepare form data
        form_data = {
            'name': name,
            'model_type': model_type,
            'is_public': str(is_public).lower(),
        }
        
        if framework:
            form_data['framework'] = framework
        if description:
            form_data['description'] = description
        if category:
            form_data['category'] = category
        if tags:
            form_data['tags'] = json.dumps(tags)
        if requirements:
            form_data['requirements'] = ','.join(requirements) if isinstance(requirements, list) else requirements
        if python_version:
            form_data['python_version'] = python_version
        if input_schema:
            form_data['input_schema'] = json.dumps(input_schema)
        if output_schema:
            form_data['output_schema'] = json.dumps(output_schema)
        
        # Open file for upload
        try:
            with open(model_path, 'rb') as f:
                files = {'file': (model_path.name, f, 'application/octet-stream')}
                
                result = self._request(
                    'POST',
                    '/api/v1/ml-models',
                    data=form_data,
                    files=files,
                    timeout=300  # 5 min timeout for upload
                )
            
            if result.get('success'):
                model_id = result.get('model_id')
                return PublishResult(
                    success=True,
                    model_id=model_id,
                    version_id=result.get('version_id'),
                    api_endpoint=f"{self.base_url}/api/v1/ml-models/{model_id}/predict",
                    message=result.get('message', 'Model published successfully')
                )
            else:
                return PublishResult(
                    success=False,
                    error=result.get('error', 'Unknown error during upload')
                )
                
        except Exception as e:
            return PublishResult(success=False, error=str(e))
    
    def generate_api(self, model_id: str) -> Dict[str, Any]:
        """Generate API endpoint for a model"""
        return self._request('POST', f'/api/v1/ml-models/{model_id}/generate-api')
    
    def predict(
        self,
        model_id: str,
        data: Dict[str, Any],
        version: str = None
    ) -> Dict[str, Any]:
        """
        Make a prediction using a model on the server
        
        Args:
            model_id: Model ID on the server
            data: Input data as dictionary
            version: Specific version to use (optional)
        
        Returns:
            Prediction result
        """
        payload = {'data': data}
        if version:
            payload['version'] = version
        
        return self._request(
            'POST',
            f'/api/v1/ml-models/{model_id}/predict',
            json=payload,
            timeout=60
        )
    
    def predict_batch(
        self,
        model_id: str,
        data_list: List[Dict[str, Any]],
        version: str = None
    ) -> Dict[str, Any]:
        """
        Make batch predictions
        
        Args:
            model_id: Model ID on the server
            data_list: List of input data dictionaries
            version: Specific version to use (optional)
        
        Returns:
            Batch prediction results
        """
        payload = {'data': data_list}
        if version:
            payload['version'] = version
        
        return self._request(
            'POST',
            f'/api/v1/ml-models/{model_id}/predict/batch',
            json=payload,
            timeout=300
        )
    
    def delete_model(self, model_id: str) -> Dict[str, Any]:
        """Delete a model from the server"""
        return self._request('DELETE', f'/api/v1/ml-models/{model_id}')
    
    def validate_model(self, model_id: str) -> Dict[str, Any]:
        """Trigger validation for a model"""
        return self._request('POST', f'/api/v1/ml-models/{model_id}/validate')


# Singleton instance
_ml_server_client: Optional[MLServerClient] = None


def get_ml_server_client(base_url: str = None, api_key: str = None) -> MLServerClient:
    """Get or create ML Server client instance"""
    global _ml_server_client
    
    if _ml_server_client is None or base_url:
        # Get URL from settings if not provided
        if not base_url:
            try:
                from app.services.settings_manager import SettingsManager
                settings = SettingsManager()
                base_url = settings.get('ml_server_url', 'http://127.0.0.1:5000')
                api_key = settings.get('ml_server_api_key')
            except:
                base_url = 'http://127.0.0.1:5000'
        
        _ml_server_client = MLServerClient(base_url, api_key)
    
    return _ml_server_client


def reset_ml_server_client():
    """Reset the client (for testing or reconnection)"""
    global _ml_server_client
    _ml_server_client = None

