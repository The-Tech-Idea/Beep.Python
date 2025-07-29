using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace Beep.Python.Model
{
    #region Model Registry and Management

    /// <summary>
    /// Central registry for managing transformer models across all providers
    /// </summary>
    public interface ITransformerModelRegistry
    {
        Task<bool> RegisterModelAsync(TransformerModelInfo model);
        Task<TransformerModelInfo?> GetModelAsync(string modelId);
        Task<List<TransformerModelInfo>> SearchModelsAsync(ModelSearchCriteria criteria);
        Task<bool> IsModelAvailableAsync(string modelId);
        Task<ModelPerformanceMetrics> GetModelPerformanceAsync(string modelId);
        Task<bool> CacheModelAsync(string modelId);
        Task<bool> RemoveModelFromCacheAsync(string modelId);
        Task<List<string>> GetCachedModelsAsync();
        Task<bool> ValidateModelAsync(string modelId);
    }

    /// <summary>
    /// Model search criteria for filtering models
    /// </summary>
    public class ModelSearchCriteria
    {
        public TransformerModelSource? Source { get; set; }
        public TransformerTask? TaskType { get; set; }
        public string? SearchTerm { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? Language { get; set; }
        public ModelSize? MinSize { get; set; }
        public ModelSize? MaxSize { get; set; }
        public bool RequiresAuth { get; set; } = false;
        public string? License { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public int? MinDownloads { get; set; }
        public double? MinRating { get; set; }
    }

    /// <summary>
    /// Model performance metrics and statistics
    /// </summary>
    public class ModelPerformanceMetrics
    {
        public string ModelId { get; set; } = string.Empty;
        public double AverageLatency { get; set; }
        public double TokensPerSecond { get; set; }
        public double AccuracyScore { get; set; }
        public int UsageCount { get; set; }
        public DateTime LastUsed { get; set; }
        public Dictionary<string, double> TaskSpecificMetrics { get; set; } = new();
        public List<UserRating> UserRatings { get; set; } = new();
        public ModelResourceUsage ResourceUsage { get; set; } = new();
    }

    /// <summary>
    /// Model resource usage information
    /// </summary>
    public class ModelResourceUsage
    {
        public long MemoryUsageMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public double GpuUsagePercent { get; set; }
        public long DiskSpaceMB { get; set; }
        public TimeSpan LoadTime { get; set; }
    }

    /// <summary>
    /// User rating for models
    /// </summary>
    public class UserRating
    {
        public string UserId { get; set; } = string.Empty;
        public int Rating { get; set; } // 1-5 stars
        public string? Comment { get; set; }
        public DateTime RatedAt { get; set; }
        public TransformerTask TaskType { get; set; }
    }

    public enum ModelSize
    {
        Tiny,       // < 100M parameters
        Small,      // 100M - 1B parameters  
        Medium,     // 1B - 10B parameters
        Large,      // 10B - 100B parameters
        XLarge      // > 100B parameters
    }

    #endregion

    #region Advanced Pipeline Management

    /// <summary>
    /// Pipeline orchestrator for managing multiple transformer pipelines
    /// </summary>
    public interface ITransformerOrchestrator
    {
        Task<string> ExecuteWorkflowAsync(TransformerWorkflow workflow);
        Task<bool> RegisterWorkflowAsync(TransformerWorkflow workflow);
        Task<List<TransformerWorkflow>> GetAvailableWorkflowsAsync();
        Task<WorkflowExecutionResult> GetExecutionStatusAsync(string executionId);
        Task<bool> CancelExecutionAsync(string executionId);
        Task<List<WorkflowExecutionResult>> GetExecutionHistoryAsync(string workflowId);
    }

    /// <summary>
    /// Transformer workflow definition
    /// </summary>
    public class TransformerWorkflow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<WorkflowStep> Steps { get; set; } = new();
        public WorkflowTrigger Trigger { get; set; } = new();
        public Dictionary<string, object> GlobalParameters { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Workflow trigger configuration
    /// </summary>
    public class WorkflowTrigger
    {
        public TriggerType Type { get; set; } = TriggerType.Manual;
        public string CronExpression { get; set; } = string.Empty;
        public Dictionary<string, object> EventFilters { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Individual step in a transformer workflow
    /// </summary>
    public class WorkflowStep
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public TransformerTask TaskType { get; set; }
        public TransformerModelSource ModelSource { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> DependsOn { get; set; } = new(); // Step IDs this depends on
        public Dictionary<string, string> InputMappings { get; set; } = new(); // Map outputs from previous steps
        public StepCondition? Condition { get; set; } // Optional execution condition
        public int MaxRetries { get; set; } = 3;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Step execution condition
    /// </summary>
    public class StepCondition
    {
        public string Expression { get; set; } = string.Empty;
        public ConditionType Type { get; set; } = ConditionType.Expression;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Workflow execution result
    /// </summary>
    public class WorkflowExecutionResult
    {
        public string ExecutionId { get; set; } = string.Empty;
        public string WorkflowId { get; set; } = string.Empty;
        public WorkflowStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Results { get; set; } = new();
        public List<StepExecutionResult> StepResults { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Step execution result
    /// </summary>
    public class StepExecutionResult
    {
        public string StepId { get; set; } = string.Empty;
        public string StepName { get; set; } = string.Empty;
        public StepStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public object? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public int Attempts { get; set; } = 1;
        public TimeSpan Duration { get; set; }
    }

    public enum TriggerType
    {
        Manual,
        Scheduled,
        Event,
        Webhook,
        FileWatcher
    }

    public enum ConditionType
    {
        Expression,
        PreviousStepSuccess,
        PreviousStepFailed,
        DataAvailable,
        TimeWindow
    }

    public enum StepStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped,
        Cancelled
    }

    public enum WorkflowStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled,
        Paused
    }

    #endregion

    #region Model Fine-tuning and Training

    /// <summary>
    /// Interface for fine-tuning transformer models
    /// </summary>
    public interface ITransformerFineTuner
    {
        Task<string> StartFineTuningAsync(FineTuningRequest request);
        Task<FineTuningStatus> GetFineTuningStatusAsync(string jobId);
        Task<bool> CancelFineTuningAsync(string jobId);
        Task<TransformerModelInfo> GetFineTunedModelAsync(string jobId);
        Task<List<FineTuningJob>> GetFineTuningHistoryAsync(string userId);
        Task<bool> ValidateTrainingDataAsync(string dataPath);
    }

    /// <summary>
    /// Fine-tuning status information
    /// </summary>
    public class FineTuningStatus
    {
        public string JobId { get; set; } = string.Empty;
        public FineTuningState State { get; set; }
        public int CurrentEpoch { get; set; }
        public int TotalEpochs { get; set; }
        public double Progress { get; set; }
        public TrainingMetrics? CurrentMetrics { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan EstimatedRemainingTime { get; set; }
    }

    /// <summary>
    /// Fine-tuning job information
    /// </summary>
    public class FineTuningJob
    {
        public string JobId { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string BaseModelId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public FineTuningState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ResultModelId { get; set; }
        public TrainingMetrics? FinalMetrics { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Training metrics
    /// </summary>
    public class TrainingMetrics
    {
        public double Loss { get; set; }
        public double ValidationLoss { get; set; }
        public double Accuracy { get; set; }
        public double LearningRate { get; set; }
        public int Step { get; set; }
        public int Epoch { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Fine-tuning request configuration
    /// </summary>
    public class FineTuningRequest
    {
        public string BaseModelId { get; set; } = string.Empty;
        public string TrainingDataPath { get; set; } = string.Empty;
        public string? ValidationDataPath { get; set; }
        public FineTuningParameters Parameters { get; set; } = new();
        public string JobName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Fine-tuning parameters
    /// </summary>
    public class FineTuningParameters
    {
        public int Epochs { get; set; } = 3;
        public double LearningRate { get; set; } = 0.0001;
        public int BatchSize { get; set; } = 4;
        public double WarmupRatio { get; set; } = 0.1;
        public string OptimizerType { get; set; } = "adamw";
        public double WeightDecay { get; set; } = 0.01;
        public int SaveSteps { get; set; } = 500;
        public int EvalSteps { get; set; } = 500;
        public bool UseGradientCheckpointing { get; set; } = true;
        public Dictionary<string, object> CustomParameters { get; set; } = new();
    }

    public enum FineTuningState
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled,
        Paused
    }

    #endregion

    #region Caching and Performance Optimization

    /// <summary>
    /// Intelligent caching system for transformer results
    /// </summary>
    public interface ITransformerCache
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task ClearAsync();
        Task<CacheStatistics> GetStatisticsAsync();
        Task<List<string>> GetKeysAsync(string pattern);
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStatistics
    {
        public long TotalKeys { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public double HitRatio => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0;
        public long TotalRequests => HitCount + MissCount;
        public long MemoryUsageBytes { get; set; }
        public TimeSpan AverageRetrievalTime { get; set; }
    }

    /// <summary>
    /// Performance optimizer for transformer pipelines
    /// </summary>
    public interface ITransformerOptimizer
    {
        Task<OptimizationRecommendations> AnalyzePerformanceAsync(string pipelineId);
        Task<bool> ApplyOptimizationsAsync(string pipelineId, List<OptimizationType> optimizations);
        Task<PerformanceBenchmark> BenchmarkModelAsync(string modelId, BenchmarkConfig config);
        Task<List<ModelAlternative>> SuggestModelAlternativesAsync(string modelId, OptimizationGoal goal);
    }

    /// <summary>
    /// Optimization type enumeration
    /// </summary>
    public enum OptimizationType
    {
        ModelQuantization,
        BatchSizeOptimization,
        CacheConfiguration,
        ModelSharding,
        PipelineParallelism,
        GradientCheckpointing,
        MixedPrecision,
        KernelOptimization,
        MemoryMapping,
        ConnectionPooling
    }

    /// <summary>
    /// Benchmark configuration
    /// </summary>
    public class BenchmarkConfig
    {
        public int WarmupRuns { get; set; } = 5;
        public int BenchmarkRuns { get; set; } = 20;
        public List<string> TestInputs { get; set; } = new();
        public Dictionary<string, object> ModelParameters { get; set; } = new();
        public bool MeasureMemory { get; set; } = true;
        public bool MeasureLatency { get; set; } = true;
        public bool MeasureThroughput { get; set; } = true;
    }

    /// <summary>
    /// Performance benchmark results
    /// </summary>
    public class PerformanceBenchmark
    {
        public string ModelId { get; set; } = string.Empty;
        public DateTime BenchmarkedAt { get; set; }
        public TimeSpan AverageLatency { get; set; }
        public double TokensPerSecond { get; set; }
        public long MemoryUsageBytes { get; set; }
        public double CpuUtilization { get; set; }
        public double GpuUtilization { get; set; }
        public Dictionary<string, double> DetailedMetrics { get; set; } = new();
        public BenchmarkConfig Configuration { get; set; } = new();
    }

    /// <summary>
    /// Model alternative suggestion
    /// </summary>
    public class ModelAlternative
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public TransformerModelSource Source { get; set; }
        public double PerformanceScore { get; set; }
        public double CostScore { get; set; }
        public double QualityScore { get; set; }
        public double OverallScore { get; set; }
        public List<string> Pros { get; set; } = new();
        public List<string> Cons { get; set; } = new();
        public string RecommendationReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Optimization recommendations
    /// </summary>
    public class OptimizationRecommendations
    {
        public string PipelineId { get; set; } = string.Empty;
        public List<OptimizationSuggestion> Suggestions { get; set; } = new();
        public PerformanceProfile CurrentProfile { get; set; } = new();
        public PerformanceProfile ProjectedProfile { get; set; } = new();
        public double EstimatedSpeedupPercent { get; set; }
        public double EstimatedCostReductionPercent { get; set; }
    }

    /// <summary>
    /// Optimization suggestion
    /// </summary>
    public class OptimizationSuggestion
    {
        public OptimizationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double EstimatedImpactPercent { get; set; }
        public ImplementationComplexity Complexity { get; set; }
        public List<string> Steps { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> Risks { get; set; } = new();
    }

    /// <summary>
    /// Performance profile
    /// </summary>
    public class PerformanceProfile
    {
        public double AverageLatency { get; set; }
        public double P95Latency { get; set; }
        public double TokensPerSecond { get; set; }
        public double MemoryUsageGB { get; set; }
        public double CpuUtilization { get; set; }
        public double GpuUtilization { get; set; }
        public decimal CostPerThousandTokens { get; set; }
        public double QualityScore { get; set; }
    }

    public enum ImplementationComplexity
    {
        Low,
        Medium,
        High,
        Expert
    }

    public enum OptimizationGoal
    {
        Speed,
        Accuracy,
        CostEfficiency,
        MemoryUsage,
        Balanced
    }

    #endregion
}