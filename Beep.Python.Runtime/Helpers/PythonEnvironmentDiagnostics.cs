using Beep.Python.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Helpers
{
    public static class PythonEnvironmentDiagnostics
    {
        /// <summary>
        /// Scans the system for all Python and Conda installations.
        /// </summary>
        /// <returns>A list of diagnostic reports for each installation found</returns>
        public static List<PythonDiagnosticsReport> LookForPythonInstallations()
        {
            var reports = new List<PythonDiagnosticsReport>();
            var searchPaths = new List<string>();

            // Standard Python installation locations
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Windows common paths
                searchPaths.AddRange(new[]
                {
            @"C:\Python27",
            @"C:\Python36",
            @"C:\Python37",
            @"C:\Python38",
            @"C:\Python39",
            @"C:\Python310",
            @"C:\Python311",
            @"C:\Python312",
            @"C:\Program Files\Python27",
            @"C:\Program Files\Python36",
            @"C:\Program Files\Python37",
            @"C:\Program Files\Python38",
            @"C:\Program Files\Python39",
            @"C:\Program Files\Python310",
            @"C:\Program Files\Python311",
            @"C:\Program Files\Python312",
            @"C:\Program Files (x86)\Python27",
            @"C:\Program Files (x86)\Python36",
            @"C:\Program Files (x86)\Python37",
            @"C:\Program Files (x86)\Python38",
            @"C:\Program Files (x86)\Python39",
            @"C:\Program Files (x86)\Python310",
            @"C:\Program Files (x86)\Python311",
            @"C:\Program Files (x86)\Python312",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python",
            @"C:\ProgramData\Anaconda3",
            @"C:\Users\" + Environment.UserName + @"\Anaconda3",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Continuum\anaconda3",
            @"C:\Users\" + Environment.UserName + @"\miniconda3",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Continuum\miniconda3",
            @"C:\ProgramData\Miniconda3"
        });

                // Search all drives for common Python installation directories
                foreach (var drive in Directory.GetLogicalDrives())
                {
                    searchPaths.Add(Path.Combine(drive, "Python"));
                    searchPaths.Add(Path.Combine(drive, "ProgramData", "Anaconda3"));
                    searchPaths.Add(Path.Combine(drive, "ProgramData", "Miniconda3"));
                }

                // Check Windows Registry for Python installations
                try
                {
                    using (var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Python\PythonCore"))
                    {
                        if (baseKey != null)
                        {
                            foreach (var versionName in baseKey.GetSubKeyNames())
                            {
                                using (var versionKey = baseKey.OpenSubKey(versionName + @"\InstallPath"))
                                {
                                    if (versionKey != null)
                                    {
                                        string path = versionKey.GetValue(null) as string;
                                        if (!string.IsNullOrEmpty(path))
                                        {
                                            searchPaths.Add(path);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accessing registry: {ex.Message}");
                }
            }
            else
            {
                // Linux/macOS common paths
                searchPaths.AddRange(new[]
                {
            "/usr/bin",
            "/usr/local/bin",
            "/opt/python",
            "/opt/anaconda3",
            "/opt/miniconda3",
            "/usr/local/anaconda3",
            "/usr/local/miniconda3",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "anaconda3"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "miniconda3")
        });
            }

            // Collect from PATH environment variable
            var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
            searchPaths.AddRange(pathDirs);

            // Deduplicate search paths
            searchPaths = searchPaths.Distinct().Where(Directory.Exists).ToList();

            // Helper to check if a directory contains a Python installation
            bool IsPythonDir(string dir, out string pythonExePath)
            {
                string exeName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "python.exe" : "python";
                pythonExePath = Path.Combine(dir, exeName);

                if (!File.Exists(pythonExePath))
                {
                    // For Linux/macOS, check if the directory itself is python executable
                    if (Environment.OSVersion.Platform != PlatformID.Win32NT &&
                        dir.EndsWith("/python") && File.Exists(dir))
                    {
                        pythonExePath = dir;
                        return true;
                    }
                    return false;
                }
                return true;
            }

            // Scan directly in the search paths
            foreach (var path in searchPaths)
            {
                if (IsPythonDir(path, out string pythonExe))
                {
                    var report = RunFullDiagnostics(Path.GetDirectoryName(pythonExe));
                    if (!reports.Any(r => r.PythonPath == report.PythonPath)) // Avoid duplicates
                    {
                        reports.Add(report);
                    }
                }
            }

            // Find Python installations within search directories (but not recursive to avoid slow scanning)
            foreach (var basePath in searchPaths)
            {
                try
                {
                    var subDirs = Directory.GetDirectories(basePath);
                    foreach (var dir in subDirs)
                    {
                        if (Path.GetFileName(dir).StartsWith("Python", StringComparison.OrdinalIgnoreCase) ||
                            Path.GetFileName(dir).EndsWith("Python", StringComparison.OrdinalIgnoreCase) ||
                            Path.GetFileName(dir).StartsWith("Anaconda", StringComparison.OrdinalIgnoreCase) ||
                            Path.GetFileName(dir).StartsWith("Miniconda", StringComparison.OrdinalIgnoreCase))
                        {
                            if (IsPythonDir(dir, out string pythonExe))
                            {
                                var report = RunFullDiagnostics(Path.GetDirectoryName(pythonExe));
                                if (!reports.Any(r => r.PythonPath == report.PythonPath)) // Avoid duplicates
                                {
                                    reports.Add(report);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning directory {basePath}: {ex.Message}");
                }
            }

            // Look for Conda environments
            FindCondaEnvironments(reports);

            return reports;
        }

        /// <summary>
        /// Finds Conda environments and adds them to the reports list.
        /// </summary>
        private static void FindCondaEnvironments(List<PythonDiagnosticsReport> reports)
        {
            // Try to find conda command to list environments
            string condaExe = FindCondaExecutable();
            if (string.IsNullOrEmpty(condaExe))
                return;

            try
            {
                // Get conda environments
                var output = ExecuteProcess(condaExe, "env list --json");
                if (string.IsNullOrEmpty(output))
                    return;

                // Parse JSON output
                try
                {
                    dynamic envInfo = JsonConvert.DeserializeObject(output);
                    if (envInfo?.envs == null)
                        return;

                    foreach (string envPath in envInfo.envs)
                    {
                        string pythonExePath;
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            pythonExePath = Path.Combine(envPath, "python.exe");
                        else
                            pythonExePath = Path.Combine(envPath, "bin", "python");

                        if (File.Exists(pythonExePath))
                        {
                            var report = RunFullDiagnostics(Path.GetDirectoryName(pythonExePath));
                            report.Warnings.Add("This is a Conda environment");

                            if (!reports.Any(r => r.PythonPath == report.PythonPath)) // Avoid duplicates
                            {
                                reports.Add(report);
                            }
                        }
                    }
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine($"Error parsing conda environments: {jsonEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding conda environments: {ex.Message}");
            }
        }

        /// <summary>
        /// Locates the conda executable on the system.
        /// </summary>
        private static string FindCondaExecutable()
        {
            string condaExe = Environment.OSVersion.Platform == PlatformID.Win32NT ? "conda.exe" : "conda";

            // Check if conda is in PATH
            var pathDirs = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);
            foreach (var dir in pathDirs)
            {
                string fullPath = Path.Combine(dir, condaExe);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            // Check common Conda installation locations
            var condaPaths = new List<string>();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                condaPaths.AddRange(new[]
                {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Anaconda3", "Scripts", condaExe),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Miniconda3", "Scripts", condaExe),
            @"C:\ProgramData\Anaconda3\Scripts\" + condaExe,
            @"C:\ProgramData\Miniconda3\Scripts\" + condaExe
        });
            }
            else
            {
                condaPaths.AddRange(new[]
                {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "anaconda3", "bin", "conda"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "miniconda3", "bin", "conda"),
            "/opt/anaconda3/bin/conda",
            "/opt/miniconda3/bin/conda",
            "/usr/local/anaconda3/bin/conda",
            "/usr/local/miniconda3/bin/conda"
        });
            }

            foreach (var path in condaPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        public static List<string> GetPythonInstalltions(string basePath)
        {
            var installations = new List<string>();
            try
            {
                if (Directory.Exists(basePath))
                {
                    var directories = Directory.GetDirectories(basePath, "python*", SearchOption.TopDirectoryOnly);
                    foreach (var dir in directories)
                    {
                        var pythonExe = Path.Combine(dir, "python.exe");
                        if (File.Exists(pythonExe))
                        {
                            installations.Add(dir);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while searching for Python installations: {ex.Message}");
            }
            return installations;
        }
        public static PythonDiagnosticsReport RunFullDiagnostics(string pythonPath)
        {
            var report = new PythonDiagnosticsReport();

            try
            {
                string pythonExe = Path.Combine(pythonPath, "python.exe");
                if (!File.Exists(pythonExe))
                {
                    report.Errors.Add($"Python not found at path: {pythonExe}");
                    return report;
                }

                report.PythonFound = true;
                report.PythonPath = pythonExe;

                report.PythonVersion = GetPythonVersion(pythonExe);
                report.PipFound = IsPipAvailable(pythonExe);
                report.InternetAvailable = CheckInternetConnection();

                if (report.PipFound)
                    report.InstalledPackages = GetInstalledPackages(pythonExe);
                else
                    report.Warnings.Add("pip is not available.");

                report.CanExecuteCode = TestPythonExecution(pythonExe);
            }
            catch (Exception ex)
            {
                report.Errors.Add("Unexpected error: " + ex.Message);
            }

            return report;
        }

        private static string GetPythonVersion(string pythonExe)
        {
            var output = ExecuteProcess(pythonExe, "--version");
            return output?.Trim();
        }

        private static bool IsPipAvailable(string pythonExe)
        {
            var output = ExecuteProcess(pythonExe, "-m pip --version");
            return !string.IsNullOrEmpty(output) && output.Contains("pip");
        }

        private static List<string> GetInstalledPackages(string pythonExe)
        {
            var output = ExecuteProcess(pythonExe, "-m pip list");
            return output?
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(2)
                .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
                .ToList() ?? new List<string>();
        }

        private static bool TestPythonExecution(string pythonExe)
        {
            var output = ExecuteProcess(pythonExe, "-c \"print('hello test')\"");
            return output?.Contains("hello test") ?? false;
        }

        private static bool CheckInternetConnection()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var response = client.GetAsync("https://pypi.org").Result;
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static string ExecuteProcess(string exePath, string args)
        {
            try
            {
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrEmpty(output) ? output : error;
            }
            catch
            {
                return null;
            }
        }

        public static PythonDiagnosticsReport LoadReportFromJson(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<PythonDiagnosticsReport>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load report: {ex.Message}");
                return null;
            }
        }

        public static void SaveReportAsJson(PythonDiagnosticsReport report, string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(report, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save report: {ex.Message}");
            }
        }
        public static void SaveReportAsText(PythonDiagnosticsReport report, string filePath)
        {
            try
            {
                var lines = new List<string>
        {
            "=== Python Environment Diagnostics Report ===",
            $"Timestamp: {report.Timestamp}",
            $"Python Found: {report.PythonFound}",
            $"Python Path: {report.PythonPath}",
            $"Python Version: {report.PythonVersion}",
            $"Pip Found: {report.PipFound}",
            $"Can Execute Code: {report.CanExecuteCode}",
            $"Internet Available: {report.InternetAvailable}",
            "",
            "Installed Packages:"
        };

                if (report.InstalledPackages.Any())
                {
                    lines.AddRange(report.InstalledPackages.Select(pkg => $"  - {pkg}"));
                }
                else
                {
                    lines.Add("  (none found)");
                }

                if (report.Warnings.Any())
                {
                    lines.Add("");
                    lines.Add("Warnings:");
                    lines.AddRange(report.Warnings.Select(w => $"  ! {w}"));
                }

                if (report.Errors.Any())
                {
                    lines.Add("");
                    lines.Add("Errors:");
                    lines.AddRange(report.Errors.Select(e => $"  !! {e}"));
                }

                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save text report: {ex.Message}");
            }
        }

        public static string GetDefaultDataPath()
        {
            string baseDir;

            // Use platform-specific paths
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Windows: Use AppData/Roaming
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                baseDir = Path.Combine(baseDir, "Beep");
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // Linux/macOS: Use ~/.beep
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                baseDir = Path.Combine(baseDir, ".beep");
            }
            else
            {
                // Fallback for other platforms
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Beep");
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            return baseDir;
        }

    }
}
