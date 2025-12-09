using System;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Progress information for model downloads
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// Model ID being downloaded
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Current file being downloaded
    /// </summary>
    public string? CurrentFile { get; set; }

    /// <summary>
    /// Total bytes to download
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Bytes downloaded so far
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// Download percentage (0-100)
    /// </summary>
    public double Percentage => TotalBytes > 0 ? (DownloadedBytes * 100.0 / TotalBytes) : 0;

    /// <summary>
    /// Download speed in bytes per second
    /// </summary>
    public long BytesPerSecond { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Current status message
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Whether the download is complete
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Whether the download encountered an error
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Error message if HasError is true
    /// </summary>
    public string? ErrorMessage { get; set; }
}
