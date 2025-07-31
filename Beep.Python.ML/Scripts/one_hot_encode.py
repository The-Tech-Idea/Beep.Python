# One-hot encode categorical features
import pandas as pd

# Parameters
categorical_features = {categorical_features}

try:
    # Determine which data to process
    data_to_process = []
    
    if 'train_data' in globals():
        data_to_process.append(('train_data', train_data))
    if 'test_data' in globals():
        data_to_process.append(('test_data', test_data))
    if 'data' in globals() and len(data_to_process) == 0:
        data_to_process.append(('data', data))
    
    if not data_to_process:
        print("Error: No data available for one-hot encoding")
        one_hot_encode_successful = False
        globals()['one_hot_encode_successful'] = one_hot_encode_successful
        exit()
    
    encoding_info = {
        'features_processed': [],
        'encoded_columns': {}
    }
    
    print("Starting one-hot encoding...")
    
    for data_name, data_df in data_to_process:
        print(f"Processing {data_name}...")
        
        # Validate categorical features exist
        available_columns = data_df.columns.tolist()
        valid_features = [col for col in categorical_features if col in available_columns]
        
        if not valid_features:
            print(f"Warning: None of the specified categorical features found in {data_name}")
            continue
        
        print(f"Features to one-hot encode: {valid_features}")
        
        if data_name == 'train_data':
            # Fit one-hot encoding on training data
            for feature in valid_features:
                print(f"  One-hot encoding feature: {feature}")
                
                # One-hot encode the feature
                encoded_df = pd.get_dummies(data_df[feature], prefix=feature)
                
                # Drop original feature and add encoded ones
                data_df = data_df.drop(columns=[feature])
                data_df = pd.concat([data_df, encoded_df], axis=1)
                
                # Store encoded column names for test data alignment
                encoding_info['encoded_columns'][feature] = encoded_df.columns.tolist()
                encoding_info['features_processed'].append(feature)
                
        elif data_name == 'test_data' and encoding_info['encoded_columns']:
            # Transform test data using training data column structure
            for feature in valid_features:
                if feature in encoding_info['encoded_columns']:
                    print(f"  One-hot encoding test feature: {feature}")
                    
                    # One-hot encode test data
                    test_encoded = pd.get_dummies(data_df[feature], prefix=feature)
                    
                    # Ensure test data has same columns as training data
                    train_columns = encoding_info['encoded_columns'][feature]
                    
                    # Add missing columns with zeros
                    for col in train_columns:
                        if col not in test_encoded.columns:
                            test_encoded[col] = 0
                    
                    # Remove extra columns and reorder to match training
                    test_encoded = test_encoded[train_columns]
                    
                    # Drop original feature and add encoded ones
                    data_df = data_df.drop(columns=[feature])
                    data_df = pd.concat([data_df, test_encoded], axis=1)
        
        # Update global data
        globals()[data_name] = data_df
    
    # Store encoding information
    globals()['encoding_info'] = encoding_info
    
    print(f"One-hot encoding completed successfully!")
    print(f"Features processed: {len(encoding_info['features_processed'])}")
    for feature, columns in encoding_info['encoded_columns'].items():
        print(f"  {feature} -> {len(columns)} columns: {columns[:3]}..." if len(columns) > 3 else f"  {feature} -> {columns}")
    
    one_hot_encode_successful = True
    
except Exception as e:
    print(f"Error during one-hot encoding: {str(e)}")
    one_hot_encode_successful = False

# Store the result
globals()['one_hot_encode_successful'] = one_hot_encode_successful