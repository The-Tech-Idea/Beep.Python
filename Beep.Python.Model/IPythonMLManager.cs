using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    /// <summary>
    /// Enhanced interface for Python machine learning operations with session management and virtual environment support
    /// </summary>
    public interface IPythonMLManager : IDisposable
    {
        #region State Properties
        bool IsDataLoaded { get; set; }
        bool IsModelTrained { get; set; }
        bool IsModelSaved { get; set; }
        bool IsModelLoaded { get; set; }
        bool IsModelPredicted { get; set; }
        bool IsModelScored { get; set; }
        bool IsModelExported { get; set; }
        bool IsDataSplit { get; set; }
        string DataFilePath { get; set; }
        string ModelFilePath { get; set; }
        string PredictionsFilePath { get; set; }
        string TrainingFilePath { get; set; }
        string TestingFilePath { get; set; }
        string ValidationFilePath { get; set; }
        bool IsInitialized { get; }
        #endregion

        #region Session Management
        /// <summary>
        /// Configure the ML manager to use a specific Python session and virtual environment
        /// </summary>
        /// <param name="session">Pre-existing Python session to use for execution</param>
        /// <param name="virtualEnvironment">Virtual environment associated with the session</param>
        /// <returns>True if configuration successful</returns>
        bool ConfigureMLSession(PythonSessionInfo session, PythonVirtualEnvironment virtualEnvironment);

        /// <summary>
        /// Configure session using username and optional environment ID with ML-specific initialization
        /// </summary>
        /// <param name="username">Username for session creation</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <returns>True if configuration successful</returns>
        bool ConfigureMLSessionForUser(string username, string? environmentId = null);

        /// <summary>
        /// Get the currently configured session
        /// </summary>
        /// <returns>The configured Python session, or null if not configured</returns>
        PythonSessionInfo? GetConfiguredSession();

        /// <summary>
        /// Get the currently configured virtual environment
        /// </summary>
        /// <returns>The configured virtual environment, or null if not configured</returns>
        PythonVirtualEnvironment? GetConfiguredVirtualEnvironment();

        /// <summary>
        /// Check if session is properly configured for ML operations
        /// </summary>
        /// <returns>True if session and environment are configured</returns>
        bool IsSessionConfigured();
        #endregion

        #region Python Module Management
        /// <summary>
        /// Import a Python module in the current session
        /// </summary>
        /// <param name="moduleName">Module name to import (e.g., "numpy as np")</param>
        void ImportPythonModule(string moduleName);
        #endregion

        #region Data Loading and Validation
        /// <summary>
        /// Validate and preview data from a file
        /// </summary>
        /// <param name="filePath">Path to the data file</param>
        /// <param name="numRows">Number of rows to preview</param>
        /// <returns>Array of strings containing previewed data</returns>
        string[] ValidateAndPreviewData(string filePath, int numRows = 5);

        /// <summary>
        /// Load data from a file
        /// </summary>
        /// <param name="filePath">Path to the data file</param>
        /// <returns>Array of strings containing loaded data</returns>
        string[] LoadData(string filePath);

        /// <summary>
        /// Load data with selected features from a file
        /// </summary>
        /// <param name="filePath">Path to the data file</param>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        /// <returns>Array of strings containing loaded data with selected features</returns>
        string[] LoadData(string filePath, string[] selectedFeatures);

        /// <summary>
        /// Filter loaded data to include only the selected features
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        void FilterDataToSelectedFeatures(string[] selectedFeatures);

        /// <summary>
        /// Get feature names from the data file
        /// </summary>
        /// <param name="filePath">Path to the data file</param>
        /// <returns>Array of strings containing feature names</returns>
        string[] GetFeatures(string filePath);

        /// <summary>
        /// Load test data from a file
        /// </summary>
        /// <param name="filePath">Path to the test data file</param>
        /// <returns>Array of strings containing loaded test data</returns>
        string[] LoadTestData(string filePath);

        /// <summary>
        /// Load prediction data from a file
        /// </summary>
        /// <param name="filePath">Path to the prediction data file</param>
        /// <returns>Array of strings containing loaded prediction data</returns>
        string[] LoadPredictionData(string filePath);

        /// <summary>
        /// Remove special characters from a DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <returns>True if removal successful</returns>
        bool RemoveSpecialCharacters(string dataFrameName);
        #endregion

        #region Model Operations
        /// <summary>
        /// Train a machine learning model
        /// </summary>
        /// <param name="modelId">Unique identifier for the model</param>
        /// <param name="algorithm">Machine learning algorithm to use</param>
        /// <param name="parameters">Algorithm parameters</param>
        /// <param name="featureColumns">Feature column names</param>
        /// <param name="labelColumn">Label column name</param>
        void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn);

        /// <summary>
        /// Load a machine learning model from a file
        /// </summary>
        /// <param name="filePath">Path to the model file</param>
        /// <returns>Model identifier</returns>
        string LoadModel(string filePath);

        /// <summary>
        /// Save a machine learning model to a file
        /// </summary>
        /// <param name="modelId">Unique identifier for the model</param>
        /// <param name="filePath">Path to the model file</param>
        void SaveModel(string modelId, string filePath);

        /// <summary>
        /// Get model classification scores (e.g., accuracy, F1 score)
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <returns>Tuple containing accuracy and F1 score</returns>
        Tuple<double, double> GetModelClassificationScore(string modelId);

        /// <summary>
        /// Get regression model scores (e.g., MSE, RMSE, MAE)
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <returns>Tuple containing MSE, RMSE, and MAE</returns>
        Tuple<double, double, double> GetModelRegressionScores(string modelId);

        /// <summary>
        /// Predict using a trained classification model
        /// </summary>
        /// <param name="training_columns">Array of training column names</param>
        /// <returns>Predicted values</returns>
        dynamic PredictClassification(string[] training_columns);

        /// <summary>
        /// Predict using a trained regression model
        /// </summary>
        /// <param name="training_columns">Array of training column names</param>
        /// <returns>Predicted values</returns>
        dynamic PredictRegression(string[] training_columns);
        #endregion

        #region Data Splitting and Export
        /// <summary>
        /// Split data into training and testing sets
        /// </summary>
        /// <param name="testSize">Proportion of data to include in the test set</param>
        /// <param name="trainFilePath">Path to the training data file</param>
        /// <param name="testFilePath">Path to the testing data file</param>
        /// <returns>Array of strings containing file paths for training and testing data</returns>
        string[] SplitData(float testSize, string trainFilePath, string testFilePath);

        /// <summary>
        /// Split data from a file into training and testing sets
        /// </summary>
        /// <param name="dataFilePath">Path to the data file</param>
        /// <param name="testSize">Proportion of data to include in the test set</param>
        /// <param name="trainFilePath">Path to the training data file</param>
        /// <param name="testFilePath">Path to the testing data file</param>
        /// <returns>Array of strings containing file paths for training and testing data</returns>
        string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath);

        /// <summary>
        /// Split data from a file into training, testing, and validation sets
        /// </summary>
        /// <param name="dataFilePath">Path to the data file</param>
        /// <param name="testSize">Proportion of data to include in the test set</param>
        /// <param name="validationSize">Proportion of data to include in the validation set</param>
        /// <param name="trainFilePath">Path to the training data file</param>
        /// <param name="testFilePath">Path to the testing data file</param>
        /// <param name="validationFilePath">Path to the validation data file</param>
        /// <returns>Array of strings containing file paths for training, testing, and validation data</returns>
        string[] SplitData(string dataFilePath, float testSize, float validationSize, string trainFilePath, string testFilePath, string validationFilePath);

        /// <summary>
        /// Split data with specified primary key and label column
        /// </summary>
        /// <param name="dataFilePath">Path to the data file</param>
        /// <param name="testSize">Proportion of data to include in the test set</param>
        /// <param name="trainFilePath">Path to the training data file</param>
        /// <param name="testFilePath">Path to the testing data file</param>
        /// <param name="validationFilePath">Path to the validation data file</param>
        /// <param name="primaryFeatureKeyID">Primary feature key ID</param>
        /// <param name="labelColumn">Label column name</param>
        /// <returns>Array of strings containing file paths for training, testing, and validation data</returns>
        string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath, string validationFilePath, string primaryFeatureKeyID, string labelColumn);

        /// <summary>
        /// Split data class file into separate train and test files
        /// </summary>
        /// <param name="urlpath">URL path to the data file</param>
        /// <param name="filename">Name of the data file</param>
        /// <param name="splitRatio">Train-test split ratio</param>
        /// <returns>Tuple containing train and test file paths</returns>
        Tuple<string, string> SplitDataClassFile(string urlpath, string filename, double splitRatio);

        /// <summary>
        /// Export test results to a file
        /// </summary>
        /// <param name="filePath">Path to the export file</param>
        /// <param name="iDColumn">ID column name</param>
        /// <param name="labelColumn">Label column name</param>
        void ExportTestResult(string filePath, string iDColumn, string labelColumn);
        #endregion

        #region Data Preprocessing
        /// <summary>
        /// Handle categorical data encoding
        /// </summary>
        /// <param name="categoricalFeatures">Array of categorical feature names</param>
        void HandleCategoricalDataEncoder(string[] categoricalFeatures);

        /// <summary>
        /// Handle multi-value categorical features
        /// </summary>
        /// <param name="multiValueFeatures">Array of multi-value feature names</param>
        void HandleMultiValueCategoricalFeatures(string[] multiValueFeatures);

        /// <summary>
        /// Handle date data features
        /// </summary>
        /// <param name="dateFeatures">Array of date feature names</param>
        void HandleDateData(string[] dateFeatures);

        /// <summary>
        /// Missing value imputation methods
        /// </summary>
        /// <param name="strategy">Imputation strategy (e.g., "mean", "median", "most_frequent")</param>
        void ImputeMissingValues(string strategy = "mean");

        /// <summary>
        /// Missing value imputation using forward fill
        /// </summary>
        /// <param name="method">Fill method (e.g., "ffill", "bfill")</param>
        void ImputeMissingValuesWithFill(string method = "ffill");

        /// <summary>
        /// Missing value imputation with a custom value
        /// </summary>
        /// <param name="customValue">Custom value for imputation</param>
        void ImputeMissingValuesWithCustomValue(object customValue);

        /// <summary>
        /// Drop missing values from the dataset
        /// </summary>
        /// <param name="axis">Axis along which to drop missing values ("rows" or "columns")</param>
        void DropMissingValues(string axis = "rows");

        /// <summary>
        /// Missing value imputation for selected features
        /// </summary>
        /// <param name="featureList">Array of feature names to impute</param>
        /// <param name="strategy">Imputation strategy (e.g., "mean", "median", "most_frequent")</param>
        void ImputeMissingValues(string[] featureList = null, string strategy = "mean");

        /// <summary>
        /// Data scaling and normalization
        /// </summary>
        void StandardizeData();

        /// <summary>
        /// Min-Max scaling of data
        /// </summary>
        /// <param name="featureRangeMin">Minimum range value for scaling</param>
        /// <param name="featureRangeMax">Maximum range value for scaling</param>
        void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0);

        /// <summary>
        /// Robust scaling of data using median and IQR
        /// </summary>
        void RobustScaleData();

        /// <summary>
        /// Normalize data using L1 or L2 norm
        /// </summary>
        /// <param name="norm">Normalization type ("l1" or "l2")</param>
        void NormalizeData(string norm = "l2");

        /// <summary>
        /// Standardize selected features
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        void StandardizeData(string[] selectedFeatures = null);

        /// <summary>
        /// Min-Max scaling of selected features
        /// </summary>
        /// <param name="featureRangeMin">Minimum range value for scaling</param>
        /// <param name="featureRangeMax">Maximum range value for scaling</param>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0, string[] selectedFeatures = null);

        /// <summary>
        /// Robust scaling of selected features using median and IQR
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        void RobustScaleData(string[] selectedFeatures = null);

        /// <summary>
        /// Normalize selected features using L1 or L2 norm
        /// </summary>
        /// <param name="norm">Normalization type ("l1" or "l2")</param>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        void NormalizeData(string norm = "l2", string[] selectedFeatures = null);
        #endregion

        #region Feature Engineering
        /// <summary>
        /// Generate polynomial features
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        /// <param name="degree">Degree of the polynomial</param>
        /// <param name="includeBias">Include bias column (intercept)</param>
        /// <param name="interactionOnly">Only include interaction terms</param>
        void GeneratePolynomialFeatures(string[] selectedFeatures = null, int degree = 2, bool includeBias = true, bool interactionOnly = false);

        /// <summary>
        /// Apply logarithmic transformation to selected features
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        void ApplyLogTransformation(string[] selectedFeatures = null);

        /// <summary>
        /// Apply binning to categorical features
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        /// <param name="numberOfBins">Number of bins for quantization</param>
        /// <param name="encodeAsOrdinal">Encode bins as ordinal variables</param>
        void ApplyBinning(string[] selectedFeatures, int numberOfBins = 5, bool encodeAsOrdinal = true);

        /// <summary>
        /// Apply feature hashing to reduce dimensionality
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        /// <param name="nFeatures">Number of features after hashing</param>
        void ApplyFeatureHashing(string[] selectedFeatures, int nFeatures = 10);
        #endregion

        #region Imbalanced Data Handling
        /// <summary>
        /// Apply random undersampling to the majority class
        /// </summary>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="samplingStrategy">Sampling strategy (e.g., 0.5 for 50%)</param>
        void ApplyRandomUndersampling(string targetColumn, float samplingStrategy = 0.5f);

        /// <summary>
        /// Apply random oversampling to the minority class
        /// </summary>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="samplingStrategy">Sampling strategy (e.g., 1.0 for 100%)</param>
        void ApplyRandomOversampling(string targetColumn, float samplingStrategy = 1.0f);

        /// <summary>
        /// Apply SMOTE (Synthetic Minority Over-sampling Technique)
        /// </summary>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="samplingStrategy">Sampling strategy (e.g., 1.0 for 100%)</param>
        void ApplySMOTE(string targetColumn, float samplingStrategy = 1.0f);

        /// <summary>
        /// Apply NearMiss under-sampling
        /// </summary>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="version">NearMiss version (1, 2, or 3)</param>
        void ApplyNearMiss(string targetColumn, int version = 1);

        /// <summary>
        /// Apply Balanced Random Forest classifier
        /// </summary>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="nEstimators">Number of trees in the forest</param>
        void ApplyBalancedRandomForest(string targetColumn, int nEstimators = 100);

        /// <summary>
        /// Adjust class weights for imbalanced data
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="algorithmName">Algorithm name</param>
        /// <param name="parameters">Algorithm parameters</param>
        /// <param name="featureColumns">Feature column names</param>
        /// <param name="labelColumn">Label column name</param>
        void AdjustClassWeights(string modelId, string algorithmName, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn);

        /// <summary>
        /// Randomly over-sample the minority class
        /// </summary>
        /// <param name="labelColumn">Label column name</param>
        void RandomOverSample(string labelColumn);

        /// <summary>
        /// Randomly under-sample the majority class
        /// </summary>
        /// <param name="labelColumn">Label column name</param>
        void RandomUnderSample(string labelColumn);

        /// <summary>
        /// Apply SMOTE to the specified label column
        /// </summary>
        /// <param name="labelColumn">Label column name</param>
        void ApplySMOTE(string labelColumn);
        #endregion

        #region Text Processing
        /// <summary>
        /// Convert text to lowercase
        /// </summary>
        /// <param name="columnName">Column name containing text data</param>
        void ConvertTextToLowercase(string columnName);

        /// <summary>
        /// Remove punctuation from text
        /// </summary>
        /// <param name="columnName">Column name containing text data</param>
        void RemovePunctuation(string columnName);

        /// <summary>
        /// Remove stopwords from text
        /// </summary>
        /// <param name="columnName">Column name containing text data</param>
        /// <param name="language">Language for stopwords (e.g., "english")</param>
        void RemoveStopwords(string columnName, string language = "english");

        /// <summary>
        /// Apply stemming to text
        /// </summary>
        /// <param name="columnName">Column name containing text data</param>
        void ApplyStemming(string columnName);

        /// <summary>
        /// Apply lemmatization to text
        /// </summary>
        /// <param name="columnName">Column name containing text data</param>
        void ApplyLemmatization(string columnName);

        /// <summary>
        /// Apply tokenization to text
        /// </summary>
        /// <param name="columnName">Column name containing text data</param>
        void ApplyTokenization(string columnName);

        /// <summary>
        /// Apply TF-IDF vectorization to text
        /// </summary>
        /// <param name="columnName">Column name containing text data</param>
        /// <param name="maxFeatures">Maximum number of features for TF-IDF</param>
        void ApplyTFIDFVectorization(string columnName, int maxFeatures = 1000);
        #endregion

        #region Date/Time Processing
        /// <summary>
        /// Extract date and time components
        /// </summary>
        /// <param name="columnName">Column name containing date/time data</param>
        void ExtractDateTimeComponents(string columnName);

        /// <summary>
        /// Calculate time difference between two columns
        /// </summary>
        /// <param name="startColumn">Start column name</param>
        /// <param name="endColumn">End column name</param>
        /// <param name="newColumnName">New column name for time difference</param>
        void CalculateTimeDifference(string startColumn, string endColumn, string newColumnName);

        /// <summary>
        /// Handle cyclical time features (e.g., hour, day of week)
        /// </summary>
        /// <param name="columnName">Column name containing cyclical time features</param>
        /// <param name="featureType">Type of cyclical feature (e.g., "hour", "day of week")</param>
        void HandleCyclicalTimeFeatures(string columnName, string featureType);

        /// <summary>
        /// Parse date column to DateTime format
        /// </summary>
        /// <param name="columnName">Column name containing date data</param>
        void ParseDateColumn(string columnName);

        /// <summary>
        /// Handle missing dates in the dataset
        /// </summary>
        /// <param name="columnName">Column name containing date data</param>
        /// <param name="method">Method for handling missing dates (e.g., "fill", "drop")</param>
        /// <param name="fillValue">Fill value for missing dates</param>
        void HandleMissingDates(string columnName, string method = "fill", string fillValue = null);
        #endregion

        #region Categorical Encoding
        /// <summary>
        /// One-hot encode categorical features
        /// </summary>
        /// <param name="categoricalFeatures">Array of categorical feature names</param>
        void OneHotEncode(string[] categoricalFeatures);

        /// <summary>
        /// Label encode categorical features
        /// </summary>
        /// <param name="categoricalFeatures">Array of categorical feature names</param>
        void LabelEncode(string[] categoricalFeatures);

        /// <summary>
        /// Target encode categorical features based on the target variable
        /// </summary>
        /// <param name="categoricalFeatures">Array of categorical feature names</param>
        /// <param name="labelColumn">Label column name</param>
        void TargetEncode(string[] categoricalFeatures, string labelColumn);

        /// <summary>
        /// Binary encode categorical features
        /// </summary>
        /// <param name="categoricalFeatures">Array of categorical feature names</param>
        void BinaryEncode(string[] categoricalFeatures);

        /// <summary>
        /// Frequency encode categorical features
        /// </summary>
        /// <param name="categoricalFeatures">Array of categorical feature names</param>
        void FrequencyEncode(string[] categoricalFeatures);

        /// <summary>
        /// Get categorical features from the dataset
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        /// <returns>Array of strings containing categorical feature names</returns>
        string[] GetCategoricalFeatures(string[] selectedFeatures);

        /// <summary>
        /// Get categorical and date features from the dataset
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        /// <returns>Tuple containing arrays of categorical and date feature names</returns>
        Tuple<string[], string[]> GetCategoricalAndDateFeatures(string[] selectedFeatures);
        #endregion

        #region Time Series
        /// <summary>
        /// Augment time series data
        /// </summary>
        /// <param name="timeSeriesColumns">Array of time series column names</param>
        /// <param name="augmentationType">Type of augmentation (e.g., "noise", "scaling")</param>
        /// <param name="parameter">Augmentation parameter (e.g., noise level)</param>
        void TimeSeriesAugmentation(string[] timeSeriesColumns, string augmentationType, double parameter);
        #endregion

        #region Feature Selection
        /// <summary>
        /// Apply variance threshold feature selection
        /// </summary>
        /// <param name="threshold">Variance threshold</param>
        void ApplyVarianceThreshold(double threshold = 0.0);

        /// <summary>
        /// Apply correlation threshold feature selection
        /// </summary>
        /// <param name="threshold">Correlation threshold</param>
        void ApplyCorrelationThreshold(double threshold = 0.9);

        /// <summary>
        /// Apply recursive feature elimination (RFE) for feature selection
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="n_features_to_select">Number of features to select</param>
        void ApplyRFE(string modelId, int n_features_to_select = 5);

        /// <summary>
        /// Apply L1 regularization for feature selection
        /// </summary>
        /// <param name="alpha">Regularization strength</param>
        void ApplyL1Regularization(double alpha = 0.01);

        /// <summary>
        /// Apply tree-based feature selection using ensemble methods
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        void ApplyTreeBasedFeatureSelection(string modelId);

        /// <summary>
        /// Apply variance threshold feature selection to a subset of features
        /// </summary>
        /// <param name="threshold">Variance threshold</param>
        /// <param name="featureList">Array of feature names</param>
        void ApplyVarianceThreshold(double threshold = 0.0, string[] featureList = null);
        #endregion

        #region Cross-Validation and Sampling
        /// <summary>
        /// Perform cross-validation for model evaluation
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="numFolds">Number of folds for cross-validation</param>
        void PerformCrossValidation(string modelId, int numFolds = 5);

        /// <summary>
        /// Perform stratified sampling to create balanced train-test splits
        /// </summary>
        /// <param name="testSize">Proportion of data to include in the test set</param>
        /// <param name="trainFilePath">Path to the training data file</param>
        /// <param name="testFilePath">Path to the testing data file</param>
        void PerformStratifiedSampling(float testSize, string trainFilePath, string testFilePath);
        #endregion

        #region Data Cleaning
        /// <summary>
        /// Remove outliers from the dataset
        /// </summary>
        /// <param name="featureList">Array of feature names</param>
        /// <param name="zThreshold">Z-score threshold for outlier detection</param>
        void RemoveOutliers(string[] featureList = null, double zThreshold = 3.0);

        /// <summary>
        /// Drop duplicate rows from the dataset
        /// </summary>
        /// <param name="featureList">Array of feature names</param>
        void DropDuplicates(string[] featureList = null);

        /// <summary>
        /// Standardize categorical values across the dataset
        /// </summary>
        /// <param name="featureList">Array of feature names</param>
        /// <param name="replacements">Dictionary of replacements for standardization</param>
        void StandardizeCategories(string[] featureList = null, Dictionary<string, string> replacements = null);
        #endregion

        #region Dimensionality Reduction
        /// <summary>
        /// Apply Principal Component Analysis (PCA) for dimensionality reduction
        /// </summary>
        /// <param name="nComponents">Number of principal components to retain</param>
        /// <param name="featureList">Array of feature names</param>
        void ApplyPCA(int nComponents = 2, string[] featureList = null);

        /// <summary>
        /// Apply Linear Discriminant Analysis (LDA) for dimensionality reduction
        /// </summary>
        /// <param name="labelColumn">Label column name</param>
        /// <param name="nComponents">Number of components to retain</param>
        /// <param name="featureList">Array of feature names</param>
        void ApplyLDA(string labelColumn, int nComponents = 2, string[] featureList = null);
        #endregion

        #region Utility Methods
        /// <summary>
        /// Add a label column to the test data if missing
        /// </summary>
        /// <param name="testDataFilePath">Path to the test data file</param>
        /// <param name="labelColumn">Label column name</param>
        void AddLabelColumnIfMissing(string testDataFilePath, string labelColumn);

        /// <summary>
        /// Add a label column to the data if missing
        /// </summary>
        /// <param name="labelColumn">Label column name</param>
        void AddLabelColumnIfMissing(string labelColumn);
        #endregion

        #region Visualization and Evaluation
        /// <summary>
        /// Create a Receiver Operating Characteristic (ROC) curve
        /// </summary>
        /// <returns>True if ROC curve creation is successful</returns>
        bool CreateROC();

        /// <summary>
        /// Create a confusion matrix
        /// </summary>
        /// <returns>True if confusion matrix creation is successful</returns>
        bool CreateConfusionMatrix();

        /// <summary>
        /// Create a learning curve for model evaluation
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="imagePath">Path to save the learning curve image</param>
        /// <returns>True if learning curve creation is successful</returns>
        bool CreateLearningCurve(string modelId, string imagePath);

        /// <summary>
        /// Create a precision-recall curve
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="imagePath">Path to save the precision-recall curve image</param>
        /// <returns>True if precision-recall curve creation is successful</returns>
        bool CreatePrecisionRecallCurve(string modelId, string imagePath);

        /// <summary>
        /// Create a feature importance plot
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="imagePath">Path to save the feature importance image</param>
        /// <returns>True if feature importance creation is successful</returns>
        bool CreateFeatureImportance(string modelId, string imagePath);

        /// <summary>
        /// Create a confusion matrix plot
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="imagePath">Path to save the confusion matrix image</param>
        /// <returns>True if confusion matrix creation is successful</returns>
        bool CreateConfusionMatrix(string modelId, string imagePath);

        /// <summary>
        /// Create a ROC curve plot
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="imagePath">Path to save the ROC curve image</param>
        /// <returns>True if ROC curve creation is successful</returns>
        bool CreateROC(string modelId, string imagePath);

        /// <summary>
        /// Generate an evaluation report for the model
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="outputHtmlPath">Path to save the evaluation report HTML file</param>
        /// <returns>True if report generation is successful</returns>
        bool GenerateEvaluationReport(string modelId, string outputHtmlPath);
        #endregion

        #region Async Operations (Future Enhancement)
        /// <summary>
        /// Load data asynchronously with session support
        /// </summary>
        /// <param name="filePath">Path to the data file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of feature names</returns>
        Task<string[]> LoadDataAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Train model asynchronously with session support
        /// </summary>
        /// <param name="modelId">Unique identifier for the model</param>
        /// <param name="algorithm">Machine learning algorithm to use</param>
        /// <param name="parameters">Algorithm parameters</param>
        /// <param name="featureColumns">Feature column names</param>
        /// <param name="labelColumn">Label column name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if training successful</returns>
        Task<bool> TrainModelAsync(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get model evaluation scores asynchronously
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple containing accuracy and F1 score for classification</returns>
        Task<Tuple<double, double>> GetModelClassificationScoreAsync(string modelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get regression model scores asynchronously
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tuple containing MSE, RMSE, and MAE</returns>
        Task<Tuple<double, double, double>> GetModelRegressionScoresAsync(string modelId, CancellationToken cancellationToken = default);
        #endregion
    }
}