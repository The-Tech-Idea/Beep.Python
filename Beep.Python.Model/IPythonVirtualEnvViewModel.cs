using TheTechIdea.Util;

namespace Beep.Python.Model
{
    public interface IPythonVirtualEnvViewModel
    {
        bool CreateVirtualEnvironment(string envPath);
        bool CreateVirtualEnvironmentFromCommand(string envPath);
        bool InitializeForUser(string envBasePath, string username);
        void InitializePythonEnvironment();
        IErrorsInfo ShutDown();
    }
}