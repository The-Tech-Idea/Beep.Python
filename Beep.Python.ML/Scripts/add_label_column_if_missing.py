# Add label column if missing from test data in memory
import pandas as pd

# Parameters
label_column = '{label_column}'

# Check if the label column is missing and add it if necessary
if 'test_data' in globals():
    if label_column not in test_data.columns:
        test_data[label_column] = None  # Assign None for missing label column
        globals()['test_data'] = test_data
        print(f"Added missing label column '{label_column}' to test data")
    else:
        print(f"Label column '{label_column}' already exists in test data")
else:
    print("Warning: test_data not available in global scope")