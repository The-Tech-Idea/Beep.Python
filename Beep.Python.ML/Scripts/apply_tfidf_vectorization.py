# Apply TF-IDF vectorization
from sklearn.feature_extraction.text import TfidfVectorizer

vectorizer = TfidfVectorizer(max_features={max_features})

if '{column_name}' in train_data.columns:
    X_train_tfidf = vectorizer.fit_transform(train_data['{column_name}'])
    train_data_tfidf = pd.DataFrame(X_train_tfidf.toarray(), columns=vectorizer.get_feature_names_out())
    train_data = pd.concat([train_data.drop(columns=['{column_name}']), train_data_tfidf], axis=1)

if 'test_data' in globals() and '{column_name}' in test_data.columns:
    X_test_tfidf = vectorizer.transform(test_data['{column_name}'])
    test_data_tfidf = pd.DataFrame(X_test_tfidf.toarray(), columns=vectorizer.get_feature_names_out())
    test_data = pd.concat([test_data.drop(columns=['{column_name}']), test_data_tfidf], axis=1)

if 'data' in globals() and '{column_name}' in data.columns:
    X_data_tfidf = vectorizer.transform(data['{column_name}'])
    data_tfidf = pd.DataFrame(X_data_tfidf.toarray(), columns=vectorizer.get_feature_names_out())
    data = pd.concat([data.drop(columns=['{column_name}']), data_tfidf], axis=1)