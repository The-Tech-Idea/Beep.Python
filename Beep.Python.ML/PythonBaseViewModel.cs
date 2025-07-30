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

        #region Constructor
        public PythonBaseViewModel(
            IBeepService beepservice, 
            IPythonRunTimeManager pythonRuntimeManager, 
            PythonSessionInfo sessionInfo)
        {
            Beepservice = beepservice ?? throw new ArgumentNullException(nameof(beepservice));
            Editor = beepservice.DMEEditor;
            PythonRuntime = pythonRuntimeManager ?? throw new ArgumentNullException(nameof(pythonRuntimeManager));
            SessionInfo = sessionInfo;

            // Get related managers from the runtime manager
            VirtualEnvManager = pythonRuntimeManager.VirtualEnvmanager;
            SessionManager = pythonRuntimeManager.SessionManager;

            InitializeTokenSource();
            InitializeAlgorithmData();
            InitializeProgressReporting();
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
            Editor = beepservice.DMEEditor;
            PythonRuntime = pythonRuntimeManager ?? throw new ArgumentNullException(nameof(pythonRuntimeManager));
            VirtualEnvManager = virtualEnvManager ?? throw new ArgumentNullException(nameof(virtualEnvManager));
            SessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            SessionInfo = sessionInfo;

            InitializeTokenSource();
            InitializeAlgorithmData();
            InitializeProgressReporting();
        }
        #endregion

        #region Initialization Methods
        private void InitializeTokenSource()
        {
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;
        }

        private void InitializeAlgorithmData()
        {
            ListofAlgorithims = new List<LOVData>();
            foreach (var item in Enum.GetNames(typeof(MachineLearningAlgorithm)))
            {
                var algorithm = (MachineLearningAlgorithm)Enum.Parse(typeof(MachineLearningAlgorithm), item);
                LOVData data = new LOVData() 
                { 
                    ID = item, 
                    DisplayValue = item, 
                    LOVDESCRIPTION = MLAlgorithmsHelpers.GenerateAlgorithmDescription(algorithm) 
                };
                ListofAlgorithims.Add(data);
            }
            
            Algorithims = MLAlgorithmsHelpers.GetAlgorithms();
            ParameterDictionaryForAlgorithms = MLAlgorithmsHelpers.GetParameterDictionaryForAlgorithms();
        }

        private void InitializeProgressReporting()
        {
            Progress = new Progress<PassedArgs>(args =>
            {
                if (Editor != null)
                {
                    var errorLevel = args.EventType == "Error" ? Errors.Failed : Errors.Ok;
                    Editor.AddLogMessage("Python ML", args.Messege, DateTime.Now, -1, null, errorLevel);
                }
            });
        }
        #endregion

        #region Session and Environment Configuration
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

            // Validate that session is associated with the environment
            if (session.VirtualEnvironmentId != virtualEnvironment.ID)
            {
                throw new ArgumentException("Session must be associated with the provided virtual environment");
            }

            // Validate session is active
            if (session.Status != PythonSessionStatus.Active)
            {
                throw new ArgumentException("Session must be in Active status");
            }

            ConfiguredSession = session;
            ConfiguredVirtualEnvironment = virtualEnvironment;
            SessionInfo = session; // Update the session info

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
                    throw new InvalidOperationException("Failed to create Python scope for session");
                }
            }

            // Initialize basic Python environment
            InitializePythonEnvironment();

            return true;
        }

        /// <summary>
        /// Configure session using username and optional environment ID
        /// </summary>
        /// <param name="username">Username for session creation</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <returns>True if configuration successful</returns>
        public virtual bool ConfigureSessionForUser(string username, string? environmentId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            if (SessionManager == null)
                throw new InvalidOperationException("Session manager is not available");

            // Create or get existing session for the user
            var session = SessionManager.CreateSession(username, environmentId);
            if (session == null)
            {
                throw new InvalidOperationException($"Failed to create session for user: {username}");
            }

            // Get the virtual environment for this session
            var virtualEnvironment = VirtualEnvManager?.GetEnvironmentById(session.VirtualEnvironmentId);
            if (virtualEnvironment == null)
            {
                throw new InvalidOperationException($"Virtual environment not found for session: {session.SessionId}");
            }

            return ConfigureSession(session, virtualEnvironment);
        }

        /// <summary>
        /// Get the currently configured session
        /// </summary>
        public PythonSessionInfo? GetConfiguredSession() => ConfiguredSession;

        /// <summary>
        /// Get the currently configured virtual environment
        /// </summary>
        public PythonVirtualEnvironment? GetConfiguredVirtualEnvironment() => ConfiguredVirtualEnvironment;

        protected virtual void InitializePythonEnvironment()
        {
            if (!IsSessionConfigured)
                throw new InvalidOperationException("Session must be configured before initializing Python environment");

            try
            {
                ExecuteInSession(() =>
                {
                    string initScript = @"
import sys
import os
import traceback
try:
    print('Basic Python environment initialized successfully')
except Exception as e:
    print(f'Environment initialization error: {e}')
    traceback.print_exc()
";
                    SessionScope!.Exec(initScript);
                });

                IsInitialized = true;
                ReportProgress("Python environment initialized successfully");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Python environment: {ex.Message}", ex);
            }
        }
        #endregion

        #region Core Helper Methods
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
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Session execution error: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Executes code safely within the session context and returns a result
        /// </summary>
        /// <typeparam name="T">Type of result to return</typeparam>
        /// <param name="func">Function to execute in session</param>
        /// <returns>Result of the function</returns>
        protected T ExecuteInSession<T>(Func<T> func)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PythonBaseViewModel));

            if (!IsSessionConfigured)
                throw new InvalidOperationException("Session must be configured before executing operations");

            lock (_operationLock)
            {
                try
                {
                    return func();
                }
                catch (PythonException pythonEx)
                {
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Session execution error: {ex.Message}", ex);
                }
            }
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
                var result = await PythonRuntime.ExecuteManager.RunCode(ConfiguredSession!, code, Progress, cancellationToken);
                return result.Flag == Errors.Ok;
            }
            catch (Exception ex)
            {
                ReportError($"Async execution error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Utility Methods
        public string GetAlgorithimName(string algorithim)
        {
            return Enum.GetName(typeof(MachineLearningAlgorithm), algorithim) ?? algorithim;
        }

        public virtual void ImportPythonModule(string moduleName)
        {
            if (!IsSessionConfigured)
            {
                ReportError("Session must be configured before importing modules");
                return;
            }

            try
            {
                string script = $"import {moduleName}";
                ExecuteInSession(() => SessionScope!.Exec(script));
                ReportProgress($"Successfully imported module: {moduleName}");
            }
            catch (Exception ex)
            {
                ReportError($"Failed to import module {moduleName}: {ex.Message}");
            }
        }

        public void SendMessege(string messege = null)
        {
            if (Progress != null)
            {
                PassedArgs ps = new PassedArgs 
                { 
                    EventType = "Update", 
                    Messege = messege, 
                    ParameterString1 = Editor?.ErrorObject?.Message 
                };
                Progress.Report(ps);
            }
        }

        public async Task RefreshPythonInstallationsAsync()
        {
            IsBusy = true;
            try
            {
                await Task.Run(() => {
                    PythonRuntime.RefreshPythonInstalltions();
                });

                // Update any UI properties
                OnPropertyChanged(nameof(AvailablePythonInstallations));
                ReportProgress("Python installations refreshed");
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh Python installations: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<bool> AddCustomPythonInstallation(string path)
        {
            try
            {
                var report = await Task.Run(() => PythonEnvironmentDiagnostics.RunFullDiagnostics(path));

                if (report.PythonFound)
                {
                    var config = PythonRunTimeDiagnostics.GetPythonConfig(path);
                    if (config != null)
                    {
                        PythonRuntime.PythonInstallations.Add(config);
                        PythonRuntime.SaveConfig();
                        ReportProgress($"Added custom Python installation: {path}");
                        return true;
                    }
                }

                ReportError($"Failed to add custom Python installation: {path}");
                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Error adding custom Python installation: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Logging and Error Handling
        protected void ReportProgress(string message)
        {
            Progress?.Report(new PassedArgs
            {
                Messege = message,
                EventType = "Info"
            });

            Editor?.AddLogMessage("Python ML", message, DateTime.Now, -1, null, Errors.Ok);
        }

        protected void ReportError(string message)
        {
            Progress?.Report(new PassedArgs
            {
                Messege = message,
                EventType = "Error",
                Flag = Errors.Failed
            });

            Editor?.AddLogMessage("Python ML", message, DateTime.Now, -1, null, Errors.Failed);
        }
        #endregion

        #region IDisposable Implementation
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                try
                {
                    TokenSource?.Cancel();
                    TokenSource?.Dispose();
                    
                    // Clean up session resources if needed
                    if (ConfiguredSession != null && SessionManager != null)
                    {
                        SessionManager.CleanupSession(ConfiguredSession);
                    }
                }
                catch (Exception ex)
                {
                    // Log disposal errors but don't throw
                    Console.WriteLine($"Warning during disposal: {ex.Message}");
                }
                finally
                {
                    _isDisposed = true;
                    disposedValue = true;
                }
            }
        }

        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
