using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Beep.Winform.Controls.Basic;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep;
using TheTechIdea.Logger;
using TheTechIdea;
using TheTechIdea.Util;
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Container.Services;
using Beep.Python.RuntimeEngine;
using Beep.Python.Model;
using CefSharp.WinForms;
using CefSharp;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;





namespace Beep.Python.Winform.JupyterNoteBook
{
    [AddinAttribute(Caption = "Jupyter NoteBook", Name = "uc_JupyterNoteBook", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_JupyterNoteBook : uc_Addin
    {
        private ChromiumWebBrowser browser;
        public uc_JupyterNoteBook()
        {
            InitializeComponent();
        }
       
        private async Task InitializeChromiumNoToken()
           
        {
            try
            {
                CefSettings settings = new CefSettings();
                Cef.Initialize(settings);
                string jupyterUrl = $"http://localhost:8888/";
                browser = new ChromiumWebBrowser(jupyterUrl) { 
                Dock=DockStyle.Fill
                }; // Jupyter Notebook URL
                                                    //  string token = await StartJupyterNotebookAsync();
            
              //  await browser.LoadUrlAsync(jupyterUrl);
                this.Controls.Add(browser);
                
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Beep",$"Error in Creating Jupyter Notebook {ex.Message}", DateTime.Now, -1, "", Errors.Failed);
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
            progress.ProgressChanged += (s, args) =>
            {
                if (args.Messege != null)
                {
                    Visutil.PasstoWaitForm(new PassedArgs() { Messege = args.Messege });
                }
            };


            CreateJupyterNotebookConfig();
            var t=StartJupyterNotebookAsync();
            t.Wait();
            var r=  Task.Run(()=> InitializeChromiumNoToken());
            r.Wait();
            Visutil.PasstoWaitForm(new PassedArgs() { Messege = "Ended Jupyter Notebook" });
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
        public async Task StartJupyterNotebookAsync()
        {
            string jupyterPath = Path.Combine(pythonRunTimeManager.CurrentRuntimeConfig.BinPath, "Scripts", "jupyter.exe");

            if (!File.Exists(jupyterPath))
            {
                Console.WriteLine("Jupyter is not installed in the specified Python environment.");
                return;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = jupyterPath,
                Arguments = "notebook --no-browser",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();

                // Read the output asynchronously
                var outputReader = process.StandardOutput;
                string outputLine;

                while ((outputLine = await outputReader.ReadLineAsync()) != null)
                {
                    Console.WriteLine(outputLine);
                }

                process.WaitForExit();
            }
        }

    }
}
