# Three-way data split (train/test/validation)
from sklearn.model_selection import train_test_split
import pandas as pd

# Parameters
data_file_path = '{data_file_path}'
test_size = {test_size}
validation_size = {validation_size}
train_file_path = '{train_file_path}'
test_file_path = '{test_file_path}'
validation_file_path = '{validation_file_path}'

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
        three_way_split_successful = False
        globals()['three_way_split_successful'] = three_way_split_successful
        exit()
    
    # First split: separate out test set
    train_val_data, test_data = train_test_split(
        data, 
        test_size=test_size,
        random_state=42
    )
    
    # Calculate validation size relative to remaining data
    val_size_adjusted = validation_size / (1 - test_size)
    
    # Second split: separate train and validation from remaining data
    train_data, validation_data = train_test_split(
        train_val_data,
        test_size=val_size_adjusted,
        random_state=42
    )
    
    # Save the split datasets to files
    train_data.to_csv(train_file_path, index=False)
    test_data.to_csv(test_file_path, index=False)
    validation_data.to_csv(validation_file_path, index=False)
    
    # Store in global scope
    globals()['train_data'] = train_data
    globals()['test_data'] = test_data
    globals()['validation_data'] = validation_data
    globals()['data'] = data
    
    print(f"Three-way data splitting completed:")
    print(f"Original dataset size: {len(data)} samples")
    print(f"Training set size: {len(train_data)} samples ({len(train_data)/len(data)*100:.1f}%)")
    print(f"Test set size: {len(test_data)} samples ({len(test_data)/len(data)*100:.1f}%)")
    print(f"Validation set size: {len(validation_data)} samples ({len(validation_data)/len(data)*100:.1f}%)")
    print(f"Training set saved to: {train_file_path}")
    print(f"Test set saved to: {test_file_path}")
    print(f"Validation set saved to: {validation_file_path}")
    
    three_way_split_successful = True
    
except FileNotFoundError:
    print(f"Error: Data file not found at: {data_file_path}")
    three_way_split_successful = False
    
except Exception as e:
    print(f"Error during three-way data splitting: {str(e)}")
    three_way_split_successful = False

# Store the result
globals()['three_way_split_successful'] = three_way_split_successful