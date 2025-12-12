"""
Workflow Service
Converts visual workflow to executable Python code with proper execution order
"""
import logging
import re
from typing import Dict, List, Any, Set, Tuple, Optional
from pathlib import Path
from app.services.workflow_executor import WorkflowExecutor
from app.services.project_context import ProjectContext, get_project_context

logger = logging.getLogger(__name__)


class WorkflowService:
    """Service for workflow operations and code generation"""
    
    def __init__(self, projects_folder: str):
        """
        Initialize Workflow Service
        
        Args:
            projects_folder: Base folder for ML projects
        """
        self.projects_folder = Path(projects_folder)
        self.projects_folder.mkdir(parents=True, exist_ok=True)
    
    def get_project_path(self, project_id: int) -> Path:
        """Get project directory path"""
        return self.projects_folder / f"project_{project_id}"
    
    def generate_code_from_workflow(self, workflow_data: Dict[str, Any], project_framework: str = 'scikit-learn', 
                                     project_id: Optional[int] = None) -> str:
        """
        Generate Python code from visual workflow using proper topological sorting
        
        Args:
            workflow_data: Workflow definition with nodes and edges
            project_framework: ML framework being used
            project_id: Optional project ID to use project context for variable names
            
        Returns:
            Generated Python code as string
        """
        nodes = workflow_data.get('nodes', [])
        edges = workflow_data.get('edges', [])
        
        # Get project context if project_id is provided
        context = None
        if project_id:
            try:
                project_path = self.get_project_path(project_id)
                context = get_project_context(project_path)
            except Exception as e:
                logger.warning(f"Could not load project context: {e}")
        
        if not nodes:
            return "# Empty workflow - add nodes to generate code\n"
        
        # Use WorkflowExecutor to determine execution order
        executor = WorkflowExecutor(workflow_data)
        
        # Validate workflow
        is_valid, errors = executor.validate_workflow()
        if not is_valid:
            error_msg = "\n".join(f"# ERROR: {e}" for e in errors)
            return f"""# Workflow Validation Failed
{error_msg}

# Please fix the workflow errors before generating code.
"""
        
        # Get execution order using topological sort
        try:
            execution_order = executor.topological_sort()
        except ValueError as e:
            return f"# Error determining execution order: {e}\n"
        
        # Build node map for quick lookup
        node_map = {node['id']: node for node in nodes}
        
        # Generate code in execution order
        code = self._generate_code_with_execution_order(
            execution_order, 
            nodes, 
            edges, 
            node_map, 
            executor,
            project_framework
        )
        
        return code
    
    def _generate_imports(self, nodes: List[Dict], framework: str) -> str:
        """Generate import statements based on nodes"""
        imports = set()
        
        # Always include these
        imports.add('import pandas as pd')
        imports.add('import numpy as np')
        imports.add('import os')
        
        # Check for specific operations
        node_types = [node.get('type', '') for node in nodes]
        
        if 'train_test_split' in str(node_types):
            imports.add('from sklearn.model_selection import train_test_split')
        
        if 'scaler' in str(node_types) or 'standardize' in str(node_types):
            imports.add('from sklearn.preprocessing import StandardScaler')
        
        if 'encoder' in str(node_types) or 'label_encode' in str(node_types):
            imports.add('from sklearn.preprocessing import LabelEncoder')
        
        if any('classifier' in str(t) or 'regressor' in str(t) for t in node_types):
            if framework == 'scikit-learn':
                imports.add('from sklearn.ensemble import RandomForestClassifier, RandomForestRegressor')
                imports.add('from sklearn.metrics import accuracy_score, classification_report, mean_squared_error, r2_score')
            elif framework == 'tensorflow':
                imports.add('from tensorflow import keras')
                imports.add('from tensorflow.keras import layers')
            elif framework == 'xgboost':
                imports.add('from xgboost import XGBClassifier, XGBRegressor')
        
        imports.add('import pickle')
        
        return '\n'.join(sorted(imports))
    
    def extract_required_packages(self, code: str) -> Set[str]:
        """
        Extract required packages from generated Python code.
        
        Args:
            code: Python code string with import statements
            
        Returns:
            Set of package names that need to be installed
        """
        packages = set()
        
        # Standard library packages that don't need installation
        stdlib_packages = {
            'os', 'sys', 'json', 'pickle', 'datetime', 'time', 'pathlib',
            'collections', 'itertools', 'functools', 'operator', 'math',
            'random', 'string', 're', 'copy', 'fractions', 'decimal'
        }
        
        # Map import names to package names (some differ)
        import_to_package = {
            'pandas': 'pandas',
            'numpy': 'numpy',
            'sklearn': 'scikit-learn',
            'tensorflow': 'tensorflow',
            'keras': 'keras',
            'xgboost': 'xgboost',
            'lightgbm': 'lightgbm',
            'matplotlib': 'matplotlib',
            'seaborn': 'seaborn',
            'plotly': 'plotly',
            'scipy': 'scipy',
            'statsmodels': 'statsmodels',
            'pytorch': 'torch',
            'torch': 'torch',
            'torchvision': 'torchvision'
        }
        
        # Extract import statements
        import_patterns = [
            r'^import\s+(\w+)',  # import pandas
            r'^from\s+(\w+)',   # from sklearn
        ]
        
        for line in code.split('\n'):
            line = line.strip()
            if not line or line.startswith('#'):
                continue
            
            for pattern in import_patterns:
                match = re.match(pattern, line)
                if match:
                    module_name = match.group(1)
                    # Skip standard library
                    if module_name in stdlib_packages:
                        continue
                    # Map to package name
                    package_name = import_to_package.get(module_name, module_name)
                    packages.add(package_name)
        
        return packages
    
    def _generate_data_loading(self, nodes: List[Dict], edges: List[Dict], node_map: Dict) -> str:
        """Generate data loading code"""
        code_lines = []
        
        # Find data source nodes
        data_nodes = [n for n in nodes if n.get('type', '').startswith('data_')]
        
        if not data_nodes:
            code_lines.append("# No data loading nodes found")
            code_lines.append("# df = pd.read_csv('data/your_dataset.csv')")
            return '\n'.join(code_lines)
        
        for node in data_nodes:
            node_type = node.get('type', '')
            node_id = node.get('id', '')
            data = node.get('data', {})
            
            if node_type == 'data_load_csv':
                file_path = data.get('file_path', 'data/your_dataset.csv')
                var_name = data.get('variable_name', 'df')
                code_lines.append(f"{var_name} = pd.read_csv('{file_path}')")
                code_lines.append(f"print(f'Loaded dataset: {var_name}.shape = {{{var_name}.shape}}')")
            
            elif node_type == 'data_load_json':
                file_path = data.get('file_path', 'data/your_dataset.json')
                var_name = data.get('variable_name', 'df')
                code_lines.append(f"{var_name} = pd.read_json('{file_path}')")
                code_lines.append(f"print(f'Loaded dataset: {var_name}.shape = {{{var_name}.shape}}')")
        
        return '\n'.join(code_lines) if code_lines else "# No data loading configured"
    
    def _generate_preprocessing(self, nodes: List[Dict], edges: List[Dict], node_map: Dict) -> str:
        """Generate preprocessing code"""
        code_lines = []
        
        # Find preprocessing nodes
        preprocess_nodes = [n for n in nodes if n.get('type', '').startswith('preprocess_')]
        
        if not preprocess_nodes:
            code_lines.append("# No preprocessing nodes found")
            return '\n'.join(code_lines)
        
        for node in preprocess_nodes:
            node_type = node.get('type', '')
            data = node.get('data', {})
            
            if node_type == 'preprocess_split':
                test_size = data.get('test_size', 0.2)
                random_state = data.get('random_state', 42)
                code_lines.append(f"X_train, X_test, y_train, y_test = train_test_split(")
                code_lines.append(f"    X, y, test_size={test_size}, random_state={random_state}")
                code_lines.append(f")")
            
            elif node_type == 'preprocess_scale':
                # Each node is self-contained - use unique scaler name based on node ID
                node_id = node.get('id', 'default')
                # Sanitize node ID for use as variable name (Python identifier)
                import re
                node_id_sanitized = re.sub(r'[^a-zA-Z0-9_]', '_', node_id)
                scaler_name = f"scaler_{node_id_sanitized}"
                with_mean = data.get('with_mean', True)
                with_std = data.get('with_std', True)
                
                # Each node is self-contained - handles ONLY numeric columns
                code_lines.append(f"# Standard Scaler Node ({node_id})")
                code_lines.append("from sklearn.preprocessing import StandardScaler")
                code_lines.append("import pandas as pd")
                code_lines.append("import numpy as np")
                code_lines.append("")
                code_lines.append("# Select only numeric columns for scaling")
                code_lines.append("if isinstance(X_train, pd.DataFrame):")
                code_lines.append("    numeric_cols = X_train.select_dtypes(include=[np.number]).columns.tolist()")
                code_lines.append("    if len(numeric_cols) == 0:")
                code_lines.append("        raise ValueError('No numeric columns found. StandardScaler requires numeric data.')")
                code_lines.append("    print(f'Scaling {len(numeric_cols)} numeric columns: {numeric_cols}')")
                code_lines.append(f"    {scaler_name} = StandardScaler(with_mean={with_mean}, with_std={with_std})")
                code_lines.append("    X_train_scaled = X_train.copy()")
                code_lines.append("    X_test_scaled = X_test.copy()")
                code_lines.append(f"    X_train_scaled[numeric_cols] = {scaler_name}.fit_transform(X_train[numeric_cols])")
                code_lines.append(f"    X_test_scaled[numeric_cols] = {scaler_name}.transform(X_test[numeric_cols])")
                code_lines.append("else:")
                code_lines.append(f"    {scaler_name} = StandardScaler(with_mean={with_mean}, with_std={with_std})")
                code_lines.append(f"    X_train_scaled = {scaler_name}.fit_transform(X_train)")
                code_lines.append(f"    X_test_scaled = {scaler_name}.transform(X_test)")
                code_lines.append("print(f'Scaled X_train: {X_train.shape} -> {X_train_scaled.shape}')")
                code_lines.append("print(f'Scaled X_test: {X_test.shape} -> {X_test_scaled.shape}')")
            
            elif node_type == 'preprocess_encode':
                code_lines.append("label_encoder = LabelEncoder()")
                code_lines.append("# y_train = label_encoder.fit_transform(y_train)")
                code_lines.append("# y_test = label_encoder.transform(y_test)")
        
        return '\n'.join(code_lines) if code_lines else "# No preprocessing configured"
    
    def _generate_model_training(self, nodes: List[Dict], edges: List[Dict], node_map: Dict, framework: str) -> str:
        """Generate model training code"""
        code_lines = []
        
        # Find model nodes
        model_nodes = [n for n in nodes if 'model' in n.get('type', '').lower() or 'train' in n.get('type', '').lower()]
        
        if not model_nodes:
            code_lines.append("# No model training nodes found")
            code_lines.append("# model = RandomForestClassifier(n_estimators=100, random_state=42)")
            code_lines.append("# model.fit(X_train, y_train)")
            return '\n'.join(code_lines)
        
        for node in model_nodes:
            node_type = node.get('type', '')
            data = node.get('data', {})
            
            if framework == 'scikit-learn':
                if 'classifier' in node_type:
                    n_estimators = data.get('n_estimators', 100)
                    random_state = data.get('random_state', 42)
                    code_lines.append(f"model = RandomForestClassifier(n_estimators={n_estimators}, random_state={random_state})")
                elif 'regressor' in node_type:
                    n_estimators = data.get('n_estimators', 100)
                    random_state = data.get('random_state', 42)
                    code_lines.append(f"model = RandomForestRegressor(n_estimators={n_estimators}, random_state={random_state})")
                code_lines.append("model.fit(X_train, y_train)")
                code_lines.append("print('Model trained successfully!')")
            
            elif framework == 'tensorflow':
                code_lines.append("# Build neural network")
                code_lines.append("model = keras.Sequential([")
                code_lines.append("    layers.Dense(64, activation='relu', input_shape=(X_train.shape[1],)),")
                code_lines.append("    layers.Dense(32, activation='relu'),")
                code_lines.append("    layers.Dense(1, activation='sigmoid')")
                code_lines.append("])")
                code_lines.append("model.compile(optimizer='adam', loss='binary_crossentropy', metrics=['accuracy'])")
                code_lines.append("model.fit(X_train, y_train, epochs=10, batch_size=32, validation_split=0.2)")
            
            elif framework == 'xgboost':
                code_lines.append("model = XGBClassifier(n_estimators=100, random_state=42)")
                code_lines.append("model.fit(X_train, y_train)")
        
        return '\n'.join(code_lines) if code_lines else "# No model training configured"
    
    def _generate_evaluation(self, nodes: List[Dict], edges: List[Dict], node_map: Dict) -> str:
        """Generate evaluation code"""
        code_lines = []
        
        # Find evaluation nodes
        eval_nodes = [n for n in nodes if 'evaluate' in n.get('type', '').lower() or 'metric' in n.get('type', '').lower()]
        
        if not eval_nodes:
            code_lines.append("# No evaluation nodes found")
            code_lines.append("# y_pred = model.predict(X_test)")
            code_lines.append("# accuracy = accuracy_score(y_test, y_pred)")
            code_lines.append("# print(f'Accuracy: {accuracy:.4f}')")
            return '\n'.join(code_lines)
        
        code_lines.append("y_pred = model.predict(X_test)")
        code_lines.append("# Calculate metrics")
        code_lines.append("# accuracy = accuracy_score(y_test, y_pred)")
        code_lines.append("# print(f'Accuracy: {accuracy:.4f}')")
        code_lines.append("# print(classification_report(y_test, y_pred))")
        
        return '\n'.join(code_lines) if code_lines else "# No evaluation configured"
    
    def _generate_model_saving(self, nodes: List[Dict], edges: List[Dict], node_map: Dict) -> str:
        """Generate model saving code"""
        code_lines = []
        
        # Find save nodes
        save_nodes = [n for n in nodes if 'save' in n.get('type', '').lower()]
        
        if not save_nodes:
            code_lines.append("# No model saving nodes found")
            code_lines.append("# os.makedirs('models', exist_ok=True)")
            code_lines.append("# with open('models/model.pkl', 'wb') as f:")
            code_lines.append("#     pickle.dump(model, f)")
            return '\n'.join(code_lines)
        
        code_lines.append("os.makedirs('models', exist_ok=True)")
        code_lines.append("with open('models/model.pkl', 'wb') as f:")
        code_lines.append("    pickle.dump(model, f)")
        code_lines.append("print('Model saved successfully!')")
        
        return '\n'.join(code_lines) if code_lines else "# No model saving configured"
    
    def _generate_code_from_start_nodes(self, start_nodes: List[Dict], nodes: List[Dict], 
                                       edges: List[Dict], node_map: Dict, framework: str) -> str:
        """
        Generate code starting from Start nodes, following the workflow graph.
        
        Args:
            start_nodes: List of Start node definitions
            nodes: All nodes in the workflow
            edges: All edges (connections) in the workflow
            node_map: Dictionary mapping node IDs to node definitions
            framework: ML framework being used
            
        Returns:
            Generated Python code as string
        """
        from collections import deque
        
        # Build adjacency list for graph traversal
        graph = {node['id']: [] for node in nodes}
        for edge in edges:
            source = edge.get('source')
            target = edge.get('target')
            if source and target:
                graph[source].append(target)
        
        # Track visited nodes and generated code
        visited = set()
        code_sections = []
        imports_set = set()
        
        # Start from each Start node
        for start_node in start_nodes:
            start_id = start_node['id']
            if start_id in visited:
                continue
            
            # BFS traversal from Start node
            queue = deque([start_id])
            visited.add(start_id)
            
            while queue:
                current_id = queue.popleft()
                current_node = node_map.get(current_id)
                
                if not current_node:
                    continue
                
                # Generate code for this node
                node_code = self._generate_node_code(current_node, framework)
                if node_code:
                    code_sections.append(node_code)
                
                # Add imports for this node
                node_imports = self._get_node_imports(current_node, framework)
                imports_set.update(node_imports)
                
                # Visit connected nodes
                for next_id in graph.get(current_id, []):
                    if next_id not in visited:
                        visited.add(next_id)
                        queue.append(next_id)
        
        # Combine imports
        imports = '\n'.join(sorted(imports_set)) if imports_set else self._generate_imports(nodes, framework)
        
        # Combine all code
        code = f"""# Generated ML Pipeline Code
# This code was automatically generated from a visual workflow
# Pipeline starts from Start node(s)

{imports}

# ============================================================================
# Pipeline Execution
# ============================================================================

{chr(10).join(code_sections)}
"""
        return code
    
    def _generate_node_code(self, node: Dict, framework: str) -> str:
        """Generate code for a single node based on its type"""
        node_type = node.get('type', '')
        data = node.get('data', {})
        
        # Start node
        if node_type == 'start':
            message = data.get('message', 'Workflow started')
            return f"""# Start: {message}
print('{message}')
print('=' * 50)
"""
        
        # Data loading nodes
        if node_type == 'data_load_csv':
            file_path = data.get('file_path', 'data/your_dataset.csv')
            var_name = data.get('variable_name', 'df')
            delimiter = data.get('delimiter', ',')
            header = data.get('header', True)
            header_param = '0' if header else 'None'
            return f"""{var_name} = pd.read_csv('{file_path}', delimiter='{delimiter}', header={header_param})
print(f'Loaded dataset: {var_name}.shape = {{{var_name}.shape}}')
"""
        
        if node_type == 'data_load_json':
            file_path = data.get('file_path', 'data/your_dataset.json')
            var_name = data.get('variable_name', 'df')
            orient = data.get('orient', 'records')
            return f"""{var_name} = pd.read_json('{file_path}', orient='{orient}')
print(f'Loaded dataset: {var_name}.shape = {{{var_name}.shape}}')
"""
        
        if node_type == 'data_load_excel':
            file_path = data.get('file_path', 'data/your_dataset.xlsx')
            var_name = data.get('variable_name', 'df')
            sheet_name = data.get('sheet_name', 0)
            sheet_param = f"'{sheet_name}'" if isinstance(sheet_name, str) else str(sheet_name)
            return f"""{var_name} = pd.read_excel('{file_path}', sheet_name={sheet_param})
print(f'Loaded dataset: {var_name}.shape = {{{var_name}.shape}}')
"""
        
        # Preprocessing nodes
        if node_type == 'preprocess_select_features_target':
            # This node selects features (X) and target (y) from the input dataframe
            target_col = data.get('target_column', 'target')
            feature_cols = data.get('feature_columns', '')
            drop_target = data.get('drop_target_from_features', True)
            
            code = f"# Select Features (X) and Target (y)\n"
            code += f"y = df['{target_col}'].copy()\n"
            
            if feature_cols and feature_cols.strip():
                # Specific columns selected - filter to only include columns that exist
                cols = [c.strip() for c in feature_cols.split(',') if c.strip()]
                cols_str = ', '.join([f"'{c}'" for c in cols])
                code += f"# Filter to only columns that exist (some may have been dropped by preprocessing)\n"
                code += f"_requested_features = [{cols_str}]\n"
                code += f"_available_features = [c for c in _requested_features if c in df.columns]\n"
                code += f"_missing_features = [c for c in _requested_features if c not in df.columns]\n"
                code += f"if _missing_features:\n"
                code += f"    print(f'Note: Skipping columns not found (may have been dropped by preprocessing): {{_missing_features}}')\n"
                code += f"X = df[_available_features].copy()\n"
            else:
                # All columns except target
                if drop_target:
                    code += f"X = df.drop(columns=['{target_col}']).copy()\n"
                else:
                    code += f"X = df.copy()\n"
            
            code += f"print(f'Features shape: {{X.shape}}, Target shape: {{y.shape}}')\n"
            code += f"print(f'Feature columns: {{list(X.columns)}}')\n"
            code += f"print(f'Target column: {target_col}')\n"
            return code
        
        if node_type == 'preprocess_select_target':
            # Simple target selector
            target_col = data.get('target_column', 'target')
            return f"""# Select Target Column
y = ${{input}}['{target_col}'].copy()
print(f'Target shape: {{y.shape}}, Target column: {target_col}')
"""
        
        if node_type == 'preprocess_split':
            test_size = data.get('test_size', 0.2)
            random_state = data.get('random_state', 42)
            return f"""X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size={test_size}, random_state={random_state}
)
print(f'Training set: X_train.shape = {{X_train.shape}}, y_train.shape = {{y_train.shape}}')
print(f'Test set: X_test.shape = {{X_test.shape}}, y_test.shape = {{y_test.shape}}')
"""
        
        if node_type == 'preprocess_onehot':
            drop = data.get('drop', 'first')
            sparse = data.get('sparse', False)
            drop_param = f", drop='{drop}'" if drop else ''
            return f"""# One-Hot Encode Categorical Features
from sklearn.preprocessing import OneHotEncoder
encoder = OneHotEncoder(sparse={sparse}{drop_param})
X_encoded = encoder.fit_transform(${{input}})
# Convert to DataFrame if sparse, otherwise keep as array
if hasattr(X_encoded, 'toarray'):
    X = pd.DataFrame(X_encoded.toarray(), columns=encoder.get_feature_names_out(${{input}}.columns))
else:
    X = pd.DataFrame(X_encoded, columns=encoder.get_feature_names_out(${{input}}.columns))
print(f'One-hot encoded features: {{X.shape}}')
"""
        
        if node_type == 'preprocess_scale':
            # Use unique scaler name based on node ID to avoid conflicts
            node_id = node.get('id', 'default')
            # Sanitize node ID for use as variable name (Python identifier)
            node_id_sanitized = node_id.replace('-', '_').replace(' ', '_').replace('.', '_')
            # Remove any non-alphanumeric/underscore characters
            import re
            node_id_sanitized = re.sub(r'[^a-zA-Z0-9_]', '_', node_id_sanitized)
            scaler_name = f"scaler_{node_id_sanitized}"
            with_mean = data.get('with_mean', True)
            with_std = data.get('with_std', True)
            
            # Each node is self-contained - handles ONLY numeric columns
            return f"""# Standard Scaler Node ({node_id})
from sklearn.preprocessing import StandardScaler
import pandas as pd
import numpy as np

# Select only numeric columns for scaling
if isinstance(X_train, pd.DataFrame):
    numeric_cols = X_train.select_dtypes(include=[np.number]).columns.tolist()
    if len(numeric_cols) == 0:
        raise ValueError("No numeric columns found in X_train. StandardScaler requires numeric data.")
    print(f'Scaling {{len(numeric_cols)}} numeric columns: {{numeric_cols}}')
    {scaler_name} = StandardScaler(with_mean={with_mean}, with_std={with_std})
    X_train_scaled = X_train.copy()
    X_test_scaled = X_test.copy()
    X_train_scaled[numeric_cols] = {scaler_name}.fit_transform(X_train[numeric_cols])
    X_test_scaled[numeric_cols] = {scaler_name}.transform(X_test[numeric_cols])
else:
    # Assume all data is numeric (numpy array)
    {scaler_name} = StandardScaler(with_mean={with_mean}, with_std={with_std})
    X_train_scaled = {scaler_name}.fit_transform(X_train)
    X_test_scaled = {scaler_name}.transform(X_test)
print(f'Scaled X_train: {{X_train.shape}} -> {{X_train_scaled.shape}}')
print(f'Scaled X_test: {{X_test.shape}} -> {{X_test_scaled.shape}}')
"""
        
        # Auto Data Prep node - handles nulls, encodes categoricals, drops high cardinality
        if node_type == 'auto_data_prep':
            handle_missing = data.get('handle_missing', 'fill_median')
            encode_categoricals = data.get('encode_categoricals', True)
            drop_high_cardinality = data.get('drop_high_cardinality', True)
            max_categories = data.get('max_categories', 10)
            
            return f"""# Auto Data Prep Node
# Automatic data preparation: handle nulls, encode categoricals, clean data
import numpy as np
from sklearn.preprocessing import LabelEncoder

print(f'Original shape: {{df.shape}}')
print(f'Original dtypes: {{df.dtypes.value_counts().to_dict()}}')
print(f'Missing values: {{df.isnull().sum().sum()}}')

# Step 1: Drop high cardinality categorical columns
cat_cols = df.select_dtypes(include=['object', 'category']).columns.tolist()
high_card_cols = [c for c in cat_cols if df[c].nunique() > {max_categories}]
if high_card_cols:
    print(f'Dropping high cardinality columns (>{max_categories} unique): {{high_card_cols}}')
    df = df.drop(columns=high_card_cols)

# Step 2: Handle missing values in numeric columns
numeric_cols = df.select_dtypes(include=[np.number]).columns.tolist()
if numeric_cols:
    df[numeric_cols] = df[numeric_cols].fillna(df[numeric_cols].median())
    print(f'Filled {{len(numeric_cols)}} numeric columns with median')

# Step 3: Encode remaining categorical columns
cat_cols = df.select_dtypes(include=['object', 'category']).columns.tolist()
if cat_cols:
    label_encoders = {{}}
    for col in cat_cols:
        le = LabelEncoder()
        df[col] = df[col].fillna('__MISSING__').astype(str)
        df[col] = le.fit_transform(df[col])
        label_encoders[col] = le
    print(f'Encoded {{len(cat_cols)}} categorical columns: {{cat_cols}}')

# Step 4: Drop any remaining rows with missing values
df = df.dropna()

print(f'Final shape: {{df.shape}}')
print(f'All columns now numeric: {{df.select_dtypes(include=[np.number]).shape[1] == df.shape[1]}}')
"""
        
        # Model training nodes
        if 'classifier' in node_type or 'regressor' in node_type:
            if 'classifier' in node_type:
                n_estimators = data.get('n_estimators', 100)
                random_state = data.get('random_state', 42)
                return f"""# Use scaled data if available, otherwise use original training data
try:
    X_train_input = X_train_scaled
except NameError:
    X_train_input = X_train
    # Filter to numeric columns if needed
    if isinstance(X_train_input, pd.DataFrame):
        import numpy as np
        numeric_cols = X_train_input.select_dtypes(include=[np.number]).columns.tolist()
        X_train_input = X_train_input[numeric_cols]

model = RandomForestClassifier(n_estimators={n_estimators}, random_state={random_state})
model.fit(X_train_input, y_train)
print(f'Model trained successfully on {{X_train_input.shape[0]}} samples with {{X_train_input.shape[1]}} features')
"""
            elif 'regressor' in node_type:
                n_estimators = data.get('n_estimators', 100)
                random_state = data.get('random_state', 42)
                return f"""# Use scaled data if available, otherwise use original training data
try:
    X_train_input = X_train_scaled
except NameError:
    X_train_input = X_train
    # Filter to numeric columns if needed
    if isinstance(X_train_input, pd.DataFrame):
        import numpy as np
        numeric_cols = X_train_input.select_dtypes(include=[np.number]).columns.tolist()
        X_train_input = X_train_input[numeric_cols]

model = RandomForestRegressor(n_estimators={n_estimators}, random_state={random_state})
model.fit(X_train_input, y_train)
print(f'Model trained successfully on {{X_train_input.shape[0]}} samples with {{X_train_input.shape[1]}} features')
"""
        
        # Evaluation nodes
        if 'evaluate' in node_type or 'metric' in node_type:
            return """# Verify model is available (requires an Algorithm node before this node)
if 'model' not in dir() or model is None:
    raise ValueError("ERROR: No model found! You must add an Algorithm node (e.g., Random Forest Classifier) before Calculate Metrics.")

# Use scaled data if available, otherwise use original test data
try:
    X_test_eval = X_test_scaled
except NameError:
    X_test_eval = X_test
    # Filter to numeric columns if needed
    if isinstance(X_test_eval, pd.DataFrame):
        import numpy as np
        numeric_cols = X_test_eval.select_dtypes(include=[np.number]).columns.tolist()
        X_test_eval = X_test_eval[numeric_cols]

y_pred = model.predict(X_test_eval)
print(f"Predictions made on {len(y_pred)} test samples")

# Calculate metrics
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score
accuracy = accuracy_score(y_test, y_pred)
precision = precision_score(y_test, y_pred, average='weighted', zero_division=0)
recall = recall_score(y_test, y_pred, average='weighted', zero_division=0)
f1 = f1_score(y_test, y_pred, average='weighted', zero_division=0)
print(f'Accuracy: {accuracy:.4f}')
print(f'Precision: {precision:.4f}')
print(f'Recall: {recall:.4f}')
print(f'F1-Score: {f1:.4f}')
"""
        
        # Save model nodes
        if 'save' in node_type and 'model' in node_type:
            file_path = data.get('file_path', 'models/model.pkl')
            format_type = data.get('format', 'pickle')
            if format_type == 'joblib':
                return f"""import joblib
os.makedirs(os.path.dirname('{file_path}'), exist_ok=True)
joblib.dump(model, '{file_path}')
print(f'Model saved to: {file_path}')
"""
            else:
                return f"""os.makedirs(os.path.dirname('{file_path}'), exist_ok=True)
with open('{file_path}', 'wb') as f:
    pickle.dump(model, f)
print(f'Model saved to: {file_path}')
"""
        
        # Default: return comment
        return f"# Node: {node.get('name', node_type)}"
    
    def _get_node_imports(self, node: Dict, framework: str) -> List[str]:
        """Get required imports for a node"""
        imports = []
        node_type = node.get('type', '')
        
        if node_type == 'start':
            pass  # No imports needed
        elif 'data_load' in node_type:
            if 'csv' in node_type or 'excel' in node_type:
                imports.append('import pandas as pd')
            elif 'json' in node_type:
                imports.append('import pandas as pd')
        elif 'preprocess' in node_type:
            if 'split' in node_type:
                imports.append('from sklearn.model_selection import train_test_split')
            elif 'scale' in node_type:
                imports.append('from sklearn.preprocessing import StandardScaler')
        elif 'classifier' in node_type or 'regressor' in node_type:
            if framework == 'scikit-learn':
                if 'classifier' in node_type:
                    imports.append('from sklearn.ensemble import RandomForestClassifier')
                elif 'regressor' in node_type:
                    imports.append('from sklearn.ensemble import RandomForestRegressor')
        elif 'evaluate' in node_type or 'metric' in node_type:
            imports.append('from sklearn.metrics import accuracy_score, classification_report')
        elif 'save' in node_type:
            imports.append('import os')
            imports.append('import pickle')
        
        return imports
    
    def _generate_code_with_execution_order(
        self, 
        execution_order: List[str],
        nodes: List[Dict],
        edges: List[Dict],
        node_map: Dict,
        executor: WorkflowExecutor,
        framework: str
    ) -> str:
        """
        Generate code following the topological execution order
        
        This ensures:
        1. Nodes execute in the correct dependency order
        2. Variables are properly passed between nodes
        3. Code is organized by execution level
        """
        imports_set = set()
        code_sections = []
        variable_counter = {}  # Track variable names for each node
        
        # Generate imports and code for each node in order
        for node_id in execution_order:
            node = node_map.get(node_id)
            if not node:
                continue
            
            # Get node inputs to determine variable names
            inputs = executor.get_node_inputs(node_id)
            
            # Generate variable name for this node's output
            node_type = node.get('type', '')
            var_name = self._get_variable_name(node_id, node_type, variable_counter)
            variable_counter[node_id] = var_name
            
            # Generate code for this node
            node_code = self._generate_node_code_with_context(
                node, 
                inputs, 
                var_name, 
                variable_counter,
                framework
            )
            
            if node_code:
                code_sections.append(node_code)
            
            # Add imports
            node_imports = self._get_node_imports(node, framework)
            imports_set.update(node_imports)
        
        # Combine imports
        imports = '\n'.join(sorted(imports_set)) if imports_set else self._generate_imports(nodes, framework)
        
        # Get execution levels for comments
        levels = executor.get_execution_levels()
        
        # Generate final code
        code = f"""# Generated ML Pipeline Code
# This code was automatically generated from a visual workflow
# Execution order determined by topological sort (dependency resolution)

{imports}

# ============================================================================
# Pipeline Execution (Topological Order)
# ============================================================================
# Execution Levels: {len(levels)} level(s)
# Nodes can run in parallel within each level

"""
        
        # Add code organized by levels - use code_sections generated in first loop
        current_level = -1
        section_index = 0
        
        for i, node_id in enumerate(execution_order):
            # Find which level this node is in
            node_level = next((j for j, level_nodes in enumerate(levels) if node_id in level_nodes), 0)
            
            if node_level != current_level:
                if current_level >= 0:
                    code += "\n"
                code += f"# --- Level {node_level} ---\n"
                current_level = node_level
            
            # Use pre-generated code section
            if section_index < len(code_sections) and code_sections[section_index]:
                code += code_sections[section_index] + "\n"
            section_index += 1
        
        return code
    
    def _get_variable_name(self, node_id: str, node_type: str, variable_counter: Dict[str, str], 
                           context: Optional[ProjectContext] = None) -> str:
        """Generate a meaningful variable name for a node using project context if available"""
        # Use existing if already assigned
        if node_id in variable_counter:
            return variable_counter[node_id]
        
        # Use standard variable names (from context or defaults)
        std_vars = ProjectContext.STANDARD_VARS
        if context:
            std_vars = context.get_all_variables()
        
        # Generate based on node type
        if node_type == 'start':
            return 'start_result'
        elif 'data_load' in node_type:
            return std_vars.get('dataframe', 'df')
        elif node_type == 'auto_data_prep':
            # Auto data prep modifies df in-place, so it outputs df
            return std_vars.get('dataframe', 'df')
        elif node_type in ['handle_missing', 'encode_categorical', 'drop_columns']:
            # Data handling nodes modify and output df
            return std_vars.get('dataframe', 'df')
        elif node_type == 'preprocess_select_features_target':
            return std_vars.get('features', 'X')
        elif node_type == 'preprocess_split':
            return std_vars.get('train_features', 'X_train')
        elif 'preprocess' in node_type and 'scale' in node_type:
            return std_vars.get('scaled_train', 'X_train_scaled')
        elif 'preprocess' in node_type:
            return 'processed_data'
        elif 'classifier' in node_type or 'regressor' in node_type:
            return std_vars.get('model', 'model')
        elif 'evaluate' in node_type:
            return std_vars.get('metrics', 'metrics')
        elif 'save' in node_type:
            return 'saved_result'
        else:
            # Generic name
            return f'result_{node_id[-6:]}'
    
    def _generate_node_code_with_context(
        self,
        node: Dict,
        inputs: List[Tuple[str, str]],
        output_var: str,
        variable_counter: Dict[str, str],
        framework: str
    ) -> str:
        """Generate code for a node with proper input/output variable handling"""
        node_type = node.get('type', '')
        data = node.get('data', {})
        node_id = node.get('id', '')
        
        # Get input variable names
        input_vars = []
        for source_id, port in inputs:
            if source_id in variable_counter:
                input_vars.append(variable_counter[source_id])
            else:
                input_vars.append('None')  # Fallback
        
        # For nodes with single input, use the first input variable
        # For nodes that require input but don't have one, use 'df' as fallback
        input_var = input_vars[0] if input_vars else None
        
        # Special handling for nodes that create X and y
        if node_type == 'preprocess_select_features_target':
            # This node creates X and y as global variables
            # Update variable_counter so downstream nodes can use them
            variable_counter[node_id + '_features'] = 'X'
            variable_counter[node_id + '_target'] = 'y'
            variable_counter[node_id] = 'X'  # Default output
            
            # Generate code
            base_code = self._generate_node_code(node, framework)
            
            # Replace ${input} with actual input variable (use 'df' as fallback)
            input_for_replace = input_var if input_var else 'df'
            base_code = base_code.replace('${input}', input_for_replace)
            base_code = base_code.replace('{input}', input_for_replace)
            
            return base_code
        
        # Special handling for train_test_split - it needs both X and y inputs
        if node_type == 'preprocess_split':
            # Build a map of input ports to variables
            input_map = {}
            for source_id, port in inputs:
                if source_id in variable_counter:
                    # Check which port this input is connected to
                    if port == 'features':
                        # Features can come from select_features_target (X) or from encoded/processed features
                        # If source has _features entry, it's from select_features_target, use X
                        # Otherwise, use the output variable from the source node (could be encoded X)
                        if source_id + '_features' in variable_counter:
                            input_map['features'] = 'X'  # Direct from select_features_target
                        else:
                            # From an intermediate node (like encode), use its output variable
                            input_map['features'] = variable_counter[source_id]
                    elif port == 'target':
                        # Target should come from select_features_target
                        if source_id + '_target' in variable_counter:
                            input_map['target'] = 'y'  # Direct from select_features_target
                        else:
                            input_map['target'] = variable_counter.get(source_id, 'y')
                    elif port == 'input':
                        # Fallback for single input port - assume it's features
                        input_map['features'] = variable_counter[source_id]
                    else:
                        input_map[port] = variable_counter[source_id]
            
            # Get X and y from input ports, or use defaults
            x_var = input_map.get('features', 'X')
            y_var = input_map.get('target', 'y')
            
            # Generate code with proper variables
            test_size = data.get('test_size', 0.2)
            random_state = data.get('random_state', 42)
            shuffle = data.get('shuffle', True)
            stratify = data.get('stratify', None)
            stratify_param = f", stratify={stratify}" if stratify else ''
            
            base_code = f"""from sklearn.model_selection import train_test_split
X_train, X_test, y_train, y_test = train_test_split(
    {x_var}, {y_var}, test_size={test_size}, random_state={random_state}, shuffle={shuffle}{stratify_param}
)
print(f'Training set: X_train.shape = {{X_train.shape}}, y_train.shape = {{y_train.shape}}')
print(f'Test set: X_test.shape = {{X_test.shape}}, y_test.shape = {{y_test.shape}}')
"""
            return base_code
        
        # Special handling for scaler nodes - they define their own scaler variable internally
        # and should NOT have their scaler variable replaced with output_var
        if node_type == 'preprocess_scale':
            # Generate code - it's already self-contained with its own scaler variable
            base_code = self._generate_node_code(node, framework)
            
            # Replace placeholder variables with actual variable names
            if input_var:
                base_code = base_code.replace('${input}', input_var)
                base_code = base_code.replace('{input}', input_var)
            
            # For scaler nodes, the output is X_train_scaled/X_test_scaled, not the scaler object itself
            # Don't try to replace the scaler variable name - it's internal to the node
            # The output_var (processed_data) is not used - the node outputs X_train_scaled/X_test_scaled
            return base_code
        
        # Generate code based on node type (delegate to existing method)
        base_code = self._generate_node_code(node, framework)
        
        # Replace placeholder variables with actual variable names
        if input_var:
            base_code = base_code.replace('${input}', input_var)
            base_code = base_code.replace('{input}', input_var)
        
        # Ensure output variable is set (but not for special nodes that handle their own variables)
        # Skip variable replacement for nodes that don't produce assignable outputs
        skip_replacement_types = [
            'preprocess_select_features_target', 
            'preprocess_scale',
            'auto_data_prep',  # Auto data prep modifies df in-place
            'handle_missing',  # Data handling modifies df in-place
            'encode_categorical',
            'drop_columns',
            'save_model',      # Save nodes don't produce output variables
            'export_results',  # Export nodes don't produce output variables
            'evaluate'         # Evaluation nodes handle their own outputs
        ]
        
        should_skip = any(skip_type in node_type for skip_type in skip_replacement_types)
        
        if output_var and output_var not in base_code and not should_skip:
            # Try to extract the variable assignment from base_code
            lines = base_code.strip().split('\n')
            if lines:
                first_line = lines[0].strip()
                # Only replace if it looks like a simple variable assignment (var = ...)
                # Not a function call with keyword args like os.makedirs(..., exist_ok=True)
                if '=' in first_line and not first_line.startswith('#'):
                    # Check if it's a simple assignment (identifier = value)
                    # Not a function call or complex expression
                    left_side = first_line.split('=')[0].strip()
                    # Simple identifier check: alphanumeric and underscore, starts with letter
                    import re
                    if re.match(r'^[a-zA-Z_][a-zA-Z0-9_]*$', left_side):
                        if left_side != output_var:
                            base_code = base_code.replace(left_side, output_var, 1)
        
        return base_code

