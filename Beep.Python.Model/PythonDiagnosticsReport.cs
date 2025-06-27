using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    public class PythonDiagnosticsReport
    {
        public bool PythonFound { get; set; }
        public string PythonPath { get; set; } //. path to python.exe and not the python.exe file itself
        public string PythonVersion { get; set; }
        public string PythonExe { get; set; }
        public bool IsConda { get; set; }
        public bool PipFound { get; set; }
        public List<string> InstalledPackages { get; set; } = new();
        public bool InternetAvailable { get; set; }
        public bool CanExecuteCode { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
     
        public bool IsVenv { get; set; }
        public bool IsBaseInstallation => !IsConda && !IsVenv;
    }

}
