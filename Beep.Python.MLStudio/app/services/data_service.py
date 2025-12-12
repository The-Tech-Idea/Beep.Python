"""
Data Service
Handles dataset upload, validation, and preprocessing
"""
import os
import pandas as pd
import json
import logging
from pathlib import Path
from typing import Dict, Any, Optional, List
from werkzeug.utils import secure_filename

logger = logging.getLogger(__name__)


class DataService:
    """Service for data management"""
    
    ALLOWED_EXTENSIONS = {'csv', 'json', 'xlsx', 'xls', 'parquet'}
    
    def __init__(self, upload_folder: str = None):
        """
        Initialize Data Service
        
        Args:
            upload_folder: Base folder for uploaded datasets (optional, uses settings if not provided)
        """
        from app.services.settings_manager import get_settings_manager
        settings_mgr = get_settings_manager()
        
        if upload_folder:
            self.upload_folder = Path(upload_folder)
        else:
            self.upload_folder = settings_mgr.get_data_folder()
            if not self.upload_folder.is_absolute():
                self.upload_folder = Path('.').resolve() / self.upload_folder
        
        self.upload_folder.mkdir(parents=True, exist_ok=True)
    
    def is_allowed_file(self, filename: str) -> bool:
        """Check if file extension is allowed"""
        return '.' in filename and \
               filename.rsplit('.', 1)[1].lower() in self.ALLOWED_EXTENSIONS
    
    def save_uploaded_file(self, file, project_id: int, filename: Optional[str] = None) -> str:
        """
        Save uploaded file to project data folder
        
        Args:
            file: File object from Flask request
            project_id: Project ID
            filename: Optional custom filename
            
        Returns:
            Path to saved file
        """
        if not filename:
            filename = secure_filename(file.filename)
        
        project_data_dir = self.upload_folder / f"project_{project_id}"
        project_data_dir.mkdir(parents=True, exist_ok=True)
        
        file_path = project_data_dir / filename
        file.save(str(file_path))
        
        return str(file_path)
    
    def load_dataset(self, file_path: str, **kwargs) -> pd.DataFrame:
        """
        Load dataset from file
        
        Args:
            file_path: Path to dataset file
            **kwargs: Additional arguments for pandas read functions
            
        Returns:
            DataFrame
        """
        file_path = Path(file_path)
        
        if not file_path.exists():
            raise FileNotFoundError(f"File not found: {file_path}")
        
        extension = file_path.suffix.lower()
        
        if extension == '.csv':
            return pd.read_csv(file_path, **kwargs)
        elif extension in ['.xlsx', '.xls']:
            return pd.read_excel(file_path, **kwargs)
        elif extension == '.json':
            return pd.read_json(file_path, **kwargs)
        elif extension == '.parquet':
            return pd.read_parquet(file_path, **kwargs)
        else:
            raise ValueError(f"Unsupported file format: {extension}")
    
    def get_dataset_info(self, file_path: str) -> Dict[str, Any]:
        """
        Get information about a dataset
        
        Args:
            file_path: Path to dataset file
            
        Returns:
            Dictionary with dataset information
        """
        try:
            df = self.load_dataset(file_path)
            
            return {
                'rows': len(df),
                'columns': len(df.columns),
                'column_names': df.columns.tolist(),
                'dtypes': df.dtypes.astype(str).to_dict(),
                'missing_values': df.isnull().sum().to_dict(),
                'memory_usage_mb': df.memory_usage(deep=True).sum() / 1024 / 1024,
                'shape': df.shape,
                'sample': df.head(10).to_dict('records')
            }
        except Exception as e:
            logger.error(f"Error getting dataset info: {e}")
            return {
                'error': str(e)
            }
    
    def validate_dataset(self, file_path: str) -> Dict[str, Any]:
        """
        Validate dataset for ML training
        
        Args:
            file_path: Path to dataset file
            
        Returns:
            Validation result dictionary
        """
        try:
            df = self.load_dataset(file_path)
            
            issues = []
            warnings = []
            
            # Check for empty dataset
            if len(df) == 0:
                issues.append("Dataset is empty")
            
            # Check for too many missing values
            missing_pct = df.isnull().sum() / len(df) * 100
            high_missing = missing_pct[missing_pct > 50]
            if len(high_missing) > 0:
                warnings.append(f"Columns with >50% missing values: {', '.join(high_missing.index)}")
            
            # Check for duplicate rows
            duplicates = df.duplicated().sum()
            if duplicates > 0:
                warnings.append(f"{duplicates} duplicate rows found")
            
            return {
                'valid': len(issues) == 0,
                'issues': issues,
                'warnings': warnings,
                'rows': len(df),
                'columns': len(df.columns)
            }
        except Exception as e:
            return {
                'valid': False,
                'issues': [str(e)],
                'warnings': []
            }

