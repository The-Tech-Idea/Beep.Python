using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

public interface IVenvManager
{
    Task<string?> EnsureProviderEnvironment(string providerName, string? modelId = null, CancellationToken cancellationToken = default);
    Task<string?> EnsureEngineEnvironment(string engineName, CancellationToken cancellationToken = default);
    Task<bool> RunPipCommand(string pythonExe, string pipArgs, CancellationToken cancellationToken);
    Task<(bool Success, string Output, string Error)> RunScriptInVenv(string pythonExe, string scriptPath, string arguments = "", IProgress<string>? lineProgress = null, CancellationToken cancellationToken = default);
    Task<bool> VerifyPackagesInstalled(string venvPythonExe, IEnumerable<string> packageNames, string environmentName, CancellationToken cancellationToken, IEnumerable<string>? optionalPackages = null);
    Task<bool> DeleteVirtualEnvironment(string venvPath, CancellationToken cancellationToken = default);
    Task<string?> CreateProviderVirtualEnvironment(string providerName, string? modelId, CancellationToken cancellationToken = default);
    Task<List<ProviderPackageInfo>> GetProviderPackageStatus(string providerName, CancellationToken cancellationToken = default);
    Dictionary<string, string>? GetProviderPackages(string providerName);
    // Embeddings-specific packages are provider/project-managed; this method has been removed to avoid
    // core-level hardcoding. Providers should call InstallProviderPackagesInVenv with their package lists.
    string ComputeProviderVenvName(string providerName, string? modelId);
    string SlugifyModelId(string modelId);
    Task<bool> InstallProviderPackagesInVenv(string providerName, string venvPythonExe, Dictionary<string, string> packages, CancellationToken cancellationToken, IEnumerable<string>? optionalPackages = null);
    Task<bool> InstallEnginePackagesInVenv(string engineName, string venvPythonExe, Dictionary<string, string> packages, CancellationToken cancellationToken, IEnumerable<string>? optionalPackages = null);
    Task<bool> DownloadEmbeddingModel(string venvPythonExe, string providerName, string modelId, CancellationToken cancellationToken);
    Task<string> DetectGPUBackend(CancellationToken cancellationToken);
    string DetermineBackendFromProviderName(string providerName);
    Task<string> AppendBackendSuffix(string providerName, CancellationToken cancellationToken);
    
    // Feature/Capability Management
    bool IsFeatureInstalled(string providerName, string featureName);
    void MarkFeatureInstalled(string providerName, string featureName, string venvPath);
    
    /// <summary>
    /// Get the registered environment path from the registry for a provider.
    /// Returns null if the provider is not registered.
    /// </summary>
    string? GetRegisteredEnvironmentPath(string providerName);
    
    /// <summary>
    /// Remove a provider from the registry (for cleanup of stale/corrupted entries).
    /// </summary>
    void UnregisterEnvironment(string providerName);
    
    // Package Tracking - tracks what packages are installed to avoid reinstalling
    /// <summary>
    /// Check if a package is already tracked as installed for a provider.
    /// </summary>
    bool IsPackageTracked(string providerName, string packageName);
    
    /// <summary>
    /// Mark a package as installed for a provider.
    /// </summary>
    void MarkPackageInstalled(string providerName, string packageName, string? version = null);
    
    /// <summary>
    /// Mark multiple packages as installed for a provider.
    /// </summary>
    void MarkPackagesInstalled(string providerName, IEnumerable<string> packageNames);
    
    /// <summary>
    /// Get all tracked packages for a provider.
    /// </summary>
    Dictionary<string, string> GetTrackedPackages(string providerName);
}
