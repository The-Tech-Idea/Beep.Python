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

    }
}
