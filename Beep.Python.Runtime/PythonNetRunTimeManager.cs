using System;
using System.Collections.Generic;
using Python.Runtime;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;
using Beep.Python.Model;
using TheTechIdea.Beep.ConfigUtil;
using System.Linq;
using System.Threading.Channels;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;
using System.Diagnostics;
using TheTechIdea.Beep.Container.Services;
using Beep.Python.RuntimeEngine.Helpers;
using Newtonsoft.Json;
using Beep.Python.RuntimeEngine.ViewModels;
using System.Text;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// Manages a Python .NET runtime environment, including initialization, configuration,
    /// and script execution.
    /// </summary>
    public class PythonNetRunTimeManager : IDisposable, IPythonRunTimeManager
    {
        #region "Fields"
        private bool _IsInitialized = false;
        private bool disposedValue;
        private readonly IBeepService _beepService;
        /// <summary>
        /// Gets the current <see cref="IProgress{PassedArgs}"/> object for reporting progress.
        /// </summary>
        public IProgress<PassedArgs> Progress { get; private set; }

        /// <summary>
        /// Gets or sets the token used for cancellation operations.
        /// </summary>
        public CancellationToken Token { get; set; }

        private string pythonpath;
        private string configfile;
        private volatile bool _shouldStop = false;
        #endregion "Fields"

        #region "Properties"
        #region "Session and Environment"
        public IPythonSessionManager SessionManager { get; set; }
        public IPythonVirtualEnvManager VirtualEnvmanager { get; set; }
         public Dictionary<string, PyModule> SessionScopes { get; } = new();
        #endregion "Session and Environment"

        #region "Status and Configuration"
        /// <summary>
        /// Indicates whether Python is fully initialized and ready to execute code.
        /// </summary>
        public bool IsInitialized => _IsInitialized;

        /// <summary>
        /// Gets or sets an observable collection of output lines captured from Python execution.
        /// </summary>
        public ObservableCollection<string> OutputLines { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// Indicates whether the runtime manager is currently performing an operation.
        /// </summary>
        public bool IsBusy { get; set; } = false;

        /// <summary>
        /// The Python configuration object, containing a list of runtimes and the currently selected one.
        /// </summary>
        public ObservableBindingList<PythonRunTime> PythonConfigs { get; set; } = new();

        /// <summary>
        /// Indicates whether a Python script is currently running.
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// Gets or sets the file path of the currently loaded Python script.
        /// </summary>
        public string CurrentFileLoaded { get; set; }

        /// <summary>
        /// Indicates whether the Python path has changed.
        /// </summary>
        public bool IsPathChanged { get; set; } = false;

        /// <summary>
        /// Stores a new path if the Python path has changed.
        /// </summary>
        public string NewPath { get; set; } = null;

        /// <summary>
        /// Gets or sets the global <see cref="IDMEEditor"/> used for logging, messages, etc.
        /// </summary>
        public IDMEEditor DMEditor { get; set; }

        /// <summary>
        /// Gets or sets the JSON loader used for reading/writing configuration files.
        /// </summary>
        public IJsonLoader JsonLoader { get; set; }
        #endregion "Status and Configuration"
        #endregion "Properties"

        #region "Constructors"
        /// <summary>
        /// Initializes a new instance of <see cref="PythonNetRunTimeManager"/> with a specified <see cref="IBeepService"/>.
        /// </summary>
        /// <param name="beepService">Service used for logging, configuration, and editor access.</param>
        public PythonNetRunTimeManager(IBeepService beepService)
        {
            _beepService = beepService;
            DMEditor = beepService.DMEEditor;
            JsonLoader = DMEditor.ConfigEditor.JsonLoader;

            // Create a VirtualEnvManager instance instead of implementing the functionality directly
            VirtualEnvmanager = new PythonVirtualEnvManager(beepService, this);
        }

        /// <summary>
        /// Provides a GIL (Global Interpreter Lock) context. Throws an exception if Python is not initialized.
        /// </summary>
        /// <returns>A <see cref="Py.GILState"/> object that manages Python GIL acquisition and release.</returns>
        public Py.GILState GIL()
        {
            return Py.GIL();
        }
        #endregion "Constructors"

        #region "Scope Management"
        /// <summary>
        /// Creates a new scope for the specified session.
        /// </summary>
        public bool CreateScope(PythonSessionInfo session)
        {
            if (session.VirtualEnvironmentId != null)
            {
                var venv =VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(e => e.ID == session.VirtualEnvironmentId);
                if (venv == null)
                {
                    ReportProgress("No virtual environment found for the given session information.", Errors.Failed);
                    return false;
                }
                return CreateScope(session, venv);
            }
            return false;
        }

        /// <summary>
        /// Gets the Python scope associated with a session.
        /// </summary>
        public PyModule GetScope(PythonSessionInfo session)
        {
            if (session == null || !SessionScopes.ContainsKey(session.SessionId))
                return null;

            return SessionScopes[session.SessionId];
        }

        /// <summary>
        /// Checks if a session has an associated Python scope.
        /// </summary>
        public bool HasScope(PythonSessionInfo session)
        {
            return session != null && SessionScopes.ContainsKey(session.SessionId);
        }

        /// <summary>
        /// Creates a new scope for a session within a specific virtual environment.
        /// </summary>
        public bool CreateScope(PythonSessionInfo session, PythonVirtualEnvironment venv)
        {
            if (session == null || venv == null)
                return false;

            if (SessionScopes.ContainsKey(session.SessionId))
                return false;

            using (Py.GIL()) // Always acquire GIL when creating or using scopes
            {
                var scope = Py.CreateScope();
                SessionScopes[session.SessionId] = scope;

                // Inject session and environment metadata into the Python scope
                string setContext = $@"
import os
import sys

os.environ['VIRTUAL_ENV'] = r'{venv.Path}'
sys.prefix = r'{venv.Path}'
sys.exec_prefix = r'{venv.Path}'

username = '{session.Username ?? "unknown"}'
session_id = '{session.SessionId}'
venv_name = '{venv.Name}'
";
                scope.Exec(setContext);
            }

            return true;
        }

        /// <summary>
        /// Clears a session's scope.
        /// </summary>
        public void ClearScope(string sessionId)
        {
            if (SessionScopes.TryGetValue(sessionId, out var scope))
            {
                scope.Dispose();
                SessionScopes.Remove(sessionId);
            }
        }

        /// <summary>
        /// Clears all scopes.
        /// </summary>
        public void ClearAll()
        {
            foreach (var scope in SessionScopes.Values)
            {
                scope.Dispose();
            }
            SessionScopes.Clear();
        }
        #endregion "Scope Management"

        #region "Initialization and Shutdown"
        /// <summary>
        /// Initializes the Python environment using the provided runtime config and virtual environment.
        /// </summary>
        public bool Initialize(PythonRunTime cfg, PythonVirtualEnvironment venv)
        {
            if (venv == null || string.IsNullOrWhiteSpace(venv.Path))
            {
                ReportProgress("Invalid virtual environment provided.", Errors.Failed);
                return false;
            }

            return Initialize(cfg, venv.Path);
        }

        /// <summary>
        /// Initializes Python with the specified home directory and library path.
        /// </summary>
        public bool Initialize(string pythonHome, string libPath)
        {
            pythonpath = pythonHome;

            // Find a Python runtime that matches this path or create a new one
            PythonRunTime cfg = PythonConfigs.FirstOrDefault(p =>
                p.BinPath.Equals(pythonHome, StringComparison.OrdinalIgnoreCase));

            if (cfg == null)
            {
                cfg = PythonRunTimeDiagnostics.GetPythonConfig(pythonHome);
                if (cfg != null)
                {
                    PythonConfigs.Add(cfg);
                }
                else
                {
                    ReportProgress("Could not create Python configuration for the specified path.", Errors.Failed);
                    return false;
                }
            }

            return Initialize(cfg, pythonHome);
        }

        /// <summary>
        /// Initializes the Python environment using the specified virtual environment path or the current runtime config if none is provided.
        /// </summary>
        public bool Initialize(PythonRunTime config, string virtualEnvPath = null)
        {
            if (IsBusy) return false;
            IsBusy = true;

            // Use the provided virtual environment path or fall back to the config's BinPath
            string pythonBinPath = virtualEnvPath ?? config.BinPath;
            if (pythonBinPath == null)
            {
                ReportProgress("No Python runtime path found in configuration.", Errors.Failed);
                IsBusy = false;
                return false;
            }

            string pythonScriptPath = Path.Combine(pythonBinPath, "Scripts"); // Common for Windows venv
            string pythonPackagePath = Path.Combine(pythonBinPath, "Lib\\site-Packages");

            if (config != null && config.IsPythonInstalled)
            {
                if (!PythonEngine.IsInitialized)
                {
                    PythonRunTimeDiagnostics.SetAiFolderPath(DMEditor);

                    Environment.SetEnvironmentVariable("PATH", $"{pythonBinPath};{pythonScriptPath};" + Environment.GetEnvironmentVariable("PATH"), EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONNET_PYTHON_RUNTIME", $"{pythonBinPath}", EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONHOME", pythonBinPath, EnvironmentVariableTarget.Process);
                    Environment.SetEnvironmentVariable("PYTHONPATH", $"{pythonPackagePath};", EnvironmentVariableTarget.Process);

                    try
                    {
                        ReportProgress("Init. of Python engine");

                        Runtime.PythonDLL = Path.Combine(pythonBinPath, Path.GetFileName(config.PythonDll));
                        PythonEngine.PythonHome = pythonBinPath;
                        PythonEngine.Initialize();

                        ReportProgress("Finished Init. of Python engine");

                        IsBusy = false;
                        _IsInitialized = true;

                        ReportProgress("Python Initialized Successfully", Errors.Ok);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        IsBusy = false;
                        ReportProgress(ex.Message, Errors.Failed);
                        return false;
                    }
                }
                else
                {
                    IsBusy = false;
                    ReportProgress("Python Already Initialized", Errors.Ok);
                    return true;
                }
            }
            else
            {
                ReportProgress("No Python Available", Errors.Failed);
                IsBusy = false;
                return false;
            }
        }

        /// <summary>
        /// Shuts down the Python engine, disposing of any persistent scope and clearing active environment state.
        /// </summary>
        public IErrorsInfo ShutDown()
        {
            var er = new ErrorsInfo { Flag = Errors.Ok };
            if (IsBusy) return er;
            IsBusy = true;

            try
            {
                foreach (var scope in SessionScopes.Values)
                {
                    scope.Dispose();
                }
                SessionScopes.Clear();

                if (PythonEngine.IsInitialized)
                {
                    PythonEngine.Shutdown();
                }

                _IsInitialized = false;

                ReportProgress("Python engine shut down.", Errors.Ok);
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;
                ReportProgress($"Shutdown error: {ex.Message}", Errors.Failed);
            }
            finally
            {
                IsBusy = false;
            }

            return er;
        }

        /// <summary>
        /// Shuts down the current Python engine and reinitializes it with a new virtual environment.
        /// </summary>
        public bool RestartWithEnvironment(PythonVirtualEnvironment venv)
        {
            // Delegate to the VirtualEnvManager for environment-specific operations
            var result = ShutDown();
            if (result.Flag != Errors.Ok)
            {
                ReportProgress("Failed to shut down current Python instance.", Errors.Failed);
                return false;
            }

            // Find the configuration for this environment
            var config = PythonConfigs?.FirstOrDefault(c => c.ID == venv.PythonConfigID);
            if (config == null)
            {
                ReportProgress($"Configuration not found for environment {venv.Name}", Errors.Failed);
                return false;
            }

            return Initialize(config, venv);
        }
        #endregion

        #region "Configuration Methods"
        /// <summary>
        /// Determines if a Python runtime is available based on runtime config.
        /// </summary>
        private bool GetIsPythonAvailable(PythonRunTime config)
        {
            if (config != null)
            {
                if (!string.IsNullOrEmpty(config?.BinPath))
                {
                    return PythonRunTimeDiagnostics.IsPythonInstalled(config.BinPath);
                }
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adds specified paths to the current process' PATH environment variable.
        /// </summary>
        public static void AddEnvPath(params string[] paths)
        {
            var envPaths = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator).ToList();
            foreach (var path in paths)
            {
                if (path.Length > 0 && !envPaths.Contains(path))
                {
                    envPaths.Insert(0, path);
                }
            }
            Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator.ToString(), envPaths), EnvironmentVariableTarget.Process);
        }

        /// <summary>
        /// Creates or loads the Python config file from disk. If none exists, it creates a default.
        /// </summary>
        public void CreateLoadConfig()
        {
            // Implement configuration loading logic
            // This will depend on your specific configuration model
        }

        /// <summary>
        /// Saves the current Python configuration to disk.
        /// </summary>
        public void SaveConfig()
        {
            // Implement configuration saving logic
            // This will depend on your specific configuration model
        }
        #endregion

        #region "Python Run Code"
        /// <summary>
        /// Runs Python code for a specific user in their environment.
        /// </summary>
        public async Task<string> RunPythonForUserAsync(PythonSessionInfo session, string username, string code, IProgress<PassedArgs> progress = null)
        {
            if (session == null)
            {
                ReportProgress("Session object is required.", Errors.Failed);
                return null;
            }

            // Try to find an existing venv by session.VirtualEnvironmentId
            var venv = VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(v =>
                v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

            // Fallback: Try to find by username if not linked by ID
            if (venv == null)
            {
                venv = VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(v =>
                    v.Name.Equals(username, StringComparison.OrdinalIgnoreCase));
            }

            if (venv == null)
            {
                ReportProgress($"No virtual environment found for user '{username}' or session.", Errors.Failed);
                session.Notes = "Virtual environment not found.";
                session.WasSuccessful = false;
                session.EndedAt = DateTime.Now;
                return null;
            }

            // Track session info
            session.StartedAt = DateTime.Now;
            session.Username = username;
            session.VirtualEnvironmentId = venv.ID;

            // Check if the session already exists in the virtual environment's Sessions collection
            if (!venv.Sessions.Any(s => s.SessionId == session.SessionId))
            {
                venv.AddSession(session);
            }

            // Check if the session already exists in the global Sessions collection
            if (!SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
            {
               SessionManager.Sessions.Add(session);
            }

            // Initialize engine with the appropriate configuration
            var config = PythonConfigs?.FirstOrDefault(c => c.ID == venv.PythonConfigID);
            if (config == null)
            {
                session.Notes = "Python runtime configuration not found.";
                session.WasSuccessful = false;
                session.EndedAt = DateTime.Now;
                return null;
            }

            if (!Initialize(config, venv))
            {
                session.Notes = "Environment initialization failed.";
                session.WasSuccessful = false;
                session.EndedAt = DateTime.Now;
                return null;
            }

            try
            {
                string output = await RunPythonCodeAndGetOutput(progress ?? DMEditor.progress, code, session);
                session.Notes = "Execution succeeded.";
                session.WasSuccessful = true;
                session.EndedAt = DateTime.Now;
                return output;
            }
            catch (Exception ex)
            {
                session.Notes = $"Execution failed: {ex.Message}";
                session.WasSuccessful = false;
                session.EndedAt = DateTime.Now;
                ReportProgress(session.Notes, Errors.Failed);
                return null;
            }
        }

        /// <summary>
        /// Runs a command line in Python (e.g., pip install) by spawning a subprocess and capturing its output.
        /// </summary>
        /// <summary>
        /// Runs a command line in Python (e.g., pip install) by spawning a subprocess and capturing its output.
        /// Uses proper concurrency controls for multi-user environments.
        /// </summary>
        public async Task<string> RunPythonCommandLineAsync(
            IProgress<PassedArgs> progress,
            string commandString,
            bool useConda = false,
            PythonSessionInfo session = null,
            PythonVirtualEnvironment environment = null)
        {
            if (progress == null)
                progress = DMEditor?.progress ?? new Progress<PassedArgs>();

            // Validate parameters
            if (string.IsNullOrEmpty(commandString))
            {
                progress.Report(new PassedArgs { Messege = "No command specified.", Flag = Errors.Failed });
                return null;
            }

            // Use provided environment or find from session
            if (environment == null && session != null)
            {
                environment = VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(v =>
                    v.ID.Equals(session?.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));
            }

            if (environment == null)
            {
                progress.Report(new PassedArgs { Messege = "No virtual environment specified or available for command execution.", Flag = Errors.Failed });
                return null;
            }

            // Create a cancellation token source that we can use to control execution timeouts
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5-minute timeout

            // If a session was provided, we'll use it to manage concurrency
            if (session != null)
            {
                // Use the concurrency control system to execute this operation
                return await ExecuteWithConcurrencyControlAsync<string>(session, async () =>
                {
                    // Track the session's activity
                    SessionManager?.UpdateSessionActivity(session.SessionId);

                    // Return the result of our command execution (defined below)
                    return await ExecuteCommandLineInternalAsync(
                        progress, commandString, useConda, session, environment, timeoutCts.Token);
                });
            }
            else
            {
                // No session provided, create a temporary one for this operation
                var tempSession = new PythonSessionInfo
                {
                    SessionName = $"TempCommandLine_{DateTime.Now.Ticks}",
                    VirtualEnvironmentId = environment.ID,
                    StartedAt = DateTime.Now
                };

                // Register temporary session
                SessionManager?.RegisterSession(tempSession);

                try
                {
                    // Use the concurrency control system with this temporary session
                    return await ExecuteWithConcurrencyControlAsync<string>(tempSession, async () =>
                    {
                        return await ExecuteCommandLineInternalAsync(
                            progress, commandString, useConda, tempSession, environment, timeoutCts.Token);
                    });
                }
                finally
                {
                    // Always clean up our temporary session
                    SessionManager?.UnregisterSession(tempSession.SessionId);
                }
            }
        }

        /// <summary>
        /// Internal implementation of command line execution with proper GIL management.
        /// </summary>
        private async Task<string> ExecuteCommandLineInternalAsync(
            IProgress<PassedArgs> progress,
            string commandString,
            bool useConda,
            PythonSessionInfo session,
            PythonVirtualEnvironment environment,
            CancellationToken cancellationToken)
        {
            // Ensure the session is properly tracked
            if (session != null)
            {
                // Ensure session is registered with SessionManager
                if (!SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    SessionManager.Sessions.Add(session);
                }

                // Ensure session is registered with environment
                if (!environment.Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    environment.AddSession(session);
                }
            }

            // Get the proper environment paths
            string customPath = environment.Path;
            string scriptPath = Path.Combine(environment.Path,
                Environment.OSVersion.Platform == PlatformID.Win32NT ? "Scripts" : "bin");

            string modifiedFilePath = $"{customPath};{scriptPath}".Replace("\\", "\\\\");

            // Python code for running package commands
            string wrappedPythonCode = $@"
import os
import subprocess
import threading
import queue
import sys

def set_custom_path(custom_path):
    # Modify the PATH environment variable
    os.environ[""PATH""] = '{modifiedFilePath}' + os.pathsep + os.environ[""PATH""]

def run_pip_and_capture_output(args, output_callback):
    def enqueue_output(stream, queue):
        for line in iter(stream.readline, b''):
            queue.put(line.decode('utf-8').strip())
        stream.close()

    # Use shell=True on Windows, shell=False on Unix
    use_shell = {(Environment.OSVersion.Platform == PlatformID.Win32NT).ToString().ToLower()}
    
    # Create the process with appropriate environment
    process = subprocess.Popen(
        args, 
        stdout=subprocess.PIPE, 
        stderr=subprocess.PIPE,
        shell=use_shell,
        env=os.environ.copy()
    )

    stdout_queue = queue.Queue()
    stderr_queue = queue.Queue()

    stdout_thread = threading.Thread(target=enqueue_output, args=(process.stdout, stdout_queue))
    stderr_thread = threading.Thread(target=enqueue_output, args=(process.stderr, stderr_queue))
    stdout_thread.daemon = True  # Daemon threads won't block program exit
    stderr_thread.daemon = True

    stdout_thread.start()
    stderr_thread.start()

    while process.poll() is None or not stdout_queue.empty() or not stderr_queue.empty():
        while not stdout_queue.empty():
            try:
                line = stdout_queue.get_nowait()
                output_callback(line)
            except queue.Empty:
                pass

        while not stderr_queue.empty():
            try:
                line = stderr_queue.get_nowait()
                output_callback(line)
            except queue.Empty:
                pass

    # Make sure we join threads properly
    stdout_thread.join(timeout=1.0)
    stderr_thread.join(timeout=1.0)
    
    # Get final output/errors
    stdout, stderr = process.communicate()
    
    # Return exit code for error handling
    return process.returncode

def run_with_timeout(func, args, output_callback, timeout):
    try:
        exit_code = func(args, output_callback)
        return exit_code
    except Exception as e:
        output_callback(f""Error executing command: {{str(e)}}"")
        return -1
";

            string output = "";
            string command = "";
            PyModule scope = null;


                try
                {
                    // Determine the scope to use
                    scope = GetScope(session);
                    if (scope == null && environment != null)
                    {
                        CreateScope(session, environment);
                        scope = GetScope(session);
                    }

                    // Fall back to a new temporary scope if needed
                    if (scope == null)
                    {
                        scope = Py.CreateScope();
                    }

                    PyObject globalsDict = scope.GetAttr("__dict__");

                    // Execute our Python helper code
                    scope.Exec(wrappedPythonCode);

                    // Set up the PATH environment
                    PyObject setCustomPathFunc = scope.GetAttr("set_custom_path");
                    setCustomPathFunc.Invoke(modifiedFilePath.ToPython());

                    PyObject captureOutputFunc = scope.GetAttr("run_pip_and_capture_output");

                    // Prepare the command arguments
                    if (useConda || environment.PythonBinary == PythonBinary.Conda)
                    {
                        command = $"conda {commandString}";
                    }
                    else
                    {
                        // For pip commands, ensure we use the right executable for the platform
                        if (commandString.StartsWith("pip ", StringComparison.OrdinalIgnoreCase))
                        {
                            var pythonExe = Environment.OSVersion.Platform == PlatformID.Win32NT ?
                                "python.exe" : "python";
                            command = $"{pythonExe} -m pip {commandString.Substring(4)}";
                        }
                        else
                        {
                            command = commandString;
                        }
                    }

                    progress.Report(new PassedArgs() { Messege = $"Running: {command}" });

                    // Build arguments list for Python
                    PyObject pyArgs = new PyList();
                    pyArgs.InvokeMethod("extend", command.Split(' ').ToPython());

                    // Set up output channel for async communication
                    var outputChannel = Channel.CreateUnbounded<string>();
                    PyObject outputCallback = PyObject.FromManagedObject((Action<string>)(line =>
                    {
                        outputChannel.Writer.TryWrite(line);
                    }));
                    globalsDict.SetItem("output_callback", outputCallback);

                    // Run the command with a timeout
                    var timeoutInSeconds = 300; // 5-minute timeout
                    PyObject runWithTimeoutFunc = scope.GetAttr("run_with_timeout");

                    // Execute command in a task to avoid blocking the GIL
                    var pythonTask = Task.Run(() =>
                    {
                        using (Py.GIL())
                        {
                            return runWithTimeoutFunc.Invoke(
                                captureOutputFunc,
                                pyArgs,
                                outputCallback,
                                timeoutInSeconds.ToPython());
                        }
                    });

                    var outputList = new List<string>();

                    // Process output asynchronously
                    async Task ReadFromChannelAsync()
                    {
                        try
                        {
                            while (await outputChannel.Reader.WaitToReadAsync(cancellationToken))
                            {
                                if (outputChannel.Reader.TryRead(out var line))
                                {
                                    outputList.Add(line);
                                    progress.Report(new PassedArgs() { Messege = line });
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            progress.Report(new PassedArgs()
                            {
                                Messege = "Command execution was cancelled.",
                                Flag = Errors.Warning
                            });
                        }
                    }

                    // Run both tasks concurrently
                    var readOutputTask = ReadFromChannelAsync();

                    // Wait for Python task to complete or timeout
                    var completedTask = await Task.WhenAny(pythonTask, Task.Delay(timeoutInSeconds * 1000, cancellationToken));

                    if (completedTask != pythonTask)
                    {
                        // Command timed out
                        progress.Report(new PassedArgs()
                        {
                            Messege = $"Command execution timed out after {timeoutInSeconds} seconds.",
                            Flag = Errors.Warning
                        });
                        _shouldStop = true; // Signal any running Python code to stop
                    }
                    else
                    {
                        // Get exit code
                        var exitCode = pythonTask.Result.As<int>();
                        if (exitCode != 0)
                        {
                            progress.Report(new PassedArgs()
                            {
                                Messege = $"Command exited with code {exitCode}",
                                Flag = Errors.Warning
                            });
                        }
                    }

                    // Signal the output channel that we're done
                    outputChannel.Writer.Complete();

                    // Wait for output processing to complete
                    await readOutputTask;

                    // Combine all output
                    output = string.Join("\n", outputList);
                }
                catch (Exception ex)
                {
                    progress.Report(new PassedArgs()
                    {
                        Messege = $"Error executing command: {ex.Message}",
                        Flag = Errors.Failed
                    });
                    return $"Error: {ex.Message}";
                }
           

            // Report completion
            if (output.Length > 0)
            {
                progress.Report(new PassedArgs() { Messege = $"Finished: {command}" });
            }
            else
            {
                progress.Report(new PassedArgs()
                {
                    Messege = $"Command completed with no output: {command}",
                    Flag = Errors.Warning
                });
            }

            // Update session status if provided
            if (session != null)
            {
                session.Notes = $"Executed command: {commandString}";

                // Update activity timestamp
                if (session.Metadata == null)
                    session.Metadata = new Dictionary<string, object>();

                session.Metadata["LastActivity"] = DateTime.Now;
            }

            return output;
        }


        /// <summary>
        /// Runs a Python script within a session's scope with optional parameters.
        /// </summary>
        public virtual bool RunPythonScript(string script, dynamic parameters, PythonSessionInfo session)
        {
            if (!IsInitialized)
            {
                return false;
            }

            try
            {
                if (!SessionScopes.ContainsKey(session.SessionId))
                {
                    ReportProgress("Session scope not found.", Errors.Failed);
                    return false;
                }

                SessionScopes[session.SessionId].Set(nameof(parameters), parameters);
                SessionScopes[session.SessionId].Exec(script);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing Python script: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Runs a Python script with parameters and returns the result asynchronously.
        /// </summary>
        public async Task<dynamic> RunPythonScriptWithResult(string script, dynamic parameters, PythonSessionInfo session)
        {
            dynamic result = null;
            if (!IsInitialized)
            {
                return result;
            }
            result = await RunPythonCodeAndGetOutput(Progress, script, session);
            return result;
        }

        /// <summary>
        /// Runs a Python file asynchronously, given a file path, and reports progress.
        /// </summary>
        public async Task<IErrorsInfo> RunFile(PythonSessionInfo session, string file, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) return DMEditor.ErrorObject;
            IsBusy = true;
    
                try
            {
                if (session != null)
                {
                    // Find the environment associated with this session
                    var venv = VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(v =>
                        v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                    if (venv != null)
                    {
                        // Check if the session should be tracked in collections
                        if (!venv.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            venv.AddSession(session);
                        }

                        if (!SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            SessionManager.Sessions.Add(session);
                        }

                        // Find the configuration for this environment
                        var config = PythonConfigs?.FirstOrDefault(c => c.ID == venv.PythonConfigID);
                        if (config == null)
                        {
                            ReportProgress($"Configuration not found for environment {venv.Name}", Errors.Failed);
                            IsBusy = false;
                            return DMEditor.ErrorObject;
                        }

                        // Ensure environment is initialized
                        if (!Initialize(config, venv))
                        {
                            ReportProgress("Failed to initialize environment for file execution.", Errors.Failed);
                            IsBusy = false;
                            return DMEditor.ErrorObject;
                        }

                        string code = $"{venv.BaseInterpreterPath} {file}";
                        await RunPythonCodeAndGetOutput(progress, code, session);
                    }
                }

                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }

            IsBusy = false;
            return DMEditor.ErrorObject;
        }

        /// <summary>
        /// Runs arbitrary Python code asynchronously, reporting progress and allowing cancellation.
        /// </summary>
        public async Task<IErrorsInfo> RunCode(PythonSessionInfo session, string code, IProgress<PassedArgs> progress, CancellationToken token)
        {
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) return DMEditor.ErrorObject;
            IsBusy = true;
            return await ExecuteWithConcurrencyControlAsync<IErrorsInfo>(session, async () =>
            {
                try
                {
                    if (session != null)
                    {
                        // Find the environment associated with this session
                        var venv = VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(v =>
                            v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                        if (venv != null)
                        {
                            // Check if the session should be tracked in collections
                            if (!venv.Sessions.Any(s => s.SessionId == session.SessionId))
                            {
                                venv.AddSession(session);
                            }

                            if (!SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                            {
                                SessionManager.Sessions.Add(session);
                            }

                            // Find the configuration for this environment
                            var config = PythonConfigs?.FirstOrDefault(c => c.ID == venv.PythonConfigID);
                            if (config == null)
                            {
                                ReportProgress($"Configuration not found for environment {venv.Name}", Errors.Failed);
                                IsBusy = false;
                                return DMEditor.ErrorObject;
                            }

                            // Ensure environment is initialized
                            if (!Initialize(config, venv))
                            {
                                ReportProgress("Failed to initialize environment for code execution.", Errors.Failed);
                                IsBusy = false;
                                return DMEditor.ErrorObject;
                            }
                        }
                    }

                    // Execute code with session-specific scope if available
                    await RunPythonCodeAndGetOutput(progress, code, session);
                    IsBusy = false;
                    return DMEditor.ErrorObject;
                }
                catch (Exception ex)
                {
                    IsBusy = false;
                    DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
                    return DMEditor.ErrorObject;
                }
            });
            



        }

        /// <summary>
        /// Runs a Python command asynchronously, with progress reporting and cancellation.
        /// </summary>
        public async Task<dynamic> RunCommand(PythonSessionInfo session, string command, IProgress<PassedArgs> progress, CancellationToken token)
        {
            PyObject pyObject = null;
            DMEditor.ErrorObject.Flag = Errors.Ok;
            if (IsBusy) { return false; }
            IsBusy = true;

            try
            {
                if (session != null)
                {
                    // Find the environment associated with this session
                    var venv = VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(v =>
                        v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                    if (venv != null)
                    {
                        // Check if the session should be tracked in collections
                        if (!venv.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            venv.AddSession(session);
                        }

                        if (!SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            SessionManager.Sessions.Add(session);
                        }

                        // Find the configuration for this environment
                        var config = PythonConfigs?.FirstOrDefault(c => c.ID == venv.PythonConfigID);
                        if (config == null)
                        {
                            ReportProgress($"Configuration not found for environment {venv.Name}", Errors.Failed);
                            IsBusy = false;
                            return null;
                        }

                        // Ensure environment is initialized
                        if (!Initialize(config, venv))
                        {
                            ReportProgress("Failed to initialize environment for command execution.", Errors.Failed);
                            IsBusy = false;
                            return null;
                        }
                    }
                }

                // Execute command with session-specific scope if available
                await RunPythonCodeAndGetOutput(progress, command, session);
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }

            IsBusy = false;
            return pyObject;
        }
        public dynamic RunPythonScriptWithResult(PythonSessionInfo session, string script, Dictionary<string, object> variables)
        {
           

            using (Py.GIL())
            {
                try
                {

                    SessionScopes[session.SessionId].Exec(script);

                    // Return the result variable if it exists
                    if (SessionScopes[session.SessionId].HasAttr("result"))
                    {
                        return SessionScopes[session.SessionId].GetAttr("result").AsManagedObject(typeof(object));
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    DMEditor?.AddLogMessage("Python Script", ex.Message, DateTime.Now, -1, null, Errors.Failed);
                    return null;
                }
            }
        }

        /// <summary>
        /// Runs Python code within a wrapper that captures stdout line-by-line and reports it.
        /// </summary>
        /// <summary>
        /// Runs Python code within a wrapper that captures stdout line-by-line and reports it.
        /// Uses concurrency controls to ensure thread safety in multi-user environments.
        /// </summary>
        /// <param name="progress">Progress reporter for output and status updates</param>
        /// <param name="code">Python code to execute</param>
        /// <param name="session">Optional session context (if null, a temporary session is used)</param>
        /// <returns>Captured output from the execution</returns>
        public async Task<string> RunPythonCodeAndGetOutput(IProgress<PassedArgs> progress, string code, PythonSessionInfo session = null)
        {
            if (progress == null)
                progress = DMEditor?.progress ?? new Progress<PassedArgs>();

            if (string.IsNullOrEmpty(code))
            {
                progress.Report(new PassedArgs { Messege = "No code provided to execute", Flag = Errors.Warning });
                return string.Empty;
            }

            // If no session was provided, create a temporary one
            bool isTemporarySession = false;
            PythonSessionInfo actualSession = session;
            PythonVirtualEnvironment environment = null;

            if (actualSession == null)
            {
                // Find a suitable environment for a temporary session
                environment = VirtualEnvmanager?.ManagedVirtualEnvironments.FirstOrDefault();
                if (environment == null)
                {
                    progress.Report(new PassedArgs { Messege = "No Python environments available", Flag = Errors.Failed });
                    return "Error: No Python environments available for code execution";
                }

                // Create a temporary session
                actualSession = new PythonSessionInfo
                {
                    SessionName = $"TempCode_{DateTime.Now.Ticks}",
                    VirtualEnvironmentId = environment.ID,
                    StartedAt = DateTime.Now,
                    Status = PythonSessionStatus.Active
                };

                // Register temporary session
                SessionManager?.RegisterSession(actualSession);
                isTemporarySession = true;
            }
            else if (string.IsNullOrEmpty(actualSession.VirtualEnvironmentId))
            {
                progress.Report(new PassedArgs { Messege = "Session has no associated environment", Flag = Errors.Failed });
                return "Error: Session has no associated environment";
            }
            else
            {
                // Find the environment for this session
                environment = VirtualEnvmanager?.ManagedVirtualEnvironments
                    .FirstOrDefault(e => e.ID == actualSession.VirtualEnvironmentId);

                if (environment == null)
                {
                    progress.Report(new PassedArgs
                    {
                        Messege = $"Environment '{actualSession.VirtualEnvironmentId}' not found",
                        Flag = Errors.Failed
                    });
                    return $"Error: Environment '{actualSession.VirtualEnvironmentId}' not found";
                }
            }

            try
            {
                // Use concurrency control to execute the code
                return await ExecuteWithConcurrencyControlAsync<string>(actualSession, async () =>
                {
                    // Track activity
                    SessionManager?.UpdateSessionActivity(actualSession.SessionId);

                    // The actual execution code
                    return await ExecutePythonCodeInternalAsync(progress, code, actualSession, environment);
                });
            }
            finally
            {
                // Clean up temporary session if we created one
                if (isTemporarySession)
                {
                    SessionManager?.UnregisterSession(actualSession.SessionId);
                }
            }
        }

        /// <summary>
        /// Internal implementation of Python code execution with controlled GIL usage.
        /// </summary>
        private async Task<string> ExecutePythonCodeInternalAsync(
            IProgress<PassedArgs> progress,
            string code,
            PythonSessionInfo session,
            PythonVirtualEnvironment environment)
        {
            string wrappedPythonCode = @"
import sys
import io
import threading
import traceback

class CustomStringIO(io.StringIO):
    def __init__(self, output_handler, should_stop):
        super().__init__()
        self.output_handler = output_handler
        self.should_stop = should_stop
        self._lock = threading.Lock()

    def write(self, s):
        with self._lock:
            super().write(s)
            output = self.getvalue()
            if output.strip():
                self.output_handler(output.strip())
                self.truncate(0)  # Clear the internal buffer
                self.seek(0)      # Reset the buffer pointer

def capture_output(code, globals_dict, output_handler, should_stop):
    original_stdout = sys.stdout
    original_stderr = sys.stderr
    custom_out = CustomStringIO(output_handler, should_stop)
    sys.stdout = custom_out
    sys.stderr = custom_out
    
    result = None
    error = None
    
    try:
        # Execute the code
        exec(code, dict(globals_dict))
        
        # Check if we should stop execution
        if should_stop():
            raise KeyboardInterrupt('Execution stopped by request')
            
    except Exception as e:
        error = traceback.format_exc()
        output_handler(f'Error: {str(e)}')
        output_handler(error)
    finally:
        # Always restore stdout/stderr
        sys.stdout = original_stdout
        sys.stderr = original_stderr
        
    return error
";
            string output = "";
            PyModule scope = null;

            // Make sure we have a scope for this session
            scope = GetScope(session);
            if (scope == null)
            {
                // Create a new scope if needed
                if (!CreateScope(session, environment))
                {
                    progress.Report(new PassedArgs
                    {
                        Messege = "Failed to create Python scope for session",
                        Flag = Errors.Failed
                    });
                    return "Error: Failed to create Python scope for session";
                }

                scope = GetScope(session);
                if (scope == null)
                {
                    progress.Report(new PassedArgs
                    {
                        Messege = "Failed to get Python scope after creation",
                        Flag = Errors.Failed
                    });
                    return "Error: Failed to get Python scope after creation";
                }
            }

            // Use a channel for asynchronous output collection
            var outputChannel = Channel.CreateUnbounded<string>();
            bool executionCompleted = false;

            try
            {
                // Execute in a separate task to avoid blocking the GIL
                var executionTask = Task.Run(() =>
                {
                    using (Py.GIL()) // Acquire the GIL for this thread only
                    {
                        try
                        {
                            // Set up the output handler
                            Action<string> OutputHandler = line =>
                            {
                                outputChannel.Writer.TryWrite(line);
                            };

                            Func<bool> ShouldStop = () => _shouldStop;

                            // Add handlers to scope
                            scope.Set("output_handler", OutputHandler);
                            scope.Set("should_stop", ShouldStop);

                            // Execute the wrapper code
                            scope.Exec(wrappedPythonCode);

                            // Get the capture function
                            PyObject captureOutputFunc = scope.GetAttr("capture_output");
                            Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                            // Execute the user's code in our wrapper
                            using (PyObject pyCode = new PyString(code))
                            using (PyObject pyGlobalsDict = globalsDict.ToPython())
                            using (PyObject pyOutputHandler = scope.Get("output_handler"))
                            using (PyObject pyShouldStop = scope.Get("should_stop"))
                            {
                                // Run the capture_output function
                                var result = captureOutputFunc.Invoke(
                                    pyCode,
                                    pyGlobalsDict,
                                    pyOutputHandler,
                                    pyShouldStop);

                                // Check if we got an error result
                                if (result != null && !result.IsNone())
                                {
                                    string errorMessage = result.ToString();
                                    if (!string.IsNullOrEmpty(errorMessage))
                                    {
                                        OutputHandler($"Execution error: {errorMessage}");
                                    }
                                }
                            }
                        }
                        catch (PythonException ex)
                        {
                            // Report Python exceptions
                            outputChannel.Writer.TryWrite($"Python error: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            // Report general exceptions
                            outputChannel.Writer.TryWrite($"Error: {ex.Message}");
                        }
                    }
                });

                // Collect output asynchronously
                var outputCollector = Task.Run(async () =>
                {
                    var outputBuilder = new StringBuilder();

                    try
                    {
                        while (await outputChannel.Reader.WaitToReadAsync() && !executionCompleted)
                        {
                            if (outputChannel.Reader.TryRead(out var line))
                            {
                                // Report progress
                                progress.Report(new PassedArgs { Messege = line });

                                // Add to our collected output
                                outputBuilder.AppendLine(line);

                                // Also add to the session's output if we have a session manager
                                if (SessionManager != null)
                                {
                                    SessionManager.AppendSessionOutput(session.SessionId, line);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue
                        progress.Report(new PassedArgs
                        {
                            Messege = $"Error collecting output: {ex.Message}",
                            Flag = Errors.Warning
                        });
                    }

                    return outputBuilder.ToString();
                });

                // Wait for execution to complete (with timeout)
                var executionTimeout = TimeSpan.FromMinutes(2); // Configure as needed
                if (await Task.WhenAny(executionTask, Task.Delay(executionTimeout)) != executionTask)
                {
                    // Execution timed out
                    _shouldStop = true; // Signal Python code to stop
                    progress.Report(new PassedArgs
                    {
                        Messege = $"Execution timed out after {executionTimeout.TotalSeconds} seconds",
                        Flag = Errors.Warning
                    });

                    outputChannel.Writer.TryWrite("Execution timed out");
                }

                // Wait for the execution task to actually complete
                await executionTask;

                // Mark execution as complete and close the channel
                executionCompleted = true;
                outputChannel.Writer.Complete();

                // Get final collected output
                output = await outputCollector;
            }
            catch (Exception ex)
            {
                // Handle any unexpected exceptions
                progress.Report(new PassedArgs
                {
                    Messege = $"Unexpected error during execution: {ex.Message}",
                    Flag = Errors.Failed
                });

                output = $"Error: {ex.Message}";
            }
            finally
            {
                // Ensure we report completion
                progress.Report(new PassedArgs
                {
                    Messege = "Execution completed",
                    EventType = "CODEFINISH"
                });
            }

            return output;
        }

        #endregion

        #region "Utility Methods"
        /// <summary>
        /// Reports progress messages using Progress if available; otherwise uses DMEditor.
        /// </summary>
        private void ReportProgress(PassedArgs args)
        {
            if (Progress != null)
            {
                Progress.Report(args);
            }
        }

        /// <summary>
        /// Overload of ReportProgress that takes a string message and an error flag.
        /// </summary>
        private void ReportProgress(string messege, Errors flag = Errors.Ok)
        {
            if (Progress != null)
            {
                PassedArgs args = new PassedArgs();
                args.Messege = messege;
                Progress.Report(args);
            }
            else if (DMEditor != null)
            {
                if (DMEditor.progress != null)
                {
                    Progress = DMEditor.progress;
                    PassedArgs args = new PassedArgs
                    {
                        Messege = messege
                    };
                    Progress.Report(args);
                }
                DMEditor.AddLogMessage("Beep AI Python", messege, DateTime.Now, 0, null, flag);
            }
        }

        /// <summary>
        /// Converts a C# dictionary to a Python dictionary object.
        /// </summary>
        public static PyObject ToPython(IDictionary<string, object> dictionary)
        {
            using (Py.GIL())
            {
                var pyDict = new PyDict();
                foreach (var kvp in dictionary)
                {
                    PyObject key = new PyString(kvp.Key);
                    PyObject value = kvp.Value.ToPython();
                    pyDict.SetItem(key, value);
                    key.Dispose();
                    value.Dispose();
                }
                return pyDict;
            }
        }

        /// <summary>
        /// Converts an arbitrary C# object to a PyObject.
        /// </summary>
        public static PyObject ToPython(object obj)
        {
            using (Py.GIL())
            {
                return PyObject.FromManagedObject(obj);
            }
        }
        #endregion

        /// <summary>
        /// Signals the runtime manager to attempt stopping any ongoing Python code execution.
        /// </summary>
        public void Stop()
        {
            _shouldStop = true;
        }

        /// <summary>
        /// Saves managed environments to a file.
        /// </summary>
        public void SaveEnvironments(string filePath)
        {
            var json = JsonConvert.SerializeObject(VirtualEnvmanager.ManagedVirtualEnvironments, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads managed environments from a file.
        /// </summary>
        public void LoadEnvironments(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var list = JsonConvert.DeserializeObject<ObservableBindingList<PythonVirtualEnvironment>>(json);
                VirtualEnvmanager.ManagedVirtualEnvironments = list ?? new ObservableBindingList<PythonVirtualEnvironment>();
            }
        }

        #region "Session Management"
        /// <summary>
        /// Updates the session management to properly dispose of Python scopes when sessions end or are removed.
        /// </summary>
        public void CleanupSession(PythonSessionInfo session)
        {
            if (session == null)
                return;

            // Clean up the session's scope if it exists
            if (HasScope(session))
            {
                ClearScope(session.SessionId);
            }
        }

        /// <summary>
        /// Overload of ShutDown that allows specifying which session to shut down.
        /// </summary>
        public IErrorsInfo ShutDownSession(PythonSessionInfo session)
        {
            var er = new ErrorsInfo { Flag = Errors.Ok };
            if (session == null)
                return er;

            try
            {
                // Mark session as ended
                session.Status = PythonSessionStatus.Terminated;
                session.EndedAt = DateTime.Now;
                session.Notes = "Session explicitly terminated";

                // Clean up the session's scope
                CleanupSession(session);
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;
                ReportProgress($"Session shutdown error: {ex.Message}", Errors.Failed);
            }

            return er;
        }

        /// <summary>
        /// Performs cleanup of stale sessions and their associated scopes.
        /// </summary>
        public void PerformSessionCleanup(TimeSpan maxAge)
        {
            if (IsBusy)
                return;

            var now = DateTime.Now;
            var sessionsToCleanup = SessionManager.Sessions
                .Where(s => s.Status == PythonSessionStatus.Terminated ||
                          (s.EndedAt.HasValue && (now - s.EndedAt.Value) > maxAge) ||
                          (!s.EndedAt.HasValue && (now - s.StartedAt) > maxAge))
                .ToList();

            foreach (var session in sessionsToCleanup)
            {
                ReportProgress($"Cleaning up stale session: {session.SessionId}", Errors.Ok);
                CleanupSession(session);

                // Remove from environment's sessions collection
                if (!string.IsNullOrEmpty(session.VirtualEnvironmentId))
                {
                    var env = VirtualEnvmanager.ManagedVirtualEnvironments.FirstOrDefault(v =>
                        v.ID.Equals(session.VirtualEnvironmentId, StringComparison.OrdinalIgnoreCase));

                    if (env != null)
                    {
                        var sessionInEnv = env.Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
                        if (sessionInEnv != null)
                        {
                            env.Sessions.Remove(sessionInEnv);
                        }
                    }
                }

                // Remove from global sessions collection
               SessionManager.Sessions.Remove(session);
            }
        }
        public async Task<T> ExecuteWithConcurrencyControlAsync<T>(PythonSessionInfo session, Func<Task<T>> operation, TimeSpan? timeout = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            // Use the session manager's concurrency control if available
            if (SessionManager != null &&
                SessionManager is PythonSessionManager sessionManager)
            {
                bool success = await sessionManager.ExecuteWithConcurrencyControlAsync(
                    session.SessionId,
                    async () => {
                        await Task.Yield(); // Ensure we're actually async
                        await operation();
                    });

                if (!success)
                    throw new InvalidOperationException("Failed to execute operation with concurrency control");
            }

            // If no session manager with concurrency control, execute directly
            // with a simple busy lock
            if (IsBusy)
                throw new InvalidOperationException("Python runtime is busy");

            IsBusy = true;
            try
            {
                return await operation();
            }
            finally
            {
                IsBusy = false;
            }
        }

        public Dictionary<string, object> GetRuntimeMetrics()
        {
            var metrics = new Dictionary<string, object>
            {
                ["IsInitialized"] = IsInitialized,
                ["IsBusy"] = IsBusy,
                ["ScopeCount"] = SessionScopes.Count,
                ["EnvCount"] = VirtualEnvmanager?.ManagedVirtualEnvironments.Count ?? 0
            };

            // Add session manager metrics if available
            if (SessionManager != null)
            {
                var sessionMetrics = SessionManager.GetMetrics();
                foreach (var kvp in sessionMetrics)
                {
                    metrics[$"SessionManager_{kvp.Key}"] = kvp.Value;
                }
            }

            return metrics;
        }

        #endregion

        #region "IDisposable Implementation"
        /// <summary>
        /// Protected virtual dispose method for freeing resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var session in SessionManager.Sessions.ToList())
                    {
                        CleanupSession(session);
                    }

                    ShutDown();
                }
                // Free unmanaged objects (if any) and set large fields to null

                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~PythonNetRunTimeManager()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
