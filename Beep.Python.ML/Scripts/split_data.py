# Split data into training and testing sets
import pandas as pd
from sklearn.model_selection import train_test_split

# Parameters
data_file_path = '{data_file_path}'
test_size = {test_size}
validation_size = {validation_size}
train_file_path = '{train_file_path}'
test_file_path = '{test_file_path}'
validation_file_path = '{validation_file_path}'
primary_feature_key_id = '{primary_feature_key_id}'
label_column = '{label_column}'
random_state = {random_state}
stratify = {stratify}

try:
    # Load data if file path is provided
    if data_file_path and data_file_path != 'None':
        try:
            data = pd.read_csv(data_file_path)
            print(f"Data loaded from: {data_file_path}")
            print(f"Data shape: {data.shape}")
        except FileNotFoundError:
            print(f"Error: Data file not found at: {data_file_path}")
            split_data_successful = False
            globals()['split_data_successful'] = split_data_successful
            exit()
        except Exception as e:
            print(f"Error loading data: {str(e)}")
            split_data_successful = False
            globals()['split_data_successful'] = split_data_successful
            exit()
    elif 'data' in globals():
        # Use existing data from global scope
        print("Using existing data from global scope")
        print(f"Data shape: {data.shape}")
    else:
        print("Error: No data available for splitting")
        split_data_successful = False
        globals()['split_data_successful'] = split_data_successful
        exit()
    
    print(f"\n=== Data Splitting Configuration ===")
    print(f"Test size: {test_size}")
    print(f"Validation size: {validation_size}")
    print(f"Label column: {label_column}")
    print(f"Random state: {random_state}")
    print(f"Stratify: {stratify}")
    
    # Prepare features and target
    if label_column and label_column != 'None' and label_column in data.columns:
        # Separate features and target
        X = data.drop(columns=[label_column])
        y = data[label_column]
        
        # Remove primary key if specified
        if primary_feature_key_id and primary_feature_key_id != 'None' and primary_feature_key_id in X.columns:
            X = X.drop(columns=[primary_feature_key_id])
            print(f"Removed primary key column: {primary_feature_key_id}")
        
        # Determine stratification
        stratify_target = None
        if stratify and len(y.unique()) < 50:  # Only stratify for classification with reasonable number of classes
            stratify_target = y
            print("Using stratified split for classification")
        
        # Perform splitting based on validation size
        if validation_size and validation_size > 0:
            # Three-way split: train, test, validation
            print(f"Performing three-way split...")
            
            # First split: separate out validation set
            X_temp, X_val, y_temp, y_val = train_test_split(
                X, y, 
                test_size=validation_size, 
                random_state=random_state,
                stratify=stratify_target
            )
            
            # Second split: split remaining into train and test
            adjusted_test_size = test_size / (1 - validation_size)  # Adjust test size for remaining data
            X_train, X_test, y_train, y_test = train_test_split(
                X_temp, y_temp,
                test_size=adjusted_test_size,
                random_state=random_state,
                stratify=y_temp if stratify_target is not None else None
            )
            
            # Recreate full datasets with target column
            train_data = pd.concat([X_train, y_train], axis=1)
            test_data = pd.concat([X_test, y_test], axis=1)
            validation_data = pd.concat([X_val, y_val], axis=1)
            
            # Store in global scope
            globals()['train_data'] = train_data
            globals()['test_data'] = test_data
            globals()['validation_data'] = validation_data
            
            print(f"Train set shape: {train_data.shape}")
            print(f"Test set shape: {test_data.shape}")
            print(f"Validation set shape: {validation_data.shape}")
            
            # Save to files if paths provided
            split_file_paths = []
            
            if train_file_path and train_file_path != 'None':
                train_data.to_csv(train_file_path, index=False)
                split_file_paths.append(train_file_path)
                print(f"Training data saved to: {train_file_path}")
            
            if test_file_path and test_file_path != 'None':
                test_data.to_csv(test_file_path, index=False)
                split_file_paths.append(test_file_path)
                print(f"Test data saved to: {test_file_path}")
            
            if validation_file_path and validation_file_path != 'None':
                validation_data.to_csv(validation_file_path, index=False)
                split_file_paths.append(validation_file_path)
                print(f"Validation data saved to: {validation_file_path}")
            
            globals()['split_file_paths'] = split_file_paths
            
        else:
            # Two-way split: train and test only
            print(f"Performing two-way split...")
            
            X_train, X_test, y_train, y_test = train_test_split(
                X, y,
                test_size=test_size,
                random_state=random_state,
                stratify=stratify_target
            )
            
            # Recreate full datasets with target column
            train_data = pd.concat([X_train, y_train], axis=1)
            test_data = pd.concat([X_test, y_test], axis=1)
            
            # Store in global scope
            globals()['train_data'] = train_data
            globals()['test_data'] = test_data
            
            print(f"Train set shape: {train_data.shape}")
            print(f"Test set shape: {test_data.shape}")
            
            # Save to files if paths provided
            split_file_paths = []
            
            if train_file_path and train_file_path != 'None':
                train_data.to_csv(train_file_path, index=False)
                split_file_paths.append(train_file_path)
                print(f"Training data saved to: {train_file_path}")
            
            if test_file_path and test_file_path != 'None':
                test_data.to_csv(test_file_path, index=False)
                split_file_paths.append(test_file_path)
                print(f"Test data saved to: {test_file_path}")
            
            globals()['split_file_paths'] = split_file_paths
        
        # Display class distribution if classification
        if stratify_target is not None:
            print(f"\n=== Class Distribution ===")
            print("Training set:")
            train_distribution = y_train.value_counts(normalize=True)
            for class_val, proportion in train_distribution.items():
                print(f"  Class {class_val}: {proportion:.3f}")
            
            print("Test set:")
            test_distribution = y_test.value_counts(normalize=True)
            for class_val, proportion in test_distribution.items():
                print(f"  Class {class_val}: {proportion:.3f}")
            
            if validation_size and validation_size > 0:
                print("Validation set:")
                val_distribution = y_val.value_counts(normalize=True)
                for class_val, proportion in val_distribution.items():
                    print(f"  Class {class_val}: {proportion:.3f}")
    
    else:
        # No label column specified - split all data
        print("No label column specified - performing random split")
        
        if validation_size and validation_size > 0:
            # Three-way split without stratification
            data_temp, validation_data = train_test_split(
                data, test_size=validation_size, random_state=random_state
            )
            
            adjusted_test_size = test_size / (1 - validation_size)
            train_data, test_data = train_test_split(
                data_temp, test_size=adjusted_test_size, random_state=random_state
            )
            
            globals()['train_data'] = train_data
            globals()['test_data'] = test_data
            globals()['validation_data'] = validation_data
            
        else:
            # Two-way split without stratification
            train_data, test_data = train_test_split(
                data, test_size=test_size, random_state=random_state
            )
            
            globals()['train_data'] = train_data
            globals()['test_data'] = test_data
    
    print(f"\nData splitting completed successfully!")
    split_data_successful = True
    
except Exception as e:
    print(f"Error during data splitting: {str(e)}")
    split_data_successful = False

# Store the result
globals()['split_data_successful'] = split_data_successful