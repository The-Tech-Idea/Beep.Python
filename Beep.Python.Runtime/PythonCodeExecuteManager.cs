using Beep.Python.Model;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// Manages Python code execution with optimizations for multiuser environments.
    /// This class separates code execution functionality from the main PythonNetRunTimeManager.
    /// </summary>
    public class PythonCodeExecuteManager : IDisposable, IPythonCodeExecuteManager
    {
        #region Fields
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly IBeepService _beepService;
        private readonly IDMEEditor _dmEditor;
        private volatile bool _shouldStop = false;
        private bool _disposed = false;
        private readonly SemaphoreSlim _executionSemaphore = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, SemaphoreSlim> _sessionSemaphores = new Dictionary<string, SemaphoreSlim>();
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the progress reporter for Python code execution.
        /// </summary>
        public IProgress<PassedArgs> Progress { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token for stopping operations.
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets a value indicating whether any Python code is currently executing.
        /// </summary>
        public bool IsExecuting { get; private set; }


        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PythonCodeExecuteManager"/> class.
        /// </summary>
        /// <param name="pythonRuntime">The runtime manager providing Python environment access.</param>
        /// <param name="beepService">The service providing access to editor and logging functionality.</param>
        public PythonCodeExecuteManager(IPythonRunTimeManager pythonRuntime, IBeepService beepService)
        {
            _pythonRuntime = pythonRuntime ?? throw new ArgumentNullException(nameof(pythonRuntime));
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _dmEditor = beepService.DMEEditor;
            Progress = _dmEditor?.progress;
        }
        #endregion

        #region Code Execution Methods

        /// <summary>
        /// Executes Python code asynchronously with output capturing and error handling.
        /// </summary>
        /// <param name="code">The Python code to execute.</param>
        /// <param name="session">The session in which to execute the code.</param>
        /// <param name="timeoutSeconds">Maximum seconds to allow for execution before cancellation.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <returns>A tuple containing execution success status and captured output.</returns>
        public async Task<(bool Success, string Output)> ExecuteCodeAsync(
            string code,
            PythonSessionInfo session,
            int timeoutSeconds = 120,
            IProgress<PassedArgs> progress = null)
        {
            if (string.IsNullOrEmpty(code))
            {
                ReportProgress("No code provided to execute.", Errors.Warning, progress);
                return (false, "Error: No code provided");
            }

            if (session == null)
            {
                ReportProgress("No session provided for code execution.", Errors.Warning, progress);
                return (false, "Error: No session provided");
            }



            // Get session-specific semaphore to ensure only one execution per session
            var sessionLock = GetSessionSemaphore(session.SessionId);

            try
            {
                // Try to acquire the semaphore with timeout
                if (!await sessionLock.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    ReportProgress($"Session {session.SessionId} is busy with another operation.", Errors.Warning, progress);
                    return (false, "Error: Session is busy with another operation");
                }

                // Mark as executing
                IsExecuting = true;

                // Create a cancellation token source with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token,
                    CancellationToken.Equals(default) ? CancellationToken.None : CancellationToken);

                try
                {
                    // Execute the code with our wrapper
                    var output = await ExecuteWithOutputCaptureAsync(code, session, linkedCts.Token, progress);
                    return (true, output);
                }
                catch (OperationCanceledException)
                {
                    ReportProgress("Code execution was cancelled or timed out.", Errors.Warning, progress);
                    return (false, "Error: Execution cancelled or timed out");
                }
                catch (Exception ex)
                {
                    ReportProgress($"Error executing code: {ex.Message}", Errors.Failed, progress);
                    return (false, $"Error: {ex.Message}");
                }
            }
            finally
            {
                IsExecuting = false;
                sessionLock.Release();
            }
        }

        /// <summary>
        /// Executes Python code and captures its output, handling GIL acquisition properly.
        /// </summary>
        private async Task<string> ExecuteWithOutputCaptureAsync(
            string code,
            PythonSessionInfo session,
            CancellationToken cancellationToken,
            IProgress<PassedArgs> progress = null)
        {
            // The Python wrapper code to capture output
            string wrapperCode = @"
import sys
import io
import threading
import traceback

class OutputCapture(io.StringIO):
    def __init__(self, output_callback, should_stop):
        super().__init__()
        self.output_callback = output_callback
        self.should_stop = should_stop
        self._lock = threading.Lock()
    
    def write(self, text):
        with self._lock:
            # Call the original write
            super().write(text)
            
            # Get all content and clear buffer if we have complete lines
            content = self.getvalue()
            if '\n' in content:
                lines = content.split('\n')
                for line in lines[:-1]:  # Process all complete lines
                    if line.strip():  # Only send non-empty lines
                        self.output_callback(line)
                
                # Keep any trailing content without newline
                if lines[-1]:
                    self.truncate(0)
                    self.seek(0)
                    super().write(lines[-1])
                else:
                    self.truncate(0)
                    self.seek(0)
    
    def flush(self):
        with self._lock:
            content = self.getvalue()
            if content:
                self.output_callback(content)
                self.truncate(0)
                self.seek(0)
        super().flush()

def execute_with_capture(code_to_execute, globals_dict, output_callback, should_stop):
    # Save original stdout/stderr
    original_stdout = sys.stdout
    original_stderr = sys.stderr
    
    # Create capturing stdout/stderr
    capture_out = OutputCapture(output_callback, should_stop)
    
    # Replace stdout/stderr
    sys.stdout = capture_out
    sys.stderr = capture_out
    
    error = None
    result = None
    
    try:
        # Execute the provided code
        exec(code_to_execute, globals_dict)
        
        # If the code defines a 'result' variable, capture it
        if 'result' in globals_dict:
            result = globals_dict['result']
        
    except Exception as e:
        error_msg = traceback.format_exc()
        output_callback(f'Error: {str(e)}')
        output_callback(error_msg)
        error = str(e)
    finally:
        # Ensure we flush any remaining content
        capture_out.flush()
        
        # Restore original stdout/stderr
        sys.stdout = original_stdout
        sys.stderr = original_stderr
    
    return (result, error)
";

            // Check if we should use a specific scope
            PyModule scope = _pythonRuntime.GetScope(session);
            if (scope == null)
            {
                ReportProgress("No Python scope available for this session", Errors.Warning, progress);

                // Ask the runtime to create a scope
                if (!_pythonRuntime.CreateScope(session))
                {
                    throw new InvalidOperationException("Failed to create Python scope for the session");
                }

                scope = _pythonRuntime.GetScope(session);
                if (scope == null)
                {
                    throw new InvalidOperationException("Could not get Python scope after creation");
                }
            }

            // Create a channel for communication
            var outputChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            // StringBuilder to collect output
            var outputBuilder = new StringBuilder();

            // Create tasks for execution and output collection
            var executionTask = Task.Run(() =>
            {
                using (Py.GIL()) // Properly acquire the GIL for this thread
                {
                    try
                    {
                        // Register the output handler
                        Action<string> outputHandler = text => outputChannel.Writer.TryWrite(text);
                        Func<bool> shouldStop = () => _shouldStop || cancellationToken.IsCancellationRequested;

                        scope.Set("output_handler", outputHandler);
                        scope.Set("should_stop", shouldStop);

                        // Execute the wrapper code
                        scope.Exec(wrapperCode);

                        // Get the execute function
                        dynamic executeFunc = scope.GetAttr("execute_with_capture");
                        dynamic globalsDict = new PyDict();

                        // Execute the user code
                        var pyResult = executeFunc.Invoke(code.ToPython(), globalsDict,
                            scope.Get("output_handler"), scope.Get("should_stop"));

                        // Check for execution error
                        if (pyResult[1] != null && !pyResult[1].Equals(null))
                        {
                            string errorMsg = pyResult[1].ToString();
                            outputChannel.Writer.TryWrite($"Execution error: {errorMsg}");
                        }

                        // Return any result
                        return pyResult[0];
                    }
                    catch (PythonException pyEx)
                    {
                        outputChannel.Writer.TryWrite($"Python Error: {pyEx.Message}");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        outputChannel.Writer.TryWrite($"Error: {ex.Message}");
                        return null;
                    }
                }
            }, cancellationToken);

            // Output collection task
            var outputTask = Task.Run(async () =>
            {
                try
                {
                    while (await outputChannel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        if (outputChannel.Reader.TryRead(out var line))
                        {
                            // Report as progress
                            ReportProgress(line, Errors.Ok, progress);

                            // Append to our output collection
                            outputBuilder.AppendLine(line);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation occurs
                }
            }, cancellationToken);

            try
            {
                // Wait for execution to complete
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(120), cancellationToken);
                var completedTask = await Task.WhenAny(executionTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Execution timed out
                    _shouldStop = true;
                    outputChannel.Writer.TryWrite("Execution timed out");
                    ReportProgress("Execution timed out", Errors.Warning, progress);
                }
                else
                {
                    // Wait for the actual result
                    _ = await executionTask;
                }

                // Close the channel once we're done
                outputChannel.Writer.Complete();

                // Wait for output collection to complete
                await outputTask;

                return outputBuilder.ToString();
            }
            finally
            {
                // Ensure the channel is closed - no need to check completion
                try
                {
                    outputChannel.Writer.TryComplete();
                }
                catch (Exception ex)
                {
                    ReportProgress($"Error closing output channel: {ex.Message}", Errors.Warning, progress);
                }
            }
        }

        /// <summary>
        /// Executes a Python script file asynchronously within a session.
        /// </summary>
        /// <param name="filePath">The path to the Python script file.</param>
        /// <param name="session">The session in which to execute the script.</param>
        /// <param name="timeoutSeconds">Maximum seconds to allow for execution before cancellation.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <returns>A tuple containing execution success status and captured output.</returns>
        public async Task<(bool Success, string Output)> ExecuteScriptFileAsync(
            string filePath,
            PythonSessionInfo session,
            int timeoutSeconds = 300,
            IProgress<PassedArgs> progress = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                ReportProgress("No file path provided.", Errors.Warning, progress);
                return (false, "Error: No file path provided");
            }

            try
            {
                // Read the file content
                string code = await System.IO.File.ReadAllTextAsync(filePath);

                // Execute the code
                return await ExecuteCodeAsync(code, session, timeoutSeconds, progress);
            }
            catch (System.IO.FileNotFoundException)
            {
                ReportProgress($"File not found: {filePath}", Errors.Failed, progress);
                return (false, $"Error: File not found: {filePath}");
            }
            catch (System.IO.IOException ex)
            {
                ReportProgress($"IO error: {ex.Message}", Errors.Failed, progress);
                return (false, $"Error: IO error: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a Python command and returns the result.
        /// </summary>
        /// <param name="command">The Python command (single line expression).</param>
        /// <param name="session">The session in which to execute the command.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <returns>The result of the command execution.</returns>
        public async Task<object> ExecuteCommandAsync(
            string command,
            PythonSessionInfo session,
            IProgress<PassedArgs> progress = null)
        {
            if (string.IsNullOrEmpty(command))
            {
                ReportProgress("No command provided.", Errors.Warning, progress);
                return null;
            }

            // Wrap the command to capture its result
            string wrappedCommand = $"result = {command}";

            // Execute the wrapped command
            var (success, output) = await ExecuteCodeAsync(wrappedCommand, session, 30, progress);

            if (!success)
            {
                return null;
            }

            // Return the result from the Python scope
            using (Py.GIL())
            {
                var scope = _pythonRuntime.GetScope(session);
                if (scope == null || !scope.HasAttr("result"))
                {
                    return null;
                }

                // Convert the Python result to a .NET object
                return scope.GetAttr("result").AsManagedObject(typeof(object));
            }
        }

        /// <summary>
        /// Executes Python code with multiple inputs/variables and returns the result.
        /// </summary>
        /// <param name="code">The Python code to execute.</param>
        /// <param name="session">The session in which to execute the code.</param>
        /// <param name="variables">Dictionary of variables to set in the Python scope before execution.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <returns>The result of the code execution.</returns>
        public async Task<object> ExecuteWithVariablesAsync(
            string code,
            PythonSessionInfo session,
            Dictionary<string, object> variables,
            IProgress<PassedArgs> progress = null)
        {
            if (string.IsNullOrEmpty(code))
            {
                ReportProgress("No code provided.", Errors.Warning, progress);
                return null;
            }

            if (variables == null)
            {
                variables = new Dictionary<string, object>();
            }

            // Get session-specific semaphore
            var sessionLock = GetSessionSemaphore(session.SessionId);

            try
            {
                // Try to acquire the semaphore with timeout
                if (!await sessionLock.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    ReportProgress($"Session {session.SessionId} is busy with another operation.", Errors.Warning, progress);
                    return null;
                }

                IsExecuting = true;

                // Get or create the Python scope
                var scope = _pythonRuntime.GetScope(session);
                if (scope == null)
                {
                    if (!_pythonRuntime.CreateScope(session))
                    {
                        ReportProgress("Failed to create Python scope for the session", Errors.Failed, progress);
                        return null;
                    }

                    scope = _pythonRuntime.GetScope(session);
                    if (scope == null)
                    {
                        ReportProgress("Could not get Python scope after creation", Errors.Failed, progress);
                        return null;
                    }
                }

                // Set the variables in the scope
                using (Py.GIL())
                {
                    foreach (var kvp in variables)
                    {
                        scope.Set(kvp.Key, kvp.Value.ToPython());
                    }

                    // Execute the code
                    try
                    {
                        scope.Exec(code);

                        // Check for a result variable
                        if (scope.HasAttr("result"))
                        {
                            return scope.GetAttr("result").AsManagedObject(typeof(object));
                        }

                        return null;
                    }
                    catch (PythonException ex)
                    {
                        ReportProgress($"Python error: {ex.Message}", Errors.Failed, progress);
                        return null;
                    }
                }
            }
            finally
            {
                IsExecuting = false;
                sessionLock.Release();
            }
        }

        /// <summary>
        /// Stops any currently executing Python code.
        /// </summary>
        public void StopExecution()
        {
            _shouldStop = true;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets or creates a semaphore specific to a session ID.
        /// </summary>
        private SemaphoreSlim GetSessionSemaphore(string sessionId)
        {
            lock (_sessionSemaphores)
            {
                if (!_sessionSemaphores.TryGetValue(sessionId, out var semaphore))
                {
                    semaphore = new SemaphoreSlim(1, 1);
                    _sessionSemaphores[sessionId] = semaphore;
                }
                return semaphore;
            }
        }

        /// <summary>
        /// Reports progress using the provided progress reporter or a fallback.
        /// </summary>
        private void ReportProgress(string message, Errors flag = Errors.Ok, IProgress<PassedArgs> customProgress = null)
        {
            var progressToUse = customProgress ?? Progress ?? _dmEditor?.progress;

            if (progressToUse != null)
            {
                progressToUse.Report(new PassedArgs
                {
                    Messege = message,
                    Flag = flag,
                    EventType = "PythonExecution"
                });
            }

            // Also log to the editor if available
            _dmEditor?.AddLogMessage("Python Execution", message, DateTime.Now, -1, null, flag);
        }
        // Add a method for profiled execution
        public async Task<(bool Success, string Output, TimeSpan ExecutionTime)> ExecuteCodeWithProfilingAsync(
            string code,
            PythonSessionInfo session,
            int timeoutSeconds = 120,
            IProgress<PassedArgs> progress = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await ExecuteCodeAsync(code, session, timeoutSeconds, progress);
            stopwatch.Stop();

            ReportProgress($"Execution completed in {stopwatch.ElapsedMilliseconds}ms", Errors.Ok, progress);

            return (result.Success, result.Output, stopwatch.Elapsed);
        }

        /// <summary>
        /// Converts a C# dictionary to a Python dictionary object.
        /// </summary>
        public  PyObject ToPython(IDictionary<string, object> dictionary)
        {
            using (Py.GIL())
            {
                var pyDict = new PyDict();
                foreach (var kvp in dictionary)
                {
                    PyObject key = new PyString(kvp.Key);
                    PyObject value = kvp.Value.ToPython();
                    pyDict.SetItem(key, value);
                    key.Dispose();
                    value.Dispose();
                }
                return pyDict;
            }
        }

        /// <summary>
        /// Converts an arbitrary C# object to a PyObject.
        /// </summary>
        public  PyObject ToPython(object obj)
        {
            using (Py.GIL())
            {
                return PyObject.FromManagedObject(obj);
            }
        }


        /// <summary>
        /// Cleans up session resources when a session is no longer needed.
        /// </summary>
        public void CleanupSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                return;

            lock (_sessionSemaphores)
            {
                if (_sessionSemaphores.TryGetValue(sessionId, out var semaphore))
                {
                    _sessionSemaphores.Remove(sessionId);
                    semaphore.Dispose();
                }
            }
        }
        #endregion

        #region Advanced Execution Methods
        /// <summary>
        /// Executes Python code in a batch, optimizing GIL usage.
        /// </summary>
        /// <param name="commands">List of Python commands to execute as a batch.</param>
        /// <param name="session">The session in which to execute the commands.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <returns>A list of results corresponding to each command.</returns>
        public async Task<List<object>> ExecuteBatchAsync(
            IList<string> commands,
            PythonSessionInfo session,
            IProgress<PassedArgs> progress = null)
        {
            if (commands == null || commands.Count == 0)
            {
                ReportProgress("No commands provided.", Errors.Warning, progress);
                return new List<object>();
            }

            // Get session-specific semaphore
            var sessionLock = GetSessionSemaphore(session.SessionId);

            try
            {
                // Try to acquire the semaphore with timeout
                if (!await sessionLock.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    ReportProgress($"Session {session.SessionId} is busy with another operation.", Errors.Warning, progress);
                    return new List<object>();
                }

                IsExecuting = true;

                // Get or create the Python scope
                var scope = _pythonRuntime.GetScope(session);
                if (scope == null)
                {
                    if (!_pythonRuntime.CreateScope(session))
                    {
                        ReportProgress("Failed to create Python scope for the session", Errors.Failed, progress);
                        return new List<object>();
                    }

                    scope = _pythonRuntime.GetScope(session);
                    if (scope == null)
                    {
                        ReportProgress("Could not get Python scope after creation", Errors.Failed, progress);
                        return new List<object>();
                    }
                }

                // Prepare the list of results
                var results = new List<object>();

                // Combine the commands into a single script with result tracking
                var batchScript = new StringBuilder();
                batchScript.AppendLine("_batch_results = []");

                for (int i = 0; i < commands.Count; i++)
                {
                    batchScript.AppendLine($"try:");
                    batchScript.AppendLine($"    _result_{i} = {commands[i]}");
                    batchScript.AppendLine($"    _batch_results.append(_result_{i})");
                    batchScript.AppendLine($"except Exception as e:");
                    batchScript.AppendLine($"    print(f'Error executing command {i}: {{str(e)}}')");
                    batchScript.AppendLine($"    _batch_results.append(None)");
                }

                // Execute the batch script
                var (success, output) = await ExecuteCodeAsync(batchScript.ToString(), session, 300, progress);

                if (!success)
                {
                    ReportProgress("Failed to execute batch commands", Errors.Failed, progress);
                    return results;
                }

                // Extract the results from the Python scope
                using (Py.GIL())
                {
                    if (scope.HasAttr("_batch_results"))
                    {
                        dynamic batchResults = scope.GetAttr("_batch_results");
                        foreach (var result in batchResults)
                        {
                            results.Add(result?.AsManagedObject(typeof(object)));
                        }
                    }
                }

                return results;
            }
            finally
            {
                IsExecuting = false;
                sessionLock.Release();
            }
        }

        /// <summary>
        /// Executes Python code that returns an iterator or generator, processing items as they are yielded.
        /// </summary>
        /// <param name="generatorCode">Python code that defines a generator function.</param>
        /// <param name="functionName">Name of the generator function to call.</param>
        /// <param name="session">The session in which to execute the generator.</param>
        /// <param name="onItemYielded">Callback to process each yielded item.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <returns>True if execution was successful.</returns>
        public async Task<bool> ExecuteGeneratorAsync(
            string generatorCode,
            string functionName,
            PythonSessionInfo session,
            Action<object> onItemYielded,
            IProgress<PassedArgs> progress = null)
        {
            if (string.IsNullOrEmpty(generatorCode) || string.IsNullOrEmpty(functionName))
            {
                ReportProgress("Generator code or function name not provided.", Errors.Warning, progress);
                return false;
            }

            if (onItemYielded == null)
            {
                ReportProgress("No callback provided for yielded items.", Errors.Warning, progress);
                return false;
            }

            // Get session-specific semaphore
            var sessionLock = GetSessionSemaphore(session.SessionId);

            try
            {
                // Try to acquire the semaphore with timeout
                if (!await sessionLock.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    ReportProgress($"Session {session.SessionId} is busy with another operation.", Errors.Warning, progress);
                    return false;
                }

                IsExecuting = true;

                // Define the generator wrapper code
                string wrapperCode = $@"
# First, define the generator function
{generatorCode}

# Create a Python function that calls our callback for each item
def process_generator(generator_func, callback):
    try:
        for item in generator_func():
            callback(item)
            if should_stop():
                break
        return True
    except Exception as e:
        print(f'Generator error: {{str(e)}}')
        return False
";

                // Get or create the Python scope
                var scope = _pythonRuntime.GetScope(session);
                if (scope == null)
                {
                    if (!_pythonRuntime.CreateScope(session))
                    {
                        ReportProgress("Failed to create Python scope for the session", Errors.Failed, progress);
                        return false;
                    }

                    scope = _pythonRuntime.GetScope(session);
                    if (scope == null)
                    {
                        ReportProgress("Could not get Python scope after creation", Errors.Failed, progress);
                        return false;
                    }
                }

                // Execute the wrapper code
                await ExecuteCodeAsync(wrapperCode, session, 30, progress);

                // Prepare for generator execution
                using (Py.GIL())
                {
                    // Register the item callback
                    Action<PyObject> callback = pyObj =>
                    {
                        object item = pyObj.AsManagedObject(typeof(object));
                        onItemYielded(item);
                    };

                    Func<bool> shouldStop = () => _shouldStop || CancellationToken.IsCancellationRequested;

                    scope.Set("callback", callback);
                    scope.Set("should_stop", shouldStop);

                    // Call the process_generator function
                    dynamic processGeneratorFunc = scope.GetAttr("process_generator");
                    dynamic generatorFunc = scope.GetAttr(functionName);

                    bool result = processGeneratorFunc.Invoke(generatorFunc, scope.Get("callback"));
                    return result;
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Error executing generator: {ex.Message}", Errors.Failed, progress);
                return false;
            }
            finally
            {
                IsExecuting = false;
                sessionLock.Release();
            }
        }

        /// <summary>
        /// Executes Python code in interactive mode, allowing for incremental execution.
        /// </summary>
        /// <param name="sessions">The ordered list of code segments to execute sequentially.</param>
        /// <param name="session">The session in which to execute the code.</param>
        /// <param name="stopOnError">Whether to stop execution on the first error.</param>
        /// <param name="progress">Optional progress reporter.</param>
        /// <returns>A list of results for each code segment.</returns>
        public async Task<List<(bool Success, string Output)>> ExecuteInteractiveAsync(
            IList<string> codeSegments,
            PythonSessionInfo session,
            bool stopOnError = false,
            IProgress<PassedArgs> progress = null)
        {
            if (codeSegments == null || codeSegments.Count == 0)
            {
                ReportProgress("No code segments provided.", Errors.Warning, progress);
                return new List<(bool, string)>();
            }

            var results = new List<(bool Success, string Output)>();

            for (int i = 0; i < codeSegments.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(codeSegments[i]))
                {
                    results.Add((true, string.Empty));
                    continue;
                }

                ReportProgress($"Executing segment {i + 1}/{codeSegments.Count}", Errors.Ok, progress);

                var result = await ExecuteCodeAsync(codeSegments[i], session, 120, progress);
                results.Add(result);

                if (stopOnError && !result.Success)
                {
                    ReportProgress($"Stopping interactive execution due to error in segment {i + 1}", Errors.Warning, progress);
                    break;
                }
            }

            return results;
        }
        #endregion
        #region Old API
        #region IPythonRunTimeManager Method Implementations

        /// <summary>
        /// Runs a Python script within a session's scope and returns the result.
        /// </summary>
        public dynamic RunPythonScriptWithResult(PythonSessionInfo session, string script, Dictionary<string, object> variables)
        {
            // Use the new ExecuteWithVariablesAsync method and wait for the result synchronously
            return ExecuteWithVariablesAsync(script, session, variables).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs Python code asynchronously within a session.
        /// </summary>
        public async Task<IErrorsInfo> RunCode(PythonSessionInfo session, string code, IProgress<PassedArgs> progress, CancellationToken token)
        {
            // Store the token for cancellation
            CancellationToken = token;

            // Execute the code using the new method
            var result = await ExecuteCodeAsync(code, session, 300, progress);

            // Create an ErrorsInfo object to return
            var errorInfo = new ErrorsInfo();

            if (result.Success)
            {
                errorInfo.Flag = Errors.Ok;
                errorInfo.Message = "Code executed successfully";
            }
            else
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Message = result.Output;
            }

            return errorInfo;
        }

        /// <summary>
        /// Runs a Python command asynchronously within a session.
        /// </summary>
        public async Task<dynamic> RunCommand(PythonSessionInfo session, string command, IProgress<PassedArgs> progress, CancellationToken token)
        {
            // Store the token for cancellation
            CancellationToken = token;

            // Execute the command using the new method
            return await ExecuteCommandAsync(command, session, progress);
        }

        /// <summary>
        /// Runs a Python file asynchronously within a session.
        /// </summary>
        public async Task<IErrorsInfo> RunFile(PythonSessionInfo session, string file, IProgress<PassedArgs> progress, CancellationToken token)
        {
            // Store the token for cancellation
            CancellationToken = token;

            // Execute the file using the new method
            var result = await ExecuteScriptFileAsync(file, session, 300, progress);

            // Create an ErrorsInfo object to return
            var errorInfo = new ErrorsInfo();

            if (result.Success)
            {
                errorInfo.Flag = Errors.Ok;
                errorInfo.Message = "File executed successfully";
            }
            else
            {
                errorInfo.Flag = Errors.Failed;
                errorInfo.Message = result.Output;
            }

            return errorInfo;
        }

        /// <summary>
        /// Runs a pip/command-line instruction in the given session/environment.
        /// </summary>
        public async Task<string> RunPythonCommandLineAsync(
            IProgress<PassedArgs> progress,
            string commandString,
            bool useConda,
            PythonSessionInfo session,
            PythonVirtualEnvironment environment)
        {
            // Build a Python script that will execute the command line operation with better Conda support
            string wrappedCommand = $@"
import subprocess
import sys
import os

try:
    # Set up the command
    cmd = '{commandString.Replace("'", "\\'")}'
    use_conda = {useConda.ToString().ToLower()}
    env_path = r'{environment?.Path ?? ""}'
    
    # Get environment variables copy
    env_vars = os.environ.copy()
    
    # Choose the right executable based on environment type
    if use_conda:
        # Try to find conda executable
        conda_exe = 'conda'
        
        # On Windows, look for conda.exe in specific locations
        if sys.platform == 'win32':
            possible_conda_paths = [
                os.path.join(env_path, 'conda.exe'),
                os.path.join(env_path, '..', 'Scripts', 'conda.exe'),
                os.path.join(env_path, '..', 'condabin', 'conda.exe')
            ]
            
            for path in possible_conda_paths:
                if os.path.exists(path):
                    conda_exe = path
                    break
                    
        # Build and execute conda command
        conda_cmd = f'{{conda_exe}} {{cmd}}'
        print(f'Executing: {{conda_cmd}}')
        result = subprocess.run(conda_cmd, shell=True, capture_output=True, text=True, env=env_vars)
    else:
        # For standard Python environments, use the python executable from the environment
        python_exe = sys.executable
        
        # If we have a specific environment path, try to use that Python executable
        if env_path and os.path.exists(env_path):
            if sys.platform == 'win32':
                python_path = os.path.join(env_path, 'python.exe')
            else:
                python_path = os.path.join(env_path, 'bin', 'python')
                
            if os.path.exists(python_path):
                python_exe = python_path
        
        # Build and execute pip command
        pip_cmd = f'{{python_exe}} -m pip {{cmd}}'
        print(f'Executing: {{pip_cmd}}')
        result = subprocess.run(pip_cmd, shell=True, capture_output=True, text=True, env=env_vars)
    
    # Capture output    
    output = result.stdout
    error = result.stderr
    
    if result.returncode != 0:
        output = f'Error (code {{result.returncode}}):\\n{{error}}\\n{{output}}'
    
    # Set a result variable that will be returned
    result = output
except Exception as e:
    result = f'Error executing command: {{str(e)}}'
";

            // Execute the command and return result
            var (success, output) = await ExecuteCodeAsync(wrappedCommand, session, 600, progress);
            return output;
        }

        /// <summary>
        /// Executes Python code for a specific user and returns stdout.
        /// </summary>
        public async Task<string> RunPythonForUserAsync(
            PythonSessionInfo session,
            string environmentName,
            string code,
            IProgress<PassedArgs> progress)
        {
            // Simply delegate to ExecuteCodeAsync
            var result = await ExecuteCodeAsync(code, session, 300, progress);
            return result.Output;
        }

        /// <summary>
        /// Runs Python code and captures output.
        /// </summary>
        public async Task<string> RunPythonCodeAndGetOutput(IProgress<PassedArgs> progress, string code, PythonSessionInfo session = null)
        {
            // Check if session is provided
            if (session == null)
            {
                ReportProgress("No session provided for code execution", Errors.Failed, progress);
                return "Error: No session provided";
            }

            // Execute using our optimized method
            var result = await ExecuteCodeAsync(code, session, 300, progress);
            return result.Output;
        }

        /// <summary>
        /// Runs a Python script within a session's scope.
        /// </summary>
        public bool RunPythonScript(string script, dynamic parameters, PythonSessionInfo session)
        {
            try
            {
                // Convert dynamic parameters to a dictionary
                Dictionary<string, object> variables = new Dictionary<string, object>();

                if (parameters != null)
                {
                    // Attempt to extract properties from dynamic object
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        try
                        {
                            variables[prop.Name] = prop.GetValue(parameters);
                        }
                        catch
                        {
                            // Skip properties that can't be accessed
                        }
                    }
                }

                // Execute synchronously
                ExecuteWithVariablesAsync(script, session, variables).GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex)
            {
                ReportProgress($"Error in RunPythonScript: {ex.Message}", Errors.Failed);
                return false;
            }
        }

        #endregion

        #endregion
        #region IDisposable Implementation
        /// <summary>
        /// Releases unmanaged and managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Stop any running processes
                _shouldStop = true;

                // Dispose semaphores
                _executionSemaphore.Dispose();

                lock (_sessionSemaphores)
                {
                    foreach (var semaphore in _sessionSemaphores.Values)
                    {
                        semaphore.Dispose();
                    }
                    _sessionSemaphores.Clear();
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~PythonCodeExecuteManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
