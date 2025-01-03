using System.Diagnostics;

namespace Beep.Python.Model
{
    public  class PythonRunTime
    {

        public PythonRunTime() { GuidObj = Guid.NewGuid();ID = GuidObj.ToString(); }
        public Guid GuidObj { get; set; }
        public string ID { get; set; }
        public string LastfilePath { get; set; } = string.Empty;
        public string Script { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
        public string RuntimePath { get; set; } = string.Empty;
        public string BinPath { get; set; } = string.Empty;
        public string Packageinstallpath { get; set; } = string.Empty;
       
        public string AiFolderpath { get; set; } = string.Empty;
        public string PythonVersion { get; set; } = string.Empty;
        public BinType32or64 BinType { get; set; } = BinType32or64.Unknown;
        public string PythonDll { get; set; } = string.Empty;   
        public string Message { get; set; }="Python is not Status";
        public bool IsPythonInstalled { get; set; } = false;
        public PackageType PackageType { get; set; } = PackageType.pypi;
        public List<PackageDefinition> Packagelist { get; set; } = new List<PackageDefinition>();

        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(RuntimePath))
            {
                errorMessage = "RuntimePath is not set.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(BinPath))
            {
                errorMessage = "BinPath is not set.";
                return false;
            }

            if (!IsPythonInstalled)
            {
                errorMessage = "Python is not Status or detected.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
        public void ResolvePathsFromEnvironment()
        {
            RuntimePath = Environment.ExpandEnvironmentVariables(RuntimePath);
            BinPath = Environment.ExpandEnvironmentVariables(BinPath);
            Packageinstallpath = Environment.ExpandEnvironmentVariables(Packageinstallpath);
        }
        public bool CheckPythonInstallation()
        {
            string pythonExe = Path.Combine(BinPath, "python.exe");
            if (File.Exists(pythonExe))
            {
                IsPythonInstalled = true;
                PythonVersion = GetPythonVersion(pythonExe);
                return true;
            }
            IsPythonInstalled = false;
            return false;
        }
        private string GetPythonVersion(string pythonExe)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = "--Version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string version = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                return version;
            }
            catch
            {
                return "Unknown";
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

        public void AddPackage(string packageName, PackageStatus status = PackageStatus.NotInstalled)
        {
            if (Packagelist.All(p => p.PackageName != packageName))
            {
                Packagelist.Add(new PackageDefinition { PackageName = packageName,Status = status });
            }
        }

        public void UpdatePackageStatus(string packageName, PackageStatus status)
        {
            var package = Packagelist.FirstOrDefault(p => p.PackageName == packageName);
            if (package != null)
            {
                package.Status = status;
            }
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
