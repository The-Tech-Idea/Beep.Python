using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using TheTechIdea.Beep.ConfigUtil;
 
//using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine.Monitoring
{
    /// <summary>
    /// Monitors health and status of Python runtimes and environments.
    /// Performs periodic checks and diagnostics to ensure environments remain functional.
    /// </summary>
    public class PythonHealthMonitor : IPythonHealthMonitor
    {
        private readonly IPythonRuntimeRegistry _registry;
       
       
        private Timer _healthCheckTimer;
        private readonly object _lock = new object();

        public PythonHealthMonitor(
            IPythonRuntimeRegistry registry          )
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
             
            
        }

        /// <summary>
        /// Starts periodic health monitoring of all registered runtimes.
        /// </summary>
        /// <param name="intervalMinutes">Check interval in minutes (default: 30)</param>
        public void StartMonitoring(int intervalMinutes = 30)
        {
            lock (_lock)
            {
                StopMonitoring();

                var interval = TimeSpan.FromMinutes(intervalMinutes);
                _healthCheckTimer = new Timer(
                    async _ => await PerformHealthCheckAsync(),
                    null,
                    TimeSpan.Zero,
                    interval);

               Messaging.AddLogMessage("Beep", $"🔍 Health monitoring started (interval: {intervalMinutes} minutes)", DateTime.Now, 0, null, Errors.Ok);
            }
        }

        /// <summary>
        /// Stops periodic health monitoring.
        /// </summary>
        public void StopMonitoring()
        {
            lock (_lock)
            {
                _healthCheckTimer?.Dispose();
                _healthCheckTimer = null;
               Messaging.AddLogMessage("Beep", "Health monitoring stopped", DateTime.Now, 0, null, Errors.Ok);
            }
        }

        /// <summary>
        /// Performs an immediate health check on all registered runtimes.
        /// </summary>
        public async Task<HealthCheckReport> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
        {
            var report = new HealthCheckReport
            {
                Timestamp = DateTime.UtcNow,
                RuntimeChecks = new List<RuntimeHealthCheck>()
            };

            try
            {
                var runtimes = _registry.GetAvailableRuntimes();

                foreach (var runtime in runtimes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var check = await CheckRuntimeHealthAsync(runtime, cancellationToken);
                    report.RuntimeChecks.Add(check);
                }

                // Calculate overall health
                var totalRuntimes = report.RuntimeChecks.Count;
                var healthyRuntimes = report.RuntimeChecks.Count(r => r.IsHealthy);
                var degradedRuntimes = report.RuntimeChecks.Count(r => r.Status == HealthStatus.Degraded);

                report.OverallHealth = totalRuntimes == 0 ? HealthStatus.Unknown :
                                      healthyRuntimes == totalRuntimes ? HealthStatus.Healthy :
                                      degradedRuntimes > 0 ? HealthStatus.Degraded :
                                      HealthStatus.Unhealthy;

                report.Summary = $"{healthyRuntimes}/{totalRuntimes} runtimes healthy";

               Messaging.AddLogMessage("Beep", $"Health check complete: {report.Summary}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
               Messaging.AddLogMessage("Beep", $"Health check failed: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                report.OverallHealth = HealthStatus.Unknown;
                report.Summary = $"Health check failed: {ex.Message}";
            }

            return report;
        }

        /// <summary>
        /// Checks health of a specific runtime.
        /// </summary>
        public async Task<RuntimeHealthCheck> CheckRuntimeHealthAsync(
            PythonRuntimeInfo runtime,
            CancellationToken cancellationToken = default)
        {
            var check = new RuntimeHealthCheck
            {
                RuntimeId = runtime.Id,
                RuntimeName = runtime.Name,
                RuntimePath = runtime.Path,
                CheckTime = DateTime.UtcNow,
                Issues = new List<string>()
            };

            try
            {
                // Check 1: Path exists
                if (!Directory.Exists(runtime.Path) && !File.Exists(runtime.Path))
                {
                    check.Issues.Add("Runtime path not found");
                    check.Status = HealthStatus.Unhealthy;
                    check.IsHealthy = false;
                    return check;
                }

                // Check 2: Python executable exists
                var pythonExe = FindPythonExecutable(runtime.Path);
                if (pythonExe == null)
                {
                    check.Issues.Add("Python executable not found");
                    check.Status = HealthStatus.Unhealthy;
                    check.IsHealthy = false;
                    return check;
                }

                // Check 3: Python version can be retrieved
                var version = await GetPythonVersionAsync(pythonExe, cancellationToken);
                if (string.IsNullOrEmpty(version))
                {
                    check.Issues.Add("Cannot retrieve Python version");
                    check.Status = HealthStatus.Degraded;
                }
                else
                {
                    check.PythonVersion = version;
                }

                // Check 4: pip availability
                var pipAvailable = await CheckPipAvailabilityAsync(pythonExe, cancellationToken);
                if (!pipAvailable)
                {
                    check.Issues.Add("pip not available");
                    check.Status = HealthStatus.Degraded;
                }

                // Check 5: Can execute simple code
                var canExecute = await CanExecuteCodeAsync(pythonExe, cancellationToken);
                if (!canExecute)
                {
                    check.Issues.Add("Cannot execute Python code");
                    check.Status = HealthStatus.Unhealthy;
                    check.IsHealthy = false;
                    return check;
                }

                // Determine final status
                if (check.Issues.Count == 0)
                {
                    check.Status = HealthStatus.Healthy;
                    check.IsHealthy = true;
                }
                else if (check.Status != HealthStatus.Unhealthy)
                {
                    check.Status = HealthStatus.Degraded;
                    check.IsHealthy = true; // Still usable but with issues
                }

                check.LastSuccessfulCheck = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                check.Issues.Add($"Health check exception: {ex.Message}");
                check.Status = HealthStatus.Unknown;
                check.IsHealthy = false;
            }

            return check;
        }

        /// <summary>
        /// Finds Python executable in a given path.
        /// </summary>
        private string FindPythonExecutable(string basePath)
        {
            var possiblePaths = new[]
            {
                Path.Combine(basePath, "python.exe"),
                Path.Combine(basePath, "Scripts", "python.exe"),
                Path.Combine(basePath, "bin", "python.exe"),
                Path.Combine(basePath, "python3.exe")
            };

            return possiblePaths.FirstOrDefault(File.Exists);
        }

        /// <summary>
        /// Gets Python version from executable.
        /// </summary>
        private async Task<string> GetPythonVersionAsync(string pythonExe, CancellationToken cancellationToken)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                return !string.IsNullOrEmpty(output) ? output.Trim() : error.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if pip is available.
        /// </summary>
        private async Task<bool> CheckPipAvailabilityAsync(string pythonExe, CancellationToken cancellationToken)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = "-m pip --version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync(cancellationToken);

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if Python can execute simple code.
        /// </summary>
        private async Task<bool> CanExecuteCodeAsync(string pythonExe, CancellationToken cancellationToken)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = "-c \"print('test')\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                return process.ExitCode == 0 && output.Trim() == "test";
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }

    #region Supporting Classes

    /// <summary>
    /// Interface for health monitoring.
    /// </summary>
    public interface IPythonHealthMonitor : IDisposable
    {
        void StartMonitoring(int intervalMinutes = 30);
        void StopMonitoring();
        Task<HealthCheckReport> PerformHealthCheckAsync(CancellationToken cancellationToken = default);
        Task<RuntimeHealthCheck> CheckRuntimeHealthAsync(PythonRuntimeInfo runtime, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Overall health check report.
    /// </summary>
    public class HealthCheckReport
    {
        public DateTime Timestamp { get; set; }
        public HealthStatus OverallHealth { get; set; }
        public string Summary { get; set; }
        public List<RuntimeHealthCheck> RuntimeChecks { get; set; } = new List<RuntimeHealthCheck>();
    }

    /// <summary>
    /// Health check for individual runtime.
    /// </summary>
    public class RuntimeHealthCheck
    {
        public string RuntimeId { get; set; }
        public string RuntimeName { get; set; }
        public string RuntimePath { get; set; }
        public DateTime CheckTime { get; set; }
        public DateTime? LastSuccessfulCheck { get; set; }
        public HealthStatus Status { get; set; } = HealthStatus.Unknown;
        public bool IsHealthy { get; set; }
        public string PythonVersion { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
    }

    /// <summary>
    /// Health status enumeration.
    /// </summary>
    public enum HealthStatus
    {
        Unknown,
        Healthy,
        Degraded,
        Unhealthy
    }

    #endregion
}
