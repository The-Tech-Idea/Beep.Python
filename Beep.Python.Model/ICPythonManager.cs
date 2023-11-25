using System;
using TheTechIdea.Beep;
using TheTechIdea.Util;

namespace Beep.Python.Model
{
    public interface ICPythonManager
    {
        event EventHandler<string> SendMessege;
        IDMEEditor DMEEditor { get; set; }
        IPIPManager PIPManager { get; set; }
        IFileManager FileManager { get; set; }
        IProcessManager ProcessManager { get; set; }
        IErrorsInfo SetRuntimePath(string runtimepath);
        PythonRunTime Config { get; set; }
        string LastfilePath { get; set; }
        string RuntimePath { get; set; }
        string BinPath { get;set; }
        string Packageinstallpath { get; set; }
        string AiFolderpath { get; set; }
        string ScriptPath { get; set; }
       string Script { get; set; }
        void NewMessege(string messege);
    }
}