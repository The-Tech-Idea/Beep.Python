using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW;

namespace Beep.Python.Model
{
    /// <summary>
    /// Enhanced Python package manager interface with session management, virtual environment support,
    /// and comprehensive package management capabilities
    /// </summary>
    public interface IPythonPackageManager : IDisposable
    {
        UnitofWork<PackageDefinition> UnitofWork { get; set; }

        #region Session and Environment Management
        /// <summary>
        /// Configure the package manager to use a specific Python session and virtual environment
        /// This is the recommended approach for multi-user environments
        /// </summary>
        /// <param name="session">Pre-existing Python session to use for execution</param>
        /// <param name="virtualEnvironment">Virtual environment associated with the session</param>
        /// <returns>True if configuration successful</returns>
        bool ConfigureSession(PythonSessionInfo session, PythonVirtualEnvironment virtualEnvironment);

        /// <summary>
        /// Configure session using username and optional environment ID
        /// This method will create or reuse a session for the specified user
        /// </summary>
        /// <param name="username">Username for session creation</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <returns>True if configuration successful</returns>
        bool ConfigureSessionForUser(string username, string? environmentId = null);

        /// <summary>
        /// Legacy compatibility - sets the active session and environment for package operations
        /// Use ConfigureSession or ConfigureSessionForUser for better session management
        /// </summary>
        void SetActiveSessionAndEnvironment(PythonSessionInfo session, PythonVirtualEnvironment environment);

        /// <summary>
        /// Get the currently configured session, if any
        /// </summary>
        /// <returns>The configured Python session, or null if not configured</returns>
        PythonSessionInfo? GetConfiguredSession();

        /// <summary>
        /// Get the currently configured virtual environment, if any
        /// </summary>
        /// <returns>The configured virtual environment, or null if not configured</returns>
        PythonVirtualEnvironment? GetConfiguredVirtualEnvironment();

        /// <summary>
        /// Check if session is properly configured
        /// </summary>
        /// <returns>True if session and environment are configured</returns>
        bool IsSessionConfigured();
        #endregion

        #region Package Management Core Methods
        // Existing synchronous methods
        bool InstallNewPackageAsync(string packagename);
        bool InstallPipToolAsync();
        bool RefreshAllPackagesAsync();
        bool RefreshPackageAsync(string packagename);
        bool UnInstallPackageAsync(string packagename);
        bool UpgradeAllPackagesAsync();
        bool UpgradePackageAsync(string packagename);

        // New asynchronous methods with session support
        /// <summary>
        /// Installs a new package asynchronously with session support
        /// </summary>
        Task<bool> InstallNewPackageWithSessionAsync(string packageName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes information for all packages asynchronously with session support
        /// </summary>
        Task<bool> RefreshAllPackagesWithSessionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes information for a specific package asynchronously with session support
        /// </summary>
        Task<bool> RefreshPackageWithSessionAsync(string packageName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uninstalls a package asynchronously with session support
        /// </summary>
        Task<bool> UnInstallPackageWithSessionAsync(string packageName, CancellationToken cancellationToken = default);
        #endregion

        #region Requirements File Management
        // New methods for requirements file support
        bool InstallFromRequirementsFile(string filePath);
        bool GenerateRequirementsFile(string filePath, bool includeVersions = true);
        bool UpdateVirtualEnvironmentWithRequirementsFile(PythonVirtualEnvironment environment);

        /// <summary>
        /// Installs packages from a requirements file asynchronously with session support
        /// </summary>
        Task<bool> InstallFromRequirementsFileWithSessionAsync(string filePath, CancellationToken cancellationToken = default);
        #endregion

        #region Package Category Management
        // New methods for categorized package management
        ObservableBindingList<PackageDefinition> GetPackagesByCategory(PackageCategory category);
        void SetPackageCategory(string packageName, PackageCategory category);
        void UpdatePackageCategories(Dictionary<string, PackageCategory> packageCategories);

        // Method to populate common package categories
        bool PopulateCommonPackageCategories();

        // Method to suggest categories for uncategorized packages
        Task<Dictionary<string, PackageCategory>> SuggestCategoriesForPackages(IEnumerable<string> packageNames);
        #endregion

        #region Package Set Management
        // Methods to work with predefined package sets
        bool InstallPackageSet(string setName);
        Dictionary<string, List<PackageDefinition>> GetAvailablePackageSets();
        bool SavePackageSetFromCurrentEnvironment(string setName, string description = "");
        #endregion
    }
}