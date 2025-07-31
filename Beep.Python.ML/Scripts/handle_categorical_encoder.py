# Handle categorical data encoding
import pandas as pd
from sklearn.preprocessing import LabelEncoder, OneHotEncoder

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
        print("Error: No data available for categorical encoding")
        handle_categorical_encoder_successful = False
        globals()['handle_categorical_encoder_successful'] = handle_categorical_encoder_successful
        exit()
    
    encoding_info = {
        'features_processed': [],
        'encoders': {}
    }
    
    print("Starting categorical data encoding...")
    
    for data_name, data_df in data_to_process:
        print(f"Processing {data_name}...")
        
        # Validate categorical features exist
        available_columns = data_df.columns.tolist()
        valid_features = [col for col in categorical_features if col in available_columns]
        
        if not valid_features:
            print(f"Warning: None of the specified categorical features found in {data_name}")
            continue
        
        print(f"Categorical features to encode: {valid_features}")
        
        if data_name == 'train_data':
            # Fit encoders on training data
            for feature in valid_features:
                print(f"  Encoding feature: {feature}")
                
                # Use one-hot encoding for categorical features
                encoded_df = pd.get_dummies(data_df[feature], prefix=feature)
                
                # Drop original feature and add encoded ones
                data_df = data_df.drop(columns=[feature])
                data_df = pd.concat([data_df, encoded_df], axis=1)
                
                # Store encoder information
                encoding_info['encoders'][feature] = {
                    'type': 'onehot',
                    'columns': encoded_df.columns.tolist()
                }
                
                encoding_info['features_processed'].append(feature)
                
        elif data_name == 'test_data' and encoding_info['encoders']:
            # Transform test data using fitted encoders
            for feature in valid_features:
                if feature in encoding_info['encoders']:
                    encoder_info = encoding_info['encoders'][feature]
                    
                    if encoder_info['type'] == 'onehot':
                        # One-hot encode test data
                        test_encoded = pd.get_dummies(data_df[feature], prefix=feature)
                        
                        # Ensure test data has same columns as training data
                        train_columns = encoder_info['columns']
                        
                        # Add missing columns
                        for col in train_columns:
                            if col not in test_encoded.columns:
                                test_encoded[col] = 0
                        
                        # Remove extra columns and reorder
                        test_encoded = test_encoded[train_columns]
                        
                        # Drop original feature and add encoded ones
                        data_df = data_df.drop(columns=[feature])
                        data_df = pd.concat([data_df, test_encoded], axis=1)
        
        # Update global data
        globals()[data_name] = data_df
    
    # Store encoding information
    globals()['encoding_info'] = encoding_info
    
    print(f"Categorical encoding completed successfully!")
    print(f"Features processed: {len(encoding_info['features_processed'])}")
    
    handle_categorical_encoder_successful = True
    
except Exception as e:
    print(f"Error during categorical encoding: {str(e)}")
    handle_categorical_encoder_successful = False

# Store the result
globals()['handle_categorical_encoder_successful'] = handle_categorical_encoder_successful