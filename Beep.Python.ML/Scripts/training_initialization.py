# Training initialization and setup
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
import matplotlib.pyplot as plt
import seaborn as sns

# Parameters
data_file_path = '{data_file_path}'
label_column = '{label_column}'
test_size = {test_size}
random_state = {random_state}

try:
    # Initialize training environment
    print("Initializing training environment...")
    
    # Set up matplotlib for headless environments
    plt.switch_backend('Agg')
    
    # Load data if file path is provided
    if data_file_path and data_file_path != 'None':
        try:
            data = pd.read_csv(data_file_path)
            print(f"Data loaded from: {data_file_path}")
            print(f"Data shape: {data.shape}")
            globals()['data'] = data
        except FileNotFoundError:
            print(f"Error: Data file not found at: {data_file_path}")
            training_initialization_successful = False
            globals()['training_initialization_successful'] = training_initialization_successful
            exit()
        except Exception as e:
            print(f"Error loading data: {str(e)}")
            training_initialization_successful = False
            globals()['training_initialization_successful'] = training_initialization_successful
            exit()
    
    # Check if data exists
    if 'data' not in globals():
        print("Warning: No data available for training initialization")
        training_initialization_successful = False
        globals()['training_initialization_successful'] = training_initialization_successful
        exit()
    
    # Validate label column
    if label_column and label_column != 'None':
        if label_column not in data.columns:
            print(f"Error: Label column '{label_column}' not found in data")
            print(f"Available columns: {list(data.columns)}")
            training_initialization_successful = False
            globals()['training_initialization_successful'] = training_initialization_successful
            exit()
        globals()['label_column'] = label_column
    else:
        print("Warning: No label column specified")
    
    # Initialize models dictionary
    if 'models' not in globals():
        models = {}
        globals()['models'] = models
        print("Models dictionary initialized")
    
    # Perform initial data analysis
    print("\n=== Data Analysis ===")
    print(f"Dataset shape: {data.shape}")
    print(f"Columns: {list(data.columns)}")
    print(f"Data types:")
    for col, dtype in data.dtypes.items():
        print(f"  {col}: {dtype}")
    
    # Check for missing values
    missing_values = data.isnull().sum()
    if missing_values.sum() > 0:
        print(f"\nMissing values found:")
        for col, missing in missing_values.items():
            if missing > 0:
                print(f"  {col}: {missing} ({missing/len(data)*100:.1f}%)")
    else:
        print("No missing values found")
    
    # Analyze target variable if specified
    if label_column and label_column in data.columns:
        print(f"\n=== Target Variable Analysis ({label_column}) ===")
        target_stats = data[label_column].describe()
        print(target_stats)
        
        # Check if classification or regression
        unique_values = data[label_column].nunique()
        if unique_values <= 20:  # Likely classification
            print(f"Target appears to be categorical ({unique_values} unique values)")
            value_counts = data[label_column].value_counts()
            print("Class distribution:")
            for value, count in value_counts.items():
                print(f"  {value}: {count} ({count/len(data)*100:.1f}%)")
            globals()['problem_type'] = 'classification'
        else:
            print(f"Target appears to be continuous ({unique_values} unique values)")
            globals()['problem_type'] = 'regression'
    
    # Split data if test_size is provided
    if test_size and test_size > 0 and test_size < 1 and label_column in data.columns:
        print(f"\n=== Data Splitting (test_size={test_size}) ===")
        
        try:
            # Stratified split for classification, regular split for regression
            if globals().get('problem_type') == 'classification':
                train_data, test_data = train_test_split(
                    data, 
                    test_size=test_size, 
                    random_state=random_state,
                    stratify=data[label_column]
                )
                print("Performed stratified split for classification")
            else:
                train_data, test_data = train_test_split(
                    data, 
                    test_size=test_size, 
                    random_state=random_state
                )
                print("Performed regular split")
            
            # Store split data
            globals()['train_data'] = train_data
            globals()['test_data'] = test_data
            
            print(f"Training set: {train_data.shape}")
            print(f"Test set: {test_data.shape}")
            
            # Prepare feature and target arrays for quick access
            feature_columns = [col for col in train_data.columns if col != label_column]
            globals()['feature_columns'] = feature_columns
            
            print(f"Number of features: {len(feature_columns)}")
            
        except Exception as split_error:
            print(f"Error during data splitting: {str(split_error)}")
            print("Continuing without split...")
    
    # Identify numerical and categorical features
    numerical_features = data.select_dtypes(include=['number']).columns.tolist()
    categorical_features = data.select_dtypes(include=['object']).columns.tolist()
    
    # Remove label column from feature lists
    if label_column in numerical_features:
        numerical_features.remove(label_column)
    if label_column in categorical_features:
        categorical_features.remove(label_column)
    
    globals()['numerical_features'] = numerical_features
    globals()['categorical_features'] = categorical_features
    
    print(f"\n=== Feature Analysis ===")
    print(f"Numerical features ({len(numerical_features)}): {numerical_features[:5]}...")
    print(f"Categorical features ({len(categorical_features)}): {categorical_features[:5]}...")
    
    # Set up default parameters for common algorithms
    globals()['default_random_state'] = random_state
    
    # Initialize results storage
    globals()['training_results'] = {}
    globals()['evaluation_results'] = {}
    
    print(f"\n=== Training Environment Ready ===")
    print("Available global variables:")
    available_vars = [var for var in globals().keys() if not var.startswith('_')]
    for var in sorted(available_vars):
        if var in ['data', 'train_data', 'test_data', 'models', 'label_column', 'problem_type']:
            print(f"  ? {var}")
    
    training_initialization_successful = True
    
except Exception as e:
    print(f"Error during training initialization: {str(e)}")
    training_initialization_successful = False

# Store the result
globals()['training_initialization_successful'] = training_initialization_successful