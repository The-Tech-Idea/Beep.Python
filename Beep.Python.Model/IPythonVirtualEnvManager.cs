 
 

namespace Beep.Python.Model
{
    public interface IPythonVirtualEnvManager : IDisposable
    {
        List<PythonVirtualEnvironment> ManagedVirtualEnvironments { get; set; }
        bool IsBusy { get; }
        PythonVirtualEnvironment GetEnvironmentByPath(string path);
        PythonVirtualEnvironment GetEnvironmentById(string id);
        bool AddToManagedEnvironments(PythonVirtualEnvironment env);
        bool RemoveEnvironment(string environmentId);
        void UpdateEnvironmentUsage(string environmentId);
        PythonVirtualEnvironment GetLeastRecentlyUsedEnvironment();
        PythonSessionInfo GetPackageManagementSession(PythonVirtualEnvironment env);
        void PerformEnvironmentCleanup(TimeSpan maxIdleTime);
        bool CreateVirtualEnvironment(PythonRunTime cfg, PythonVirtualEnvironment env);
        bool CreateVirtualEnvironment(PythonRunTime config, string envPath);
        bool CreateEnvForUser(PythonRunTime cfg, PythonSessionInfo sessionInfo);
        bool CreateEnvForUser(PythonRunTime config, string envBasePath, string username);
        PythonSessionInfo CreateEnvironmentForUser(PythonRunTime config, string envBasePath, string username, string envName = null);
        void InitializePythonEnvironment(PythonVirtualEnvironment env);
        void SaveEnvironments(string filePath);
        void LoadEnvironments(string filePath);
        PassedParameters ShutDown(PythonVirtualEnvironment env);

        /// <summary>
        /// Checks if a virtual environment exists in the system for pip or conda.
        /// </summary>
        /// <param name="envName">The name of the environment to check.</param>
        /// <param name="baseRuntime">The base Python runtime to search from.</param>
        /// <param name="binary">The type of environment: Pip or Conda.</param>
        /// <returns>True if the environment exists, otherwise false.</returns>
        bool VirtualEnvironmentExists(string envName, PythonRunTime baseRuntime, PythonBinary binary);
    }
}