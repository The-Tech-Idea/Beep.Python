using Beep.Python.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine
{
    public class PythonEnvironmentManager
    {
        public List<PythonRunTime> InstalledRuntimes { get; set; } = new();
        public List<PythonVirtualEnvironment> VirtualEnvironments { get; set; } = new();
        public string LastUsedEnvironmentId { get; set; }

        public void AddRuntime(PythonRunTime runtime)
        {
            if (!InstalledRuntimes.Any(r => r.ID == runtime.ID))
            {
                InstalledRuntimes.Add(runtime);
            }
        }

        public void AddVirtualEnvironment(PythonVirtualEnvironment venv)
        {
            if (!VirtualEnvironments.Any(v => v.ID == venv.ID))
            {
                VirtualEnvironments.Add(venv);
                LastUsedEnvironmentId = venv.ID;
            }
        }

        public PythonVirtualEnvironment GetVirtualEnvironmentById(string id)
            => VirtualEnvironments.FirstOrDefault(v => v.ID == id);

        public PythonRunTime GetRuntimeById(string id)
            => InstalledRuntimes.FirstOrDefault(r => r.ID == id);

        public IEnumerable<PythonVirtualEnvironment> GetVirtualEnvironmentsByBase(string basePath)
            => VirtualEnvironments.Where(v => string.Equals(v.BaseInterpreterPath, basePath, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<PythonVirtualEnvironment> GetUnlinkedVirtualEnvironments()
            => VirtualEnvironments.Where(v => string.IsNullOrWhiteSpace(v.BaseInterpreterPath));

        public PythonVirtualEnvironment GetLastUsedEnvironment()
            => VirtualEnvironments.FirstOrDefault(v => v.ID == LastUsedEnvironmentId);

        public void SetLastUsed(string id)
        {
            if (VirtualEnvironments.Any(v => v.ID == id))
                LastUsedEnvironmentId = id;
        }

        public string GetSummary()
        {
            var lines = new List<string>
            {
                "Installed Runtimes:",
                string.Join("\n", InstalledRuntimes.Select(r => r.GetSummary())),
                "Virtual Environments:",
                string.Join("\n", VirtualEnvironments.Select(v => v.ToString()))
            };
            return string.Join("\n---\n", lines);
        }

        public void SaveToFile(string filePath)
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public static PythonEnvironmentManager LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return new PythonEnvironmentManager();
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<PythonEnvironmentManager>(json);
        }
    

    }

}
