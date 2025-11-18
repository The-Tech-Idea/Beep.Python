using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

 
 

namespace Beep.Python.Model
{
    public interface IPythonCodeExecuteManager : IDisposable
    {
        // Existing properties
        CancellationToken CancellationToken { get; set; }
        bool IsExecuting { get; }
        IProgress<PassedParameters> Progress { get; set; }
      

        // Utility methods
        PyObject ToPython(object obj);
        PyObject ToPython(IDictionary<string, object> dictionary);
        void CleanupSession(string sessionId);
        void StopExecution();

        // New optimized methods
        Task<List<object>> ExecuteBatchAsync(IList<string> commands, PythonSessionInfo session, IProgress<PassedParameters> progress = null);
        Task<(bool Success, string Output)> ExecuteCodeAsync(string code, PythonSessionInfo session, int timeoutSeconds = 120, IProgress<PassedParameters> progress = null);
        Task<(bool Success, string Output, TimeSpan ExecutionTime)> ExecuteCodeWithProfilingAsync(string code, PythonSessionInfo session, int timeoutSeconds = 120, IProgress<PassedParameters> progress = null);
        Task<object> ExecuteCommandAsync(string command, PythonSessionInfo session, IProgress<PassedParameters> progress = null);
        Task<bool> ExecuteGeneratorAsync(string generatorCode, string functionName, PythonSessionInfo session, Action<object> onItemYielded, IProgress<PassedParameters> progress = null);
        Task<List<(bool Success, string Output)>> ExecuteInteractiveAsync(IList<string> codeSegments, PythonSessionInfo session, bool stopOnError = false, IProgress<PassedParameters> progress = null);
        Task<(bool Success, string Output)> ExecuteScriptFileAsync(string filePath, PythonSessionInfo session, int timeoutSeconds = 300, IProgress<PassedParameters> progress = null);
        Task<object> ExecuteWithVariablesAsync(string code, PythonSessionInfo session, Dictionary<string, object> variables, IProgress<PassedParameters> progress = null);

        // Methods to match IPythonRunTimeManager signatures
        dynamic RunPythonScriptWithResult(PythonSessionInfo session, string script, Dictionary<string, object> variables);
        Task<PassedParameters> RunCode(PythonSessionInfo session, string code, IProgress<PassedParameters> progress, CancellationToken token);
        Task<dynamic> RunCommand(PythonSessionInfo session, string command, IProgress<PassedParameters> progress, CancellationToken token);
        Task<PassedParameters> RunFile(PythonSessionInfo session, string file, IProgress<PassedParameters> progress, CancellationToken token);
        Task<string> RunPythonCommandLineAsync(IProgress<PassedParameters> progress, string commandString, bool useConda, PythonSessionInfo session, PythonVirtualEnvironment environment);
        Task<string> RunPythonForUserAsync(PythonSessionInfo session, string environmentName, string code, IProgress<PassedParameters> progress);
        Task<string> RunPythonCodeAndGetOutput(IProgress<PassedParameters> progress, string code, PythonSessionInfo session = null);
        bool RunPythonScript(string script, dynamic parameters, PythonSessionInfo session);
    }
}
