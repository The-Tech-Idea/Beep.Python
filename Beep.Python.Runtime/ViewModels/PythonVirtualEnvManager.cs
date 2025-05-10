using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Python.Runtime;
using System;
using System.Diagnostics;
using System.IO;
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

            return PythonRuntime.Initialize(userEnvPath);
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

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Virtual environment created at: {envPath}");
                    return true;
                }
                else
                {
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
                using (Py.GIL()) // Ensure thread safety with Python GIL
                {
                    PythonRuntime.RunCode(pythonCode, Progress, Token);
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
