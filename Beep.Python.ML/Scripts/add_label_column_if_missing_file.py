# Add label column if missing from test data
import pandas as pd

# Parameters
test_data_file_path = '{test_data_file_path}'
label_column = '{label_column}'

try:
    # Load test data
    test_data = pd.read_csv(test_data_file_path)
    
    # Check if the label column is missing and add it if necessary
    if label_column not in test_data.columns:
        test_data[label_column] = None  # Assign None for missing label column
        print(f"Added missing label column '{label_column}' to test data")
    else:
        print(f"Label column '{label_column}' already exists in test data")
    
    # Save the modified test data back to file
    test_data.to_csv(test_data_file_path, index=False)
    
    # Store in global scope
    globals()['test_data'] = test_data
    
    print(f"Test data processed and saved to: {test_data_file_path}")
    
except FileNotFoundError:
    print(f"Error: Test data file not found at: {test_data_file_path}")
except Exception as e:
    print(f"Error processing test data: {str(e)}")