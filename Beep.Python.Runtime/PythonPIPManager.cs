using Beep.Python.Model;
using Newtonsoft.Json;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine
{
    public class PythonPIPManager:IDisposable
    {
        private readonly PythonNetRunTimeManager _pythonRuntimeManager;
        private PyModule _persistentScope;
        public PythonPIPManager(PythonNetRunTimeManager pythonRuntimeManager)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            InitializePythonEnvironment();
        }
        public PythonPIPManager(PythonNetRunTimeManager pythonRuntimeManager, PyModule persistentScope)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            _persistentScope = persistentScope;
            PythonHelpers._persistentScope = persistentScope;
            PythonHelpers._pythonRuntimeManager = pythonRuntimeManager;
        }
        #region "PIP Methods"
        public void InstallPackage(string packageName)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import pip;pip.main(['install','{packageName}'])";
            RunPythonScript(script, null);
        }
        public void InstallPackage(string packageName, string version)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import pip;pip.main(['install','{packageName}=={version}'])";
            RunPythonScript(script, null);
        }
        public void InstallPackage(string packageName, string version, string path)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import pip;pip.main(['install','{packageName}=={version}','--target={path}'])";
            RunPythonScript(script, null);
        }
        // remove package
        public void RemovePackage(string packageName)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import pip;pip.main(['uninstall','{packageName}'])";
            RunPythonScript(script, null);
        }
        //get package list and convert them to a list of PackageDefinition get script output using result dictionary in python script
       
        public string GetPackageVersion(string packageName)
        {
            if (!IsInitialized)
            {
                return null;
            }

            string script = $@"
import pip
import pkg_resources

pkg_version = None
installed_packages = [pkg.key for pkg in pkg_resources.working_set]

if '{packageName}' in installed_packages:
    pkg_version = pkg_resources.get_distribution('{packageName}').version

pkg_version
";

            dynamic result = PythonHelpers.RunPythonScriptWithResult(script, null);

            // Parse and return the package version
            string packageVersion = result?.ToString();
            return packageVersion;
        }
        public void FreezePackages(string outputPath)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import pip;import pkg_resources;installed_packages = [pkg.key for pkg in pkg_resources.working_set];with open('{outputPath}', 'w') as f: f.write('\\n'.join(installed_packages))";
            RunPythonScript(script, null);
        }
        public List<string> SearchPackages(string query)
        {
            if (!IsInitialized)
            {
                return null;
            }

            string script = $@"
import pip
import json

result = pip.search('{query}')
result_list = [package['name'] for package in result]

json.dumps(result_list)
";

            dynamic result = PythonHelpers.RunPythonScriptWithResult(script, null);

            // Parse and return the search results as a list of package names
            List<string> packageNames = new List<string>();

            if (result != null)
            {
                string resultJson = result.ToString();
                packageNames = JsonConvert.DeserializeObject<List<string>>(resultJson);
            }

            return packageNames;
        }
        public void UpgradePackage(string packageName)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import pip;pip.main(['install', '--upgrade', '{packageName}'])";
            RunPythonScript(script, null);
        }
        public List<PackageDefinition> GetPackageList()
        {
            if (!IsInitialized)
            {
                return null;
            }

            string script = @"
import pip
import json

result = pip.list()
json.dumps(result)
";

            dynamic result = PythonHelpers.RunPythonScriptWithResult(script, null);

            // Parse and return the package list as a List<PackageDefinition>
            List<PackageDefinition> packageList = new List<PackageDefinition>();

            if (result != null)
            {
                string resultJson = result.ToString();
                List<Dictionary<string, string>> packageDicts = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(resultJson);

                foreach (var packageDict in packageDicts)
                {
                    PackageDefinition package = new PackageDefinition();
                    package.packagetitle = packageDict["name"];
                    package.packagename = packageDict["version"];
                    packageList.Add(package);
                }
            }

            return packageList;
        }
        #endregion "PIP Methods"
        public void ImportPythonModule(string moduleName)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import {moduleName}";
            RunPythonScript(script, null);
        }
        public bool IsInitialized => _pythonRuntimeManager.IsInitialized;
        private bool InitializePythonEnvironment()
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)
            {
                _pythonRuntimeManager.Initialize();
            }
            if (!_pythonRuntimeManager.IsInitialized)
            {
                return retval;
            }
            using (Py.GIL())
            {
                _persistentScope = Py.CreateScope("__main__");
                _persistentScope.Exec("models = {}");  // Initialize the models dictionary
                _persistentScope.Exec("import pandas as pd");

                retval = true;
            }
            return retval;
        }
        private void RunPythonScript(string script, dynamic parameters)
        {
            if (!IsInitialized)
            {
                return;
            }
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                _persistentScope.Exec(script); // Execute the script in the persistent scope
                                               // Handle outputs if needed

                // If needed, return results or handle outputs
            }
        }
        public void Dispose()
        {
            _persistentScope.Dispose();
            _pythonRuntimeManager.ShutDown();
        }


    }
}
