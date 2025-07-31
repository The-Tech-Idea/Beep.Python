from sklearn.preprocessing import MinMaxScaler
import pandas as pd

# Parameters
feature_range_min = {feature_range_min}
feature_range_max = {feature_range_max}
selected_features = {selected_features}

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
        print("Error: No data available for Min-Max scaling")
        minmax_scale_successful = False
        globals()['minmax_scale_successful'] = minmax_scale_successful
        exit()
    
    scaling_info = {
        'scaler': None,
        'features_scaled': [],
        'feature_range': (feature_range_min, feature_range_max)
    }
    
    print(f"Starting Min-Max scaling with range ({feature_range_min}, {feature_range_max})...")
    
    for data_name, data_df in data_to_process:
        print(f"Processing {data_name}...")
        
        # Determine which features to scale
        if selected_features:
            # Use specified features
            features_to_scale = [col for col in selected_features if col in data_df.columns]
        else:
            # Use all numerical columns
            features_to_scale = data_df.select_dtypes(include=['number']).columns.tolist()
        
        if not features_to_scale:
            print(f"Warning: No numerical features found for scaling in {data_name}")
            continue
        
        print(f"Features to scale: {features_to_scale}")
        
        if data_name == 'train_data':
            # Fit scaler on training data
            scaler = MinMaxScaler(feature_range=(feature_range_min, feature_range_max))
            data_df[features_to_scale] = scaler.fit_transform(data_df[features_to_scale])
            
            # Store scaler for test data
            scaling_info['scaler'] = scaler
            scaling_info['features_scaled'] = features_to_scale
            
            print(f"  Fitted Min-Max scaler on {len(features_to_scale)} features")
            
        elif data_name == 'test_data' and scaling_info['scaler'] is not None:
            # Transform test data using fitted scaler
            scaler = scaling_info['scaler']
            features_to_scale = [col for col in scaling_info['features_scaled'] if col in data_df.columns]
            
            if features_to_scale:
                data_df[features_to_scale] = scaler.transform(data_df[features_to_scale])
                print(f"  Transformed {len(features_to_scale)} features in test data")
            
        elif data_name == 'data':
            # For single dataset, just fit and transform
            scaler = MinMaxScaler(feature_range=(feature_range_min, feature_range_max))
            data_df[features_to_scale] = scaler.fit_transform(data_df[features_to_scale])
            
            scaling_info['scaler'] = scaler
            scaling_info['features_scaled'] = features_to_scale
            
            print(f"  Applied Min-Max scaling to {len(features_to_scale)} features")
        
        # Update global data
        globals()[data_name] = data_df
    
    # Store scaling information
    globals()['scaling_info'] = scaling_info
    
    print(f"Min-Max scaling completed successfully!")
    print(f"Features scaled: {scaling_info['features_scaled']}")
    print(f"Feature range: {scaling_info['feature_range']}")
    
    minmax_scale_successful = True
    
except Exception as e:
    print(f"Error during Min-Max scaling: {str(e)}")
    minmax_scale_successful = False

# Store the result
globals()['minmax_scale_successful'] = minmax_scale_successful