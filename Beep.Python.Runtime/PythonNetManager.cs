using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine
{
    /// <summary>
    /// A Python .NET manager that provides methods for running Python code, scripts, and interactive sessions.
    /// </summary>
    public class PythonNetManager : PythonBaseViewModel
    {
        // An event that consumers can subscribe to for error notifications
        public event EventHandler<PythonErrorEventArgs> OnError;
        private CancellationTokenSource _cts;
        private volatile bool _shouldStop = false;

        /// <summary>
        /// Initializes a new instance of <see cref="PythonNetManager"/>.
        /// </summary>
        /// <param name="beepservice">An <see cref="IBeepService"/> instance for service resolution and logging.</param>
        /// <param name="pythonRuntimeManager">The <see cref="IPythonRunTimeManager"/> that manages the Python runtime environment.</param>
        public PythonNetManager(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager)
            : base(beepservice, pythonRuntimeManager)
        {
            //  pythonRuntimeManager = pythonRuntimeManager;
            InitializePythonEnvironment(); // Presumably from the base class
        }

        /// <summary>
        /// Runs Python code (string) and captures both text output and a potential image object.
        /// </summary>
        /// <param name="runTimeManager">Reference to the active <see cref="IPythonRunTimeManager"/>.</param>
        /// <param name="progress">A progress reporter for real-time messages.</param>
        /// <param name="code">The Python code to execute.</param>
        /// <returns>A string representing captured textual output.</returns>
        public string RunPythonCodeAndGetOutput(
        string code,
        bool detectImages, // Required parameter must come before optional parameters
        out bool isImage, // Out parameter
        IProgress<PassedArgs> progress = null // Optional parameter
                                              )
        {
            string wrappedPythonCode = @"
import sys
import io
import clr

class CustomStringIO(io.StringIO):
    def write(self, s):
        super().write(s)
        output = self.getvalue()
        if output.strip():
            OutputHandler(output.strip())
            self.truncate(0)  # Clear the internal buffer
            self.seek(0)  # Reset the buffer pointer

def is_image(obj):
    try:
        from PIL import Image
        if isinstance(obj, Image.Image):
            return True
    except ImportError:
        pass

    try:
        import matplotlib.pyplot as plt
        if isinstance(obj, plt.Figure):
            return True
    except ImportError:
        pass

    return False

def capture_output(code, globals_dict, detect_images):
    original_stdout = sys.stdout
    sys.stdout = CustomStringIO()

    output = None
    is_img = False
    try:
        output = exec(code, dict(globals_dict))
        if detect_images:
            is_img = is_image(output)
    finally:
        sys.stdout = original_stdout

    return output, is_img
";

            string output = "";
            isImage = false;

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    Action<string> OutputHandler = line =>
                    {
                        progress?.Report(new PassedArgs() { Messege = line });
                        Console.WriteLine(line);
                    };
                    scope.Set(nameof(OutputHandler), OutputHandler);

                    scope.Exec(wrappedPythonCode);
                    PyObject captureOutputFunc = scope.GetAttr("capture_output");

                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();
                    PyObject pyCode = code.ToPython();
                    PyObject pyGlobalsDict = globalsDict.ToPython();
                    PyObject pyDetectImages = detectImages.ToPython();

                    PyTuple resultTuple = captureOutputFunc.Invoke(pyCode, pyGlobalsDict, pyDetectImages).As<PyTuple>();
                    output = resultTuple[0].As<string>();
                    isImage = resultTuple[1].As<bool>();
                }
            }

            return output;
        }

        #region "Interactive Python"
        /// <summary>
        /// Runs an interactive Python session in the console, line by line, until 'exit()' is typed. 
        /// </summary>
        /// <param name="runTimeManager">Reference to the active <see cref="IPythonRunTimeManager"/>.</param>
        public void RunInteractivePython(IPythonRunTimeManager runTimeManager)
        {
            string wrappedPythonCode = $@"
from io import StringIO
import sys

def capture_output_line(code, globals_dict):
    original_stdout = sys.stdout
    sys.stdout = StringIO()
    output = None

    try:
        exec(code, globals_dict)
        output = sys.stdout.getvalue()
    finally:
        sys.stdout = original_stdout

    return output.strip()
";

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    scope.Exec(wrappedPythonCode);
                    PyObject captureOutputFunc = scope.GetAttr("capture_output_line");
                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                    Console.WriteLine("Python interactive mode. Type 'exit()' to quit.");
                    while (true)
                    {
                        Console.Write(">>> ");
                        string inputLine = Console.ReadLine();

                        if (inputLine.ToLower().Trim() == "exit()")
                        {
                            break;
                        }

                        PyObject pyInputLine = new PyString(inputLine);
                        PyObject pyGlobalsDict = globalsDict.ToPython();
                        PyObject pyOutput = captureOutputFunc.Invoke(pyInputLine, pyGlobalsDict);
                        string output = pyOutput.As<string>();

                        if (!string.IsNullOrEmpty(output))
                        {
                            Console.WriteLine(output);
                        }
                    }
                }
            }

            runTimeManager.IsBusy = false;
        }

        /// <summary>
        /// Runs an interactive Python session for a given block of code, line by line, reporting to <paramref name="progress"/> as needed.
        /// </summary>
        /// <param name="runTimeManager">Reference to the active <see cref="IPythonRunTimeManager"/>.</param>
        /// <param name="progress">Progress reporter for output lines.</param>
        /// <param name="code">A block of Python code, possibly multiline, to execute interactively.</param>
        public void RunInteractivePython(
            IPythonRunTimeManager runTimeManager,
            IProgress<PassedArgs> progress,
            string code)
        {
            string wrappedPythonCode = $@"
from io import StringIO
import sys

def capture_output_line(code, globals_dict):
    original_stdout = sys.stdout
    sys.stdout = StringIO()
    output = None

    try:
        exec(code, dict(globals_dict))
        output = sys.stdout.getvalue()
    finally:
        sys.stdout = original_stdout

    return output.strip()
";

            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    scope.Exec(wrappedPythonCode);
                    PyObject captureOutputFunc = scope.GetAttr("capture_output_line");
                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                    Console.WriteLine("Python interactive mode. Type 'exit()' to quit.");
                    StringBuilder codeBlock = new StringBuilder();
                    int currentIndentLevel = 0;
                    bool inBlock = false;

                    // Split code by lines
                    string[] lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string inputLine in lines)
                    {
                        Console.Write(codeBlock.Length == 0 ? ">>> " : "... ");

                        if (inputLine.ToLower().Trim() == "exit()")
                        {
                            break;
                        }

                        codeBlock.AppendLine(inputLine);

                        int newIndentLevel = inputLine.TakeWhile(c => char.IsWhiteSpace(c)).Count();
                        bool isDedent = codeBlock.Length > 0 && newIndentLevel < currentIndentLevel;
                        bool isEmptyLine = string.IsNullOrWhiteSpace(inputLine);

                        if (!inBlock && !isEmptyLine)
                        {
                            inBlock = true;
                        }

                        if (inBlock && (isDedent || isEmptyLine))
                        {
                            inBlock = false;

                            PyObject pyCodeBlock = codeBlock.ToString().ToPython();
                            PyObject pyGlobalsDict = globalsDict.ToPython();
                            PyObject pyOutput = captureOutputFunc.Invoke(pyCodeBlock, pyGlobalsDict);
                            string output = pyOutput.As<string>();

                            if (!string.IsNullOrEmpty(output))
                            {
                                Console.WriteLine(output);
                            }

                            codeBlock.Clear();
                        }

                        currentIndentLevel = newIndentLevel;
                    }
                }
            }

            runTimeManager.IsBusy = false;
        }

        public void AdvancedMultilineInteractiveSession()
        {
            using (Py.GIL())
            {
                using (PyModule scope = Py.CreateScope())
                {
                    Console.WriteLine("Advanced Python-like REPL. Type 'exit()' to quit.\n");

                    var codeBlock = new List<string>();
                    bool inBlock = false;

                    while (true)
                    {
                        Console.Write(inBlock ? "... " : ">>> ");
                        string line = Console.ReadLine();
                        if (line == null) break;  // Ctrl+Z/Ctrl+D
                        if (line.Trim().ToLower() == "exit()") break;

                        // If we are not already in a block, check if we need to start one
                        if (!inBlock && NeedsMoreInput(line))
                        {
                            inBlock = true;
                            codeBlock.Clear();
                        }

                        codeBlock.Add(line);

                        // If the block has ended, execute it
                        if (inBlock && BlockEnded(line))
                        {
                            string fullCode = string.Join("\n", codeBlock);
                            codeBlock.Clear();
                            inBlock = false;

                            try
                            {
                                scope.Exec(fullCode);
                            }
                            catch (PythonException pex)
                            {
                                Console.WriteLine($"Python error:\n{pex}");
                            }
                        }
                        else if (!inBlock)
                        {
                            // Single-line command
                            try
                            {
                                scope.Exec(line);
                            }
                            catch (PythonException pex)
                            {
                                Console.WriteLine($"Python error:\n{pex}");
                            }
                        }
                    }
                }
            }
        }

        private int _previousIndentLevel = 0;
        private bool BlockEnded(string line)
        {
            // If the line is empty or whitespace, the block is assumed to end
            if (string.IsNullOrWhiteSpace(line))
            {
                return true;
            }

            // Check if the current line closes all open brackets/parentheses
            int openParentheses = line.Count(c => c == '(') - line.Count(c => c == ')');
            int openBrackets = line.Count(c => c == '[') - line.Count(c => c == ']');
            int openBraces = line.Count(c => c == '{') - line.Count(c => c == '}');

            if (openParentheses > 0 || openBrackets > 0 || openBraces > 0)
            {
                // Block is not complete if there are unmatched brackets
                return false;
            }

            // Check for dedentation (assumes we are tracking indentation levels)
            int currentIndentLevel = line.TakeWhile(char.IsWhiteSpace).Count();
            if (currentIndentLevel < _previousIndentLevel)
            {
                // Dedentation typically indicates the end of a block
                return true;
            }

            // Save current indentation level for the next call
            _previousIndentLevel = currentIndentLevel;

            // Block ends if none of the above indicate continuation
            return false;
        }
        private bool NeedsMoreInput(string line)
        {
            // Check if the line ends with a colon (indicates a block starts)
            if (line.TrimEnd().EndsWith(":"))
            {
                return true;
            }

            // Check for unmatched parentheses, brackets, or braces
            int openParentheses = line.Count(c => c == '(') - line.Count(c => c == ')');
            int openBrackets = line.Count(c => c == '[') - line.Count(c => c == ']');
            int openBraces = line.Count(c => c == '{') - line.Count(c => c == '}');

            if (openParentheses > 0 || openBrackets > 0 || openBraces > 0)
            {
                return true;
            }

            return false; // Otherwise, assume the input is complete
        }

        #endregion "Interactive Python"

        /// <summary>
        /// Runs a Python command line (pip or conda) asynchronously, capturing console output in real time.
        /// </summary>
        /// <param name="runTimeManager">Reference to the active <see cref="IPythonRunTimeManager"/>.</param>
        /// <param name="progress">Progress reporter for output messages.</param>
        /// <param name="commandstring">Command arguments (e.g., "install requests").</param>
        /// <param name="useConda">If true, use conda; otherwise use pip/python.exe.</param>
        /// <returns>A string containing the captured console output.</returns>
        public async Task<string> RunPythonCommandLineAsync(
     IPythonRunTimeManager runTimeManager,
     string commandString,
     IProgress<PassedArgs> progress = null,
     bool useConda = false,
     int timeoutInSeconds = 120)
        {
            string customPath = $"{runTimeManager.CurrentRuntimeConfig.BinPath.Trim()};{runTimeManager.CurrentRuntimeConfig.ScriptPath.Trim()}".Trim();
            string modifiedFilePath = customPath.Replace("\\", "\\\\");
            string output = "";

            string wrappedPythonCode = @"
import os
import subprocess
import queue
import threading

def set_custom_path(custom_path):
    os.environ['PATH'] = custom_path + os.pathsep + os.environ['PATH']

def run_command_with_output(args, output_callback, timeout):
    def enqueue_output(stream, queue):
        for line in iter(stream.readline, b''):
            queue.put(line.decode('utf-8').strip())
        stream.close()

    process = subprocess.Popen(args, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    stdout_queue = queue.Queue()
    stderr_queue = queue.Queue()

    stdout_thread = threading.Thread(target=enqueue_output, args=(process.stdout, stdout_queue))
    stderr_thread = threading.Thread(target=enqueue_output, args=(process.stderr, stderr_queue))
    stdout_thread.start()
    stderr_thread.start()

    try:
        process.wait(timeout=timeout)
    except subprocess.TimeoutExpired:
        process.kill()
        output_callback('Timeout expired. Process killed.')
        return

    while not stdout_queue.empty():
        output_callback(stdout_queue.get_nowait())
    while not stderr_queue.empty():
        output_callback(stderr_queue.get_nowait())

    stdout_thread.join()
    stderr_thread.join()
";

            try
            {
                using (Py.GIL())
                {
                    using (PyModule scope = Py.CreateScope())
                    {
                        scope.Exec(wrappedPythonCode);
                        PyObject setCustomPathFunc = scope.GetAttr("set_custom_path");
                        setCustomPathFunc.Invoke(modifiedFilePath.ToPython());

                        PyObject runCommandFunc = scope.GetAttr("run_command_with_output");

                        string command = useConda
                            ? $"conda {commandString}"
                            : $"python.exe {commandString}";

                        progress?.Report(new PassedArgs { Messege = $"Running {command}" });

                        // Set up the Python function arguments
                        PyObject pyArgs = new PyList(command.Split(' ').ToPython());
                        Channel<string> outputChannel = Channel.CreateUnbounded<string>();
                        PyObject outputCallback = PyObject.FromManagedObject((Action<string>)(line =>
                        {
                            outputChannel.Writer.TryWrite(line);
                        }));

                        Task pythonTask = Task.Run(() =>
                            runCommandFunc.Invoke(pyArgs, outputCallback, timeoutInSeconds.ToPython()));

                        // Collect output from the channel
                        var outputList = new List<string>();
                        async Task ReadFromChannelAsync()
                        {
                            while (await outputChannel.Reader.WaitToReadAsync())
                            {
                                if (outputChannel.Reader.TryRead(out var line))
                                {
                                    outputList.Add(line);
                                    progress?.Report(new PassedArgs { Messege = line });
                                    Console.WriteLine(line);
                                }
                            }
                        }

                        Task readOutputTask = ReadFromChannelAsync();
                        await pythonTask;
                        outputChannel.Writer.Complete();
                        await readOutputTask;

                        output = string.Join("\n", outputList);
                    }
                }

                progress?.Report(new PassedArgs { Messege = $"Finished {commandString}" });
            }
            catch (Exception ex)
            {
                progress?.Report(new PassedArgs { Messege = $"Error: {ex.Message}" });
                Console.WriteLine($"Error: {ex}");
            }

            return output;
        }

        #region "Error Handling"
        /// <summary>
        /// Invokes the OnError event with the given Python and .NET error information.
        /// </summary>
        private void RaiseOnError(string errorMessage, string pythonTraceback, Exception ex)
        {
            OnError?.Invoke(this, new PythonErrorEventArgs
            {
                ErrorMessage = errorMessage,
                PythonTraceback = pythonTraceback,
                DotNetException = ex
            });
        }
        /// <summary>
        /// Example method that shows how to capture a Python traceback in case of failure.
        /// </summary>
        public void RunWithDetailedErrorHandling(string code)
        {
            try
            {
                // Acquire the GIL
                using (Py.GIL())
                {
                    // Attempt to execute code
                    using (PyModule scope = Py.CreateScope())
                    {
                        scope.Exec(code);
                    }
                }
            }
            catch (PythonException pex)
            {
                // PythonException gives you .NET access to the Python traceback
                string pythonTraceback = pex.StackTrace; // This typically includes the Python traceback

                // Optionally, you can get more detail by:
                // string fullDetails = pex.Format(); 
                // or pex.ToString()

                // Raise event or log
                RaiseOnError(
                    $"A PythonException occurred: {pex.Message}",
                    pythonTraceback,
                    pex
                );
            }
            catch (Exception ex)
            {
                // Handle other .NET exceptions
                RaiseOnError(
                    $"A .NET exception occurred: {ex.Message}",
                    null,
                    ex
                );
            }
        }
        #endregion "Error Handling"
        #region "Long Running Task"
        /// <summary>
        /// Example method showing how to pass a CancellationToken to a Python execution loop.
        /// </summary>
        /// <remarks>
        /// The Python code itself should periodically check an external condition
        /// or the `_shouldStop` flag. Alternatively, you can forcibly shut down
        /// the Python engine if needed.
        /// </remarks>
        public async Task RunLongPythonScriptAsync(string script, IProgress<PassedArgs> progress, CancellationToken externalToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _shouldStop = false;

            try
            {
                // Example: We pass the token to a helper method
                await Task.Run(() =>
                {
                    // Acquire GIL
                    using (Py.GIL())
                    {
                        using (PyModule scope = Py.CreateScope())
                        {
                            // Insert your usual code capturing mechanism here
                            // E.g., scope.Exec(...), but wrap it in a while loop or code chunk
                            // that checks `_shouldStop` or `_cts.Token.IsCancellationRequested`.
                            scope.Exec(script);
                        }
                    }
                }, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Execution was cancelled
                progress?.Report(new PassedArgs() { Messege = "Python script cancelled." });
            }
            catch (PythonException pex)
            {
                // Handle Python exceptions, see "More Robust Error Handling"
                progress?.Report(new PassedArgs() { Messege = $"Python error: {pex.Message}" });
            }
            catch (Exception ex)
            {
                // Handle .NET exceptions
                progress?.Report(new PassedArgs() { Messege = $"C# error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Signals that execution should stop gracefully.
        /// </summary>
        public void Stop()
        {
            _shouldStop = true;
            _cts?.Cancel(); // Cancel any tasks
        }

        /// <summary>
        /// Forces a Python shutdown if you need a more drastic kill.
        /// (Use with caution: affects the entire Python runtime in your process.)
        /// </summary>
        public void ForceStopPython()
        {
            try
            {
                PythonEngine.Shutdown();
            }
            catch (Exception ex)
            {
                // Log or handle
            }
        }
    }
    #endregion "Long Running Task"

}
