using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Container.Services;
using Beep.Python.ML.Utils;
using Beep.Python.ML.Assistants;

namespace Beep.Python.ML
{
    /// <summary>
    /// Core Python ML Manager with clean architecture using assistant classes for specialized operations.
    /// This class focuses on core ML operations while delegating specialized tasks to assistant classes.
    /// </summary>
    public class PythonMLManager : PythonBaseViewModel, IPythonMLManager, IDisposable
    {
        private Dictionary<string, bool> algorithmSupervision = new Dictionary<string, bool>();

        #region Assistant Classes (Lazy Initialization)
        private PythonDataPreprocessingAssistant _dataPreprocessingAssistant;
        private PythonFeatureEngineeringAssistant _featureEngineeringAssistant;
        private PythonCategoricalEncodingAssistant _categoricalEncodingAssistant;
        private PythonTextProcessingAssistant _textProcessingAssistant;
        private PythonDateTimeProcessingAssistant _dateTimeProcessingAssistant;
        private PythonImbalancedDataAssistant _imbalancedDataAssistant;
        private PythonTimeSeriesAssistant _timeSeriesAssistant;
        private PythonFeatureSelectionAssistant _featureSelectionAssistant;
        private PythonCrossValidationAssistant _crossValidationAssistant;
        private PythonDataCleaningAssistant _dataCleaningAssistant;
        private PythonDimensionalityReductionAssistant _dimensionalityReductionAssistant;
        private PythonVisualizationAssistant _visualizationAssistant;
        private PythonUtilityAssistant _utilityAssistant;

        /// <summary>
        /// Get the data preprocessing assistant for data cleaning and preparation operations
        /// </summary>
        public PythonDataPreprocessingAssistant DataPreprocessing =>
            _dataPreprocessingAssistant ??= new PythonDataPreprocessingAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the feature engineering assistant for advanced feature creation and transformation
        /// </summary>
        public PythonFeatureEngineeringAssistant FeatureEngineering =>
            _featureEngineeringAssistant ??= new PythonFeatureEngineeringAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the categorical encoding assistant for handling categorical variables
        /// </summary>
        public PythonCategoricalEncodingAssistant CategoricalEncoding =>
            _categoricalEncodingAssistant ??= new PythonCategoricalEncodingAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the text processing assistant for natural language processing operations
        /// </summary>
        public PythonTextProcessingAssistant TextProcessing =>
            _textProcessingAssistant ??= new PythonTextProcessingAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the date/time processing assistant for temporal data operations
        /// </summary>
        public PythonDateTimeProcessingAssistant DateTimeProcessing =>
            _dateTimeProcessingAssistant ??= new PythonDateTimeProcessingAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the imbalanced data assistant for handling class imbalance
        /// </summary>
        public PythonImbalancedDataAssistant ImbalancedData =>
            _imbalancedDataAssistant ??= new PythonImbalancedDataAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the time series assistant for time series analysis
        /// </summary>
        public PythonTimeSeriesAssistant TimeSeries =>
            _timeSeriesAssistant ??= new PythonTimeSeriesAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the feature selection assistant for feature importance and selection
        /// </summary>
        public PythonFeatureSelectionAssistant FeatureSelection =>
            _featureSelectionAssistant ??= new PythonFeatureSelectionAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the cross-validation assistant for model validation
        /// </summary>
        public PythonCrossValidationAssistant CrossValidation =>
            _crossValidationAssistant ??= new PythonCrossValidationAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the data cleaning assistant for outlier detection and data quality
        /// </summary>
        public PythonDataCleaningAssistant DataCleaning =>
            _dataCleaningAssistant ??= new PythonDataCleaningAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the dimensionality reduction assistant for PCA, LDA, etc.
        /// </summary>
        public PythonDimensionalityReductionAssistant DimensionalityReduction =>
            _dimensionalityReductionAssistant ??= new PythonDimensionalityReductionAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the visualization assistant for creating charts and plots
        /// </summary>
        public PythonVisualizationAssistant Visualization =>
            _visualizationAssistant ??= new PythonVisualizationAssistant(PythonRuntime, SessionInfo);

        /// <summary>
        /// Get the utility assistant for data splitting, export, and other utilities
        /// </summary>
        public PythonUtilityAssistant Utilities =>
            _utilityAssistant ??= new PythonUtilityAssistant(PythonRuntime, SessionInfo);
        #endregion

        #region State Properties
        public bool IsDataLoaded { get; set; } = false;
        public bool IsModelTrained { get; set; } = false;
        public bool IsModelSaved { get; set; } = false;
        public bool IsModelLoaded { get; set; } = false;
        public bool IsModelPredicted { get; set; } = false;
        public bool IsModelScored { get; set; } = false;
        public bool IsModelExported { get; set; } = false;
        public bool IsDataSplit { get; set; } = false;
        public string DataFilePath { get; set; } = string.Empty;
        public string ModelFilePath { get; set; } = string.Empty;
        public string PredictionsFilePath { get; set; } = string.Empty;
        public string TrainingFilePath { get; set; } = string.Empty;
        public string TestingFilePath { get; set; } = string.Empty;
        public string ValidationFilePath { get; set; } = string.Empty;
        #endregion

        #region Constructor
        public PythonMLManager(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager, PythonSessionInfo sessionInfo) 
            : base(beepservice, pythonRuntimeManager, sessionInfo)
        {
            InitializeAlgorithmSupervision();
        }
        #endregion

        #region Session Management
        public bool ConfigureMLSession(PythonSessionInfo session, PythonVirtualEnvironment virtualEnvironment)
        {
            return ConfigureSession(session, virtualEnvironment);
        }

        public bool ConfigureMLSessionForUser(string username, string? environmentId = null)
        {
            return ConfigureSessionForUser(username, environmentId);
        }

        public bool IsSessionConfigured()
        {
            return base.IsSessionConfigured;
        }

        public PythonSessionInfo? GetConfiguredSession()
        {
            return SessionInfo;
        }

        public PythonVirtualEnvironment? GetConfiguredVirtualEnvironment()
        {
            return ConfiguredVirtualEnvironment;
        }
        #endregion

        #region Python Module Management
        public void ImportPythonModule(string moduleName)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Python environment is not initialized");

            string script = $"import {moduleName}";
            PythonRuntime.ExecuteManager.RunPythonScript(script, null, SessionInfo);
        }
        #endregion

        #region Core Data Loading and Validation
        public string[] ValidateAndPreviewData(string filePath, int numRows = 5)
        {
            if (!IsInitialized)
            {
                return null;
            }

            var parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath.Replace("\\", "\\\\"),
                ["num_rows"] = numRows
            };

            string script = PythonScriptTemplateManager.GetScript("validate_and_preview_data", parameters);
            ExecuteInSession(script);
            return GetStringArrayFromSession("preview_columns");
        }

        public string[] LoadData(string filePath)
        {
            if (!IsInitialized)
            {
                return null;
            }

            var parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath.Replace("\\", "\\\\"),
                ["selected_features"] = Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("load_data_with_features", parameters);

            if (ExecuteInSession(script))
            {
                IsDataLoaded = true;
                DataFilePath = filePath.Replace("\\", "\\\\");
            }
            else
            {
                IsDataLoaded = false;
            }

            return GetStringArrayFromSession("features");
        }

        public string[] LoadData(string filePath, string[] selectedFeatures)
        {
            if (!IsInitialized)
            {
                return null;
            }

            var parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath.Replace("\\", "\\\\"),
                ["selected_features"] = selectedFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("load_data_with_features", parameters);

            if (ExecuteInSession(script))
            {
                IsDataLoaded = true;
                DataFilePath = filePath.Replace("\\", "\\\\");
            }
            else
            {
                IsDataLoaded = false;
            }

            return GetStringArrayFromSession("features");
        }

        public void FilterDataToSelectedFeatures(string[] selectedFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("filter_selected_features", parameters);
            ExecuteInSession(script);
        }

        public string[] GetFeatures(string filePath) => ValidateAndPreviewData(filePath, 1);
        public string[] LoadTestData(string filePath) => LoadData(filePath);
        public string[] LoadPredictionData(string filePath) => LoadData(filePath);
        #endregion

        #region Core Model Operations
        public void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            var scriptParameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["algorithm_module"] = GetAlgorithmModule(algorithm),
                ["algorithm_name"] = GetScikitLearnAlgorithmName(algorithm),
                ["parameters"] = parameters ?? new Dictionary<string, object>(),
                ["feature_columns"] = featureColumns,
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("train_model", scriptParameters);
            
            if (ExecuteInSession(script))
            {
                IsModelTrained = true;
            }
        }

        public string LoadModel(string filePath)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string modelId = Guid.NewGuid().ToString();
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["file_path"] = filePath.Replace("\\", "\\\\")
            };

            string script = PythonScriptTemplateManager.GetScript("load_model", parameters);
            
            if (ExecuteInSession(script))
            {
                IsModelLoaded = true;
                ModelFilePath = filePath;
                return modelId;
            }
            
            return null;
        }

        public void SaveModel(string modelId, string filePath)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["file_path"] = filePath.Replace("\\", "\\\\")
            };

            string script = PythonScriptTemplateManager.GetScript("save_model", parameters);
            
            if (ExecuteInSession(script))
            {
                IsModelSaved = true;
                ModelFilePath = filePath;
            }
        }

        public Tuple<double, double> GetModelClassificationScore(string modelId)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["is_classification"] = true
            };

            string script = PythonScriptTemplateManager.GetScript("evaluate_model", parameters);
            ExecuteInSession(script);

            var accuracy = GetFromSessionScope<double>("accuracy", 0.0);
            var f1Score = GetFromSessionScope<double>("f1_score", 0.0);
            
            return new Tuple<double, double>(accuracy, f1Score);
        }

        public Tuple<double, double, double> GetModelRegressionScores(string modelId)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["is_classification"] = false
            };

            string script = PythonScriptTemplateManager.GetScript("evaluate_model", parameters);
            ExecuteInSession(script);

            var mse = GetFromSessionScope<double>("mse", 0.0);
            var rmse = GetFromSessionScope<double>("rmse", 0.0);
            var mae = GetFromSessionScope<double>("mae", 0.0);
            
            return new Tuple<double, double, double>(mse, rmse, mae);
        }

        public dynamic PredictClassification(string[] training_columns)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            var parameters = new Dictionary<string, object>
            {
                ["training_columns"] = training_columns,
                ["prediction_type"] = "classification"
            };

            string script = PythonScriptTemplateManager.GetScript("predict_model", parameters);
            ExecuteInSession(script);

            IsModelPredicted = true;
            return GetFromSessionScope<object>("predictions", Array.Empty<int>());
        }

        public dynamic PredictRegression(string[] training_columns)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            var parameters = new Dictionary<string, object>
            {
                ["training_columns"] = training_columns,
                ["prediction_type"] = "regression"
            };

            string script = PythonScriptTemplateManager.GetScript("predict_model", parameters);
            ExecuteInSession(script);

            IsModelPredicted = true;
            return GetFromSessionScope<object>("predictions", Array.Empty<double>());
        }
        #endregion

        #region Async Operations
        public async Task<string[]> LoadDataAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(LoadData(filePath));
        }

        public async Task<bool> TrainModelAsync(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    TrainModel(modelId, algorithm, parameters, featureColumns, labelColumn);
                    return true;
                }
                catch
                {
                    return false;
                }
            }, cancellationToken);
        }

        public async Task<Tuple<double, double>> GetModelClassificationScoreAsync(string modelId, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(GetModelClassificationScore(modelId));
        }

        public async Task<Tuple<double, double, double>> GetModelRegressionScoresAsync(string modelId, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(GetModelRegressionScores(modelId));
        }
        #endregion

        #region Private Helper Methods
        private void InitializeAlgorithmSupervision()
        {
            algorithmSupervision["HistGradientBoostingRegressor"] = true;
            algorithmSupervision["HistGradientBoostingClassifier"] = true;
            algorithmSupervision["LogisticRegression"] = true;
            algorithmSupervision["RandomForestClassifier"] = true;
            algorithmSupervision["RandomForestRegressor"] = true;
            algorithmSupervision["SVC"] = true;
            algorithmSupervision["SVR"] = true;
            algorithmSupervision["KNeighborsClassifier"] = true;
            algorithmSupervision["KNeighborsRegressor"] = true;
            algorithmSupervision["GradientBoostingClassifier"] = true;
            algorithmSupervision["GradientBoostingRegressor"] = true;
            algorithmSupervision["DecisionTreeClassifier"] = true;
            algorithmSupervision["DecisionTreeRegressor"] = true;
            algorithmSupervision["LinearRegression"] = true;
            algorithmSupervision["LassoRegression"] = true;
            algorithmSupervision["RidgeRegression"] = true;
            algorithmSupervision["ElasticNet"] = true;
            algorithmSupervision["KMeans"] = false;
            algorithmSupervision["DBSCAN"] = false;
            algorithmSupervision["AgglomerativeClustering"] = false;
            algorithmSupervision["GaussianNB"] = true;
            algorithmSupervision["MultinomialNB"] = true;
            algorithmSupervision["BernoulliNB"] = true;
            algorithmSupervision["AdaBoostClassifier"] = true;
        }

        private bool ExecuteInSession(string script)
        {
            if (!IsInitialized || SessionInfo == null)
                return false;

            return PythonRuntime.ExecuteManager.RunPythonScript(script, null, SessionInfo);
        }

        private string[] GetStringArrayFromSession(string variableName)
        {
            if (!IsInitialized || SessionInfo == null)
                return Array.Empty<string>();

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["variable_name"] = variableName
                };

                string script = PythonScriptTemplateManager.GetScript("get_string_array", parameters);
                ExecuteInSession(script);
                
                var jsonResult = PythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(null, "result_json", SessionInfo);
                
                if (!string.IsNullOrEmpty(jsonResult?.ToString()))
                {
                    var cleanJson = jsonResult.ToString().Trim('"').Replace("\\\"", "\"");
                    var result = System.Text.Json.JsonSerializer.Deserialize<string[]>(cleanJson);
                    return result ?? Array.Empty<string>();
                }
            }
            catch (Exception)
            {
                // Return empty array on any error
            }

            return Array.Empty<string>();
        }

        private T GetFromSessionScope<T>(string variableName, T defaultValue = default(T))
        {
            if (!IsInitialized || SessionInfo == null)
                return defaultValue;

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["variable_name"] = variableName
                };

                string script = PythonScriptTemplateManager.GetScript("get_from_session_scope", parameters);
                ExecuteInSession(script);
                
                var jsonResult = PythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(null, "result_json", SessionInfo);
                
                if (!string.IsNullOrEmpty(jsonResult?.ToString()))
                {
                    var cleanJson = jsonResult.ToString().Trim('"').Replace("\\\"", "\"");
                    if (cleanJson != "null")
                    {
                        var result = System.Text.Json.JsonSerializer.Deserialize<T>(cleanJson);
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                // Return default value on any error
            }

            return defaultValue;
        }

        private string GetAlgorithmModule(MachineLearningAlgorithm algorithm)
        {
            return algorithm switch
            {
                MachineLearningAlgorithm.RandomForestClassifier or MachineLearningAlgorithm.RandomForestRegressor => "ensemble",
                MachineLearningAlgorithm.GradientBoostingClassifier or MachineLearningAlgorithm.GradientBoostingRegressor => "ensemble",
                MachineLearningAlgorithm.LogisticRegression or MachineLearningAlgorithm.LinearRegression => "linear_model",
                MachineLearningAlgorithm.SVC or MachineLearningAlgorithm.SVR => "svm",
                MachineLearningAlgorithm.DecisionTreeClassifier or MachineLearningAlgorithm.DecisionTreeRegressor => "tree",
                MachineLearningAlgorithm.KNeighborsClassifier => "neighbors",
                _ => "ensemble"
            };
        }

        private string GetScikitLearnAlgorithmName(MachineLearningAlgorithm algorithm)
        {
            return algorithm switch
            {
                MachineLearningAlgorithm.RandomForestClassifier => "RandomForestClassifier",
                MachineLearningAlgorithm.RandomForestRegressor => "RandomForestRegressor",
                MachineLearningAlgorithm.LogisticRegression => "LogisticRegression",
                MachineLearningAlgorithm.LinearRegression => "LinearRegression",
                MachineLearningAlgorithm.SVC => "SVC",
                MachineLearningAlgorithm.SVR => "SVR",
                MachineLearningAlgorithm.DecisionTreeClassifier => "DecisionTreeClassifier",
                MachineLearningAlgorithm.DecisionTreeRegressor => "DecisionTreeRegressor",
                MachineLearningAlgorithm.KNeighborsClassifier => "KNeighborsClassifier",
                MachineLearningAlgorithm.GradientBoostingClassifier => "GradientBoostingClassifier",
                MachineLearningAlgorithm.GradientBoostingRegressor => "GradientBoostingRegressor",
                _ => "RandomForestClassifier"
            };
        }
        #endregion
    }
}
