using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
 
using TheTechIdea.Beep.Editor;
using SysEnv = System.Environment;

namespace Beep.Python.RuntimeEngine.Infrastructure
{
    /// <summary>
    /// Manages registration, discovery, and persistence of Python runtime installations.
    /// Tracks both managed (created by framework) and discovered (system) runtimes.
    /// </summary>
    public class PythonRuntimeRegistry : IPythonRuntimeRegistry
    {
       
        private readonly IDMEEditor _dmEditor;
        private readonly string _registryPath;
        private readonly List<PythonRuntimeInfo> _runtimes = new();
        private PythonRuntimeInfo _defaultRuntime;
        private readonly object _lock = new();

        public PythonRuntimeRegistry( )
        {
             
          
            var baseDir = Path.Combine(
                SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile),
                ".beep-python");
            
            Directory.CreateDirectory(baseDir);
            _registryPath = Path.Combine(baseDir, "runtimes.json");
        }

        /// <summary>
        /// Initializes the registry by loading persisted data and discovering runtimes.
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                _dmEditor?.AddLogMessage("Beep", "Initializing Python runtime registry...", DateTime.Now, 0, null, Errors.Ok);

                // Load persisted runtimes
                await LoadRuntimeConfigurationsAsync();

                // Discover system runtimes if registry is empty
                if (!_runtimes.Any())
                {
                    _dmEditor?.AddLogMessage("Beep", "No runtimes in registry, discovering system installations...", DateTime.Now, 0, null, Errors.Ok);
                    await DiscoverRuntimesAsync();
                }

                // Set default runtime if not already set
                if (_defaultRuntime == null && _runtimes.Any())
                {
                    _defaultRuntime = _runtimes.FirstOrDefault(r => r.Status == PythonRuntimeStatus.Ready)
                                   ?? _runtimes.First();
                    
                    _dmEditor?.AddLogMessage("Beep", $"Default runtime set to: {_defaultRuntime.Name}", DateTime.Now, 0, null, Errors.Ok);
                }

                await SaveRuntimeConfigurationsAsync();

                _dmEditor?.AddLogMessage("Beep", $"Runtime registry initialized with {_runtimes.Count} runtime(s)", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Failed to initialize runtime registry: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Gets all available runtimes.
        /// </summary>
        public IEnumerable<PythonRuntimeInfo> GetAvailableRuntimes()
        {
            lock (_lock)
            {
                return _runtimes.ToList();
            }
        }

        /// <summary>
        /// Gets a specific runtime by ID.
        /// </summary>
        public PythonRuntimeInfo GetRuntime(string runtimeId)
        {
            lock (_lock)
            {
                return _runtimes.FirstOrDefault(r => r.Id == runtimeId);
            }
        }

        /// <summary>
        /// Gets the default runtime.
        /// </summary>
        public PythonRuntimeInfo GetDefaultRuntime()
        {
            lock (_lock)
            {
                return _defaultRuntime;
            }
        }

        /// <summary>
        /// Sets the default runtime.
        /// </summary>
        public async Task<bool> SetDefaultRuntimeAsync(string runtimeId)
        {
            lock (_lock)
            {
                var runtime = _runtimes.FirstOrDefault(r => r.Id == runtimeId);
                if (runtime == null)
                    return false;

                _defaultRuntime = runtime;
                _dmEditor?.AddLogMessage("Beep", $"Default runtime changed to: {runtime.Name} ({runtimeId})", DateTime.Now, 0, null, Errors.Ok);
            }

            await SaveRuntimeConfigurationsAsync();
            return true;
        }

        /// <summary>
        /// Registers a new managed runtime.
        /// </summary>
        public async Task<string> RegisterManagedRuntimeAsync(
            string name,
            PythonRuntimeType type = PythonRuntimeType.Embedded)
        {
            var runtimeId = Guid.NewGuid().ToString("N")[..8];

            var runtimeDir = type == PythonRuntimeType.Embedded
                ? Path.Combine(SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile), ".beep-python", "embedded")
                : Path.Combine(SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile), ".beep-python", "runtimes", runtimeId);

            var runtime = new PythonRuntimeInfo
            {
                Id = runtimeId,
                Name = name,
                Type = type,
                Path = runtimeDir,
                Version = "Unknown",
                IsManaged = true,
                CreatedAt = DateTime.UtcNow,
                Status = PythonRuntimeStatus.NotInitialized,
                InstalledPackages = new Dictionary<string, string>(),
                Warnings = new List<string>(),
                Errors = new List<string>()
            };

            lock (_lock)
            {
                _runtimes.Add(runtime);
            }

            await SaveRuntimeConfigurationsAsync();

            _dmEditor?.AddLogMessage("Beep", $"Registered managed runtime: {name} ({runtimeId}) at {runtimeDir}", DateTime.Now, 0, null, Errors.Ok);
            return runtimeId;
        }

        /// <summary>
        /// Deletes a managed runtime.
        /// </summary>
        public async Task<bool> DeleteRuntimeAsync(string runtimeId)
        {
            PythonRuntimeInfo runtime;

            lock (_lock)
            {
                runtime = _runtimes.FirstOrDefault(r => r.Id == runtimeId);
                if (runtime == null || !runtime.IsManaged)
                    return false;
            }

            try
            {
                // Delete physical directory
                if (Directory.Exists(runtime.Path))
                {
                    Directory.Delete(runtime.Path, true);
                }

                lock (_lock)
                {
                    _runtimes.Remove(runtime);

                    // Clear default if it was the deleted runtime
                    if (_defaultRuntime?.Id == runtimeId)
                    {
                        _defaultRuntime = _runtimes.FirstOrDefault();
                    }
                }

                await SaveRuntimeConfigurationsAsync();

                _dmEditor?.AddLogMessage("Beep", $"Deleted runtime: {runtime.Name} ({runtimeId})", DateTime.Now, 0, null, Errors.Ok);
                return true;
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Failed to delete runtime {runtimeId}: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Discovers Python installations on the system.
        /// </summary>
        public async Task<List<PythonRuntimeInfo>> DiscoverRuntimesAsync()
        {
            var discovered = new List<PythonRuntimeInfo>();

            try
            {
                _dmEditor?.AddLogMessage("Beep", "Discovering Python runtimes...", DateTime.Now, 0, null,   Errors.Ok);

                var reports = await Task.Run(() => PythonEnvironmentDiagnostics.LookForPythonInstallations());

                foreach (var report in reports)
                {
                    if (!report.PythonFound || string.IsNullOrEmpty(report.PythonPath))
                        continue;

                    var existingId = _runtimes.FirstOrDefault(r => r.Path == report.PythonPath)?.Id;
                    var runtimeId = existingId ?? Guid.NewGuid().ToString("N")[..8];

                    var runtime = new PythonRuntimeInfo
                    {
                        Id = runtimeId,
                        Name = $"Python {report.PythonVersion ?? "Unknown"} ({Path.GetFileName(report.PythonPath)})",
                        Type = report.IsConda ? PythonRuntimeType.Conda : PythonRuntimeType.System,
                        Path = report.PythonPath,
                        Version = report.PythonVersion,
                        IsManaged = false,
                        CreatedAt = DateTime.UtcNow,
                        Status = report.CanExecuteCode ? PythonRuntimeStatus.Ready : PythonRuntimeStatus.Error,
                        InstalledPackages = report.InstalledPackages?.ToDictionary(p => p, p => "Unknown") ?? new Dictionary<string, string>(),
                        Warnings = report.Warnings ?? new List<string>(),
                        Errors = report.Errors ?? new List<string>()
                    };

                    if (existingId == null)
                    {
                        lock (_lock)
                        {
                            _runtimes.Add(runtime);
                        }
                        discovered.Add(runtime);
                    }
                    else
                    {
                        // Update existing runtime
                        lock (_lock)
                        {
                            var existing = _runtimes.First(r => r.Id == existingId);
                            existing.Status = runtime.Status;
                            existing.Version = runtime.Version;
                            existing.InstalledPackages = runtime.InstalledPackages;
                            existing.Warnings = runtime.Warnings;
                            existing.Errors = runtime.Errors;
                        }
                    }
                }

                _dmEditor?.AddLogMessage("Beep", $"Discovered {discovered.Count} new Python runtime(s)", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Error during runtime discovery: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }

            return discovered;
        }

        /// <summary>
        /// Updates runtime information after initialization or changes.
        /// </summary>
        public async Task UpdateRuntimeAsync(string runtimeId, Action<PythonRuntimeInfo> updateAction)
        {
            lock (_lock)
            {
                var runtime = _runtimes.FirstOrDefault(r => r.Id == runtimeId);
                if (runtime != null)
                {
                    updateAction(runtime);
                    runtime.LastUsed = DateTime.UtcNow;
                }
            }

            await SaveRuntimeConfigurationsAsync();
        }

        /// <summary>
        /// Loads runtime configurations from disk.
        /// </summary>
        private async Task LoadRuntimeConfigurationsAsync()
        {
            try
            {
                if (!File.Exists(_registryPath))
                    return;

                var json = await File.ReadAllTextAsync(_registryPath);
                var config = JsonConvert.DeserializeObject<RuntimeRegistryConfig>(json);

                if (config != null)
                {
                    lock (_lock)
                    {
                        _runtimes.Clear();
                        _runtimes.AddRange(config.Runtimes ?? new List<PythonRuntimeInfo>());

                        if (!string.IsNullOrEmpty(config.DefaultRuntimeId))
                        {
                            _defaultRuntime = _runtimes.FirstOrDefault(r => r.Id == config.DefaultRuntimeId);
                        }
                    }

                    _dmEditor?.AddLogMessage("Beep", $"Loaded {_runtimes.Count} runtime(s) from registry", DateTime.Now, 0, null, Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Failed to load runtime configurations: {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }

        /// <summary>
        /// Saves runtime configurations to disk.
        /// </summary>
        private async Task SaveRuntimeConfigurationsAsync()
        {
            try
            {
                var config = new RuntimeRegistryConfig
                {
                    Version = "1.0",
                    DefaultRuntimeId = _defaultRuntime?.Id,
                    Runtimes = _runtimes.ToList()
                };

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);

                var directory = Path.GetDirectoryName(_registryPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(_registryPath, json);
            }
            catch (Exception ex)
            {
                _dmEditor?.AddLogMessage("Beep", $"Failed to save runtime configurations: {ex.Message}", DateTime.Now, 0, null,     Errors.Failed);
            }
        }
    }

    /// <summary>
    /// Runtime registry configuration file format.
    /// </summary>
    public class RuntimeRegistryConfig
    {
        public string Version { get; set; }
        public string DefaultRuntimeId { get; set; }
        public List<PythonRuntimeInfo> Runtimes { get; set; }
    }

    /// <summary>
    /// Information about a Python runtime installation.
    /// </summary>
    public class PythonRuntimeInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PythonRuntimeType Type { get; set; }
        public string Path { get; set; }
        public string Version { get; set; }
        public bool IsManaged { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsed { get; set; }
        public PythonRuntimeStatus Status { get; set; }
        public Dictionary<string, string> InstalledPackages { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Type of Python runtime.
    /// </summary>
    public enum PythonRuntimeType
    {
        Embedded,
        System,
        Conda,
        VirtualEnv,
        Unknown
    }

    /// <summary>
    /// Status of a Python runtime.
    /// </summary>
    public enum PythonRuntimeStatus
    {
        NotInitialized,
        Ready,
        Error,
        Updating
    }

    /// <summary>
    /// Interface for Python runtime registry.
    /// </summary>
    public interface IPythonRuntimeRegistry
    {
        Task<bool> InitializeAsync();
        IEnumerable<PythonRuntimeInfo> GetAvailableRuntimes();
        PythonRuntimeInfo GetRuntime(string runtimeId);
        PythonRuntimeInfo GetDefaultRuntime();
        Task<bool> SetDefaultRuntimeAsync(string runtimeId);
        Task<string> RegisterManagedRuntimeAsync(string name, PythonRuntimeType type = PythonRuntimeType.Embedded);
        Task<bool> DeleteRuntimeAsync(string runtimeId);
        Task<List<PythonRuntimeInfo>> DiscoverRuntimesAsync();
        Task UpdateRuntimeAsync(string runtimeId, Action<PythonRuntimeInfo> updateAction);
    }
}
