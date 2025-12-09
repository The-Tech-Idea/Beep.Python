using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Environment = System.Environment;


namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Manages Python virtual environment lifecycle (create, update, delete) and package installation
/// </summary>
public class VenvManager : IVenvManager
{
    private readonly ILogger<VenvManager> _logger;
    private readonly string _pythonPath;
    private readonly IModelCatalog _modelCatalog;
    private readonly IConfigurationManager? _configurationManager;
    
    private string? _cachedGpuBackend = null;

    // Provider-specific package requirements (per model)
    private Dictionary<string, Dictionary<string, string>> _providerPackages = new();
    private Dictionary<string, Dictionary<string, string>> ProviderPackages
    {
        get
        {
            if (_providerPackages.Count == 0)
            {
                LoadProviderRequirements();
            }
            return _providerPackages;
        }
    }

        public string ComputeProviderVenvName(string providerName, string? modelId)
        {
            try
            {
                var baseName = providerName;
                var isRocm = providerName.IndexOf("-rocm", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isRocm)
                    return baseName;

                var strategy = ReadRocmVenvStrategy();
                switch (strategy?.ToLowerInvariant())
                {
                    case "single":
                        return "rocm";
                    case "family":
                        var fam = providerName.Split('-', 2)[0];
                        return fam + "-rocm";
                    case "model":
                    default:
                        if (!string.IsNullOrEmpty(modelId))
                        {
                            var slug = SlugifyModelId(modelId);
                            return $"{providerName}-{slug}";
                        }
                        return baseName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compute provider venv name; falling back to provider name");
                return providerName;
            }
        }

        public string SlugifyModelId(string modelId)
        {
            var slug = modelId.Replace('/', '-').Replace(':', '-').Replace(' ', '-');
            var sb = new System.Text.StringBuilder();
            foreach (var c in slug)
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
                else sb.Append('-');
            }
            return sb.ToString();
        }

        private string? ReadRocmVenvStrategy()
        {
            try
            {
                // Use IConfigurationManager if provided, otherwise default to 'model'
                if (_configurationManager != null)
                {
                    var strategy = _configurationManager.RocmVenvStrategy;
                    if (!string.IsNullOrEmpty(strategy)) return strategy;
                }
                return "model";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read ROCm venv strategy");
            }
            return "model";
        }

    /// <summary>
    /// Download an embedding model using the provided venv python executable.
    /// </summary>
    public async Task<bool> DownloadEmbeddingModel(string venvPythonExe, string providerName, string modelId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("📥 Downloading sentence-transformers embedding model for {Provider}...", providerName);
            if (string.IsNullOrEmpty(modelId)) modelId = "sentence-transformers/all-MiniLM-L6-v2";

            var downloadScript = @$"
from sentence_transformers import SentenceTransformer
import sys

try:
    print('Downloading model...')
    model = SentenceTransformer('{modelId}')
    print(f'✅ Model downloaded successfully')
    print(f'Dimensions: {{model.get_sentence_embedding_dimension()}}')
    print(f'Max sequence length: {{model.max_seq_length}}')
    sys.exit(0)
except Exception as e:
    print(f'❌ Error: {{str(e)}}')
    sys.exit(1)
";

            var tempScript = Path.Combine(Path.GetTempPath(), $"download_embedding_model_{Guid.NewGuid():N}.py");
            await File.WriteAllTextAsync(tempScript, downloadScript, cancellationToken);
            try
            {
                var processInfo = new ProcessStartInfo(venvPythonExe, $"\"{tempScript}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    var outputTask = Task.Run(async () =>
                    {
                        while (true)
                        {
                            var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
                            if (line == null) break;
                            if (!string.IsNullOrEmpty(line)) Console.WriteLine($"  {line}");
                        }
                    }, cancellationToken);

                    var errorTask = Task.Run(async () =>
                    {
                        while (true)
                        {
                            var line = await process.StandardError.ReadLineAsync(cancellationToken);
                            if (line == null) break;
                            if (!string.IsNullOrEmpty(line)) Console.WriteLine($"  [error] {line}");
                        }
                    }, cancellationToken);

                    await process.WaitForExitAsync(cancellationToken);
                    await Task.WhenAll(outputTask, errorTask);

                    return process.ExitCode == 0;
                }
                return false;
            }
            finally
            {
                try { if (File.Exists(tempScript)) File.Delete(tempScript); } catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pre-download embedding model for {Provider}", providerName);
            return false;
        }
    }

    // Engine-specific package requirements
    private static readonly Dictionary<string, Dictionary<string, string>> EnginePackages = new()
    {
        ["huggingface"] = new Dictionary<string, string>
        {
            { "numpy", ">=1.21.0" },
            { "torch", ">=2.0.0" },              // CPU-only version, lighter
            { "transformers", ">=4.35.0" },      // More flexible version
            { "tokenizers", ">=0.13.0" },
            { "sentencepiece", ">=0.1.96" },
            { "safetensors", ">=0.3.0" },        // For model loading
            { "huggingface-hub", ">=0.18.0" },
            { "tqdm", ">=4.65.0" },
            { "requests", ">=2.25.0" }
        },
        ["llamacpp"] = new Dictionary<string, string>
        {
            { "numpy", ">=1.21.0" },
            { "llama-cpp-python", ">=0.2.26" },
            { "requests", ">=2.25.0" },
            { "tqdm", ">=4.65.0" }
        },
        ["ollama"] = new Dictionary<string, string>
        {
            { "ollama", ">=0.1.2" },
            { "requests", ">=2.25.0" },
            { "pydantic", ">=2.0.0" },
            { "aiohttp", ">=3.8.0" }
        },
        ["gemini"] = new Dictionary<string, string>
        {
            { "google-genai", ">=1.0.0" },
            { "requests", ">=2.25.0" },
            { "pydantic", ">=2.0.0" },
            { "aiohttp", ">=3.8.0" }
        }
    };

    // Embedding-specific packages must be provided by the embeddings project (python-service/requirements.txt)
    // The VenvManager should not hardcode project-specific requirements. If the provider/project wants default
    // packages, they should populate provider-requirements.json or pass package maps to IVenvManager.InstallProviderPackagesInVenv.

    public VenvManager(ILogger<VenvManager> logger, string pythonPath, IModelCatalog? modelCatalog = null, IConfigurationManager? configurationManager = null)
    {
        _logger = logger;
        _pythonPath = pythonPath;
        _modelCatalog = modelCatalog ?? new ModelCatalog();
        _configurationManager = configurationManager;
    }

    private void LoadProviderRequirements()
    {
        try
        {
            // First, try to load from provider-requirements.json (legacy support)
            var requirementsPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "provider-requirements.json");
            
            if (File.Exists(requirementsPath))
            {
                _logger.LogInformation("Loading provider requirements from legacy file: {Path}", requirementsPath);
                var json = File.ReadAllText(requirementsPath);
                var requirements = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
                _providerPackages = requirements ?? new();
                _logger.LogInformation("✅ Loaded provider requirements for {Count} providers from legacy file", _providerPackages.Count);
                return;
            }

            // Load from models.json via ModelCatalog
            _logger.LogInformation("Loading provider requirements from models.json via ModelCatalog");
            var models = _modelCatalog.GetAllModels();
            
            foreach (var model in models)
            {
                if (string.IsNullOrEmpty(model.Provider))
                    continue;

                // Use provider name to determine package requirements
                // Get default packages based on provider pattern
                var packages = GetDefaultPackagesForProvider(model.Provider);
                
                if (packages != null && !_providerPackages.ContainsKey(model.Provider))
                {
                    _providerPackages[model.Provider] = packages;
                }
            }

            // DO NOT load repo-specific requirements here; providers (and the embeddings project) should manage their own requirements files
            // Do not add fallback 'embeddings' provider packages here; the embeddings project is responsible for that

            _logger.LogInformation("✅ Loaded provider requirements for {Count} providers from models.json", _providerPackages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load provider requirements, using defaults");
            _providerPackages = new();
        }
    }

    private Dictionary<string, string>? GetDefaultPackagesForProvider(string provider)
    {
        // Base packages for all transformers-based models
        // On Windows, we pin torch to 2.5.1 for Triton compatibility
        var torchVersion = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "==2.5.1" : ">=2.0.0";
        
        var basePackages = new Dictionary<string, string>
        {
            { "torch", torchVersion },
            { "transformers", ">=4.35.0" },
            { "tokenizers", ">=0.13.0" },
            { "huggingface-hub", ">=0.18.0" },
            { "tqdm", ">=4.65.0" },
            { "requests", ">=2.25.0" }
        };

        // Provider-specific package additions
        var providerLower = provider.ToLowerInvariant();
        
        if (providerLower.Contains("phi"))
        {
            // Phi models need additional packages
            basePackages["safetensors"] = ">=0.3.0";
            basePackages["accelerate"] = ">=0.20.0";
            basePackages["sympy"] = ">=1.13.0";
            basePackages["networkx"] = "";
            basePackages["filelock"] = "";
            
            // Phi-3.5 needs specific transformers version
            if (providerLower.Contains("phi35") || providerLower.Contains("phi3.5"))
            {
                basePackages["transformers"] = "==4.43.0";
            }
            else if (providerLower.Contains("phi4"))
            {
                basePackages["transformers"] = ">=4.43.0";
            }
        }
        else if (providerLower.Contains("llama"))
        {
            // Llama models
            basePackages["safetensors"] = ">=0.3.0";
            basePackages["accelerate"] = ">=0.20.0";
            basePackages["sympy"] = ">=1.13.0";
            basePackages["networkx"] = "";
            basePackages["filelock"] = "";
            
            if (providerLower.Contains("llama32") || providerLower.Contains("llama31"))
            {
                basePackages["transformers"] = ">=4.40.0";
            }
        }
        else if (providerLower.Contains("gemma"))
        {
            // Gemma models
            basePackages["safetensors"] = ">=0.3.0";
            basePackages["accelerate"] = ">=0.20.0";
        }
        else if (providerLower.Contains("qwen"))
        {
            // Qwen models
            basePackages["safetensors"] = ">=0.3.0";
            basePackages["accelerate"] = ">=0.20.0";
            basePackages["sentencepiece"] = ">=0.1.96";
        }
        else if (providerLower.Contains("mistral"))
        {
            // Mistral models
            basePackages["safetensors"] = ">=0.3.0";
            basePackages["accelerate"] = ">=0.20.0";
            basePackages["sentencepiece"] = ">=0.1.96";
        }
        else if (providerLower.Contains("dialogpt"))
        {
            // DialoGPT is lighter, fewer packages needed
            basePackages["sympy"] = ">=1.13.0";
            basePackages["networkx"] = "";
            basePackages["filelock"] = "";
        }
        else if (providerLower.Contains("tinyllama"))
        {
            // TinyLlama
            basePackages["safetensors"] = ">=0.3.0";
            basePackages["sympy"] = ">=1.13.0";
            basePackages["networkx"] = "";
            basePackages["filelock"] = "";
        }
        else
        {
            // Unknown provider - return base packages
            _logger.LogWarning("Unknown provider pattern '{Provider}', using base packages", provider);
        }

        // Add CUDA-specific packages for GPU acceleration and fine-tuning
        if (providerLower.EndsWith("-cuda"))
        {
            basePackages["bitsandbytes"] = ">=0.41.0";
            basePackages["accelerate"] = ">=0.24.0";
            basePackages["peft"] = ">=0.7.0";
            basePackages["trl"] = ">=0.7.0";
            basePackages["datasets"] = ">=2.14.0";
            basePackages["unsloth"] = "";  // Unsloth for fast fine-tuning
            basePackages["xformers"] = ""; // Memory-efficient attention
            _logger.LogInformation("Adding CUDA packages for {Provider}: bitsandbytes, peft, trl, unsloth, xformers", provider);
        }
        else if (providerLower.EndsWith("-rocm"))
        {
            basePackages["accelerate"] = ">=0.24.0";
            basePackages["peft"] = ">=0.7.0";
            basePackages["trl"] = ">=0.7.0";
            basePackages["datasets"] = ">=2.14.0";
            // bitsandbytes doesn't support ROCm well, skip it
            _logger.LogInformation("Adding ROCm packages for {Provider}: peft, trl, datasets", provider);
        }

        return basePackages;
    }

    // NOTE: Per design, provider and project-specific requirements must be managed by provider code or by the embeddings project.
    // The core VenvManager should not attempt to read repository files; it relies on provider configurations passed via
    // provider-requirements.json or model catalog entries. The embeddings project installer (EmbeddingsInitCommand) should
    // read its own `python-service/requirements.txt` and call IVenvManager.InstallProviderPackagesInVenv directly.

    /// <summary>
    /// Install packages in a provider virtual environment
    /// </summary>
    public async Task<bool> InstallProviderPackagesInVenv(string providerName, string venvPythonExe, Dictionary<string, string> packages, CancellationToken cancellationToken, IEnumerable<string>? optionalPackages = null)
    {
        // Filter out packages that are already tracked OR actually installed in venv
        var packagesToInstall = new Dictionary<string, string>();
        var alreadyInstalled = new List<string>();
        
        // First, get list of actually installed packages from pip (quick check)
        var installedInVenv = await GetInstalledPackagesFromPip(venvPythonExe, cancellationToken);
        
        foreach (var (packageName, version) in packages)
        {
            var normalizedName = NormalizePackageName(packageName);
            
            // Check if tracked in registry OR actually installed in venv
            if (IsPackageTracked(providerName, packageName))
            {
                alreadyInstalled.Add(packageName);
            }
            else if (installedInVenv.Contains(normalizedName))
            {
                // Package is installed but not tracked - mark it as tracked and skip
                alreadyInstalled.Add(packageName);
                MarkPackageInstalled(providerName, packageName);
                _logger.LogDebug("Package {Package} found in venv but not tracked - now tracked", packageName);
            }
            else
            {
                packagesToInstall[packageName] = version;
            }
        }
        
        if (alreadyInstalled.Count > 0)
        {
            Console.WriteLine($"⏭️  Skipping {alreadyInstalled.Count} already installed packages: {string.Join(", ", alreadyInstalled)}");
            _logger.LogInformation("⏭️ Skipping {Count} already installed packages: {Packages}", alreadyInstalled.Count, string.Join(", ", alreadyInstalled));
        }
        
        if (packagesToInstall.Count == 0)
        {
            Console.WriteLine($"✅ All {packages.Count} packages already installed in {providerName} venv");
            _logger.LogInformation("✅ All packages already installed for {Provider}", providerName);
            return true;
        }
        
        Console.WriteLine($"📦 Installing {packagesToInstall.Count} packages in {providerName} virtual environment...");
        Console.WriteLine($"🔧 Provider: {providerName} | Venv: {Path.GetDirectoryName(Path.GetDirectoryName(venvPythonExe))}");
        Console.WriteLine($"📋 Packages: {string.Join(", ", packagesToInstall.Keys)}");
        Console.WriteLine();
        
        _logger.LogInformation("📦 Installing {Count} packages in {Provider} virtual environment...", packagesToInstall.Count, providerName);
        _logger.LogInformation("🔧 Provider: {Provider} | Venv: {VenvPath}", providerName, Path.GetDirectoryName(Path.GetDirectoryName(venvPythonExe)));
        _logger.LogInformation("📋 Packages: {PackageList}", string.Join(", ", packagesToInstall.Keys));;
        
        // Upgrade pip in virtual environment first
        Console.WriteLine("⬆️  Upgrading pip in {0} virtual environment...", providerName);
        _logger.LogInformation("⬆️ Upgrading pip in {Provider} virtual environment...", providerName);
        await RunPipCommand(venvPythonExe, "-m pip install --upgrade pip setuptools wheel", cancellationToken);
        Console.WriteLine("✅ pip upgraded");
        Console.WriteLine();

        var anyFailure = false;
        var totalStartTime = DateTime.UtcNow;
        var successfullyInstalled = new List<string>();

        // Install packages in dependency order to avoid build issues
        var orderedPackages = GetOrderedPackages(packagesToInstall);
        
        int current = 1;
        foreach (var (packageName, version) in orderedPackages)
        {
            var packageSpec = string.IsNullOrEmpty(version) ? packageName : $"{packageName}{version}";
            
            Console.WriteLine($"📦 [{current}/{orderedPackages.Count}] Installing {packageSpec} in {providerName} venv...");
            _logger.LogInformation("📦 [{Current}/{Total}] Installing {Package} in {Provider} venv...", 
                current, orderedPackages.Count, packageSpec, providerName);
            
            var startTime = DateTime.UtcNow;
            
            // Use smart torch installation that detects GPU
            bool success;
            if (packageName == "torch")
            {
                success = await InstallTorchInProviderVenv(venvPythonExe, packageSpec, providerName, cancellationToken);
            }
            else if (packageName == "llama-cpp-python")
            {
                success = await InstallLlamaCppPythonInVenv(venvPythonExe, packageSpec, providerName, cancellationToken);
            }
            else
            {
                // Pass providerName so CUDA providers use --extra-index-url to prevent torch CPU override
                success = await InstallSingleInVenv(venvPythonExe, packageSpec, cancellationToken, true, providerName);
            }
            
            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

            var isOptionalPackage = optionalPackages?.Contains(packageName, StringComparer.OrdinalIgnoreCase) ?? false;
            if (success)
            {
                Console.WriteLine($"  ✅ {packageSpec} installed successfully ({elapsed:F1}s)");
                Console.WriteLine();
                _logger.LogInformation("  ✅ {Package} installed successfully ({Elapsed:F1}s)", packageSpec, elapsed);
                successfullyInstalled.Add(packageName);
            }
            else
            {
                Console.WriteLine($"  ❌ Failed to install {packageSpec}");
                Console.WriteLine();
                _logger.LogError("  ❌ Failed to install {Package}", packageSpec);
                if (isOptionalPackage)
                {
                    _logger.LogWarning("⚠️ Optional package {Package} failed to install in {Provider} venv - skipping", packageName, providerName);
                    // do not mark as anyFailure
                }
                else
                {
                    anyFailure = true;
                }
            }
            
            current++;
        }

        var totalElapsed = (DateTime.UtcNow - totalStartTime).TotalSeconds;
        
        // Track successfully installed packages
        if (successfullyInstalled.Count > 0)
        {
            MarkPackagesInstalled(providerName, successfullyInstalled);
        }
        
        if (anyFailure)
        {
            Console.WriteLine($"❌ Some packages failed to install in {providerName} venv (took {totalElapsed:F1}s)");
            _logger.LogError("❌ Some packages failed to install in {Provider} venv (took {Elapsed:F1}s)", providerName, totalElapsed);
            return false;
        }
        
        // CRITICAL: For CUDA providers, force reinstall torch with CUDA to ensure other packages didn't override it with CPU version
        bool isCudaProvider = providerName.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase);
        if (isCudaProvider && packages.ContainsKey("torch"))
        {
            Console.WriteLine("🔧 Ensuring PyTorch CUDA version is installed (fixing potential CPU override)...");
            _logger.LogInformation("🔧 Ensuring PyTorch CUDA version for {Provider}...", providerName);
            
            var torchVersion = packages["torch"];
            // Pin to 2.5.1 if no version specified, to ensure compatibility with available Windows Triton wheels
            string torchSpec;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, we MUST enforce 2.5.1 regardless of what was requested, because of the Triton wheel constraint
                torchSpec = "torch==2.5.1 torchvision==0.20.1 torchaudio==2.5.1";
            }
            else
            {
                torchSpec = string.IsNullOrEmpty(torchVersion) ? "torch==2.5.1 torchvision==0.20.1 torchaudio==2.5.1" : $"torch{torchVersion} torchvision torchaudio";
            }
            
            // Use --upgrade instead of --force-reinstall to avoid locked file issues
            // This still ensures we have the GPU version from the CUDA index
            var reinstallSuccess = await RunPipCommand(venvPythonExe, 
                $"-m pip install {torchSpec} --index-url https://download.pytorch.org/whl/cu124 --upgrade --no-deps", 
                cancellationToken);
            
            if (reinstallSuccess)
            {
                Console.WriteLine("✅ PyTorch CUDA version installed");
                _logger.LogInformation("✅ PyTorch CUDA version installed for {Provider}", providerName);
                
                // Note: Triton is installed automatically by unsloth/xformers as a dependency
                // We no longer install it explicitly to avoid version conflicts and Access Denied errors
                
                // Track torch as installed
                MarkPackageInstalled(providerName, "torch");
            }
            else
            {
                Console.WriteLine("⚠️ Failed to ensure PyTorch CUDA version - GPU acceleration may not work");
                _logger.LogWarning("⚠️ Failed to ensure PyTorch CUDA version for {Provider}", providerName);
            }
        }
        
        Console.WriteLine($"✅ All {packagesToInstall.Count} packages installed successfully in {providerName} venv (took {totalElapsed:F1}s)");
        Console.WriteLine();
        _logger.LogInformation("✅ All {Count} packages installed successfully in {Provider} venv (took {Elapsed:F1}s)", 
            packagesToInstall.Count, providerName, totalElapsed);
        return true;
    }

    /// <summary>
    /// Install packages in an engine virtual environment
    /// </summary>
    public async Task<bool> InstallEnginePackagesInVenv(string engineName, string venvPythonExe, Dictionary<string, string> packages, CancellationToken cancellationToken, IEnumerable<string>? optionalPackages = null)
    {
        _logger.LogInformation("📦 Installing {Count} packages in {Engine} virtual environment...", packages.Count, engineName);
        _logger.LogInformation("🔧 Engine: {Engine} | Venv: {VenvPath}", engineName, Path.GetDirectoryName(Path.GetDirectoryName(venvPythonExe)));
        _logger.LogInformation("📋 Packages: {PackageList}", string.Join(", ", packages.Keys));
        
        // Upgrade pip in virtual environment first
        _logger.LogInformation("⬆️ Upgrading pip in {Engine} virtual environment...", engineName);
        await RunPipCommand(venvPythonExe, "-m pip install --upgrade pip setuptools wheel", cancellationToken);

        // Default optional packages for engines (can be overridden via optionalPackages param)
        var defaultEngineOptional = new[] { "llama-cpp-python" };
        var effectiveOptionalPackages = optionalPackages ?? defaultEngineOptional;
        var anyFailure = false;
        var totalStartTime = DateTime.UtcNow;

        // Install packages in dependency order to avoid build issues
        var orderedPackages = GetOrderedPackages(packages);
        
        int current = 1;
        foreach (var (packageName, version) in orderedPackages)
        {
            var packageSpec = string.IsNullOrEmpty(version) ? packageName : $"{packageName}{version}";
            var isOptional = effectiveOptionalPackages.Contains(packageName);
            
            _logger.LogInformation("📦 [{Current}/{Total}] Installing {Package} in {Engine} venv...", 
                current, orderedPackages.Count, packageSpec, engineName);
            
            var startTime = DateTime.UtcNow;
            
            // Use GPU-aware installation for torch packages
            bool success;
            if (packageName == "torch")
            {
                success = await InstallTorchInProviderVenv(venvPythonExe, packageSpec, engineName, cancellationToken);
            }
            else if (packageName == "llama-cpp-python")
            {
                success = await InstallLlamaCppPythonInVenv(venvPythonExe, packageSpec, engineName, cancellationToken);
            }
            else
            {
                // Pass engineName so CUDA providers use --extra-index-url for torch dependencies
                success = await InstallSingleInVenv(venvPythonExe, packageSpec, cancellationToken, true, engineName);
            }
            
            var duration = DateTime.UtcNow - startTime;

            if (success)
            {
                _logger.LogInformation("✅ [{Current}/{Total}] {Package} installed successfully in {Engine} venv ({Duration:mm\\:ss})", 
                    current, orderedPackages.Count, packageSpec, engineName, duration);
            }
            else if (isOptional)
            {
                _logger.LogWarning("⚠️ [{Current}/{Total}] Optional package {Package} failed in {Engine} venv - skipping ({Duration:mm\\:ss})", 
                    current, orderedPackages.Count, packageSpec, engineName, duration);
                _logger.LogInformation("ℹ️  Tip: {Package} can be installed manually later if needed", packageName);
            }
            else
            {
                _logger.LogError("❌ [{Current}/{Total}] Critical package {Package} failed in {Engine} venv ({Duration:mm\\:ss})", 
                    current, orderedPackages.Count, packageSpec, engineName, duration);
                anyFailure = true;
            }

            current++;
        }

        var totalDuration = DateTime.UtcNow - totalStartTime;
        if (anyFailure)
        {
            _logger.LogError("❌ {Engine} virtual environment setup failed - critical packages missing (Total time: {Duration:mm\\:ss})", 
                engineName, totalDuration);
            return false;
        }

        // CRITICAL: For CUDA engines, force reinstall torch with CUDA to ensure other packages didn't override it with CPU version
        bool isCudaEngine = engineName.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase);
        if (isCudaEngine && packages.ContainsKey("torch"))
        {
            _logger.LogInformation("🔧 Ensuring PyTorch CUDA version for {Engine}...", engineName);
            
            var torchVersion = packages["torch"];
            var torchSpec = string.IsNullOrEmpty(torchVersion) ? "torch" : $"torch{torchVersion}";
            
            // Use --upgrade instead of --force-reinstall to avoid locked file issues
            // This still ensures we have the GPU version from the CUDA index
            var reinstallSuccess = await RunPipCommand(venvPythonExe, 
                $"-m pip install {torchSpec} torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124 --upgrade --no-deps", 
                cancellationToken);
            
            if (reinstallSuccess)
            {
                _logger.LogInformation("✅ PyTorch CUDA version confirmed for {Engine}", engineName);
            }
            else
            {
                _logger.LogWarning("⚠️ Failed to ensure PyTorch CUDA version for {Engine}", engineName);
            }
        }

        _logger.LogInformation("🎉 {Engine} virtual environment packages installed successfully! (Total time: {Duration:mm\\:ss})", 
            engineName, totalDuration);
        return true;
    }

    /// <summary>
    /// Get package requirements for a provider
    /// </summary>
    public Dictionary<string, string>? GetProviderPackages(string providerName)
    {
        var baseProvider = StripBackendSuffix(providerName);
        return ProviderPackages.TryGetValue(baseProvider, out var packages) ? packages : null;
    }

    /// <summary>
    /// Get package requirements for an engine
    /// </summary>
    public Dictionary<string, string>? GetEnginePackages(string engineName)
    {
        return EnginePackages.TryGetValue(engineName, out var packages) ? packages : null;
    }

    /// <summary>
    /// Get embedding package requirements
    /// </summary>
    // Embedding packages are managed by the provider/project and should be passed into InstallProviderPackagesInVenv

    /// <summary>
    /// Verify packages are installed in a virtual environment
    /// </summary>
        public async Task<bool> VerifyPackagesInstalled(string venvPythonExe, IEnumerable<string> packageNames, string environmentName, CancellationToken cancellationToken, IEnumerable<string>? optionalPackages = null)
        {
            _logger.LogInformation("🔍 Verifying packages in {Environment} environment...", environmentName);

            var allInstalled = true;
            var packageList = packageNames.ToList();

            for (int i = 0; i < packageList.Count; i++)
            {
                var packageName = packageList[i];
                    // Strip extras specification (e.g., "uvicorn[standard]" -> "uvicorn")
                var packageNameForVerification = packageName.Contains('[')
                    ? packageName.Substring(0, packageName.IndexOf('['))
                    : packageName;

                Console.WriteLine($"  [{i + 1}/{packageList.Count}] Checking {packageName}...");

                try
                {
                    // Map common package names to importable module names when we want to verify via import
                    var importNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["numpy"] = "numpy",
                        ["torch"] = "torch",
                        ["transformers"] = "transformers",
                        ["faiss-cpu"] = "faiss",
                        ["faiss-gpu"] = "faiss",
                        ["sentence-transformers"] = "sentence_transformers",
                        ["llama-cpp-python"] = "llama_cpp"
                    };

                    var isOptional = optionalPackages?.Contains(packageNameForVerification, StringComparer.OrdinalIgnoreCase) ?? false;

                    // FAST PATH: Check file system directly to avoid overhead and file locks
                    // This avoids running python/pip which can trigger "Access Denied" on loaded DLLs
                    try 
                    {
                        var venvRoot = Path.GetDirectoryName(Path.GetDirectoryName(venvPythonExe));
                        if (!string.IsNullOrEmpty(venvRoot))
                        {
                            var sitePackages = Path.Combine(venvRoot, "Lib", "site-packages");
                            if (Directory.Exists(sitePackages))
                            {
                                // Check for dist-info (most reliable for installed packages)
                                // e.g. numpy-1.26.4.dist-info or numpy-1.26.4+cu124.dist-info
                                var searchPattern = $"{packageNameForVerification.Replace('-', '_')}-*.dist-info";
                                var distInfoDirs = Directory.GetDirectories(sitePackages, searchPattern);
                                
                                // Special check for torch version on Windows CUDA providers
                                if (packageNameForVerification.Equals("torch", StringComparison.OrdinalIgnoreCase) && 
                                    environmentName.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase) &&
                                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    // Check if any of the dist-info folders contains "2.5.1"
                                    // e.g. torch-2.5.1.dist-info or torch-2.5.1+cu124.dist-info
                                    var validTorch = distInfoDirs.Any(d => Path.GetFileName(d).Contains("2.5.1"));
                                    if (validTorch)
                                    {
                                        Console.WriteLine($"    ✅ {packageName} verified (filesystem, version 2.5.1)");
                                        _logger.LogDebug("  ✅ {Package} verified via filesystem (version 2.5.1)", packageName);
                                        continue;
                                    }
                                    // If found but wrong version, fall through to pip show for better error reporting
                                }
                                else if (distInfoDirs.Any())
                                {
                                    Console.WriteLine($"    ✅ {packageName} verified (filesystem)");
                                    _logger.LogDebug("  ✅ {Package} verified via filesystem", packageName);
                                    continue;
                                }
                                
                                // Try with original name if it had hyphens
                                if (packageNameForVerification.Contains('-'))
                                {
                                    searchPattern = $"{packageNameForVerification}-*.dist-info";
                                    distInfoDirs = Directory.GetDirectories(sitePackages, searchPattern);
                                    if (distInfoDirs.Any())
                                    {
                                        Console.WriteLine($"    ✅ {packageName} verified (filesystem)");
                                        _logger.LogDebug("  ✅ {Package} verified via filesystem", packageName);
                                        continue;
                                    }
                                }

                                // Check for package folder directly (fallback)
                                // e.g. "numpy" folder
                                var checkName = packageNameForVerification;
                                if (importNameMap.TryGetValue(packageNameForVerification, out var mappedName))
                                {
                                    checkName = mappedName;
                                }
                                checkName = checkName.Replace('.', Path.DirectorySeparatorChar);
                                
                                if (Directory.Exists(Path.Combine(sitePackages, checkName)))
                                {
                                    Console.WriteLine($"    ✅ {packageName} verified (filesystem folder)");
                                    _logger.LogDebug("  ✅ {Package} verified via filesystem folder", packageName);
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Filesystem verification failed for {Package}: {Message}", packageName, ex.Message);
                    }

                    // Special check for torch version on Windows CUDA providers
                    if (packageNameForVerification.Equals("torch", StringComparison.OrdinalIgnoreCase) && 
                        environmentName.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase) &&
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var versionCheckInfo = new ProcessStartInfo(venvPythonExe, "-m pip show torch")
                        {
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        // Clear environment variables that could interfere with venv isolation
                        versionCheckInfo.Environment["PYTHONPATH"] = "";
                        versionCheckInfo.Environment["PYTHONHOME"] = "";
                        
                        // Set working directory to venv root to ensure proper isolation
                        var venvRoot = Path.GetDirectoryName(Path.GetDirectoryName(venvPythonExe));
                        if (!string.IsNullOrEmpty(venvRoot) && Directory.Exists(venvRoot))
                        {
                            versionCheckInfo.WorkingDirectory = venvRoot;
                        }
                        
                        using var vProcess = Process.Start(versionCheckInfo);
                        if (vProcess != null)
                        {
                            var output = await vProcess.StandardOutput.ReadToEndAsync();
                            await vProcess.WaitForExitAsync(cancellationToken);
                            
                            if (!output.Contains("Version: 2.5.1"))
                            {
                                Console.WriteLine($"    ❌ Torch version mismatch (found incompatible version, need 2.5.1)");
                                Console.WriteLine($"    🔍 Actual output: {output.Trim()}");
                                _logger.LogWarning("Torch version mismatch in {Env}. Need 2.5.1 for Windows Triton compatibility. Output: {Output}", environmentName, output);
                                allInstalled = false;
                                continue;
                            }
                        }
                    }



                    // If package maps to an import name, attempt to verify by importing instead of using pip show
                    if (importNameMap.TryGetValue(packageNameForVerification, out var importName))
                    {
                        try
                        {
                            var script = Path.Combine(Path.GetTempPath(), $"verify_import_{Guid.NewGuid():N}.py");
                            var scriptContent = $"import sys\ntry:\n    import {importName}\n    print('OK')\nexcept Exception as e:\n    print('IMPORT_ERROR:' + str(e))\n    sys.exit(1)\n";
                            await File.WriteAllTextAsync(script, scriptContent, cancellationToken);
                            var (success, output, error) = await RunScriptInVenv(venvPythonExe, script, string.Empty, null, cancellationToken);
                            try { File.Delete(script); } catch { }
                            if (success)
                            {
                                Console.WriteLine($"    ✅ {packageName} import verified");
                                _logger.LogDebug("  ✅ {Package} import verified", packageName);
                                continue; // next package
                            }
                            else
                            {
                                _logger.LogWarning("Import verification failed for {Package}: {Output} {Error}", packageName, output, error);
                                // If this is transformers, try verifying PreTrainedModel symbol directly for better diagnostics
                                if (string.Equals(importName, "transformers", StringComparison.OrdinalIgnoreCase) && !isOptional)
                                {
                                    try
                                    {
                                        var preScript = Path.Combine(Path.GetTempPath(), $"verify_transformers_pretrained_{Guid.NewGuid():N}.py");
                                        var preScriptContent = "import sys\ntry:\n    from transformers import PreTrainedModel\n    print('OK')\nexcept Exception as e:\n    print('IMPORT_ERROR:' + str(e))\n    sys.exit(1)\n";
                                        await File.WriteAllTextAsync(preScript, preScriptContent, cancellationToken);
                                        var (preSuccess, preOut, preErr) = await RunScriptInVenv(venvPythonExe, preScript, string.Empty, null, cancellationToken);
                                        try { File.Delete(preScript); } catch { }
                                        if (!preSuccess)
                                        {
                                            _logger.LogWarning("Transformers import/PreTrainedModel check failed for {Package}: {Output} {Error}", packageName, preOut, preErr);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed secondary transformers PreTrainedModel verification");
                                    }
                                }
                                if (isOptional)
                                {
                                    Console.WriteLine($"    ⚠ {packageName} optional import failed: {output} {error}");
                                    _logger.LogWarning("Optional package {Package} failed import verification: {Output}", packageName, output);
                                    continue; // don't fail init for optional
                                }
                                // fall through to pip show verification if import fails for required packages
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Import-based verification for {Package} failed; falling back to pip show", packageName);
                        }
                    }
                    var attemptCount = 3;
                    var verified = false;
                    for (int attempt = 1; attempt <= attemptCount && !verified; attempt++)
                    {
                        var processInfo = new ProcessStartInfo(venvPythonExe, $"-m pip show {packageNameForVerification}")
                        {
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(processInfo);
                        if (process != null)
                        {
                            // Add timeout to prevent hanging
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                            try
                            {
                                await process.WaitForExitAsync(linkedCts.Token);
                            }
                            catch (OperationCanceledException) when (attempt < attemptCount)
                            {
                                _logger.LogWarning("Timeout verifying {Package} in attempt {Attempt}; retrying...", packageName, attempt);
                                await Task.Delay(1000 * attempt, cancellationToken);
                                continue;
                            }

                            if (process.ExitCode == 0)
                            {
                                Console.WriteLine($"    ✅ {packageName} verified");
                                _logger.LogDebug("  ✅ {Package} verified", packageName);
                                verified = true;
                            }
                            else
                            {
                                if (attempt < attemptCount)
                                {
                                    _logger.LogWarning("{Package} verification failed (exit code {Code}) on attempt {Attempt}; retrying...", packageName, process.ExitCode, attempt);
                                    await Task.Delay(1000 * attempt, cancellationToken);
                                    continue;
                                }
                                Console.WriteLine($"    ❌ {packageName} not found");
                                _logger.LogError("  ❌ {Package} not found", packageName);
                                if (!isOptional)
                                {
                                    allInstalled = false;
                                }
                                else
                                {
                                    _logger.LogWarning("Optional package {Package} not found - continuing", packageName);
                                }
                            }
                        }
                        else
                        {
                            if (attempt < attemptCount)
                            {
                                _logger.LogWarning("Failed to start pip show process for {Package}; retrying attempt {Attempt}...", packageName, attempt);
                                await Task.Delay(1000 * attempt, cancellationToken);
                                continue;
                            }
                            Console.WriteLine($"    ❌ {packageName} verification process failed to start");
                            _logger.LogError("Failed to start pip show process for {Package}", packageName);
                            if (!isOptional)
                            {
                                allInstalled = false;
                            }
                            else
                            {
                                _logger.LogWarning("Optional package {Package} verification process failed to start, continuing", packageName);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"    ⏱️  {packageName} verification timeout");
                    _logger.LogWarning("  ⏱️ {Package} verification timeout", packageName);
                    allInstalled = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ❌ {packageName} verification error: {ex.Message}");
                    _logger.LogError(ex, "  ❌ Error verifying {Package}", packageName);
                    allInstalled = false;
                }
            }

            return allInstalled;
        }

    private List<KeyValuePair<string, string>> GetOrderedPackages(Dictionary<string, string> packages)
    {
        var orderedList = new List<KeyValuePair<string, string>>();
        
        // Define installation order to handle dependencies
        var installOrder = new[] 
        { 
            "numpy", "setuptools", "wheel", "packaging",
            "torch", "safetensors", "accelerate",  // torch first, then packages that depend on it
            "tokenizers", "transformers", "huggingface-hub", "sentencepiece", 
            "tqdm", "requests", "pydantic", "aiohttp",
            "ollama", "google-genai", "llama-cpp-python" // llama-cpp-python last as it can be problematic
        };
        
        // Add packages in dependency order
        foreach (var packageName in installOrder)
        {
            if (packages.TryGetValue(packageName, out var version))
            {
                orderedList.Add(new KeyValuePair<string, string>(packageName, version));
            }
        }
        
        // Add any remaining packages not in our order list
        foreach (var kvp in packages)
        {
            if (!installOrder.Contains(kvp.Key))
            {
                orderedList.Add(kvp);
            }
        }
        
        return orderedList;
    }

    /// <summary>
    /// Check if triton's native library is locked (loaded by another process)
    /// </summary>
    private bool IsTritonLocked(string venvPythonExe)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;
            
        var venvRoot = Path.GetDirectoryName(Path.GetDirectoryName(venvPythonExe));
        var tritonPyd = Path.Combine(venvRoot!, "Lib", "site-packages", "triton", "_C", "libtriton.pyd");
        
        if (!File.Exists(tritonPyd))
            return false;
            
        try
        {
            using var fs = File.Open(tritonPyd, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            fs.Close();
            return false; // Not locked
        }
        catch (IOException)
        {
            return true; // Locked
        }
    }

    private async Task<bool> InstallSingleInVenv(string venvPythonExe, string packageSpec, CancellationToken ct, bool verbose = false, string? providerName = null)
    {
        try
        {
            // Note: torch packages should be installed via InstallTorchInProviderVenv for GPU support
            // This method is for non-torch packages only
            
            // For CUDA providers, use --extra-index-url to ensure torch dependencies resolve to CUDA versions
            var isCudaProvider = providerName?.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase) == true;
            
            // Special handling for unsloth - must use correct version for torch/CUDA combination
            // See: https://docs.unsloth.ai/get-started/install-and-update/pip-install
            if (packageSpec.StartsWith("unsloth", StringComparison.OrdinalIgnoreCase) && isCudaProvider)
            {
                Console.WriteLine("  🦥 Installing Unsloth with correct torch/CUDA variant...");
                // For torch 2.5.1 + CUDA 12.4, use cu124-torch251
                // Install from git to get the latest with correct extras
                var unslothCmd = "-m pip install \"unsloth[cu124-torch251] @ git+https://github.com/unslothai/unsloth.git\" --no-deps";
                var unslothResult = await RunPipCommand(venvPythonExe, unslothCmd, ct);
                
                if (unslothResult)
                {
                    // Also install unsloth's dependencies (excluding triton which we handle separately)
                    Console.WriteLine("  📦 Installing Unsloth dependencies...");
                    await RunPipCommand(venvPythonExe, "-m pip install packaging psutil --extra-index-url https://download.pytorch.org/whl/cu124", ct);
                }
                
                return unslothResult;
            }
            
            // Special handling for Triton on Windows (no official PyPI wheels)
            if (packageSpec.StartsWith("triton") && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Check if triton is locked first
                if (IsTritonLocked(venvPythonExe))
                {
                    Console.WriteLine("  ⚠️ Triton is in use (locked), skipping reinstall");
                    _logger.LogWarning("Triton DLL is locked, skipping reinstall to avoid Access Denied");
                    return true; // Pretend success - triton is already installed
                }
                
                Console.WriteLine("  🪟 Windows detected: Using unofficial Triton wheel...");
                // Use a known compatible wheel for Python 3.11 (which Beep.LLM uses)
                var tritonUrl = "https://github.com/woct0rdho/triton-windows/releases/download/v3.1.0-windows.post5/triton-3.1.0-cp311-cp311-win_amd64.whl";
                var command = $"-m pip install \"{tritonUrl}\" --no-cache-dir";
                return await RunPipCommand(venvPythonExe, command, ct);
            }
            
            // Check if triton is locked - if so, we need to constrain pip to not touch it
            bool tritonLocked = isCudaProvider && IsTritonLocked(venvPythonExe);
            
            // Packages that are known to pull triton as a dependency
            var tritonDependentPackages = new[] { "xformers", "flash-attn", "flash_attn" };
            bool isTritonDependent = tritonDependentPackages.Any(p => 
                packageSpec.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            
            string cmd;
            if (isCudaProvider)
            {
                // Use extra-index-url so torch dependencies are fetched from PyTorch CUDA repository
                if (tritonLocked && isTritonDependent)
                {
                    // For packages that depend on triton, install with --no-deps then install other deps
                    // This avoids pip trying to reinstall the locked triton
                    Console.WriteLine($"  ⚠️ Triton is locked, installing {packageSpec} with --no-deps...");
                    _logger.LogWarning("Triton is locked, installing {Package} with --no-deps to avoid Access Denied", packageSpec);
                    
                    cmd = $"-m pip install {packageSpec} --extra-index-url https://download.pytorch.org/whl/cu124 --no-deps";
                }
                else
                {
                    cmd = $"-m pip install {packageSpec} --extra-index-url https://download.pytorch.org/whl/cu124";
                }
            }
            else
            {
                cmd = $"-m pip install {packageSpec}";
            }
            
            return await RunPipCommand(venvPythonExe, cmd, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install {Package}", packageSpec);
            return false;
        }
    }

    public async Task<bool> RunPipCommand(string pythonExe, string pipArgs, CancellationToken cancellationToken)
    {
        try
        {
            var processInfo = new ProcessStartInfo(pythonExe, pipArgs)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            // Clear environment variables that could interfere with venv isolation
            processInfo.Environment["PYTHONPATH"] = "";
            processInfo.Environment["PYTHONHOME"] = "";
            
            // Set working directory to venv root to ensure proper isolation
            var venvRoot = Path.GetDirectoryName(Path.GetDirectoryName(pythonExe));
            
            if (!string.IsNullOrEmpty(venvRoot) && Directory.Exists(venvRoot))
            {
                processInfo.WorkingDirectory = venvRoot;
            }

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var errorLines = new List<string>();
                
                // Stream output in real-time so user can see progress
                var outputTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync(cancellationToken)) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            // Show important pip progress messages
                            if (line.Contains("Collecting") || 
                                line.Contains("Downloading") || 
                                line.Contains("Installing") ||
                                line.Contains("Successfully installed") ||
                                line.Contains("Requirement already satisfied"))
                            {
                                Console.WriteLine($"    {line}");
                            }
                            _logger.LogDebug(line);
                        }
                    }
                }, cancellationToken);

                var errorTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync(cancellationToken)) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            errorLines.Add(line);
                            _logger.LogDebug(line);
                        }
                    }
                }, cancellationToken);

                await process.WaitForExitAsync(cancellationToken);
                await Task.WhenAll(outputTask, errorTask);
                
                if (process.ExitCode == 0)
                {
                    _logger.LogDebug("Pip command successful: {Args}", pipArgs);
                    return true;
                }
                else
                {
                    var errorText = string.Join("\n", errorLines);
                    Console.WriteLine($"    ❌ Error: {errorText}");
                    _logger.LogWarning("Pip command failed: {Args}. Error: {Error}", pipArgs, errorText);
                    return false;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run pip command: {Args}", pipArgs);
            return false;
        }
    }

    public async Task<List<ProviderPackageInfo>> GetProviderPackageStatus(string providerName, CancellationToken cancellationToken = default)
    {
        var packages = new List<ProviderPackageInfo>();

        var requiredPackages = GetProviderPackages(providerName);
        if (requiredPackages == null)
        {
            _logger.LogWarning("No package requirements found for provider: {Provider}", providerName);
            return packages;
        }

        // Find venv path: search in ~/.beep-llm/providers for folder matching providerName or starting with providerName (family/model)
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "providers");
        string? venvPath = null;
        if (Directory.Exists(baseDir))
        {
            // exact match
            var exact = Path.Combine(baseDir, providerName);
            if (Directory.Exists(exact))
            {
                venvPath = exact;
            }
            else
            {
                // family or model match
                var candidates = Directory.GetDirectories(baseDir).Where(p => Path.GetFileName(p).Equals(providerName, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(p).StartsWith(providerName + "-", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(p).StartsWith(providerName + ":", StringComparison.OrdinalIgnoreCase));
                venvPath = candidates.FirstOrDefault();
            }
        }

        if (string.IsNullOrEmpty(venvPath))
        {
            // Provider not yet set up - all packages are NotInstalled
            foreach (var pkg in requiredPackages)
            {
                packages.Add(new ProviderPackageInfo
                {
                    Name = pkg.Key,
                    VersionConstraint = pkg.Value,
                    Status = PackageStatus.NotInstalled
                });
            }
            return packages;
        }

        var venvPythonExe = Path.Combine(venvPath, "Scripts", "python.exe");
        if (!File.Exists(venvPythonExe))
        {
            _logger.LogError("Provider venv Python not found: {Path}", venvPythonExe);
            foreach (var pkg in requiredPackages)
            {
                packages.Add(new ProviderPackageInfo
                {
                    Name = pkg.Key,
                    VersionConstraint = pkg.Value,
                    Status = PackageStatus.Failed,
                    ErrorMessage = "Virtual environment not found"
                });
            }
            return packages;
        }

        foreach (var pkg in requiredPackages)
        {
            var packageInfo = new ProviderPackageInfo
            {
                Name = pkg.Key,
                VersionConstraint = pkg.Value,
                Status = PackageStatus.NotInstalled
            };

            try
            {
                var processInfo = new ProcessStartInfo(venvPythonExe, $"-m pip show {pkg.Key}")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Clear environment variables that could interfere with venv isolation
                processInfo.Environment["PYTHONPATH"] = "";
                processInfo.Environment["PYTHONHOME"] = "";
                
                // Set working directory to venv root to ensure proper isolation
                var venvRoot = Path.GetDirectoryName(Path.GetDirectoryName(venvPythonExe));
                if (!string.IsNullOrEmpty(venvRoot) && Directory.Exists(venvRoot))
                {
                    processInfo.WorkingDirectory = venvRoot;
                }

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync(cancellationToken);
                    if (process.ExitCode == 0)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                        var versionLine = output.Split('\n').FirstOrDefault(l => l.StartsWith("Version:", StringComparison.OrdinalIgnoreCase));
                        if (versionLine != null)
                        {
                            packageInfo.InstalledVersion = versionLine.Split(':')[1].Trim();
                            packageInfo.Status = PackageStatus.Installed;
                            packageInfo.LastVerified = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check package status: {Package}", pkg.Key);
                packageInfo.Status = PackageStatus.Failed;
                packageInfo.ErrorMessage = ex.Message;
            }
            packages.Add(packageInfo);
        }
        return packages;
    }

    /// <summary>
    /// Execute a Python script in a virtual environment, ensuring proper PATH/PYTHONHOME and VIRTUAL_ENV are set.
    /// </summary>
    public async Task<(bool Success, string Output, string Error)> RunScriptInVenv(
        string pythonExe,
        string scriptPath,
        string arguments = "",
        IProgress<string>? lineProgress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var venvDir = Path.GetDirectoryName(Path.GetDirectoryName(pythonExe));
            var venvScripts = Path.GetDirectoryName(pythonExe);

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"-u -I \"{scriptPath}\" {arguments}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try { psi.StandardOutputEncoding = Encoding.UTF8; psi.StandardErrorEncoding = Encoding.UTF8; } catch { }

            if (!string.IsNullOrEmpty(venvDir) && !string.IsNullOrEmpty(venvScripts))
            {
                psi.EnvironmentVariables["VIRTUAL_ENV"] = venvDir;
                psi.EnvironmentVariables["PATH"] = $"{venvScripts};{Environment.GetEnvironmentVariable("PATH")}";
                psi.EnvironmentVariables["PYTHONHOME"] = "";
                psi.EnvironmentVariables["PYTHONPATH"] = "";
                psi.WorkingDirectory = venvDir;
            }

            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, string.Empty, "Failed to start process");
            }

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    try { lineProgress?.Report(e.Data); } catch { }
                }
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    try { lineProgress?.Report($"[ERR] {e.Data}"); } catch { }
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(cancellationToken);

            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            var success = process.ExitCode == 0;
            return (success, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute script in venv: {Script}", scriptPath);
            return (false, string.Empty, ex.Message);
        }
    }

    private async Task<bool> InstallTorchInProviderVenv(string venvPythonExe, string packageSpec, string providerName, CancellationToken cancellationToken)
    {
        Console.WriteLine("  🔍 Detecting GPU capabilities...");
        _logger.LogInformation("  🔍 Detecting GPU capabilities for {Provider}...", providerName);
        
        // Check provider name - if it ends with -cuda, force CUDA installation
        bool isCudaProvider = providerName.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase);
        bool isRocmProvider = providerName.EndsWith("-rocm", StringComparison.OrdinalIgnoreCase);
        bool isCpuProvider = providerName.EndsWith("-cpu", StringComparison.OrdinalIgnoreCase);
        
        // If explicitly CPU provider, install CPU version
        if (isCpuProvider)
        {
            Console.WriteLine("  💻 CPU provider - Installing CPU-only PyTorch...");
            _logger.LogInformation("💻 CPU provider - Installing CPU-only PyTorch for {Provider}...", providerName);
            return await RunPipCommand(venvPythonExe, 
                $"-m pip install {packageSpec} --index-url https://download.pytorch.org/whl/cpu", 
                cancellationToken);
        }
        
        // If explicitly CUDA provider, install CUDA version
        if (isCudaProvider)
        {
            Console.WriteLine("  🎮 CUDA provider - Installing PyTorch with CUDA 12.4 support...");
            _logger.LogInformation("🎮 CUDA provider - Installing PyTorch with CUDA 12.4 support for {Provider}...", providerName);
            
            // Pin to 2.5.1 if no version specified, to ensure compatibility with available Windows Triton wheels
            string installSpec;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                installSpec = "torch==2.5.1 torchvision==0.20.1 torchaudio==2.5.1";
            }
            else
            {
                installSpec = packageSpec == "torch" ? "torch==2.5.1 torchvision==0.20.1 torchaudio==2.5.1" : $"{packageSpec} torchvision torchaudio";
            }
            
            var success = await RunPipCommand(venvPythonExe, 
                $"-m pip install {installSpec} --index-url https://download.pytorch.org/whl/cu124", 
                cancellationToken);
            
            if (success)
            {
                Console.WriteLine("  ✅ PyTorch with CUDA 12.4 installed successfully");
                _logger.LogInformation("✅ PyTorch with CUDA 12.4 installed successfully for {Provider}", providerName);
            }
            return success;
        }
        
        // If ROCm provider, install ROCm version
        if (isRocmProvider)
        {
            Console.WriteLine("  🔴 ROCm provider - Installing PyTorch with ROCm support...");
            _logger.LogInformation("🔴 ROCm provider - Installing PyTorch with ROCm support for {Provider}...", providerName);
            var success = await RunPipCommand(venvPythonExe, 
                $"-m pip install {packageSpec} torchvision torchaudio --index-url https://download.pytorch.org/whl/rocm6.2", 
                cancellationToken);
            
            if (success)
            {
                Console.WriteLine("  ✅ PyTorch with ROCm installed successfully");
                _logger.LogInformation("✅ PyTorch with ROCm installed successfully for {Provider}", providerName);
            }
            return success;
        }
        
        // Auto-detect GPU for providers without explicit backend suffix
        var cudaCheckScript = @"
try:
    import subprocess
    result = subprocess.run(['nvidia-smi'], capture_output=True, text=True)
    if result.returncode == 0 and 'CUDA' in result.stdout:
        print('CUDA_AVAILABLE')
    else:
        print('NO_CUDA')
except:
    print('NO_CUDA')
";
        
        var tempScript = Path.Combine(Path.GetTempPath(), $"cuda_check_{Guid.NewGuid():N}.py");
        await File.WriteAllTextAsync(tempScript, cudaCheckScript, cancellationToken);
        
        try
        {
            var processInfo = new ProcessStartInfo(venvPythonExe, $"\"{tempScript}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                
                bool hasCuda = output.Contains("CUDA_AVAILABLE");
                
                if (hasCuda)
                {
                    Console.WriteLine("  🎮 GPU detected! Installing PyTorch with CUDA 12.4 support...");
                    _logger.LogInformation("🎮 GPU detected! Installing PyTorch with CUDA 12.4 support for {Provider}...", providerName);
                    var success = await RunPipCommand(venvPythonExe, 
                        $"-m pip install {packageSpec} torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124", 
                        cancellationToken);
                    
                    if (success)
                    {
                        Console.WriteLine("  ✅ PyTorch with CUDA 12.4 installed successfully");
                        _logger.LogInformation("✅ PyTorch with CUDA 12.4 installed successfully for {Provider}", providerName);
                    }
                    return success;
                }
                else
                {
                    Console.WriteLine("  💻 No GPU detected. Installing CPU-only PyTorch...");
                    _logger.LogInformation("💻 No GPU detected. Installing CPU-only PyTorch for {Provider}...", providerName);
                    return await RunPipCommand(venvPythonExe, 
                        $"-m pip install {packageSpec} --index-url https://download.pytorch.org/whl/cpu", 
                        cancellationToken);
                }
            }
        }
        finally
        {
            if (File.Exists(tempScript))
            {
                File.Delete(tempScript);
            }
        }
        
        return false;
    }

    private async Task<bool> InstallLlamaCppPythonInVenv(string venvPythonExe, string packageSpec, string providerName, CancellationToken cancellationToken)
    {
        Console.WriteLine("  🔍 Detecting GPU for llama-cpp-python installation...");
        _logger.LogInformation("  🔍 Detecting GPU for llama-cpp-python installation in {Provider}...", providerName);
        
        // Determine backend from provider name suffix
        var backend = DetermineBackendFromProviderName(providerName);
        
        switch (backend.ToLowerInvariant())
        {
            case "rocm":
                Console.WriteLine("  🔥 ROCm backend selected! Installing llama-cpp-python with ROCm/HIP support...");
                _logger.LogInformation("🔥 ROCm backend: Installing llama-cpp-python with HIP support for {Provider}...", providerName);
                
                // Build from source with HIPBlas enabled
                var rocmInstall = new ProcessStartInfo(venvPythonExe, $"-m pip install {packageSpec} --no-cache-dir --force-reinstall --no-binary :all:")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                // Set CMake args for ROCm/HIP
                rocmInstall.EnvironmentVariables["CMAKE_ARGS"] = "-DLLAMA_HIPBLAS=ON";
                rocmInstall.EnvironmentVariables["FORCE_CMAKE"] = "1";
                
                using (var rocmProcess = Process.Start(rocmInstall))
                {
                    if (rocmProcess != null)
                    {
                        var output = await rocmProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                        var error = await rocmProcess.StandardError.ReadToEndAsync(cancellationToken);
                        await rocmProcess.WaitForExitAsync(cancellationToken);
                        
                        if (rocmProcess.ExitCode == 0)
                        {
                            Console.WriteLine("  ✅ llama-cpp-python with ROCm/HIP support installed successfully");
                            _logger.LogInformation("✅ llama-cpp-python with ROCm/HIP installed for {Provider}", providerName);
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"  ❌ ROCm build failed. Error output:");
                            Console.WriteLine(error);
                            _logger.LogError("❌ ROCm build failed for {Provider}: {Error}", providerName, error);
                            return false;
                        }
                    }
                }
                return false;
                
            case "cuda":
                Console.WriteLine("  🎮 CUDA backend selected! Installing llama-cpp-python with CUDA support...");
                _logger.LogInformation("🎮 CUDA backend: Installing llama-cpp-python with CUDA for {Provider}...", providerName);
                
                // Use pre-built CUDA wheels
                var success = await RunPipCommand(venvPythonExe, 
                    $"-m pip install {packageSpec} --extra-index-url https://abetlen.github.io/llama-cpp-python/whl/cu121", 
                    cancellationToken);
                
                if (success)
                {
                    Console.WriteLine("  ✅ llama-cpp-python with CUDA support installed successfully");
                    _logger.LogInformation("✅ llama-cpp-python with CUDA installed for {Provider}", providerName);
                }
                return success;
                
            case "vulkan":
                Console.WriteLine("  🌋 Vulkan backend selected! Installing llama-cpp-python with Vulkan support...");
                _logger.LogInformation("🌋 Vulkan backend: Installing llama-cpp-python with Vulkan for {Provider}...", providerName);
                
                // Build from source with Vulkan enabled
                var vulkanInstall = new ProcessStartInfo(venvPythonExe, $"-m pip install {packageSpec} --no-cache-dir --force-reinstall --no-binary :all:")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                vulkanInstall.EnvironmentVariables["CMAKE_ARGS"] = "-DLLAMA_VULKAN=ON";
                vulkanInstall.EnvironmentVariables["FORCE_CMAKE"] = "1";
                
                using (var vulkanProcess = Process.Start(vulkanInstall))
                {
                    if (vulkanProcess != null)
                    {
                        var output = await vulkanProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                        var error = await vulkanProcess.StandardError.ReadToEndAsync(cancellationToken);
                        await vulkanProcess.WaitForExitAsync(cancellationToken);
                        
                        if (vulkanProcess.ExitCode == 0)
                        {
                            Console.WriteLine("  ✅ llama-cpp-python with Vulkan support installed successfully");
                            _logger.LogInformation("✅ llama-cpp-python with Vulkan installed for {Provider}", providerName);
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"  ❌ Vulkan build failed: {error}");
                            _logger.LogError("❌ Vulkan build failed for {Provider}: {Error}", providerName, error);
                            return false;
                        }
                    }
                }
                return false;
                
            default: // CPU
                Console.WriteLine("  💻 CPU backend selected. Installing standard llama-cpp-python...");
                _logger.LogInformation("💻 CPU backend: Installing standard llama-cpp-python for {Provider}...", providerName);
                return await RunPipCommand(venvPythonExe, $"-m pip install {packageSpec}", cancellationToken);
        }
    }
    
    // NOTE: DetermineBackendFromProviderName is implemented as public later in this class to allow reuse across the project.

    /// <summary>
    /// Create a provider virtual environment
    /// </summary>
    public async Task<string?> CreateProviderVirtualEnvironment(
        string providerName,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "providers");
            var venvName = ComputeProviderVenvName(providerName, modelId);
            var venvPath = Path.Combine(baseDir, venvName);

            // Check if virtual environment already exists
            var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");
            if (File.Exists(venvPython))
            {
                Console.WriteLine($"  ✅ Virtual environment already exists");
                _logger.LogInformation("✅ Virtual environment for {Provider} already exists at: {Path}", providerName, venvPath);
                return venvPath;
            }

            Console.WriteLine($"  📁 Creating directory: {baseDir}");
            _logger.LogInformation("🏗️ Creating virtual environment for {Provider} provider...", providerName);
            Directory.CreateDirectory(baseDir);

            // First, ensure venv capabilities are available
            Console.WriteLine($"  🔍 Checking virtualenv availability...");
            if (!await EnsureVenvCapabilities(cancellationToken))
            {
                Console.WriteLine($"  ❌ Cannot create virtual environments - venv/virtualenv not available");
                _logger.LogError("❌ Cannot create virtual environments - venv/virtualenv not available");
                return null;
            }

            // Try virtualenv first (works with embedded Python), then fallback to venv
            Console.WriteLine($"  🛠️  Attempting to create venv with virtualenv...");
            if (await TryCreateWithVirtualenv(venvPath, cancellationToken))
            {
                Console.WriteLine($"  ✅ Created virtual environment using virtualenv");
                _logger.LogInformation("✅ Created virtual environment using virtualenv: {Path}", venvPath);
                return venvPath;
            }

            // Fallback to venv (for standard Python installations)
            Console.WriteLine($"  🛠️  Attempting to create venv with built-in venv module...");
            if (await TryCreateWithVenv(venvPath, cancellationToken))
            {
                Console.WriteLine($"  ✅ Created virtual environment using venv module");
                _logger.LogInformation("✅ Created virtual environment using venv: {Path}", venvPath);
                return venvPath;
            }

            Console.WriteLine($"  ❌ Failed to create virtual environment");
            _logger.LogError("❌ Failed to create virtual environment for {Provider}", providerName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating virtual environment for {Provider}", providerName);
            return null;
        }
    }

    /// <summary>
    /// Ensure the provider virtual environment exists, packages are installed and verified.
    /// Orchestrates creation, installation and verification for provider venvs.
    /// </summary>
    public async Task<string?> EnsureProviderEnvironment(
        string providerName,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        // FAST PATH: Check registry FIRST for any backend variant before calling AppendBackendSuffix
        // This avoids spawning nvidia-smi/rocm-smi processes when environment is already registered
        foreach (var suffix in new[] { "-cuda", "-rocm", "-vulkan", "-cpu", "" })
        {
            var providerWithBackend = providerName + suffix;
            if (IsFeatureInstalled(providerWithBackend, "base"))
            {
                var registeredPath = GetRegisteredEnvironmentPath(providerWithBackend);
                if (registeredPath != null && Directory.Exists(registeredPath))
                {
                    // Verify the venv is actually valid (has python.exe)
                    var venvPython = Path.Combine(registeredPath, "Scripts", "python.exe");
                    if (File.Exists(venvPython))
                    {
                        _logger.LogInformation("Provider {Provider} base environment is ready (cached in registry).", providerWithBackend);
                        return registeredPath;
                    }
                    else
                    {
                        // Registry entry exists but venv is corrupted - remove from registry and continue
                        _logger.LogWarning("Provider {Provider} registry entry points to corrupted venv (missing python.exe) - cleaning up", providerWithBackend);
                        UnregisterEnvironment(providerWithBackend);
                    }
                }
                else if (registeredPath != null)
                {
                    // Registry entry exists but directory doesn't - clean up stale entry
                    _logger.LogWarning("Provider {Provider} registry entry points to non-existent path - cleaning up", providerWithBackend);
                    UnregisterEnvironment(providerWithBackend);
                }
            }
        }
        
        // No registered environment found - need to detect GPU and create new environment
        // Append backend suffix if not present
        providerName = await AppendBackendSuffix(providerName, cancellationToken);

        // Double-check registry with the detected backend suffix (in case it was added by previous run)
        if (IsFeatureInstalled(providerName, "base"))
        {
            var registry = LoadRegistry();
            if (registry.Environments.TryGetValue(providerName, out var entry))
            {
                // Verify the venv is actually valid
                var venvPython = Path.Combine(entry.Path, "Scripts", "python.exe");
                if (Directory.Exists(entry.Path) && File.Exists(venvPython))
                {
                    _logger.LogInformation("Provider {Provider} base environment is ready (cached in registry).", providerName);
                    return entry.Path;
                }
                else
                {
                    // Corrupted or missing - clean up
                    _logger.LogWarning("Provider {Provider} registry entry invalid - cleaning up", providerName);
                    UnregisterEnvironment(providerName);
                }
            }
        }

        var packages = GetProviderPackages(providerName);
        if (packages == null)
        {
            if (string.Equals(providerName, "embeddings", StringComparison.OrdinalIgnoreCase))
            {
                // Embeddings packages are managed by the embeddings project and providers.
                // Leave packages empty and let provider logic install/verify packages as appropriate.
                packages = new Dictionary<string, string>();
            }
            else
            {
                // Unknown provider - fail
                _logger.LogWarning("Unknown provider: {Provider}", providerName);
                return null;
            }
        }

        var venvPath = await CreateProviderVirtualEnvironment(providerName, modelId, cancellationToken);
        if (venvPath == null)
        {
            _logger.LogError("Failed to create provider venv for {Provider}", providerName);
            return null;
        }

        var venvPythonExe = Path.Combine(venvPath, "Scripts", "python.exe");
        if (!File.Exists(venvPythonExe))
        {
            _logger.LogError("Provider venv python not found: {Path}", venvPythonExe);
            return null;
        }

        var markerFile = Path.Combine(venvPath, $".{providerName}-installed");
        if (File.Exists(markerFile))
        {
            _logger.LogInformation("Provider {Provider} already installed (marker found). Skipping verification.", providerName);
            
            // Update registry since we found a marker but no registry entry
            MarkFeatureInstalled(providerName, "base", venvPath);
            return venvPath;
        }

        // Install packages
        if (!await InstallProviderPackagesInVenv(providerName, venvPythonExe, packages, cancellationToken))
        {
            _logger.LogError("Failed to install provider packages for {Provider}", providerName);
            return null;
        }

        // Verify
        if (!await VerifyPackagesInstalled(venvPythonExe, packages.Keys, providerName, cancellationToken))
        {
            _logger.LogError("Package verification failed for {Provider}", providerName);
            try
            {
                var status = await GetProviderPackageStatus(providerName, cancellationToken);
                var failed = status.Where(p => p.Status != PackageStatus.Installed).Select(p => p.ErrorMessage != null && p.ErrorMessage.Length > 0 ? $"{p.Name}:{p.Status} ({p.ErrorMessage})" : $"{p.Name}:{p.Status}").ToList();
                if (failed.Any())
                {
                    _logger.LogInformation("Packages failing verification: {Failed}", string.Join(", ", failed));
                    Console.WriteLine();
                    Console.WriteLine("The following packages failed verification in the provider virtual environment:");
                    foreach (var f in failed) Console.WriteLine($"  - {f}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect package verification status for {Provider}", providerName);
            }
            return null;
        }

        // Install sentence-transformers for embeddings optionally
        if (packages.ContainsKey("sentence-transformers") == false && providerName.Equals("embeddings", StringComparison.OrdinalIgnoreCase))
        {
            await RunPipCommand(venvPythonExe, "-m pip install -U sentence-transformers", cancellationToken);
        }

        // Write marker
        var installInfo = $"Version: 2.0\nProvider: {providerName}\nVenv Path: {venvPath}\nInstalled: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\nPackages: {string.Join(", ", packages.Keys)}";
        await File.WriteAllTextAsync(markerFile, installInfo, cancellationToken);

        // Update registry on success
        MarkFeatureInstalled(providerName, "base", venvPath);

        _logger.LogInformation("Provider environment ready: {Provider} at {Path}", providerName, venvPath);
        return venvPath;
    }

    /// <summary>
    /// Ensure engine environment exists with packages installed.
    /// Orchestrates engine venv creation and packages install.
    /// </summary>
    public async Task<string?> EnsureEngineEnvironment(string engineName, CancellationToken cancellationToken = default)
    {
        // 1. Check persistent registry first
        if (IsFeatureInstalled(engineName, "base"))
        {
            var registry = LoadRegistry();
            if (registry.Environments.TryGetValue(engineName, out var entry))
            {
                _logger.LogInformation("Engine {Engine} environment is ready (cached in registry).", engineName);
                return entry.Path;
            }
        }

        var packages = GetEnginePackages(engineName);
        if (packages == null)
        {
            _logger.LogWarning("Unknown engine: {Engine}", engineName);
            return null;
        }

        var venvPath = await CreateEngineVirtualEnvironment(engineName, cancellationToken);
        if (venvPath == null)
        {
            _logger.LogError("Failed to create engine venv for {Engine}", engineName);
            return null;
        }

        var venvPythonExe = Path.Combine(venvPath, "Scripts", "python.exe");
        if (!File.Exists(venvPythonExe))
        {
            _logger.LogError("Engine venv python not found: {Path}", venvPythonExe);
            return null;
        }

        var markerFile = Path.Combine(venvPath, $".{engineName}-installed");
        if (File.Exists(markerFile))
        {
            _logger.LogInformation("{Engine} packages already installed in venv (marker found).", engineName);
            
            // Update registry since we found a marker but no registry entry
            MarkFeatureInstalled(engineName, "base", venvPath);
            return venvPath;
        }

        if (!await InstallEnginePackagesInVenv(engineName, venvPythonExe, packages, cancellationToken))
        {
            _logger.LogError("Failed to install engine packages for {Engine}", engineName);
            return null;
        }

        var installInfo = $"Engine: {engineName}\nVenv Path: {venvPath}\nInstalled: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}\nPackages: {string.Join(", ", packages.Keys)}";
        await File.WriteAllTextAsync(markerFile, installInfo, cancellationToken);

        // Update registry on success
        MarkFeatureInstalled(engineName, "base", venvPath);

        _logger.LogInformation("Engine venv ready: {Engine} at {Path}", engineName, venvPath);
        return venvPath;
    }

    /// <summary>
    /// Create an engine virtual environment
    /// </summary>
    public async Task<string?> CreateEngineVirtualEnvironment(
        string engineName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "engines");
            var venvPath = Path.Combine(baseDir, engineName);

            // Check if virtual environment already exists
            var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");
            if (File.Exists(venvPython))
            {
                _logger.LogInformation("✅ Virtual environment for {Engine} already exists at: {Path}", engineName, venvPath);
                return venvPath;
            }

            _logger.LogInformation("🏗️ Creating virtual environment for {Engine} engine...", engineName);
            Directory.CreateDirectory(baseDir);

            // First, ensure venv capabilities are available
            if (!await EnsureVenvCapabilities(cancellationToken))
            {
                _logger.LogError("❌ Cannot create virtual environments - venv/virtualenv not available");
                return null;
            }

            // Try virtualenv first (works with embedded Python), then fallback to venv
            if (await TryCreateWithVirtualenv(venvPath, cancellationToken))
            {
                _logger.LogInformation("✅ Created virtual environment using virtualenv: {Path}", venvPath);
                return venvPath;
            }

            // Fallback to venv (for standard Python installations)
            if (await TryCreateWithVenv(venvPath, cancellationToken))
            {
                _logger.LogInformation("✅ Created virtual environment using venv: {Path}", venvPath);
                return venvPath;
            }

            _logger.LogError("❌ Failed to create virtual environment for {Engine}", engineName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creating virtual environment for {Engine}", engineName);
            return null;
        }
    }

    /// <summary>
    /// Delete a virtual environment
    /// </summary>
    public async Task<bool> DeleteVirtualEnvironment(string venvPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(venvPath))
            {
                _logger.LogWarning("Virtual environment does not exist: {Path}", venvPath);
                return false;
            }

            _logger.LogInformation("🗑️  Deleting virtual environment: {Path}", venvPath);

            // Kill any Python processes that are using this venv
            await KillPythonProcessesInVenv(venvPath);

            await Task.Run(() =>
            {
                try
                {
                    Directory.Delete(venvPath, true);
                }
                catch (UnauthorizedAccessException)
                {
                    // Try to remove read-only attributes
                    var dirInfo = new DirectoryInfo(venvPath);
                    SetAttributesNormal(dirInfo);
                    Directory.Delete(venvPath, true);
                }
            }, cancellationToken);

            _logger.LogInformation("✅ Virtual environment deleted successfully");

            // Remove from registry
            try
            {
                var registry = LoadRegistry();
                var keysToRemove = registry.Environments
                    .Where(kvp => string.Equals(kvp.Value.Path, venvPath, StringComparison.OrdinalIgnoreCase) || 
                                  (Path.IsPathRooted(kvp.Value.Path) && Path.IsPathRooted(venvPath) && 
                                   string.Equals(Path.GetFullPath(kvp.Value.Path), Path.GetFullPath(venvPath), StringComparison.OrdinalIgnoreCase)))
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (keysToRemove.Any())
                {
                    foreach (var key in keysToRemove)
                    {
                        registry.Environments.Remove(key);
                    }
                    SaveRegistry(registry);
                    _logger.LogInformation("Removed {Count} entries from registry for path {Path}", keysToRemove.Count, venvPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to update registry after deleting venv: {Message}", ex.Message);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to delete virtual environment: {Path}", venvPath);
            return false;
        }
    }

    private static void SetAttributesNormal(DirectoryInfo dir)
    {
        foreach (var subDir in dir.GetDirectories())
        {
            SetAttributesNormal(subDir);
        }
        foreach (var file in dir.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
        dir.Attributes = FileAttributes.Normal;
    }

    /// <summary>
    /// Kill any Python processes that are running from the specified venv
    /// </summary>
    private async Task KillPythonProcessesInVenv(string venvPath)
    {
        try
        {
            var normalizedVenvPath = Path.GetFullPath(venvPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var pythonProcesses = Process.GetProcessesByName("python")
                .Concat(Process.GetProcessesByName("python3"))
                .Concat(Process.GetProcessesByName("pythonw"));

            var killedCount = 0;
            foreach (var process in pythonProcesses)
            {
                try
                {
                    var processPath = process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(processPath))
                    {
                        var normalizedProcessPath = Path.GetFullPath(processPath);
                        // Check if this Python process is from our venv
                        if (normalizedProcessPath.StartsWith(normalizedVenvPath, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Killing Python process {Pid} from venv {Path}", process.Id, venvPath);
                            process.Kill();
                            await Task.Delay(100); // Give it time to die
                            killedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not check/kill process {Pid}", process.Id);
                }
            }

            if (killedCount > 0)
            {
                _logger.LogInformation("Killed {Count} Python processes from venv", killedCount);
                await Task.Delay(500); // Extra wait for file handles to be released
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking for Python processes in venv");
        }
    }

    private async Task<bool> EnsureVenvCapabilities(CancellationToken cancellationToken)
    {
        try
        {
            var pythonExe = Path.Combine(_pythonPath, "python.exe");

            // Check if venv module is available (it's not in embedded Python)
            _logger.LogDebug("🔍 Checking venv module availability...");
            var venvCheckProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m venv --help",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            venvCheckProcess.Start();
            var venvStderr = await venvCheckProcess.StandardError.ReadToEndAsync(cancellationToken);
            await venvCheckProcess.WaitForExitAsync(cancellationToken);

            if (venvCheckProcess.ExitCode == 0)
            {
                _logger.LogInformation("✅ venv module is available");
                return true;
            }

            _logger.LogInformation("ℹ️ venv module not available (embedded Python), ensuring virtualenv package is installed...");

            // Ensure pip is available first
            var pipCheckProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m pip --version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            pipCheckProcess.Start();
            await pipCheckProcess.WaitForExitAsync(cancellationToken);

            if (pipCheckProcess.ExitCode != 0)
            {
                _logger.LogError("❌ pip is not available - cannot install virtualenv");
                return false;
            }

            // Install virtualenv package
            _logger.LogInformation("📦 Installing virtualenv package...");
            var installProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m pip install --upgrade virtualenv",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            installProcess.Start();
            var installStdout = await installProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            var installStderr = await installProcess.StandardError.ReadToEndAsync(cancellationToken);
            await installProcess.WaitForExitAsync(cancellationToken);

            if (installProcess.ExitCode == 0)
            {
                _logger.LogInformation("✅ virtualenv package installed successfully");
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to install virtualenv: {Error}", installStderr);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error ensuring venv capabilities");
            return false;
        }
    }

    private async Task<bool> TryCreateWithVenv(string venvPath, CancellationToken cancellationToken)
    {
        try
        {
            var pythonExe = Path.Combine(_pythonPath, "python.exe");
            if (!File.Exists(pythonExe))
            {
                _logger.LogError("❌ Python executable not found: {Path}", pythonExe);
                return false;
            }

            _logger.LogDebug("Creating isolated venv with: {Python} -m venv --clear \"{Path}\"", pythonExe, venvPath);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"-m venv --clear \"{venvPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                // Verify the venv was actually created
                var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");
                if (File.Exists(venvPython))
                {
                    _logger.LogDebug("✅ Virtual environment created successfully: {Path}", venvPath);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠️ venv command succeeded but python.exe not found at {Path}", venvPython);
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("⚠️ venv creation failed with exit code {Code}: {Error}", process.ExitCode, stderr);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception creating venv at {Path}", venvPath);
            return false;
        }
    }

    private async Task<bool> TryCreateWithVirtualenv(string venvPath, CancellationToken cancellationToken)
    {
        try
        {
            var pythonExe = Path.Combine(_pythonPath, "python.exe");
            if (!File.Exists(pythonExe))
            {
                _logger.LogError("❌ Python executable not found: {Path}", pythonExe);
                return false;
            }

            // Complete isolation: --always-copy copies files, --clear removes existing, --no-download prevents parent env lookup
            _logger.LogDebug("Creating isolated virtualenv with: {Python} -m virtualenv --always-copy --clear --no-download \"{Path}\"", pythonExe, venvPath);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"-m virtualenv --always-copy --clear --no-download \"{venvPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            // Clear PYTHONPATH to prevent package inheritance
            process.StartInfo.EnvironmentVariables["PYTHONPATH"] = "";
            process.StartInfo.EnvironmentVariables["PYTHONHOME"] = "";

            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                // Verify the virtualenv was actually created
                var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");
                if (File.Exists(venvPython))
                {
                    _logger.LogDebug("✅ Virtual environment created successfully: {Path}", venvPath);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠️ virtualenv command succeeded but python.exe not found at {Path}", venvPython);
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("⚠️ virtualenv creation failed with exit code {Code}: {Error}", process.ExitCode, stderr);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception creating venv with virtualenv at {Path}", venvPath);
            return false;
        }
    }

    /// <summary>
    /// Detect GPU backend for optimal package installation (ROCm, CUDA, Vulkan, or CPU)
    /// </summary>
    public async Task<string> DetectGPUBackend(CancellationToken cancellationToken)
    {
        if (_cachedGpuBackend != null) return _cachedGpuBackend;

        // Check for ROCm first (AMD GPUs)
        try
        {
            var rocmCheck = new ProcessStartInfo("rocm-smi")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var rocmProcess = Process.Start(rocmCheck);
            if (rocmProcess != null)
            {
                await rocmProcess.WaitForExitAsync(cancellationToken);
                if (rocmProcess.ExitCode == 0)
                {
                    _logger.LogInformation("🔥 AMD GPU with ROCm detected");
                    _cachedGpuBackend = "rocm";
                    return "rocm";
                }
            }
        }
        catch { /* ROCm not available */ }
        
        // Check for NVIDIA CUDA
        try
        {
            var cudaCheck = new ProcessStartInfo("nvidia-smi")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var cudaProcess = Process.Start(cudaCheck);
            if (cudaProcess != null)
            {
                var output = await cudaProcess.StandardOutput.ReadToEndAsync(cancellationToken);
                await cudaProcess.WaitForExitAsync(cancellationToken);
                if (cudaProcess.ExitCode == 0 && output.Contains("CUDA"))
                {
                    _logger.LogInformation("🎮 NVIDIA GPU with CUDA detected");
                    _cachedGpuBackend = "cuda";
                    return "cuda";
                }
            }
        }
        catch { /* CUDA not available */ }
        
        // Check for Vulkan (cross-platform GPU API)
        try
        {
            var vulkanCheck = new ProcessStartInfo("vulkaninfo")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var vulkanProcess = Process.Start(vulkanCheck);
            if (vulkanProcess != null)
            {
                await vulkanProcess.WaitForExitAsync(cancellationToken);
                if (vulkanProcess.ExitCode == 0)
                {
                    _logger.LogInformation("🎨 Vulkan backend detected");
                    _cachedGpuBackend = "vulkan";
                    return "vulkan";
                }
            }
        }
        catch { /* Vulkan not available */ }
        
        // Default to CPU
        _logger.LogInformation("💻 No GPU detected, using CPU backend");
        _cachedGpuBackend = "cpu";
        return "cpu";
    }

    /// <summary>
    /// Determine backend from provider name suffix (-rocm, -cuda, -vulkan, -cpu)
    /// </summary>
    public string DetermineBackendFromProviderName(string providerName)
    {
        // Check if provider name has backend suffix
        if (providerName.EndsWith("-rocm", StringComparison.OrdinalIgnoreCase))
            return "rocm";
        if (providerName.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase))
            return "cuda";
        if (providerName.EndsWith("-vulkan", StringComparison.OrdinalIgnoreCase))
            return "vulkan";
        if (providerName.EndsWith("-cpu", StringComparison.OrdinalIgnoreCase))
            return "cpu";
            
        // No suffix - default to CPU for safety
        return "cpu";
    }

    /// <summary>
    /// Append backend suffix to provider name based on detected GPU
    /// </summary>
    public async Task<string> AppendBackendSuffix(string providerName, CancellationToken cancellationToken)
    {
        var baseProviderName = providerName;
        // Remove potential existing backend suffix for comparison
        foreach (var suffix in new[] { "-rocm", "-cuda", "-vulkan", "-cpu" })
        {
            if (baseProviderName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                baseProviderName = baseProviderName.Substring(0, baseProviderName.Length - suffix.Length);
                break;
            }
        }
        
        // Skip embeddings provider (shared backend)
        if (baseProviderName.Equals("embeddings", StringComparison.OrdinalIgnoreCase))
        {
            return providerName;
        }
        
        // Already has backend suffix
        if (providerName.EndsWith("-rocm", StringComparison.OrdinalIgnoreCase) ||
            providerName.EndsWith("-cuda", StringComparison.OrdinalIgnoreCase) ||
            providerName.EndsWith("-vulkan", StringComparison.OrdinalIgnoreCase) ||
            providerName.EndsWith("-cpu", StringComparison.OrdinalIgnoreCase))
        {
            return providerName;
        }
        
        // Detect GPU and append appropriate suffix
        var backend = await DetectGPUBackend(cancellationToken);
        
        _logger.LogInformation("🔍 Detected backend: {Backend} for provider {Provider}", backend, providerName);
        
        return backend switch
        {
            "rocm" => $"{providerName}-rocm",
            "cuda" => $"{providerName}-cuda",
            "vulkan" => $"{providerName}-vulkan",
            _ => $"{providerName}-cpu"
        };
    }

    /// <summary>
    /// Strip common backend suffixes from a provider name (e.g., '-cuda', '-rocm', '-vulkan', '-cpu').
    /// Returns the provider name without suffix; useful for package and provider lookups.
    /// </summary>
    private string StripBackendSuffix(string providerName)
    {
        if (string.IsNullOrEmpty(providerName)) return providerName;
        foreach (var suffix in new[] { "-rocm", "-cuda", "-vulkan", "-cpu" })
        {
            if (providerName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return providerName.Substring(0, providerName.Length - suffix.Length);
            }
        }
        return providerName;
    }

    public bool IsFeatureInstalled(string providerName, string featureName)
    {
        var registry = LoadRegistry();
        if (registry.Environments.TryGetValue(providerName, out var entry))
        {
            if (entry.Features.TryGetValue(featureName, out var status))
            {
                // Trust the registry status without checking filesystem
                return status.Installed;
            }
        }
        return false;
    }

    public void MarkFeatureInstalled(string providerName, string featureName, string venvPath)
    {
        var registry = LoadRegistry();
        if (!registry.Environments.TryGetValue(providerName, out var entry))
        {
            entry = new EnvironmentEntry { Name = providerName, Path = venvPath };
            registry.Environments[providerName] = entry;
        }
        
        entry.Path = venvPath; // Ensure path is up to date
        entry.Features[featureName] = new FeatureStatus 
        { 
            Installed = true, 
            LastVerified = DateTime.UtcNow 
        };
        
        SaveRegistry(registry);
    }

    /// <summary>
    /// Get the registered environment path from the registry for a provider.
    /// Returns null if the provider is not registered.
    /// </summary>
    public string? GetRegisteredEnvironmentPath(string providerName)
    {
        var registry = LoadRegistry();
        if (registry.Environments.TryGetValue(providerName, out var entry))
        {
            return entry.Path;
        }
        return null;
    }

    /// <summary>
    /// Remove a provider from the registry (for cleanup of stale/corrupted entries).
    /// </summary>
    public void UnregisterEnvironment(string providerName)
    {
        var registry = LoadRegistry();
        if (registry.Environments.Remove(providerName))
        {
            SaveRegistry(registry);
            _logger.LogInformation("Unregistered provider {Provider} from registry", providerName);
        }
    }

    // --- Package Tracking ---

    /// <summary>
    /// Check if a package is already tracked as installed for a provider.
    /// </summary>
    public bool IsPackageTracked(string providerName, string packageName)
    {
        var registry = LoadRegistry();
        
        // Normalize package name (pip uses lowercase with underscores)
        var normalizedName = NormalizePackageName(packageName);
        
        if (registry.Environments.TryGetValue(providerName, out var entry))
        {
            return entry.InstalledPackages.ContainsKey(normalizedName);
        }
        return false;
    }

    /// <summary>
    /// Mark a package as installed for a provider.
    /// </summary>
    public void MarkPackageInstalled(string providerName, string packageName, string? version = null)
    {
        var registry = LoadRegistry();
        var normalizedName = NormalizePackageName(packageName);
        
        if (!registry.Environments.TryGetValue(providerName, out var entry))
        {
            // Provider not registered yet - this shouldn't happen normally
            _logger.LogWarning("Attempted to mark package installed for unregistered provider: {Provider}", providerName);
            return;
        }
        
        entry.InstalledPackages[normalizedName] = new PackageInfo
        {
            Version = version ?? "",
            InstalledAt = DateTime.UtcNow
        };
        
        SaveRegistry(registry);
        _logger.LogDebug("Marked package {Package} as installed for {Provider}", normalizedName, providerName);
    }

    /// <summary>
    /// Mark multiple packages as installed for a provider.
    /// </summary>
    public void MarkPackagesInstalled(string providerName, IEnumerable<string> packageNames)
    {
        var registry = LoadRegistry();
        
        if (!registry.Environments.TryGetValue(providerName, out var entry))
        {
            _logger.LogWarning("Attempted to mark packages installed for unregistered provider: {Provider}", providerName);
            return;
        }
        
        foreach (var packageName in packageNames)
        {
            var normalizedName = NormalizePackageName(packageName);
            if (!entry.InstalledPackages.ContainsKey(normalizedName))
            {
                entry.InstalledPackages[normalizedName] = new PackageInfo
                {
                    Version = "",
                    InstalledAt = DateTime.UtcNow
                };
            }
        }
        
        SaveRegistry(registry);
        _logger.LogDebug("Marked {Count} packages as installed for {Provider}", packageNames.Count(), providerName);
    }

    /// <summary>
    /// Get all tracked packages for a provider.
    /// </summary>
    public Dictionary<string, string> GetTrackedPackages(string providerName)
    {
        var registry = LoadRegistry();
        
        if (registry.Environments.TryGetValue(providerName, out var entry))
        {
            return entry.InstalledPackages.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value.Version
            );
        }
        return new Dictionary<string, string>();
    }

    /// <summary>
    /// Get list of installed packages from pip in a venv (fast check using pip list --format=freeze)
    /// </summary>
    private async Task<HashSet<string>> GetInstalledPackagesFromPip(string venvPythonExe, CancellationToken cancellationToken)
    {
        var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = venvPythonExe,
                Arguments = "-m pip list --format=freeze",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            // Clear environment to avoid interference
            psi.EnvironmentVariables["PYTHONPATH"] = "";
            psi.EnvironmentVariables["PYTHONHOME"] = "";
            
            using var process = Process.Start(psi);
            if (process == null) return installed;
            
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            
            if (process.ExitCode == 0)
            {
                // Parse output: each line is "package==version" or "package @ path"
                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    
                    // Extract package name (before == or @)
                    var packageName = trimmed.Split(new[] { "==", " @ " }, StringSplitOptions.None)[0].Trim();
                    if (!string.IsNullOrEmpty(packageName))
                    {
                        installed.Add(NormalizePackageName(packageName));
                    }
                }
            }
            
            _logger.LogDebug("Found {Count} packages installed in venv", installed.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get installed packages from pip");
        }
        
        return installed;
    }

    /// <summary>
    /// Normalize package name for consistent tracking (lowercase, underscores).
    /// </summary>
    private static string NormalizePackageName(string packageName)
    {
        // Extract just the package name (strip version specs like >=, ==, etc.)
        var name = packageName.Split(new[] { '>', '=', '<', '[', ';' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        // Pip normalizes to lowercase with underscores
        return name.ToLowerInvariant().Replace('-', '_');
    }

    // --- Environment Registry Persistence ---

    private string _registryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".beep-llm", "environments.json");

    private class EnvironmentRegistry
    {
        public Dictionary<string, EnvironmentEntry> Environments { get; set; } = new();
    }

    private class EnvironmentEntry
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        // public bool IsReady { get; set; } // Deprecated in favor of Features
        public Dictionary<string, FeatureStatus> Features { get; set; } = new();
        public Dictionary<string, PackageInfo> InstalledPackages { get; set; } = new();
    }

    private class FeatureStatus
    {
        public bool Installed { get; set; }
        public DateTime LastVerified { get; set; }
    }

    private class PackageInfo
    {
        public string Version { get; set; } = "";
        public DateTime InstalledAt { get; set; }
    }

    private EnvironmentRegistry LoadRegistry()
    {
        try
        {
            if (File.Exists(_registryPath))
            {
                var json = File.ReadAllText(_registryPath);
                return JsonSerializer.Deserialize<EnvironmentRegistry>(json) ?? new EnvironmentRegistry();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to load environment registry: {Message}", ex.Message);
        }
        return new EnvironmentRegistry();
    }

    private void SaveRegistry(EnvironmentRegistry registry)
    {
        try
        {
            var dir = Path.GetDirectoryName(_registryPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            
            var json = JsonSerializer.Serialize(registry, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_registryPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to save environment registry: {Message}", ex.Message);
        }
    }
}
