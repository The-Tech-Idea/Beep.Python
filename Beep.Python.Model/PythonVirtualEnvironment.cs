using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    public class PythonVirtualEnvironment
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Path { get; set; }
        public string BaseInterpreterPath { get; set; } = null;
        public string PythonVersion { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public List<PackageDefinition> InstalledPackages { get; set; } = new();

        // === New: Session Tracking ===
        public List<PythonSessionInfo> Sessions { get; set; } = new();

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
