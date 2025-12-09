using System;
using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Result of a Python script or code execution
/// </summary>
public class PythonExecutionResult
{
    /// <summary>
    /// Whether the execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Standard output from the execution
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Standard error from the execution
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Return value from the Python execution
    /// </summary>
    public object? ReturnValue { get; set; }

    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Exit code (if applicable)
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Additional execution metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
