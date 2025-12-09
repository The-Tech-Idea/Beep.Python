using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Environment = System.Environment;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Manages embedded Python runtime for LLM execution
/// </summary>
public partial class PythonHost : IPythonHost
{
    private readonly ILogger<PythonHost> _logger;
    private readonly IPythonRuntimeManager _runtimeManager;
    // Configuration manager removed - not needed for Python.Runtime independence
    private readonly IVenvManager _venvManager;  // Virtual environment manager (injectable, required)
    protected bool _isInitialized;
    private string? _pythonPath;
    private bool _packagesInstalled;
    private readonly Dictionary<string, string> _engineEnvironments = new();  // engine -> venv path
    private readonly Dictionary<string, string> _providerEnvironments = new();  // provider -> venv path
    private string? _currentProvider;
    private string? _currentProviderModelId;
    private string? _currentProviderVenvPath;

    public bool IsInitialized => _isInitialized;
    public string? PythonPath => _pythonPath;
    public string? CurrentProvider => _currentProvider;
    public string? CurrentProviderModelId => _currentProviderModelId;
    public string? CurrentProviderVenvPath => _currentProviderVenvPath;

    /// <summary>
    /// Get package installation status for a specific provider
    /// </summary>
    public async Task<List<ProviderPackageInfo>> GetProviderPackageStatus(string providerName, CancellationToken cancellationToken = default)
    {
        // IVenvManager is required and injected via DI

        return await _venvManager.GetProviderPackageStatus(providerName, cancellationToken);
    }

    public async Task<string?> GetEngineExecutable(string engineName, CancellationToken cancellationToken = default)
    {
        var envPath = await EnsureEngineEnvironment(engineName, cancellationToken);
        if (envPath == null) return null;

        // Check if this is a virtual environment path
        var venvPython = Path.Combine(envPath, "Scripts", "python.exe");
        if (File.Exists(venvPython))
        {
            return venvPython; // Virtual environment Python
        }

        // Fallback to direct python.exe (for backward compatibility)
        var directPython = Path.Combine(envPath, "python.exe");
        return File.Exists(directPython) ? directPython : null;
    }

    public async Task<string?> GetProviderExecutable(string providerName, string? modelId = null, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"[DEBUG-GPE] GetProviderExecutable called for provider: {providerName}, modelId: {modelId}");
        
        // FAST PATH: Check registry for existing environment without spawning processes
        foreach (var suffix in new[] { "-cuda", "-rocm", "-vulkan", "-cpu", "" })
        {
            var providerWithBackend = providerName + suffix;
            System.Console.WriteLine($"[DEBUG-GPE] Checking registry for: {providerWithBackend}");
            var registeredPath = _venvManager.GetRegisteredEnvironmentPath(providerWithBackend);
            System.Console.WriteLine($"[DEBUG-GPE]   Registry path: {registeredPath ?? "null"}");
            if (registeredPath != null && Directory.Exists(registeredPath))
            {
                var venvPython = Path.Combine(registeredPath, "Scripts", "python.exe");
                System.Console.WriteLine($"[DEBUG-GPE]   Checking python.exe: {venvPython}, exists: {File.Exists(venvPython)}");
                if (File.Exists(venvPython))
                {
                    _logger.LogDebug("Found registered Python executable for {Provider} at {Path}", providerWithBackend, venvPython);
                    System.Console.WriteLine($"[DEBUG-GPE] FOUND! Returning: {venvPython}");
                    return venvPython;
                }
            }
        }
        
        System.Console.WriteLine($"[DEBUG-GPE] Registry lookup failed, falling back to PrepareProviderEnvironment...");
        // Fallback to PrepareProviderEnvironment if not found in registry
        var envPath = await PrepareProviderEnvironment(providerName, modelId, false, cancellationToken);
        System.Console.WriteLine($"[DEBUG-GPE] PrepareProviderEnvironment returned: {envPath ?? "null"}");
        if (envPath == null) return null;

        // Virtual environment Python
        var fallbackPython = Path.Combine(envPath, "Scripts", "python.exe");
        System.Console.WriteLine($"[DEBUG-GPE] Fallback python exists: {File.Exists(fallbackPython)}");
        return File.Exists(fallbackPython) ? fallbackPython : null;
    }

    public async Task<string?> GetProviderBackend(string providerName, string? modelId = null, CancellationToken cancellationToken = default)
    {
        // FAST PATH: Check registry for existing environment without spawning processes
        foreach (var suffix in new[] { "-cuda", "-rocm", "-vulkan", "-cpu", "" })
        {
            var providerWithBackend = providerName + suffix;
            var registeredPath = _venvManager.GetRegisteredEnvironmentPath(providerWithBackend);
            if (registeredPath != null && Directory.Exists(registeredPath))
            {
                var dirName = Path.GetFileName(registeredPath);
                var backend = _venvManager.DetermineBackendFromProviderName(dirName);
                _logger.LogDebug("Found registered backend for {Provider}: {Backend}", providerWithBackend, backend);
                return backend;
            }
        }
        
        // Fallback to EnsureProviderEnvironment if not found in registry
        var envPath = await EnsureProviderEnvironment(providerName, modelId, cancellationToken);
        if (envPath == null) return null;
        var foundDirName = Path.GetFileName(envPath);
        return _venvManager.DetermineBackendFromProviderName(foundDirName);
    }

    public async Task<string?> PrepareProviderEnvironment(string providerName, string? modelId = null, bool forceRecreate = false, CancellationToken cancellationToken = default)
    {
        if (forceRecreate)
        {
            await RemoveProviderEnvironment(providerName, modelId, cancellationToken);
            return await EnsureProviderEnvironment(providerName, modelId, cancellationToken);
        }

        // FAST PATH: Check registry for existing environment without spawning processes
        foreach (var suffix in new[] { "-cuda", "-rocm", "-vulkan", "-cpu", "" })
        {
            var providerWithBackend = providerName + suffix;
            var registeredPath = _venvManager.GetRegisteredEnvironmentPath(providerWithBackend);
            if (registeredPath != null && Directory.Exists(registeredPath))
            {
                _logger.LogDebug("Found registered environment for {Provider} at {Path}", providerWithBackend, registeredPath);
                return registeredPath;
            }
        }

        // Fallback to EnsureProviderEnvironment if not found in registry
        return await EnsureProviderEnvironment(providerName, modelId, cancellationToken);
    }

    public async Task<bool> RemoveProviderEnvironment(string providerName, string? modelId = null, CancellationToken cancellationToken = default)
    {
        // IVenvManager is injected via DI

        providerName = await _venvManager.AppendBackendSuffix(providerName, cancellationToken);
        var cacheKey = GetProviderEnvCacheKey(providerName, modelId);

        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "providers");
        var venvName = _venvManager.ComputeProviderVenvName(providerName, modelId);
        var venvPath = Path.Combine(baseDir, venvName);

        if (!Directory.Exists(venvPath))
        {
            _providerEnvironments.Remove(cacheKey);
            _logger.LogInformation("Provider environment path not found for removal: {Path}", venvPath);
            return false;
        }

        try
        {
            var deleted = await _venvManager.DeleteVirtualEnvironment(venvPath, cancellationToken);
            if (deleted)
            {
                _providerEnvironments.Remove(cacheKey);
                _logger.LogInformation("Removed provider environment {Provider} at {Path}", providerName, venvPath);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to delete provider environment via VenvManager: {Path}", venvPath);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove provider environment {Provider} at {Path}", providerName, venvPath);
            return false;
        }
    }

    public string GetDefaultExecutable()
    {
        // Return main Python executable for backward compatibility
        return Path.Combine(_pythonPath!, "python.exe");
    }

    /// <summary>
    /// Get Python executable for the currently active provider/engine environment
    /// </summary>
    public async Task<string?> GetCurrentExecutable(CancellationToken cancellationToken = default)
    {
        // If a provider is currently active, return its executable
        if (!string.IsNullOrEmpty(_currentProvider))
        {
            _logger.LogDebug("Getting executable for current provider: {Provider}", _currentProvider);
            return await GetProviderExecutable(_currentProvider, null, cancellationToken);
        }

        // Fall back to default Python executable
        _logger.LogDebug("No active provider, using default executable");
        return GetDefaultExecutable();
    }

    public PythonHost(ILogger<PythonHost> logger, IPythonRuntimeManager runtimeManager, IVenvManager venvManager)
    {
        _logger = logger;
        _runtimeManager = runtimeManager;
        _venvManager = venvManager ?? throw new ArgumentNullException(nameof(venvManager));
    }

    // Compute provider environment cache key
    private static string GetProviderEnvCacheKey(string providerName, string? modelId)
    {
        return string.IsNullOrEmpty(modelId) ? providerName : $"{providerName}:{modelId}";
    }

    // Provider venv naming and ROCm strategy are handled by VenvManager

    public virtual async Task<bool> Initialize(string? pythonPath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing Python runtime...");

            // Use runtime manager to get or create default runtime
            var runtime = await GetOrCreateDefaultRuntime(pythonPath, cancellationToken);
            if (runtime == null)
            {
                _logger.LogError("Failed to setup Python environment");
                return false;
            }

            _pythonPath = runtime.Path;

            // Only initialize Python.NET if not already initialized
            if (!PythonEngine.IsInitialized)
            {
                // Set environment variables
                Environment.SetEnvironmentVariable("PYTHONHOME", _pythonPath);
                
                // Pass Hugging Face token to Python if available
                var hfToken = Environment.GetEnvironmentVariable("HUGGING_FACE_HUB_TOKEN");
                if (!string.IsNullOrEmpty(hfToken))
                {
                    Environment.SetEnvironmentVariable("HF_TOKEN", hfToken);
                    _logger.LogInformation("Hugging Face token set for Python runtime");
                }
                
                // Explicitly set the Python DLL for pythonnet
                var pythonDll = Path.Combine(_pythonPath, "python311.dll");
                if (!File.Exists(pythonDll))
                {
                    _logger.LogWarning("python311.dll not found at {path}. This might cause issues.", pythonDll);
                    // Fallback for different naming conventions or versions if necessary
                    var dlls = Directory.GetFiles(_pythonPath, "python3*.dll");
                    if (dlls.Any())
                    {
                        pythonDll = dlls.First();
                        _logger.LogInformation("Found Python DLL: {dll}", pythonDll);
                    }
                    else
                    {
                         _logger.LogError("Could not find any python3*.dll in {path}", _pythonPath);
                         return false;
                    }
                }
                Runtime.PythonDLL = pythonDll;

                var libPath = Path.Combine(_pythonPath, "Lib");
                var sitePackagesPath = Path.Combine(libPath, "site-packages");
                if (Directory.Exists(libPath))
                {
                    if (!Directory.Exists(sitePackagesPath)) Directory.CreateDirectory(sitePackagesPath);

                    PatchPthFile(_pythonPath); // ensure import site enabled and paths present

                    // Merge Lib and site-packages into PYTHONPATH
                    var currentPath = Environment.GetEnvironmentVariable("PYTHONPATH");
                    var newPathEntries = new[] { libPath, sitePackagesPath };
                    var updatedPath = string.Join(Path.PathSeparator.ToString(), newPathEntries
                        .Concat((currentPath ?? string.Empty)
                            .Split(Path.PathSeparator)
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .Where(p => !newPathEntries.Contains(p))));
                    Environment.SetEnvironmentVariable("PYTHONPATH", updatedPath);

                    // Also prepend to PATH so any native extensions are found
                    var envPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    if (!envPath.Split(Path.PathSeparator).Contains(_pythonPath))
                    {
                        Environment.SetEnvironmentVariable("PATH", _pythonPath + Path.PathSeparator + envPath);
                    }
                }

                // Add virtual environment site-packages to PYTHONPATH if they exist
                AddVirtualEnvironmentPaths();

                // Initialize Python.NET with timeout
                _logger.LogInformation("Initializing Python.NET engine...");
                
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout
                
                try
                {
                    await Task.Run(() =>
                    {
                        _logger.LogDebug("Starting PythonEngine.Initialize()...");
                        PythonEngine.Initialize();
                        _logger.LogDebug("Starting PythonEngine.BeginAllowThreads()...");
                        PythonEngine.BeginAllowThreads();
                        
                        // Set Hugging Face token in Python's os.environ
                        var hfToken = Environment.GetEnvironmentVariable("HUGGING_FACE_HUB_TOKEN");
                        if (!string.IsNullOrEmpty(hfToken))
                        {
                            using (Py.GIL())
                            {
                                dynamic os = Py.Import("os");
                                using (PyString pyToken = new PyString(hfToken))
                                {
                                    os.environ.SetItem("HUGGING_FACE_HUB_TOKEN", pyToken);
                                    os.environ.SetItem("HF_TOKEN", pyToken);
                                }
                                _logger.LogInformation("Set Hugging Face token in Python environment");
                            }
                        }
                        
                        _logger.LogDebug("Python.NET initialized successfully");
                    }, timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("Python.NET initialization timed out after 30 seconds");
                    return false;
                }
            }
            else
            {
                _logger.LogInformation("Python.NET already initialized, reusing existing runtime");
            }

            _isInitialized = true;
            
            // Initialize the backend (Python.NET, HTTP, Pipe, or RPC)
            await InitializeBackendAsync(cancellationToken);
            _logger.LogInformation("Python backend initialized: {BackendType}", _backendType);
            
            // IVenvManager should already be provided through DI at this point. If it isn't present, log error and fail.
            if (_venvManager == null)
            {
                _logger.LogError("IVenvManager not injected via DI; please register IVenvManager in DI container before creating PythonHost.");
                return false;
            }
            
            _logger.LogInformation("Python runtime initialized successfully");
            
            // Check if packages are already installed in this runtime
            _packagesInstalled = runtime.InstalledPackages.ContainsKey("transformers") && 
                                runtime.InstalledPackages.ContainsKey("torch");
            
            if (!_packagesInstalled)
            {
                _logger.LogInformation("Python packages will be installed in the background on first use");
            }
            else
            {
                _logger.LogInformation("Python packages already installed");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Python runtime");
            return false;
        }
    }

    private async Task<PythonRuntimeInfo?> GetOrCreateDefaultRuntime(string? pythonPath, CancellationToken cancellationToken)
    {
        try
        {
            // If specific path is provided, try to use it
            if (!string.IsNullOrEmpty(pythonPath))
            {
                var existingRuntime = _runtimeManager.GetAvailableRuntimes()
                    .FirstOrDefault(r => r.Path?.Equals(pythonPath, StringComparison.OrdinalIgnoreCase) == true);
                
                if (existingRuntime != null)
                {
                    return existingRuntime;
                }
                
                // Create new runtime from existing path
                var runtimeId = await _runtimeManager.CreateManagedRuntime(
                    $"Custom-{Path.GetFileName(pythonPath)}", 
                    PythonRuntimeType.System);
                
                return _runtimeManager.GetRuntime(runtimeId);
            }

            // Get or create default runtime
            var defaultRuntime = _runtimeManager.GetDefaultRuntime();
            if (defaultRuntime != null)
            {
                // Verify the runtime has a valid Python executable
                var pythonExe = Path.Combine(defaultRuntime.Path, "python.exe");
                if (File.Exists(pythonExe))
                {
                    _logger.LogInformation("Using existing default runtime: {RuntimeId}", defaultRuntime.Id);
                    return defaultRuntime;
                }
                else
                {
                    _logger.LogWarning("Default runtime python.exe not found, reinitializing...");
                }
            }

            // No valid runtime found - create new embedded runtime with full setup
            _logger.LogInformation("📥 No Python installation found. Setting up embedded Python environment...");
            _logger.LogInformation("   This will download Python 3.11, install pip, and setup virtual environment support.");
            _logger.LogInformation("   This is a one-time setup and may take a few minutes.");
            
            // Create new embedded runtime as default
            var defaultRuntimeId = await _runtimeManager.CreateManagedRuntime(
                "Default-Runtime", 
                PythonRuntimeType.Embedded);
            
            // Initialize the runtime (this triggers download, pip install, venv setup)
            var initSuccess = await _runtimeManager.InitializeRuntime(defaultRuntimeId);
            if (!initSuccess)
            {
                _logger.LogError("❌ Failed to initialize embedded Python runtime");
                return null;
            }
            
            var runtime = _runtimeManager.GetRuntime(defaultRuntimeId);
            if (runtime != null)
            {
                _logger.LogInformation("✅ Embedded Python environment setup completed successfully!");
                _logger.LogInformation("   Location: {Path}", runtime.Path);
                _logger.LogInformation("   Version: {Version}", runtime.Version);
            }
            
            return runtime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create default runtime");
            return null;
        }
    }

    private void AddVirtualEnvironmentPaths()
    {
        try
        {
            // Add all engine virtual environment site-packages to PYTHONPATH
            var enginesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "engines");
                if (Directory.Exists(enginesDir))
                {
                    _logger.LogDebug("Engines directory exists at {EnginesDir} - but will NOT set PYTHONPATH globally to avoid venv contamination.", enginesDir);
                    // Do not set PYTHONPATH globally here. Engines' venvs should be configured per-process or via ConfigureEngineEnvironment()
                }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add virtual environment paths to PYTHONPATH");
        }
    }

    private void PatchPthFile(string pythonDir)
    {
        try
        {
            var pthFile = Path.Combine(pythonDir, "python311._pth");
            if (!File.Exists(pthFile)) return;
            var lines = File.ReadAllLines(pthFile).Select(l => l.TrimEnd()).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("#import site")) lines[i] = "import site";
            }
            if (!lines.Any(l => l == "Lib")) lines.Add("Lib");
            if (!lines.Any(l => string.Equals(l, "Lib\\site-packages", StringComparison.OrdinalIgnoreCase))) lines.Add("Lib\\site-packages");
            if (!lines.Any(l => l == "import site")) lines.Add("import site");
            File.WriteAllLines(pthFile, lines);
            _logger.LogInformation("Patched _pth file during Initialize.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to patch python311._pth during Initialize");
        }
    }

    private async Task<bool> VerifyImport(string pythonExe, string module, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"-c \"import {module}; print('ok')\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return false;
            await proc.WaitForExitAsync(ct);
            var outTxt = await proc.StandardOutput.ReadToEndAsync(ct);
            return proc.ExitCode == 0 && outTxt.Contains("ok");
        }
        catch { return false; }
    }

    private async Task<bool> InstallSingle(string pythonExe, string package, CancellationToken ct, bool verbose = false)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = verbose ? $"-m pip install {package} --no-warn-script-location" : $"-m pip install {package} --quiet --no-warn-script-location",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return false;
            await proc.WaitForExitAsync(ct);
            if (proc.ExitCode != 0)
            {
                var err = await proc.StandardError.ReadToEndAsync(ct);
                _logger.LogWarning("InstallSingle failed for {Package}: {Err}", package, err);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception in InstallSingle for {Package}", package);
            return false;
        }
    }

    public async Task<PythonExecutionResult> ExecuteScript(
        string scriptPath,
        Dictionary<string, object>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing Python script: {ScriptPath}", scriptPath);

            using (Py.GIL())
            {
                dynamic scope = Py.CreateScope();

            // Set arguments
            if (arguments != null)
            {
                foreach (var arg in arguments)
                {
                    scope.SetAttr(arg.Key, arg.Value.ToPython());
                }
            }

            // Execute script - try file first, then embedded resource
            string scriptContent;
            if (File.Exists(scriptPath))
            {
                scriptContent = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            }
            else
            {
                // Try to find as embedded resource
                var fileName = Path.GetFileName(scriptPath);
                var embeddedContent = await ResourceManager.GetEmbeddedResourceAsString($"Python/{fileName}");
                if (embeddedContent == null)
                {
                    throw new FileNotFoundException($"Python script not found: {scriptPath}");
                }
                scriptContent = embeddedContent;
            }
            
            scope.Exec(scriptContent);                stopwatch.Stop();

                return new PythonExecutionResult
                {
                    Success = true,
                    Duration = stopwatch.Elapsed,
                    ExitCode = 0
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to execute Python script");

            return new PythonExecutionResult
            {
                Success = false,
                Error = ex.Message,
                Duration = stopwatch.Elapsed,
                ExitCode = 1
            };
        }
    }

    public async Task<PythonExecutionResult> ExecuteCode(string code, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Python runtime not initialized");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing Python code");

            await Task.Run(() =>
            {
                using (Py.GIL())
                {
                    PythonEngine.Exec(code);
                }
            }, cancellationToken);

            stopwatch.Stop();

            return new PythonExecutionResult
            {
                Success = true,
                Duration = stopwatch.Elapsed,
                ExitCode = 0
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to execute Python code");

            return new PythonExecutionResult
            {
                Success = false,
                Error = ex.Message,
                Duration = stopwatch.Elapsed,
                ExitCode = 1
            };
        }
    }

    /// <summary>
    /// Execute a Python script in a specific provider's virtual environment with proper isolation
    /// </summary>
    public async Task<(bool Success, string Output, string Error)> ExecuteScriptInVenv(
        string providerName,
        string scriptPath,
        string arguments = "",
        IProgress<string>? lineProgress = null,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"🔍 ExecuteScriptInVenv called with provider: {providerName}");
        
        var pythonExe = await GetProviderExecutable(providerName, null, cancellationToken);
        
        Console.WriteLine($"🔍 Python executable path: {pythonExe}");
        
        if (string.IsNullOrEmpty(pythonExe) || !File.Exists(pythonExe))
        {
            _logger.LogError("Python executable not found for provider: {Provider}", providerName);
            return (false, "", $"Python executable not found for {providerName}");
        }

        // Use VenvManager's RunScriptInVenv helper which handles venv isolation properly
        // IVenvManager is injected via DI
        var result = await _venvManager.RunScriptInVenv(pythonExe, scriptPath, arguments, lineProgress, cancellationToken);
        if (!result.Success)
        {
            _logger.LogError("Script execution in venv failed. Error: {Error}", result.Error);
        }
        else
        {
            _logger.LogInformation("Script executed in venv successfully: {Script}", scriptPath);
        }
        return result;
    }

    public async Task<bool> InstallPackages(IEnumerable<string> packages, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the current provider's Python executable (or default if no provider active)
            var pythonExe = await GetCurrentExecutable(cancellationToken);
            if (string.IsNullOrEmpty(pythonExe) || !File.Exists(pythonExe))
            {
                _logger.LogError("Python executable not found for package installation");
                return false;
            }

            // IVenvManager is injected via DI
            
            // Check if current provider is CUDA-based - if so, use extra-index-url to prevent torch CPU override
            var isCudaProvider = _currentProvider?.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase) == true;
            var extraIndexUrl = isCudaProvider ? " --extra-index-url https://download.pytorch.org/whl/cu124" : "";
            
            _logger.LogInformation("InstallPackages: Provider={Provider}, IsCuda={IsCuda}", _currentProvider ?? "none", isCudaProvider);
            Console.WriteLine($"📦 InstallPackages: Provider={_currentProvider ?? "none"}, IsCuda={isCudaProvider}");

            _logger.LogInformation("Installing Python packages using {Executable}: {Packages}", 
                pythonExe, string.Join(", ", packages));

            foreach (var package in packages)
            {
                string pipCmd;
                
                // Special handling for unsloth - must use correct version for torch/CUDA combination
                if (package.StartsWith("unsloth", StringComparison.OrdinalIgnoreCase) && isCudaProvider)
                {
                    Console.WriteLine("  🦥 Installing Unsloth with correct torch/CUDA variant...");
                    // For torch 2.5.1 + CUDA 12.4, use cu124-torch251
                    pipCmd = "-m pip install \"unsloth[cu124-torch251] @ git+https://github.com/unslothai/unsloth.git\" --no-deps";
                    
                    if (!await _venvManager.RunPipCommand(pythonExe, pipCmd, cancellationToken))
                    {
                        _logger.LogError("Failed to install package: {Package}", package);
                        return false;
                    }
                    
                    // Also install unsloth's non-triton dependencies
                    Console.WriteLine("  📦 Installing Unsloth dependencies...");
                    await _venvManager.RunPipCommand(pythonExe, $"-m pip install packaging psutil{extraIndexUrl}", cancellationToken);
                    continue;
                }
                
                pipCmd = $"-m pip install {package}{extraIndexUrl}";
                
                // Use VenvManager's RunPipCommand which handles venv isolation properly
                if (!await _venvManager.RunPipCommand(pythonExe, pipCmd, cancellationToken))
                {
                    _logger.LogError("Failed to install package: {Package}", package);
                    return false;
                }
            }

            // CRITICAL: For CUDA providers, force reinstall torch with CUDA to ensure packages didn't override with CPU version
            if (isCudaProvider)
            {
                Console.WriteLine("🔧 Ensuring PyTorch CUDA version after package installation...");
                _logger.LogInformation("🔧 Ensuring PyTorch CUDA version after package installation...");
                
                // Use --upgrade instead of --force-reinstall to avoid locked file issues
                // Pin to 2.5.1 to ensure compatibility with available Windows Triton wheels
                var torchReinstall = await _venvManager.RunPipCommand(pythonExe, 
                    "-m pip install torch==2.5.1 torchvision==0.20.1 torchaudio==2.5.1 --index-url https://download.pytorch.org/whl/cu124 --upgrade --no-deps", 
                    cancellationToken);
                
                if (torchReinstall)
                {
                    Console.WriteLine("✅ PyTorch CUDA version installed");
                    _logger.LogInformation("✅ PyTorch CUDA version installed");
                    
                    // Note: Triton is installed automatically by unsloth/xformers as a dependency
                    // We no longer install it explicitly to avoid version conflicts
                }
                else
                {
                    Console.WriteLine("⚠️ Failed to ensure PyTorch CUDA - GPU acceleration may not work");
                    _logger.LogWarning("⚠️ Failed to ensure PyTorch CUDA - GPU acceleration may not work");
                }
            }
            else
            {
                Console.WriteLine($"ℹ️ Skipping CUDA torch reinstall (provider: {_currentProvider}, isCuda: {isCudaProvider})");
            }

            _logger.LogInformation("Packages installed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install packages");
            return false;
        }
    }

    /// <summary>
    /// Install packages on demand (called when first needed)
    /// </summary>
    public async Task<bool> EnsurePackagesInstalledOnDemand(CancellationToken cancellationToken = default)
    {
        if (_packagesInstalled)
            return true;

        _logger.LogInformation("Installing Python packages on demand...");
        await EnsurePackagesInstalled(cancellationToken);
        
        var pythonExe = Path.Combine(_pythonPath!, "python.exe");
        
        // Verify critical packages
        var criticalPackages = new[] { "numpy", "transformers", "huggingface_hub" };
        
        foreach (var package in criticalPackages)
        {
            if (!await VerifyImport(pythonExe, package, cancellationToken))
            {
                _logger.LogWarning("{Package} not importable after installation; attempting reinstall.", package);
                var packageToInstall = package == "huggingface_hub" ? "huggingface-hub" : package;
                await InstallSingle(pythonExe, packageToInstall, cancellationToken, true);
            }
        }
        
        return _packagesInstalled;
    }

    /// <summary>
    /// Configures Python.NET environment for a specific engine with proper module paths
    /// </summary>
    /// <param name="engineName">Name of the engine (huggingface, ollama, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if configuration was successful</returns>
    public async Task<bool> ConfigureEngineEnvironment(string engineName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isInitialized)
            {
                _logger.LogError("Python host not initialized. Call Initialize() first.");
                return false;
            }

            // Get the engine's virtual environment path
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "engines");
            var venvPath = Path.Combine(baseDir, engineName);
            var venvSitePackages = Path.Combine(venvPath, "Lib", "site-packages");

            // Get Python scripts directory
            var pythonDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python");

            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                
                // Clear any previous paths to avoid conflicts
                var currentPaths = new List<string>();
                foreach (var path in sys.path)
                {
                    currentPaths.Add((string)path);
                }

                // Add virtual environment site-packages path at the beginning (highest priority)
                if (Directory.Exists(venvSitePackages))
                {
                    var venvPathStr = venvSitePackages.Replace("\\", "/");
                    if (!currentPaths.Contains(venvPathStr))
                    {
                        sys.path.insert(0, venvPathStr);
                        _logger.LogDebug("Added {Engine} virtual environment site-packages to Python path: {Path}", engineName, venvPathStr);
                    }
                }

                // Add Python scripts directory
                if (Directory.Exists(pythonDir))
                {
                    var pythonDirStr = pythonDir.Replace("\\", "/");
                    if (!currentPaths.Contains(pythonDirStr))
                    {
                        sys.path.append(pythonDirStr);
                        _logger.LogDebug("Added Python scripts directory to path: {Path}", pythonDirStr);
                    }
                }

                // Log current Python path for debugging
                _logger.LogTrace("Current Python sys.path for {Engine}:", engineName);
                foreach (var path in sys.path)
                {
                    _logger.LogTrace("  - {Path}", (string)path);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure Python environment for engine {Engine}", engineName);
            return false;
        }
    }

    public async Task<bool> ConfigureProviderEnvironment(string providerName, string? modelId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_isInitialized)
            {
                _logger.LogError("Python host not initialized. Call Initialize() first.");
                return false;
            }

            // Use unified PrepareProviderEnvironment which handles backend suffix detection internally
            var providerPath = await PrepareProviderEnvironment(providerName, modelId, false, cancellationToken);
            if (providerPath == null)
            {
                _logger.LogWarning("Failed to prepare provider environment for {Provider}", providerName);
                return false;
            }

            // Get provider's site-packages path
            var venvSitePackages = Path.Combine(providerPath, "Lib", "site-packages");

            // Get Python scripts directory
            var pythonDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python");

            using (Py.GIL())
            {
                dynamic sys = Py.Import("sys");
                
                // Get current paths
                var currentPaths = new List<string>();
                foreach (var path in sys.path)
                {
                    currentPaths.Add((string)path);
                }

                // Add provider's site-packages at the HIGHEST priority (index 0)
                if (Directory.Exists(venvSitePackages))
                {
                    var venvPathStr = venvSitePackages.Replace("\\", "/");
                    
                    // Remove if already exists (to avoid duplicates)
                    if (currentPaths.Contains(venvPathStr))
                    {
                        sys.path.remove(venvPathStr);
                    }
                    
                    // Insert at beginning for highest priority
                    sys.path.insert(0, venvPathStr);
                    _logger.LogInformation("Configured provider '{Provider}' environment: {Path}", providerName, venvPathStr);
                }
                else
                {
                    _logger.LogWarning("Provider '{Provider}' site-packages not found: {Path}", providerName, venvSitePackages);
                }

                // Ensure Python scripts directory is in path
                if (Directory.Exists(pythonDir))
                {
                    var pythonDirStr = pythonDir.Replace("\\", "/");
                    if (!currentPaths.Contains(pythonDirStr))
                    {
                        sys.path.append(pythonDirStr);
                    }
                }

                // Log configured Python path
                _logger.LogDebug("Python sys.path after configuring provider '{Provider}':", providerName);
                foreach (var path in sys.path)
                {
                    _logger.LogDebug("  - {Path}", (string)path);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure provider environment for {Provider}", providerName);
            return false;
        }
    }

    public Task Shutdown()
    {
        if (_isInitialized && PythonEngine.IsInitialized)
        {
            _logger.LogInformation("Shutting down Python runtime");
            try
            {
                // On .NET 8+, PythonEngine.Shutdown() uses BinaryFormatter which is removed.
                // We skip the call entirely - the runtime will be cleaned up on process exit.
                // This is a known Python.NET limitation with modern .NET versions.
#if NET8_0_OR_GREATER
                _logger.LogDebug("Skipping PythonEngine.Shutdown() on .NET 8+ to avoid BinaryFormatter error");
                // Just release the GIL if we have it, don't call full shutdown
                try
                {
                    using (Py.GIL()) { /* Release any held references */ }
                }
                catch { /* Ignore GIL errors */ }
#else
                PythonEngine.Shutdown();
#endif
            }
            catch (Exception ex)
            {
                // Log at debug level since this is expected on .NET 8+
                _logger.LogDebug(ex, "Python shutdown encountered an expected error on .NET 8+");
            }
            _isInitialized = false;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears a provider from the environment cache
    /// </summary>
    public void ClearProviderCache(string providerName)
    {
        var keysToRemove = _providerEnvironments.Keys
            .Where(k =>
                k.Equals(providerName, StringComparison.OrdinalIgnoreCase) ||
                k.StartsWith(providerName + ":", StringComparison.OrdinalIgnoreCase) ||
                k.StartsWith(providerName + "-", StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var key in keysToRemove)
        {
            _providerEnvironments.Remove(key);
            _logger.LogInformation("Cleared cache for provider: {ProviderKey}", key);
        }
    }

    /// <summary>
    /// Switch Python.NET to use a specific provider's environment
    /// </summary>
    public async Task<bool> SwitchToProvider(string providerName, string? modelId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // FAST PATH: Check registry for existing environment without spawning processes
            // The registry stores the actual venv path, so we use that directly
            string? providerPath = null;
            string? foundProviderWithBackend = null;
            
            foreach (var suffix in new[] { "-cuda", "-rocm", "-vulkan", "-cpu", "" })
            {
                var providerWithBackend = providerName + suffix;
                // GetRegisteredEnvironmentPath returns the stored path from registry, or null if not found
                var registeredPath = _venvManager.GetRegisteredEnvironmentPath(providerWithBackend);
                if (registeredPath != null && Directory.Exists(registeredPath))
                {
                    providerPath = registeredPath;
                    foundProviderWithBackend = providerWithBackend;
                    _logger.LogDebug("Found registered environment for {Provider} at {Path}", providerWithBackend, providerPath);
                    break;
                }
            }
            
            // Fallback to full PrepareProviderEnvironment if not found in registry
            if (providerPath == null)
            {
                _logger.LogInformation("Provider {Provider} not in registry, preparing environment...", providerName);
                providerPath = await PrepareProviderEnvironment(providerName, modelId, false, cancellationToken);
            }
            
            if (providerPath == null)
            {
                _logger.LogError("Failed to prepare provider environment for {Provider}", providerName);
                return false;
            }

            // Extract the final provider name with backend suffix from the path
            var providerDirName = Path.GetFileName(providerPath);
            
            // If already on this provider, nothing to do
            if (_currentProvider == providerDirName && _isInitialized)
            {
                _logger.LogInformation("Already using provider: {Provider}", providerDirName);
                return true;
            }

            _logger.LogInformation("Switching from provider '{OldProvider}' to '{NewProvider}'", 
                _currentProvider ?? "none", providerDirName);

            // Initialize Python.NET if not already initialized
            if (!_isInitialized || !PythonEngine.IsInitialized)
            {
                _logger.LogInformation("Initializing Python.NET for first time...");
                var initSuccess = await Initialize();
                if (!initSuccess)
                {
                    _logger.LogError("Failed to initialize Python runtime");
                    return false;
                }
            }

            // Get provider's site-packages path
            var venvSitePackages = Path.Combine(providerPath, "Lib", "site-packages");

            // CRITICAL: Clear module cache and update sys.path to switch to new provider
            if (PythonEngine.IsInitialized)
            {
                using (Py.GIL())
                {
                    try
                    {
                    dynamic sys = Py.Import("sys");
                    
                    // 1. Clear cached modules from old provider
                    var modulesToClear = new List<string>();
                    foreach (var key in sys.modules.keys())
                    {
                        string moduleName = (string)key;
                        // Clear provider-specific modules
                        if (moduleName.StartsWith("torch") || 
                            moduleName.StartsWith("transformers") || 
                            moduleName.StartsWith("tokenizers") ||
                            moduleName.StartsWith("accelerate") ||
                            moduleName.StartsWith("safetensors") ||
                            moduleName.StartsWith("sentence_transformers"))
                        {
                            modulesToClear.Add(moduleName);
                        }
                    }
                    
                    foreach (var moduleName in modulesToClear)
                    {
                        sys.modules.pop(moduleName, null);
                    }
                    
                    _logger.LogInformation("Cleared {Count} cached modules", modulesToClear.Count);

                    // 2. Update sys.path to prioritize new provider
                    var venvPathStr = venvSitePackages.Replace("\\", "/");
                    
                    // Remove old provider paths
                    var pathsToRemove = new List<dynamic>();
                    foreach (var path in sys.path)
                    {
                        string pathStr = (string)path;
                        if (pathStr.Contains(".beep-llm\\providers\\") || pathStr.Contains(".beep-llm/providers/"))
                        {
                            pathsToRemove.Add(path);
                        }
                    }
                    
                    foreach (var path in pathsToRemove)
                    {
                        sys.path.remove(path);
                    }
                    
                    // Insert new provider path at highest priority
                    sys.path.insert(0, venvPathStr);
                    
                    _logger.LogInformation("Updated sys.path - new provider at index 0: {Path}", venvPathStr);
                    
                    // Log first few paths for verification
                    _logger.LogDebug("Python sys.path after switch:");
                    int count = 0;
                    foreach (var path in sys.path)
                    {
                        if (count++ < 5)
                        {
                            _logger.LogDebug("  [{Index}] {Path}", count - 1, (string)path);
                        }
                    }
                    }
                    catch (Exception ex)
                    {
                    _logger.LogError(ex, "Failed to update Python environment for provider switch");
                    return false;
                    }
                }
            }

            // Update current provider with the backend-suffixed name
            _currentProvider = providerDirName;
            _currentProviderModelId = modelId;
            _currentProviderVenvPath = providerPath;
            
            _logger.LogInformation("Successfully switched to provider: {Provider}", providerDirName);
            _logger.LogInformation("Provider environment: {Path}", providerPath);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching to provider {Provider}", providerName);
            return false;
        }
    }

    /// <summary>
    /// Clears an engine from the environment cache
    /// </summary>
    public void ClearEngineCache(string engineName)
    {
        if (_engineEnvironments.ContainsKey(engineName))
        {
            _engineEnvironments.Remove(engineName);
            _logger.LogInformation("Cleared cache for engine: {Engine}", engineName);
        }
    }

    private string? FindPythonPath()
    {
        _logger.LogInformation("Searching for Python installation...");
        
        // Check common Windows locations
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "python"),
            "./python-embed",
            Path.Combine(Environment.CurrentDirectory, "python-embed"),
            @"C:\Python311",
            @"C:\Python310",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311")
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                _logger.LogDebug("Checking Python path: {Path}", path);
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "python.exe")))
                {
                    _logger.LogInformation("Found Python at: {Path}", path);
                    return path;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error checking path {Path}: {Error}", path, ex.Message);
            }
        }

        _logger.LogWarning("Python path not found in common locations");
        return null;
    }

    private async Task<string?> DownloadEmbeddedPython(CancellationToken cancellationToken)
    {
        try
        {
            var pythonDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".beep-llm",
                "python"
            );

            // Check if already downloaded
            if (Directory.Exists(pythonDir) && File.Exists(Path.Combine(pythonDir, "python.exe")))
            {
                _logger.LogInformation("Embedded Python already exists at: {Path}", pythonDir);
                return pythonDir;
            }

            _logger.LogInformation("Downloading Python embedded distribution...");
            Directory.CreateDirectory(pythonDir);

            // Download Python 3.11 embedded (64-bit)
            var pythonUrl = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip";
            var zipPath = Path.Combine(pythonDir, "python.zip");

            using var client = new HttpClient();
            var response = await client.GetAsync(pythonUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var fileStream = File.Create(zipPath);
            await response.Content.CopyToAsync(fileStream, cancellationToken);
            fileStream.Close();

            _logger.LogInformation("Extracting Python...");
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, pythonDir);
            File.Delete(zipPath);

            // Patch embedded distribution _pth file to enable site-packages
            try
            {
                var pthFile = Directory.GetFiles(pythonDir, "python*.?_pth").FirstOrDefault();
                if (pthFile == null)
                    pthFile = Directory.GetFiles(pythonDir, "python*.?_pth").FirstOrDefault(f => f.EndsWith("python311._pth")) ?? Path.Combine(pythonDir, "python311._pth");

                if (File.Exists(pthFile))
                {
                    var lines = File.ReadAllLines(pthFile).Select(l => l.TrimEnd()).ToList();
                    // Remove comment marker preventing site import if present
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith("#import site"))
                            lines[i] = "import site"; // enable site
                    }
                    if (!lines.Any(l => l == "Lib")) lines.Add("Lib");
                    if (!lines.Any(l => string.Equals(l, "Lib\\site-packages", StringComparison.OrdinalIgnoreCase))) lines.Add("Lib\\site-packages");
                    if (!lines.Any(l => l == "import site")) lines.Add("import site");
                    File.WriteAllLines(pthFile, lines);
                    _logger.LogInformation("Patched embedded python _pth file: {File}", pthFile);

                    var sitePackages = Path.Combine(pythonDir, "Lib", "site-packages");
                    if (!Directory.Exists(sitePackages))
                    {
                        Directory.CreateDirectory(sitePackages);
                        _logger.LogInformation("Created missing site-packages directory: {Dir}", sitePackages);
                    }
                }
                else
                {
                    _logger.LogWarning("Embedded python _pth file not found; will attempt import site via PYTHONPATH only.");
                }
            }
            catch (Exception exPatch)
            {
                _logger.LogWarning(exPatch, "Failed to patch embedded python _pth file");
            }

            // Download and install pip
            _logger.LogInformation("Installing pip...");
            await InstallPip(pythonDir, cancellationToken);

            // Install venv module for virtual environment support
            _logger.LogInformation("Installing venv module for virtual environment support...");
            await InstallVenvModule(pythonDir, cancellationToken);

            _logger.LogInformation("Python setup complete at: {Path}", pythonDir);
            return pythonDir;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download embedded Python");
            return null;
        }
    }

    private async Task InstallVenvModule(string pythonDir, CancellationToken cancellationToken)
    {
        try
        {
            var pythonExe = Path.Combine(pythonDir, "python.exe");
            
            if (!File.Exists(pythonExe))
            {
                _logger.LogError("❌ Python executable not found at: {Path}", pythonExe);
                return;
            }

            _logger.LogInformation("🔧 Setting up virtual environment capabilities...");
            
            // First upgrade pip
            _logger.LogInformation("⬆️ Upgrading pip...");
            var pipProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m pip install --upgrade pip setuptools wheel",
                    WorkingDirectory = pythonDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            pipProcess.Start();
            var pipOutput = await pipProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            var pipError = await pipProcess.StandardError.ReadToEndAsync(cancellationToken);
            await pipProcess.WaitForExitAsync(cancellationToken);

            if (pipProcess.ExitCode == 0)
            {
                _logger.LogInformation("✅ pip upgraded successfully");
            }
            else
            {
                _logger.LogWarning("⚠️ pip upgrade failed: {Error}", pipError);
            }

            // Always install virtualenv as it works better with embedded Python
            _logger.LogInformation("📦 Installing virtualenv package (required for virtual environments)...");
            var virtualenvProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m pip install --upgrade virtualenv",
                    WorkingDirectory = pythonDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            virtualenvProcess.Start();
            var venvOutput = await virtualenvProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            var venvError = await virtualenvProcess.StandardError.ReadToEndAsync(cancellationToken);
            await virtualenvProcess.WaitForExitAsync(cancellationToken);
            
            if (virtualenvProcess.ExitCode == 0)
            {
                _logger.LogInformation("✅ virtualenv package installed successfully");
                
                // Verify installation by trying to import it
                var verifyProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = "-c \"import virtualenv; print('virtualenv version:', virtualenv.__version__)\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                verifyProcess.Start();
                var verifyOutput = await verifyProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                await verifyProcess.WaitForExitAsync(cancellationToken);
                
                if (verifyProcess.ExitCode == 0)
                {
                    _logger.LogInformation("✅ Verified: {Output}", verifyOutput.Trim());
                }
            }
            else
            {
                _logger.LogError("❌ Failed to install virtualenv: {Error}", venvError);
                _logger.LogError("   Output: {Output}", venvOutput);
            }

            // Test venv capability (won't work with embedded Python, but check anyway)
            var testProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m venv --help",
                    WorkingDirectory = pythonDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            testProcess.Start();
            await testProcess.WaitForExitAsync(cancellationToken);

            if (testProcess.ExitCode == 0)
            {
                _logger.LogInformation("✅ Built-in venv module available");
            }
            else
            {
                _logger.LogInformation("ℹ️ Built-in venv not available (expected for embedded Python), using virtualenv package");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to setup virtual environment capabilities");
        }
    }

    private async Task InstallPip(string pythonDir, CancellationToken cancellationToken)
    {
        try
        {
            var getPipUrl = "https://bootstrap.pypa.io/get-pip.py";
            var getPipPath = Path.Combine(pythonDir, "get-pip.py");

            using var client = new HttpClient();
            var getPipScript = await client.GetStringAsync(getPipUrl, cancellationToken);
            await File.WriteAllTextAsync(getPipPath, getPipScript, cancellationToken);

            // Run get-pip.py
            var pythonExe = Path.Combine(pythonDir, "python.exe");
            var processInfo = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{getPipPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = pythonDir
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync(cancellationToken);
                var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                _logger.LogDebug("pip installation output: {Output}", output);
            }

            File.Delete(getPipPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install pip");
            throw;
        }
    }

    private async Task EnsurePackagesInstalled(CancellationToken cancellationToken)
    {
        try
        {
            // Check if packages are already installed by testing critical imports
            var markerFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".beep-llm",
                ".packages-installed"
            );

            var huggingfaceEnvPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "engines", "huggingface");
            var huggingfacePython = Path.Combine(huggingfaceEnvPath, "Scripts", "python.exe");
            var huggingfaceMarker = Path.Combine(huggingfaceEnvPath, ".huggingface-installed");
            var huggingfaceReady = File.Exists(huggingfacePython) && File.Exists(huggingfaceMarker);

            var pythonExe = huggingfaceReady ? huggingfacePython : Path.Combine(_pythonPath!, "python.exe");
            if (!File.Exists(pythonExe))
            {
                pythonExe = huggingfaceReady ? huggingfacePython : "python";
            }

            // Verify critical packages are actually importable
            bool allPackagesWorking = File.Exists(markerFile);
            if (allPackagesWorking)
            {
                // FAST PATH: Trust the marker file if it exists.
                // Checking imports via python process can cause file locking issues (Access Denied)
                // and slows down startup significantly.
                if (huggingfaceReady)
                {
                    _engineEnvironments["huggingface"] = huggingfaceEnvPath;
                }
                _logger.LogInformation("Required packages already installed (marker found). Skipping verification.");
                _packagesInstalled = true;
                return;
            }

            // Clean up conflicting installations first
            _logger.LogInformation("Cleaning up any conflicting package installations...");
            await CleanupConflictingPackages(pythonExe, cancellationToken);

            if (_venvManager == null)
            {
                _logger.LogError("VenvManager not initialized");
                return;
            }

            // Upgrade pip first to ensure compatibility
            _logger.LogInformation("Step 1/2: Upgrading pip to latest version...");
            var pipSuccess = await _venvManager.RunPipCommand(pythonExe, "-m pip install --upgrade pip", cancellationToken);
            if (pipSuccess)
            {
                _logger.LogInformation("✓ Pip upgraded successfully");
            }
            else
            {
                _logger.LogWarning("⚠ Pip upgrade failed, continuing with existing version");
            }
            
            _logger.LogInformation("Step 2/2: Installing Python packages...");

            // Install default HuggingFace packages for backward compatibility
            _logger.LogInformation("🚀 Installing default HuggingFace engine packages...");
            _logger.LogInformation("📋 This includes: PyTorch, Transformers, Accelerate (Phi-3, Llama-3, etc.)");
            _logger.LogInformation("🔄 Additional engines (Ollama, Llama.cpp, Gemini) install on-demand");
            _logger.LogInformation("⏱️ Installation time varies: 3-15 minutes depending on internet speed");

            var defaultEnvPath = await EnsureEngineEnvironment("huggingface", cancellationToken);
            if (defaultEnvPath == null)
            {
                _logger.LogError("❌ Failed to install HuggingFace engine packages");
                return;
            }

            // Mark as successfully installed
            if (defaultEnvPath != null)
            {
                var markerDir = Path.GetDirectoryName(markerFile);
                if (markerDir != null)
                {
                    Directory.CreateDirectory(markerDir);
                    await File.WriteAllTextAsync(markerFile, DateTime.UtcNow.ToString(), cancellationToken);
                }
                _packagesInstalled = true;
                _logger.LogInformation("✅ Default HuggingFace environment ready! Additional engines available on-demand.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install required packages");
            // Don't throw - let the app continue, providers will fail if packages are missing
        }
    }

    public async Task<string?> EnsureEngineEnvironment(string engineName, CancellationToken cancellationToken = default)
    {
        if (_engineEnvironments.TryGetValue(engineName, out var existingPath))
        {
            return existingPath;
        }
            // IVenvManager is injected via DI

        var venvPath = await _venvManager.EnsureEngineEnvironment(engineName, cancellationToken);
        if (venvPath == null)
        {
            _logger.LogError("Failed to ensure engine environment for {Engine}", engineName);
            return null;
        }

        _engineEnvironments[engineName] = venvPath;
        return venvPath;
    }

    public virtual async Task<string?> EnsureProviderEnvironment(string providerName, string? modelId = null, CancellationToken cancellationToken = default)
    {
        if (_venvManager == null)
        {
            _logger.LogError("VenvManager not initialized");
            return null;
        }
     //   providerName = await _venvManager.AppendBackendSuffix(providerName, cancellationToken);
        var cacheKey = GetProviderEnvCacheKey(providerName, modelId);
        if (_providerEnvironments.TryGetValue(cacheKey, out var existingPath))
        {
            return existingPath;
        }

        var venvPath = await _venvManager.EnsureProviderEnvironment(providerName, modelId, cancellationToken);
        if (venvPath == null)
        {
            _logger.LogError("Failed to ensure provider environment for {Provider}", providerName);
            return null;
        }
        _providerEnvironments[cacheKey] = venvPath;
        return venvPath;
    }








    private async Task CleanupConflictingPackages(string pythonExe, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for conflicting package versions...");
        
        // IVenvManager is injected via DI
        
        // Packages that often have version conflicts
        var conflictingPackages = new[] { 
            "huggingface-hub", "transformers", "tokenizers", "torch", 
            "google-generativeai"  // Legacy Google package that conflicts with google-genai
        };
        
        foreach (var package in conflictingPackages)
        {
            try
            {
                _logger.LogInformation("Uninstalling potentially conflicting package: {Package}", package);
                var uninstallArgs = $"-m pip uninstall -y {package}";
                await _venvManager.RunPipCommand(pythonExe, uninstallArgs, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to cleanup {Package}: {Error}", package, ex.Message);
            }
        }
    }

    // Embedding model download is delegated to VenvManager -> DownloadEmbeddingModel
    // Python Execution Abstraction Layer is in PythonHost.Abstraction.cs (partial class)
}
