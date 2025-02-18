﻿using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Newtonsoft.Json;
using Python.Runtime;
using System.Collections.Generic;
using TheTechIdea.Beep.Container.Services;
namespace Beep.Python.RuntimeEngine
{
    public class PythonPIPManager: PythonBaseViewModel
    {

        public PythonPIPManager(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager) : base(beepservice, pythonRuntimeManager)
        {

            InitializePythonEnvironment();
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
    pkg_version = pkg_resources.get_distribution('{packageName}').Version

pkg_version
";

            dynamic result = RunPythonScriptWithResult(script, null);

            // Parse and return the package Version
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

            dynamic result = RunPythonScriptWithResult(script, null);

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

            dynamic result = RunPythonScriptWithResult(script, null);

            // Parse and return the package list as a List<PackageDefinition>
            List<PackageDefinition> packageList = new List<PackageDefinition>();

            if (result != null)
            {
                string resultJson = result.ToString();
                List<Dictionary<string, string>> packageDicts = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(resultJson);

                foreach (var packageDict in packageDicts)
                {
                    PackageDefinition package = new PackageDefinition();
                    package.PackageTitle = packageDict["name"];
                    package.PackageName = packageDict["Version"];
                    packageList.Add(package);
                }
            }

            return packageList;
        }
        #endregion "PIP Methods"
     


    }
}
