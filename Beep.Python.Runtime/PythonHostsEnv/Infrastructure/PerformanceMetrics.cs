using System;
using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Performance metrics for model inference
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Time taken for model initialization
    /// </summary>
    public TimeSpan InitializationTime { get; set; }

    /// <summary>
    /// Time taken for first token generation
    /// </summary>
    public TimeSpan TimeToFirstToken { get; set; }

    /// <summary>
    /// Total inference time
    /// </summary>
    public TimeSpan TotalInferenceTime { get; set; }

    /// <summary>
    /// Number of tokens generated
    /// </summary>
    public int TokensGenerated { get; set; }

    /// <summary>
    /// Tokens per second
    /// </summary>
    public double TokensPerSecond => TokensGenerated / TotalInferenceTime.TotalSeconds;

    /// <summary>
    /// Peak memory usage in bytes
    /// </summary>
    public long PeakMemoryBytes { get; set; }

    /// <summary>
    /// Average memory usage in bytes
    /// </summary>
    public long AverageMemoryBytes { get; set; }

    /// <summary>
    /// GPU utilization percentage (0-100)
    /// </summary>
    public double? GpuUtilization { get; set; }

    /// <summary>
    /// Additional performance data
    /// </summary>
    public Dictionary<string, object>? AdditionalMetrics { get; set; }
}
