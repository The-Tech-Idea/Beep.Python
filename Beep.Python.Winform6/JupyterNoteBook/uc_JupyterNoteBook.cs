
using TheTechIdea.Beep.Winform.Controls.Basic;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Addin;

using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Container.Services;
using Beep.Python.Model;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Collections;
using Beep.Python.RuntimeEngine.Services;





namespace Beep.Python.Winform.JupyterNoteBook
{
    [AddinAttribute(Caption = "Jupyter NoteBook", Name = "uc_JupyterNoteBook", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_JupyterNoteBook : uc_Addin
    {
        bool IsJupyterRunning = false;

        public uc_JupyterNoteBook()
        {
            InitializeComponent();
        }

        private void InitializeWebView()
        {
            try
            {
                Visutil.PasstoWaitForm(new PassedArgs() { Messege = "Opening Jupyter ..." });
              
               
                webView21.Source = new Uri("http://localhost:8888"); // No token needed
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep", $"Error in Creating Jupyter Notebook {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
            }
        }
        IBeepService beepService;
        IPythonRunTimeManager pythonRunTimeManager;
        public override void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            base.SetConfig(pDMEEditor, plogger, putil, args, e, per);
            beepService = DMEEditor.GetBeepService();
            pythonRunTimeManager = DMEEditor.GetPythonRunTimeManager();
            Progress<PassedArgs> progress = new Progress<PassedArgs>();
            CancellationToken token = new CancellationToken();
            Visutil.ShowWaitForm(new PassedArgs() { Messege = "Starting Jupyter Notebook" });
            Visutil.PasstoWaitForm(new PassedArgs() { Messege = "Starting Jupyter Notebook" });
            progress.ProgressChanged += (s, args) =>
            {
                if (args.Messege != null)
                {
                    Visutil.PasstoWaitForm(new PassedArgs() { Messege = args.Messege });
                }
            };


            CreateJupyterNotebookConfig();
           var t= Task.Run(() => StartJupyterNotebookAsync2());
            t.Wait();
            if (IsJupyterRunning)
            {
                DMEEditor.AddLogMessage("Beep", "Jupyter is Running", DateTime.Now, -1, "", Errors.Ok);
                Visutil.PasstoWaitForm(new PassedArgs() { Messege="Jupyter is Running" });
                InitializeWebView();
            }
            else
            {
                Visutil.PasstoWaitForm(new PassedArgs() { Messege = "Jupyter is not Running" });
                DMEEditor.AddLogMessage("Beep", "Jupyter is not Running", DateTime.Now, -1, "", Errors.Failed);
            }
            Visutil.PasstoWaitForm(new PassedArgs() { Messege = "Ended Jupyter Notebook init." });
            DMEEditor.AddLogMessage("Beep", "Ended Jupyter Notebook init.", DateTime.Now, -1, "", Errors.Ok);
            Visutil.CloseWaitForm();
        }
        public bool CreateJupyterNotebookConfig()
        {
            try
            {
                string jupyterConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".jupyter");
                string configFilePath = Path.Combine(jupyterConfigDir, "jupyter_notebook_config.py");

                // Ensure the .jupyter directory exists
                if (!Directory.Exists(jupyterConfigDir))
                {
                    Directory.CreateDirectory(jupyterConfigDir);
                }

                // Configuration content to disable token and password authentication
                string configContent = @"
c.NotebookApp.token = ''
c.NotebookApp.password = ''
c.NotebookApp.open_browser = False
c.NotebookApp.ip = 'localhost'
";

                // Write the configuration content to the file
                File.WriteAllText(configFilePath, configContent);

                Console.WriteLine("Jupyter Notebook configuration file has been created successfully.");
                Console.WriteLine($"Path: {configFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the Jupyter Notebook configuration file: {ex.Message}");
                return false;
            }
        }
        public async Task StartJupyterNotebookAsync2()
        {
            string pythonPath = Path.Combine(pythonRunTimeManager.CurrentRuntimeConfig.BinPath, "python.exe");
            string jupyterPath = Path.Combine(pythonRunTimeManager.CurrentRuntimeConfig.BinPath, "Scripts", "jupyter-notebook.exe");
            string batchFilePath = Path.Combine(pythonRunTimeManager.CurrentRuntimeConfig.BinPath, "Scripts", "launch_jupyter.bat");

            if (!File.Exists(pythonPath))
            {
                Console.WriteLine("Python executable is not found in the specified Python environment.");
                DMEEditor.AddLogMessage("Beep", "Python executable is not found in the specified Python environment.", DateTime.Now, -1, "", Errors.Failed);
                return;
            }

            if (!File.Exists(jupyterPath))
            {
                Console.WriteLine("Jupyter Notebook executable is not found in the specified Python environment.");
                DMEEditor.AddLogMessage("Beep", "Jupyter Notebook executable is not found in the specified Python environment.", DateTime.Now, -1, "", Errors.Failed);
                return;
            }

            // Create the batch file
            try
            {
                using (StreamWriter writer = new StreamWriter(batchFilePath))
                {
                    writer.WriteLine("@echo off");
                    writer.WriteLine($"set PYTHON_EXEC={pythonPath}");
                    writer.WriteLine($"set JUPYTER_PATH={jupyterPath}");
                    writer.WriteLine("if not exist \"%PYTHON_EXEC%\" (");
                    writer.WriteLine("    echo Python executable not found");
                    writer.WriteLine("    exit /b 1");
                    writer.WriteLine(")");
                    writer.WriteLine("if not exist \"%JUPYTER_PATH%\" (");
                    writer.WriteLine("    echo Jupyter Notebook executable not found");
                    writer.WriteLine("    exit /b 1");
                    writer.WriteLine(")");
                    writer.WriteLine("\"%PYTHON_EXEC%\" \"%JUPYTER_PATH%\" --no-browser");
                }

                Console.WriteLine("Batch file created successfully.");
                DMEEditor.AddLogMessage("Beep", "Batch file created successfully.", DateTime.Now, -1, "", Errors.Ok);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the batch file: {ex.Message}");
                DMEEditor.AddLogMessage("Beep", $"An error occurred while creating the batch file: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                return;
            }

            // Run the batch file
            var processStartInfo = new ProcessStartInfo
            {
                FileName = batchFilePath,
               
                UseShellExecute = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(batchFilePath)
            };

            using (var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true })
            {
                var tcs = new TaskCompletionSource<string>();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        Console.WriteLine(args.Data);
                        DMEEditor.AddLogMessage("Beep", $"STDOUT: {args.Data}", DateTime.Now, -1, "", Errors.Ok);

                        // Detect the URL of the running Jupyter Notebook server
                        if (args.Data.Contains("http://localhost:"))
                        {
                            var match = Regex.Match(args.Data, @"(http://localhost:\d+/)");
                            if (match.Success)
                            {
                                var jupyterUrl = match.Groups[1].Value;
                                IsJupyterRunning = true;
                                tcs.SetResult(jupyterUrl);
                            }
                        }
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        DMEEditor.AddLogMessage("Beep", $"STDERR: {args.Data}", DateTime.Now, -1, "", Errors.Failed);
                        Console.WriteLine($"ERROR: {args.Data}");
                    }
                };

                process.Exited += (sender, args) =>
                {
                    if (!IsJupyterRunning)
                    {
                        Console.WriteLine("Jupyter Notebook process exited unexpectedly.");
                        DMEEditor.AddLogMessage("Beep", "Jupyter Notebook process exited unexpectedly.", DateTime.Now, -1, "", Errors.Failed);
                        tcs.SetResult(null);
                    }
                    else
                    {
                        Console.WriteLine("Jupyter Notebook process exited.");
                        DMEEditor.AddLogMessage("Beep", "Jupyter Notebook process exited.", DateTime.Now, -1, "", Errors.Ok);
                    }
                };

                Console.WriteLine("Starting the Jupyter Notebook process...");
                DMEEditor.AddLogMessage("Beep", "Starting the Jupyter Notebook process...", DateTime.Now, -1, "", Errors.Ok);

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Wait for the Jupyter URL to be detected
                    var jupyterUrl = await tcs.Task;

                    if (!string.IsNullOrEmpty(jupyterUrl))
                    {
                        Console.WriteLine("Jupyter Notebook is running at: " + jupyterUrl);
                        DMEEditor.AddLogMessage("Beep", "Jupyter Notebook is running at: " + jupyterUrl, DateTime.Now, -1, "", Errors.Ok);

                       IsJupyterRunning = true;
                    }
                    else
                    {
                        IsJupyterRunning = false;
                        DMEEditor.AddLogMessage("Beep", "Jupyter Notebook failed to start.", DateTime.Now, -1, "", Errors.Failed);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to start process: {ex.Message}");
                    DMEEditor.AddLogMessage("Beep", $"Failed to start process: {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
                    tcs.SetResult(null);
                }

                Console.WriteLine("Process task completed.");
                DMEEditor.AddLogMessage("Beep", "Process task completed.", DateTime.Now, -1, "", Errors.Ok);
            }
        }

        public async Task StartJupyterNotebookAsync()
        {
            string pythonPath = Path.Combine(pythonRunTimeManager.CurrentRuntimeConfig.BinPath, "Scripts", "python.exe");
            string jupyterPath = Path.Combine(pythonRunTimeManager.CurrentRuntimeConfig.BinPath, "Scripts", "jupyter-notebook.exe");
            string batchFilePath = Path.Combine(pythonRunTimeManager.CurrentRuntimeConfig.BinPath, "Scripts", "launch_jupyter.bat");

            if (!File.Exists(pythonPath))
            {
                Console.WriteLine("Python executable is not found in the specified Python environment.");
                return;
            }

            if (!File.Exists(jupyterPath))
            {
                Console.WriteLine("Jupyter Notebook executable is not found in the specified Python environment.");
                return;
            }

            // Create the batch file
            try
            {
                using (StreamWriter writer = new StreamWriter(batchFilePath))
                {
                    writer.WriteLine("@echo off");
                    writer.WriteLine($"set PYTHON_EXEC={pythonPath}");
                    writer.WriteLine($"set JUPYTER_PATH={jupyterPath}");
                    writer.WriteLine("if not exist \"%PYTHON_EXEC%\" (");
                    writer.WriteLine("    echo Python executable not found");
                    writer.WriteLine("    exit /b 1");
                    writer.WriteLine(")");
                    writer.WriteLine("if not exist \"%JUPYTER_PATH%\" (");
                    writer.WriteLine("    echo Jupyter Notebook executable not found");
                    writer.WriteLine("    exit /b 1");
                    writer.WriteLine(")");
                    writer.WriteLine("\"%PYTHON_EXEC%\" \"%JUPYTER_PATH%\" --no-browser");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the batch file: {ex.Message}");
                return;
            }

            // Run the batch file
            var processStartInfo = new ProcessStartInfo
            {
                FileName = batchFilePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = true,
                CreateNoWindow = false,
                WorkingDirectory = Path.GetDirectoryName(jupyterPath) // Set the working directory to the location of jupyter-notebook.exe
            };
            // Copy environment variables from the current process
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                processStartInfo.EnvironmentVariables[de.Key.ToString()] = de.Value.ToString();
            }
            using (var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true })
            {
                var tcs = new TaskCompletionSource<bool>();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        Console.WriteLine(args.Data);
                        DMEEditor.AddLogMessage("Beep", args.Data, DateTime.Now, -1, "", Errors.Ok);
                        if (args.Data.Contains("http://localhost:8888/"))
                        {
                            IsJupyterRunning = true;
                            tcs.SetResult(true);
                        }
                    }
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        DMEEditor.AddLogMessage("Beep", args.Data, DateTime.Now, -1, "", Errors.Failed);
                        Console.WriteLine($"ERROR: {args.Data}");
                    }
                };

                process.Exited += (sender, args) =>
                {
                    if (!IsJupyterRunning)
                    {
                        Console.WriteLine("Jupyter Notebook process exited unexpectedly.");
                        DMEEditor.AddLogMessage("Beep", "Jupyter Notebook process exited unexpectedly." ,DateTime.Now, -1, "", Errors.Failed);
                        tcs.SetResult(false);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await tcs.Task;
            }
        }



        private void webView21_Click(object sender, EventArgs e)
        {

        }
    }
}
