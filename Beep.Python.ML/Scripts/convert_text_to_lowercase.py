# Convert text to lowercase
if '{column_name}' in train_data.columns:
    train_data['{column_name}'] = train_data['{column_name}'].str.lower()

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    test_data['{column_name}'] = test_data['{column_name}'].str.lower()

if 'data' in globals() and '{column_name}' in data.columns:
    data['{column_name}'] = data['{column_name}'].str.lower()