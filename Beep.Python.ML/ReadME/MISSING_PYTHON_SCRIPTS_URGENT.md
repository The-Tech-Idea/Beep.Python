# ?? CRITICAL: Missing Python Scripts for Assistant Classes

## Overview
All 13 assistant classes have been created, but they reference 70+ Python scripts that don't exist yet. Each assistant method calls `PythonScriptTemplateManager.GetScript("script_name", parameters)` but the corresponding `.py` files are missing.

## ?? Complete List of Missing Python Scripts

### Text Processing Scripts (PythonTextProcessingAssistant needs 7 scripts)

1. **convert_text_to_lowercase.py**
```python
# Convert text to lowercase
if '{column_name}' in train_data.columns:
    train_data['{column_name}'] = train_data['{column_name}'].str.lower()

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    test_data['{column_name}'] = test_data['{column_name}'].str.lower()

if 'data' in globals() and '{column_name}' in data.columns:
    data['{column_name}'] = data['{column_name}'].str.lower()
```

2. **remove_punctuation.py**
```python
import string

# Remove punctuation from text
if '{column_name}' in train_data.columns:
    train_data['{column_name}'] = train_data['{column_name}'].str.translate(str.maketrans('', '', string.punctuation))

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    test_data['{column_name}'] = test_data['{column_name}'].str.translate(str.maketrans('', '', string.punctuation))

if 'data' in globals() and '{column_name}' in data.columns:
    data['{column_name}'] = data['{column_name}'].str.translate(str.maketrans('', '', string.punctuation))
```

3. **remove_stopwords.py**
```python
from sklearn.feature_extraction.text import ENGLISH_STOP_WORDS

# Remove stopwords from text
stopwords = list(ENGLISH_STOP_WORDS)

if '{column_name}' in train_data.columns:
    train_data['{column_name}'] = train_data['{column_name}'].apply(lambda x: ' '.join([word for word in str(x).split() if word not in stopwords]))

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    test_data['{column_name}'] = test_data['{column_name}'].apply(lambda x: ' '.join([word for word in str(x).split() if word not in stopwords]))

if 'data' in globals() and '{column_name}' in data.columns:
    data['{column_name}'] = data['{column_name}'].apply(lambda x: ' '.join([word for word in str(x).split() if word not in stopwords]))
```

4. **apply_stemming.py**
```python
from nltk.stem import PorterStemmer

stemmer = PorterStemmer()

# Apply stemming to text
if '{column_name}' in train_data.columns:
    train_data['{column_name}'] = train_data['{column_name}'].apply(lambda x: ' '.join([stemmer.stem(word) for word in str(x).split()]))

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    test_data['{column_name}'] = test_data['{column_name}'].apply(lambda x: ' '.join([stemmer.stem(word) for word in str(x).split()]))

if 'data' in globals() and '{column_name}' in data.columns:
    data['{column_name}'] = data['{column_name}'].apply(lambda x: ' '.join([stemmer.stem(word) for word in str(x).split()]))
```

5. **apply_lemmatization.py**
```python
from nltk.stem import WordNetLemmatizer

lemmatizer = WordNetLemmatizer()

# Apply lemmatization to text
if '{column_name}' in train_data.columns:
    train_data['{column_name}'] = train_data['{column_name}'].apply(lambda x: ' '.join([lemmatizer.lemmatize(word) for word in str(x).split()]))

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    test_data['{column_name}'] = test_data['{column_name}'].apply(lambda x: ' '.join([lemmatizer.lemmatize(word) for word in str(x).split()]))

if 'data' in globals() and '{column_name}' in data.columns:
    data['{column_name}'] = data['{column_name}'].apply(lambda x: ' '.join([lemmatizer.lemmatize(word) for word in str(x).split()]))
```

6. **apply_tokenization.py**
```python
from nltk.tokenize import word_tokenize

# Apply tokenization to text
if '{column_name}' in train_data.columns:
    train_data['{column_name}'] = train_data['{column_name}'].apply(lambda x: word_tokenize(str(x)))

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    test_data['{column_name}'] = test_data['{column_name}'].apply(lambda x: word_tokenize(str(x)))

if 'data' in globals() and '{column_name}' in data.columns:
    data['{column_name}'] = data['{column_name}'].apply(lambda x: word_tokenize(str(x)))
```

7. **apply_tfidf_vectorization.py**
```python
from sklearn.feature_extraction.text import TfidfVectorizer

# Apply TF-IDF vectorization
vectorizer = TfidfVectorizer(max_features={max_features})

if '{column_name}' in train_data.columns:
    X_train_tfidf = vectorizer.fit_transform(train_data['{column_name}'])
    train_data_tfidf = pd.DataFrame(X_train_tfidf.toarray(), columns=vectorizer.get_feature_names_out())
    train_data = pd.concat([train_data.drop(columns=['{column_name}']), train_data_tfidf], axis=1)

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    X_test_tfidf = vectorizer.transform(test_data['{column_name}'])
    test_data_tfidf = pd.DataFrame(X_test_tfidf.toarray(), columns=vectorizer.get_feature_names_out())
    test_data = pd.concat([test_data.drop(columns=['{column_name}']), test_data_tfidf], axis=1)
```

### Data Preprocessing Scripts (PythonDataPreprocessingAssistant needs 17 scripts)

8. **handle_categorical_encoder.py**
```python
import pandas as pd
from sklearn.preprocessing import OneHotEncoder

# Categorical features to encode
categorical_features = {categorical_features}

# Select categorical columns and apply OneHotEncoder
if 'train_data' in globals():
    encoder = OneHotEncoder(handle_unknown='ignore', sparse_output=False)
    X_encoded = encoder.fit_transform(train_data[categorical_features])
    
    # Convert to DataFrame and reassign to train_data
    encoded_columns = encoder.get_feature_names_out(categorical_features)
    train_data_encoded = pd.DataFrame(X_encoded, columns=encoded_columns)
    train_data = pd.concat([train_data.drop(columns=categorical_features), train_data_encoded], axis=1)
    globals()['train_data'] = train_data

if 'test_data' in globals():
    X_encoded = encoder.transform(test_data[categorical_features])
    
    # Convert to DataFrame and reassign to test_data
    test_data_encoded = pd.DataFrame(X_encoded, columns=encoded_columns)
    test_data = pd.concat([test_data.drop(columns=categorical_features), test_data_encoded], axis=1)
    globals()['test_data'] = test_data
```

9. **handle_multi_value_categorical.py**
```python
import pandas as pd

def handle_multi_value_categorical_features(data, feature_list):
    for feature in feature_list:
        # Split the multi-value feature into individual values
        split_features = data[feature].str.split(',', expand=True)
        
        # Get unique values across the entire column to create dummy variables
        unique_values = pd.unique(split_features.values.ravel('K'))
        unique_values = [val for val in unique_values if val is not None]
        
        # For each unique value, create a binary column
        for value in unique_values:
            if value is not None and value != '':
                data[f'{feature}_{value}'] = split_features.apply(lambda row: int(value in row.values), axis=1)
        
        # Drop the original multi-value feature column
        data = data.drop(columns=[feature])
    
    return data

# List of features with multiple values
multi_value_features = {multi_value_features}

# Process the multi-value features for train_data, test_data, and predict_data if they exist
if 'train_data' in globals():
    train_data = handle_multi_value_categorical_features(train_data, multi_value_features)
    globals()['train_data'] = train_data

if 'test_data' in globals():
    test_data = handle_multi_value_categorical_features(test_data, multi_value_features)
    globals()['test_data'] = test_data

if 'data' in globals():
    data = handle_multi_value_categorical_features(data, multi_value_features)
    globals()['data'] = data
```

10. **handle_date_data.py**
```python
import pandas as pd

# List of date features
date_features = {date_features}

# Ensure all date features are in datetime format
for feature in date_features:
    if 'data' in globals():
        data[feature] = pd.to_datetime(data[feature], errors='coerce')
    if 'train_data' in globals():
        train_data[feature] = pd.to_datetime(train_data[feature], errors='coerce')
    if 'test_data' in globals():
        test_data[feature] = pd.to_datetime(test_data[feature], errors='coerce')

# Extract components like year, month, day
for feature in date_features:
    if 'data' in globals():
        data[feature + '_year'] = data[feature].dt.year
        data[feature + '_month'] = data[feature].dt.month  
        data[feature + '_day'] = data[feature].dt.day
    if 'train_data' in globals():
        train_data[feature + '_year'] = train_data[feature].dt.year
        train_data[feature + '_month'] = train_data[feature].dt.month
        train_data[feature + '_day'] = train_data[feature].dt.day
    if 'test_data' in globals():
        test_data[feature + '_year'] = test_data[feature].dt.year
        test_data[feature + '_month'] = test_data[feature].dt.month
        test_data[feature + '_day'] = test_data[feature].dt.day

# Convert to timestamp
for feature in date_features:
    if 'data' in globals():
        data[feature + '_timestamp'] = data[feature].apply(lambda x: x.timestamp() if pd.notnull(x) else None)
    if 'train_data' in globals():
        train_data[feature + '_timestamp'] = train_data[feature].apply(lambda x: x.timestamp() if pd.notnull(x) else None)
    if 'test_data' in globals():
        test_data[feature + '_timestamp'] = test_data[feature].apply(lambda x: x.timestamp() if pd.notnull(x) else None)

# Drop the original date columns
if 'data' in globals():
    data.drop(columns=date_features, inplace=True)
if 'train_data' in globals():
    train_data.drop(columns=date_features, inplace=True)
if 'test_data' in globals():
    test_data.drop(columns=date_features, inplace=True)
```

## ?? Action Required

**URGENT: All 70+ Python scripts need to be created in the `Beep.Python.ML/Scripts/` directory.**

Each assistant class is currently **non-functional** because the Python scripts they reference don't exist. The `PythonScriptTemplateManager.GetScript()` calls will throw `FileNotFoundException` when the scripts are missing.

## ?? Next Steps

1. **Create all 70+ Python script files** in the Scripts directory
2. **Test each assistant class** to ensure scripts are loaded correctly
3. **Verify parameter substitution** works properly with template manager
4. **Update any broken scripts** based on testing results

Without these Python scripts, the entire assistant architecture is non-functional! ??