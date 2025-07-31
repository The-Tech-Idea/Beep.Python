using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Beep.Python.Model;

namespace Beep.Python.ML
{
    #region Enhanced ML Enums
    public enum MissingValueStrategy
    {
        Mean,
        Median,
        Mode,
        Drop,
        Forward,
        Backward,
        Custom,
        KNNImputer,
        IterativeImputer,
        Constant
    }

    public enum CategoricalEncoding
    {
        OneHot,
        Label,
        Target,
        Binary,
        Frequency,
        Ordinal,
        WOE, // Weight of Evidence
        BaseN
    }

    public enum ScalingMethod
    {
        None,
        StandardScaler,
        MinMaxScaler,
        RobustScaler,
        Normalizer,
        PowerTransformer,
        QuantileTransformer
    }

    public enum FeatureSelectionMethod
    {
        Correlation,
        Statistical,
        RFE,
        TreeBased,
        L1Regularization,
        MutualInformation,
        ChiSquare,
        ANOVA,
        VarianceThreshold,
        SelectKBest
    }

    public enum SearchType
    {
        GridSearch,
        RandomSearch,
        BayesianOptimization,
        HalvingGridSearch,
        HalvingRandomSearch,
        HyperBand,
        EvolutionarySearch,
        PopulationBasedTraining
    }

    public enum EnsembleType
    {
        Voting,
        Bagging,
        Stacking,
        Boosting,
        RandomForest,
        ExtraTrees,
        Blending
    }

    public enum ModelType
    {
        Classification,
        Regression,
        Clustering,
        Dimensionality,
        Anomaly,
        TimeSeries,
        NeuralNetwork
    }

    public enum CrossValidationType
    {
        KFold,
        StratifiedKFold,
        TimeSeriesSplit,
        GroupKFold,
        RepeatedKFold,
        LeaveOneOut,
        ShuffleSplit
    }

    public enum OptimizationStrategy
    {
        GridSearch,
        RandomSearch,
        BayesianOptimization,
        HyperBand,
        EvolutionarySearch,
        AutoML
    }

    public enum ParameterType
    {
        Integer,
        Float,
        Boolean,
        Categorical,
        Discrete,
        Continuous
    }
    #endregion

    #region Configuration Classes
    public class PreprocessingConfiguration
    {
        public MissingValueStrategy MissingValueStrategy { get; set; } = MissingValueStrategy.Mean;
        public CategoricalEncoding CategoricalEncoding { get; set; } = CategoricalEncoding.OneHot;
        public ScalingMethod ScalingMethod { get; set; } = ScalingMethod.StandardScaler;
        public double TestSize { get; set; } = 0.2;
        public int RandomState { get; set; } = 42;
        public bool Stratify { get; set; } = true;
        public bool RemoveOutliers { get; set; } = false;
        public double OutlierThreshold { get; set; } = 3.0;
        public bool HandleImbalanced { get; set; } = false;
        public string ImbalancedStrategy { get; set; } = "SMOTE";
        public bool ApplyFeatureSelection { get; set; } = false;
        public FeatureSelectionMethod FeatureSelectionMethod { get; set; } = FeatureSelectionMethod.SelectKBest;
        public int MaxFeatures { get; set; } = 20;
        public string[] CategoricalFeatures { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }

    public class AdvancedTrainingConfiguration
    {
        // Core Training Settings
        public bool EnablePreprocessing { get; set; } = true;
        public bool EnableAlgorithmComparison { get; set; } = false;
        public bool EnableHyperparameterOptimization { get; set; } = true;
        public bool EnableCrossValidation { get; set; } = true;
        public bool EnableEnsemble { get; set; } = false;
        public bool EnableFeatureEngineering { get; set; } = false;
        
        // Algorithm Selection
        public string SelectedAlgorithm { get; set; } = "RandomForestClassifier";
        public string TargetColumn { get; set; } = "";
        
        // Preprocessing Settings
        public PreprocessingConfiguration PreprocessingConfig { get; set; } = new PreprocessingConfiguration();
        
        // Hyperparameter Optimization Settings
        public OptimizationStrategy OptimizationStrategy { get; set; } = OptimizationStrategy.GridSearch;
        public Dictionary<string, object[]> CustomParameterGrid { get; set; }
        public int MaxOptimizationIterations { get; set; } = 100;
        
        // Cross-Validation Settings
        public int CrossValidationFolds { get; set; } = 5;
        public CrossValidationType CrossValidationType { get; set; } = CrossValidationType.StratifiedKFold;
        public string ScoringMetric { get; set; } = "accuracy";
        
        // Algorithm Comparison Settings
        public MachineLearningAlgorithm[] AlgorithmsToCompare { get; set; }
        
        // Ensemble Settings
        public EnsembleType EnsembleType { get; set; } = EnsembleType.Voting;
        public MachineLearningAlgorithm[] EnsembleBaseModels { get; set; }
        
        // Model Management
        public bool SaveModel { get; set; } = true;
        public string ModelSavePath { get; set; }
        public bool GenerateReport { get; set; } = true;
        public string ReportSavePath { get; set; }
        
        // Performance Settings
        public int MaxTrainingTimeMinutes { get; set; } = 60;
        public bool UseParallelProcessing { get; set; } = true;
        public int MaxCpuCores { get; set; } = -1; // -1 means use all available
        
        public int GetTotalSteps()
        {
            int steps = 3; // Setup, Training, Evaluation
            if (EnablePreprocessing) steps++;
            if (EnableAlgorithmComparison) steps++;
            if (EnableHyperparameterOptimization) steps++;
            if (EnableCrossValidation) steps++;
            if (EnableEnsemble) steps++;
            if (EnableFeatureEngineering) steps++;
            if (SaveModel || GenerateReport) steps++;
            return steps;
        }
    }

    public class ParameterRange
    {
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
        public ParameterType Type { get; set; }
        public object[] DiscreteValues { get; set; }
        public bool IsLogarithmic { get; set; } = false;
        public double Step { get; set; } = 1.0;
    }
    #endregion

    #region Core Data Classes
    public class ModelMetrics
    {
        // Classification Metrics
        public double Accuracy { get; set; }
        public double Precision { get; set; }
        public double Recall { get; set; }
        public double F1Score { get; set; }
        public double AUC { get; set; }
        public double ROCAuc { get; set; }
        public double LogLoss { get; set; }
        
        // Regression Metrics
        public double MeanSquaredError { get; set; }
        public double RootMeanSquaredError { get; set; }
        public double MeanAbsoluteError { get; set; }
        public double R2Score { get; set; }
        public double MeanAbsolutePercentageError { get; set; }
        
        // Cross-Validation Metrics
        public double CVMean { get; set; }
        public double CVStd { get; set; }
        public double[] CVScores { get; set; } = Array.Empty<double>();
        
        // Training Metrics
        public double TrainingTime { get; set; }
        public double PredictionTime { get; set; }
        
        // Additional Metrics
        public Dictionary<string, double> CustomMetrics { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> ClassificationReport { get; set; } = new Dictionary<string, double>();
        
        // Confidence intervals
        public Dictionary<string, (double Lower, double Upper)> ConfidenceIntervals { get; set; } = new Dictionary<string, (double, double)>();
    }

    public class ROCData
    {
        public double[] FPR { get; set; } = Array.Empty<double>();
        public double[] TPR { get; set; } = Array.Empty<double>();
        public double[] Thresholds { get; set; } = Array.Empty<double>();
        public double AUC { get; set; }
        public double OptimalThreshold { get; set; }
        public int OptimalThresholdIndex { get; set; }
    }

    public class PrecisionRecallData
    {
        public double[] Precision { get; set; } = Array.Empty<double>();
        public double[] Recall { get; set; } = Array.Empty<double>();
        public double[] Thresholds { get; set; } = Array.Empty<double>();
        public double AveragePrecision { get; set; }
        public double OptimalThreshold { get; set; }
        public double OptimalF1Score { get; set; }
    }

    public class ConfusionMatrixData
    {
        public int[,] Matrix { get; set; }
        public string[] ClassLabels { get; set; } = Array.Empty<string>();
        public Dictionary<string, double> ClassificationMetrics { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, Dictionary<string, double>> PerClassMetrics { get; set; } = new Dictionary<string, Dictionary<string, double>>();
        public bool IsNormalized { get; set; } = false;
    }

    public class FeatureImportanceData
    {
        public string[] Features { get; set; } = Array.Empty<string>();
        public double[] Importances { get; set; } = Array.Empty<double>();
        public double[] StandardDeviations { get; set; } = Array.Empty<double>();
        public Dictionary<string, double> PermutationImportances { get; set; } = new Dictionary<string, double>();
        public string ImportanceType { get; set; } = "feature_importances_";
    }

    public class LearningCurveData
    {
        public int[] TrainSizes { get; set; } = Array.Empty<int>();
        public double[] TrainScores { get; set; } = Array.Empty<double>();
        public double[] ValidationScores { get; set; } = Array.Empty<double>();
        public double[] TrainStd { get; set; } = Array.Empty<double>();
        public double[] ValidationStd { get; set; } = Array.Empty<double>();
        public string ScoringMetric { get; set; }
    }

    public class ValidationCurveData
    {
        public string ParameterName { get; set; } = "";
        public double[] ParameterValues { get; set; } = Array.Empty<double>();
        public double[] TrainScores { get; set; } = Array.Empty<double>();
        public double[] ValidationScores { get; set; } = Array.Empty<double>();
        public double[] TrainStd { get; set; } = Array.Empty<double>();
        public double[] ValidationStd { get; set; } = Array.Empty<double>();
    }
    #endregion

    #region Result Classes
    public class DataPreparationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public Tuple<int, int> OriginalShape { get; set; }
        public Tuple<int, int> ProcessedShape { get; set; }
        public Tuple<int, int> TrainShape { get; set; }
        public Tuple<int, int> TestShape { get; set; }
        public string[] FeatureNames { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> DataQuality { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> TargetDistribution { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> DataTypes { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, int> MissingValues { get; set; } = new Dictionary<string, int>();
        public List<string> ProcessingSteps { get; set; } = new List<string>();
        public TimeSpan ProcessingTime { get; set; }
    }

    public class FeatureSelectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string[] SelectedFeatures { get; set; } = Array.Empty<string>();
        public int OriginalFeatureCount { get; set; }
        public int SelectedFeatureCount { get; set; }
        public double ReductionRatio { get; set; }
        public Dictionary<string, object> MethodResults { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, double> FeatureScores { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, int> FeatureRankings { get; set; } = new Dictionary<string, int>();
        public List<FeatureSelectionMethod> MethodsUsed { get; set; } = new List<FeatureSelectionMethod>();
        public TimeSpan SelectionTime { get; set; }
    }

    public class HyperparameterOptimizationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public Dictionary<string, object> BestParams { get; set; } = new Dictionary<string, object>();
        public double BestScore { get; set; }
        public string SearchType { get; set; } = "";
        public string Algorithm { get; set; } = "";
        public int CVFolds { get; set; }
        public Dictionary<string, object> CVResults { get; set; } = new Dictionary<string, object>();
        public List<OptimizationIteration> OptimizationHistory { get; set; } = new List<OptimizationIteration>();
        public TimeSpan OptimizationTime { get; set; }
        public int TotalIterations { get; set; }
        public Dictionary<string, object> BestEstimatorParams { get; set; } = new Dictionary<string, object>();
    }

    public class OptimizationIteration
    {
        public int Iteration { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public double Score { get; set; }
        public double ScoreStd { get; set; }
        public TimeSpan IterationTime { get; set; }
        public bool IsBest { get; set; }
    }

    public class CrossValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Algorithm { get; set; } = "";
        public int Folds { get; set; }
        public double[] Scores { get; set; } = Array.Empty<double>();
        public double MeanScore { get; set; }
        public double StdScore { get; set; }
        public string ScoringMetric { get; set; }
        public CrossValidationType CVType { get; set; }
        public TimeSpan EvaluationTime { get; set; }
    }

    public class AlgorithmResult
    {
        public string Algorithm { get; set; } = "";
        public ModelMetrics Metrics { get; set; } = new ModelMetrics();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string ModelId { get; set; } = "";
        public TimeSpan TrainingTime { get; set; }
        public Dictionary<string, object> ValidationScores { get; set; } = new Dictionary<string, object>();
        public int Rank { get; set; }
    }

    public class ModelComparisonResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public AlgorithmResult[] AlgorithmResults { get; set; } = Array.Empty<AlgorithmResult>();
        public string BestAlgorithm { get; set; } = "";
        public ModelMetrics BestMetrics { get; set; } = new ModelMetrics();
        public int CVFolds { get; set; }
        public string ComparisonMetric { get; set; } = "accuracy";
        public DateTime ComparisonDate { get; set; }
        public Dictionary<string, object> ComparisonParameters { get; set; } = new Dictionary<string, object>();
        public StatisticalSignificanceResult SignificanceTest { get; set; }
    }

    public class StatisticalSignificanceResult
    {
        public bool IsSignificant { get; set; }
        public double PValue { get; set; }
        public string TestType { get; set; } = "";
        public Dictionary<string, double> PairwiseComparisons { get; set; } = new Dictionary<string, double>();
        public double CriticalValue { get; set; }
        public double TestStatistic { get; set; }
    }

    public class EnsembleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public EnsembleType EnsembleType { get; set; }
        public string[] Algorithms { get; set; } = Array.Empty<string>();
        public double EnsembleScore { get; set; }
        public double[] BaseModelScores { get; set; } = Array.Empty<double>();
        public int EstimatorCount { get; set; }
        public ModelMetrics Metrics { get; set; } = new ModelMetrics();
        public Dictionary<string, double> IndividualContributions { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, object> EnsembleParameters { get; set; } = new Dictionary<string, object>();
        public string EnsembleId { get; set; }
        public TimeSpan TrainingTime { get; set; }

        public EnsembleResult()
        {
            EnsembleId = System.Guid.NewGuid().ToString();
        }
    }

    public class ComprehensiveEvaluationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ModelName { get; set; } = "";
        public string ModelId { get; set; } = "";
        public ModelType ModelType { get; set; }
        public DateTime EvaluationDate { get; set; }
        
        // Core Metrics
        public ModelMetrics Metrics { get; set; } = new ModelMetrics();
        
        // Classification Specific
        public Dictionary<string, object> ClassificationReport { get; set; } = new Dictionary<string, object>();
        public ConfusionMatrixData ConfusionMatrix { get; set; }
        public ROCData ROCData { get; set; }
        public PrecisionRecallData PrecisionRecallData { get; set; }
        
        // Feature Analysis
        public FeatureImportanceData FeatureImportance { get; set; }
        
        // Performance Analysis
        public LearningCurveData LearningCurve { get; set; }
        public ValidationCurveData ValidationCurve { get; set; }
        
        // Additional Data
        public bool HasProbabilities { get; set; }
        public Dictionary<string, double> CrossValidationScores { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new Dictionary<string, object>();
    }
    #endregion

    #region Workflow and Progress Classes
    public class TrainingStepResult
    {
        public string StepName { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
    }

    public class ComprehensiveTrainingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public AdvancedTrainingConfiguration Configuration { get; set; }
        public string ExperimentId { get; set; }
        
        // Step Results
        public List<TrainingStepResult> Steps { get; set; } = new List<TrainingStepResult>();
        public TrainingStepResult PreprocessingResult { get; set; }
        public TrainingStepResult AlgorithmComparisonResult { get; set; }
        public TrainingStepResult HyperparameterOptimizationResult { get; set; }
        public TrainingStepResult CrossValidationResult { get; set; }
        public TrainingStepResult FinalTrainingResult { get; set; }
        public TrainingStepResult EvaluationResult { get; set; }
        public TrainingStepResult EnsembleResult { get; set; }
        
        // Summary Metrics
        public ModelMetrics FinalMetrics { get; set; } = new ModelMetrics();
        public string BestAlgorithm { get; set; }
        public Dictionary<string, object> BestParameters { get; set; } = new Dictionary<string, object>();
        public double BestCrossValidationScore { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }

    public class MLTrainingProgress
    {
        public string Stage { get; set; } = "";
        public int Progress { get; set; }
        public string Message { get; set; } = "";
        public int CurrentStep { get; set; }
        public int TotalSteps { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedRemaining { get; set; }
        public Dictionary<string, object> StageData { get; set; } = new Dictionary<string, object>();
        public string Status { get; set; } = "Running";
    }

    public class MLOperationStatus
    {
        public string OperationId { get; set; }
        public string OperationType { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Message { get; set; } = "";
        public double Progress { get; set; } = 0.0;
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public MLOperationStatus()
        {
            OperationId = System.Guid.NewGuid().ToString();
        }
    }
    #endregion

    #region Experiment Tracking Classes
    public class ExperimentConfig
    {
        public string ExperimentId { get; set; }
        public string ExperimentName { get; set; } = "";
        public string Description { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>();
        public string DatasetPath { get; set; } = "";
        public string TargetColumn { get; set; } = "";
        public List<string> Metrics { get; set; } = new List<string>();
        public bool LogArtifacts { get; set; } = true;
        public string OutputDirectory { get; set; } = "";

        public ExperimentConfig()
        {
            ExperimentId = System.Guid.NewGuid().ToString();
        }
    }

    public class ExperimentResult
    {
        public string ExperimentId { get; set; } = "";
        public string ExperimentName { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, string> Artifacts { get; set; } = new Dictionary<string, string>();
        public string Status { get; set; } = "Completed";
        public string ModelId { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
    #endregion

    #region Model Management Classes
    public class MLModelInfo
    {
        public string ModelId { get; set; }
        public string ModelName { get; set; } = "";
        public string Algorithm { get; set; } = "";
        public ModelType ModelType { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = "";
        public string Version { get; set; } = "1.0";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public ModelMetrics Metrics { get; set; } = new ModelMetrics();
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public MLModelInfo()
        {
            ModelId = System.Guid.NewGuid().ToString();
        }
    }

    public class ModelLoadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public MLModelInfo ModelInfo { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public bool HasModel { get; set; }
        public bool HasScaler { get; set; }
        public bool HasEncoders { get; set; }
        public bool HasSelector { get; set; }
        public DateTime LoadDate { get; set; }
        public List<string> ComponentTypes { get; set; } = new List<string>();
    }

    public class ModelExplanationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ModelId { get; set; } = "";
        public Dictionary<string, double> GlobalImportances { get; set; } = new Dictionary<string, double>();
        public List<InstanceExplanation> InstanceExplanations { get; set; } = new List<InstanceExplanation>();
        public Dictionary<string, object> SHAPValues { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> LIMEExplanations { get; set; } = new Dictionary<string, object>();
        public string ExplanationMethod { get; set; } = "";
        public DateTime ExplanationDate { get; set; }
    }

    public class InstanceExplanation
    {
        public int InstanceIndex { get; set; }
        public Dictionary<string, object> InputFeatures { get; set; } = new Dictionary<string, object>();
        public object Prediction { get; set; }
        public double PredictionProbability { get; set; }
        public Dictionary<string, double> FeatureContributions { get; set; } = new Dictionary<string, double>();
        public string ExplanationText { get; set; } = "";
    }
    #endregion

    #region Training Result Classes
    public class QuickTrainingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string Algorithm { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TrainingTime { get; set; }
        public double Accuracy { get; set; }
        public double F1Score { get; set; }
        public double RMSE { get; set; }
        public double MAE { get; set; }
        public string Summary { get; set; } = "";
    }

    public class AutoMLResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string TaskType { get; set; } = "";
        public TimeSpan TimeLimit { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan ActualDuration { get; set; }
        public string BestAlgorithm { get; set; } = "";
        public double BestScore { get; set; }
        public Dictionary<string, object> BestParameters { get; set; } = new Dictionary<string, object>();
        public List<Dictionary<string, object>> TrialResults { get; set; } = new List<Dictionary<string, object>>();
        public int TotalTrials { get; set; }
        public string Summary { get; set; } = "";
    }
    #endregion

    #region Advanced Search Classes
    public class HyperparameterSearchResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public SearchType SearchStrategy { get; set; }
        public string Algorithm { get; set; }
        public Dictionary<string, object> BestParameters { get; set; } = new Dictionary<string, object>();
        public double BestScore { get; set; }
        public int TotalEvaluations { get; set; }
        public int MaxEvaluations { get; set; }
        public TimeSpan SearchTime { get; set; }
        public List<EvaluationResult> EvaluationHistory { get; set; } = new List<EvaluationResult>();
        public Dictionary<string, object> SearchMetadata { get; set; } = new Dictionary<string, object>();
    }

    public class EvaluationResult
    {
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public double Score { get; set; }
        public double StandardDeviation { get; set; }
        public TimeSpan EvaluationTime { get; set; }
        public int Iteration { get; set; }
        public bool IsBest { get; set; }
    }
    #endregion

    #region Missing Classes
    public class AdvancedTrainingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public AdvancedTrainingConfiguration Configuration { get; set; }
        public HyperparameterOptimizationResult OptimizationResult { get; set; }
        public CrossValidationResult CrossValidationResult { get; set; }
        public ComprehensiveEvaluationResult ComprehensiveEvaluation { get; set; }
        public bool ModelSaved { get; set; }
        public ModelMetrics FinalMetrics { get; set; } = new ModelMetrics();
    }

    public class TrainingProgressInfo
    {
        public string Stage { get; set; } = "";
        public int Progress { get; set; }
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class AlgorithmComparisonResult
    {
        public string AlgorithmName { get; set; } = "";
        public double CVMeanScore { get; set; }
        public double CVStdScore { get; set; }
        public TimeSpan TrainingTime { get; set; }
    }

    public class OptimizationSearchType
    {
        public const string GridSearch = "GridSearch";
        public const string RandomSearch = "RandomSearch";
        public const string BayesianOptimization = "BayesianOptimization";
    }
    #endregion
}