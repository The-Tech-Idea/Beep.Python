using Beep.Python.Model;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Util;

namespace Beep.Python.RuntimeEngine
{
    public class PythonNetManager :PythonBaseViewModel
    {
        public PythonNetManager(PythonNetRunTimeManager pythonRuntimeManager, PyModule persistentScope) : base(pythonRuntimeManager, persistentScope)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            _persistentScope = persistentScope;

        }

        public PythonNetManager(PythonNetRunTimeManager pythonRuntimeManager) : base(pythonRuntimeManager)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            InitializePythonEnvironment();
        }

        public async Task<bool> InstallPIP(IPythonRunTimeManager runTimeManager, IProgress<PassedArgs> progress, CancellationToken token)
        {

            bool pipInstall = true;
            if (runTimeManager.IsBusy) return false;
            runTimeManager.IsBusy = true;
            try
            {// Execute Python code and capture its output
             // Download the pip installer script
                string url = "https://bootstrap.pypa.io/get-pip.py";
                string scriptPath = Path.Combine(Path.GetTempPath(), "get-pip.py");
                WebClient client = new WebClient();
                client.DownloadFile(url, scriptPath);

                // Install pip
                //using (Py.GIL())
                //{
                //    using (PyModule scope = Py.CreateScope())
                //    {
                //        string code = File.ReadAllText(scriptPath);
                //       //unPythonCodeAndGetOutput(runTimeManager,progress,code);
                //    }


                //}
               await  RunPackageManagerAsync(runTimeManager, progress, scriptPath, PackageAction.InstallPackager,PythonRunTimeDiagnostics.GetPackageType(runTimeManager.CurrentRuntimeConfig.BinPath)== PackageType.conda);
                runTimeManager.IsBusy = false;
                // Delete the installer script
                File.Delete(scriptPath);

            }
            catch (Exception ex)
            {
                runTimeManager.IsBusy = false;
                pipInstall = false;
                runTimeManager.DMEditor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            runTimeManager.IsBusy = false;
            return pipInstall;
        }
        public  string RunPythonCodeAndGetOutput(IPythonRunTimeManager runTimeManager, IProgress<PassedArgs> progress, string code)
        {
            string wrappedPythonCode = $@"
import sys
import io
import clr

class CustomStringIO(io.StringIO):
    def write(self, s):
        super().write(s)
        output = self.getvalue()
        if output.strip():
            OutputHandler(output.strip())
            self.truncate(0)  # Clear the internal buffer
            self.seek(0)  # Reset the buffer pointer

def capture_output(code, globals_dict):
    original_stdout = sys.stdout
    sys.stdout = CustomStringIO()

    try:
        exec(code, dict(globals_dict))
    finally:
        sys.stdout = original_stdout
";
            bool isImage = false;
            string output = "";

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                 

                    Action<string> OutputHandler = line =>
                    {
                       // runTimeManager.OutputLines.Add(line);
                        progress.Report(new PassedArgs() { Messege = line });
                        Console.WriteLine(line);
                    };
                    scope.Set(nameof(OutputHandler), OutputHandler);

                    scope.Exec(wrappedPythonCode);
                    PyObject captureOutputFunc = scope.GetAttr("capture_output");
                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                    PyObject pyCode = code.ToPython();
                    PyObject pyGlobalsDict = globalsDict.ToPython();
                    PyObject result = captureOutputFunc.Invoke(pyCode, pyGlobalsDict);
                    if (result is PyObject pyObj)
                    {
                        var pyObjType = pyObj.GetPythonType();
                        var pyObjTypeName = pyObjType.ToString();


                    }
                }
            }

            runTimeManager.IsBusy = false;
            return output;
        }
        public  string RunPythonCodeAndGetOutput2(IPythonRunTimeManager runTimeManager, IProgress<PassedArgs> progress, string code)
        {
            string wrappedPythonCode = $@"
import sys
import io
import clr

class CustomStringIO(io.StringIO):
    def write(self, s):
        super().write(s)
        output = self.getvalue()
        if output.strip():
            OutputHandler(output.strip())

def is_image(obj):
    try:
        from PIL import Image
        if isinstance(obj, Image.Image):
            return True
    except ImportError:
        pass

    try:
        import matplotlib.pyplot as plt
        if isinstance(obj, plt.Figure):
            return True
    except ImportError:
        pass

    # Add checks for other image libraries here

    return False

def capture_output(code, globals_dict):
    original_stdout = sys.stdout
    sys.stdout = CustomStringIO()

    output = None
    try:
        output = exec(code, dict(globals_dict))
    finally:
        sys.stdout = original_stdout

    return output, is_image(output)
";

            string output = "";
            bool isImage = false;

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    Action<string> OutputHandler = line =>
                    {
                        //runTimeManager.OutputLines.Add(line);
                        progress.Report(new PassedArgs() { Messege = line });
                        Console.WriteLine(line);
                    };
                    scope.Set(nameof(OutputHandler), OutputHandler);

                    scope.Exec(wrappedPythonCode);
                    PyObject captureOutputFunc = scope.GetAttr("capture_output");
                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                    PyObject pyCode = code.ToPython();
                    PyObject pyGlobalsDict = globalsDict.ToPython();
                    PyTuple resultTuple = captureOutputFunc.Invoke(pyCode, pyGlobalsDict).As<PyTuple>();

                    PyObject result = resultTuple[0];

                    // Check if the returned object is an image
                    using (Py.GIL())
                    {
                        if (result is PyObject pyObj)
                        {
                            isImage = resultTuple[1].As<bool>();

                            if (!isImage)
                            {
                                var pyObjType = pyObj.GetPythonType();
                                var pyObjTypeName = pyObjType.ToString();

                                if (pyObjTypeName == "<class 'str'>")
                                {
                                    output = pyObj.As<string>();
                                }
                            }
                        }
                    }
                }
            }

            runTimeManager.IsBusy = false;
            return output;
        }
        public  void RunInteractivePython(PythonNetRunTimeManager runTimeManager)
        {

            string wrappedPythonCode = $@"
from io import StringIO
import sys

def capture_output_line(code, globals_dict):
    original_stdout = sys.stdout
    sys.stdout = StringIO()
    output = None

    try:
        exec(code, globals_dict)
        output = sys.stdout.getvalue()
    finally:
        sys.stdout = original_stdout

    return output.strip()
";

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    scope.Exec(wrappedPythonCode);
                    PyObject captureOutputFunc = scope.GetAttr("capture_output_line");
                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                    Console.WriteLine("Python interactive mode. Type 'exit()' to quit.");
                    while (true)
                    {
                        Console.Write(">>> ");
                        string inputLine = Console.ReadLine();

                        if (inputLine.ToLower().Trim() == "exit()")
                        {
                            break;
                        }

                        PyObject pyInputLine = new PyString(inputLine);
                        PyObject pyGlobalsDict = globalsDict.ToPython();
                        PyObject pyOutput = captureOutputFunc.Invoke(pyInputLine, pyGlobalsDict);
                        string output = pyOutput.As<string>();

                        if (!string.IsNullOrEmpty(output))
                        {
                            Console.WriteLine(output);
                        }
                    }
                }
            }
            runTimeManager.IsBusy = false;
        }
        public  void RunInteractivePython(PythonNetRunTimeManager runTimeManager, IProgress<PassedArgs> progress, string code)
        {

            string wrappedPythonCode = $@"
from io import StringIO
import sys

def capture_output_line(code, globals_dict):
    original_stdout = sys.stdout
    sys.stdout = StringIO()
    output = None

    try:
        exec(code, dict(globals_dict))
        output = sys.stdout.getvalue()
    finally:
        sys.stdout = original_stdout

    return output.strip()
";

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    scope.Exec(wrappedPythonCode);
                    PyObject captureOutputFunc = scope.GetAttr("capture_output_line");
                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                    Console.WriteLine("Python interactive mode. Type 'exit()' to quit.");
                    StringBuilder codeBlock = new StringBuilder();
                    int currentIndentLevel = 0;
                    bool inBlock = false;
                    string[] lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string inputLine in lines)
                    {
                        Console.Write(codeBlock.Length == 0 ? ">>> " : "... ");


                        if (inputLine.ToLower().Trim() == "exit()")
                        {
                            break;
                        }

                        codeBlock.AppendLine(inputLine);

                        int newIndentLevel = inputLine.TakeWhile(c => char.IsWhiteSpace(c)).Count();
                        bool isDedent = codeBlock.Length > 0 && newIndentLevel < currentIndentLevel;
                        bool isEmptyLine = string.IsNullOrWhiteSpace(inputLine);

                        if (!inBlock && !isEmptyLine)
                        {
                            inBlock = true;
                        }

                        if (inBlock && (isDedent || isEmptyLine))
                        {
                            inBlock = false;
                            PyObject pyCodeBlock = codeBlock.ToString().ToPython();
                            PyObject pyGlobalsDict = globalsDict.ToPython();
                            PyObject pyOutput = captureOutputFunc.Invoke(pyCodeBlock, pyGlobalsDict);
                            string output = pyOutput.As<string>();

                            if (!string.IsNullOrEmpty(output))
                            {
                                Console.WriteLine(output);
                            }

                            codeBlock.Clear();
                        }

                        currentIndentLevel = newIndentLevel;
                    }
                }
            }
            runTimeManager.IsBusy = false;
        }
        private  void UpgradePackage(string packageName)
    {
        using (Py.GIL()) // Ensure the Global Interpreter Lock is acquired
        {
            dynamic sys = Py.Import("sys");
            dynamic pip = Py.Import("pip");
            try
            {
                pip.main(new[] { "install", "--upgrade", packageName });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error upgrading package: {ex.Message}");
            }
        }
    }
        public  async Task<string> RunPackageManagerAsync(IPythonRunTimeManager runTimeManager, IProgress<PassedArgs> progress,string packageName, PackageAction packageAction, bool useConda = false)
        {
            string customPath = $"{runTimeManager.CurrentRuntimeConfig.BinPath.Trim()};{runTimeManager.CurrentRuntimeConfig.ScriptPath.Trim()}".Trim();
            string modifiedFilePath = customPath.Replace("\\", "\\\\");
            string output = "";
            string command = "";
            string wrappedPythonCode = $@"
import os
import subprocess
import threading
import queue

def set_custom_path(custom_path):
    # Modify the PATH environment variable
    os.environ[""PATH""] = '{modifiedFilePath}' + os.pathsep + os.environ[""PATH""]

def run_pip_and_capture_output(args, output_callback):
    def enqueue_output(stream, queue):
        for line in iter(stream.readline, b''):
            queue.put(line.decode('utf-8').strip())
        stream.close()

    process = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    stdout_queue = queue.Queue()
    stderr_queue = queue.Queue()

    stdout_thread = threading.Thread(target=enqueue_output, args=(process.stdout, stdout_queue))
    stderr_thread = threading.Thread(target=enqueue_output, args=(process.stderr, stderr_queue))

    stdout_thread.start()
    stderr_thread.start()

    while process.poll() is None or not stdout_queue.empty() or not stderr_queue.empty():
        while not stdout_queue.empty():
            line = stdout_queue.get_nowait()
            output_callback(line)

        while not stderr_queue.empty():
            line = stderr_queue.get_nowait()
            output_callback(line)

    stdout_thread.join()
    stderr_thread.join()
    process.communicate()

def run_with_timeout(func, args, output_callback, timeout):
    try:
        func(args, output_callback)
    except Exception as e:
        output_callback(str(e))
";

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    PyObject globalsDict = scope.GetAttr("__dict__");

                 
                    scope.Exec(wrappedPythonCode);
                   // Set the custom_path from C# and call set_custom_path function in Python
                 
                    PyObject setCustomPathFunc = scope.GetAttr("set_custom_path");
                    setCustomPathFunc.Invoke(modifiedFilePath.ToPython());

                    PyObject captureOutputFunc = scope.GetAttr("run_pip_and_capture_output");

                   

                    if (useConda)
                    {
                        switch (packageAction)
                        {
                            case PackageAction.Install:
                                command = $"conda install -c conda-forge {packageName}";
                                break;
                            case PackageAction.Remove:
                                command = $"conda remove {packageName}";
                                break;
                            case PackageAction.Update:
                                command = $"conda update {packageName}";
                                break;
                            case PackageAction.UpgradePackager:
                                command = $"conda update conda";
                                break;
                              
                            case PackageAction.InstallPackager:
                                command = $"conda {packageName}";
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch (packageAction)
                        {
                            case PackageAction.Install:
                                command = $"pip install -U {packageName}";
                                break;
                            case PackageAction.Remove:
                                command = $"pip uninstall  {packageName}";
                                break;
                            case PackageAction.Update:
                                command = $"pip install --upgrade {packageName}";
                                break;
                            case PackageAction.UpgradePackager:
                                command = $"python.exe -m pip install --upgrade pip";
                                break;
                            case PackageAction.InstallPackager:
                                command = $"python.exe {packageName}";
                                break;
                            default:
                                break;
                        }

                    }
                    progress.Report(new PassedArgs() { Messege = $"Running {command}" });
                    //runTimeManager.OutputLines.Add($"Running {command}");
                    PyObject pyArgs = new PyList();

                    pyArgs.InvokeMethod("extend", command.Split(' ').ToPython());


                    // Set the output_callback function in Python
                    Channel<string> outputChannel = Channel.CreateUnbounded<string>();
                    PyObject outputCallback = PyObject.FromManagedObject((Action<string>)(s => {
                        outputChannel.Writer.TryWrite(s);
                    }));
                    globalsDict.SetItem("output_callback", outputCallback);

                    // Run the Python code with a timeout
                    int timeoutInSeconds = 120; // Adjust this value as needed
                    PyObject runWithTimeoutFunc = scope.GetAttr("run_with_timeout");
                    Task pythonTask = Task.Run(() => runWithTimeoutFunc.Invoke(captureOutputFunc, pyArgs, outputCallback, timeoutInSeconds.ToPython()));

                    var outputList = new List<string>();
                    // Create an async method to read from the channel
                    async Task ReadFromChannelAsync()
                    { while (await outputChannel.Reader.WaitToReadAsync())
                        {
                            if (outputChannel.Reader.TryRead(out var line))
                            {
                                outputList.Add(line);
                                progress.Report(new PassedArgs() { Messege = line });
                                Console.WriteLine(line);
                            }
                        }
                       
                    }

                    // Process the output lines asynchronously
                    Task readOutputTask = ReadFromChannelAsync();

                    // Wait for the Python task to complete and close the channel writer
                    await pythonTask;
                    outputChannel.Writer.Complete();

                    // Wait for the readOutputTask to complete
                    await readOutputTask;


                    output = string.Join("\n", outputList);
                }
            }
            if(output.Length > 0)
            {
                progress.Report(new PassedArgs() { Messege = $"Finished {command}" });
            }else
                progress.Report(new PassedArgs() { Messege = $"Finished {command} eith error" });
            return output;
        }
        public  async Task<bool> listpackagesAsync(IPythonRunTimeManager runTimeManager, IProgress<PassedArgs> progress,  CancellationToken token, bool useConda = false, string packagename = null)
        {
            if (runTimeManager.IsBusy) return false;
            runTimeManager.IsBusy = true;
            int i = 0;
            try
            {
                bool checkall = true;
                if (!string.IsNullOrEmpty(packagename))
                {
                    checkall = false;
                }
      //           runTimeManager.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                using (Py.GIL())
                {
                    dynamic pkgResources = Py.Import("importlib.metadata");
                    dynamic workingSet = pkgResources.distributions();
               
                    foreach (dynamic pkg in workingSet)
                    {
                        i++;
                        string packageName = pkg.metadata["Name"];
                        string packageVersion = pkg.version.ToString();
                        string line = $"Checking Package {packageName}: {packageVersion}";
                        Console.WriteLine(line);
                       // runTimeManager.OutputLines.Add(line);
                        progress.Report(new PassedArgs() { Messege = line });
                        bool IsInternetAvailabe = PythonRunTimeDiagnostics.CheckNet();
                        PackageDefinition onlinepk = new PackageDefinition();
                        if (!string.IsNullOrEmpty(packageVersion))
                        {
                            if (checkall)
                            {
                                if (IsInternetAvailabe)
                                {
                                    onlinepk = PythonRunTimeDiagnostics.CheckIfPackageExistsAsync(packageName).Result;
                                }
                                

                                PackageDefinition package = runTimeManager.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename !=null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (package != null)
                                {
                                    int idx = runTimeManager.CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                    package.version = packageVersion;
                                    if (onlinepk != null)
                                    {
                                        package.updateversion = onlinepk.version;
                                    }
                                    else package.updateversion = packageVersion;
                                    package.installed = true;
                                    package.buttondisplay = "Installed";

                                    if (onlinepk != null)
                                    {
                                        runTimeManager.CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                        runTimeManager.CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                   // runTimeManager.OutputLines.Add(line);
                                    progress.Report(new PassedArgs() { Messege = line });
                                }
                                else
                                {
                                    PackageDefinition packagelist = new PackageDefinition();
                                    packagelist.packagename = packageName;
                                    packagelist.version = packageVersion;
                                    if (onlinepk != null)
                                    {
                                        packagelist.updateversion = onlinepk.version;
                                    }
                                    else packagelist.updateversion = packageVersion;

                                    packagelist.installed = true;
                                    packagelist.buttondisplay = "Installed";
                                    runTimeManager.CurrentRuntimeConfig.Packagelist.Add(packagelist);
                                    line = $"Added new Package {packagelist}: {packagelist.version}";
                                    Console.WriteLine(line);
                                  //  runTimeManager.OutputLines.Add(line);
                                    progress.Report(new PassedArgs() { Messege = line });
                                }
                               
                            }
                            else
                            {
                                if (runTimeManager.CurrentRuntimeConfig.Packagelist.Any(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    PackageDefinition package = runTimeManager.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                    int idx = runTimeManager.CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                    package.version = packageVersion;
                                    package.updateversion = packageVersion;
                                    package.installed = true;
                                    package.buttondisplay = "Installed";
                                    if (IsInternetAvailabe)
                                    {
                                        onlinepk = PythonRunTimeDiagnostics.CheckIfPackageExistsAsync(packageName).Result;
                                    }
                                    if (onlinepk != null)
                                    {
                                        runTimeManager.CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        package.updateversion = onlinepk.version;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                        runTimeManager.CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                   // runTimeManager.OutputLines.Add(line);
                                    progress.Report(new PassedArgs() { Messege = line });
                                }
                            }


                        }
                        else Console.WriteLine($" empty {packageName}: {packageVersion}");


                    }
                }
                if (i == 0)
                {
                    progress.Report(new PassedArgs() { Messege = "No Packages Found" });
                    runTimeManager.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                }
                runTimeManager.IsBusy = false;
            }
            catch (Exception ex)
            {
                runTimeManager.IsBusy = false;
                Console.WriteLine("Error: in Listing Packages");
            }
            runTimeManager.IsBusy = false;
            return await Task.FromResult<bool>(runTimeManager.IsBusy);
        }
        public  string Execute(string code)
        {
            StringBuilder output = new StringBuilder();
            using (Py.GIL()) // Acquire Python GIL (Global Interpreter Lock)
            {
                dynamic sys = Py.Import("sys");
                sys.stdout = new StringWriter(output);

                try
                {
                    PythonEngine.Exec(code);
                }
                catch (PythonException ex)
                {
                    output.AppendLine("Error: " + ex.Message);
                }
            }
            return output.ToString();
        }
    }
}
