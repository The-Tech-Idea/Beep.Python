
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;



namespace Beep.Python.Model
{
    public interface IPythonVirtualEnvManager:IDisposable
    {

         ObservableBindingList<PythonVirtualEnvironment> ManagedVirtualEnvironments { get; set; }
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
        IErrorsInfo ShutDown(PythonVirtualEnvironment env);
    }
}