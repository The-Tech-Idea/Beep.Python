using System;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Information about a Python package required by a provider
/// </summary>
public class ProviderPackageInfo
{
    /// <summary>
    /// Package name (e.g., "torch", "transformers")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Required version constraint (e.g., ">=2.1.0", "==4.40.0")
    /// </summary>
    public string VersionConstraint { get; set; } = string.Empty;
    
    /// <summary>
    /// Current installation status
    /// </summary>
    public PackageStatus Status { get; set; } = PackageStatus.NotInstalled;
    
    /// <summary>
    /// Installed version (if installed)
    /// </summary>
    public string? InstalledVersion { get; set; }
    
    /// <summary>
    /// Last verification timestamp
    /// </summary>
    public DateTime? LastVerified { get; set; }
    
    /// <summary>
    /// Error message if status is Failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
