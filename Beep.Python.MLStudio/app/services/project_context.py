"""
Project Context Manager
Centralized configuration and variable management for ML projects.
Each project has a context file that stores:
- Data source configuration
- Column information (names, types, roles)
- Variable mappings
- Preprocessing settings
"""

import json
import os
from pathlib import Path
from typing import Dict, List, Optional, Any
from datetime import datetime


class ProjectContext:
    """
    Manages project-level context including:
    - Variable names (df, X, y, X_train, X_test, etc.)
    - Column configurations
    - Data file paths
    - Preprocessing settings
    """
    
    # Standard variable names used throughout the pipeline
    STANDARD_VARS = {
        'dataframe': 'df',
        'features': 'X',
        'target': 'y',
        'train_features': 'X_train',
        'test_features': 'X_test',
        'train_target': 'y_train',
        'test_target': 'y_test',
        'scaled_train': 'X_train_scaled',
        'scaled_test': 'X_test_scaled',
        'model': 'model',
        'predictions': 'y_pred',
        'metrics': 'metrics'
    }
    
    def __init__(self, project_path: Path):
        self.project_path = Path(project_path)
        self.context_file = self.project_path / 'project_context.json'
        self._context = self._load_or_create()
    
    def _load_or_create(self) -> Dict:
        """Load existing context or create default"""
        if self.context_file.exists():
            try:
                with open(self.context_file, 'r') as f:
                    return json.load(f)
            except (json.JSONDecodeError, IOError):
                pass
        
        return self._create_default_context()
    
    def _create_default_context(self) -> Dict:
        """Create default project context"""
        return {
            'version': '1.0',
            'created_at': datetime.utcnow().isoformat(),
            'updated_at': datetime.utcnow().isoformat(),
            
            # Data source configuration
            'data_source': {
                'file_path': None,
                'delimiter': ',',
                'has_header': True,
                'encoding': 'utf-8'
            },
            
            # Column information
            'columns': {
                'all': [],           # List of all column names
                'numeric': [],       # Numeric columns
                'categorical': [],   # Categorical/string columns
                'target': None,      # Target column name
                'features': [],      # Feature column names
                'id_column': None,   # ID column (to exclude from features)
                'drop': []           # Columns to drop
            },
            
            # Column metadata (dtype, null count, etc.)
            'column_metadata': {},
            
            # Variable mappings - standard names for generated code
            'variables': dict(self.STANDARD_VARS),
            
            # Preprocessing configuration
            'preprocessing': {
                'handle_missing': {
                    'enabled': True,
                    'strategy': 'fill_median',  # drop, fill_mean, fill_median, fill_mode, fill_constant
                    'fill_value': None
                },
                'encode_categorical': {
                    'enabled': True,
                    'method': 'label',  # label, onehot, ordinal
                    'max_categories': 10
                },
                'scale_features': {
                    'enabled': True,
                    'method': 'standard',  # standard, minmax, robust
                    'with_mean': True,
                    'with_std': True
                },
                'drop_high_cardinality': {
                    'enabled': True,
                    'threshold': 10
                }
            },
            
            # Train/test split configuration
            'split': {
                'test_size': 0.2,
                'random_state': 42,
                'shuffle': True,
                'stratify': True
            },
            
            # Model configuration
            'model': {
                'type': None,  # classifier, regressor
                'algorithm': None,
                'params': {}
            },
            
            # Workflow metadata
            'workflows': {}
        }
    
    def save(self):
        """Save context to file"""
        self._context['updated_at'] = datetime.utcnow().isoformat()
        self.project_path.mkdir(parents=True, exist_ok=True)
        with open(self.context_file, 'w') as f:
            json.dump(self._context, f, indent=2)
    
    # ============== Data Source ==============
    
    def set_data_source(self, file_path: str, delimiter: str = ',', 
                        has_header: bool = True, encoding: str = 'utf-8'):
        """Set the data source file"""
        self._context['data_source'] = {
            'file_path': file_path,
            'delimiter': delimiter,
            'has_header': has_header,
            'encoding': encoding
        }
        self.save()
    
    def get_data_source(self) -> Dict:
        """Get data source configuration"""
        return self._context.get('data_source', {})
    
    # ============== Columns ==============
    
    def set_columns(self, all_columns: List[str], numeric: List[str] = None,
                    categorical: List[str] = None):
        """Set column information"""
        self._context['columns']['all'] = all_columns
        if numeric is not None:
            self._context['columns']['numeric'] = numeric
        if categorical is not None:
            self._context['columns']['categorical'] = categorical
        self.save()
    
    def set_target(self, target_column: str):
        """Set the target column"""
        self._context['columns']['target'] = target_column
        self.save()
    
    def set_features(self, feature_columns: List[str]):
        """Set the feature columns"""
        self._context['columns']['features'] = feature_columns
        self.save()
    
    def set_id_column(self, id_column: str):
        """Set the ID column (to be excluded from features)"""
        self._context['columns']['id_column'] = id_column
        self.save()
    
    def set_drop_columns(self, columns: List[str]):
        """Set columns to drop"""
        self._context['columns']['drop'] = columns
        self.save()
    
    def get_columns(self) -> Dict:
        """Get all column configuration"""
        return self._context.get('columns', {})
    
    def set_column_metadata(self, column_name: str, metadata: Dict):
        """Set metadata for a specific column"""
        if 'column_metadata' not in self._context:
            self._context['column_metadata'] = {}
        self._context['column_metadata'][column_name] = metadata
        self.save()
    
    # ============== Variables ==============
    
    def get_variable(self, key: str) -> str:
        """Get a standard variable name"""
        return self._context.get('variables', {}).get(key, self.STANDARD_VARS.get(key))
    
    def set_variable(self, key: str, name: str):
        """Set a custom variable name"""
        if 'variables' not in self._context:
            self._context['variables'] = dict(self.STANDARD_VARS)
        self._context['variables'][key] = name
        self.save()
    
    def get_all_variables(self) -> Dict[str, str]:
        """Get all variable mappings"""
        return self._context.get('variables', dict(self.STANDARD_VARS))
    
    # ============== Preprocessing ==============
    
    def set_preprocessing(self, config: Dict):
        """Set preprocessing configuration"""
        self._context['preprocessing'].update(config)
        self.save()
    
    def get_preprocessing(self) -> Dict:
        """Get preprocessing configuration"""
        return self._context.get('preprocessing', {})
    
    # ============== Split ==============
    
    def set_split_config(self, test_size: float = 0.2, random_state: int = 42,
                         shuffle: bool = True, stratify: bool = True):
        """Set train/test split configuration"""
        self._context['split'] = {
            'test_size': test_size,
            'random_state': random_state,
            'shuffle': shuffle,
            'stratify': stratify
        }
        self.save()
    
    def get_split_config(self) -> Dict:
        """Get split configuration"""
        return self._context.get('split', {})
    
    # ============== Model ==============
    
    def set_model_config(self, model_type: str, algorithm: str, params: Dict = None):
        """Set model configuration"""
        self._context['model'] = {
            'type': model_type,
            'algorithm': algorithm,
            'params': params or {}
        }
        self.save()
    
    def get_model_config(self) -> Dict:
        """Get model configuration"""
        return self._context.get('model', {})
    
    # ============== Code Generation Helpers ==============
    
    def generate_imports(self) -> str:
        """Generate standard imports for the pipeline"""
        imports = [
            "import pandas as pd",
            "import numpy as np",
            "import os",
            "import pickle",
            "from sklearn.model_selection import train_test_split",
            "from sklearn.preprocessing import StandardScaler, LabelEncoder",
            "from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score"
        ]
        
        model_config = self.get_model_config()
        if model_config.get('algorithm'):
            algo = model_config['algorithm']
            if 'RandomForest' in algo:
                if 'Classifier' in algo:
                    imports.append("from sklearn.ensemble import RandomForestClassifier")
                else:
                    imports.append("from sklearn.ensemble import RandomForestRegressor")
        
        return '\n'.join(imports)
    
    def generate_data_loading_code(self) -> str:
        """Generate code to load data"""
        ds = self.get_data_source()
        df_var = self.get_variable('dataframe')
        
        if not ds.get('file_path'):
            return f"# No data source configured\n{df_var} = pd.DataFrame()"
        
        delimiter = ds.get('delimiter', ',')
        header = '0' if ds.get('has_header', True) else 'None'
        
        code = f"""{df_var} = pd.read_csv('{ds['file_path']}', delimiter='{delimiter}', header={header})
print(f'Loaded dataset: {{{df_var}}}.shape = {{{{{df_var}}}.shape}}')
print(f'Columns: {{list({df_var}.columns)}}')
"""
        return code
    
    def generate_preprocessing_code(self) -> str:
        """Generate preprocessing code based on configuration"""
        df_var = self.get_variable('dataframe')
        prep = self.get_preprocessing()
        code_lines = []
        
        code_lines.append(f"# === Data Preprocessing ===")
        code_lines.append(f"print(f'Original shape: {{{df_var}.shape}}')")
        
        # Handle missing values
        if prep.get('handle_missing', {}).get('enabled', True):
            strategy = prep['handle_missing'].get('strategy', 'fill_median')
            code_lines.append(f"\n# Handle missing values (strategy: {strategy})")
            code_lines.append(f"numeric_cols = {df_var}.select_dtypes(include=[np.number]).columns.tolist()")
            
            if strategy == 'drop':
                code_lines.append(f"{df_var} = {df_var}.dropna()")
            elif strategy == 'fill_mean':
                code_lines.append(f"{df_var}[numeric_cols] = {df_var}[numeric_cols].fillna({df_var}[numeric_cols].mean())")
            elif strategy == 'fill_median':
                code_lines.append(f"{df_var}[numeric_cols] = {df_var}[numeric_cols].fillna({df_var}[numeric_cols].median())")
            elif strategy == 'fill_mode':
                code_lines.append(f"for col in numeric_cols:")
                code_lines.append(f"    mode_val = {df_var}[col].mode()")
                code_lines.append(f"    if len(mode_val) > 0:")
                code_lines.append(f"        {df_var}[col] = {df_var}[col].fillna(mode_val[0])")
        
        # Drop high cardinality
        if prep.get('drop_high_cardinality', {}).get('enabled', True):
            threshold = prep['drop_high_cardinality'].get('threshold', 10)
            code_lines.append(f"\n# Drop high cardinality categorical columns (>{threshold} unique values)")
            code_lines.append(f"cat_cols = {df_var}.select_dtypes(include=['object', 'category']).columns.tolist()")
            code_lines.append(f"high_card_cols = [c for c in cat_cols if {df_var}[c].nunique() > {threshold}]")
            code_lines.append(f"if high_card_cols:")
            code_lines.append(f"    print(f'Dropping high cardinality columns: {{high_card_cols}}')")
            code_lines.append(f"    {df_var} = {df_var}.drop(columns=high_card_cols)")
        
        # Encode categorical
        if prep.get('encode_categorical', {}).get('enabled', True):
            method = prep['encode_categorical'].get('method', 'label')
            code_lines.append(f"\n# Encode categorical columns (method: {method})")
            code_lines.append(f"cat_cols = {df_var}.select_dtypes(include=['object', 'category']).columns.tolist()")
            code_lines.append(f"if cat_cols:")
            code_lines.append(f"    label_encoders = {{}}")
            code_lines.append(f"    for col in cat_cols:")
            code_lines.append(f"        le = LabelEncoder()")
            code_lines.append(f"        {df_var}[col] = {df_var}[col].fillna('__MISSING__').astype(str)")
            code_lines.append(f"        {df_var}[col] = le.fit_transform({df_var}[col])")
            code_lines.append(f"        label_encoders[col] = le")
            code_lines.append(f"    print(f'Encoded {{len(cat_cols)}} categorical columns')")
        
        code_lines.append(f"\n# Final cleanup")
        code_lines.append(f"{df_var} = {df_var}.dropna()")
        code_lines.append(f"print(f'Final shape after preprocessing: {{{df_var}.shape}}')")
        
        return '\n'.join(code_lines)
    
    def generate_feature_selection_code(self) -> str:
        """Generate code for feature/target selection"""
        cols = self.get_columns()
        df_var = self.get_variable('dataframe')
        x_var = self.get_variable('features')
        y_var = self.get_variable('target')
        
        target = cols.get('target')
        features = cols.get('features', [])
        
        if not target:
            return "# No target column configured"
        
        code_lines = []
        code_lines.append("# === Select Features and Target ===")
        code_lines.append(f"{y_var} = {df_var}['{target}'].copy()")
        
        if features:
            feature_list = str(features)
            code_lines.append(f"{x_var} = {df_var}[{feature_list}].copy()")
        else:
            code_lines.append(f"{x_var} = {df_var}.drop(columns=['{target}']).copy()")
        
        code_lines.append(f"print(f'Features shape: {{{x_var}.shape}}, Target shape: {{{y_var}.shape}}')")
        
        return '\n'.join(code_lines)
    
    def generate_split_code(self) -> str:
        """Generate train/test split code"""
        split = self.get_split_config()
        vars_ = self.get_all_variables()
        
        x_var = vars_.get('features', 'X')
        y_var = vars_.get('target', 'y')
        x_train = vars_.get('train_features', 'X_train')
        x_test = vars_.get('test_features', 'X_test')
        y_train = vars_.get('train_target', 'y_train')
        y_test = vars_.get('test_target', 'y_test')
        
        test_size = split.get('test_size', 0.2)
        random_state = split.get('random_state', 42)
        shuffle = split.get('shuffle', True)
        stratify = f", stratify={y_var}" if split.get('stratify', True) else ""
        
        code = f"""# === Train/Test Split ===
{x_train}, {x_test}, {y_train}, {y_test} = train_test_split(
    {x_var}, {y_var}, test_size={test_size}, random_state={random_state}, shuffle={shuffle}{stratify}
)
print(f'Training set: {{{x_train}.shape}}, Test set: {{{x_test}.shape}}')
"""
        return code
    
    def to_dict(self) -> Dict:
        """Return the full context as a dictionary"""
        return self._context.copy()
    
    @classmethod
    def from_dict(cls, project_path: Path, data: Dict) -> 'ProjectContext':
        """Create a ProjectContext from a dictionary"""
        ctx = cls(project_path)
        ctx._context = data
        ctx.save()
        return ctx


# Global cache of project contexts
_context_cache: Dict[str, ProjectContext] = {}


def get_project_context(project_path: Path) -> ProjectContext:
    """Get or create a project context (cached)"""
    path_str = str(project_path)
    if path_str not in _context_cache:
        _context_cache[path_str] = ProjectContext(project_path)
    return _context_cache[path_str]


def clear_context_cache():
    """Clear the context cache"""
    _context_cache.clear()

