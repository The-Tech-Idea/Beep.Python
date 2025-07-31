# Perform stratified sampling to split data
from sklearn.model_selection import train_test_split
import pandas as pd

# Parameters
test_size = {test_size}
train_file_path = '{train_file_path}'
test_file_path = '{test_file_path}'

# Perform stratified sampling to split the data
if 'data' in globals() and 'label_column' in globals():
    try:
        # Perform stratified train-test split
        train_data, test_data = train_test_split(
            data, 
            test_size=test_size, 
            stratify=data[label_column],
            random_state=42
        )
        
        # Save the split datasets to files
        train_data.to_csv(train_file_path, index=False)
        test_data.to_csv(test_file_path, index=False)
        
        # Store in global scope
        globals()['train_data'] = train_data
        globals()['test_data'] = test_data
        
        print(f"Stratified sampling completed:")
        print(f"Training set size: {len(train_data)} samples")
        print(f"Test set size: {len(test_data)} samples")
        print(f"Training set saved to: {train_file_path}")
        print(f"Test set saved to: {test_file_path}")
        
        # Display class distribution
        train_dist = train_data[label_column].value_counts(normalize=True)
        test_dist = test_data[label_column].value_counts(normalize=True)
        
        print("\nClass distribution in training set:")
        for class_label, proportion in train_dist.items():
            print(f"  {class_label}: {proportion:.3f}")
            
        print("\nClass distribution in test set:")
        for class_label, proportion in test_dist.items():
            print(f"  {class_label}: {proportion:.3f}")
            
    except Exception as e:
        print(f"Error during stratified sampling: {str(e)}")
        # Fallback to regular train_test_split without stratification
        try:
            train_data, test_data = train_test_split(
                data, 
                test_size=test_size,
                random_state=42
            )
            
            train_data.to_csv(train_file_path, index=False)
            test_data.to_csv(test_file_path, index=False)
            
            globals()['train_data'] = train_data
            globals()['test_data'] = test_data
            
            print(f"Regular sampling completed (stratification failed):")
            print(f"Training set size: {len(train_data)} samples")
            print(f"Test set size: {len(test_data)} samples")
            
        except Exception as e2:
            print(f"Sampling failed completely: {str(e2)}")
            
else:
    print("Warning: 'data' DataFrame or 'label_column' not available for stratified sampling.")