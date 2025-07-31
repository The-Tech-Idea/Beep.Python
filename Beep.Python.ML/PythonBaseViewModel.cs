using Beep.Python.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Container.Services;
using Beep.Python.RuntimeEngine.Helpers;

namespace Beep.Python.ML
{
    /// <summary>
    /// Enhanced base view model with proper session management, virtual environment support,
    /// and async operations for Python ML components.
    /// </summary>
    public partial class PythonBaseViewModel : ObservableObject, IDisposable
    {
        #region Private Fields
        private readonly object _operationLock = new object();
        private volatile bool _isDisposed = false;
        #endregion

        #region Observable Properties
        [ObservableProperty]
        IPythonRunTimeManager pythonRuntime;
        
        [ObservableProperty]
        PyModule persistentScope;
        
        [ObservableProperty]
        bool disposedValue;
        
        [ObservableProperty]
        public CancellationTokenSource tokenSource;
        
        [ObservableProperty]
        public CancellationToken token;
        
        [ObservableProperty]
        public IProgress<PassedArgs> progress;
        
        [ObservableProperty]
        IDMEEditor editor;
        
        [ObservableProperty]
        bool isBusy;
        
        [ObservableProperty]
        string pythonDatafolder;
        
        [ObservableProperty]
        List<LOVData> listofAlgorithims;
        
        [ObservableProperty]
        List<ParameterDictionaryForAlgorithm> parameterDictionaryForAlgorithms;
        
        [ObservableProperty]
        List<string> algorithims;
        #endregion

        #region Core Dependencies
        public readonly IBeepService Beepservice;
        public readonly IPythonVirtualEnvManager VirtualEnvManager;
        public readonly IPythonSessionManager SessionManager;
        
        // Session and Environment management
        protected PythonSessionInfo? ConfiguredSession;
        protected PythonVirtualEnvironment? ConfiguredVirtualEnvironment;
        protected PyModule? SessionScope;
        #endregion

        #region Properties
        public PythonSessionInfo SessionInfo { get; protected set; }
        public bool IsInitialized { get; protected set; } = false;
        public bool IsSessionConfigured => ConfiguredSession != null && ConfiguredVirtualEnvironment != null && SessionScope != null;
        public ObservableBindingList<PythonRunTime> AvailablePythonInstallations => pythonRuntime?.PythonInstallations ?? new ObservableBindingList<PythonRunTime>();
        #endregion

        #region Constructors
        public PythonBaseViewModel(
            IBeepService beepservice, 
            IPythonRunTimeManager pythonRuntimeManager, 
            PythonSessionInfo sessionInfo)
        {
            Beepservice = beepservice ?? throw new ArgumentNullException(nameof(beepservice));
            PythonRuntime = pythonRuntimeManager ?? throw new ArgumentNullException(nameof(pythonRuntimeManager));
            SessionInfo = sessionInfo ?? throw new ArgumentNullException(nameof(sessionInfo));

            Editor = beepservice.DMEEditor;
            Progress = beepservice.DMEEditor.progress;
            
            VirtualEnvManager = pythonRuntimeManager.VirtualEnvmanager;
            SessionManager = pythonRuntimeManager.SessionManager;

            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;

            InitializeCommonComponents();
        }

        /// <summary>
        /// Enhanced constructor with virtual environment and session managers
        /// </summary>
        public PythonBaseViewModel(
            IBeepService beepservice,
            IPythonRunTimeManager pythonRuntimeManager,
            IPythonVirtualEnvManager virtualEnvManager,
            IPythonSessionManager sessionManager,
            PythonSessionInfo sessionInfo)
        {
            Beepservice = beepservice ?? throw new ArgumentNullException(nameof(beepservice));
            PythonRuntime = pythonRuntimeManager ?? throw new ArgumentNullException(nameof(pythonRuntimeManager));
            VirtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
            SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            SessionInfo = sessionInfo ?? throw new ArgumentNullException(nameof(sessionInfo));

            Editor = beepservice.DMEEditor;
            Progress = beepservice.DMEEditor.progress;

            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;

            InitializeCommonComponents();
        }
        #endregion

        #region Initialization
        private void InitializeCommonComponents()
        {
            InitializeParameterDictionary();
            InitializePythonDataFolder();
            InitializeAlgorithmsList();
            
            IsInitialized = PythonRuntime?.IsInitialized ?? false;
        }

        private void InitializeParameterDictionary()
        {
            try
            {
                ParameterDictionaryForAlgorithms = MLAlgorithmsHelpers.GetParameterDictionaryForAlgorithms();
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to initialize parameter dictionary: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                ParameterDictionaryForAlgorithms = new List<ParameterDictionaryForAlgorithm>();
            }
        }

        private void InitializePythonDataFolder()
        {
            try
            {
                // Use a default path if Editor or Config is not available
                string basePath = Editor?.ConfigEditor?.Config?.Folders?.FirstOrDefault()?.FolderPath ?? 
                                  System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                PythonDatafolder = System.IO.Path.Combine(basePath, "Beep", "Python", "ML");
                System.IO.Directory.CreateDirectory(PythonDatafolder);
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to initialize Python data folder: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                PythonDatafolder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Beep", "Python", "ML");
            }
        }

        private void InitializeAlgorithmsList()
        {
            try
            {
                ListofAlgorithims = new List<LOVData>();
                Algorithims = new List<string>();

                foreach (var algorithmName in Enum.GetNames(typeof(MachineLearningAlgorithm)))
                {
                    var algorithm = (MachineLearningAlgorithm)Enum.Parse(typeof(MachineLearningAlgorithm), algorithmName);
                    var description = MLAlgorithmsHelpers.GenerateAlgorithmDescription(algorithm);
                    
                    ListofAlgorithims.Add(new LOVData 
                    { 
                        ID = algorithmName, 
                        DisplayValue = algorithmName, 
                        LOVDESCRIPTION = description 
                    });
                    
                    Algorithims.Add(algorithmName);
                }
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to initialize algorithms list: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                ListofAlgorithims = new List<LOVData>();
                Algorithims = new List<string>();
            }
        }

        protected virtual void InitializePythonEnvironment()
        {
            if (!IsInitialized && PythonRuntime != null)
            {
                try
                {
                    // Basic Python environment setup can be done here if needed
                    IsInitialized = PythonRuntime.IsInitialized;
                }
                catch (Exception ex)
                {
                    Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to initialize Python environment: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                }
            }
        }
        #endregion

        #region Enhanced Session Management
        /// <summary>
        /// Configure the view model to use a specific Python session and virtual environment
        /// </summary>
        /// <param name="session">Pre-existing Python session to use for execution</param>
        /// <param name="virtualEnvironment">Virtual environment associated with the session</param>
        /// <returns>True if configuration successful</returns>
        public virtual bool ConfigureSession(PythonSessionInfo session, PythonVirtualEnvironment virtualEnvironment)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (virtualEnvironment == null)
                throw new ArgumentNullException(nameof(virtualEnvironment));

            try
            {
                ConfiguredSession = session;
                ConfiguredVirtualEnvironment = virtualEnvironment;
                SessionInfo = session;

                // Get or create the session scope
                if (PythonRuntime.HasScope(session))
                {
                    SessionScope = PythonRuntime.GetScope(session);
                }
                else
                {
                    if (PythonRuntime.CreateScope(session, virtualEnvironment))
                    {
                        SessionScope = PythonRuntime.GetScope(session);
                    }
                    else
                    {
                        Editor?.AddLogMessage("PythonBaseViewModel", "Failed to create Python scope for session", DateTime.Now, -1, null, Errors.Failed);
                        return false;
                    }
                }

                InitializePythonEnvironment();
                return true;
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to configure session: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Configure session using username and optional environment ID
        /// </summary>
        /// <param name="username">Username for session creation</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <returns>True if configuration successful</returns>
        public virtual bool ConfigureSessionForUser(string username, string? environmentId = null)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            try
            {
                // Create or get session for user
                var session = SessionManager?.CreateSession(username, environmentId);
                if (session == null)
                {
                    Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to create session for user: {username}", DateTime.Now, -1, null, Errors.Failed);
                    return false;
                }

                // Get the associated virtual environment
                var environment = VirtualEnvManager?.GetEnvironmentByPath(session.VirtualEnvironmentId);
                if (environment == null)
                {
                    Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to get environment for session: {session.SessionId}", DateTime.Now, -1, null, Errors.Failed);
                    return false;
                }

                return ConfigureSession(session, environment);
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to configure session for user {username}: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Get the currently configured session
        /// </summary>
        public PythonSessionInfo? GetConfiguredSession()
        {
            return ConfiguredSession;
        }

        /// <summary>
        /// Get the currently configured virtual environment
        /// </summary>
        public PythonVirtualEnvironment? GetConfiguredVirtualEnvironment()
        {
            return ConfiguredVirtualEnvironment;
        }
        #endregion

        #region Session-based Execution Helpers
        /// <summary>
        /// Executes code safely within the session context
        /// </summary>
        /// <param name="action">Action to execute in session</param>
        protected void ExecuteInSession(Action action)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PythonBaseViewModel));

            if (!IsSessionConfigured)
                throw new InvalidOperationException("Session must be configured before executing operations");

            lock (_operationLock)
            {
                try
                {
                    action();
                }
                catch (PythonException pythonEx)
                {
                    Editor?.AddLogMessage("PythonBaseViewModel", $"Python execution error: {pythonEx.Message}", DateTime.Now, -1, null, Errors.Failed);
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    Editor?.AddLogMessage("PythonBaseViewModel", $"Session execution error: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    throw new InvalidOperationException($"Session execution error: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Executes Python code using the session scope instead of manual GIL management
        /// </summary>
        protected bool ExecuteInSession(string script)
        {
            if (!IsSessionConfigured || SessionInfo == null)
                return false;

            return PythonRuntime.ExecuteManager.RunPythonScript(script, null, SessionInfo);
        }

        /// <summary>
        /// Executes Python code asynchronously with session support
        /// </summary>
        /// <param name="code">Python code to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if execution successful</returns>
        protected async Task<bool> ExecuteInSessionAsync(string code, CancellationToken cancellationToken = default)
        {
            if (!IsSessionConfigured)
                throw new InvalidOperationException("Session must be configured before executing operations");

            try
            {
                var result = await PythonRuntime.ExecuteManager.ExecuteCodeAsync(code, SessionInfo, 120, Progress);
                return result.Success;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonBaseViewModel", $"Async execution error: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                return false;
            }
        }

        /// <summary>
        /// Gets string array from Python session using ExecuteManager
        /// </summary>
        protected string[] GetStringArrayFromSession(string variableName)
        {
            if (!IsSessionConfigured || SessionInfo == null)
                return Array.Empty<string>();

            try
            {
                string script = $@"
import json
if '{variableName}' in globals():
    result_json = json.dumps({variableName})
else:
    result_json = '[]'
";
                ExecuteInSession(script);
                
                var jsonResult = PythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(null, "result_json", SessionInfo);
                
                if (!string.IsNullOrEmpty(jsonResult?.ToString()))
                {
                    var cleanJson = jsonResult.ToString().Trim('"').Replace("\\\"", "\"");
                    var result = System.Text.Json.JsonSerializer.Deserialize<string[]>(cleanJson);
                    return result ?? Array.Empty<string>();
                }
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to get string array from session: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return Array.Empty<string>();
        }

        /// <summary>
        /// Gets data from Python session scope without manual GIL management
        /// </summary>
        protected T GetFromSessionScope<T>(string variableName, T defaultValue = default(T))
        {
            if (!IsSessionConfigured || SessionInfo == null)
                return defaultValue;

            try
            {
                string script = $@"
import json
if '{variableName}' in globals():
    if isinstance({variableName}, (list, dict, str, int, float, bool)):
        result_json = json.dumps({variableName})
    else:
        result_json = json.dumps(str({variableName}))
else:
    result_json = 'null'
";
                ExecuteInSession(script);
                
                var jsonResult = PythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(null, "result_json", SessionInfo);
                
                if (!string.IsNullOrEmpty(jsonResult?.ToString()))
                {
                    var cleanJson = jsonResult.ToString().Trim('"').Replace("\\\"", "\"");
                    if (cleanJson != "null")
                    {
                        var result = System.Text.Json.JsonSerializer.Deserialize<T>(cleanJson);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to get data from session scope: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }

            return defaultValue;
        }
        #endregion

        #region Python Module Management
        public virtual void ImportPythonModule(string moduleName)
        {
            if (!IsSessionConfigured)
                throw new InvalidOperationException("Session must be configured before importing modules");

            string script = $"import {moduleName}";
            ExecuteInSession(script);
        }
        #endregion

        #region Utility Methods
        public async Task RefreshPythonInstallationsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    PythonRuntime?.RefreshPythonInstalltions();
                }
                catch (Exception ex)
                {
                    Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to refresh Python installations: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                }
            });
        }

        public async Task<bool> AddCustomPythonInstallation(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var runtime = PythonRuntime?.Initialize(path);
                    return runtime != null;
                }
                catch (Exception ex)
                {
                    Editor?.AddLogMessage("PythonBaseViewModel", $"Failed to add custom Python installation: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    return false;
                }
            });
        }
        #endregion

        #region Disposal
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    try
                    {
                        TokenSource?.Cancel();
                        TokenSource?.Dispose();
                        
                        // Clean up session resources
                        if (ConfiguredSession != null)
                        {
                            SessionManager?.CleanupSession(ConfiguredSession);
                        }
                    }
                    catch (Exception ex)
                    {
                        Editor?.AddLogMessage("PythonBaseViewModel", $"Error during disposal: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                    }
                }
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
