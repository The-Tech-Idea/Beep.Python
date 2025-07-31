using Beep.Python.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML
{
    /// <summary>
    /// Enhanced training view model with session management, virtual environment support, 
    /// and advanced ML capabilities including hyperparameter optimization and comprehensive evaluation
    /// </summary>
    public partial class PythonTrainingViewModel : PythonBaseViewModel
    {
        #region Observable Properties
        [ObservableProperty]
        float testsize;
        [ObservableProperty]
        string testfilePath;
        [ObservableProperty]
        string trainfilePath;
        [ObservableProperty]
        string modelId;
        [ObservableProperty]
        string filename;
        [ObservableProperty]
        string entityname;
        [ObservableProperty]
        string datasourcename;
        [ObservableProperty]
        bool isFile;
        [ObservableProperty]
        MachineLearningAlgorithm selectAlgorithm;
        [ObservableProperty]
        List<ParameterDictionaryForAlgorithm> parameterDictionaryForAlgorithms;
        [ObservableProperty]
        bool isDataReady;
        [ObservableProperty]
        bool isTrainingReady;
        [ObservableProperty]
        bool isTrainDataLoaded;
        [ObservableProperty]
        bool isModelTrained;
        [ObservableProperty]
        bool isModelEvaluated;
        [ObservableProperty]
        bool isModelPredicted;
        [ObservableProperty]
        double mseScore;
        [ObservableProperty]
        double rmseScore;
        [ObservableProperty]
        double maeScore;
        [ObservableProperty]
        double f1Accuracy;
        [ObservableProperty]
        double evalScore;
        [ObservableProperty]
        bool isInit;
        [ObservableProperty]
        Dictionary<string, object> parameters;
        [ObservableProperty]
        string[] features;
        [ObservableProperty]
        string[] labels;
        [ObservableProperty]
        string labelColumn;
        [ObservableProperty]
        string[] selectedFeatures;

        // Enhanced properties for advanced training
        [ObservableProperty]
        bool isOptimizingHyperparameters;
        [ObservableProperty]
        double optimizationProgress;
        [ObservableProperty]
        Dictionary<string, object> bestParameters;
        [ObservableProperty]
        double bestCrossValidationScore;
        [ObservableProperty]
        CrossValidationResult lastCrossValidationResult;
        [ObservableProperty]
        HyperparameterOptimizationResult lastOptimizationResult;
        [ObservableProperty]
        ModelComparisonResult lastModelComparisonResult;
        [ObservableProperty]
        bool enableAdvancedFeatures;
        #endregion

        #region Properties
        public IPythonMLManager PythonMLManager { get; private set; }
        #endregion

        #region Constructor
        public PythonTrainingViewModel(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager, PythonSessionInfo sessionInfo) 
            : base(beepservice, pythonRuntimeManager, sessionInfo)
        {
            // Initialize default values
            ResetTraining();
            EnableAdvancedFeatures = true;
        }
        #endregion

        #region Session Management Methods
        /// <summary>
        /// Configure training session with ML manager
        /// </summary>
        /// <param name="session">Python session</param>
        /// <param name="virtualEnvironment">Virtual environment</param>
        /// <param name="mlManager">ML manager instance</param>
        /// <returns>True if configuration successful</returns>
        public bool ConfigureTrainingSession(PythonSessionInfo session, PythonVirtualEnvironment virtualEnvironment, IPythonMLManager mlManager)
        {
            if (ConfigureSession(session, virtualEnvironment))
            {
                PythonMLManager = mlManager;
                IsInit = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Configure session for user with training-specific initialization
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="environmentId">Environment ID</param>
        /// <param name="mlManager">ML manager instance</param>
        /// <returns>True if configuration successful</returns>
        public bool ConfigureTrainingSessionForUser(string username, string? environmentId, IPythonMLManager mlManager)
        {
            if (ConfigureSessionForUser(username, environmentId))
            {
                PythonMLManager = mlManager;
                IsInit = true;
                return InitializeTrainingEnvironment();
            }
            return false;
        }
        #endregion

        #region Initialization Methods
        /// <summary>
        /// Initialize with ML manager (legacy support)
        /// </summary>
        /// <param name="mLManager">ML manager instance</param>
        public void init(IPythonMLManager mLManager)
        {
            PythonMLManager = mLManager;
            IsInit = true;
            ResetTraining();
        }

        /// <summary>
        /// Initialize training-specific environment with advanced ML libraries
        /// </summary>
        /// <returns>True if initialization successful</returns>
        private bool InitializeTrainingEnvironment()
        {
            if (!IsSessionConfigured)
                return false;

            try
            {
                // Import training-specific libraries including advanced ones
                string script = @"
import numpy as np
import pandas as pd
from sklearn.model_selection import (
    train_test_split, GridSearchCV, RandomizedSearchCV, 
    cross_val_score, StratifiedKFold, learning_curve
)
from sklearn.metrics import (
    accuracy_score, mean_squared_error, mean_absolute_error, f1_score,
    classification_report, confusion_matrix, roc_curve, roc_auc_score,
    precision_recall_curve, average_precision_score
)
from sklearn.preprocessing import StandardScaler, RobustScaler, MinMaxScaler
from sklearn.feature_selection import SelectKBest, f_classif, f_regression
import pickle
import joblib
import warnings
warnings.filterwarnings('ignore')

print('Advanced ML training environment initialized successfully')
";
                ExecuteInSession(script);
                
                Editor?.AddLogMessage("PythonTrainingViewModel", "Advanced training environment initialized successfully", DateTime.Now, -1, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Failed to initialize training environment: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Reset training status and clear data
        /// </summary>
        public void ResetTraining()
        {
            ModelId = string.Empty;
            IsTrainingReady = false;
            IsBusy = false;
            IsDataReady = false;
            IsTrainDataLoaded = false;
            IsModelEvaluated = false;
            IsModelPredicted = false;
            IsModelTrained = false;
            MseScore = 0;
            RmseScore = 0;
            MaeScore = 0;
            F1Accuracy = 0;
            EvalScore = 0;
            Parameters = new Dictionary<string, object>();
            
            // Reset advanced properties
            IsOptimizingHyperparameters = false;
            OptimizationProgress = 0;
            BestParameters = new Dictionary<string, object>();
            BestCrossValidationScore = 0;
            LastCrossValidationResult = null;
            LastOptimizationResult = null;
            LastModelComparisonResult = null;
        }
        #endregion

        #region Data Loading Methods
        /// <summary>
        /// Load training data from file with session support
        /// </summary>
        /// <param name="filePath">Path to training data file</param>
        /// <returns>True if data loaded successfully</returns>
        public async Task<bool> LoadTrainingDataAsync(string filePath)
        {
            if (!IsSessionConfigured || PythonMLManager == null)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", "Session or ML manager not configured", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            try
            {
                IsBusy = true;
                
                // Use ML manager to load data
                Features = await PythonMLManager.LoadDataAsync(filePath, Token);
                
                if (Features != null && Features.Length > 0)
                {
                    IsDataReady = true;
                    IsTrainDataLoaded = true;
                    TrainfilePath = filePath;
                    Filename = Path.GetFileName(filePath);
                    
                    Editor?.AddLogMessage("PythonTrainingViewModel", $"Training data loaded successfully: {Features.Length} features", DateTime.Now, -1, null, Errors.Ok);
                    return true;
                }
                else
                {
                    Editor?.AddLogMessage("PythonTrainingViewModel", "No features found in training data", DateTime.Now, -1, null, Errors.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Failed to load training data: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Set selected features for training
        /// </summary>
        /// <param name="selectedFeatures">Array of selected feature names</param>
        public void SetSelectedFeatures(string[] selectedFeatures)
        {
            SelectedFeatures = selectedFeatures;
            
            if (IsSessionConfigured && PythonMLManager != null && selectedFeatures?.Length > 0)
            {
                try
                {
                    PythonMLManager.FilterDataToSelectedFeatures(selectedFeatures);
                    Editor?.AddLogMessage("PythonTrainingViewModel", $"Features filtered to {selectedFeatures.Length} selected features", DateTime.Now, -1, null, Errors.Ok);
                }
                catch (Exception ex)
                {
                    Editor?.AddLogMessage("PythonTrainingViewModel", $"Failed to filter features: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                }
            }
        }
        #endregion

        #region Advanced Training Methods

        /// <summary>
        /// Execute advanced training with hyperparameter optimization and comprehensive evaluation
        /// </summary>
        /// <param name="configuration">Advanced training configuration</param>
        /// <param name="progressReporter">Progress reporter for UI updates</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Advanced training result</returns>
        public async Task<AdvancedTrainingResult> ExecuteAdvancedTrainingAsync(
            AdvancedTrainingConfiguration configuration = null,
            IProgress<TrainingProgressInfo> progressReporter = null,
            CancellationToken cancellationToken = default)
        {
            configuration ??= new AdvancedTrainingConfiguration();
            
            var startTime = DateTime.Now;
            var result = new AdvancedTrainingResult
            {
                Success = false,
                StartTime = startTime,
                Configuration = configuration
            };

            try
            {
                // Stage 1: Validation (10%)
                progressReporter?.Report(new TrainingProgressInfo 
                { 
                    Stage = "Validation", 
                    Progress = 10, 
                    Message = "Validating training configuration and data" 
                });

                if (!ValidateAdvancedTrainingSetup(configuration))
                {
                    result.ErrorMessage = "Advanced training validation failed";
                    return result;
                }

                // Stage 2: Data Preprocessing (25%)
                if (configuration.EnablePreprocessing)
                {
                    progressReporter?.Report(new TrainingProgressInfo 
                    { 
                        Stage = "Preprocessing", 
                        Progress = 25, 
                        Message = "Applying data preprocessing" 
                    });

                    await ApplyAdvancedPreprocessingAsync(configuration, cancellationToken);
                }

                // Stage 3: Hyperparameter Optimization (50%)
                if (configuration.EnableHyperparameterOptimization)
                {
                    progressReporter?.Report(new TrainingProgressInfo 
                    { 
                        Stage = "Optimization", 
                        Progress = 40, 
                        Message = "Optimizing hyperparameters" 
                    });

                    var optimizationResult = await OptimizeHyperparametersAdvancedAsync(configuration, cancellationToken);
                    result.OptimizationResult = optimizationResult;
                    LastOptimizationResult = optimizationResult;

                    if (optimizationResult.Success)
                    {
                        Parameters = optimizationResult.BestParams;
                        BestParameters = optimizationResult.BestParams;
                        BestCrossValidationScore = optimizationResult.BestScore;
                    }
                }

                // Stage 4: Cross-Validation (65%)
                if (configuration.EnableCrossValidation)
                {
                    progressReporter?.Report(new TrainingProgressInfo 
                    { 
                        Stage = "CrossValidation", 
                        Progress = 65, 
                        Message = "Performing cross-validation" 
                    });

                    var cvResult = await PerformCrossValidationAdvancedAsync(configuration, cancellationToken);
                    result.CrossValidationResult = cvResult;
                    LastCrossValidationResult = cvResult;
                }

                // Stage 5: Final Model Training (80%)
                progressReporter?.Report(new TrainingProgressInfo 
                { 
                    Stage = "Training", 
                    Progress = 80, 
                    Message = "Training final model with optimized parameters" 
                });

                bool trainingSuccess = await TrainAsync();
                if (!trainingSuccess)
                {
                    result.ErrorMessage = "Final model training failed";
                    return result;
                }

                // Stage 6: Comprehensive Evaluation (95%)
                progressReporter?.Report(new TrainingProgressInfo 
                { 
                    Stage = "Evaluation", 
                    Progress = 95, 
                    Message = "Performing comprehensive model evaluation" 
                });

                var evaluationResult = await GenerateComprehensiveEvaluationAsync(cancellationToken);
                result.ComprehensiveEvaluation = evaluationResult;

                // Stage 7: Model Persistence (98%)
                if (!string.IsNullOrEmpty(configuration.ModelSavePath))
                {
                    progressReporter?.Report(new TrainingProgressInfo 
                    { 
                        Stage = "Saving", 
                        Progress = 98, 
                        Message = "Saving trained model and metadata" 
                    });

                    bool saveSuccess = SaveModelWithMetadata(configuration.ModelSavePath);
                    result.ModelSaved = saveSuccess;
                }

                // Complete (100%)
                progressReporter?.Report(new TrainingProgressInfo 
                { 
                    Stage = "Complete", 
                    Progress = 100, 
                    Message = "Advanced training completed successfully" 
                });

                result.Success = true;
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.FinalMetrics = ExtractCurrentModelMetrics();

                Editor?.AddLogMessage("PythonTrainingViewModel", 
                    $"Advanced training completed successfully in {result.Duration.TotalMinutes:F2} minutes", 
                    DateTime.Now, -1, null, Errors.Ok);

            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Training was cancelled by user";
                Editor?.AddLogMessage("PythonTrainingViewModel", 
                    "Advanced training cancelled by user", DateTime.Now, -1, null, Errors.Warning);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                Editor?.AddLogMessage("PythonTrainingViewModel", 
                    $"Advanced training failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            finally
            {
                IsBusy = false;
                IsOptimizingHyperparameters = false;
                OptimizationProgress = 0;
            }

            return result;
        }

        /// <summary>
        /// Execute hyperparameter optimization using grid search or random search
        /// </summary>
        /// <param name="parameterGrid">Custom parameter grid (optional)</param>
        /// <param name="searchType">Search strategy</param>
        /// <param name="cvFolds">Cross-validation folds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Optimization result</returns>
        public async Task<HyperparameterOptimizationResult> OptimizeHyperparametersAsync(
            Dictionary<string, object[]> parameterGrid = null,
            SearchType searchType = SearchType.GridSearch,
            int cvFolds = 5,
            CancellationToken cancellationToken = default)
        {
            var result = new HyperparameterOptimizationResult
            {
                Success = false,
                Algorithm = SelectAlgorithm.ToString(),
                SearchType = searchType.ToString(),
                CVFolds = cvFolds
            };

            try
            {
                IsOptimizingHyperparameters = true;
                OptimizationProgress = 0;

                parameterGrid ??= GetDefaultParameterGrid(SelectAlgorithm);
                
                string script = searchType switch
                {
                    OptimizationSearchType.GridSearch => GenerateGridSearchScript(parameterGrid, cvFolds),
                    OptimizationSearchType.RandomSearch => GenerateRandomSearchScript(parameterGrid, 50, cvFolds),
                    _ => GenerateGridSearchScript(parameterGrid, cvFolds)
                };

                bool success = await ExecuteInSessionAsync(script, cancellationToken);
                
                if (success)
                {
                    var optimizationData = GetFromSessionScope<Dictionary<string, object>>("optimization_result", new Dictionary<string, object>());
                    
                    if (optimizationData.ContainsKey("success") && (bool)optimizationData["success"])
                    {
                        result.Success = true;
                        result.BestParams = optimizationData.ContainsKey("best_params") ? 
                            (Dictionary<string, object>)optimizationData["best_params"] : 
                            new Dictionary<string, object>();
                        result.BestScore = optimizationData.ContainsKey("best_score") ? 
                            Convert.ToDouble(optimizationData["best_score"]) : 0.0;

                        // Update view model properties
                        BestParameters = result.BestParams;
                        BestCrossValidationScore = result.BestScore;
                        Parameters = result.BestParams; // Update current parameters
                        
                        Editor?.AddLogMessage("PythonTrainingViewModel", 
                            $"Hyperparameter optimization completed. Best score: {result.BestScore:F4}", 
                            DateTime.Now, -1, null, Errors.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                Editor?.AddLogMessage("PythonTrainingViewModel", 
                    $"Hyperparameter optimization failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            finally
            {
                IsOptimizingHyperparameters = false;
                OptimizationProgress = 0;
            }

            LastOptimizationResult = result;
            return result;
        }

        /// <summary>
        /// Perform cross-validation evaluation
        /// </summary>
        /// <param name="cvFolds">Number of cross-validation folds</param>
        /// <param name="scoringMetric">Scoring metric</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cross-validation result</returns>
        public async Task<CrossValidationResult> PerformCrossValidationAsync(
            int cvFolds = 5,
            string scoringMetric = "accuracy",
            CancellationToken cancellationToken = default)
        {
            var result = new CrossValidationResult
            {
                Success = false,
                Algorithm = SelectAlgorithm.ToString(),
                Folds = cvFolds,
                ScoringMetric = scoringMetric
            };

            try
            {
                string script = GenerateCrossValidationScript(
                    SelectAlgorithm,
                    Parameters ?? new Dictionary<string, object>(),
                    cvFolds,
                    scoringMetric);

                bool success = await ExecuteInSessionAsync(script, cancellationToken);
                
                if (success)
                {
                    var cvData = GetFromSessionScope<Dictionary<string, object>>("cv_result", new Dictionary<string, object>());
                    
                    if (cvData.ContainsKey("success") && (bool)cvData["success"])
                    {
                        result.Success = true;
                        result.MeanScore = cvData.ContainsKey("mean_score") ? 
                            Convert.ToDouble(cvData["mean_score"]) : 0.0;
                        result.StdScore = cvData.ContainsKey("std_score") ? 
                            Convert.ToDouble(cvData["std_score"]) : 0.0;
                        
                        if (cvData.ContainsKey("scores"))
                        {
                            var scores = (object[])cvData["scores"];
                            result.Scores = scores.Select(Convert.ToDouble).ToArray();
                        }

                        Editor?.AddLogMessage("PythonTrainingViewModel", 
                            $"Cross-validation completed. Mean score: {result.MeanScore:F4} (±{result.StdScore:F4})", 
                            DateTime.Now, -1, null, Errors.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                Editor?.AddLogMessage("PythonTrainingViewModel", 
                    $"Cross-validation failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            LastCrossValidationResult = result;
            return result;
        }

        /// <summary>
        /// Compare multiple algorithms and return the best performing one
        /// </summary>
        /// <param name="algorithms">Algorithms to compare</param>
        /// <param name="cvFolds">Cross-validation folds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Model comparison result</returns>
        public async Task<ModelComparisonResult> CompareAlgorithmsAsync(
            MachineLearningAlgorithm[] algorithms,
            int cvFolds = 5,
            CancellationToken cancellationToken = default)
        {
            var result = new ModelComparisonResult
            {
                Success = false,
                CVFolds = cvFolds,
                ComparisonDate = DateTime.Now,
                AlgorithmResults = Array.Empty<AlgorithmResult>()
            };

            try
            {
                string script = GenerateModelComparisonScript(algorithms, cvFolds);
                bool success = await ExecuteInSessionAsync(script, cancellationToken);
                
                if (success)
                {
                    var comparisonData = GetFromSessionScope<Dictionary<string, object>>("comparison_result", new Dictionary<string, object>());
                    
                    if (comparisonData.ContainsKey("success") && (bool)comparisonData["success"])
                    {
                        result.Success = true;
                        result.BestAlgorithm = comparisonData.ContainsKey("best_algorithm") ? 
                            comparisonData["best_algorithm"].ToString() : "";
                        
                        if (comparisonData.ContainsKey("algorithm_results"))
                        {
                            var algorithmResults = (List<object>)comparisonData["algorithm_results"];
                            var resultsList = new List<AlgorithmResult>();
                            
                            foreach (var algResult in algorithmResults)
                            {
                                var dict = (Dictionary<string, object>)algResult;
                                resultsList.Add(new AlgorithmResult
                                {
                                    Algorithm = dict["algorithm"].ToString(),
                                    TrainingTime = TimeSpan.FromSeconds(Convert.ToDouble(dict["training_time"])),
                                    Metrics = new ModelMetrics
                                    {
                                        CVMean = Convert.ToDouble(dict["cv_mean"]),
                                        CVStd = Convert.ToDouble(dict["cv_std"])
                                    }
                                });
                            }
                            
                            result.AlgorithmResults = resultsList.ToArray();
                        }

                        Editor?.AddLogMessage("PythonTrainingViewModel", 
                            $"Algorithm comparison completed. Best: {result.BestAlgorithm}", 
                            DateTime.Now, -1, null, Errors.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                Editor?.AddLogMessage("PythonTrainingViewModel", 
                    $"Algorithm comparison failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            LastModelComparisonResult = result;
            return result;
        }

        /// <summary>
        /// Generate comprehensive model evaluation including ROC curves, confusion matrix, etc.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive evaluation result</returns>
        public async Task<ComprehensiveEvaluationResult> GenerateComprehensiveEvaluationAsync(CancellationToken cancellationToken = default)
        {
            var result = new ComprehensiveEvaluationResult
            {
                Success = false,
                ModelName = SelectAlgorithm.ToString(),
                ModelId = ModelId,
                EvaluationDate = DateTime.Now,
                ModelType = IsClassificationAlgorithm(SelectAlgorithm) ? ModelType.Classification : ModelType.Regression
            };

            try
            {
                if (!IsModelTrained)
                {
                    result.Message = "Model must be trained before comprehensive evaluation";
                    return result;
                }

                string script = GenerateComprehensiveEvaluationScript();
                bool success = await ExecuteInSessionAsync(script, cancellationToken);
                
                if (success)
                {
                    var evaluationData = GetFromSessionScope<Dictionary<string, object>>("comprehensive_evaluation", new Dictionary<string, object>());
                    
                    if (evaluationData.ContainsKey("success") && (bool)evaluationData["success"])
                    {
                        result.Success = true;
                        
                        // Extract metrics based on model type
                        if (result.ModelType == ModelType.Classification)
                        {
                            ExtractClassificationMetrics(result, evaluationData);
                        }
                        else
                        {
                            ExtractRegressionMetrics(result, evaluationData);
                        }
                        
                        // Extract additional analysis data
                        ExtractFeatureImportance(result, evaluationData);
                        ExtractLearningCurves(result, evaluationData);
                        
                        Editor?.AddLogMessage("PythonTrainingViewModel", 
                            "Comprehensive evaluation completed successfully", 
                            DateTime.Now, -1, null, Errors.Ok);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                Editor?.AddLogMessage("PythonTrainingViewModel", 
                    $"Comprehensive evaluation failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return result;
        }

        #endregion

        #region Traditional Training Methods (Enhanced)

        /// <summary>
        /// Execute training with enhanced session support
        /// </summary>
        /// <returns>Error information</returns>
        public IErrorsInfo Train()
        {
            if (!IsDataReady)
            {
                return new ErrorsInfo { Flag = Errors.Failed, Message = "Data is not ready for training" };
            }

            if (!IsSessionConfigured || PythonMLManager == null)
            {
                return new ErrorsInfo { Flag = Errors.Failed, Message = "Session or ML manager not configured" };
            }

            try
            {
                IsBusy = true;
                IsTrainingReady = true;

                // Generate model ID
                ModelId = Guid.NewGuid().ToString();

                // Train the model using ML manager
                PythonMLManager.TrainModel(ModelId, SelectAlgorithm, Parameters ?? new Dictionary<string, object>(), SelectedFeatures ?? Features, LabelColumn);

                IsModelTrained = true;
                
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Model training completed successfully: {ModelId}", DateTime.Now, -1, null, Errors.Ok);
                
                return new ErrorsInfo { Flag = Errors.Ok, Message = "Training completed successfully" };
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Training failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return new ErrorsInfo { Flag = Errors.Failed, Message = ex.Message };
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Execute training asynchronously
        /// </summary>
        /// <returns>True if training successful</returns>
        public async Task<bool> TrainAsync()
        {
            if (!IsDataReady)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", "Data is not ready for training", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            if (!IsSessionConfigured || PythonMLManager == null)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", "Session or ML manager not configured", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            try
            {
                IsBusy = true;
                IsTrainingReady = true;

                // Generate model ID
                ModelId = Guid.NewGuid().ToString();

                // Train the model asynchronously using ML manager
                bool success = await PythonMLManager.TrainModelAsync(ModelId, SelectAlgorithm, Parameters ?? new Dictionary<string, object>(), SelectedFeatures ?? Features, LabelColumn, Token);

                if (success)
                {
                    IsModelTrained = true;
                    Editor?.AddLogMessage("PythonTrainingViewModel", $"Async model training completed successfully: {ModelId}", DateTime.Now, -1, null, Errors.Ok);
                }
                else
                {
                    Editor?.AddLogMessage("PythonTrainingViewModel", "Async model training failed", DateTime.Now, -1, null, Errors.Failed);
                }

                return success;
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Async training failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Evaluation Methods
        /// <summary>
        /// Evaluate the trained model
        /// </summary>
        /// <returns>True if evaluation successful</returns>
        public async Task<bool> EvaluateModelAsync()
        {
            if (!IsModelTrained || string.IsNullOrEmpty(ModelId))
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", "Model must be trained before evaluation", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            if (!IsSessionConfigured || PythonMLManager == null)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", "Session or ML manager not configured", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            try
            {
                IsBusy = true;

                // Check if this is a classification or regression problem
                bool isClassification = IsClassificationAlgorithm(SelectAlgorithm);

                if (isClassification)
                {
                    var scores = await PythonMLManager.GetModelClassificationScoreAsync(ModelId, Token);
                    F1Accuracy = scores.Item1;
                    EvalScore = scores.Item2;
                }
                else
                {
                    var scores = await PythonMLManager.GetModelRegressionScoresAsync(ModelId, Token);
                    MseScore = scores.Item1;
                    RmseScore = scores.Item2;
                    MaeScore = scores.Item3;
                }

                IsModelEvaluated = true;
                Editor?.AddLogMessage("PythonTrainingViewModel", "Model evaluation completed successfully", DateTime.Now, -1, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Model evaluation failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Check if the algorithm is a classification algorithm
        /// </summary>
        /// <param name="algorithm">Machine learning algorithm</param>
        /// <returns>True if classification algorithm</returns>
        private bool IsClassificationAlgorithm(MachineLearningAlgorithm algorithm)
        {
            return algorithm.ToString().Contains("Classifier") || 
                   algorithm == MachineLearningAlgorithm.LogisticRegression ||
                   algorithm.ToString().Contains("NB");
        }
        #endregion

        #region Model Management Methods
        /// <summary>
        /// Save the trained model
        /// </summary>
        /// <param name="filePath">Path to save the model</param>
        /// <returns>True if model saved successfully</returns>
        public bool SaveModel(string filePath)
        {
            if (!IsModelTrained || string.IsNullOrEmpty(ModelId))
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", "Model must be trained before saving", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            if (PythonMLManager == null)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", "ML manager not configured", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            try
            {
                PythonMLManager.SaveModel(ModelId, filePath);
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Model saved successfully: {filePath}", DateTime.Now, -1, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Failed to save model: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Save model with comprehensive metadata
        /// </summary>
        /// <param name="filePath">Path to save the model</param>
        /// <returns>True if model saved successfully</returns>
        public bool SaveModelWithMetadata(string filePath)
        {
            try
            {
                // Save the model using the base functionality
                bool saveSuccess = SaveModel(filePath);
                
                if (saveSuccess)
                {
                    // Save additional metadata
                    var metadata = new
                    {
                        ModelId = ModelId,
                        Algorithm = SelectAlgorithm.ToString(),
                        Parameters = Parameters,
                        BestParameters = BestParameters,
                        Features = Features,
                        SelectedFeatures = SelectedFeatures,
                        LabelColumn = LabelColumn,
                        TrainingDate = DateTime.Now,
                        IsModelTrained = IsModelTrained,
                        IsModelEvaluated = IsModelEvaluated,
                        Metrics = ExtractCurrentModelMetrics(),
                        OptimizationResult = LastOptimizationResult,
                        CrossValidationResult = LastCrossValidationResult,
                        TrainingSummary = GetTrainingSummary()
                    };

                    string metadataPath = filePath.Replace(".pkl", "_metadata.json").Replace(".joblib", "_metadata.json");
                    string metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(metadataPath, metadataJson);
                    
                    Editor?.AddLogMessage("PythonTrainingViewModel", $"Model metadata saved: {metadataPath}", DateTime.Now, -1, null, Errors.Ok);
                }

                return saveSuccess;
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", 
                    $"Failed to save model with metadata: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Load a pre-trained model
        /// </summary>
        /// <param name="filePath">Path to the model file</param>
        /// <returns>True if model loaded successfully</returns>
        public bool LoadModel(string filePath)
        {
            if (PythonMLManager == null)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", "ML manager not configured", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }

            try
            {
                ModelId = PythonMLManager.LoadModel(filePath);
                if (!string.IsNullOrEmpty(ModelId))
                {
                    IsModelTrained = true;
                    
                    // Try to load metadata if available
                    string metadataPath = filePath.Replace(".pkl", "_metadata.json").Replace(".joblib", "_metadata.json");
                    if (File.Exists(metadataPath))
                    {
                        try
                        {
                            string metadataJson = File.ReadAllText(metadataPath);
                            var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
                            
                            // Restore some metadata if available
                            if (metadata.ContainsKey("Parameters"))
                            {
                                Parameters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(metadata["Parameters"].ToString());
                            }
                            
                            Editor?.AddLogMessage("PythonTrainingViewModel", "Model metadata loaded successfully", DateTime.Now, -1, null, Errors.Ok);
                        }
                        catch (Exception metaEx)
                        {
                            Editor?.AddLogMessage("PythonTrainingViewModel", $"Failed to load model metadata: {metaEx.Message}", DateTime.Now, -1, null, Errors.Warning);
                        }
                    }
                    
                    Editor?.AddLogMessage("PythonTrainingViewModel", $"Model loaded successfully: {ModelId}", DateTime.Now, -1, null, Errors.Ok);
                    return true;
                }
                else
                {
                    Editor?.AddLogMessage("PythonTrainingViewModel", "Failed to load model", DateTime.Now, -1, null, Errors.Failed);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonTrainingViewModel", $"Failed to load model: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Get enhanced training summary with advanced metrics
        /// </summary>
        /// <returns>Training summary information</returns>
        public string GetTrainingSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"=== Training Summary ===");
            summary.AppendLine($"Model ID: {ModelId}");
            summary.AppendLine($"Algorithm: {SelectAlgorithm}");
            summary.AppendLine($"Features: {Features?.Length ?? 0}");
            summary.AppendLine($"Selected Features: {SelectedFeatures?.Length ?? 0}");
            summary.AppendLine($"Label Column: {LabelColumn}");
            summary.AppendLine($"Data Ready: {IsDataReady}");
            summary.AppendLine($"Model Trained: {IsModelTrained}");
            summary.AppendLine($"Model Evaluated: {IsModelEvaluated}");
            
            if (BestParameters?.Count > 0)
            {
                summary.AppendLine($"\n=== Optimized Parameters ===");
                foreach (var param in BestParameters)
                {
                    summary.AppendLine($"{param.Key}: {param.Value}");
                }
                summary.AppendLine($"Best CV Score: {BestCrossValidationScore:F4}");
            }
            
            if (IsModelEvaluated)
            {
                summary.AppendLine($"\n=== Performance Metrics ===");
                if (IsClassificationAlgorithm(SelectAlgorithm))
                {
                    summary.AppendLine($"F1 Accuracy: {F1Accuracy:F4}");
                    summary.AppendLine($"Evaluation Score: {EvalScore:F4}");
                }
                else
                {
                    summary.AppendLine($"MSE Score: {MseScore:F4}");
                    summary.AppendLine($"RMSE Score: {RmseScore:F4}");
                    summary.AppendLine($"MAE Score: {MaeScore:F4}");
                }
            }

            if (LastCrossValidationResult?.Success == true)
            {
                summary.AppendLine($"\n=== Cross-Validation ===");
                summary.AppendLine($"CV Mean Score: {LastCrossValidationResult.MeanScore:F4}");
                summary.AppendLine($"CV Std Score: {LastCrossValidationResult.StdScore:F4}");
                summary.AppendLine($"CV Folds: {LastCrossValidationResult.Folds}");
            }

            return summary.ToString();
        }
        #endregion
    }
}
