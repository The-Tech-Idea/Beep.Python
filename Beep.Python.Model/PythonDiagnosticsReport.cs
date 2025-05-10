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
        public string PythonPath { get; set; }
        public string PythonVersion { get; set; }
        public bool PipFound { get; set; }
        public List<string> InstalledPackages { get; set; } = new();
        public bool InternetAvailable { get; set; }
        public bool CanExecuteCode { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

}
