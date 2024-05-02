using Beep.Python.Model;
using DataManagementModels.Editor;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Util;
using System.Linq;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using TheTechIdea.Beep.Container.Services;
using System.Diagnostics;

namespace Beep.Python.RuntimeEngine.ViewModels
{
    public class PythonVirtualEnvViewModel : PythonBaseViewModel, IPythonVirtualEnvViewModel
    {
        public PythonVirtualEnvViewModel(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager) : base(beepservice, pythonRuntimeManager)
        {
            InitializePythonEnvironment();
        }

        public bool InitializeForUser(string envBasePath, string username)
        {

            string userEnvPath = Path.Combine(envBasePath, username);

            if (!Directory.Exists(userEnvPath))
            {
                // Create the virtual environment if it does not exist
                bool creationSuccess = CreateVirtualEnvironmentFromCommand(userEnvPath); //CreateVirtualEnvironment(userEnvPath);
                if (!creationSuccess)
                {
                    return false;
                }
            }

            return PythonRuntime.Initialize(userEnvPath);  // Call to the modified Initialize method with the path to the virtual environment
        }
        public bool CreateVirtualEnvironmentFromCommand(string envPath)
        {
            if (Directory.Exists(envPath))
            {
                Console.WriteLine("Virtual environment already exists.");
                return true; // No need to create if it already exists
            }

            try
            {

                // Command to create virtual environment
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = PythonRunTimeDiagnostics.GetPythonExe(PythonRuntime.CurrentRuntimeConfig.BinPath), // Ensure this points to the global/system Python executable
                        Arguments = $"-m venv {envPath} --copies --clear ",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Virtual environment created at: {envPath}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Failed to create virtual environment: {err}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return false;
            }
        }
        public bool CreateVirtualEnvironment(string envPath)
        {
            if (Directory.Exists(envPath))
            {
                Console.WriteLine("Virtual environment already exists.");
                return false;
            }

            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(envPath);

                using (Py.GIL())  // Acquire the Python Global Interpreter Lock
                {
                    // Import the required Python module
                    dynamic venv = Py.Import("venv");

                    // Create the virtual environment
                    venv.create(envPath, with_pip: true);
                }

                Console.WriteLine($"Virtual environment created at: {envPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create virtual environment: {ex.Message}");
                return false;
            }
        }
        public bool CreateVirtualEnv(string envPath)
        {
            string pythonCode = $@"
import os
import sys
from urllib.request import urlretrieve
from subprocess import Popen, PIPE
import venv

class ExtendedEnvBuilder(venv.EnvBuilder):
    def post_setup(self, context):
        os.environ['VIRTUAL_ENV'] = context.env_dir
        if not self.nodist:
            self.install_setuptools(context)
        if not self.nopip and not self.nodist:
            self.install_pip(context)

    def reader(self, stream, context):
        while True:
            s = stream.readline()
            if not s:
                break
            sys.stderr.write(s.decode('utf-8'))
            sys.stderr.flush()
        stream.close()

    def install_script(self, context, name, url):
        _, _, path, _, _, _ = urlparse(url)
        fn = os.path.split(path)[-1]
        binpath = context.bin_path
        distpath = os.path.join(binpath, fn)
        urlretrieve(url, distpath)
        args = [context.env_exe, fn]
        p = Popen(args, stdout=PIPE, stderr=PIPE, cwd=binpath)
        t1 = Thread(target=self.reader, args=(p.stdout, 'stdout'))
        t1.start()
        t2 = Thread(target=self.reader, args=(p.stderr, 'stderr'))
        t2.start()
        p.wait()
        t1.join()
        t2.join()
        os.unlink(distpath)

    def install_setuptools(self, context):
        url = 'https://bootstrap.pypa.io/ez_setup.py'
        self.install_script(context, 'setuptools', url)

    def install_pip(self, context):
        url = 'https://bootstrap.pypa.io/get-pip.py'
        self.install_script(context, 'pip', url)

builder = ExtendedEnvBuilder(with_pip=False, with_setuptools=False)
builder.create(r'{envPath}')
";

            try
            {
                PythonRuntime.RunCode(pythonCode, Progress, Token);
                Console.WriteLine($"Virtual environment created at: {envPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create virtual environment: {ex.Message}");
                return false;
            }
        }

        public IErrorsInfo ShutDown()
        {
            ErrorsInfo er = new ErrorsInfo();
            er.Flag = Errors.Ok;
            if (IsBusy) return er;
            IsBusy = true;

            try
            {
                PythonRuntime.ShutDown();
            }
            catch (Exception ex)
            {
                er.Ex = ex;
                er.Flag = Errors.Failed;
                er.Message = ex.Message;

            }
            IsBusy = false;
            return er;

        }
        public async void InitializePythonEnvironment()
        {
            await Task.Run(() =>
            {
                PythonRuntime.Initialize();
            });
        }

    }
}
