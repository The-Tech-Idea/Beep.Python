using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Beep.Python.RuntimeEngine.Infrastructure;
using TheTechIdea.Beep.ConfigUtil;
 
using TheTechIdea.Beep.Editor;
using SysEnv = System.Environment;

namespace Beep.Python.RuntimeEngine.Diagnostics
{
    /// <summary>
    /// Advanced diagnostic tools for Python runtime analysis and troubleshooting
    /// </summary>
    public class PythonAdvancedDiagnostics
    {
       


        public PythonAdvancedDiagnostics()
        {
             
           
        }

        /// <summary>
        /// Runs comprehensive diagnostics on a Python runtime
        /// </summary>
        public async Task<ComprehensiveDiagnosticReport> RunComprehensiveDiagnosticsAsync(
            PythonRuntimeInfo runtime,
            CancellationToken cancellationToken = default)
        {
            var report = new ComprehensiveDiagnosticReport
            {
                RuntimeId = runtime.Id,
                RuntimeName = runtime.Name,
                RuntimePath = runtime.Path,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // 1. Basic environment check
                report.BasicDiagnostics = PythonEnvironmentDiagnostics.RunFullDiagnostics(runtime.Path);

                // 2. DLL dependency check
                report.DllDependencies = await CheckDllDependenciesAsync(runtime.Path, cancellationToken);

                // 3. Package analysis
                report.PackageAnalysis = await AnalyzeInstalledPackagesAsync(runtime.Path, cancellationToken);

                // 4. Performance benchmarks
                report.PerformanceBenchmarks = await RunPerformanceBenchmarksAsync(runtime.Path, cancellationToken);

                // 5. Python.NET compatibility
                report.PythonNetCompatibility = await CheckPythonNetCompatibilityAsync(runtime.Path, cancellationToken);

                // 6. Security analysis
                report.SecurityAnalysis = await PerformSecurityAnalysisAsync(runtime.Path, cancellationToken);

                // 7. Disk usage analysis
                report.DiskUsage = AnalyzeDiskUsage(runtime.Path);

                report.EndTime = DateTime.UtcNow;
                report.IsSuccessful = true;

            //    _dmEditor?.AddLogMessage("Beep", $"Comprehensive diagnostics completed for {runtime.Name}", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                report.IsSuccessful = false;
                report.ErrorMessage = ex.Message;
              //  _dmEditor?.AddLogMessage("Beep", $"Diagnostics failed: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return report;
        }

        /// <summary>
        /// Checks DLL dependencies for a Python runtime
        /// </summary>
        public async Task<DllDependencyReport> CheckDllDependenciesAsync(
            string pythonPath,
            CancellationToken cancellationToken = default)
        {
            var report = new DllDependencyReport();

            var criticalDlls = new Dictionary<string, string>
            {
                ["python311.dll"] = "Core Python interpreter",
                ["python3.dll"] = "Python 3 redirector",
                ["vcruntime140.dll"] = "Visual C++ Runtime",
                ["sqlite3.dll"] = "SQLite database support"
            };

            var optionalDlls = new Dictionary<string, string>
            {
                ["_sqlite3.pyd"] = "SQLite Python extension",
                ["_ssl.pyd"] = "SSL/TLS support",
                ["_hashlib.pyd"] = "Cryptographic hashing",
                ["_socket.pyd"] = "Network socket support"
            };

            foreach (var dll in criticalDlls)
            {
                var dllPath = Path.Combine(pythonPath, dll.Key);
                var exists = File.Exists(dllPath);

                report.Dependencies.Add(new DllDependency
                {
                    Name = dll.Key,
                    Description = dll.Value,
                    IsRequired = true,
                    Found = exists,
                    Path = exists ? dllPath : null,
                    Size = exists ? new FileInfo(dllPath).Length : 0
                });

                if (!exists)
                    report.MissingCritical.Add(dll.Key);
            }

            foreach (var dll in optionalDlls)
            {
                var dllPath = Path.Combine(pythonPath, "DLLs", dll.Key);
                var exists = File.Exists(dllPath);

                if (!exists)
                {
                    dllPath = Path.Combine(pythonPath, dll.Key);
                    exists = File.Exists(dllPath);
                }

                report.Dependencies.Add(new DllDependency
                {
                    Name = dll.Key,
                    Description = dll.Value,
                    IsRequired = false,
                    Found = exists,
                    Path = exists ? dllPath : null,
                    Size = exists ? new FileInfo(dllPath).Length : 0
                });

                if (!exists)
                    report.MissingOptional.Add(dll.Key);
            }

            report.IsHealthy = report.MissingCritical.Count == 0;

            return report;
        }

        /// <summary>
        /// Analyzes installed packages for issues
        /// </summary>
        public async Task<PackageAnalysisReport> AnalyzeInstalledPackagesAsync(
            string pythonPath,
            CancellationToken cancellationToken = default)
        {
            var report = new PackageAnalysisReport();

            try
            {
                var pythonExe = Path.Combine(pythonPath, "python.exe");
                if (!File.Exists(pythonExe))
                {
                    report.ErrorMessage = "Python executable not found";
                    return report;
                }

                // Get list of installed packages
                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m pip list --format=json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync(cancellationToken);

                    if (process.ExitCode == 0)
                    {
                        // Parse package list
                        var packages = ParsePackageList(output);
                        report.TotalPackages = packages.Count;
                        report.Packages = packages;

                        // Check for outdated packages
                        report.OutdatedPackages = await CheckOutdatedPackagesAsync(pythonExe, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// Runs performance benchmarks on Python runtime
        /// </summary>
        public async Task<PerformanceBenchmarkReport> RunPerformanceBenchmarksAsync(
            string pythonPath,
            CancellationToken cancellationToken = default)
        {
            var report = new PerformanceBenchmarkReport();

            try
            {
                var pythonExe = Path.Combine(pythonPath, "python.exe");
                if (!File.Exists(pythonExe))
                {
                    report.ErrorMessage = "Python executable not found";
                    return report;
                }

                // Benchmark 1: Simple arithmetic
                report.SimpleArithmeticMs = await BenchmarkCodeAsync(pythonExe,
                    "sum(range(10000))", cancellationToken);

                // Benchmark 2: List comprehension
                report.ListComprehensionMs = await BenchmarkCodeAsync(pythonExe,
                    "[x*x for x in range(1000)]", cancellationToken);

                // Benchmark 3: String operations
                report.StringOperationsMs = await BenchmarkCodeAsync(pythonExe,
                    "''.join(['test' for _ in range(1000)])", cancellationToken);

                // Benchmark 4: Import time
                report.ImportTimeMs = await BenchmarkCodeAsync(pythonExe,
                    "import sys, os, json", cancellationToken);

                report.OverallScore = CalculatePerformanceScore(report);
            }
            catch (Exception ex)
            {
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// Checks Python.NET compatibility
        /// </summary>
        public async Task<PythonNetCompatibilityReport> CheckPythonNetCompatibilityAsync(
            string pythonPath,
            CancellationToken cancellationToken = default)
        {
            var report = new PythonNetCompatibilityReport();

            try
            {
                // Check Python version compatibility
                var diagnostics = PythonEnvironmentDiagnostics.RunFullDiagnostics(pythonPath);
                report.PythonVersion = diagnostics.PythonVersion;

                // Python.NET supports Python 3.7+
                if (!string.IsNullOrEmpty(report.PythonVersion))
                {
                    var versionMatch = System.Text.RegularExpressions.Regex.Match(
                        report.PythonVersion, @"(\d+)\.(\d+)");

                    if (versionMatch.Success)
                    {
                        var major = int.Parse(versionMatch.Groups[1].Value);
                        var minor = int.Parse(versionMatch.Groups[2].Value);

                        report.IsCompatible = major == 3 && minor >= 7;
                        report.CompatibilityNotes.Add($"Python {major}.{minor} detected");

                        if (!report.IsCompatible)
                        {
                            report.CompatibilityNotes.Add("Python.NET requires Python 3.7 or later");
                        }
                    }
                }

                // Check for required modules
                report.HasRequiredModules = diagnostics.PythonFound && diagnostics.CanExecuteCode;

                // Check architecture (x64 vs x86)
                report.Architecture = SysEnv.Is64BitOperatingSystem ? "x64" : "x86";
                report.CompatibilityNotes.Add($"System architecture: {report.Architecture}");
            }
            catch (Exception ex)
            {
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// Performs security analysis
        /// </summary>
        public async Task<SecurityAnalysisReport> PerformSecurityAnalysisAsync(
            string pythonPath,
            CancellationToken cancellationToken = default)
        {
            var report = new SecurityAnalysisReport();

            try
            {
                // Check file permissions
                var dirInfo = new DirectoryInfo(pythonPath);
                report.IsReadable = dirInfo.Exists;
                report.IsWritable = CheckDirectoryWritable(pythonPath);

                // Check for suspicious files
                report.SuspiciousFiles = FindSuspiciousFiles(pythonPath);

                // Check SSL/TLS support
                var sslPath = Path.Combine(pythonPath, "DLLs", "_ssl.pyd");
                report.HasSslSupport = File.Exists(sslPath);

                // Check for pip
                report.HasPip = File.Exists(Path.Combine(pythonPath, "Scripts", "pip.exe")) ||
                               File.Exists(Path.Combine(pythonPath, "Scripts", "pip3.exe"));

                report.OverallSecurityLevel = DetermineSecurityLevel(report);
            }
            catch (Exception ex)
            {
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        /// <summary>
        /// Analyzes disk usage
        /// </summary>
        public DiskUsageReport AnalyzeDiskUsage(string pythonPath)
        {
            var report = new DiskUsageReport
            {
                RootPath = pythonPath
            };

            try
            {
                if (!Directory.Exists(pythonPath))
                {
                    report.ErrorMessage = "Path does not exist";
                    return report;
                }

                // Calculate total size
                var dirInfo = new DirectoryInfo(pythonPath);
                report.TotalSizeBytes = CalculateDirectorySize(dirInfo);
                report.TotalSizeMB = report.TotalSizeBytes / (1024.0 * 1024.0);

                // Breakdown by subdirectory
                foreach (var subdir in dirInfo.GetDirectories())
                {
                    var size = CalculateDirectorySize(subdir);
                    report.SubdirectoryUsage[subdir.Name] = size;
                }

                // Count files
                report.TotalFiles = dirInfo.GetFiles("*", SearchOption.AllDirectories).Length;
            }
            catch (Exception ex)
            {
                report.ErrorMessage = ex.Message;
            }

            return report;
        }

        #region Private Helper Methods

        private List<PackageInfo> ParsePackageList(string json)
        {
            // Simple parsing - in production use JSON deserializer
            var packages = new List<PackageInfo>();
            // TODO: Implement JSON parsing
            return packages;
        }

        private async Task<List<string>> CheckOutdatedPackagesAsync(
            string pythonExe,
            CancellationToken cancellationToken)
        {
            var outdated = new List<string>();
            // TODO: Implement outdated package check
            return outdated;
        }

        private async Task<double> BenchmarkCodeAsync(
            string pythonExe,
            string code,
            CancellationToken cancellationToken)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"-c \"{code}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync(cancellationToken);
                    sw.Stop();
                    return sw.Elapsed.TotalMilliseconds;
                }
            }
            catch { }

            return -1;
        }

        private double CalculatePerformanceScore(PerformanceBenchmarkReport report)
        {
            // Simple scoring based on execution times
            var scores = new List<double>
            {
                100 - (report.SimpleArithmeticMs * 0.1),
                100 - (report.ListComprehensionMs * 0.05),
                100 - (report.StringOperationsMs * 0.05),
                100 - (report.ImportTimeMs * 0.02)
            };

            return scores.Where(s => s > 0).Average();
        }

        private bool CheckDirectoryWritable(string path)
        {
            try
            {
                var testFile = Path.Combine(path, $".write-test-{Guid.NewGuid()}");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<string> FindSuspiciousFiles(string path)
        {
            var suspicious = new List<string>();
            // TODO: Implement suspicious file detection
            return suspicious;
        }

        private string DetermineSecurityLevel(SecurityAnalysisReport report)
        {
            if (!report.HasSslSupport || !report.HasPip)
                return "Low";
            if (report.SuspiciousFiles.Any())
                return "Warning";
            return "Good";
        }

        private long CalculateDirectorySize(DirectoryInfo directory)
        {
            long size = 0;

            try
            {
                // Add file sizes
                var files = directory.GetFiles();
                foreach (var file in files)
                {
                    size += file.Length;
                }

                // Add subdirectory sizes
                var subdirs = directory.GetDirectories();
                foreach (var subdir in subdirs)
                {
                    size += CalculateDirectorySize(subdir);
                }
            }
            catch { }

            return size;
        }

        #endregion
    }

    #region Report Classes

    public class ComprehensiveDiagnosticReport
    {
        public string RuntimeId { get; set; }
        public string RuntimeName { get; set; }
        public string RuntimePath { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }

        public PythonDiagnosticsReport BasicDiagnostics { get; set; }
        public DllDependencyReport DllDependencies { get; set; }
        public PackageAnalysisReport PackageAnalysis { get; set; }
        public PerformanceBenchmarkReport PerformanceBenchmarks { get; set; }
        public PythonNetCompatibilityReport PythonNetCompatibility { get; set; }
        public SecurityAnalysisReport SecurityAnalysis { get; set; }
        public DiskUsageReport DiskUsage { get; set; }

        public TimeSpan Duration => EndTime - StartTime;
    }

    public class DllDependencyReport
    {
        public List<DllDependency> Dependencies { get; set; } = new();
        public List<string> MissingCritical { get; set; } = new();
        public List<string> MissingOptional { get; set; } = new();
        public bool IsHealthy { get; set; }
    }

    public class DllDependency
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public bool Found { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
    }

    public class PackageAnalysisReport
    {
        public int TotalPackages { get; set; }
        public List<PackageInfo> Packages { get; set; } = new();
        public List<string> OutdatedPackages { get; set; } = new();
        public string ErrorMessage { get; set; }
    }

    public class PackageInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Location { get; set; }
    }

    public class PerformanceBenchmarkReport
    {
        public double SimpleArithmeticMs { get; set; }
        public double ListComprehensionMs { get; set; }
        public double StringOperationsMs { get; set; }
        public double ImportTimeMs { get; set; }
        public double OverallScore { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PythonNetCompatibilityReport
    {
        public string PythonVersion { get; set; }
        public bool IsCompatible { get; set; }
        public bool HasRequiredModules { get; set; }
        public string Architecture { get; set; }
        public List<string> CompatibilityNotes { get; set; } = new();
        public string ErrorMessage { get; set; }
    }

    public class SecurityAnalysisReport
    {
        public bool IsReadable { get; set; }
        public bool IsWritable { get; set; }
        public bool HasSslSupport { get; set; }
        public bool HasPip { get; set; }
        public List<string> SuspiciousFiles { get; set; } = new();
        public string OverallSecurityLevel { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DiskUsageReport
    {
        public string RootPath { get; set; }
        public long TotalSizeBytes { get; set; }
        public double TotalSizeMB { get; set; }
        public int TotalFiles { get; set; }
        public Dictionary<string, long> SubdirectoryUsage { get; set; } = new();
        public string ErrorMessage { get; set; }
    }

    #endregion
}
