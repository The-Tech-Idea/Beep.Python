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

namespace Beep.Python.RuntimeEngine.ViewModels
{
    public partial class PackageManagerViewModel : PythonBaseViewModel, IPackageManagerViewModel
    {
        [ObservableProperty]
        ObservableBindingList<PackageDefinition> packages;
        public UnitofWork<PackageDefinition> unitofWork { get; set; }
        public PackageManagerViewModel() : base()
        {
        }
        public PackageManagerViewModel(IPythonRunTimeManager pythonRuntimeManager, PyModule persistentScope) : base(pythonRuntimeManager, persistentScope)
        {
            Init();
        }
        public PackageManagerViewModel(IPythonRunTimeManager pythonRuntimeManager) : base(pythonRuntimeManager)
        {
            InitializePythonEnvironment();
            
        }
        public void Init()
        {
            if(PythonRuntime.CurrentRuntimeConfig.Packagelist==null)
            {
                PythonRuntime.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
            }
            Packages=new ObservableBindingList<PackageDefinition>(PythonRuntime.CurrentRuntimeConfig.Packagelist);
            Editor = PythonRuntime.DMEditor;
            unitofWork = new UnitofWork<PackageDefinition>(Editor, true, new ObservableBindingList<PackageDefinition>(PythonRuntime.CurrentRuntimeConfig.Packagelist), "ID");
            unitofWork.PostCreate += UnitofWork_PostCreate;
        }
        private void UnitofWork_PostCreate(object sender, UnitofWorkParams e)
        {
           PackageDefinition package= (PackageDefinition)sender;
            if (package != null)
            {
               package.ID=global::System.Guid.NewGuid().ToString();
            }
        }

        public bool InstallPipToolAsync()
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)

                return retval;
            if (PythonRuntime.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }

             InstallPIP(Progress, Token);


            PythonRuntime.IsBusy = false;
            return retval;
        }
        public bool InstallNewPackageAsync(string packagename)
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)

                return retval;
            if (PythonRuntime.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
                retval =  InstallPackage(packagename, Progress, Token);
            }

            return retval;
        }
        public bool UnInstallPackageAsync(string packagename)
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)

                return retval;
            if (PythonRuntime.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
                retval =   RemovePackage(packagename, Progress, Token);
            }

            return retval;
        }
        public bool UpgradePackageAsync(string packagename)
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)

                return retval;
            if (PythonRuntime.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
                retval =   UpdatePackage(packagename, Progress, Token).Result;
            }

            return retval;
        }
        public bool UpgradeAllPackagesAsync()
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)

                return retval;
            if (PythonRuntime.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            retval =  RefreshInstalledPackagesList(Progress, Token);
            return retval;
        }
        public bool RefreshPackageAsync(string packagename)
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)

                return retval;
            if (PythonRuntime.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (!string.IsNullOrEmpty(packagename))
            {
                retval =   RefreshInstalledPackage(packagename, Progress, Token).Result;
            }

            return retval;
        }
        public bool RefreshAllPackagesAsync()
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)

                return retval;
            if (PythonRuntime.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (PythonRuntime != null)
            {
                retval= RefreshInstalledPackagesList(Progress, Token);
                PythonRuntime.SaveConfig();
            }
            return retval;
        }
        public async Task<bool> GetPackagesAsync()
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)

                return retval;
            if (PythonRuntime.IsBusy)
            {
                //  MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }
            if (PythonRuntime != null)
            {
                 RefreshInstalledPackagesList(Progress, Token);
                PythonRuntime.SaveConfig();
            }
            return retval;
        }
        public void Dispose()
        {
           
            unitofWork = null;
           
        }
        #region "Package Manager"
        public async Task<string> RunPackageManagerAsync(IProgress<PassedArgs> progress, string packageName, PackageAction packageAction, bool useConda = false)
        {
            string customPath = $"{PythonRuntime.CurrentRuntimeConfig.BinPath.Trim()};{PythonRuntime.CurrentRuntimeConfig.ScriptPath.Trim()}".Trim();
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

            //    using (Py.GIL())
            //   
            PyModule scope = PythonRuntime.PersistentScope;
                
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
                    Progress.Report(new PassedArgs() { Messege = $"Running {command}" });
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
                    {
                        while (await outputChannel.Reader.WaitToReadAsync())
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
                //}
        //    }
            if (output.Length > 0)
            {
                progress.Report(new PassedArgs() { Messege = $"Finished {command}" });
            }
            else
                progress.Report(new PassedArgs() { Messege = $"Finished {command} eith error" });
            return output;
        }
        public  bool ListpackagesAsync(IProgress<PassedArgs> _progress, CancellationToken token, bool useConda = false, string packagename = null)
        {
            if (IsBusy) return false;
            IsBusy = true;
            int i = 0;
            if (_progress != null)
            {
                Progress = _progress;
            }
            try
            {
                bool checkall = true;
                if (!string.IsNullOrEmpty(packagename))
                {
                    checkall = false;
                }
             
                //           runTimeManager._pythonRuntimeManager.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                using (var gil = PythonRuntime.GIL())
                {
                  
                        dynamic pkgResources = Py.Import("importlib.metadata");
                        dynamic workingSet = pkgResources.distributions();
                        int count = PythonRuntime.CurrentRuntimeConfig.Packagelist.Count;
                        int j = 1;
                        bool IsInternetAvailabe = PythonRunTimeDiagnostics.CheckNet();
                        foreach (dynamic pkg in workingSet)
                        {
                            i++;
                            string packageName = pkg.metadata["Name"];
                            string packageVersion = pkg.version.ToString();
                            string line = $"Checking Package {packageName}: {packageVersion}";
                            Console.WriteLine(line);
                            // runTimeManager.OutputLines.Add(line);
                            Progress.Report(new PassedArgs() { Messege = line, ParameterInt1 = j, ParameterInt2 = count });
                          
                            PackageDefinition onlinepk = new PackageDefinition();
                            if (!string.IsNullOrEmpty(packageVersion))
                            {
                                if (checkall)
                                {
                                if (IsInternetAvailabe)
                                {
                                    onlinepk = PythonRunTimeDiagnostics.CheckIfPackageExistsAsync(packageName).Result;

                                }


                                PackageDefinition package = PythonRuntime.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                    if (package != null)
                                    {
                                        int idx = PythonRuntime.CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                        if (onlinepk != null)
                                        {
                                            package.updateversion = onlinepk.version;
                                        }
                                        package.installed = true;
                                        package.buttondisplay = "Installed";

                                        if (onlinepk != null)
                                        {
                                            PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                            line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                        }
                                        else
                                        {
                                            PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                            line = $"Package {packageName}: {"Not Found"}";
                                        }

                                        Console.WriteLine(line);
                                        // runTimeManager.OutputLines.Add(line);
                                        Progress.Report(new PassedArgs() { Messege = line });
                                    }
                                    else
                                    {
                                        PackageDefinition packagelist = new PackageDefinition();
                                        packagelist.packagename = packageName;
                                        packagelist.version = packageVersion;
                                        packagelist.updateversion = packageVersion;
                                        packagelist.installed = true;
                                        packagelist.buttondisplay = "Added";
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist.Add(packagelist);
                                        line = $"Added new Package {packagelist}: {packagelist.version}";
                                        Console.WriteLine(line);
                                        //  runTimeManager.OutputLines.Add(line);
                                        Progress.Report(new PassedArgs() { Messege = line });
                                    }

                                }
                                else
                                {
                                    if (PythonRuntime.CurrentRuntimeConfig.Packagelist.Any(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        PackageDefinition package = PythonRuntime.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                        int idx = PythonRuntime.CurrentRuntimeConfig.Packagelist.IndexOf(package);
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
                                            PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                            package.updateversion = onlinepk.version;
                                            line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                        }
                                        else
                                        {
                                            PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                            line = $"Package {packageName}: {"Not Found"}";
                                        }

                                        Console.WriteLine(line);
                                        // runTimeManager.OutputLines.Add(line);
                                        Progress.Report(new PassedArgs() { Messege = line });
                                    }
                                }

                            Packages = new ObservableBindingList<PackageDefinition>(PythonRuntime.CurrentRuntimeConfig.Packagelist);
                        }
                            else Console.WriteLine($" empty {packageName}: {packageVersion}");

                            j++;
                        }
                   
                   
                }
                if (i == 0)
                {
                    Progress.Report(new PassedArgs() { Messege = "No Packages Found" });
                    PythonRuntime.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                    Packages=new ObservableBindingList<PackageDefinition>(PythonRuntime.CurrentRuntimeConfig.Packagelist);
                }
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Console.WriteLine("Error: in Listing Packages");
            }
            IsBusy = false;
            return IsBusy;
        }
        public bool Listpackages2(IProgress<PassedArgs> _progress, CancellationToken token, bool useConda = false, string packagename = null)
        {
            if (IsBusy) return false;
            IsBusy = true;
            int i = 0;
            if (_progress != null)
            {
                Progress = _progress;
            }
            try
            {
                bool checkall = true;
                if (!string.IsNullOrEmpty(packagename))
                {
                    checkall = false;
                }

                //           runTimeManager._pythonRuntimeManager.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                using (var gil = PythonRuntime.GIL())
                {

                    dynamic pkgResources = Py.Import("importlib.metadata");
                    dynamic workingSet = pkgResources.distributions();
                    int count = PythonRuntime.CurrentRuntimeConfig.Packagelist.Count;
                    int j = 1;
                    foreach (dynamic pkg in workingSet)
                    {
                        i++;
                        string packageName = pkg.metadata["Name"];
                        string packageVersion = pkg.version.ToString();
                        string line = $"Checking Package {packageName}: {packageVersion}";
                        Console.WriteLine(line);
                        // runTimeManager.OutputLines.Add(line);
                        Progress.Report(new PassedArgs() { Messege = line, ParameterInt1 = j, ParameterInt2 = count });
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


                                PackageDefinition package = PythonRuntime.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (package != null)
                                {
                                    int idx = PythonRuntime.CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                    if (onlinepk != null)
                                    {
                                        package.updateversion = onlinepk.version;
                                    }
                                    package.installed = true;
                                    package.buttondisplay = "Installed";

                                    if (onlinepk != null)
                                    {
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                    // runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }
                                else
                                {
                                    PackageDefinition packagelist = new PackageDefinition();
                                    packagelist.packagename = packageName;
                                    packagelist.version = packageVersion;
                                    packagelist.updateversion = packageVersion;
                                    packagelist.installed = true;
                                    packagelist.buttondisplay = "Added";
                                    PythonRuntime.CurrentRuntimeConfig.Packagelist.Add(packagelist);
                                    line = $"Added new Package {packagelist}: {packagelist.version}";
                                    Console.WriteLine(line);
                                    //  runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }

                            }
                            else
                            {
                                if (PythonRuntime.CurrentRuntimeConfig.Packagelist.Any(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    PackageDefinition package = PythonRuntime.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                    int idx = PythonRuntime.CurrentRuntimeConfig.Packagelist.IndexOf(package);
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
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        package.updateversion = onlinepk.version;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                    // runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }
                            }


                        }
                        else Console.WriteLine($" empty {packageName}: {packageVersion}");

                        j++;
                    }


                }
                if (i == 0)
                {
                    Progress.Report(new PassedArgs() { Messege = "No Packages Found" });
                    PythonRuntime.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                }
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Console.WriteLine("Error: in Listing Packages");
            }
            IsBusy = false;
            return IsBusy;
        }
        public bool listpackages(IProgress<PassedArgs> _progress, CancellationToken token, bool useConda = false, string packagename = null)
        {
            if (IsBusy) return false;
            IsBusy = true;
            int i = 0;
            if (_progress != null)
            {
                Progress = _progress;
            }
            try
            {
                bool checkall = true;
                if (!string.IsNullOrEmpty(packagename))
                {
                    checkall = false;
                }
                string script = @"
import importlib.metadata
result = [{'name': pkg.metadata['Name'], 'version': pkg.version} for pkg in importlib.metadata.distributions()]
result";

                // Execute the script and get the result
                dynamic packages = RunPythonScriptWithResult(script, null);

                if (packages != null)
                {
                    int j = 1;
                    int count = 0;// packages.Count;
                    foreach (var pkg in packages)
                    {
                        string packageName = pkg.name;
                        string packageVersion = pkg.version;
                        string line = $"Checking Package {packageName}: {packageVersion}";
                        Console.WriteLine(line);
                        _progress?.Report(new PassedArgs() { Messege = line });
                       
                        Console.WriteLine(line);
                        // runTimeManager.OutputLines.Add(line);
                        Progress.Report(new PassedArgs() { Messege = line, ParameterInt1 = j, ParameterInt2 = count });
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


                                PackageDefinition package = PythonRuntime.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (package != null)
                                {
                                    int idx = PythonRuntime.CurrentRuntimeConfig.Packagelist.IndexOf(package);
                                    if (onlinepk != null)
                                    {
                                        package.updateversion = onlinepk.version;
                                    }
                                    package.installed = true;
                                    package.buttondisplay = "Installed";

                                    if (onlinepk != null)
                                    {
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                    // runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }
                                else
                                {
                                    PackageDefinition packagelist = new PackageDefinition();
                                    packagelist.packagename = packageName;
                                    packagelist.version = packageVersion;
                                    packagelist.updateversion = packageVersion;
                                    packagelist.installed = true;
                                    packagelist.buttondisplay = "Added";
                                    PythonRuntime.CurrentRuntimeConfig.Packagelist.Add(packagelist);
                                    line = $"Added new Package {packagelist}: {packagelist.version}";
                                    Console.WriteLine(line);
                                    //  runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }

                            }
                            else
                            {
                                if (PythonRuntime.CurrentRuntimeConfig.Packagelist.Any(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    PackageDefinition package = PythonRuntime.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename != null && p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                    int idx = PythonRuntime.CurrentRuntimeConfig.Packagelist.IndexOf(package);
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
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = onlinepk.updateversion;
                                        package.updateversion = onlinepk.version;
                                        line = $"Package {packageName}: {packageVersion} found with version {onlinepk.updateversion}";
                                    }
                                    else
                                    {
                                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx].updateversion = "Not Found";
                                        line = $"Package {packageName}: {"Not Found"}";
                                    }

                                    Console.WriteLine(line);
                                    // runTimeManager.OutputLines.Add(line);
                                    Progress.Report(new PassedArgs() { Messege = line });
                                }
                            }


                        }
                        else Console.WriteLine($" empty {packageName}: {packageVersion}");

                        // Add logic to update PythonRuntime.CurrentRuntimeConfig.Packagelist or other necessary operations
                    }
                }
                //           runTimeManager._pythonRuntimeManager.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                using (var gil = PythonRuntime.GIL())
                {

                    dynamic pkgResources = Py.Import("importlib.metadata");
                    dynamic workingSet = pkgResources.distributions();
                    int count = PythonRuntime.CurrentRuntimeConfig.Packagelist.Count;
                    int j = 1;
                    foreach (dynamic pkg in workingSet)
                    {
                        i++;
     
                        j++;
                    }


                }
                if (i == 0)
                {
                    Progress.Report(new PassedArgs() { Messege = "No Packages Found" });
                    PythonRuntime.CurrentRuntimeConfig.Packagelist = new List<PackageDefinition>();
                }
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Console.WriteLine("Error: in Listing Packages");
            }
            IsBusy = false;
            return IsBusy;
        }
        public bool InstallPIP(IProgress<PassedArgs> progress, CancellationToken token)
        {

            bool pipInstall = true;
            if (IsBusy) return false;
            IsBusy = true;
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
                RunPackageManagerAsync(progress, scriptPath, PackageAction.InstallPackager, PythonRunTimeDiagnostics.GetPackageType(PythonRuntime.CurrentRuntimeConfig.BinPath) == PackageType.pypi);
                IsBusy = false;
                // Delete the installer script
                File.Delete(scriptPath);

            }
            catch (Exception ex)
            {
                IsBusy = false;
                pipInstall = false;
                Editor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return pipInstall;
        }
        public async Task<PackageDefinition> FindPackageUpdate(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            bool IsInternetAvailabe = PythonRunTimeDiagnostics.CheckNet();
            PackageDefinition retval = null;
            Editor.ErrorObject.Flag = Errors.Ok;
            if (IsInternetAvailabe)
            {
                retval =  await CheckIfPackageExistsAsync(packageName);
            }

            PackageDefinition installedpackage = null;
            bool isInstalled = false;
            int idx = -1;
            if (IsBusy) return null;
            IsBusy = true;

            try
            {
                // Create a new Python scope
                using (Py.GIL())
                {
                    // dynamic pip = Py.Import("pip");

                    //string packageName = "requests"; // Replace with the name of the package you are interested in


                    // Check if an update is available
                    bool isUpdateAvailable = false;
                    if (retval != null)
                    {

                         listpackages(progress, token);



                        installedpackage = PythonRuntime.CurrentRuntimeConfig.Packagelist.Where(p => p.packagename.Equals(packageName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        idx = PythonRuntime.CurrentRuntimeConfig.Packagelist.IndexOf(installedpackage);
                        isUpdateAvailable = (new Version(retval.updateversion) > new Version(installedpackage.version));
                    }

                    // Print the result
                    if (isUpdateAvailable)
                    {
                        isInstalled = true;
                        installedpackage.updateversion = retval.updateversion;
                        installedpackage.buttondisplay = "Update";
                        PythonRuntime.CurrentRuntimeConfig.Packagelist[idx] = installedpackage;
                        //OutputLines.Add($"An update to {packageName} is available ({retval.updateversion}).");
                        progress.Report(new PassedArgs() { Messege = $"An update to {packageName} is available ({retval.updateversion})" });
                        Console.WriteLine($"An update to {packageName} is available ({retval.updateversion}).");
                    }
                    else
                    {
                        isInstalled = false;
                        installedpackage.buttondisplay = "Installed";
                        installedpackage.updateversion = installedpackage.version;
                        progress.Report(new PassedArgs() { Messege = $"No update to {packageName} is available." });
                        Console.WriteLine($"No update to {packageName} is available.");
                    }
                }

            }
            catch (Exception ex)
            {
                isInstalled = false;
                Editor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return installedpackage;
        }
        public async Task<bool> IsPackageInstalled(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            Editor.ErrorObject.Flag = Errors.Ok;
            bool isInstalled = false;

            try
            {
                // Create a new Python scope
                // Check if a package is installed and capture output
                using (Py.GIL())
                {
                    //dynamic scope = Py.CreateScope();
                    // string packageName = "numpy"; // Replace with the name of the package you want to check
                    string code = @"
        import subprocess
        output = subprocess.check_output(['pip', 'list'])
        print(output.decode('utf-8'))
    ";

                 //   PythonEngine.Exec(code, scope);
                 //string output = scope.get("__builtins__").get("print")?.ToString();
                    await Task.Run(()=> PythonRuntime.PersistentScope.Exec(code));
                    dynamic scope = PythonRuntime.PersistentScope;
                    string output = scope.get("__builtins__").get("print")?.ToString();
                    // Console.WriteLine(output);

                    isInstalled = output.Contains(packageName);
                    string outputmessage = $"Package '{packageName}' is {(isInstalled ? "installed" : "not installed")}";
                    Console.WriteLine(outputmessage);
                    // OutputLines.Add(outputmessage);
                    isInstalled = true;
                }

            }
            catch (Exception ex)
            {
                IsBusy = false;
                isInstalled = false;
                Editor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return isInstalled;
        }
        public   bool InstallPackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            Editor.ErrorObject.Flag = Errors.Ok;



            try
            {
                if (IsBusy) { return false; }
                IsBusy = true;
                  RunPackageManagerAsync(progress, packageName, PackageAction.Install, PythonRunTimeDiagnostics.GetPackageType(PythonRuntime.CurrentRuntimeConfig.BinPath) == PackageType.conda);


                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Editor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return true;
        }
        public bool RemovePackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            Editor.ErrorObject.Flag = Errors.Ok;

            bool isInstalled = false;

            try
            {
                if (IsBusy) { return false; }
                RunPackageManagerAsync(progress, packageName, PackageAction.Remove, false);
                IsBusy = false;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                isInstalled = false;
                Editor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return isInstalled;
        }
        public async Task<bool> UpdatePackage(string packageName, IProgress<PassedArgs> progress, CancellationToken token)
        {
            Editor.ErrorObject.Flag = Errors.Ok;

            bool isInstalled = false;
            try
            {
                if (packageName.Equals("pip", StringComparison.CurrentCultureIgnoreCase))
                {
                     RunPackageManagerAsync(progress, packageName, PackageAction.UpgradePackager, PythonRunTimeDiagnostics.GetPackageType(PythonRuntime.CurrentRuntimeConfig.BinPath) == PackageType.conda);
                }
                else RunPackageManagerAsync(progress, packageName, PackageAction.Update, PythonRunTimeDiagnostics.GetPackageType(PythonRuntime.CurrentRuntimeConfig.BinPath) == PackageType.conda);


                IsBusy = false;

            }
            catch (Exception ex)
            {
                IsBusy = false;
                isInstalled = false;
                Editor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
            }
            IsBusy = false;
            return isInstalled;
        }
        public  bool RefreshInstalledPackagesList(IProgress<PassedArgs> progress, CancellationToken token)
        {

            try
            {
                var retval = ListpackagesAsync(progress, token);
                IsBusy = false;
                return true;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Editor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

        }
        public async Task<bool> RefreshInstalledPackage(string packagename, IProgress<PassedArgs> progress, CancellationToken token)
        {

            try
            {
                var retval =  ListpackagesAsync(progress, token);
                IsBusy = false;
                return true;
            }
            catch (Exception ex)
            {
                IsBusy = false;
                Editor.AddLogMessage("Beep AI Python", ex.Message, DateTime.Now, 0, null, Errors.Failed);
                return false;
            }

        }
        public async Task<PackageDefinition> CheckIfPackageExistsAsync(string packageName)
        {
            using HttpClient httpClient = new HttpClient();
            HttpResponseMessage response;

            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // set timeout to 30 seconds
            try
            {
                response = await httpClient.GetAsync($"https://pypi.org/pypi/{packageName}/json", cts.Token).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                // Network error, API not available, etc.
                Console.WriteLine("An error occurred while checking the package. Please try again later.");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"The request to '{packageName}' timed out.");
                return null;
            }

            // If the response status code is OK (200), the package exists
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic packageData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);
                    string latestVersion = packageData.info.version;
                    string description = packageData.info.description;

                    PackageDefinition packageInfo = new PackageDefinition
                    {
                        packagename = packageName,
                        version = latestVersion,
                        description = description
                    };

                    return packageInfo;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while parsing package data for '{packageName}': {ex.Message}");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"The package '{packageName}' does not exist on PyPI.");
                return null;
            }
        }
        #endregion "Package Manager"
    }
}
