# Load data with features from file
import pandas as pd

# Parameters
file_path = '{file_path}'
selected_features = {selected_features}

try:
    # Load the dataset
    print(f"Loading data from: {file_path}")
    data = pd.read_csv(file_path)
    print(f"Data loaded successfully. Shape: {data.shape}")
    
    # Validate selected features exist in the dataset
    available_columns = data.columns.tolist()
    valid_features = []
    
    for feature in selected_features:
        if feature in available_columns:
            valid_features.append(feature)
        else:
            print(f"Warning: Feature '{feature}' not found in dataset")
    
    if not valid_features:
        print("Error: None of the selected features exist in the dataset")
        print(f"Available columns: {available_columns}")
        load_data_with_features_successful = False
        globals()['load_data_with_features_successful'] = load_data_with_features_successful
        exit()
    
    # Filter the data based on valid selected features
    data = data[valid_features]
    print(f"Data filtered to {len(valid_features)} features")
    
    # Get the final list of features after filtering
    features = data.columns.tolist()
    
    # Store the filtered data back to the global scope
    globals()['data'] = data
    globals()['features'] = features
    
    print(f"Features: {features}")
    print(f"Final dataset shape: {data.shape}")
    
    load_data_with_features_successful = True
    
except FileNotFoundError:
    print(f"Error: File not found at path: {file_path}")
    load_data_with_features_successful = False
    
except pd.errors.EmptyDataError:
    print(f"Error: The file {file_path} is empty")
    load_data_with_features_successful = False
    
except pd.errors.ParserError as e:
    print(f"Error parsing file: {str(e)}")
    load_data_with_features_successful = False
    
except Exception as e:
    print(f"Error loading data with features: {str(e)}")
    load_data_with_features_successful = False

# Store the result
globals()['load_data_with_features_successful'] = load_data_with_features_successful