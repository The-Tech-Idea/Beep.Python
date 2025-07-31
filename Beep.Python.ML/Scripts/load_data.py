import pandas as pd

# Load the dataset
data = pd.read_csv('{file_path}')

# Get the list of features (column names)
features = data.columns.tolist()

# Store the data in the global scope
globals()['data'] = data