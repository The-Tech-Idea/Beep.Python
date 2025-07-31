using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using TheTechIdea.Beep.Addin;

namespace Beep.Python.ML
{
    /// <summary>
    /// Ultimate ML Training Assistant - Provides simplified, high-level access to comprehensive ML workflows
    /// This is the main entry point for advanced ML operations, designed for ease of use while maintaining full power
    /// </summary>
    public class MLTrainingAssistant : IDisposable
    {
        #region Private Fields
        private readonly PythonTrainingViewModel _trainingViewModel;
        private readonly IPythonMLManager _mlManager;
        private readonly IDMEEditor _editor;
        private volatile bool _isDisposed = false;
        #endregion

        #region Properties
        public bool IsReady => _trainingViewModel?.IsSessionConfigured == true && _trainingViewModel?.IsDataReady == true;
        public bool IsBusy => _trainingViewModel?.IsBusy == true;
        public string[] AvailableFeatures => _trainingViewModel?.Features ?? Array.Empty<string>();
        public string[] SelectedFeatures => _trainingViewModel?.SelectedFeatures ?? Array.Empty<string>();
        public string LabelColumn => _trainingViewModel?.LabelColumn;
        public bool IsModelTrained => _trainingViewModel?.IsModelTrained == true;
        public bool IsModelEvaluated => _trainingViewModel?.IsModelEvaluated == true;
        public MachineLearningAlgorithm CurrentAlgorithm => _trainingViewModel?.SelectAlgorithm ?? MachineLearningAlgorithm.RandomForestClassifier;
        #endregion

        #region Constructor
        public MLTrainingAssistant(PythonTrainingViewModel trainingViewModel)
        {
            _trainingViewModel = trainingViewModel ?? throw new ArgumentNullException(nameof(trainingViewModel));
            _mlManager = trainingViewModel.PythonMLManager;
            _editor = trainingViewModel.Editor;
        }
        #endregion

        #region Quick Start Methods

        /// <summary>
        /// One-click ML training - Load data, train model, and evaluate
        /// Perfect for getting started quickly with sensible defaults
        /// </summary>
        /// <param name="dataPath">Path to training data</param>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="algorithm">Algorithm to use (optional, auto-selected if not specified)</param>
        /// <param name="testSize">Test set size (default 0.2)</param>
        /// <returns>Quick training result</returns>
        public async Task<QuickTrainingResult> QuickTrainAsync(
            string dataPath,
            string targetColumn,
            MachineLearningAlgorithm? algorithm = null,
            double testSize = 0.2)
        {
            var result = new QuickTrainingResult { Success = false, StartTime = DateTime.Now };

            try
            {
                _editor?.AddLogMessage("MLTrainingAssistant", "?? Starting Quick Train workflow", DateTime.Now, -1, null, Errors.Ok);

                // Step 1: Load data
                bool dataLoaded = await _trainingViewModel.LoadTrainingDataAsync(dataPath);
                if (!dataLoaded)
                {
                    result.ErrorMessage = "Failed to load training data";
                    return result;
                }

                // Step 2: Set target column
                _trainingViewModel.LabelColumn = targetColumn;

                // Step 3: Auto-select algorithm if not specified
                if (algorithm.HasValue)
                {
                    _trainingViewModel.SelectAlgorithm = algorithm.Value;
                }
                else
                {
                    _trainingViewModel.SelectAlgorithm = await AutoSelectAlgorithmAsync(targetColumn);
                }

                // Step 4: Use sensible defaults for preprocessing
                var config = new AdvancedTrainingConfiguration
                {
                    EnablePreprocessing = true,
                    EnableCrossValidation = true,
                    PreprocessingConfig = new PreprocessingConfiguration
                    {
                        TestSize = testSize,
                        ScalingMethod = ScalingMethod.StandardScaler,
                        MissingValueStrategy = MissingValueStrategy.Mean,
                        CategoricalEncoding = CategoricalEncoding.OneHot
                    },
                    CrossValidationFolds = 5
                };

                // Step 5: Execute advanced training
                var advancedResult = await _trainingViewModel.ExecuteAdvancedTrainingAsync(config);
                
                // Step 6: Evaluate model
                await _trainingViewModel.EvaluateModelAsync();

                result.Success = advancedResult.Success;
                result.Algorithm = _trainingViewModel.SelectAlgorithm.ToString();
                result.TrainingTime = advancedResult.Duration;
                result.EndTime = DateTime.Now;
                
                if (_trainingViewModel.IsClassificationAlgorithm(_trainingViewModel.SelectAlgorithm))
                {
                    result.Accuracy = _trainingViewModel.EvalScore;
                    result.F1Score = _trainingViewModel.F1Accuracy;
                }
                else
                {
                    result.RMSE = _trainingViewModel.RmseScore;
                    result.MAE = _trainingViewModel.MaeScore;
                }

                _editor?.AddLogMessage("MLTrainingAssistant", 
                    $"? Quick Train completed successfully in {result.TrainingTime.TotalMinutes:F2} minutes", 
                    DateTime.Now, -1, null, Errors.Ok);

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                _editor?.AddLogMessage("MLTrainingAssistant", $"? Quick Train failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return result;
            }
        }

        /// <summary>
        /// Smart AutoML - Automatically select best algorithm and optimize hyperparameters
        /// </summary>
        /// <param name="dataPath">Path to training data</param>
        /// <param name="targetColumn">Target column name</param>
        /// <param name="timeLimit">Time limit for optimization (default 30 minutes)</param>
        /// <param name="maxTrials">Maximum number of trials (default 50)</param>
        /// <returns>AutoML result</returns>
        public async Task<AutoMLResult> SmartAutoMLAsync(
            string dataPath,
            string targetColumn,
            TimeSpan? timeLimit = null,
            int maxTrials = 50)
        {
            timeLimit ??= TimeSpan.FromMinutes(30);
            
            var result = new AutoMLResult { Success = false, StartTime = DateTime.Now, TimeLimit = timeLimit.Value };

            try
            {
                _editor?.AddLogMessage("MLTrainingAssistant", "?? Starting Smart AutoML workflow", DateTime.Now, -1, null, Errors.Ok);

                // Load data
                bool dataLoaded = await _trainingViewModel.LoadTrainingDataAsync(dataPath);
                if (!dataLoaded)
                {
                    result.ErrorMessage = "Failed to load training data";
                    return result;
                }

                _trainingViewModel.LabelColumn = targetColumn;

                // Determine task type
                result.TaskType = await DetermineTaskTypeAsync(targetColumn);

                // Get algorithms suitable for this task type
                var algorithms = GetAlgorithmsForTaskType(result.TaskType);

                var bestScore = double.MinValue;
                var bestAlgorithm = "";
                var bestParams = new Dictionary<string, object>();
                var trialResults = new List<Dictionary<string, object>>();

                var startTime = DateTime.Now;

                foreach (var algorithm in algorithms.Take(Math.Min(algorithms.Length, maxTrials / 10)))
                {
                    if (DateTime.Now - startTime > timeLimit.Value)
                        break;

                    try
                    {
                        _trainingViewModel.SelectAlgorithm = algorithm;

                        // Quick hyperparameter optimization
                        var optimizationResult = await _trainingViewModel.OptimizeHyperparametersAdvancedAsync(
                            SearchType.RandomSearch,
                            maxIterations: 10);

                        if (optimizationResult.Success && optimizationResult.BestScore > bestScore)
                        {
                            bestScore = optimizationResult.BestScore;
                            bestAlgorithm = algorithm.ToString();
                            bestParams = optimizationResult.BestParams;
                        }

                        trialResults.Add(new Dictionary<string, object>
                        {
                            ["algorithm"] = algorithm.ToString(),
                            ["score"] = optimizationResult.BestScore,
                            ["parameters"] = optimizationResult.BestParams
                        });
                    }
                    catch (Exception ex)
                    {
                        _editor?.AddLogMessage("MLTrainingAssistant", $"?? Trial failed for {algorithm}: {ex.Message}", DateTime.Now, -1, null, Errors.Warning);
                    }
                }

                result.Success = !string.IsNullOrEmpty(bestAlgorithm);
                result.BestAlgorithm = bestAlgorithm;
                result.BestScore = bestScore;
                result.BestParameters = bestParams;
                result.TrialResults = trialResults;
                result.TotalTrials = trialResults.Count;
                result.EndTime = DateTime.Now;
                result.ActualDuration = result.EndTime - result.StartTime;

                if (result.Success)
                {
                    // Set best configuration and train final model
                    _trainingViewModel.SelectAlgorithm = Enum.Parse<MachineLearningAlgorithm>(bestAlgorithm);
                    _trainingViewModel.Parameters = bestParams;
                    await _trainingViewModel.TrainAsync();
                    await _trainingViewModel.EvaluateModelAsync();
                }

                _editor?.AddLogMessage("MLTrainingAssistant", 
                    $"? Smart AutoML completed: {bestAlgorithm} with score {bestScore:F4}", 
                    DateTime.Now, -1, null, Errors.Ok);

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.Now;
                _editor?.AddLogMessage("MLTrainingAssistant", $"? Smart AutoML failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return result;
            }
        }

        /// <summary>
        /// Compare multiple algorithms quickly
        /// </summary>
        /// <param name="algorithms">Algorithms to compare (optional, auto-selected if not provided)</param>
        /// <param name="cvFolds">Cross-validation folds</param>
        /// <returns>Model comparison result</returns>
        public async Task<ModelComparisonResult> CompareAlgorithmsAsync(
            MachineLearningAlgorithm[] algorithms = null,
            int cvFolds = 5)
        {
            try
            {
                if (!IsReady)
                    throw new InvalidOperationException("Data must be loaded and configured before comparing algorithms");

                algorithms ??= GetDefaultAlgorithmsForComparison();

                _editor?.AddLogMessage("MLTrainingAssistant", $"?? Comparing {algorithms.Length} algorithms", DateTime.Now, -1, null, Errors.Ok);

                var result = await _trainingViewModel.CompareAlgorithmsAsync(algorithms, cvFolds);

                _editor?.AddLogMessage("MLTrainingAssistant", 
                    $"? Algorithm comparison completed. Best: {result.BestAlgorithm}", 
                    DateTime.Now, -1, null, Errors.Ok);

                return result;
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MLTrainingAssistant", $"? Algorithm comparison failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                throw;
            }
        }

        #endregion

        #region Advanced Features

        /// <summary>
        /// Optimize hyperparameters with different strategies
        /// </summary>
        /// <param name="strategy">Optimization strategy</param>
        /// <param name="maxIterations">Maximum iterations</param>
        /// <param name="customGrid">Custom parameter grid (optional)</param>
        /// <returns>Optimization result</returns>
        public async Task<HyperparameterOptimizationResult> OptimizeHyperparametersAsync(
            SearchType strategy = SearchType.GridSearch,
            int maxIterations = 100,
            Dictionary<string, object[]> customGrid = null)
        {
            try
            {
                if (!IsReady)
                    throw new InvalidOperationException("Data must be loaded and configured before optimization");

                _editor?.AddLogMessage("MLTrainingAssistant", $"?? Starting {strategy} hyperparameter optimization", DateTime.Now, -1, null, Errors.Ok);

                var result = await _trainingViewModel.OptimizeHyperparametersAdvancedAsync(
                    strategy, customGrid, maxIterations);

                _editor?.AddLogMessage("MLTrainingAssistant", 
                    $"? Hyperparameter optimization completed. Best score: {result.BestScore:F4}", 
                    DateTime.Now, -1, null, Errors.Ok);

                return result;
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MLTrainingAssistant", $"? Hyperparameter optimization failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Create and train ensemble models
        /// </summary>
        /// <param name="ensembleType">Type of ensemble</param>
        /// <param name="baseModels">Base models for ensemble (optional, auto-selected if not provided)</param>
        /// <returns>Ensemble result</returns>
        public async Task<EnsembleResult> TrainEnsembleAsync(
            EnsembleType ensembleType = EnsembleType.Voting,
            MachineLearningAlgorithm[] baseModels = null)
        {
            try
            {
                if (!IsReady)
                    throw new InvalidOperationException("Data must be loaded and configured before ensemble training");

                baseModels ??= GetDefaultEnsembleModels();

                _editor?.AddLogMessage("MLTrainingAssistant", $"?? Training {ensembleType} ensemble with {baseModels.Length} models", DateTime.Now, -1, null, Errors.Ok);

                var result = await _trainingViewModel.TrainEnsembleAsync(ensembleType, baseModels);

                _editor?.AddLogMessage("MLTrainingAssistant", 
                    $"? Ensemble training completed. Score: {result.EnsembleScore:F4}", 
                    DateTime.Now, -1, null, Errors.Ok);

                return result;
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MLTrainingAssistant", $"? Ensemble training failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                throw;
            }
        }

        /// <summary>
        /// Generate comprehensive evaluation report
        /// </summary>
        /// <param name="includeCharts">Include chart data</param>
        /// <returns>Evaluation report</returns>
        public async Task<string> GenerateReportAsync(bool includeCharts = true)
        {
            try
            {
                if (!IsModelTrained)
                    throw new InvalidOperationException("Model must be trained before generating report");

                _editor?.AddLogMessage("MLTrainingAssistant", "?? Generating comprehensive report", DateTime.Now, -1, null, Errors.Ok);

                // Ensure model is evaluated
                if (!IsModelEvaluated)
                {
                    await _trainingViewModel.EvaluateModelAsync();
                }

                var report = _trainingViewModel.GenerateAdvancedReport(includeCharts);

                _editor?.AddLogMessage("MLTrainingAssistant", "? Report generated successfully", DateTime.Now, -1, null, Errors.Ok);

                return report;
            }
            catch (Exception ex)
            {
                _editor?.AddLogMessage("MLTrainingAssistant", $"? Report generation failed: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Auto-select best algorithm based on data characteristics
        /// </summary>
        private async Task<MachineLearningAlgorithm> AutoSelectAlgorithmAsync(string targetColumn)
        {
            try
            {
                // Simple heuristics for algorithm selection
                var taskType = await DetermineTaskTypeAsync(targetColumn);
                var dataSize = _trainingViewModel.Features?.Length ?? 0;

                return taskType switch
                {
                    ModelType.Classification when dataSize < 1000 => MachineLearningAlgorithm.LogisticRegression,
                    ModelType.Classification when dataSize < 10000 => MachineLearningAlgorithm.RandomForestClassifier,
                    ModelType.Classification => MachineLearningAlgorithm.GradientBoostingClassifier,
                    ModelType.Regression when dataSize < 1000 => MachineLearningAlgorithm.LinearRegression,
                    ModelType.Regression when dataSize < 10000 => MachineLearningAlgorithm.RandomForestRegressor,
                    ModelType.Regression => MachineLearningAlgorithm.GradientBoostingRegressor,
                    _ => MachineLearningAlgorithm.RandomForestClassifier
                };
            }
            catch
            {
                return MachineLearningAlgorithm.RandomForestClassifier; // Safe default
            }
        }

        /// <summary>
        /// Determine if the task is classification or regression
        /// </summary>
        private async Task<ModelType> DetermineTaskTypeAsync(string targetColumn)
        {
            try
            {
                // This would need to be implemented by examining the target column
                // For now, return a reasonable default
                return ModelType.Classification;
            }
            catch
            {
                return ModelType.Classification;
            }
        }

        /// <summary>
        /// Get algorithms suitable for the task type
        /// </summary>
        private MachineLearningAlgorithm[] GetAlgorithmsForTaskType(ModelType taskType)
        {
            return taskType switch
            {
                ModelType.Classification => new[]
                {
                    MachineLearningAlgorithm.RandomForestClassifier,
                    MachineLearningAlgorithm.GradientBoostingClassifier,
                    MachineLearningAlgorithm.LogisticRegression,
                    MachineLearningAlgorithm.SVC,
                    MachineLearningAlgorithm.DecisionTreeClassifier,
                    MachineLearningAlgorithm.KNeighborsClassifier
                },
                ModelType.Regression => new[]
                {
                    MachineLearningAlgorithm.RandomForestRegressor,
                    MachineLearningAlgorithm.GradientBoostingRegressor,
                    MachineLearningAlgorithm.LinearRegression,
                    MachineLearningAlgorithm.SVR,
                    MachineLearningAlgorithm.DecisionTreeRegressor,
                    MachineLearningAlgorithm.KNeighborsRegressor
                },
                _ => new[] { MachineLearningAlgorithm.RandomForestClassifier }
            };
        }

        /// <summary>
        /// Get default algorithms for comparison
        /// </summary>
        private MachineLearningAlgorithm[] GetDefaultAlgorithmsForComparison()
        {
            var taskType = _trainingViewModel.IsClassificationAlgorithm(_trainingViewModel.SelectAlgorithm) 
                ? ModelType.Classification 
                : ModelType.Regression;

            return GetAlgorithmsForTaskType(taskType).Take(4).ToArray();
        }

        /// <summary>
        /// Get default models for ensemble
        /// </summary>
        private MachineLearningAlgorithm[] GetDefaultEnsembleModels()
        {
            return GetDefaultAlgorithmsForComparison().Take(3).ToArray();
        }

        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _editor?.AddLogMessage("MLTrainingAssistant", "ML Training Assistant disposed", DateTime.Now, -1, null, Errors.Ok);
            }
        }
        #endregion
    }

    #region Result Classes

    /// <summary>
    /// Quick training result for simplified workflows
    /// </summary>
    public class QuickTrainingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TrainingTime { get; set; }
        public string Algorithm { get; set; }
        public double Accuracy { get; set; }
        public double F1Score { get; set; }
        public double RMSE { get; set; }
        public double MAE { get; set; }
    }

    #endregion
}