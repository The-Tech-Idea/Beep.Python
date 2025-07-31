# Automated feature engineering with multiple techniques
import pandas as pd
import numpy as np
from sklearn.preprocessing import PolynomialFeatures
from scipy import stats

# Parameters
feature_engineering_steps = {feature_engineering_steps}
label_column = '{label_column}'

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
        print("Error: No data available for feature engineering")
        feature_engineering_successful = False
        globals()['feature_engineering_successful'] = feature_engineering_successful
        exit()
    
    engineering_info = {
        'steps_completed': [],
        'new_features': [],
        'transformers': {}
    }
    
    print("Starting automated feature engineering...")
    
    # Process each feature engineering step
    for step in feature_engineering_steps:
        step_name = step.get('name', 'unknown')
        step_params = step.get('parameters', {})
        
        print(f"\nApplying feature engineering step: {step_name}")
        
        if step_name == 'polynomial_features':
            # Create polynomial features
            degree = step_params.get('degree', 2)
            interaction_only = step_params.get('interaction_only', False)
            
            for data_name, data_df in data_to_process:
                # Select numerical features
                numerical_cols = data_df.select_dtypes(include=['number']).columns.tolist()
                
                # Remove label column
                if label_column in numerical_cols:
                    numerical_cols.remove(label_column)
                
                if numerical_cols and data_name == 'train_data':
                    # Fit polynomial features on training data
                    poly = PolynomialFeatures(degree=degree, interaction_only=interaction_only, include_bias=False)
                    poly_features = poly.fit_transform(data_df[numerical_cols])
                    
                    # Create DataFrame with polynomial features
                    poly_feature_names = poly.get_feature_names_out(numerical_cols)
                    poly_df = pd.DataFrame(poly_features, columns=poly_feature_names, index=data_df.index)
                    
                    # Remove original features (they're included in polynomial features)
                    original_features = data_df.columns.tolist()
                    for col in numerical_cols:
                        if col in original_features:
                            original_features.remove(col)
                    
                    # Combine with non-numerical features and label
                    if original_features:
                        final_df = pd.concat([data_df[original_features], poly_df], axis=1)
                    else:
                        final_df = poly_df
                    
                    # Update data
                    globals()[data_name] = final_df
                    engineering_info['transformers']['polynomial'] = poly
                    engineering_info['new_features'].extend(poly_feature_names.tolist())
                    
                elif numerical_cols and data_name == 'test_data' and 'polynomial' in engineering_info['transformers']:
                    # Transform test data using fitted polynomial features
                    poly = engineering_info['transformers']['polynomial']
                    poly_features = poly.transform(data_df[numerical_cols])
                    
                    poly_feature_names = poly.get_feature_names_out(numerical_cols)
                    poly_df = pd.DataFrame(poly_features, columns=poly_feature_names, index=data_df.index)
                    
                    # Combine with non-numerical features and label
                    original_features = data_df.columns.tolist()
                    for col in numerical_cols:
                        if col in original_features:
                            original_features.remove(col)
                    
                    if original_features:
                        final_df = pd.concat([data_df[original_features], poly_df], axis=1)
                    else:
                        final_df = poly_df
                    
                    globals()[data_name] = final_df
            
            engineering_info['steps_completed'].append(f'polynomial_features_degree_{degree}')
            
        elif step_name == 'log_transform':
            # Apply log transformation to skewed features
            skew_threshold = step_params.get('skew_threshold', 1.0)
            
            for data_name, data_df in data_to_process:
                numerical_cols = data_df.select_dtypes(include=['number']).columns.tolist()
                
                # Remove label column
                if label_column in numerical_cols:
                    numerical_cols.remove(label_column)
                
                # Find skewed features
                skewed_features = []
                for col in numerical_cols:
                    if data_df[col].min() > 0:  # Only for positive values
                        skewness = data_df[col].skew()
                        if abs(skewness) > skew_threshold:
                            skewed_features.append(col)
                
                # Apply log transformation
                for col in skewed_features:
                    new_col_name = f'{col}_log'
                    data_df[new_col_name] = np.log1p(data_df[col])  # log1p for numerical stability
                    engineering_info['new_features'].append(new_col_name)
                
                # Update data
                globals()[data_name] = data_df
                
                if skewed_features:
                    print(f"  Applied log transformation to: {skewed_features}")
            
            engineering_info['steps_completed'].append('log_transform')
            
        elif step_name == 'binning':
            # Create binned versions of continuous features
            n_bins = step_params.get('n_bins', 5)
            strategy = step_params.get('strategy', 'quantile')
            
            for data_name, data_df in data_to_process:
                numerical_cols = data_df.select_dtypes(include=['number']).columns.tolist()
                
                # Remove label column
                if label_column in numerical_cols:
                    numerical_cols.remove(label_column)
                
                # Apply binning to selected features
                features_to_bin = step_params.get('features', numerical_cols[:3])  # Default to first 3
                
                for col in features_to_bin:
                    if col in data_df.columns:
                        if strategy == 'quantile':
                            data_df[f'{col}_binned'] = pd.qcut(data_df[col], q=n_bins, labels=False, duplicates='drop')
                        else:  # equal width
                            data_df[f'{col}_binned'] = pd.cut(data_df[col], bins=n_bins, labels=False)
                        
                        engineering_info['new_features'].append(f'{col}_binned')
                
                # Update data
                globals()[data_name] = data_df
            
            engineering_info['steps_completed'].append(f'binning_{strategy}')
            
        elif step_name == 'ratio_features':
            # Create ratio features between numerical columns
            for data_name, data_df in data_to_process:
                numerical_cols = data_df.select_dtypes(include=['number']).columns.tolist()
                
                # Remove label column
                if label_column in numerical_cols:
                    numerical_cols.remove(label_column)
                
                # Create ratios between first few numerical features
                if len(numerical_cols) >= 2:
                    feature_pairs = step_params.get('feature_pairs', [(numerical_cols[0], numerical_cols[1])])
                    
                    for col1, col2 in feature_pairs:
                        if col1 in data_df.columns and col2 in data_df.columns:
                            # Avoid division by zero
                            ratio_col = f'{col1}_div_{col2}'
                            data_df[ratio_col] = data_df[col1] / (data_df[col2] + 1e-8)
                            engineering_info['new_features'].append(ratio_col)
                            
                            # Also create sum and difference
                            sum_col = f'{col1}_plus_{col2}'
                            diff_col = f'{col1}_minus_{col2}'
                            data_df[sum_col] = data_df[col1] + data_df[col2]
                            data_df[diff_col] = data_df[col1] - data_df[col2]
                            
                            engineering_info['new_features'].extend([sum_col, diff_col])
                
                # Update data
                globals()[data_name] = data_df
            
            engineering_info['steps_completed'].append('ratio_features')
            
        elif step_name == 'aggregation_features':
            # Create aggregation features (mean, std, min, max across features)
            for data_name, data_df in data_to_process:
                numerical_cols = data_df.select_dtypes(include=['number']).columns.tolist()
                
                # Remove label column
                if label_column in numerical_cols:
                    numerical_cols.remove(label_column)
                
                if len(numerical_cols) >= 2:
                    # Row-wise aggregations
                    data_df['feature_mean'] = data_df[numerical_cols].mean(axis=1)
                    data_df['feature_std'] = data_df[numerical_cols].std(axis=1)
                    data_df['feature_min'] = data_df[numerical_cols].min(axis=1)
                    data_df['feature_max'] = data_df[numerical_cols].max(axis=1)
                    data_df['feature_range'] = data_df['feature_max'] - data_df['feature_min']
                    
                    new_agg_features = ['feature_mean', 'feature_std', 'feature_min', 'feature_max', 'feature_range']
                    engineering_info['new_features'].extend(new_agg_features)
                
                # Update data
                globals()[data_name] = data_df
            
            engineering_info['steps_completed'].append('aggregation_features')
    
    # Store feature engineering information
    globals()['engineering_info'] = engineering_info
    
    # Display final summary
    print(f"\nFeature engineering completed successfully!")
    print(f"Steps completed: {len(engineering_info['steps_completed'])}")
    for step in engineering_info['steps_completed']:
        print(f"  ? {step}")
    
    print(f"New features created: {len(engineering_info['new_features'])}")
    if engineering_info['new_features']:
        print(f"Sample new features: {engineering_info['new_features'][:5]}...")
    
    # Display final data shapes
    for data_name, data_df in data_to_process:
        print(f"{data_name} shape: {data_df.shape}")
    
    feature_engineering_successful = True
    
except Exception as e:
    print(f"Error during feature engineering: {str(e)}")
    feature_engineering_successful = False

# Store the result
globals()['feature_engineering_successful'] = feature_engineering_successful