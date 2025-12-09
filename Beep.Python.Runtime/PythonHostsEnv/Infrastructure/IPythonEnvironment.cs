using System;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Interface for managing Python environment setup and dependencies
/// </summary>
public interface IPythonEnvironment
{
    /// <summary>
    /// Python version string
    /// </summary>
    string? PythonVersion { get; }

    /// <summary>
    /// Path to Python executable
    /// </summary>
    string? PythonExecutablePath { get; }

    /// <summary>
    /// Whether the environment is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Setup the Python environment
    /// </summary>
    Task<bool> Setup(string? pythonPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Install dependencies from requirements file
    /// </summary>
    Task<bool> InstallDependencies(string requirementsPath, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
