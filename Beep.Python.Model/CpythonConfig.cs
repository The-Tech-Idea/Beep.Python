
using System.Collections.ObjectModel;

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
        public string Message { get; set; }="Python is not installed";
        public bool IsPythonInstalled { get; set; } = false;
        public PackageType PackageType { get; set; } = PackageType.pypi;
        public List<PackageDefinition> Packagelist { get; set; } = new List<PackageDefinition>();



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
