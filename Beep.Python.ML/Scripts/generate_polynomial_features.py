# Generate polynomial features
from sklearn.preprocessing import PolynomialFeatures
import pandas as pd

def generate_polynomial_features(df, selected_features=None, degree=2, include_bias=True, interaction_only=False):
    if selected_features is None or len(selected_features) == 0:
        # If no features are selected, use all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    if len(selected_features) > 0:
        poly = PolynomialFeatures(degree=degree, include_bias=include_bias, interaction_only=interaction_only)
        poly_features = poly.fit_transform(df[selected_features])
        poly_feature_names = poly.get_feature_names_out(selected_features)
        
        df_poly = pd.DataFrame(data=poly_features, columns=poly_feature_names, index=df.index)
        
        # Drop the original features and add the polynomial features
        df = df.drop(columns=selected_features)
        df = pd.concat([df, df_poly], axis=1)
    return df

selected_features = {selected_features}
degree = {degree}
include_bias = {include_bias}
interaction_only = {interaction_only}

if 'train_data' in globals():
    train_data = generate_polynomial_features(train_data, selected_features, degree, include_bias, interaction_only)
if 'test_data' in globals():
    test_data = generate_polynomial_features(test_data, selected_features, degree, include_bias, interaction_only)
if 'data' in globals():
    data = generate_polynomial_features(data, selected_features, degree, include_bias, interaction_only)