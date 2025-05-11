using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Python.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;


namespace Beep.Python.RuntimeEngine.ViewModels
{
    public class PythonVirtualEnvViewModel : PythonBaseViewModel, IPythonVirtualEnvViewModel
    {
        public PythonVirtualEnvViewModel(IBeepService beepService, IPythonRunTimeManager pythonRuntimeManager)
            : base(beepService, pythonRuntimeManager)
        {
            InitializePythonEnvironment();
        }

        /// <summary>
        /// Initializes a virtual environment for a specific user.
        /// </summary>
        /// <summary>
        /// Initializes a virtual environment for a specific user.
        /// </summary>
        public bool InitializeForUser(string envBasePath, string username)
        {
            string userEnvPath = Path.Combine(envBasePath, username);

            if (!Directory.Exists(userEnvPath))
            {
                // Create the virtual environment if it does not exist
                if (!CreateVirtualEnvironmentFromCommand(userEnvPath))
                {
                    return false;
                }
            }

            // Look for an existing environment or create a new definition
            var existingEnv = PythonRuntime.ManagedVirtualEnvironments
                .FirstOrDefault(e => e.Path.Equals(userEnvPath, StringComparison.OrdinalIgnoreCase));

            if (existingEnv == null)
            {
                // Create a new environment definition
                existingEnv = new PythonVirtualEnvironment
                {
                    Name = username,
                    Path = userEnvPath
                };

                // Add it to the managed environments
                if (!PythonRuntime.ManagedVirtualEnvironments.Any(e =>
                    e.Path.Equals(userEnvPath, StringComparison.OrdinalIgnoreCase)))
                {
                    PythonRuntime.ManagedVirtualEnvironments.Add(existingEnv);
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
            if (!PythonRuntime.Sessions.Any(s => s.SessionId == session.SessionId))
            {
                PythonRuntime.Sessions.Add(session);
            }

            // Initialize the Python runtime with the user's environment
            bool result = PythonRuntime.Initialize(existingEnv);

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
        public bool CreateVirtualEnvironmentFromCommand(string envPath)
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

                // Use current environment or create temporary definition
                var currentEnv = PythonRuntime.CurrentVirtualEnvironment;
                if (currentEnv != null)
                {
                    session.VirtualEnvironmentId = currentEnv.ID;

                    // Track session
                    if (!currentEnv.Sessions.Any(s => s.SessionId == session.SessionId))
                    {
                        currentEnv.AddSession(session);
                    }

                    if (!PythonRuntime.Sessions.Any(s => s.SessionId == session.SessionId))
                    {
                        PythonRuntime.Sessions.Add(session);
                    }
                }

                string pythonExe = PythonRunTimeDiagnostics.GetPythonExe(PythonRuntime.CurrentRuntimeConfig.BinPath);
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
        public bool CreateVirtualEnvironment(string envPath)
        {
            if (Directory.Exists(envPath))
            {
                Console.WriteLine("Virtual environment already exists.");
                return true;
            }

            try
            {
                Directory.CreateDirectory(envPath);

                using (Py.GIL()) // Acquire Python GIL
                {
                    dynamic venv = Py.Import("venv");
                    venv.create(envPath, with_pip: true);
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
        /// Creates a virtual environment with enhanced configuration.
        /// </summary>
        /// <summary>
        /// Creates a virtual environment with enhanced configuration.
        /// </summary>
        public bool CreateVirtualEnv(string envPath)
        {
            string pythonCode = $@"
import os
import venv
import subprocess

class ExtendedEnvBuilder(venv.EnvBuilder):
    def post_setup(self, context):
        self.install_pip(context)

    def install_pip(self, context):
        try:
            subprocess.check_call([context.env_exe, '-m', 'ensurepip', '--upgrade'])
        except Exception as ex:
            raise RuntimeError(f'Failed to install pip: {{ex}}')

builder = ExtendedEnvBuilder(with_pip=True)
builder.create(r'{envPath}')
";

            try
            {
                // Create a new session for this specific operation
                var session = new PythonSessionInfo
                {
                    SessionName = $"CreateVEnv_{Path.GetFileName(envPath)}",
                    StartedAt = DateTime.Now
                };

                // Find or create a virtual environment to associate with this session
                var currentEnv = PythonRuntime.CurrentVirtualEnvironment;
                if (currentEnv != null)
                {
                    session.VirtualEnvironmentId = currentEnv.ID;

                    // Add the session to the environment and the session collection
                    if (!currentEnv.Sessions.Any(s => s.SessionId == session.SessionId))
                    {
                        currentEnv.AddSession(session);
                    }

                    if (!PythonRuntime.Sessions.Any(s => s.SessionId == session.SessionId))
                    {
                        PythonRuntime.Sessions.Add(session);
                    }

                    // Create a session-specific scope if needed
                    if (!PythonRuntime.HasScope(session))
                    {
                        PythonRuntime.CreateScope(session, currentEnv);
                    }
                }

                using (Py.GIL()) // Ensure thread safety with Python GIL
                {
                    // Run the code in the session-specific scope
                    PythonRuntime.RunCode(session, pythonCode, Progress, Token);

                    // Update session status
                    session.EndedAt = DateTime.Now;
                    session.Notes = $"Created virtual environment at {envPath}";
                    session.WasSuccessful = true;
                }

                Console.WriteLine($"Virtual environment created at: {envPath}");
                return true;
            }
            catch (PythonException ex) // Python.NET specific exception
            {
                Console.WriteLine($"Python error creating virtual environment: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating virtual environment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shuts down the Python runtime.
        /// </summary>
        public IErrorsInfo ShutDown()
        {
            ErrorsInfo er = new ErrorsInfo { Flag = Errors.Ok };
            if (IsBusy) return er;

            IsBusy = true;
            try
            {
                PythonRuntime.ShutDown();
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

        /// <summary>
        /// Initializes the Python runtime asynchronously.
        /// </summary>
        public async void InitializePythonEnvironment()
        {
            await Task.Run(() =>
            {
                PythonRuntime.Initialize();
            });
        }
    }
}
