# Apply binning to features
import pandas as pd
import numpy as np

def apply_binning(df, selected_features, number_of_bins=5, encode_as_ordinal=True):
    for feature in selected_features:
        if feature in df.columns:
            try:
                if encode_as_ordinal:
                    df[feature + '_binned'] = pd.cut(df[feature], bins=number_of_bins, labels=False)
                else:
                    df[feature + '_binned'] = pd.cut(df[feature], bins=number_of_bins)
            except Exception as e:
                print(f"Error binning feature {feature}: {e}")
                continue
    return df

selected_features = {selected_features}
number_of_bins = {number_of_bins}
encode_as_ordinal = {encode_as_ordinal}

if 'train_data' in globals():
    train_data = apply_binning(train_data, selected_features, number_of_bins, encode_as_ordinal)
if 'test_data' in globals():
    test_data = apply_binning(test_data, selected_features, number_of_bins, encode_as_ordinal)
if 'data' in globals():
    data = apply_binning(data, selected_features, number_of_bins, encode_as_ordinal)