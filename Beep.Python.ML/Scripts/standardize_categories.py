# Standardize categories with replacements
import pandas as pd

# Parameters
feature_list = {feature_list}
replacements = {replacements}

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
        print("Error: No data available for category standardization")
        standardize_categories_successful = False
        globals()['standardize_categories_successful'] = standardize_categories_successful
        exit()
    
    standardization_info = {
        'features_processed': [],
        'replacements_applied': {},
        'automatic_standardizations': {}
    }
    
    print("Starting category standardization...")
    
    for data_name, data_df in data_to_process:
        print(f"\nProcessing {data_name}...")
        
        # Determine which features to process
        if feature_list:
            # Use specified features
            features_to_process = [col for col in feature_list if col in data_df.columns]
        else:
            # Use all categorical (object) columns
            features_to_process = data_df.select_dtypes(include=['object']).columns.tolist()
        
        if not features_to_process:
            print(f"No categorical features to process in {data_name}")
            continue
        
        print(f"Features to standardize: {features_to_process}")
        
        for feature in features_to_process:
            print(f"  Standardizing feature: {feature}")
            
            # Apply manual replacements if provided
            if replacements and feature in replacements:
                feature_replacements = replacements[feature]
                original_values = data_df[feature].value_counts()
                
                for old_value, new_value in feature_replacements.items():
                    data_df[feature] = data_df[feature].replace(old_value, new_value)
                
                standardization_info['replacements_applied'][f'{data_name}_{feature}'] = feature_replacements
                print(f"    Applied {len(feature_replacements)} manual replacements")
            
            # Apply automatic standardizations
            automatic_changes = {}
            
            # Convert to lowercase
            original_values = data_df[feature].copy()
            data_df[feature] = data_df[feature].astype(str).str.lower()
            
            # Remove extra whitespace
            data_df[feature] = data_df[feature].str.strip()
            
            # Replace common variations
            common_replacements = {
                # Boolean-like values
                'yes': 'yes', 'y': 'yes', 'true': 'yes', '1': 'yes',
                'no': 'no', 'n': 'no', 'false': 'no', '0': 'no',
                
                # Common abbreviations
                'usa': 'united states', 'us': 'united states', 'america': 'united states',
                'uk': 'united kingdom', 'britain': 'united kingdom',
                'n/a': 'unknown', 'na': 'unknown', 'null': 'unknown', 'none': 'unknown',
                
                # Gender standardization
                'm': 'male', 'f': 'female', 'man': 'male', 'woman': 'female',
                
                # Status standardization
                'active': 'active', 'inactive': 'inactive', 'pending': 'pending',
                'approved': 'approved', 'rejected': 'rejected', 'cancelled': 'cancelled'
            }
            
            for old_val, new_val in common_replacements.items():
                mask = data_df[feature] == old_val
                if mask.any():
                    data_df.loc[mask, feature] = new_val
                    if old_val not in automatic_changes:
                        automatic_changes[old_val] = new_val
            
            # Handle special characters and normalize
            data_df[feature] = data_df[feature].str.replace('[^a-zA-Z0-9\s]', '', regex=True)
            data_df[feature] = data_df[feature].str.replace('\s+', ' ', regex=True)
            
            # Track changes
            if automatic_changes:
                standardization_info['automatic_standardizations'][f'{data_name}_{feature}'] = automatic_changes
            
            # Count unique values before and after
            unique_before = len(original_values.unique())
            unique_after = data_df[feature].nunique()
            
            print(f"    Unique values: {unique_before} -> {unique_after}")
            
            standardization_info['features_processed'].append(f'{data_name}_{feature}')
        
        # Update global data
        globals()[data_name] = data_df
    
    # Store standardization information
    globals()['standardization_info'] = standardization_info
    
    print(f"\nCategory standardization completed successfully!")
    print(f"Features processed: {len(standardization_info['features_processed'])}")
    print(f"Manual replacements applied: {len(standardization_info['replacements_applied'])}")
    print(f"Automatic standardizations: {len(standardization_info['automatic_standardizations'])}")
    
    standardize_categories_successful = True
    
except Exception as e:
    print(f"Error during category standardization: {str(e)}")
    standardize_categories_successful = False

# Store the result
globals()['standardize_categories_successful'] = standardize_categories_successful