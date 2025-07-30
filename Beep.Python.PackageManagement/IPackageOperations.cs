using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Beep.Python.Model;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Interface for core package operations that can be used by other managers
    /// </summary>
    public interface IPackageOperations
    {
        Task<bool> InstallPackageAsync(string packageName, PythonVirtualEnvironment environment);
        Task<bool> UninstallPackageAsync(string packageName, PythonVirtualEnvironment environment);
        Task<bool> UpgradePackageAsync(string packageName, PythonVirtualEnvironment environment);
        Task<PackageDefinition> GetPackageInfoAsync(string packageName, PythonVirtualEnvironment environment);
        Task<List<PackageDefinition>> GetAllPackagesAsync(PythonVirtualEnvironment environment);
        Task<PackageDefinition> CheckIfPackageExistsAsync(string packageName);
        Task<string> RunPackageCommandAsync(string command, PackageAction action, PythonVirtualEnvironment environment, bool useConda = false);
    }
}