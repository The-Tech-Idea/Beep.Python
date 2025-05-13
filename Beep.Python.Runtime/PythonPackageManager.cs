using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Helpers;
using Python.Runtime;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine.ViewModels
{
    public class PythonPackageManager : IPackageManagerViewModel
    {
        private readonly IBeepService _beepService;
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly IPythonSessionManager _sessionManager;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isBusy = false;
        private PythonSessionInfo _currentSession;
        private PythonVirtualEnvironment _currentEnvironment;
        private bool _isDisposed = false;

        public IDMEEditor Editor => _beepService?.DMEEditor;
        public ObservableBindingList<PackageDefinition> Packages { get; set; } = new();
        public UnitofWork<PackageDefinition> UnitofWork { get; set; }
        public IProgress<PassedArgs> Progress { get; set; }
        public CancellationToken Token => _cancellationTokenSource.Token;
        public bool IsBusy => _isBusy;

        public PythonPackageManager(
            IBeepService beepService,
            IPythonRunTimeManager pythonRuntime,
            IPythonSessionManager sessionManager)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _pythonRuntime = pythonRuntime ?? throw new ArgumentNullException(nameof(pythonRuntime));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));

            // Initialize progress reporting
            Progress = new Progress<PassedArgs>(args =>
            {
                if (Editor != null)
                {
                    Editor.AddLogMessage("Package Manager", args.Messege, DateTime.Now, -1, null, Errors.Ok);
                }
            });

            InitializeUnitOfWork();
        }

        private void InitializeUnitOfWork()
        {
            // Create a unit of work for managing package data
            if (Editor != null)
            {
                UnitofWork = new UnitofWork<PackageDefinition>(Editor, true, Packages);
            }
        }

        public void SetActiveSessionAndEnvironment(PythonSessionInfo session, PythonVirtualEnvironment environment)
        {
            _currentSession = session ?? throw new ArgumentNullException(nameof(session));
            _currentEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));

            // Refresh packages in this environment
            RefreshAllPackagesAsync();
        }

        #region IPackageManagerViewModel Implementation

        public bool InstallNewPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
               

                var result = InstallNewPackageAsync(packageName);

                if (result)
                {
                    // If installation was successful, refresh package list
                    RefreshPackageAsync(packageName);
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public bool InstallPipToolAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
             

                return InstallPipToolAsync();
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install pip tool: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public bool RefreshAllPackagesAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
                

                bool result = RefreshAllPackagesAsync();
                if (result)
                {
                    // Update our package list from the package manager
                    SynchronizePackages(Packages);
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh packages: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public bool RefreshPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
               

                bool result = RefreshPackageAsync(packageName);
                if (result)
                {
                    // Update the specific package in our list
                    var updatedPackage = Packages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                    if (updatedPackage != null)
                    {
                        var existingPackage = Packages.FirstOrDefault(p =>
                            p.PackageName != null &&
                            p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                        if (existingPackage != null)
                        {
                            // Update properties of existing package
                            existingPackage.Version = updatedPackage.Version;
                            existingPackage.Updateversion = updatedPackage.Updateversion;
                            existingPackage.Status = updatedPackage.Status;
                            existingPackage.Buttondisplay = updatedPackage.Buttondisplay;
                            existingPackage.Description = updatedPackage.Description;
                        }
                        else
                        {
                            // Add new package
                            Packages.Add(updatedPackage);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public bool UnInstallPackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
              

                bool result = UnInstallPackageAsync(packageName);
                if (result)
                {
                    // Remove the package from our list
                    var packageToRemove = Packages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                    if (packageToRemove != null)
                    {
                        Packages.Remove(packageToRemove);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to uninstall package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public bool UpgradeAllPackagesAsync()
        {
            if (_isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
               

                bool result = UpgradeAllPackagesAsync();
                if (result)
                {
                    // Refresh our package list
                    RefreshAllPackagesAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to upgrade all packages: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public bool UpgradePackageAsync(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy || !ValidateSessionAndEnvironment())
                return false;

            _isBusy = true;
            try
            {
               

                bool result = UpgradePackageAsync(packageName);
                if (result)
                {
                    // Refresh the package in our list
                    RefreshPackageAsync(packageName);
                }

                return result;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to upgrade package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        #endregion

        #region Helper Methods

        private bool ValidateSessionAndEnvironment()
        {
            if (_currentSession == null)
            {
                ReportError("No Python session is assigned.");
                return false;
            }

            if (_currentEnvironment == null)
            {
                ReportError("No Python environment is assigned.");
                return false;
            }

            return true;
        }

        private void SynchronizePackages(ObservableBindingList<PackageDefinition> sourcePackages)
        {
            if (sourcePackages == null)
                return;

            // Clear existing packages
            Packages.Clear();

            // Add all packages from source
            foreach (var package in sourcePackages)
            {
                Packages.Add(package);
            }
        }

        private void ReportError(string message)
        {
            Progress?.Report(new PassedArgs
            {
                Messege = message,
                EventType = "Error",
                Flag = Errors.Failed
            });

            // Also log to editor if available
            Editor?.AddLogMessage("Package Manager", message, DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    UnitofWork = null;
                }

                // Clean up unmanaged resources

                _isDisposed = true;
            }
        }

        #endregion
        #region "Package Manager"
        public async Task<string> RunPackageManagerAsync(IProgress<PassedArgs> progress, string packageName, PackageAction packageAction, PythonVirtualEnvironment environment = null, bool useConda = false)
        {
            // Determine the environment to use
            PythonVirtualEnvironment envToUse = environment ?? _currentEnvironment;

            if (envToUse == null)
            {
                ReportError("No environment specified for package operation.");
                return string.Empty;
            }

            // Get the correct paths for this environment
            string environmentPath = envToUse.Path;
            string binPath = Path.Combine(environmentPath, "Scripts"); // Windows
            if (!Directory.Exists(binPath))
            {
                binPath = Path.Combine(environmentPath, "bin"); // Linux/macOS
            }

            string customPath = $"{binPath}".Trim();
            string modifiedFilePath = customPath.Replace("\\", "\\\\");
            string output = "";
            string command = "";

            // Python code for running package commands
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

            // Use the session scope if available
            PyModule scope = null;
            if (_currentSession != null)
            {
                // Get the scope from the PythonRunTimeManager
                scope = _pythonRuntime.GetScope(_currentSession);
            }

            // If no scope is available, use a new temporary scope
            if (scope == null)
            {
                using (Py.GIL())
                {
                    scope = Py.CreateScope();
                }
            }

            using (Py.GIL())
            {
                PyObject globalsDict = scope.GetAttr("__dict__");
                scope.Exec(wrappedPythonCode);

                // Set the custom_path from C# and call set_custom_path function in Python
                PyObject setCustomPathFunc = scope.GetAttr("set_custom_path");
                setCustomPathFunc.Invoke(modifiedFilePath.ToPython());

                PyObject captureOutputFunc = scope.GetAttr("run_pip_and_capture_output");

                // Use conda if the environment is conda-based
                bool useCondaCommand = useConda || envToUse.PythonBinary == PythonBinary.Conda;

                if (useCondaCommand)
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
                    // Determine the python executable name
                    string pythonExe = "python";
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        pythonExe = "python.exe";
                    }

                    switch (packageAction)
                    {
                        case PackageAction.Install:
                            command = $"pip install -U {packageName}";
                            break;
                        case PackageAction.Remove:
                            command = $"pip uninstall -y {packageName}";
                            break;
                        case PackageAction.Update:
                            command = $"pip install --upgrade {packageName}";
                            break;
                        case PackageAction.UpgradePackager:
                            command = $"{pythonExe} -m pip install --upgrade pip";
                            break;
                        case PackageAction.InstallPackager:
                            command = $"{pythonExe} {packageName}";
                            break;
                        default:
                            break;
                    }
                }

                Progress?.Report(new PassedArgs() { Messege = $"Running {command}" });
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
                            progress?.Report(new PassedArgs() { Messege = line });
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

            if (output.Length > 0)
            {
                progress?.Report(new PassedArgs() { Messege = $"Finished {command}" });

                // Update the packages for the environment if needed
                if (envToUse.InstalledPackages != null && packageAction != PackageAction.InstallPackager)
                {
                    // Refresh the package list after installation/removal/update
                    await RefreshPackagesForEnvironmentAsync(envToUse, progress, _cancellationTokenSource.Token);
                }
            }
            else
            {
                progress?.Report(new PassedArgs() { Messege = $"Finished {command} with error" });
            }

            return output;
        }

        private async Task<bool> RefreshPackagesForEnvironmentAsync(PythonVirtualEnvironment environment, IProgress<PassedArgs> progress, CancellationToken token)
        {
            if (environment == null || _isBusy)
                return false;

            _isBusy = true;

            try
            {
                // Make sure environment has a package list
                if (environment.InstalledPackages == null)
                {
                    environment.InstalledPackages = new ObservableBindingList<PackageDefinition>();
                }

                // Use Python to get the installed packages
                using (Py.GIL())
                {
                    // Create a session for this operation if needed
                    PythonSessionInfo session = environment.GetLastSession();
                    if (session == null)
                    {
                        session = new PythonSessionInfo
                        {
                            SessionName = $"PackageRefresh_{DateTime.Now.Ticks}",
                            VirtualEnvironmentId = environment.ID,
                            StartedAt = DateTime.Now
                        };
                        environment.AddSession(session);
                    }

                    // Create a scope for the environment if needed
                    PyModule scope = _pythonRuntime.GetScope(session);
                    if (scope == null)
                    {
                        _pythonRuntime.CreateScope(session, environment);
                        scope = _pythonRuntime.GetScope(session);
                    }

                    if (scope != null)
                    {
                        // Script to get the package list
                        string packageListScript = @"
import importlib.metadata
import json

# Get all installed packages with versions
packages = []
for dist in importlib.metadata.distributions():
    try:
        packages.append({
            'name': dist.metadata['Name'],
            'version': dist.version,
            'summary': dist.metadata.get('Summary', '')
        })
    except Exception as e:
        print(f'Error with package {dist}: {e}')
        
# Convert to JSON string
json_packages = json.dumps(packages)
";

                        scope.Exec(packageListScript);

                        // Get the JSON result
                        scope.Exec("print(json_packages)");

                        // To capture the printed output, we need to use RunPythonCodeAndGetOutput
                        string output = await _pythonRuntime.RunPythonCodeAndGetOutput(
                            progress ?? Progress,
                            "print(json_packages)",
                            session);

                        if (!string.IsNullOrEmpty(output))
                        {
                            // Parse the JSON output
                            List<Dictionary<string, string>> packages =
                                Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(output);

                            if (packages != null)
                            {
                                // Clear current packages
                                environment.InstalledPackages.Clear();

                                // Process each package
                                int count = packages.Count;
                                int i = 0;
                                bool isInternetAvailable = PythonRunTimeDiagnostics.CheckNet();

                                foreach (var pkg in packages)
                                {
                                    i++;
                                    string packageName = pkg["name"];
                                    string packageVersion = pkg["version"];
                                    string summary = pkg.ContainsKey("summary") ? pkg["summary"] : "";

                                    progress?.Report(new PassedArgs
                                    {
                                        Messege = $"Processing {packageName} {packageVersion}",
                                        ParameterInt1 = i,
                                        ParameterInt2 = count
                                    });

                                    // Check online for updates if internet is available
                                    PackageDefinition onlinePkg = null;
                                    if (isInternetAvailable)
                                    {
                                        onlinePkg = await CheckIfPackageExistsAsync(packageName);
                                    }

                                    // Create a new package definition
                                    var packageDef = new PackageDefinition
                                    {
                                        PackageName = packageName,
                                        Version = packageVersion,
                                        Updateversion = onlinePkg?.Version ?? packageVersion,
                                        Status = PackageStatus.Installed,
                                        Description = summary,
                                        Buttondisplay = onlinePkg != null &&
                                                       new Version(onlinePkg.Version) > new Version(packageVersion)
                                                       ? "Update" : "Status"
                                    };

                                    environment.InstalledPackages.Add(packageDef);
                                }

                                // Update our main package list if this is the current environment
                                if (environment == _currentEnvironment)
                                {
                                    SynchronizePackages(environment.InstalledPackages);
                                }

                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to refresh packages for environment {environment.Name}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task<bool> ListpackagesAsync(IProgress<PassedArgs> _progress, CancellationToken token, bool useConda = false, string packagename = null)
        {
            if (_isBusy)
                return false;

            _isBusy = true;

            try
            {
                if (_progress != null)
                {
                    Progress = _progress;
                }

                // Use the current environment's packages if available
                if (_currentEnvironment != null && _currentEnvironment.InstalledPackages != null)
                {
                    // Refresh packages in the current environment
                    var refreshTask = RefreshPackagesForEnvironmentAsync(_currentEnvironment, Progress, token);
                    refreshTask.Wait();

                    // Synchronize with our main package list
                    SynchronizePackages(_currentEnvironment.InstalledPackages);

                    _isBusy = false;
                    return true;
                }
                else
                {
                    // Legacy behavior - fall back to the runtime config's package list
                    int i = 0;
                    bool checkall = true;
                    if (!string.IsNullOrEmpty(packagename))
                    {
                        checkall = false;
                    }

                    using (var gil = _pythonRuntime.GIL())
                    {
                        dynamic pkgResources = Py.Import("importlib.metadata");
                        dynamic workingSet = pkgResources.distributions();

                        // Create a package list if needed
                        if (_currentEnvironment.InstalledPackages == null)
                        {
                            _currentEnvironment.InstalledPackages = new ObservableBindingList<PackageDefinition>();
                        }

                        int count = _currentEnvironment.InstalledPackages.Count;
                        int j = 1;
                        bool isInternetAvailable = PythonRunTimeDiagnostics.CheckNet();

                        foreach (dynamic pkg in workingSet)
                        {
                            i++;
                            string packageName = pkg.metadata["Name"];
                            string packageVersion = pkg.version.ToString();
                            string line = $"Checking Package {packageName}: {packageVersion}";

                            Progress?.Report(new PassedArgs
                            {
                                Messege = line,
                                ParameterInt1 = j,
                                ParameterInt2 = count
                            });

                            PackageDefinition onlinepk = null;
                            if (!string.IsNullOrEmpty(packageVersion))
                            {
                                if (checkall)
                                {
                                    if (isInternetAvailable)
                                    {
                                        onlinepk = await CheckIfPackageExistsAsync(packageName);
                                    }

                                    // Find existing package or create new one
                                    PackageDefinition package = _currentEnvironment.InstalledPackages
                                        .FirstOrDefault(p => p.PackageName != null &&
                                                           p.PackageName.Equals(packageName, StringComparison.InvariantCultureIgnoreCase));

                                    if (package != null)
                                    {
                                        // Update existing package
                                        int idx = _currentEnvironment.InstalledPackages.IndexOf(package);
                                        if (onlinepk != null)
                                        {
                                            package.Updateversion = onlinepk.Version;
                                        }
                                        package.Status = PackageStatus.Installed;
                                        package.Buttondisplay = "Status";

                                        if (onlinepk != null)
                                        {
                                            _currentEnvironment.InstalledPackages[idx].Updateversion = onlinepk.Updateversion;
                                            line = $"Package {packageName}: {packageVersion} found with Version {onlinepk.Updateversion}";
                                        }
                                        else
                                        {
                                            _currentEnvironment.InstalledPackages[idx].Updateversion = "Not Found";
                                            line = $"Package {packageName}: {"Not Found"}";
                                        }

                                        Progress?.Report(new PassedArgs { Messege = line });
                                    }
                                    else
                                    {
                                        // Add new package
                                        PackageDefinition packagelist = new PackageDefinition
                                        {
                                            PackageName = packageName,
                                            Version = packageVersion,
                                            Updateversion = packageVersion,
                                            Status = PackageStatus.Installed,
                                            Buttondisplay = "Added"
                                        };

                                        _currentEnvironment.InstalledPackages.Add(packagelist);
                                        line = $"Added new Package {packagelist}: {packagelist.Version}";
                                        Progress?.Report(new PassedArgs { Messege = line });
                                    }
                                }
                                else if (!string.IsNullOrEmpty(packagename) &&
                                        packageName.Equals(packagename, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // Find matching package
                                    PackageDefinition package = _currentEnvironment.InstalledPackages
                                        .FirstOrDefault(p => p.PackageName != null &&
                                                           p.PackageName.Equals(packageName, StringComparison.InvariantCultureIgnoreCase));

                                    if (package != null)
                                    {
                                        // Update specific package
                                        int idx = _currentEnvironment.InstalledPackages.IndexOf(package);
                                        package.Version = packageVersion;
                                        package.Updateversion = packageVersion;
                                        package.Status = PackageStatus.Installed;
                                        package.Buttondisplay = "Status";

                                        if (isInternetAvailable)
                                        {
                                            onlinepk = await CheckIfPackageExistsAsync(packageName);
                                        }

                                        if (onlinepk != null)
                                        {
                                            _currentEnvironment.InstalledPackages[idx].Updateversion = onlinepk.Updateversion;
                                            package.Updateversion = onlinepk.Version;
                                            line = $"Package {packageName}: {packageVersion} found with Version {onlinepk.Updateversion}";
                                        }
                                        else
                                        {
                                            _currentEnvironment.InstalledPackages[idx].Updateversion = "Not Found";
                                            line = $"Package {packageName}: {"Not Found"}";
                                        }

                                        Progress?.Report(new PassedArgs { Messege = line });
                                    }
                                }
                            }

                            j++;
                        }

                        if (i == 0)
                        {
                            Progress?.Report(new PassedArgs { Messege = "No Packages Found" });
                            _currentEnvironment.InstalledPackages = new ObservableBindingList<PackageDefinition>();
                        }

                        Packages = _currentEnvironment.InstalledPackages;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Error listing packages: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task<PackageDefinition> CheckIfPackageExistsAsync(string packageName)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(
                            $"https://pypi.org/pypi/{packageName}/json",
                            cts.Token).ConfigureAwait(false);

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string jsonResponse = await response.Content.ReadAsStringAsync();
                            dynamic packageData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);

                            PackageDefinition packageInfo = new PackageDefinition
                            {
                                PackageName = packageName,
                                Version = packageData.info.version,
                                Description = packageData.info.description
                            };

                            return packageInfo;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("An error occurred while checking the package. Please try again later.");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"The request to '{packageName}' timed out.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while parsing package data for '{packageName}': {ex.Message}");
                }
            }

            return null;
        }

        // New methods for working with virtual environments
        public async Task<bool> InstallPackageInEnvironmentAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            environment = environment ?? _currentEnvironment;
            if (environment == null)
            {
                ReportError("No environment specified for package installation.");
                return false;
            }

            _isBusy = true;

            try
            {
                await RunPackageManagerAsync(
                    Progress,
                    packageName,
                    PackageAction.Install,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to install package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task<bool> UninstallPackageFromEnvironmentAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            environment = environment ?? _currentEnvironment;
            if (environment == null)
            {
                ReportError("No environment specified for package uninstallation.");
                return false;
            }

            _isBusy = true;

            try
            {
                await RunPackageManagerAsync(
                    Progress,
                    packageName,
                    PackageAction.Remove,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to uninstall package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task<bool> UpgradePackageInEnvironmentAsync(string packageName, PythonVirtualEnvironment environment = null)
        {
            if (string.IsNullOrEmpty(packageName) || _isBusy)
                return false;

            environment = environment ?? _currentEnvironment;
            if (environment == null)
            {
                ReportError("No environment specified for package upgrade.");
                return false;
            }

            _isBusy = true;

            try
            {
                PackageAction action = packageName.Equals("pip", StringComparison.OrdinalIgnoreCase)
                    ? PackageAction.UpgradePackager
                    : PackageAction.Update;

                await RunPackageManagerAsync(
                    Progress,
                    packageName,
                    action,
                    environment,
                    environment.PythonBinary == PythonBinary.Conda);

                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to upgrade package {packageName}: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task<bool> UpgradeAllPackagesInEnvironmentAsync(PythonVirtualEnvironment environment = null)
        {
            if (_isBusy)
                return false;

            environment = environment ?? _currentEnvironment;
            if (environment == null)
            {
                ReportError("No environment specified for upgrading all packages.");
                return false;
            }

            _isBusy = true;

            try
            {
                // First refresh package list to get the latest information
                await RefreshPackagesForEnvironmentAsync(environment, Progress, _cancellationTokenSource.Token);

                // Find packages that need updates
                var packagesToUpdate = environment.InstalledPackages
                    .Where(p => p.Buttondisplay == "Update" ||
                              (p.Updateversion != null && p.Version != null &&
                               p.Updateversion != p.Version))
                    .ToList();

                if (packagesToUpdate.Count == 0)
                {
                    Progress?.Report(new PassedArgs { Messege = "No packages to upgrade." });
                    return true;
                }

                // Upgrade each package
                foreach (var package in packagesToUpdate)
                {
                    Progress?.Report(new PassedArgs { Messege = $"Upgrading {package.PackageName} from {package.Version} to {package.Updateversion}" });

                    await UpgradePackageInEnvironmentAsync(package.PackageName, environment);
                }

                // Final refresh to confirm upgrades
                await RefreshPackagesForEnvironmentAsync(environment, Progress, _cancellationTokenSource.Token);

                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Failed to upgrade all packages: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        // Initialize helper for some methods
        private void Init()
        {
            // This is a stub for compatibility with legacy code
            // No initialization is needed since we're using the environment directly
        }
        #endregion "Package Manager"

    }
}
