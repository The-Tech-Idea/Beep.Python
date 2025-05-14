using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.Model
{
    public interface IPythonCodeExecuteManager : IDisposable
    {
        // Existing properties
        CancellationToken CancellationToken { get; set; }
        bool IsExecuting { get; }
        IProgress<PassedArgs> Progress { get; set; }
      

        // Utility methods
        PyObject ToPython(object obj);
        PyObject ToPython(IDictionary<string, object> dictionary);
        void CleanupSession(string sessionId);
        void StopExecution();

        // New optimized methods
        Task<List<object>> ExecuteBatchAsync(IList<string> commands, PythonSessionInfo session, IProgress<PassedArgs> progress = null);
        Task<(bool Success, string Output)> ExecuteCodeAsync(string code, PythonSessionInfo session, int timeoutSeconds = 120, IProgress<PassedArgs> progress = null);
        Task<(bool Success, string Output, TimeSpan ExecutionTime)> ExecuteCodeWithProfilingAsync(string code, PythonSessionInfo session, int timeoutSeconds = 120, IProgress<PassedArgs> progress = null);
        Task<object> ExecuteCommandAsync(string command, PythonSessionInfo session, IProgress<PassedArgs> progress = null);
        Task<bool> ExecuteGeneratorAsync(string generatorCode, string functionName, PythonSessionInfo session, Action<object> onItemYielded, IProgress<PassedArgs> progress = null);
        Task<List<(bool Success, string Output)>> ExecuteInteractiveAsync(IList<string> codeSegments, PythonSessionInfo session, bool stopOnError = false, IProgress<PassedArgs> progress = null);
        Task<(bool Success, string Output)> ExecuteScriptFileAsync(string filePath, PythonSessionInfo session, int timeoutSeconds = 300, IProgress<PassedArgs> progress = null);
        Task<object> ExecuteWithVariablesAsync(string code, PythonSessionInfo session, Dictionary<string, object> variables, IProgress<PassedArgs> progress = null);

        // Methods to match IPythonRunTimeManager signatures
        dynamic RunPythonScriptWithResult(PythonSessionInfo session, string script, Dictionary<string, object> variables);
        Task<IErrorsInfo> RunCode(PythonSessionInfo session, string code, IProgress<PassedArgs> progress, CancellationToken token);
        Task<dynamic> RunCommand(PythonSessionInfo session, string command, IProgress<PassedArgs> progress, CancellationToken token);
        Task<IErrorsInfo> RunFile(PythonSessionInfo session, string file, IProgress<PassedArgs> progress, CancellationToken token);
        Task<string> RunPythonCommandLineAsync(IProgress<PassedArgs> progress, string commandString, bool useConda, PythonSessionInfo session, PythonVirtualEnvironment environment);
        Task<string> RunPythonForUserAsync(PythonSessionInfo session, string environmentName, string code, IProgress<PassedArgs> progress);
        Task<string> RunPythonCodeAndGetOutput(IProgress<PassedArgs> progress, string code, PythonSessionInfo session = null);
        bool RunPythonScript(string script, dynamic parameters, PythonSessionInfo session);
    }
}
