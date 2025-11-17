using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// High-level orchestrator for Python runtime management.
    /// Provides a developer-friendly API for managing Python environments, sessions, and code execution.
    /// This is the main entry point for working with the Python runtime framework.
    /// </summary>
    public interface IPythonRuntimeOrchestrator : IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets the underlying runtime manager.
        /// </summary>
        IPythonRunTimeManager RuntimeManager { get; }

        /// <summary>
        /// Gets the session manager.
        /// </summary>
        IPythonSessionManager SessionManager { get; }

        /// <summary>
        /// Gets the virtual environment manager.
        /// </summary>
        IPythonVirtualEnvManager VirtualEnvManager { get; }

        /// <summary>
        /// Gets the code execution manager.
        /// </summary>
        IPythonCodeExecuteManager ExecuteManager { get; }

        /// <summary>
        /// Gets the current operational mode (SingleUser or MultiUser).
        /// </summary>
        PythonEngineMode Mode { get; }

        /// <summary>
        /// Gets whether the orchestrator is initialized and ready.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets the admin/base environment used for package management and system operations.
        /// </summary>
        PythonVirtualEnvironment AdminEnvironment { get; }

        /// <summary>
        /// Gets the current active environment (for single-user mode).
        /// </summary>
        PythonVirtualEnvironment CurrentEnvironment { get; }

        /// <summary>
        /// Gets all available Python installations.
        /// </summary>
        ObservableBindingList<PythonRunTime> AvailablePythonInstallations { get; }

        /// <summary>
        /// Gets all managed virtual environments.
        /// </summary>
        ObservableBindingList<PythonVirtualEnvironment> ManagedEnvironments { get; }

        /// <summary>
        /// Gets or sets the working directory for Python environments.
        /// When not set, defaults to Python runtime root path.
        /// Set this to shell directory when using from BeepShell.
        /// </summary>
        string? WorkingDirectory { get; set; }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the orchestrator with an embedded Python runtime (downloads if needed).
        /// This creates an admin environment with all necessary packages for management.
        /// </summary>
        /// <param name="mode">Operating mode (SingleUser or MultiUser)</param>
        /// <param name="basePackages">List of base packages to install in admin environment</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if initialization succeeded</returns>
        Task<bool> InitializeWithEmbeddedPythonAsync(
            PythonEngineMode mode = PythonEngineMode.SingleUser,
            List<string> basePackages = null,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes the orchestrator with an existing Python installation.
        /// This creates an admin environment with all necessary packages for management.
        /// </summary>
        /// <param name="pythonPath">Path to existing Python installation</param>
        /// <param name="mode">Operating mode (SingleUser or MultiUser)</param>
        /// <param name="basePackages">List of base packages to install in admin environment</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if initialization succeeded</returns>
        Task<bool> InitializeWithExistingPythonAsync(
            string pythonPath,
            PythonEngineMode mode = PythonEngineMode.SingleUser,
            List<string> basePackages = null,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reinitializes the orchestrator with a different mode or configuration.
        /// </summary>
        /// <param name="newMode">New operational mode</param>
        /// <returns>True if reinitialization succeeded</returns>
        Task<bool> ReinitializeAsync(PythonEngineMode newMode);

        #endregion

        #region Single-User Mode Operations

        /// <summary>
        /// Sets the current active environment for single-user mode.
        /// </summary>
        /// <param name="environmentId">ID of the environment to set as current</param>
        /// <returns>True if the environment was set successfully</returns>
        bool SetCurrentEnvironment(string environmentId);

        /// <summary>
        /// Creates a new environment and sets it as current (single-user mode).
        /// </summary>
        /// <param name="envName">Name for the new environment</param>
        /// <param name="packageProfiles">Package profiles to install (e.g., "base", "data-science")</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created environment</returns>
        Task<PythonVirtualEnvironment> CreateAndSetEnvironmentAsync(
            string envName,
            List<string> packageProfiles = null,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes Python code in the current environment (single-user mode).
        /// </summary>
        /// <param name="code">Python code to execute</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>Execution result (success, output)</returns>
        Task<(bool Success, string Output)> ExecuteAsync(
            string code,
            int timeout = 120,
            IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Gets the current session for single-user mode.
        /// </summary>
        /// <returns>The current session</returns>
        PythonSessionInfo GetCurrentSession();

        #endregion

        #region Multi-User Mode Operations

        /// <summary>
        /// Creates a new environment for a specific user (multi-user mode).
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="envName">Optional environment name (defaults to username)</param>
        /// <param name="packageProfiles">Package profiles to install</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created environment and session</returns>
        Task<(PythonVirtualEnvironment Environment, PythonSessionInfo Session)> CreateUserEnvironmentAsync(
            string username,
            string envName = null,
            List<string> packageProfiles = null,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets or creates a session for a user in multi-user mode.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="environmentId">Optional environment ID (auto-selects if null)</param>
        /// <returns>The user's session</returns>
        PythonSessionInfo GetOrCreateUserSession(string username, string environmentId = null);

        /// <summary>
        /// Executes Python code for a specific user (multi-user mode).
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="code">Python code to execute</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>Execution result (success, output)</returns>
        Task<(bool Success, string Output)> ExecuteForUserAsync(
            string username,
            string code,
            int timeout = 120,
            IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Terminates a user's session.
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>Error information</returns>
        IErrorsInfo TerminateUserSession(string username);

        #endregion

        #region Environment Management

        /// <summary>
        /// Creates a new virtual environment with specified configuration.
        /// </summary>
        /// <param name="envName">Environment name</param>
        /// <param name="envPath">Optional custom path (auto-generated if null)</param>
        /// <param name="packageProfiles">Package profiles to install</param>
        /// <param name="createdBy">User creating the environment</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created environment</returns>
        Task<PythonVirtualEnvironment> CreateEnvironmentAsync(
            string envName,
            string envPath = null,
            List<string> packageProfiles = null,
            string createdBy = "System",
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an environment by name or ID.
        /// </summary>
        /// <param name="nameOrId">Environment name or ID</param>
        /// <returns>The environment if found, null otherwise</returns>
        PythonVirtualEnvironment GetEnvironment(string nameOrId);

        /// <summary>
        /// Deletes an environment and cleans up resources.
        /// </summary>
        /// <param name="environmentId">Environment ID to delete</param>
        /// <returns>True if deletion succeeded</returns>
        bool DeleteEnvironment(string environmentId);

        /// <summary>
        /// Installs packages in a specific environment.
        /// </summary>
        /// <param name="environmentId">Environment ID</param>
        /// <param name="packages">Packages to install</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>True if installation succeeded</returns>
        Task<bool> InstallPackagesAsync(
            string environmentId,
            List<string> packages,
            IProgress<string> progress = null);

        #endregion

        #region Code Execution (Advanced)

        /// <summary>
        /// Executes code in a specific session with variables.
        /// </summary>
        /// <param name="session">Session to execute in</param>
        /// <param name="code">Python code</param>
        /// <param name="variables">Variables to pass to Python</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>Execution result</returns>
        Task<object> ExecuteWithVariablesAsync(
            PythonSessionInfo session,
            string code,
            Dictionary<string, object> variables,
            int timeout = 120,
            IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Executes a Python script file in a session.
        /// </summary>
        /// <param name="session">Session to execute in</param>
        /// <param name="filePath">Path to Python script</param>
        /// <param name="timeout">Timeout in seconds</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>Execution result</returns>
        Task<(bool Success, string Output)> ExecuteScriptAsync(
            PythonSessionInfo session,
            string filePath,
            int timeout = 300,
            IProgress<PassedArgs> progress = null);

        /// <summary>
        /// Executes batch commands in a session.
        /// </summary>
        /// <param name="session">Session to execute in</param>
        /// <param name="commands">List of commands</param>
        /// <param name="progress">Progress reporter</param>
        /// <returns>List of results for each command</returns>
        Task<List<object>> ExecuteBatchAsync(
            PythonSessionInfo session,
            IList<string> commands,
            IProgress<PassedArgs> progress = null);

        #endregion

        #region Utilities

        /// <summary>
        /// Refreshes the list of available Python installations.
        /// </summary>
        void RefreshPythonInstallations();

        /// <summary>
        /// Performs cleanup of stale sessions and environments.
        /// </summary>
        /// <param name="sessionMaxAge">Maximum age for sessions</param>
        /// <param name="environmentMaxIdleTime">Maximum idle time for environments</param>
        void PerformMaintenance(
            TimeSpan? sessionMaxAge = null,
            TimeSpan? environmentMaxIdleTime = null);

        /// <summary>
        /// Gets diagnostic information about the orchestrator state.
        /// </summary>
        /// <returns>Diagnostic information dictionary</returns>
        Dictionary<string, object> GetDiagnostics();

        /// <summary>
        /// Switches between single-user and multi-user modes.
        /// </summary>
        /// <param name="newMode">New mode to switch to</param>
        /// <returns>True if switch succeeded</returns>
        Task<bool> SwitchModeAsync(PythonEngineMode newMode);

        #endregion
    }
}
