using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Python.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Model;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;


namespace Beep.Python.RuntimeEngine
{
    public class PythonVirtualEnvManager :  IPythonVirtualEnvManager,IDisposable
    {
        private bool disposedValue;
        private IBeepService _beepservice;
        IPythonRunTimeManager PythonRuntime;
        public bool IsBusy { get; private set; }
        public ObservableBindingList<PythonVirtualEnvironment> ManagedVirtualEnvironments { get; set; } = new();

        public PythonVirtualEnvManager(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager)
        {
            _beepservice = beepservice;
            PythonRuntime = pythonRuntimeManager;
            IsBusy = false;
        }

        /// <summary>
        /// Creates a virtual environment from an existing environment definition.
        /// </summary>
        public bool CreateVirtualEnvironmentFromDefinition(PythonRunTime cfg, PythonVirtualEnvironment env)
        {
            if (Directory.Exists(env.Path))
            {
                Console.WriteLine($"Virtual environment already exists at {env.Path}.");
                return true;
            }

            try
            {
                // Create a session for this operation
                var session = new PythonSessionInfo
                {
                    SessionName = $"CreateVEnvDef_{Path.GetFileName(env.Path)}",
                    StartedAt = DateTime.Now,
                    VirtualEnvironmentId = env.ID
                    
                };

                // Associate session with environment
                env.AddSession(session);

                // Add to global sessions collection
                if (!PythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    PythonRuntime.SessionManager.Sessions.Add(session);
                }

                // Use the Python executable from the configuration
                string pythonExe = PythonRunTimeDiagnostics.GetPythonExe(cfg.BinPath);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = $"-m venv \"{env.Path}\" --copies --clear",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Update session info
                session.EndedAt = DateTime.Now;

                if (process.ExitCode == 0)
                {
                    session.WasSuccessful = true;
                    session.Notes = $"Created virtual environment at {env.Path}";
                    Console.WriteLine($"Virtual environment created at: {env.Path}");

                    // Add environment to managed environments
                    if (!PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments.Any(e =>
                        e.Path.Equals(env.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments.Add(env);
                    }

                    return true;
                }
                else
                {
                    session.WasSuccessful = false;
                    session.Notes = $"Failed: {error}";
                    Console.WriteLine($"Failed to create virtual environment: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating virtual environment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initializes a virtual environment for a specific user with a session.
        /// </summary>
        public bool InitializeForUser(PythonRunTime cfg, PythonSessionInfo sessionInfo)
        {
            try
            {
                // Find existing environment by ID
                var existingEnv = PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments
                    .FirstOrDefault(e => e.ID == sessionInfo.VirtualEnvironmentId);

                if (existingEnv == null)
                {
                    Console.WriteLine($"No virtual environment found with ID: {sessionInfo.VirtualEnvironmentId}");
                    return false;
                }

                // Associate session with environment if not already associated
                if (!existingEnv.Sessions.Any(s => s.SessionId == sessionInfo.SessionId))
                {
                    existingEnv.AddSession(sessionInfo);
                }

                // Add to global sessions collection if not already present
                if (!PythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == sessionInfo.SessionId))
                {
                    PythonRuntime.SessionManager.Sessions.Add(sessionInfo);
                }

                // Initialize the Python runtime with the user's environment
                bool result = PythonRuntime.Initialize(cfg, existingEnv);

                // Create a session-specific scope
                if (result && !PythonRuntime.HasScope(sessionInfo))
                {
                    PythonRuntime.CreateScope(sessionInfo, existingEnv);
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing for user: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initializes a virtual environment for a specific user.
        /// </summary>
        public bool InitializeForUser(PythonRunTime config, string envBasePath, string username)
        {
            string userEnvPath = Path.Combine(envBasePath, username);

            if (!Directory.Exists(userEnvPath))
            {
                // Create the virtual environment if it does not exist
                if (!CreateVirtualEnvironmentFromCommand(config, userEnvPath))
                {
                    return false;
                }
            }

            // Look for an existing environment or create a new definition
            var existingEnv = PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments
                .FirstOrDefault(e => e.Path.Equals(userEnvPath, StringComparison.OrdinalIgnoreCase));

            if (existingEnv == null)
            {
                // Create a new environment definition
                existingEnv = new PythonVirtualEnvironment
                {
                    Name = username,
                    Path = userEnvPath,
                    PythonConfigID = config.ID,
                    BaseInterpreterPath = config.RuntimePath,
                    CreatedOn = DateTime.Now,
                    EnvironmentType = PythonEnvironmentType.VirtualEnv
                };

                // Add it to the managed environments
                if (!PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments.Any(e =>
                    e.Path.Equals(userEnvPath, StringComparison.OrdinalIgnoreCase)))
                {
                    PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments.Add(existingEnv);
                }
            }

            // Create a session for this user
            var session = new PythonSessionInfo
            {
                Username = username,
                VirtualEnvironmentId = existingEnv.ID,
                StartedAt = DateTime.Now
            };

            // Associate session with environment
            if (!existingEnv.Sessions.Any(s => s.SessionId == session.SessionId))
            {
                existingEnv.AddSession(session);
            }

            // Add to global sessions collection
            if (!PythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
            {
                PythonRuntime.SessionManager.Sessions.Add(session);
            }

            // Initialize the Python runtime with the user's environment
            bool result = PythonRuntime.Initialize(config, existingEnv);

            // Create a session-specific scope
            if (result && !PythonRuntime.HasScope(session))
            {
                PythonRuntime.CreateScope(session, existingEnv);
            }

            return result;
        }

        /// <summary>
        /// Creates a virtual environment using a subprocess to invoke Python.
        /// </summary>
        public bool CreateVirtualEnvironmentFromCommand(PythonRunTime config, string envPath)
        {
            if (Directory.Exists(envPath))
            {
                Console.WriteLine("Virtual environment already exists.");
                return true;
            }

            try
            {
                // Create a session for this command
                var session = new PythonSessionInfo
                {
                    SessionName = $"CreateVEnvCommand_{Path.GetFileName(envPath)}",
                    StartedAt = DateTime.Now
                };

                // Create virtual environment definition
                var currentEnv = new PythonVirtualEnvironment
                {
                    Name = Path.GetFileName(envPath),
                    Path = envPath,
                    PythonConfigID = config.ID,
                    BaseInterpreterPath = config.RuntimePath,
                    CreatedOn = DateTime.Now,
                    EnvironmentType = PythonEnvironmentType.VirtualEnv
                };

                // Associate session with environment
                session.VirtualEnvironmentId = currentEnv.ID;
                currentEnv.AddSession(session);

                // Track session
                if (!PythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    PythonRuntime.SessionManager.Sessions.Add(session);
                }

                string pythonExe = PythonRunTimeDiagnostics.GetPythonExe(config.BinPath);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = $"-m venv \"{envPath}\" --copies --clear",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Update session info
                session.EndedAt = DateTime.Now;

                if (process.ExitCode == 0)
                {
                    session.WasSuccessful = true;
                    session.Notes = $"Created virtual environment at {envPath}";
                    Console.WriteLine($"Virtual environment created at: {envPath}");

                    // Add environment to managed environments
                    if (!PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments.Any(e =>
                        e.Path.Equals(envPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments.Add(currentEnv);
                    }

                    return true;
                }
                else
                {
                    session.WasSuccessful = false;
                    session.Notes = $"Failed: {error}";
                    Console.WriteLine($"Failed to create virtual environment: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating virtual environment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a virtual environment using Python's venv module directly.
        /// </summary>
        public bool CreateVirtualEnvironment(PythonRunTime config, string envPath)
        {
            if (Directory.Exists(envPath))
            {
                Console.WriteLine("Virtual environment already exists.");
                return true;
            }

            try
            {
                // Create environment directory
                Directory.CreateDirectory(envPath);

                // Create a new environment definition
                var newEnv = new PythonVirtualEnvironment
                {
                    Name = Path.GetFileName(envPath),
                    Path = envPath,
                    PythonConfigID = config.ID,
                    BaseInterpreterPath = config.RuntimePath,
                    CreatedOn = DateTime.Now,
                    EnvironmentType = PythonEnvironmentType.VirtualEnv
                };

                // Create a session for this operation
                var session = new PythonSessionInfo
                {
                    SessionName = $"CreateVEnv_{Path.GetFileName(envPath)}",
                    StartedAt = DateTime.Now,
                    VirtualEnvironmentId = newEnv.ID
                };

                newEnv.AddSession(session);

                using (Py.GIL()) // Acquire Python GIL
                {
                    dynamic venv = Py.Import("venv");
                    venv.create(envPath, with_pip: true);
                }

                // Update session
                session.EndedAt = DateTime.Now;
                session.WasSuccessful = true;
                session.Notes = $"Created virtual environment at {envPath}";

                // Add environment to managed environments
                if (!PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments.Any(e =>
                    e.Path.Equals(envPath, StringComparison.OrdinalIgnoreCase)))
                {
                    PythonRuntime.VirtualEnvmanager.ManagedVirtualEnvironments.Add(newEnv);
                }

                // Add to global sessions
                if (!PythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                {
                    PythonRuntime.SessionManager.Sessions.Add(session);
                }

                Console.WriteLine($"Virtual environment created at: {envPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating virtual environment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initializes the Python runtime asynchronously for a specific environment.
        /// </summary>
        public void InitializePythonEnvironment(PythonVirtualEnvironment env)
        {
            if (env == null)
            {
                Console.WriteLine("Cannot initialize Python environment: environment is null.");
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    // Find the configuration for this environment
                    var config = PythonRuntime.PythonConfigs?.FirstOrDefault(c => c.ID == env.PythonConfigID);
                    if (config == null)
                    {
                        Console.WriteLine($"Configuration not found for environment {env.Name}");
                        return;
                    }

                    PythonRuntime.Initialize(config, env);

                    // Create a session for this initialization if needed
                    if (env.Sessions.Count == 0)
                    {
                        var session = new PythonSessionInfo
                        {
                            SessionName = $"Init_{env.Name}_{DateTime.Now.Ticks}",
                            StartedAt = DateTime.Now,
                            VirtualEnvironmentId = env.ID
                        };

                        env.AddSession(session);

                        if (!PythonRuntime.SessionManager.Sessions.Any(s => s.SessionId == session.SessionId))
                        {
                            PythonRuntime.SessionManager.Sessions.Add(session);
                        }

                        if (PythonRuntime.HasScope(session) == false)
                        {
                            PythonRuntime.CreateScope(session, env);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing Python environment: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Shuts down the Python runtime for a specific environment.
        /// </summary>
        public IErrorsInfo ShutDown(PythonVirtualEnvironment env)
        {
            ErrorsInfo er = new ErrorsInfo { Flag = Errors.Ok };
            if (IsBusy) return er;

          
            try
            {
                // Close all sessions for this environment
                foreach (var session in env.Sessions.ToList())
                {
                    if (PythonRuntime.HasScope(session))
                    {
                        // Remove the scope for this session
                        if (PythonRuntime.SessionScopes.ContainsKey(session.SessionId))
                        {
                            PythonRuntime.SessionScopes.Remove(session.SessionId);
                        }
                    }

                    session.EndedAt = DateTime.Now;
                    session.Status = PythonSessionStatus.Terminated;
                }

                // If there are no active sessions, we can shut down the runtime
                if (!PythonRuntime.SessionManager.Sessions.Any(s => s.Status == PythonSessionStatus.Active &&
                                                     s.VirtualEnvironmentId != env.ID))
                {
                    PythonRuntime.ShutDown();
                }
            }
            catch (Exception ex)
            {
                er.Flag = Errors.Failed;
                er.Message = ex.Message;
                er.Ex = ex;
            }
            finally
            {
                IsBusy = false;
            }

            return er;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PythonVirtualEnvManager()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
