# Split data from class-based file structure
import pandas as pd
import os
from sklearn.model_selection import train_test_split

# Parameters
url_path = '{url_path}'
filename = '{filename}'
split_ratio = {split_ratio}

try:
    # Construct full file path
    if url_path and filename:
        full_path = os.path.join(url_path, filename)
    elif filename:
        full_path = filename
    else:
        print("Error: No file path provided")
        split_class_file_successful = False
        globals()['split_class_file_successful'] = split_class_file_successful
        exit()
    
    # Load data from file
    if os.path.exists(full_path):
        data = pd.read_csv(full_path)
        print(f"Loaded data from: {full_path}")
    else:
        print(f"Error: File not found at: {full_path}")
        split_class_file_successful = False
        globals()['split_class_file_successful'] = split_class_file_successful
        exit()
    
    # Validate split ratio
    if split_ratio <= 0 or split_ratio >= 1:
        print(f"Warning: Invalid split ratio {split_ratio}. Using default 0.8")
        split_ratio = 0.8
    
    # Determine train size (split_ratio is for training)
    train_size = split_ratio
    test_size = 1 - split_ratio
    
    # Perform the split
    # If there's a clear label column (last column or named 'label', 'class', 'target')
    label_candidates = ['label', 'class', 'target', 'y']
    label_column = None
    
    for candidate in label_candidates:
        if candidate in data.columns:
            label_column = candidate
            break
    
    # If no standard label column found, use the last column
    if label_column is None and len(data.columns) > 1:
        label_column = data.columns[-1]
        print(f"Using last column '{label_column}' as label column")
    
    # Split the data
    if label_column and label_column in data.columns:
        # Stratified split to maintain class distribution
        try:
            train_data, test_data = train_test_split(
                data, 
                train_size=train_size,
                random_state=42,
                stratify=data[label_column]
            )
            print(f"Performed stratified split using label column: {label_column}")
        except ValueError:
            # Fallback to regular split if stratification fails
            train_data, test_data = train_test_split(
                data, 
                train_size=train_size,
                random_state=42
            )
            print(f"Performed regular split (stratification failed)")
    else:
        # Regular split without stratification
        train_data, test_data = train_test_split(
            data, 
            train_size=train_size,
            random_state=42
        )
        print("Performed regular split (no label column identified)")
    
    # Generate output file names
    base_name = os.path.splitext(filename)[0] if filename else "data"
    train_filename = f"{base_name}_train.csv"
    test_filename = f"{base_name}_test.csv"
    
    # Save to the same directory as input or current directory
    output_dir = url_path if url_path else "."
    train_path = os.path.join(output_dir, train_filename)
    test_path = os.path.join(output_dir, test_filename)
    
    train_data.to_csv(train_path, index=False)
    test_data.to_csv(test_path, index=False)
    
    # Store in global scope
    globals()['train_data'] = train_data
    globals()['test_data'] = test_data
    globals()['data'] = data
    
    print(f"Class-based data splitting completed:")
    print(f"Original dataset size: {len(data)} samples")
    print(f"Training set size: {len(train_data)} samples ({len(train_data)/len(data)*100:.1f}%)")
    print(f"Test set size: {len(test_data)} samples ({len(test_data)/len(data)*100:.1f}%)")
    print(f"Split ratio: {split_ratio:.2f}")
    print(f"Training set saved to: {train_path}")
    print(f"Test set saved to: {test_path}")
    
    # Store file paths for return
    globals()['train_file_output'] = train_filename
    globals()['test_file_output'] = test_filename
    
    if label_column:
        # Show class distribution
        train_dist = train_data[label_column].value_counts()
        test_dist = test_data[label_column].value_counts()
        
        print(f"\nClass distribution in training set:")
        for class_name, count in train_dist.items():
            print(f"  {class_name}: {count} ({count/len(train_data)*100:.1f}%)")
        
        print(f"\nClass distribution in test set:")
        for class_name, count in test_dist.items():
            print(f"  {class_name}: {count} ({count/len(test_data)*100:.1f}%)")
    
    split_class_file_successful = True
    
except Exception as e:
    print(f"Error during class-based file splitting: {str(e)}")
    split_class_file_successful = False

# Store the result
globals()['split_class_file_successful'] = split_class_file_successful