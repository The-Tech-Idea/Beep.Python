using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Environment = System.Environment;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Manages multiple Python runtime environments
/// </summary>
public class PythonRuntimeManager : IPythonRuntimeManager
{
    private readonly ILogger<PythonRuntimeManager> _logger;
    private readonly ConcurrentDictionary<string, PythonRuntimeInfo> _runtimes;
    private readonly string _runtimesConfigPath;
    private PythonRuntimeInfo? _defaultRuntime;

    public PythonRuntimeManager(ILogger<PythonRuntimeManager> logger)
    {
        _logger = logger;
        _runtimes = new ConcurrentDictionary<string, PythonRuntimeInfo>();
        _runtimesConfigPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".beep-llm",
            "python-runtimes.json"
        );
    }

    public async Task<bool> Initialize()
    {
        try
        {
            _logger.LogInformation("Initializing Python Runtime Manager...");
            
            // Load existing runtime configurations
            await LoadRuntimeConfigurations();
            
            // Discover available Python installations
            await DiscoverPythonRuntimes();
            
            // Ensure at least one runtime is available
            if (!_runtimes.Any())
            {
                _logger.LogInformation("No Python runtimes found. Creating embedded runtime...");
                await CreateEmbeddedRuntime();
            }

            // Set default runtime if none exists
            if (_defaultRuntime == null && _runtimes.Any())
            {
                _defaultRuntime = _runtimes.Values.First();
                _logger.LogInformation("Set default runtime: {RuntimeId}", _defaultRuntime.Id);
            }

            await SaveRuntimeConfigurations();
            
            _logger.LogInformation("Python Runtime Manager initialized with {Count} runtimes", _runtimes.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Python Runtime Manager");
            return false;
        }
    }

    public IEnumerable<PythonRuntimeInfo> GetAvailableRuntimes()
    {
        return _runtimes.Values.ToList();
    }

    public PythonRuntimeInfo? GetRuntime(string runtimeId)
    {
        _runtimes.TryGetValue(runtimeId, out var runtime);
        return runtime;
    }

    public PythonRuntimeInfo? GetDefaultRuntime()
    {
        return _defaultRuntime;
    }

    public async Task<bool> SetDefaultRuntime(string runtimeId)
    {
        if (_runtimes.TryGetValue(runtimeId, out var runtime))
        {
            _defaultRuntime = runtime;
            await SaveRuntimeConfigurations();
            _logger.LogInformation("Default runtime changed to: {RuntimeId}", runtimeId);
            return true;
        }
        return false;
    }

    public async Task<string> CreateManagedRuntime(string name, PythonRuntimeType type = PythonRuntimeType.Embedded)
    {
        var runtimeId = Guid.NewGuid().ToString("N")[..8];
        
        // Use fixed path for embedded Python (base runtime for the app)
        var runtimeDir = type == PythonRuntimeType.Embedded
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "python")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "runtimes", runtimeId);

        var runtime = new PythonRuntimeInfo
        {
            Id = runtimeId,
            Name = name,
            Type = type,
            Path = runtimeDir,
            Version = "3.11.9",
            IsManaged = true,
            CreatedAt = DateTime.UtcNow,
            Status = PythonRuntimeStatus.NotInitialized,
            InstalledPackages = new Dictionary<string, string>()
        };

        _runtimes[runtimeId] = runtime;
        await SaveRuntimeConfigurations();

        _logger.LogInformation("Created managed runtime: {Name} ({Id}) at {Path}", name, runtimeId, runtimeDir);
        return runtimeId;
    }

    public async Task<bool> DeleteRuntime(string runtimeId)
    {
        if (_runtimes.TryGetValue(runtimeId, out var runtime) && runtime.IsManaged)
        {
            try
            {
                if (Directory.Exists(runtime.Path))
                {
                    Directory.Delete(runtime.Path, true);
                }

                _runtimes.TryRemove(runtimeId, out _);

                if (_defaultRuntime?.Id == runtimeId)
                {
                    _defaultRuntime = _runtimes.Values.FirstOrDefault();
                }

                await SaveRuntimeConfigurations();
                _logger.LogInformation("Deleted runtime: {RuntimeId}", runtimeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete runtime: {RuntimeId}", runtimeId);
                return false;
            }
        }
        return false;
    }

    public async Task<bool> InitializeRuntime(string runtimeId)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime))
            return false;

        try
        {
            _logger.LogInformation("Initializing runtime: {RuntimeId}", runtimeId);
            
            runtime.Status = PythonRuntimeStatus.Initializing;

            if (runtime.Type == PythonRuntimeType.Embedded && !Directory.Exists(runtime.Path))
            {
                await SetupEmbeddedRuntime(runtime);
            }

            // Verify Python executable
            var pythonExe = Path.Combine(runtime.Path, "python.exe");
            if (!File.Exists(pythonExe))
            {
                runtime.Status = PythonRuntimeStatus.Error;
                runtime.LastError = "Python executable not found";
                return false;
            }

            runtime.Status = PythonRuntimeStatus.Ready;
            runtime.LastInitialized = DateTime.UtcNow;

            await SaveRuntimeConfigurations();
            _logger.LogInformation("Runtime initialized successfully: {RuntimeId}", runtimeId);
            return true;
        }
        catch (Exception ex)
        {
            runtime.Status = PythonRuntimeStatus.Error;
            runtime.LastError = ex.Message;
            _logger.LogError(ex, "Failed to initialize runtime: {RuntimeId}", runtimeId);
            return false;
        }
    }

    public async Task<bool> InstallPackages(string runtimeId, IEnumerable<string> packages)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime))
            return false;

        try
        {
            var pythonExe = Path.Combine(runtime.Path, "python.exe");
            if (!File.Exists(pythonExe))
                return false;

            foreach (var package in packages)
            {
                _logger.LogInformation("Installing {Package} in runtime {RuntimeId}", package, runtimeId);

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"-m pip install {package} --quiet --no-warn-script-location",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = runtime.Path
                };

                using var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        runtime.InstalledPackages[package] = "latest";
                    }
                    else
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        _logger.LogWarning("Failed to install {Package}: {Error}", package, error);
                    }
                }
            }

            await SaveRuntimeConfigurations();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install packages in runtime: {RuntimeId}", runtimeId);
            return false;
        }
    }

    private async Task LoadRuntimeConfigurations()
    {
        try
        {
            if (File.Exists(_runtimesConfigPath))
            {
                var json = await File.ReadAllTextAsync(_runtimesConfigPath);
                var config = JsonSerializer.Deserialize<PythonRuntimeManagerConfig>(json);
                
                if (config != null)
                {
                    foreach (var runtime in config.Runtimes)
                    {
                        _runtimes[runtime.Id] = runtime;
                    }

                    if (!string.IsNullOrEmpty(config.DefaultRuntimeId))
                    {
                        _defaultRuntime = GetRuntime(config.DefaultRuntimeId);
                    }

                    _logger.LogInformation("Loaded {Count} runtime configurations", config.Runtimes.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load runtime configurations");
        }
    }

    private async Task SaveRuntimeConfigurations()
    {
        try
        {
            var config = new PythonRuntimeManagerConfig
            {
                DefaultRuntimeId = _defaultRuntime?.Id,
                Runtimes = _runtimes.Values.ToList()
            };

            var directory = Path.GetDirectoryName(_runtimesConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(_runtimesConfigPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save runtime configurations");
        }
    }

    private async Task DiscoverPythonRuntimes()
    {
        _logger.LogInformation("Discovering Python runtimes...");

        var discoveryPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "python"),
            @"C:\Python311",
            @"C:\Python310",
            @"C:\Python39",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python310")
        };

        foreach (var path in discoveryPaths)
        {
            try
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "python.exe")))
                {
                    var runtimeId = Path.GetFileName(path).ToLowerInvariant();
                    if (_runtimes.ContainsKey(runtimeId))
                        continue;

                    var version = await GetPythonVersion(Path.Combine(path, "python.exe"));
                    
                    var runtime = new PythonRuntimeInfo
                    {
                        Id = runtimeId,
                        Name = $"System Python {version}",
                        Type = PythonRuntimeType.System,
                        Path = path,
                        Version = version,
                        IsManaged = false,
                        Status = PythonRuntimeStatus.Ready,
                        InstalledPackages = new Dictionary<string, string>()
                    };

                    _runtimes[runtimeId] = runtime;
                    _logger.LogInformation("Discovered runtime: {Name} at {Path}", runtime.Name, path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking path {Path}", path);
            }
        }
    }

    private async Task<string> GetPythonVersion(string pythonExe)
    {
        try
        {
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                return output.Trim().Replace("Python ", "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get Python version for {Exe}", pythonExe);
        }

        return "Unknown";
    }

    private async Task CreateEmbeddedRuntime()
    {
        var runtimeId = await CreateManagedRuntime("Default Embedded", PythonRuntimeType.Embedded);
        await InitializeRuntime(runtimeId);
    }

    private async Task SetupEmbeddedRuntime(PythonRuntimeInfo runtime)
    {
        _logger.LogInformation("🔧 Setting up embedded Python runtime: {RuntimeId}", runtime.Id);

        Directory.CreateDirectory(runtime.Path);

        // Download Python embedded distribution
        Console.WriteLine();
        Console.WriteLine("📥 Step 1/3: Downloading Python 3.11.9 embedded distribution...");
        var pythonUrl = "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip";
        var zipPath = Path.Combine(runtime.Path, "python.zip");

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(10); // Increase timeout for large download
        
        var response = await client.GetAsync(pythonUrl);
        response.EnsureSuccessStatusCode();

        await using var fileStream = File.Create(zipPath);
        await response.Content.CopyToAsync(fileStream);
        fileStream.Close();
        
        var downloadSize = new FileInfo(zipPath).Length / (1024.0 * 1024.0);
        Console.WriteLine($"✅ Python downloaded successfully ({downloadSize:F2} MB)");
        _logger.LogInformation("✅ Python downloaded successfully ({0:F2} MB)", downloadSize);

        // Extract
        Console.WriteLine("📦 Extracting Python files...");
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, runtime.Path);
        File.Delete(zipPath);
        Console.WriteLine($"✅ Python extracted to: {runtime.Path}");
        _logger.LogInformation("✅ Python extracted to: {Path}", runtime.Path);

        // Setup pip and site-packages
        Console.WriteLine();
        Console.WriteLine("🔧 Step 2/3: Installing pip package manager...");
        await SetupPipForEmbeddedRuntime(runtime);
        Console.WriteLine("✅ pip installed successfully");
        _logger.LogInformation("✅ pip installed successfully");
        
        Console.WriteLine();
        Console.WriteLine("🔧 Step 3/3: Setting up virtual environment capabilities...");
        // virtualenv is installed in SetupPipForEmbeddedRuntime
        
        runtime.Status = PythonRuntimeStatus.Ready;
        Console.WriteLine("✅ Virtual environment support configured");
        Console.WriteLine();
        _logger.LogInformation("🎉 Embedded Python runtime setup completed!");
    }

    private async Task SetupPipForEmbeddedRuntime(PythonRuntimeInfo runtime)
    {
        // Patch _pth file to enable site-packages
        Console.WriteLine("   ⚙️  Configuring Python environment...");
        var pthFiles = Directory.GetFiles(runtime.Path, "python*._pth");
        if (pthFiles.Any())
        {
            var pthFile = pthFiles.First();
            var lines = File.ReadAllLines(pthFile).Select(l => l.TrimEnd()).ToList();
            
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].StartsWith("#import site"))
                    lines[i] = "import site";
            }
            
            if (!lines.Any(l => l == "Lib")) lines.Add("Lib");
            if (!lines.Any(l => l.Equals("Lib\\site-packages", StringComparison.OrdinalIgnoreCase))) 
                lines.Add("Lib\\site-packages");
            if (!lines.Any(l => l == "import site")) lines.Add("import site");
            
            File.WriteAllLines(pthFile, lines);
            Console.WriteLine("   ✅ Python environment configured");
        }

        // Create directories
        var libPath = Path.Combine(runtime.Path, "Lib");
        var sitePackagesPath = Path.Combine(libPath, "site-packages");
        Directory.CreateDirectory(sitePackagesPath);
        Console.WriteLine("   ✅ Created site-packages directory");

        // Install pip
        Console.WriteLine("   📥 Downloading pip installer...");
        var getPipUrl = "https://bootstrap.pypa.io/get-pip.py";
        var getPipPath = Path.Combine(runtime.Path, "get-pip.py");

        using var client = new HttpClient();
        var getPipScript = await client.GetStringAsync(getPipUrl);
        await File.WriteAllTextAsync(getPipPath, getPipScript);
        Console.WriteLine("   ✅ pip installer downloaded");

        Console.WriteLine("   🔧 Running pip installer (this may take a minute)...");
        var pythonExe = Path.Combine(runtime.Path, "python.exe");
        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{getPipPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = runtime.Path
        };

        using var pipProcess = System.Diagnostics.Process.Start(processInfo);
        if (pipProcess != null)
        {
            await pipProcess.WaitForExitAsync();
            if (pipProcess.ExitCode != 0)
            {
                var error = await pipProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"   ❌ Failed to install pip: {error}");
                _logger.LogError("   ❌ Failed to install pip: {Error}", error);
                throw new Exception($"Failed to install pip: {error}");
            }
            var output = await pipProcess.StandardOutput.ReadToEndAsync();
            _logger.LogDebug("   pip installation output: {Output}", output);
        }

        File.Delete(getPipPath);
        Console.WriteLine("   ✅ pip package manager installed");

        // Upgrade pip to latest version
        Console.WriteLine("   ⬆️  Upgrading pip to latest version...");
        var upgradeInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = "-m pip install --upgrade pip setuptools wheel",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = runtime.Path
        };

        using var upgradeProcess = System.Diagnostics.Process.Start(upgradeInfo);
        if (upgradeProcess != null)
        {
            await upgradeProcess.WaitForExitAsync();
            if (upgradeProcess.ExitCode == 0)
            {
                Console.WriteLine("   ✅ pip upgraded successfully");
            }
        }

        // Install virtualenv package (required for embedded Python to create virtual environments)
        Console.WriteLine("   📦 Installing virtualenv package...");
        var venvProcessInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = "-m pip install virtualenv",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = runtime.Path
        };

        using var venvProcess = System.Diagnostics.Process.Start(venvProcessInfo);
        if (venvProcess != null)
        {
            await venvProcess.WaitForExitAsync();
            if (venvProcess.ExitCode != 0)
            {
                var error = await venvProcess.StandardError.ReadToEndAsync();
                Console.WriteLine($"   ⚠️  Failed to install virtualenv: {error}");
                _logger.LogWarning("   ⚠️ Failed to install virtualenv: {Error}", error);
            }
            else
            {
                Console.WriteLine("   ✅ virtualenv package installed");
            }
        }

        // Verify virtualenv installation
        Console.WriteLine("   🔍 Verifying virtual environment capabilities...");
        var verifyInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = "-c \"import virtualenv; print('virtualenv version:', virtualenv.__version__)\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = runtime.Path
        };

        using var verifyProcess = System.Diagnostics.Process.Start(verifyInfo);
        if (verifyProcess != null)
        {
            await verifyProcess.WaitForExitAsync();
            if (verifyProcess.ExitCode == 0)
            {
                var output = await verifyProcess.StandardOutput.ReadToEndAsync();
                Console.WriteLine($"   ✅ {output.Trim()}");
                _logger.LogInformation("   ✅ {Output}", output.Trim());
            }
            else
            {
                Console.WriteLine("   ⚠️  Could not verify virtualenv installation");
                _logger.LogWarning("   ⚠️ Could not verify virtualenv installation");
            }
        }
    }
}
