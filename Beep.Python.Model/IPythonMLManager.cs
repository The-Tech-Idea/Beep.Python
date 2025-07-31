using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    /// <summary>
    /// Core interface for Python machine learning operations with session management and virtual environment support.
    /// This interface provides the essential ML operations while specialized tasks are handled by assistant classes.
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

        #region Core Data Loading and Validation
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
        /// <returns>Array of feature names</returns>
        string[] LoadData(string filePath);

        /// <summary>
        /// Load data with selected features from a file
        /// </summary>
        /// <param name="filePath">Path to the data file</param>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        /// <returns>Array of feature names</returns>
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
        /// <returns>Array of feature names</returns>
        string[] LoadTestData(string filePath);

        /// <summary>
        /// Load prediction data from a file
        /// </summary>
        /// <param name="filePath">Path to the prediction data file</param>
        /// <returns>Array of feature names</returns>
        string[] LoadPredictionData(string filePath);
        #endregion

        #region Core Model Operations
        /// <summary>
        /// Train a machine learning model using Python scripts
        /// </summary>
        /// <param name="modelId">Unique identifier for the model</param>
        /// <param name="algorithm">Machine learning algorithm to use</param>
        /// <param name="parameters">Algorithm parameters</param>
        /// <param name="featureColumns">Feature column names</param>
        /// <param name="labelColumn">Label column name</param>
        void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn);

        /// <summary>
        /// Load a machine learning model from a file using Python scripts
        /// </summary>
        /// <param name="filePath">Path to the model file</param>
        /// <returns>Model identifier</returns>
        string LoadModel(string filePath);

        /// <summary>
        /// Save a machine learning model to a file using Python scripts
        /// </summary>
        /// <param name="modelId">Unique identifier for the model</param>
        /// <param name="filePath">Path to the model file</param>
        void SaveModel(string modelId, string filePath);

        /// <summary>
        /// Get model classification scores using Python scripts
        /// </summary>
        /// <param name="modelId">Model identifier</param>
        /// <returns>Tuple containing accuracy and F1 score</returns>
        Tuple<double, double> GetModelClassificationScore(string modelId);

        /// <summary>
        /// Get regression model scores using Python scripts
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

        #region Async Operations
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