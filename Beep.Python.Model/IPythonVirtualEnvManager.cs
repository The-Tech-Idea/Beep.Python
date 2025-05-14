
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;



namespace Beep.Python.Model
{
    public interface IPythonVirtualEnvManager:IDisposable
    {

         ObservableBindingList<PythonVirtualEnvironment> ManagedVirtualEnvironments { get; set; } 

        bool InitializeForUser(PythonRunTime cfg, PythonSessionInfo sessionInfo);
        bool CreateVirtualEnvironmentFromDefinition(PythonRunTime cfg, PythonVirtualEnvironment env);
        bool CreateVirtualEnvironment(PythonRunTime config, string envPath);
        bool CreateVirtualEnvironmentFromCommand(PythonRunTime config, string envPath);
        bool InitializeForUser(PythonRunTime config, string envBasePath, string username);
        void InitializePythonEnvironment(PythonVirtualEnvironment env);
        void SaveEnvironments(string filePath);
        void LoadEnvironments(string filePath);
        IErrorsInfo ShutDown(PythonVirtualEnvironment env);
    }
}