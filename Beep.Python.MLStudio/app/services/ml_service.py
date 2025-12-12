"""
ML Service
Handles ML model training, evaluation, and prediction
"""
import os
import subprocess
import json
import logging
import pickle
import platform
from pathlib import Path
from typing import Dict, Any, Optional, List
from datetime import datetime

logger = logging.getLogger(__name__)


class MLService:
    """Service for ML model operations"""
    
    def __init__(self, projects_folder: str = None, environment_manager=None):
        """
        Initialize ML Service
        
        Args:
            projects_folder: Base folder for ML projects (optional, uses settings if not provided)
            environment_manager: EnvironmentManager instance (optional, creates if not provided)
        """
        from app.services.settings_manager import get_settings_manager
        settings_mgr = get_settings_manager()
        
        if projects_folder:
            self.projects_folder = Path(projects_folder)
        else:
            self.projects_folder = settings_mgr.get_projects_folder()
            if not self.projects_folder.is_absolute():
                self.projects_folder = Path('.').resolve() / self.projects_folder
        
        self.projects_folder.mkdir(parents=True, exist_ok=True)
        
        if environment_manager:
            self.env_manager = environment_manager
        else:
            from app.services.environment_manager import EnvironmentManager
            self.env_manager = EnvironmentManager()
    
    def get_project_path(self, project_id: int) -> Path:
        """Get project directory path"""
        return self.projects_folder / f"project_{project_id}"
    
    def create_project_structure(self, project_id: int, project_name: str):
        """Create directory structure for a new project"""
        project_path = self.get_project_path(project_id)
        project_path.mkdir(parents=True, exist_ok=True)
        
        # Create subdirectories
        (project_path / 'data').mkdir(exist_ok=True)
        (project_path / 'models').mkdir(exist_ok=True)
        (project_path / 'notebooks').mkdir(exist_ok=True)
        (project_path / 'scripts').mkdir(exist_ok=True)
        
        # Create default files
        self._create_default_files(project_path, project_name)
    
    def _create_default_files(self, project_path: Path, project_name: str):
        """Create default project files"""
        # Create README
        readme_content = f"""# {project_name}

This is an ML project created with Beep.Python.MLStudio.

## Project Structure

- `data/` - Datasets
- `models/` - Trained models
- `notebooks/` - Jupyter notebooks
- `scripts/` - Python scripts

## Getting Started

1. Upload your dataset to the `data/` folder
2. Create a training script or notebook
3. Train your model
4. Evaluate and compare results
"""
        (project_path / 'README.md').write_text(readme_content)
        
        # Create example training script
        example_script = """# Example Training Script
# This is a template for training ML models

import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, classification_report
import pickle

# Load data
# df = pd.read_csv('data/your_dataset.csv')

# Preprocess data
# X = df.drop('target', axis=1)
# y = df['target']

# Split data
# X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

# Train model
# model = RandomForestClassifier(n_estimators=100, random_state=42)
# model.fit(X_train, y_train)

# Evaluate
# y_pred = model.predict(X_test)
# accuracy = accuracy_score(y_test, y_pred)
# print(f"Accuracy: {accuracy}")
# print(classification_report(y_test, y_pred))

# Save model
# with open('models/model.pkl', 'wb') as f:
#     pickle.dump(model, f)
"""
        (project_path / 'scripts' / 'train_example.py').write_text(example_script)
    
    def train_model(self, project_id: int, experiment_id: int, 
                   script_path: str, env_name: str) -> Dict[str, Any]:
        """
        Train a model using a Python script in the project's environment
        
        Args:
            project_id: Project ID
            experiment_id: Experiment ID
            script_path: Path to training script (relative to project)
            env_name: Virtual environment name
            
        Returns:
            Training result dictionary
        """
        project_path = self.get_project_path(project_id)
        script_full_path = project_path / script_path
        
        if not script_full_path.exists():
            raise FileNotFoundError(f"Script not found: {script_path}")
        
        # Get Python executable from environment
        env = self.env_manager.providers_path / env_name
        if platform.system() == "Windows":
            python_exe = env / "Scripts" / "python.exe"
        else:
            python_exe = env / "bin" / "python"
        
        if not python_exe.exists():
            raise RuntimeError(f"Environment '{env_name}' not found or Python executable not available")
        
        python_exe = str(python_exe)
        
        # Run training script
        try:
            result = subprocess.run(
                [python_exe, str(script_full_path)],
                cwd=str(project_path),
                capture_output=True,
                text=True,
                timeout=3600  # 1 hour timeout
            )
            
            return {
                'success': result.returncode == 0,
                'stdout': result.stdout,
                'stderr': result.stderr,
                'returncode': result.returncode
            }
        except subprocess.TimeoutExpired:
            return {
                'success': False,
                'stdout': '',
                'stderr': 'Training timeout (exceeded 1 hour)',
                'returncode': -1
            }
        except Exception as e:
            return {
                'success': False,
                'stdout': '',
                'stderr': str(e),
                'returncode': -1
            }
    
    def save_model(self, project_id: int, model_name: str, model_object: Any):
        """Save a trained model to disk"""
        project_path = self.get_project_path(project_id)
        models_dir = project_path / 'models'
        models_dir.mkdir(parents=True, exist_ok=True)
        
        model_path = models_dir / f"{model_name}.pkl"
        with open(model_path, 'wb') as f:
            pickle.dump(model_object, f)
        
        return str(model_path)
    
    def load_model(self, project_id: int, model_name: str) -> Any:
        """Load a trained model from disk"""
        project_path = self.get_project_path(project_id)
        model_path = project_path / 'models' / f"{model_name}.pkl"
        
        if not model_path.exists():
            raise FileNotFoundError(f"Model not found: {model_name}")
        
        with open(model_path, 'rb') as f:
            return pickle.load(f)
    
    def list_models(self, project_id: int) -> List[str]:
        """List all saved models in a project"""
        project_path = self.get_project_path(project_id)
        models_dir = project_path / 'models'
        
        if not models_dir.exists():
            return []
        
        models = [f.stem for f in models_dir.glob('*.pkl')]
        return models

