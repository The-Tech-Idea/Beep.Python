# Apply variance threshold with specific features
from sklearn.feature_selection import VarianceThreshold
import pandas as pd

# Parameters
threshold = {threshold}
feature_list = {feature_list}

try:
    # Determine which features to apply variance threshold to
    if feature_list and len(feature_list) > 0:
        target_features = feature_list
        print(f"Applying variance threshold to specific features: {len(target_features)} features")
    else:
        # Apply to all numerical features
        if 'train_data' in globals():
            target_features = train_data.select_dtypes(include=['number']).columns.tolist()
        elif 'data' in globals():
            target_features = data.select_dtypes(include=['number']).columns.tolist()
        else:
            print("Warning: No data available for variance threshold")
            variance_threshold_successful = False
            globals()['variance_threshold_successful'] = variance_threshold_successful
            exit()
        print(f"Applying variance threshold to all numerical features: {len(target_features)} features")
    
    # Apply variance threshold
    if 'train_data' in globals() and target_features:
        # Filter to existing features
        existing_features = [f for f in target_features if f in train_data.columns]
        
        if existing_features:
            # Apply variance threshold
            selector = VarianceThreshold(threshold=threshold)
            selected_data = selector.fit_transform(train_data[existing_features])
            
            # Get selected feature names
            selected_mask = selector.get_support()
            selected_features = [existing_features[i] for i, selected in enumerate(selected_mask) if selected]
            
            # Keep non-target features and selected target features
            other_features = [col for col in train_data.columns if col not in existing_features]
            final_features = other_features + selected_features
            
            # Update train_data
            train_data = train_data[final_features]
            globals()['train_data'] = train_data
            
            # Apply same selection to test_data if it exists
            if 'test_data' in globals():
                test_final_features = [col for col in final_features if col in test_data.columns]
                test_data = test_data[test_final_features]
                globals()['test_data'] = test_data
            
            # Store variance threshold information
            feature_variances = selector.variances_ if hasattr(selector, 'variances_') else None
            
            print(f"Variance threshold feature selection completed:")
            print(f"Threshold: {threshold}")
            print(f"Original target features: {len(existing_features)}")
            print(f"Selected features: {len(selected_features)}")
            print(f"Removed features: {len(existing_features) - len(selected_features)}")
            
            if feature_variances is not None:
                removed_features = [existing_features[i] for i, selected in enumerate(selected_mask) if not selected]
                if removed_features:
                    removed_variances = [feature_variances[i] for i, selected in enumerate(selected_mask) if not selected]
                    print(f"Removed features (low variance): {list(zip(removed_features[:5], removed_variances[:5]))}")
            
        else:
            print("Warning: No target features found in train_data")
            selected_features = []
        
    elif 'data' in globals() and target_features:
        # Same logic for single data DataFrame
        existing_features = [f for f in target_features if f in data.columns]
        
        if existing_features:
            selector = VarianceThreshold(threshold=threshold)
            selected_data = selector.fit_transform(data[existing_features])
            
            selected_mask = selector.get_support()
            selected_features = [existing_features[i] for i, selected in enumerate(selected_mask) if selected]
            
            other_features = [col for col in data.columns if col not in existing_features]
            final_features = other_features + selected_features
            
            data = data[final_features]
            globals()['data'] = data
            
            feature_variances = selector.variances_ if hasattr(selector, 'variances_') else None
            
            print(f"Variance threshold feature selection completed:")
            print(f"Threshold: {threshold}")
            print(f"Original target features: {len(existing_features)}")
            print(f"Selected features: {len(selected_features)}")
            print(f"Removed features: {len(existing_features) - len(selected_features)}")
            
        else:
            print("Warning: No target features found in data")
            selected_features = []
    
    else:
        print("Warning: No data available or no target features specified")
        selected_features = []
    
    # Store results
    globals()['variance_selected_features'] = selected_features
    globals()['variance_threshold_value'] = threshold
    
    variance_threshold_successful = len(selected_features) > 0 if target_features else True
    
except Exception as e:
    print(f"Error applying variance threshold: {str(e)}")
    variance_threshold_successful = False

# Store the result
globals()['variance_threshold_successful'] = variance_threshold_successful