using Python.Runtime;
using System.Diagnostics;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    public  class PythonRunTime : Entity
    {

        public PythonRunTime() { GuidObj = Guid.NewGuid();ID = GuidObj.ToString(); }

        private Guid guid = Guid.NewGuid();
        public Guid GuidObj
        {
            get { return guid; }
            set
            {
                guid = value;
                SetProperty(ref guid, value);
            }
        }
        private string _id;
        public string ID
        {
            get { return _id; }
            set
            {
                _id = value;
                SetProperty(ref _id, value);
            }
        }
        private string _lastfilePath = string.Empty;
        public string LastfilePath
        {
            get { return _lastfilePath; }
            set
            {
                _lastfilePath = value;
                SetProperty(ref _lastfilePath, value);
            }
        }
        private string _script = string.Empty;
        public string Script
        {
            get { return _script; }
            set
            {
                _script = value;
                SetProperty(ref _script, value);
            }
        }
        private string _scriptPath = string.Empty;  
        public string ScriptPath
        {
            get { return _scriptPath; }
            set
            {
                _scriptPath = value;
                SetProperty(ref _scriptPath, value);
            }
        }
        private string _runtimePath = string.Empty;
        public string RuntimePath
        {
            get { return _runtimePath; }
            set
            {
                _runtimePath = value;
                SetProperty(ref _runtimePath, value);
            }
        }
        private string _binpath = string.Empty;
        public string BinPath
        {
            get { return _binpath; }
            set
            {
                _binpath = value;
                SetProperty(ref _binpath, value);
            }
        }
        private string _packageinstallpath = string.Empty;
        public string Packageinstallpath
        {
            get { return _packageinstallpath; }
            set
            {
                _packageinstallpath = value;
                SetProperty(ref _packageinstallpath, value);
            }
        }
       private string _aifolderpath = string.Empty;
        public string AiFolderpath
        {
            get { return _aifolderpath; }
            set
            {
                _aifolderpath = value;
                SetProperty(ref _aifolderpath, value);
            }
        }
        private string _pythonversion = string.Empty;
        public string PythonVersion
        {
            get { return _pythonversion; }
            set
            {
                _pythonversion = value;
                SetProperty(ref _pythonversion, value);
            }
        }
        private BinType32or64 binType = BinType32or64.Unknown;
        public BinType32or64 BinType
        {
            get { return binType; }
            set
            {
                binType = value;
                SetProperty(ref binType, value);
            }
        }
        private string _pythondll = string.Empty;
        public string PythonDll
        {
            get { return _pythondll; }
            set
            {
                _pythondll = value;
                SetProperty(ref _pythondll, value);
            }
        } 
        private string _message = "Python is not Status";
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                SetProperty(ref _message, value);
            }
        }
        private bool _ispythoninstalled = false;
        public bool IsPythonInstalled
        {
            get { return _ispythoninstalled; }
            set
            {
                _ispythoninstalled = value;
                SetProperty(ref _ispythoninstalled, value);
            }
        }
        private PackageType _packageType = PackageType.pypi;
        public PackageType PackageType
        {
            get { return _packageType; }
            set
            {
                _packageType = value;
                SetProperty(ref _packageType, value);
            }
        }
        private ObservableBindingList<PackageDefinition> packageDefinitions = new();
        public ObservableBindingList<PackageDefinition> Packagelist
        {
            get { return packageDefinitions; }
            set
            {
                packageDefinitions = value;
                SetProperty(ref packageDefinitions, value);
            }
        }
        private PythonBinary pythonBinary = PythonBinary.Python;
        public PythonBinary Binary
        {
            get { return pythonBinary; }
            set
            {
                pythonBinary = value;
                SetProperty(ref pythonBinary, value);
            }
        }
        public string GetSummary()
        {
            return $@"
        ID: {ID}
        Runtime Path: {RuntimePath}
        Bin Path: {BinPath}
        Python Version: {PythonVersion}
        Python Status: {IsPythonInstalled}
        Package Install Path: {Packageinstallpath}
        AI Folder Path: {AiFolderpath}
        Message: {Message}
        ";
        }
    }
    public enum BinType32or64
    {
        p395x32,
        p395x64,
        Unknown 

    }
    public enum PackageType
    {
        pypi,
        conda,
        None
    }
    public enum PackageStatus
    {
        Installed,
        NotInstalled,
        UpdateAvailable,
        UpdateNotAvailable
    }
    public enum PackageAction
    {
        Install,
        Remove,
        Update,
        UpgradePackager,
        UpgradeAll,
        InstallPackager

    }
}

//public bool Validate(out string errorMessage)
//{
//    if (string.IsNullOrWhiteSpace(RuntimePath))
//    {
//        errorMessage = "RuntimePath is not set.";
//        return false;
//    }

//    if (string.IsNullOrWhiteSpace(BinPath))
//    {
//        errorMessage = "BinPath is not set.";
//        return false;
//    }

//    if (!IsPythonInstalled)
//    {
//        errorMessage = "Python is not Status or detected.";
//        return false;
//    }

//    errorMessage = string.Empty;
//    return true;
//}
//public void ResolvePathsFromEnvironment()
//{
//    RuntimePath = Environment.ExpandEnvironmentVariables(RuntimePath);
//    BinPath = Environment.ExpandEnvironmentVariables(BinPath);
//    Packageinstallpath = Environment.ExpandEnvironmentVariables(Packageinstallpath);
//}
//public bool CheckPythonInstallation()
//{
//    string pythonExe = Path.Combine(BinPath, "python.exe");
//    if (File.Exists(pythonExe))
//    {
//        IsPythonInstalled = true;
//        PythonVersion = GetPythonVersion(pythonExe);
//        return true;
//    }
//    IsPythonInstalled = false;
//    return false;
//}
//private string GetPythonVersion(string pythonExe)
//{
//    try
//    {
//        var process = new Process
//        {
//            StartInfo = new ProcessStartInfo
//            {
//                FileName = pythonExe,
//                Arguments = "--Version",
//                RedirectStandardOutput = true,
//                UseShellExecute = false,
//                CreateNoWindow = true
//            }
//        };

//        process.Start();
//        string version = process.StandardOutput.ReadToEnd().Trim();
//        process.WaitForExit();
//        return version;
//    }
//    catch
//    {
//        return "Unknown";
//    }
//}


//public void AddPackage(string packageName, PackageStatus status = PackageStatus.NotInstalled)
//{
//    if (Packagelist.All(p => p.PackageName != packageName))
//    {
//        Packagelist.Add(new PackageDefinition { PackageName = packageName, Status = status });
//    }
//}

//public void UpdatePackageStatus(string packageName, PackageStatus status)
//{
//    var package = Packagelist.FirstOrDefault(p => p.PackageName == packageName);
//    if (package != null)
//    {
//        package.Status = status;
//    }
//}