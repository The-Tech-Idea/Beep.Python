# Split data from file with train/test outputs
from sklearn.model_selection import train_test_split
import pandas as pd

# Parameters
data_file_path = '{data_file_path}'
test_size = {test_size}
train_file_path = '{train_file_path}'
test_file_path = '{test_file_path}'

try:
    # Load data from file
    data = pd.read_csv(data_file_path)
    
    # Simple train-test split
    train_data, test_data = train_test_split(
        data, 
        test_size=test_size,
        random_state=42
    )
    
    # Save the split datasets to files
    train_data.to_csv(train_file_path, index=False)
    test_data.to_csv(test_file_path, index=False)
    
    # Store in global scope
    globals()['train_data'] = train_data
    globals()['test_data'] = test_data
    globals()['data'] = data
    
    print(f"Data splitting from file completed:")
    print(f"Source file: {data_file_path}")
    print(f"Original dataset size: {len(data)} samples")
    print(f"Training set size: {len(train_data)} samples ({len(train_data)/len(data)*100:.1f}%)")
    print(f"Test set size: {len(test_data)} samples ({len(test_data)/len(data)*100:.1f}%)")
    print(f"Training set saved to: {train_file_path}")
    print(f"Test set saved to: {test_file_path}")
    
    split_from_file_successful = True
    
except FileNotFoundError:
    print(f"Error: Data file not found at: {data_file_path}")
    split_from_file_successful = False
    
except Exception as e:
    print(f"Error during data splitting from file: {str(e)}")
    split_from_file_successful = False

# Store the result
globals()['split_from_file_successful'] = split_from_file_successful