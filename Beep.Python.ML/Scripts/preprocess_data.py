# Preprocess data with comprehensive cleaning and transformation
import pandas as pd
import numpy as np
from sklearn.preprocessing import StandardScaler, MinMaxScaler, RobustScaler, LabelEncoder, OneHotEncoder
from sklearn.impute import SimpleImputer

# Parameters
preprocessing_steps = {preprocessing_steps}
label_column = '{label_column}'

try:
    # Determine which data to preprocess
    data_to_process = []
    
    if 'train_data' in globals():
        data_to_process.append(('train_data', train_data))
    if 'test_data' in globals():
        data_to_process.append(('test_data', test_data))
    if 'data' in globals() and len(data_to_process) == 0:
        data_to_process.append(('data', data))
    
    if not data_to_process:
        print("Error: No data available for preprocessing")
        preprocess_data_successful = False
        globals()['preprocess_data_successful'] = preprocess_data_successful
        exit()
    
    preprocessing_info = {
        'steps_completed': [],
        'scalers': {},
        'encoders': {},
        'imputers': {}
    }
    
    print("Starting comprehensive data preprocessing...")
    
    # Process each preprocessing step
    for step in preprocessing_steps:
        step_name = step.get('name', 'unknown')
        step_params = step.get('parameters', {})
        
        print(f"\nApplying preprocessing step: {step_name}")
        
        if step_name == 'handle_missing_values':
            # Handle missing values
            strategy = step_params.get('strategy', 'mean')
            
            for data_name, data_df in data_to_process:
                # Separate numerical and categorical columns
                numerical_cols = data_df.select_dtypes(include=['number']).columns.tolist()
                categorical_cols = data_df.select_dtypes(include=['object']).columns.tolist()
                
                # Remove label column from preprocessing
                if label_column in numerical_cols:
                    numerical_cols.remove(label_column)
                if label_column in categorical_cols:
                    categorical_cols.remove(label_column)
                
                # Impute numerical columns
                if numerical_cols and data_name == 'train_data':
                    if strategy in ['mean', 'median']:
                        num_imputer = SimpleImputer(strategy=strategy)
                        data_df[numerical_cols] = num_imputer.fit_transform(data_df[numerical_cols])
                        preprocessing_info['imputers']['numerical'] = num_imputer
                    elif strategy == 'zero':
                        data_df[numerical_cols] = data_df[numerical_cols].fillna(0)
                
                elif numerical_cols and data_name == 'test_data' and 'numerical' in preprocessing_info['imputers']:
                    num_imputer = preprocessing_info['imputers']['numerical']
                    data_df[numerical_cols] = num_imputer.transform(data_df[numerical_cols])
                
                # Impute categorical columns
                if categorical_cols and data_name == 'train_data':
                    cat_imputer = SimpleImputer(strategy='most_frequent')
                    data_df[categorical_cols] = cat_imputer.fit_transform(data_df[categorical_cols])
                    preprocessing_info['imputers']['categorical'] = cat_imputer
                    
                elif categorical_cols and data_name == 'test_data' and 'categorical' in preprocessing_info['imputers']:
                    cat_imputer = preprocessing_info['imputers']['categorical']
                    data_df[categorical_cols] = cat_imputer.transform(data_df[categorical_cols])
                
                # Update global data
                globals()[data_name] = data_df
            
            preprocessing_info['steps_completed'].append('handle_missing_values')
            
        elif step_name == 'scale_features':
            # Scale numerical features
            scaler_type = step_params.get('scaler', 'standard')
            
            for data_name, data_df in data_to_process:
                numerical_cols = data_df.select_dtypes(include=['number']).columns.tolist()
                
                # Remove label column
                if label_column in numerical_cols:
                    numerical_cols.remove(label_column)
                
                if numerical_cols:
                    if data_name == 'train_data':
                        # Fit scaler on training data
                        if scaler_type == 'standard':
                            scaler = StandardScaler()
                        elif scaler_type == 'minmax':
                            scaler = MinMaxScaler()
                        elif scaler_type == 'robust':
                            scaler = RobustScaler()
                        else:
                            scaler = StandardScaler()
                        
                        data_df[numerical_cols] = scaler.fit_transform(data_df[numerical_cols])
                        preprocessing_info['scalers'][scaler_type] = scaler
                        
                    elif data_name == 'test_data' and scaler_type in preprocessing_info['scalers']:
                        # Transform test data using fitted scaler
                        scaler = preprocessing_info['scalers'][scaler_type]
                        data_df[numerical_cols] = scaler.transform(data_df[numerical_cols])
                
                # Update global data
                globals()[data_name] = data_df
            
            preprocessing_info['steps_completed'].append(f'scale_features_{scaler_type}')
            
        elif step_name == 'encode_categorical':
            # Encode categorical features
            encoding_type = step_params.get('encoding', 'onehot')
            
            for data_name, data_df in data_to_process:
                categorical_cols = data_df.select_dtypes(include=['object']).columns.tolist()
                
                # Remove label column if it's categorical
                if label_column in categorical_cols:
                    categorical_cols.remove(label_column)
                
                if categorical_cols:
                    if encoding_type == 'onehot':
                        if data_name == 'train_data':
                            # One-hot encode training data
                            encoded_df = pd.get_dummies(data_df[categorical_cols], prefix=categorical_cols)
                            
                            # Drop original categorical columns and add encoded ones
                            data_df = data_df.drop(columns=categorical_cols)
                            data_df = pd.concat([data_df, encoded_df], axis=1)
                            
                            # Store column names for test data
                            preprocessing_info['encoders']['onehot_columns'] = encoded_df.columns.tolist()
                            
                        elif data_name == 'test_data' and 'onehot_columns' in preprocessing_info['encoders']:
                            # One-hot encode test data
                            test_encoded = pd.get_dummies(data_df[categorical_cols], prefix=categorical_cols)
                            
                            # Ensure test data has same columns as training data
                            train_columns = preprocessing_info['encoders']['onehot_columns']
                            
                            # Add missing columns
                            for col in train_columns:
                                if col not in test_encoded.columns:
                                    test_encoded[col] = 0
                            
                            # Remove extra columns and reorder
                            test_encoded = test_encoded[train_columns]
                            
                            # Drop original categorical columns and add encoded ones
                            data_df = data_df.drop(columns=categorical_cols)
                            data_df = pd.concat([data_df, test_encoded], axis=1)
                    
                    elif encoding_type == 'label':
                        if data_name == 'train_data':
                            # Label encode each categorical column
                            label_encoders = {}
                            for col in categorical_cols:
                                le = LabelEncoder()
                                data_df[col] = le.fit_transform(data_df[col].astype(str))
                                label_encoders[col] = le
                            
                            preprocessing_info['encoders']['label_encoders'] = label_encoders
                            
                        elif data_name == 'test_data' and 'label_encoders' in preprocessing_info['encoders']:
                            label_encoders = preprocessing_info['encoders']['label_encoders']
                            for col in categorical_cols:
                                if col in label_encoders:
                                    le = label_encoders[col]
                                    # Handle unknown categories
                                    unique_values = set(le.classes_)
                                    data_df[col] = data_df[col].apply(
                                        lambda x: x if x in unique_values else le.classes_[0]
                                    )
                                    data_df[col] = le.transform(data_df[col].astype(str))
                
                # Update global data
                globals()[data_name] = data_df
            
            preprocessing_info['steps_completed'].append(f'encode_categorical_{encoding_type}')
            
        elif step_name == 'remove_outliers':
            # Remove outliers using IQR method
            iqr_factor = step_params.get('iqr_factor', 1.5)
            
            for data_name, data_df in data_to_process:
                if data_name == 'train_data':  # Only remove outliers from training data
                    numerical_cols = data_df.select_dtypes(include=['number']).columns.tolist()
                    
                    # Remove label column
                    if label_column in numerical_cols:
                        numerical_cols.remove(label_column)
                    
                    original_size = len(data_df)
                    
                    for col in numerical_cols:
                        Q1 = data_df[col].quantile(0.25)
                        Q3 = data_df[col].quantile(0.75)
                        IQR = Q3 - Q1
                        lower_bound = Q1 - iqr_factor * IQR
                        upper_bound = Q3 + iqr_factor * IQR
                        
                        # Keep rows within bounds
                        data_df = data_df[(data_df[col] >= lower_bound) & (data_df[col] <= upper_bound)]
                    
                    new_size = len(data_df)
                    removed_count = original_size - new_size
                    
                    print(f"  Removed {removed_count} outlier rows ({removed_count/original_size*100:.1f}%)")
                    
                    # Update global data
                    globals()[data_name] = data_df
            
            preprocessing_info['steps_completed'].append('remove_outliers')
    
    # Store preprocessing information
    globals()['preprocessing_info'] = preprocessing_info
    
    # Display final summary
    print(f"\nPreprocessing completed successfully!")
    print(f"Steps completed: {len(preprocessing_info['steps_completed'])}")
    for step in preprocessing_info['steps_completed']:
        print(f"  ? {step}")
    
    # Display final data shapes
    for data_name, data_df in data_to_process:
        print(f"{data_name} shape: {data_df.shape}")
    
    preprocess_data_successful = True
    
except Exception as e:
    print(f"Error during data preprocessing: {str(e)}")
    preprocess_data_successful = False

# Store the result
globals()['preprocess_data_successful'] = preprocess_data_successful