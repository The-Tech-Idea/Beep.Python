using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
   public enum PythonBinary
    {
        Python,
        Pip,
        Conda
    }
   public enum PythonEnvironmentType
    {
        
        VirtualEnv,

        Standalone
    }
    public class PythonVirtualEnvironment:Entity
    {
        private string _id;
        public string ID
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_id))
                {
                    _id = Guid.NewGuid().ToString();
                }
                return _id;
            }
            set
            {
                _id = value;
                SetProperty(ref _id, value);
            }
        }
        private string _name;
        public string Name
        {
            get
            {
                
                return _name;
            }
            set
            {
                _name = value;
                SetProperty(ref _name, value);
            }
        }
        private string _description;
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                SetProperty(ref _description, value);
            }
        }
     
        private string _path;
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
                SetProperty(ref _path, value);
            }
        }
        private string _baseInterpreterPath;
        public string BaseInterpreterPath
        {
            get
            {
                return _baseInterpreterPath;
            }
            set
            {
                _baseInterpreterPath = value;
                SetProperty(ref _baseInterpreterPath, value);
            }
        }
        private string _pythonversion;
        public string PythonVersion
        {
            get
            {
                return _pythonversion;
            }
            set
            {
                _pythonversion = value;
                SetProperty(ref _pythonversion, value);
            }

        }
        private PythonBinary _pythonBinary = PythonBinary.Python;
        public PythonBinary PythonBinary
        {
            get
            {
                return _pythonBinary;
            }
            set
            {
                _pythonBinary = value;
                SetProperty(ref _pythonBinary, value);
            }
        }
        private PythonEnvironmentType _environmentType = PythonEnvironmentType.VirtualEnv;
        public PythonEnvironmentType EnvironmentType
        {
            get
            {
                return _environmentType;
            }
            set
            {
                _environmentType = value;
                SetProperty(ref _environmentType, value);
            }
        }
        private DateTime _createdon = DateTime.Now;
        public DateTime CreatedOn {
            get
            {
                return _createdon;
            }
            set
            {
                _createdon = value;
                SetProperty(ref _createdon, value);
            }
        }
        private ObservableBindingList<PackageDefinition> _installedpackages = new ObservableBindingList<PackageDefinition>();
        public ObservableBindingList<PackageDefinition> InstalledPackages {

            get
            {
                return _installedpackages;
            }
            set
            {
                _installedpackages = value;
                SetProperty(ref _installedpackages, value);
            }
        }

        // === New: Session Tracking ===
        private ObservableBindingList<PythonSessionInfo> _sessions = new();
        public ObservableBindingList<PythonSessionInfo> Sessions
        {
            get
            {
                return _sessions;
            }
            set
            {
                _sessions = value;
                SetProperty(ref _sessions, value);
            }
        }

        public void AddSession(PythonSessionInfo session)
        {
            if (session != null)
            {
                Sessions.Add(session);
            }
        }

        public PythonSessionInfo GetLastSession()
        {
            return Sessions.LastOrDefault();
        }

        public bool IsValid()
        {
            return System.IO.Directory.Exists(Path) &&
                   System.IO.File.Exists(System.IO.Path.Combine(Path, "Scripts", "python.exe"));
        }

        public override string ToString()
        {
            return $"{Name} (v{PythonVersion}) @ {Path}" +
                   (string.IsNullOrWhiteSpace(BaseInterpreterPath) ? " [Standalone]" : $" [From: {BaseInterpreterPath}]");
        }
    }

}
