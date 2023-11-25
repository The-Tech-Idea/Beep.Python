using System.Collections.Generic;
using System.Diagnostics;

namespace Beep.Python.Model
{
    public interface IProcessManager
    {
        int numOutputLines { get; set; }
        List<string> outputdata { get; set; }
        Process Process { get; set; }

        void RunPIP(string Command, string Commandpath);
        void runPythonScriptcommandlineSync(string Command, string Commandpath);
        void RunScript(string script);
        void SetupEnvVariables();
    }
}