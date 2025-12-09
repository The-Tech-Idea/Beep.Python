namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Represents the installation status of a Python package
/// </summary>
public enum PackageStatus
{
    /// <summary>
    /// Package is not installed
    /// </summary>
    NotInstalled,
    
    /// <summary>
    /// Package is currently being installed
    /// </summary>
    Installing,
    
    /// <summary>
    /// Package is installed and verified
    /// </summary>
    Installed,
    
    /// <summary>
    /// Package is installed but needs update
    /// </summary>
    NeedsUpdate,
    
    /// <summary>
    /// Package installation failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// Package version mismatch detected
    /// </summary>
    VersionMismatch
}
