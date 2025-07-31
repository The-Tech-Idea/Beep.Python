# Split data with primary key ID preservation
from sklearn.model_selection import train_test_split
import pandas as pd

# Parameters
data_file_path = '{data_file_path}'
test_size = {test_size}
train_file_path = '{train_file_path}'
test_file_path = '{test_file_path}'
validation_file_path = '{validation_file_path}'
primary_feature_key_id = '{primary_feature_key_id}'
label_column = '{label_column}'

try:
    # Load data from file if file path is provided, otherwise use existing data
    if data_file_path and data_file_path != 'None':
        data = pd.read_csv(data_file_path)
        print(f"Loaded data from: {data_file_path}")
    elif 'data' in globals():
        data = globals()['data']
        print("Using existing data from memory")
    else:
        print("Error: No data available for splitting")
        split_with_key_successful = False
        globals()['split_with_key_successful'] = split_with_key_successful
        exit()
    
    # Ensure the primary key column exists
    if primary_feature_key_id not in data.columns:
        print(f"Warning: Primary key column '{primary_feature_key_id}' not found in data")
        # Create a simple index as primary key
        data[primary_feature_key_id] = range(len(data))
    
    # Split data while preserving key relationships
    # First split: separate out test set
    train_val_data, test_data = train_test_split(
        data, 
        test_size=test_size,
        random_state=42,
        stratify=data[label_column] if label_column in data.columns else None
    )
    
    # Second split: separate train and validation (if validation file is specified)
    if validation_file_path and validation_file_path != 'None':
        # Use remaining data for validation (e.g., 20% of original data)
        val_size_adjusted = 0.25  # 25% of remaining data after test split
        
        train_data, validation_data = train_test_split(
            train_val_data,
            test_size=val_size_adjusted,
            random_state=42,
            stratify=train_val_data[label_column] if label_column in train_val_data.columns else None
        )
        
        # Save validation data
        validation_data.to_csv(validation_file_path, index=False)
        print(f"Validation set size: {len(validation_data)} samples ({len(validation_data)/len(data)*100:.1f}%)")
        print(f"Validation set saved to: {validation_file_path}")
        globals()['validation_data'] = validation_data
    else:
        train_data = train_val_data
    
    # Save the split datasets to files
    train_data.to_csv(train_file_path, index=False)
    test_data.to_csv(test_file_path, index=False)
    
    # Store in global scope
    globals()['train_data'] = train_data
    globals()['test_data'] = test_data
    globals()['data'] = data
    
    print(f"Data splitting with key preservation completed:")
    print(f"Original dataset size: {len(data)} samples")
    print(f"Training set size: {len(train_data)} samples ({len(train_data)/len(data)*100:.1f}%)")
    print(f"Test set size: {len(test_data)} samples ({len(test_data)/len(data)*100:.1f}%)")
    print(f"Primary key column: {primary_feature_key_id}")
    print(f"Training set saved to: {train_file_path}")
    print(f"Test set saved to: {test_file_path}")
    
    # Verify key uniqueness
    train_keys = set(train_data[primary_feature_key_id])
    test_keys = set(test_data[primary_feature_key_id])
    if validation_file_path and validation_file_path != 'None':
        val_keys = set(validation_data[primary_feature_key_id])
        overlap = train_keys.intersection(test_keys).union(train_keys.intersection(val_keys)).union(test_keys.intersection(val_keys))
    else:
        overlap = train_keys.intersection(test_keys)
    
    if overlap:
        print(f"Warning: Found {len(overlap)} overlapping keys between splits")
    else:
        print("? No key overlap between splits - data integrity maintained")
    
    split_with_key_successful = True
    
except FileNotFoundError:
    print(f"Error: Data file not found at: {data_file_path}")
    split_with_key_successful = False
    
except Exception as e:
    print(f"Error during data splitting with key: {str(e)}")
    split_with_key_successful = False

# Store the result
globals()['split_with_key_successful'] = split_with_key_successful