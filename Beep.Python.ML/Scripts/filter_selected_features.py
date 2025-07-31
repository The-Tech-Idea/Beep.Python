# List of selected features
selected_features = {selected_features}

# Filter train_data, test_data, and data based on selected features
if 'train_data' in globals():
    train_data = train_data[selected_features]
if 'test_data' in globals():
    test_data = test_data[selected_features]
if 'data' in globals():
    data = data[selected_features]

# Update the datasets in the Python scope
globals()['train_data'] = train_data if 'train_data' in globals() else None
globals()['test_data'] = test_data if 'test_data' in globals() else None
globals()['data'] = data if 'data' in globals() else None