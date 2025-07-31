# Export test results to CSV file
import pandas as pd

# Parameters
file_path = '{file_path}'
id_column = '{id_column}'
label_column = '{label_column}'

try:
    # Check if we have the necessary data
    if 'predict_data' in globals() and 'predictions' in globals():
        # Create output DataFrame with ID and predictions
        output = pd.DataFrame({
            id_column: predict_data[id_column] if id_column in predict_data.columns else range(len(predictions)),
            label_column: predictions
        })
        
        # Save to CSV file
        output.to_csv(file_path, index=False)
        
        print(f"Test results exported successfully to: {file_path}")
        print(f"Number of predictions: {len(predictions)}")
        print(f"Columns: {list(output.columns)}")
        
        # Store the output in global scope
        globals()['export_output'] = output
        
    elif 'test_data' in globals() and 'predictions' in globals():
        # Alternative: use test_data if predict_data is not available
        output = pd.DataFrame({
            id_column: test_data[id_column] if id_column in test_data.columns else range(len(predictions)),
            label_column: predictions
        })
        
        output.to_csv(file_path, index=False)
        print(f"Test results exported successfully to: {file_path}")
        globals()['export_output'] = output
        
    else:
        print("Warning: Required data (predict_data/test_data and predictions) not available for export")
        
except Exception as e:
    print(f"Error exporting test results: {str(e)}")