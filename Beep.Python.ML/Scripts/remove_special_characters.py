# Remove special characters from data
import pandas as pd
import re

def remove_special_characters_from_data(df):
    for col in df.columns:
        if df[col].dtype == 'object':  # Checking if the column is of string type
            # Apply the regex to each element in the column to remove special characters
            df[col] = df[col].apply(lambda x: re.sub(r"[^a-zA-Z0-9_]+", '', str(x)))
    return df

dataframe_name = '{dataframe_name}'

# Apply the function to the specified DataFrame
if dataframe_name in globals():
    globals()[dataframe_name] = remove_special_characters_from_data(globals()[dataframe_name])
elif 'data' in globals():
    data = remove_special_characters_from_data(data)
elif 'train_data' in globals():
    train_data = remove_special_characters_from_data(train_data)
elif 'test_data' in globals():
    test_data = remove_special_characters_from_data(test_data)