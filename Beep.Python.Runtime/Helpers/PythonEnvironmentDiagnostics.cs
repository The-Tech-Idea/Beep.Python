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
        public static List<PythonRunTime> PythonRunTimes { get; set; } = new List<PythonRunTime>();
        public static string DefaultReportFileName = "PythonDiagnosticsReport.json";
        public static string DefaultReportTextFileName = "PythonDiagnosticsReport.txt";
        public static string DefaultReportFilePath = Path.Combine(GetDefaultDataPath(), DefaultReportFileName);
        public static string DefaultReportTextFilePath = Path.Combine(GetDefaultDataPath(), DefaultReportTextFileName);
        public static string DefaultReportFilePathWithTimestamp =>
            Path.Combine(GetDefaultDataPath(), $"PythonDiagnosticsReport_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        public static string DefaultReportTextFilePathWithTimestamp =>
            Path.Combine(GetDefaultDataPath(), $"PythonDiagnosticsReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        public static List<PythonRunTime> SyncRuntimesWithExisting(List<PythonRunTime> runtimes)
        {
            var existingRuntimes = GetPythonRuntimesInstallations();
            foreach (var runtime in runtimes)
            {
                var existingRuntime = existingRuntimes.FirstOrDefault(r => r.RuntimePath == runtime.RuntimePath);
                if (existingRuntime != null)
                {
                    existingRuntime.IsPythonInstalled = runtime.IsPythonInstalled;
                    existingRuntime.RuntimePath = runtime.RuntimePath;
                    existingRuntime.PackageType = runtime.PackageType;
                    existingRuntime.CondaPath = runtime.CondaPath;
                    existingRuntime.AiFolderpath = runtime.AiFolderpath;
                    existingRuntime.Binary = runtime.Binary;
                    existingRuntime.PythonVersion = runtime.PythonVersion;
                    existingRuntime.PipFound = runtime.PipFound;
                }
                else
                {
                    // Add new runtime
                    existingRuntimes.Add(runtime);
                }
            }
            return existingRuntimes;
        }

        /// <summary>
        /// Scans the system for all Python and Conda installations.
        /// </summary>
        /// <returns>A list of diagnostic reports for each installation found</returns>
        public static List<PythonDiagnosticsReport> LookForPythonInstallations()
        {
            var reports = new List<PythonDiagnosticsReport>();
            var searchPaths = new List<string>();

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var parentDirs = new[]
                {
            @"C:\",
            @"C:\Program Files\",
            @"C:\Program Files (x86)\",
            $@"C:\Users\{Environment.UserName}\AppData\Local\Programs\",
              $@"C:\Users\{Environment.UserName}\AppData\Local\Programs\Python",
            $@"C:\Users\{Environment.UserName}",
            @"C:\ProgramData\"
        };

                foreach (var parent in parentDirs)
                {
                    if (Directory.Exists(parent))
                    {
                        AddMatchingDirectories(searchPaths, parent, "Python*", true);
                        AddMatchingDirectories(searchPaths, parent, "Anaconda*", true);
                        AddMatchingDirectories(searchPaths, parent, "Miniconda*", true);
                    }
                }

                // Scan all drives for common Python installation directories
                foreach (var drive in Directory.GetLogicalDrives())
                {
                    var driveRoot = drive.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (Directory.Exists(driveRoot))
                    {
                        AddMatchingDirectories(searchPaths, driveRoot, "Python*", true);
                        AddMatchingDirectories(searchPaths, driveRoot, "Anaconda*", true);
                        AddMatchingDirectories(searchPaths, driveRoot, "Miniconda*", true);
                    }
                }
            }
            else
            {
                // Linux/macOS handling can be added similarly
                var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var parentDirs = new[]
                {
            "/usr/bin",
            "/usr/local/bin",
            "/opt/",
            "/usr/local/",
            Path.Combine(userHome),
        };
                foreach (var parent in parentDirs)
                {
                    if (Directory.Exists(parent))
                    {
                        AddMatchingDirectories(searchPaths, parent, "python*", true);
                        AddMatchingDirectories(searchPaths, parent, "anaconda*", true);
                        AddMatchingDirectories(searchPaths, parent, "miniconda*", true);
                    }
                }
            }

            // Deduplicate and filter out invalid paths and unwanted ones like pkgs, venvs
            searchPaths = searchPaths
                .Distinct()
                .Where(Directory.Exists)
                .Where(p => !p.Contains(@"\pkgs\") && !IsSubEnvironment(p))
                .ToList();

            // Helper to check if a directory contains a Python installation
            bool IsPythonDir(string dir, out string pythonExePath)
            {
                string exeName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "python.exe" : "python";
                pythonExePath = Path.Combine(dir, exeName);
                return File.Exists(pythonExePath);
            }

            // Run diagnostics on each valid candidate path
            foreach (var path in searchPaths)
            {
                if (IsPythonDir(path, out string pythonExe))
                {
                    var report = RunFullDiagnostics(Path.GetDirectoryName(pythonExe));
                    if (!reports.Any(r => r.PythonPath == report.PythonPath))
                    {
                        reports.Add(report);
                    }
                }
            }

            return reports;
        }
        // Helper method to avoid recursion and scan only top-level directories
        private static void AddMatchingDirectories(List<string> list, string parent, string pattern, bool topOnly = false)
        {
            try
            {
                var dirs = Directory.GetDirectories(parent, pattern, topOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories);
                foreach (var dir in dirs)
                {
                    list.Add(dir);
                }
            }
            catch { /* ignore access errors */ }
        }
        // Determine if a path is likely a virtual/conda environment
        private static bool IsSubEnvironment(string path)
        {
            var info = new DirectoryInfo(path);

            // Exclude paths that look like environments (e.g., Scripts/, bin/, lib/)
            if (info.Parent != null &&
                (info.Name.Equals("Scripts", StringComparison.OrdinalIgnoreCase) ||
                 info.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                 info.Name.Equals("lib", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Also exclude paths under known environment roots
            var envMarkers = new[] { "env_", "venv", ".venv", "conda-env" };
            return envMarkers.Any(marker => path.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        public static List<PythonDiagnosticsReport> GetCondaEnvironmentsFromBase(PythonRunTime baseRuntime)
        {
            var reports = new List<PythonDiagnosticsReport>();

            if (baseRuntime == null || !baseRuntime.IsPythonInstalled || string.IsNullOrEmpty(baseRuntime.RuntimePath))
                return reports;

            string condaExe = FindCondaExecutable();
            if (string.IsNullOrEmpty(condaExe))
                return reports;

            try
            {
                // Run 'conda env list --json' with the base environment path
                var output = ExecuteProcess(condaExe, $"--root {baseRuntime.RuntimePath} env list --json");
                dynamic envInfo = JsonConvert.DeserializeObject(output);

                if (envInfo?.envs != null)
                {
                    foreach (string envPath in envInfo.envs)
                    {
                        // Skip the base environment itself
                        if (string.Equals(envPath, baseRuntime.RuntimePath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string pythonExePath;
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            pythonExePath = Path.Combine(envPath, "python.exe");
                        else
                            pythonExePath = Path.Combine(envPath, "bin", "python");

                        if (File.Exists(pythonExePath))
                        {
                            var report = RunFullDiagnostics(Path.GetDirectoryName(pythonExePath));
                            report.Warnings.Add("This is a Conda environment.");
                            reports.Add(report);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Conda environments: {ex.Message}");
            }

            return reports;
        }
        public static List<PythonDiagnosticsReport> GetPythonEnvironmentsFromBase(PythonRunTime baseRuntime)
        {
            var reports = new List<PythonDiagnosticsReport>();

            if (baseRuntime == null || !baseRuntime.IsPythonInstalled || string.IsNullOrEmpty(baseRuntime.RuntimePath))
                return reports;

            try
            {
                string venvDir = Path.Combine(baseRuntime.RuntimePath, "Lib", "venv", "scripts");

                if (Directory.Exists(venvDir))
                {
                    foreach (var dir in Directory.GetDirectories(venvDir))
                    {
                        string pyvenvCfg = Path.Combine(dir, "pyvenv.cfg");
                        if (File.Exists(pyvenvCfg))
                        {
                            string pythonExePath = Environment.OSVersion.Platform == PlatformID.Win32NT
                                ? Path.Combine(dir, "Scripts", "python.exe")
                                : Path.Combine(dir, "bin", "python");

                            if (File.Exists(pythonExePath))
                            {
                                var report = RunFullDiagnostics(Path.GetDirectoryName(pythonExePath));
                                report.Warnings.Add("This is a standard Python venv environment.");
                                reports.Add(report);
                            }
                        }
                    }
                }

                // Also scan common project directories for virtual environments
                var projectDirs = new[]
                {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Projects"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects")
        };

                foreach (var projectDir in projectDirs)
                {
                    if (!Directory.Exists(projectDir)) continue;

                    var subDirs = Directory.GetDirectories(projectDir, "*", SearchOption.TopDirectoryOnly);
                    foreach (var dir in subDirs)
                    {
                        string pyvenvCfg = Path.Combine(dir, "pyvenv.cfg");
                        if (File.Exists(pyvenvCfg))
                        {
                            string pythonExePath = Environment.OSVersion.Platform == PlatformID.Win32NT
                                ? Path.Combine(dir, "Scripts", "python.exe")
                                : Path.Combine(dir, "bin", "python");

                            if (File.Exists(pythonExePath))
                            {
                                var report = RunFullDiagnostics(Path.GetDirectoryName(pythonExePath));
                                report.Warnings.Add("This is a standard Python venv environment.");
                                reports.Add(report);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning for Python environments: {ex.Message}");
            }

            return reports;
        }
        public static List<PythonRunTime> GetPythonRuntimesInstallations()
        {
            List<PythonRunTime> pythonRunTimes = new List<PythonRunTime>();
            var reports = LookForPythonInstallations();
            foreach (var report in reports)
            {
                if (report.PythonFound && !string.IsNullOrEmpty(report.PythonPath))
                {
                    var runTime = GetPythonRunTime(report.PythonPath);
                    if (runTime != null)
                    {
                        pythonRunTimes.Add(runTime);
                    }
                }
            }
            return pythonRunTimes;
        }
        public static List<PythonRunTime> GetCondaEnvironmentsFromRuntime(PythonRunTime baseRuntime)
        {
            var pythonRuntimes = new List<PythonRunTime>();

            if (baseRuntime == null || !baseRuntime.IsPythonInstalled || string.IsNullOrEmpty(baseRuntime.RuntimePath))
                return pythonRuntimes;

            string condaExe = FindCondaExecutable();
            if (string.IsNullOrEmpty(condaExe))
                return pythonRuntimes;

            try
            {
                // Run 'conda env list --json' with the base environment path
                var output = ExecuteProcess(condaExe, $"--root {baseRuntime.RuntimePath} env list --json");
                dynamic envInfo = JsonConvert.DeserializeObject(output);

                if (envInfo?.envs != null)
                {
                    foreach (string envPath in envInfo.envs)
                    {
                        // Skip the base environment itself
                        if (string.Equals(envPath, baseRuntime.RuntimePath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string pythonExePath;
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                            pythonExePath = Path.Combine(envPath, "python.exe");
                        else
                            pythonExePath = Path.Combine(envPath, "bin", "python");

                        if (File.Exists(pythonExePath))
                        {
                            var runTime = GetPythonRunTime(envPath);
                            if (runTime != null)
                            {
                                runTime.Message = "This is a Conda environment.";
                                pythonRuntimes.Add(runTime);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Conda environments: {ex.Message}");
            }

            return pythonRuntimes;
        }
        public static List<PythonRunTime> GetPythonEnvironmentsFromRuntime(PythonRunTime baseRuntime)
        {
            var pythonRuntimes = new List<PythonRunTime>();

            if (baseRuntime == null || !baseRuntime.IsPythonInstalled || string.IsNullOrEmpty(baseRuntime.RuntimePath))
                return pythonRuntimes;

            try
            {
                // Look for venv environments inside Lib\venv\scripts by default
                string venvDir = Path.Combine(baseRuntime.RuntimePath, "Lib", "venv", "scripts");

                if (Directory.Exists(venvDir))
                {
                    foreach (var dir in Directory.GetDirectories(venvDir))
                    {
                        string pyvenvCfg = Path.Combine(dir, "pyvenv.cfg");
                        if (File.Exists(pyvenvCfg))
                        {
                            string pythonExePath = Environment.OSVersion.Platform == PlatformID.Win32NT
                                ? Path.Combine(dir, "Scripts", "python.exe")
                                : Path.Combine(dir, "bin", "python");

                            if (File.Exists(pythonExePath))
                            {
                                var runTime = GetPythonRunTime(Path.GetDirectoryName(pythonExePath));
                                if (runTime != null)
                                {
                                    runTime.Message = "This is a standard Python venv environment.";
                                    pythonRuntimes.Add(runTime);
                                }
                            }
                        }
                    }
                }

                // Also scan common project directories for virtual environments
                var projectDirs = new[]
                {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Projects"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects")
        };

                foreach (var projectDir in projectDirs)
                {
                    if (!Directory.Exists(projectDir)) continue;

                    var subDirs = Directory.GetDirectories(projectDir, "*", SearchOption.TopDirectoryOnly);
                    foreach (var dir in subDirs)
                    {
                        string pyvenvCfg = Path.Combine(dir, "pyvenv.cfg");
                        if (File.Exists(pyvenvCfg))
                        {
                            string pythonExePath = Environment.OSVersion.Platform == PlatformID.Win32NT
                                ? Path.Combine(dir, "Scripts", "python.exe")
                                : Path.Combine(dir, "bin", "python");

                            if (File.Exists(pythonExePath))
                            {
                                var runTime = GetPythonRunTime(Path.GetDirectoryName(pythonExePath));
                                if (runTime != null)
                                {
                                    runTime.Message = "This is a standard Python venv environment.";
                                    pythonRuntimes.Add(runTime);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning for Python environments: {ex.Message}");
            }

            return pythonRuntimes;
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
                report.PythonPath = pythonPath;
                report.PythonExe = pythonExe;

                // Check for missing DLLs or critical errors
                string versionOutput = GetPythonVersion(pythonExe);
                if (versionOutput != null && versionOutput.StartsWith("ERROR:"))
                {
                    report.Errors.Add(versionOutput);
                    return report;
                }
                report.PythonVersion = versionOutput;


                report.InternetAvailable = CheckInternetConnection();
                report.Timestamp = DateTime.Now;
                report.CanExecuteCode = TestPythonExecution(pythonExe);

                // Detect if this is a conda environment
                string condaMeta = Path.Combine(pythonPath, "conda-meta");
                report.IsConda = Directory.Exists(condaMeta);
                if (report.CanExecuteCode)
                {
                    report.PipFound = IsPipAvailable(pythonExe);
                }
                if (report.PipFound && report.CanExecuteCode)
                {
                    var pkgs = GetInstalledPackages(pythonExe);
                    if (pkgs != null && pkgs.Count == 1 && pkgs[0].StartsWith("ERROR:"))
                        report.Warnings.Add(pkgs[0]);
                    else
                        report.InstalledPackages = pkgs;
                }
                else
                {
                    report.Warnings.Add("pip is not available.");
                }
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

                try
                {
                    process.Start();
                }
                catch (Exception ex)
                {
                    // This will catch missing DLLs and other startup errors
                    return $"ERROR: Failed to start process: {ex.Message}. This may indicate a missing DLL such as zlib.dll or a corrupted Python installation.";
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (error != null && error.ToLower().Contains("zlib.dll"))
                {
                    return "ERROR: Missing DLL detected: zlib.dll. The Python installation may be incomplete or corrupted.";
                }

                if (string.IsNullOrEmpty(output) && string.IsNullOrEmpty(error) && process.ExitCode != 0)
                {
                    return $"ERROR: Process failed to start. Possible missing DLL (e.g., zlib.dll) or corrupted Python installation.";
                }

                return !string.IsNullOrEmpty(output) ? output : error;
            }
            catch (Exception ex)
            {
                return $"ERROR: Process execution failed: {ex.Message}";
            }
        }
        private static bool TestPythonExecution(string pythonExe)
        {
            var output = ExecuteProcess(pythonExe, "-c \"print('hello test')\"");
            // If output is an error, you can log or handle it here
            if (output != null && output.StartsWith("ERROR:"))
            {
                // Optionally, log this error somewhere or pass it up
                return false;
            }
            return output?.Contains("hello test") ?? false;
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
        public static PythonRunTime GetPythonRunTime(string runtimepath)
        {
            // Check for null path first
            if (string.IsNullOrEmpty(runtimepath))
            {
                return null;
            }

            // Check if Python is installed at the specified path
            if (PythonRunTimeDiagnostics.IsPythonInstalled(runtimepath))
            {
                try
                {
                    // Use the more comprehensive method from PythonRunTimeDiagnostics
                    var runTime = PythonRunTimeDiagnostics.GetPythonConfig(runtimepath);
                    runTime.ID = Guid.NewGuid().ToString();
                    // Set package type
                    runTime.PackageType = PythonRunTimeDiagnostics.GetPackageType(runtimepath);

                    // Set both RuntimePath and BinPath
                    runTime.RuntimePath = runtimepath;
                    string pythonExe = System.IO.Path.Combine(runtimepath, "python.exe");
                    if (System.IO.File.Exists(pythonExe))
                    {
                        runTime.BinPath = runtimepath;
                    }
                    else
                    {
                        // Try common subdirectories
                        string scriptsPath = System.IO.Path.Combine(runtimepath, "Scripts");
                        string binPath = System.IO.Path.Combine(runtimepath, "bin");
                        if (System.IO.File.Exists(System.IO.Path.Combine(scriptsPath, "python.exe")))
                            runTime.BinPath = scriptsPath;
                        else if (System.IO.File.Exists(System.IO.Path.Combine(binPath, "python")))
                            runTime.BinPath = binPath;
                        else
                            runTime.BinPath = runtimepath; // fallback
                    }

                    // Check and set conda path
                    string condaExe = PythonRunTimeDiagnostics.IsCondaInstalled(runtimepath);
                    if (!string.IsNullOrEmpty(condaExe))
                    {
                        runTime.CondaPath = System.IO.Path.Combine(runtimepath, condaExe);
                        runTime.Binary = PythonBinary.Pip;  // Assuming this is the appropriate enum value
                    }

                    // Set AI folder path if needed
                    if (string.IsNullOrEmpty(runTime.AiFolderpath))
                    {
                        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        runTime.AiFolderpath = System.IO.Path.Combine(documentsPath, "AI");

                        // Create the directory if it doesn't exist
                        if (!System.IO.Directory.Exists(runTime.AiFolderpath))
                        {
                            System.IO.Directory.CreateDirectory(runTime.AiFolderpath);
                        }
                    }

                    return runTime;
                }
                catch (Exception ex)
                {
                    // Return a basic runtime object if there's an exception
                    var basicRuntime = new PythonRunTime
                    {
                        IsPythonInstalled = true,
                        RuntimePath = runtimepath,
                        BinPath = runtimepath,
                        Packageinstallpath = Path.Combine(runtimepath, "Lib", "site-packages"),
                        Message = $"Error configuring Python: {ex.Message}"
                    };
                    return basicRuntime;
                }
            }

            // Python not installed at this path
            return null;
        }
        // Save and Load PythonRunTimes methods
        public static void SavePythonRunTimes(List<PythonRunTime> runtimes, string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(runtimes, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save Python runtimes: {ex.Message}");
            }
        }
        public static List<PythonRunTime> LoadPythonRunTimes(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<List<PythonRunTime>>(json) ?? new List<PythonRunTime>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load Python runtimes: {ex.Message}");
            }
            return new List<PythonRunTime>();
        }
        public static void SavePythonRunTimesToDefaultPath(List<PythonRunTime> runtimes)
        {
            SavePythonRunTimes(runtimes, DefaultReportFilePath);
        }
        public static List<PythonRunTime> LoadPythonRunTimesFromDefaultPath()
        {
            return LoadPythonRunTimes(DefaultReportFilePath);

        }
        // Load runtimes and sync with existing installations
        public static List<PythonRunTime> LoadAndSyncPythonRuntimes()
        {
            var runtimes = LoadPythonRunTimesFromDefaultPath();
            return SyncRuntimesWithExisting(runtimes);
        }
        // Package Management
        // Package type detection 
        public static PythonBinary GetPackageType(string runtimePath)
        {
            if (string.IsNullOrEmpty(runtimePath))
                return PythonBinary.Unknown;
            // Check for conda environment
            if (Directory.Exists(Path.Combine(runtimePath, "conda-meta")))
                return PythonBinary.Conda;
            // Check for pip environment
            if (Directory.Exists(Path.Combine(runtimePath, "Lib", "site-packages")))
                return PythonBinary.Pip;
            // Default to unknown if no specific package type is detected
            return PythonBinary.Unknown;
        }
        // Get Packages From Runtime in List<string>
        public static List<string> GetPackagesFromRuntime(PythonRunTime runtime)
        {
           
            if (runtime == null || string.IsNullOrEmpty(runtime.BinPath))
                return new List<string>();
            var packages = new List<string>();
            try
            {
                string pythonExe = Path.Combine(runtime.BinPath, "python.exe");
                if (File.Exists(pythonExe))
                {
                    var output = ExecuteProcess(pythonExe, "-m pip list");
                    if (!string.IsNullOrEmpty(output))
                    {
                        packages = output
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Skip(2) // Skip header lines
                            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0])
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting packages: {ex.Message}");
            }
            return packages;
        }
        // Get Packages From Runtime in List<PackageDefinition>
        public static List<PackageDefinition> GetPackagesFromRuntimeAsDefinitions(PythonRunTime runtime)
        {
            var packages = new List<PackageDefinition>();
            if (runtime == null || string.IsNullOrEmpty(runtime.BinPath))
                return packages;
            try
            {
                string pythonExe = Path.Combine(runtime.BinPath, "python.exe");
                if (File.Exists(pythonExe))
                {
                    var output = ExecuteProcess(pythonExe, "-m pip list");
                    if (!string.IsNullOrEmpty(output))
                    {
                        packages = output
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Skip(2) // Skip header lines
                            .Select(line =>
                            {
                                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                return new PackageDefinition
                                {
                                    PackageName = parts[0],
                                    Version = parts.Length > 1 ? parts[1] : "Unknown",
                                    Status = PackageStatus.Installed
                                };
                            })
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting packages: {ex.Message}");
            }
            return packages;
        }
        // Generate a package list txt file from the runtime for installation on another system
        public static void GeneratePackageListFile(PythonRunTime runtime, string filePath)
        {
            if (runtime == null || string.IsNullOrEmpty(runtime.BinPath))
                return;
            try
            {
                var packages = GetPackagesFromRuntimeAsDefinitions(runtime);
                if (packages.Count > 0)
                {
                    var lines = packages.Select(pkg => $"{pkg.PackageName}=={pkg.Version}");
                    File.WriteAllLines(filePath, lines);
                }
                else
                {
                    File.WriteAllText(filePath, "No packages found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating package list file: {ex.Message}");
            }
        }
        /// <summary>
        /// Creates a new Conda environment at the specified path with the given Python version.
        /// </summary>
        /// <param name="condaExePath">Path to the conda executable.</param>
        /// <param name="envPath">Path where the environment should be created.</param>
        /// <param name="pythonVersion">Optional Python version to use.</param>
        /// <returns>True if creation was successful, false otherwise.</returns>
        public static bool CreateCondaEnvironment(string condaExePath, string envPath, string pythonVersion = null)
        {
            if (string.IsNullOrEmpty(condaExePath) || string.IsNullOrEmpty(envPath))
                return false;
            try
            {
                var args = string.IsNullOrEmpty(pythonVersion)
                    ? $"create -y -p \"{envPath}\" python"
                    : $"create -y -p \"{envPath}\" python={pythonVersion}";
                var output = ExecuteProcess(condaExePath, args);
                return output != null && !output.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Conda environment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new Python venv environment at the specified path.
        /// </summary>
        /// <param name="pythonExePath">Path to the python executable.</param>
        /// <param name="envPath">Path where the environment should be created.</param>
        /// <returns>True if creation was successful, false otherwise.</returns>
        public static bool CreatePythonVenvEnvironment(string pythonExePath, string envPath)
        {
            if (string.IsNullOrEmpty(pythonExePath) || string.IsNullOrEmpty(envPath))
                return false;
            try
            {
                var args = $"-m venv \"{envPath}\" --copies --clear";
                var output = ExecuteProcess(pythonExePath, args);
                return output != null && !output.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Python venv environment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new Conda environment using a PythonRunTime object for the base installation.
        /// </summary>
        /// <param name="baseRuntime">The base Python runtime (must have CondaPath set).</param>
        /// <param name="envPath">Path where the environment should be created.</param>
        /// <param name="pythonVersion">Optional Python version to use.</param>
        /// <returns>True if creation was successful, false otherwise.</returns>
        public static bool CreateCondaEnvironmentFromRuntime(PythonRunTime baseRuntime, string envPath, string pythonVersion = null)
        {
            if (baseRuntime == null || string.IsNullOrEmpty(baseRuntime.CondaPath) || string.IsNullOrEmpty(envPath))
                return false;
            return CreateCondaEnvironment(baseRuntime.CondaPath, envPath, pythonVersion);
        }

        /// <summary>
        /// Creates a new Python venv environment using a PythonRunTime object for the base installation.
        /// </summary>
        /// <param name="baseRuntime">The base Python runtime (must have BinPath set).</param>
        /// <param name="envPath">Path where the environment should be created.</param>
        /// <returns>True if creation was successful, false otherwise.</returns>
        public static bool CreatePythonVenvEnvironmentFromRuntime(PythonRunTime baseRuntime, string envPath)
        {
            if (baseRuntime == null || string.IsNullOrEmpty(baseRuntime.BinPath) || string.IsNullOrEmpty(envPath))
                return false;
            string pythonExe = System.IO.Path.Combine(baseRuntime.BinPath, "python.exe");
            return CreatePythonVenvEnvironment(pythonExe, envPath);
        }
    }
}
