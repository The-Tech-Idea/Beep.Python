using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;


namespace Beep.Python.Model
{
    public interface IPythonVirtualEnvViewModel:IDisposable
    {
        bool CreateVirtualEnvironment(string envPath);
        bool CreateVirtualEnvironmentFromCommand(string envPath);
        bool InitializeForUser(string envBasePath, string username);
        void InitializePythonEnvironment();
        IErrorsInfo ShutDown();
    }
}