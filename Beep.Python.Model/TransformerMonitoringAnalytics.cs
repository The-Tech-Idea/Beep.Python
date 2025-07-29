using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    #region Real-time Monitoring

    /// <summary>
    /// Real-time monitoring and observability for transformer pipelines
    /// </summary>
    public interface ITransformerMonitoring
    {
        Task<MonitoringDashboard> GetDashboardAsync();
        Task<List<AlertRule>> GetActiveAlertsAsync();
        Task<bool> CreateAlertRuleAsync(AlertRule rule);
        Task<SystemHealth> GetSystemHealthAsync();
        Task<List<PerformanceMetric>> GetMetricsAsync(string pipelineId, TimeSpan timeRange);
        Task<UsageAnalytics> GetUsageAnalyticsAsync(DateTime startDate, DateTime endDate);
        Task LogEventAsync(TransformerEvent eventData);
        Task<List<TransformerEvent>> GetEventHistoryAsync(string pipelineId, TimeSpan timeRange);
    }

    /// <summary>
    /// System health status
    /// </summary>
    public class SystemHealth
    {
        public HealthStatus Status { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public int ActiveConnections { get; set; }
        public double ResponseTime { get; set; }
        public List<string> Issues { get; set; } = new();
        public DateTime LastCheck { get; set; }
    }

    /// <summary>
    /// Performance metric data point
    /// </summary>
    public class PerformanceMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
        public string PipelineId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Transformer event for logging and monitoring
    /// </summary>
    public class TransformerEvent
    {
        public string Id { get; set; } = string.Empty;
        public string PipelineId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public EventSeverity Severity { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Monitoring dashboard data
    /// </summary>
    public class MonitoringDashboard
    {
        public SystemOverview SystemOverview { get; set; } = new();
        public List<PipelineStatus> ActivePipelines { get; set; } = new();
        public List<RecentAlert> RecentAlerts { get; set; } = new();
        public PerformanceSnapshot PerformanceSnapshot { get; set; } = new();
        public ResourceUtilization ResourceUtilization { get; set; } = new();
        public List<TopModel> TopPerformingModels { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Pipeline status information
    /// </summary>
    public class PipelineStatus
    {
        public string PipelineId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public PipelineState State { get; set; }
        public DateTime LastActivity { get; set; }
        public int RequestsPerMinute { get; set; }
        public double SuccessRate { get; set; }
        public double AverageLatency { get; set; }
        public List<string> ActiveModels { get; set; } = new();
    }

    /// <summary>
    /// Recent alert information
    /// </summary>
    public class RecentAlert
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public DateTime TriggeredAt { get; set; }
        public string PipelineId { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    /// <summary>
    /// Performance snapshot
    /// </summary>
    public class PerformanceSnapshot
    {
        public double OverallLatency { get; set; }
        public double OverallThroughput { get; set; }
        public double OverallErrorRate { get; set; }
        public DateTime SnapshotTime { get; set; }
        public Dictionary<string, double> ModelLatencies { get; set; } = new();
        public Dictionary<string, double> ModelThroughputs { get; set; } = new();
    }

    /// <summary>
    /// Resource utilization metrics
    /// </summary>
    public class ResourceUtilization
    {
        public double CpuUtilization { get; set; }
        public double MemoryUtilization { get; set; }
        public double GpuUtilization { get; set; }
        public double NetworkUtilization { get; set; }
        public double StorageUtilization { get; set; }
        public DateTime MeasuredAt { get; set; }
        public Dictionary<string, double> DetailedMetrics { get; set; } = new();
    }

    /// <summary>
    /// Top performing model information
    /// </summary>
    public class TopModel
    {
        public string ModelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public double AverageLatency { get; set; }
        public double SuccessRate { get; set; }
        public double QualityScore { get; set; }
        public decimal Cost { get; set; }
    }

    /// <summary>
    /// System overview metrics
    /// </summary>
    public class SystemOverview
    {
        public int TotalPipelines { get; set; }
        public int ActivePipelines { get; set; }
        public int TotalRequests24h { get; set; }
        public double AverageLatency { get; set; }
        public double SuccessRate { get; set; }
        public double ErrorRate { get; set; }
        public long TotalTokensProcessed { get; set; }
        public decimal EstimatedCosts24h { get; set; }
    }

    /// <summary>
    /// Alert rule configuration
    /// </summary>
    public class AlertRule
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AlertCondition Condition { get; set; } = new();
        public AlertSeverity Severity { get; set; }
        public List<NotificationTarget> NotificationTargets { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(15);
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Notification target for alerts
    /// </summary>
    public class NotificationTarget
    {
        public string Id { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Address { get; set; } = string.Empty; // Email, URL, phone number
        public Dictionary<string, string> Settings { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Alert condition definition
    /// </summary>
    public class AlertCondition
    {
        public string MetricName { get; set; } = string.Empty;
        public ComparisonOperator Operator { get; set; }
        public double Threshold { get; set; }
        public TimeSpan EvaluationPeriod { get; set; } = TimeSpan.FromMinutes(5);
        public int ConsecutiveFailures { get; set; } = 1;
        public string? Filter { get; set; } // Optional filter expression
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    public enum EventSeverity
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public enum PipelineState
    {
        Starting,
        Running,
        Paused,
        Stopped,
        Error,
        Maintenance
    }

    public enum NotificationType
    {
        Email,
        Webhook,
        SMS,
        Slack,
        Teams
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical,
        Emergency
    }

    public enum ComparisonOperator
    {
        GreaterThan,
        LessThan,
        Equals,
        NotEquals,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

    #endregion

    #region Analytics and Insights

    /// <summary>
    /// Advanced analytics and insights for transformer usage
    /// </summary>
    public interface ITransformerAnalytics
    {
        Task<UsageReport> GenerateUsageReportAsync(ReportRequest request);
        Task<CostAnalysis> GetCostAnalysisAsync(DateTime startDate, DateTime endDate);
        Task<List<UsageTrend>> GetUsageTrendsAsync(string pipelineId, TimeSpan period);
        Task<PerformanceComparison> CompareModelsAsync(List<string> modelIds, ComparisonMetrics metrics);
        Task<List<Insight>> GetInsightsAsync(string pipelineId);
        Task<PredictiveAnalysis> GetPredictiveAnalysisAsync(string pipelineId);
        Task<ROIAnalysis> CalculateROIAsync(string pipelineId, DateTime startDate, DateTime endDate);
    }

    /// <summary>
    /// Usage report data
    /// </summary>
    public class UsageReport
    {
        public string ReportId { get; set; } = string.Empty;
        public ReportRequest Request { get; set; } = new();
        public UsageAnalytics Analytics { get; set; } = new();
        public List<UsageTrend> Trends { get; set; } = new();
        public CostAnalysis CostAnalysis { get; set; } = new();
        public List<Insight> Insights { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Report request configuration
    /// </summary>
    public class ReportRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> PipelineIds { get; set; } = new();
        public List<string> ModelIds { get; set; } = new();
        public List<string> UserIds { get; set; } = new();
        public ReportFormat Format { get; set; } = ReportFormat.Json;
        public bool IncludeTrends { get; set; } = true;
        public bool IncludeCostAnalysis { get; set; } = true;
        public bool IncludeInsights { get; set; } = true;
    }

    /// <summary>
    /// Usage trend data
    /// </summary>
    public class UsageTrend
    {
        public DateTime Date { get; set; }
        public int RequestCount { get; set; }
        public long TokenCount { get; set; }
        public decimal Cost { get; set; }
        public double AverageLatency { get; set; }
        public double ErrorRate { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Comparison metrics configuration
    /// </summary>
    public class ComparisonMetrics
    {
        public List<string> MetricNames { get; set; } = new();
        public ComparisonPeriod Period { get; set; } = new();
        public List<string> Dimensions { get; set; } = new();
        public AggregationMethod Aggregation { get; set; } = AggregationMethod.Average;
    }

    /// <summary>
    /// Comparison period definition
    /// </summary>
    public class ComparisonPeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan Granularity { get; set; } = TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Predictive analysis results
    /// </summary>
    public class PredictiveAnalysis
    {
        public string PipelineId { get; set; } = string.Empty;
        public List<UsageForecast> UsageForecasts { get; set; } = new();
        public List<CostForecast> CostForecasts { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public double ConfidenceLevel { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Usage forecast
    /// </summary>
    public class UsageForecast
    {
        public DateTime Date { get; set; }
        public int PredictedRequests { get; set; }
        public long PredictedTokens { get; set; }
        public double ConfidenceInterval { get; set; }
    }

    /// <summary>
    /// Cost forecast
    /// </summary>
    public class CostForecast
    {
        public DateTime Date { get; set; }
        public decimal PredictedCost { get; set; }
        public double ConfidenceInterval { get; set; }
    }

    /// <summary>
    /// ROI analysis results
    /// </summary>
    public class ROIAnalysis
    {
        public string PipelineId { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public decimal EstimatedValue { get; set; }
        public double ROIPercentage { get; set; }
        public TimeSpan PaybackPeriod { get; set; }
        public List<string> ValueDrivers { get; set; } = new();
        public List<string> CostDrivers { get; set; } = new();
        public DateTime AnalysisDate { get; set; }
    }

    /// <summary>
    /// Usage analytics data
    /// </summary>
    public class UsageAnalytics
    {
        public TimeSpan Period { get; set; }
        public int TotalRequests { get; set; }
        public int UniqueUsers { get; set; }
        public Dictionary<TransformerTask, int> RequestsByTask { get; set; } = new();
        public Dictionary<TransformerModelSource, int> RequestsByProvider { get; set; } = new();
        public Dictionary<string, int> TopModels { get; set; } = new();
        public Dictionary<string, int> TopUsers { get; set; } = new();
        public List<HourlyUsage> HourlyBreakdown { get; set; } = new();
        public double AverageTokensPerRequest { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// Hourly usage breakdown
    /// </summary>
    public class HourlyUsage
    {
        public DateTime Hour { get; set; }
        public int RequestCount { get; set; }
        public long TokenCount { get; set; }
        public decimal Cost { get; set; }
        public double AverageLatency { get; set; }
        public double ErrorRate { get; set; }
    }

    /// <summary>
    /// Cost analysis breakdown
    /// </summary>
    public class CostAnalysis
    {
        public decimal TotalCost { get; set; }
        public Dictionary<TransformerModelSource, decimal> CostByProvider { get; set; } = new();
        public Dictionary<string, decimal> CostByModel { get; set; } = new();
        public Dictionary<string, decimal> CostByUser { get; set; } = new();
        public List<CostTrend> DailyTrends { get; set; } = new();
        public decimal ProjectedMonthlyCost { get; set; }
        public List<CostOptimizationSuggestion> OptimizationSuggestions { get; set; } = new();
    }

    /// <summary>
    /// Cost trend data point
    /// </summary>
    public class CostTrend
    {
        public DateTime Date { get; set; }
        public decimal Cost { get; set; }
        public decimal BudgetVariance { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cost optimization suggestion
    /// </summary>
    public class CostOptimizationSuggestion
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialSavings { get; set; }
        public double ImplementationComplexity { get; set; }
        public List<string> ActionItems { get; set; } = new();
        public string Impact { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance comparison between models
    /// </summary>
    public class PerformanceComparison
    {
        public List<string> ModelIds { get; set; } = new();
        public Dictionary<string, PerformanceMetric> Latency { get; set; } = new();
        public Dictionary<string, PerformanceMetric> Throughput { get; set; } = new();
        public Dictionary<string, PerformanceMetric> ErrorRate { get; set; } = new();
        public Dictionary<string, PerformanceMetric> Cost { get; set; } = new();
        public Dictionary<string, PerformanceMetric> Quality { get; set; } = new();
        public string RecommendedModel { get; set; } = string.Empty;
        public string RecommendationReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Insights and recommendations
    /// </summary>
    public class Insight
    {
        public string Id { get; set; } = string.Empty;
        public InsightType Type { get; set; }
        public InsightSeverity Severity { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public bool IsActionable { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public enum ReportFormat
    {
        Json,
        Csv,
        Excel,
        Pdf
    }

    public enum AggregationMethod
    {
        Average,
        Sum,
        Count,
        Min,
        Max,
        Median,
        Percentile95
    }

    public enum InsightType
    {
        Performance,
        Cost,
        Usage,
        Quality,
        Security,
        Optimization
    }

    public enum InsightSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion

    #region A/B Testing and Experimentation

    /// <summary>
    /// A/B testing framework for transformer models
    /// </summary>
    public interface ITransformerExperimentation
    {
        Task<string> CreateExperimentAsync(ExperimentConfig config);
        Task<ExperimentResult> GetExperimentResultAsync(string experimentId);
        Task<bool> StartExperimentAsync(string experimentId);
        Task<bool> StopExperimentAsync(string experimentId);
        Task<List<Experiment>> GetActiveExperimentsAsync();
        Task<StatisticalSignificance> CheckSignificanceAsync(string experimentId);
        Task<ExperimentRecommendation> GetRecommendationAsync(string experimentId);
    }

    /// <summary>
    /// Experiment result data
    /// </summary>
    public class ExperimentResult
    {
        public string ExperimentId { get; set; } = string.Empty;
        public ExperimentStatus Status { get; set; }
        public List<VariantResult> VariantResults { get; set; } = new();
        public StatisticalSignificance Significance { get; set; } = new();
        public string WinningVariant { get; set; } = string.Empty;
        public double ConfidenceLevel { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Experiment information
    /// </summary>
    public class Experiment
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ExperimentStatus Status { get; set; }
        public List<ExperimentVariant> Variants { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistical significance test results
    /// </summary>
    public class StatisticalSignificance
    {
        public bool IsSignificant { get; set; }
        public double PValue { get; set; }
        public double ConfidenceLevel { get; set; }
        public string TestMethod { get; set; } = string.Empty;
        public Dictionary<string, double> EffectSizes { get; set; } = new();
        public string Interpretation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Experiment recommendation
    /// </summary>
    public class ExperimentRecommendation
    {
        public string ExperimentId { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public string RecommendedVariant { get; set; } = string.Empty;
        public List<string> Reasons { get; set; } = new();
        public double Confidence { get; set; }
        public List<string> NextSteps { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Variant result data
    /// </summary>
    public class VariantResult
    {
        public string VariantId { get; set; } = string.Empty;
        public int SampleSize { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new();
        public double ConversionRate { get; set; }
        public double ConfidenceInterval { get; set; }
    }

    /// <summary>
    /// Experiment configuration
    /// </summary>
    public class ExperimentConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ExperimentVariant> Variants { get; set; } = new();
        public TrafficSplit TrafficSplit { get; set; } = new();
        public List<ExperimentMetric> Metrics { get; set; } = new();
        public ExperimentDuration Duration { get; set; } = new();
        public string CreatedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Experiment metric definition
    /// </summary>
    public class ExperimentMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MetricType Type { get; set; }
        public bool IsPrimary { get; set; }
        public double MinDetectableEffect { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// Experiment duration configuration
    /// </summary>
    public class ExperimentDuration
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MinSampleSize { get; set; }
        public double RequiredConfidenceLevel { get; set; } = 0.95;
        public bool AutoStop { get; set; } = true;
    }

    /// <summary>
    /// Experiment variant (control or treatment)
    /// </summary>
    public class ExperimentVariant
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool IsControl { get; set; }
        public double TrafficPercentage { get; set; }
    }

    /// <summary>
    /// Traffic split configuration
    /// </summary>
    public class TrafficSplit
    {
        public SplitMethod Method { get; set; } = SplitMethod.Random;
        public string? SplitKey { get; set; } // User ID, session ID, etc.
        public List<SplitRule> Rules { get; set; } = new();
    }

    /// <summary>
    /// Split rule for traffic allocation
    /// </summary>
    public class SplitRule
    {
        public string Condition { get; set; } = string.Empty;
        public string VariantId { get; set; } = string.Empty;
        public double Weight { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public enum ExperimentStatus
    {
        Draft,
        Running,
        Paused,
        Completed,
        Cancelled
    }

    public enum MetricType
    {
        Counter,
        Gauge,
        Histogram,
        Rate,
        Boolean
    }

    public enum SplitMethod
    {
        Random,
        UserBased,
        SessionBased,
        GeographyBased,
        Custom
    }

    #endregion
}