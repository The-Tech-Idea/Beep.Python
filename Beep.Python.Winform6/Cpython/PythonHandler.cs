﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;

using AI;
using Beep.Python.Model;

using ScintillaNET;
using TheTechIdea.Beep.AIBuilder;
using DialogResult = System.Windows.Forms.DialogResult;


namespace AIBuilder.Cpython
{
    public class PythonHandler
    {
        public PythonHandler(IDMEEditor pDMEEditor, Scintilla prichBoxWriter, RichTextBox poutbox,BindingSource pbindingSource)
        {
            DMEEditor = pDMEEditor;
            bindingSource = pbindingSource;
            outrichtextbox = poutbox;
            scriptrichtextbox = prichBoxWriter;

            scriptWriter = prichBoxWriter;
            outputBoxWriter = new RichTextBoxWriter(poutbox);
            String AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            if (Environment.Is64BitOperatingSystem)
            {
                binpath = Path.Combine(DMEEditor.ConfigEditor.Config.ClassPath,AppName, "python-3.9.5-embed-amd64");
                DMEEditor.assemblyHandler.LoadAssembly(Path.Combine(DMEEditor.ConfigEditor.Config.ConnectionDriversPath, "sqllite\\x64"), FolderFileTypes.ConnectionDriver);

            }
            else
            {
                binpath = Path.Combine(DMEEditor.ConfigEditor.Config.ClassPath,AppName, "python-3.9.5-embed-win32");
                DMEEditor.assemblyHandler.LoadAssembly(Path.Combine(DMEEditor.ConfigEditor.Config.ConnectionDriversPath, "sqllite\\x86"), FolderFileTypes.ConnectionDriver);
            }
            if(!DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.Scripts && c.FolderPath.Contains("AI")).Any())
            {
                if (Directory.Exists(Path.Combine(DMEEditor.ConfigEditor.ExePath, "AI")) == false)
                {
                    Directory.CreateDirectory(Path.Combine(DMEEditor.ConfigEditor.ExePath, "AI"));

                }
                if (!DMEEditor.ConfigEditor.Config.Folders.Any(item => item.FolderPath.Equals(Path.Combine(DMEEditor.ConfigEditor.ExePath, "AI"), StringComparison.OrdinalIgnoreCase)))
                {
                    DMEEditor.ConfigEditor.Config.Folders.Add(new StorageFolders(Path.Combine(DMEEditor.ConfigEditor.ExePath, "AI"), FolderFileTypes.Scripts));
                }
                aifolder = Path.Combine(DMEEditor.ConfigEditor.ExePath, "AI");
            }
            else
            {
                aifolder = DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.Scripts && c.FolderPath.Contains("AI")).FirstOrDefault().FolderPath;
            }
            tmpcsvfile = lookfortmopcsv();
            packageinstallpath = Path.Combine(binpath, @"Lib\site-Packages");
            lasttmpcsvhash = GetFileHash(tmpcsvfile);
            SetupEnvVariables();
        }
        public IDMEEditor DMEEditor { get; set; }
        public string binpath { get; set; }
        public string aifolder { get; set; }
        public string packageinstallpath { get; set; }
        public BindingSource bindingSource { get; set; }
        public string filenameLoaded { get; set; } = null;
     
        public string packagenames { get; } = "Plotly;plotly;Chart,PyQt5;PyQt5;Chart,Dash;Ploty and Dash App;Chart,pythonnet;Python.Net;Tools,qtconsole;qtconsole;Tools,jupyter;Jupyter;Tools,winpty;Pseudoterminals;Tools,ipython;IPython;Tools,pprint36;Pretty Print;Tools,tabulate;Tabular Print;Tools,Pillow;Imaging Library;Chart,Matplotlib;MatPlot;Chart,Numpy;Numpy;Compute,opencv-python;OpenCV;Chart,Requests;HTTP library;Tools,Keras;Keras;ML,TensorFlow;TensorFlow;ML,Theano;Theano Math;ML,NLTK;Natural Language Toolkit;ML,Fire;Fire Auto. command line interfaces Generation;Tools,Arrow;Arrow Date Manupliation;Tools,FlashText;FlashText;Tools,Scipy;SciPy Scientific Library;ML,SQLAlchemy;SQLAlcemy Database Abstraction;ML,wxPython;wx GUI toolkit;Gui,torch;PyTorch Tensors and Dynamic neural networks;ML,Luminoth;Luminoth Computer vision toolkit based on TensorFlow;Chart,BeautifulSoup;BeautifulSoup Screen-scraping library;Tools,Bokeh;Bokeh Interactive plots and applications;Chart,Poetry;Poetry dependency management and packaging made easy;Tools,Gensim;Gensim fast Vector Space Modelling;ML,pandas;Pandas data structures for data analysis-time series-statistics;ML,Pytil;tility library;Tools,scikit-learn;Scikit Learn machine learning and data mining;ML,NetworkX;Networkx creating and manipulating graphs and networks;ML,TextBlob;TextBlob text processing;ML,Mahotas;Mahotas Computer Vision;ML";
        public string packagecatgoryimages { get; } = "Tools;tools,ML;ml,GUI;gui,Chart;gfx,Compute;Compute";
        public List<PackageDefinition> packages { get; set; } = new List<PackageDefinition>();
        public List<packageCategoryImages> packageCategorys { get; set; } = new List<packageCategoryImages>();
        public string[] packs { get; set; }
        public int numOutputLines { get; set; }
        public List<string> outputdata { get; set; } = new List<string>();
        public byte[] lasttmpcsvhash { get; set; }
       // RichTextBoxWriter scriptWriter { get; }
        Scintilla scriptWriter { get; }
        RichTextBoxWriter outputBoxWriter { get; }
        public Process Process { get; set; }

        string FilenameLoaded = null;
        bool TmpCSVFileUpdated = false;

        public Scintilla scriptrichtextbox { get; set; }
        public RichTextBox outrichtextbox { get; set; }

        public  ResourceManager resourceManager { get; set; } = new ResourceManager();
        string tmpcsvfile;
        #region "Pip Handling"
        public void SetupPipMenu(ToolStripMenuItem packagesToolStripMenuItem )
        {
            string pname;
            string ptitle;
            PackageCategory category;
            string[] packs = packagenames.Split(',');
            string[] packscategoriesimages = packagecatgoryimages.Split(',');
            foreach (string item in packscategoriesimages)
            {
                string[] imgs = item.Split(';');
                packageCategorys.Add(new packageCategoryImages { category = imgs[0], image = imgs[1] });

            }
            foreach (string item in packs)
            {
                try
                {
                    string[] pc = item.Split(';');

                    pname = pc[0];
                    ptitle = pc[1];
                    category = (PackageCategory)Enum.Parse(typeof(PackageCategory), pc[2]);

                    packages.Add(new PackageDefinition { PackageName = pname, PackageTitle = ptitle, Category = category, Installpath = packageinstallpath });
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Could not add {item}");
                }
            

            }
            ToolStripItem t = packagesToolStripMenuItem.DropDownItems.Add("Install pip");
            t.Click += Installpip_Click;
            t.Image= resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "install.ico");

            ToolStripItem tupdate = packagesToolStripMenuItem.DropDownItems.Add("Update pip");
            tupdate.Click += updatePIP_Click;
            tupdate.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "install.ico");
            t = packagesToolStripMenuItem.DropDownItems.Add("List Packages");
            t.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "list.ico");
            t.Click += PackagesInstalledbutton_Click;

          

            //ToolStripMenuItem AILMitem = new ToolStripMenuItem("ML");
            //ToolStripMenuItem GFXitem = new ToolStripMenuItem("GFX");
            //ToolStripMenuItem Toolsitem = new ToolStripMenuItem("Tools");
            //ToolStripMenuItem Computesitem = new ToolStripMenuItem("Compute");
            //ToolStripMenuItem Guisitem = new ToolStripMenuItem("GUI");
            //packagesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {AILMitem, GFXitem ,Toolsitem, Computesitem,Guisitem });

            //t.Click += InstallPythonNetbutton_Click;
            // ToolStripMenuItem
            foreach (PackageCategory item in packages.Select(o=>o.Category).Distinct().ToList())
            {
                ToolStripMenuItem o = new ToolStripMenuItem();
                o.ImageScaling = ToolStripItemImageScaling.SizeToFit;
                o.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                o.Text=item.ToString();
                if (packageCategorys.Where(u=>u.category.Equals(o.Text, StringComparison.OrdinalIgnoreCase)).Any())
                {
                    switch (item)
                    {
                        case  PackageCategory.UI:
                            
                            o.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "gui.ico");
                            break;
                        case  PackageCategory.Utilities:
                            o.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "tools.ico");
                            break;
                        case  PackageCategory.Graphics:
                            o.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "gfx.ico");
                            break;
                        case  PackageCategory.DataScience:
                            o.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "ml.ico");
                            break;
                        case  PackageCategory.Compute:
                            o.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "compute.ico");
                            break;
                        default:
                            break;
                    }

                   
                }
        //        o.Text = item;
               
                packagesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { o });
                foreach (PackageDefinition package in packages.Where(p=>p.Category==item))
                {


                    t = o.DropDownItems.Add(package.PackageTitle);


                    if (Checkifpackageinstalled(package.PackageName))
                    {
                        t.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "linked.ico");
                    }
                    else
                    {
                        t.Image = resourceManager.GetImage("TheTechIdea.Beep.AIBuilder.gfx.", "nolink.ico");
                    }
                    t.Click += PackagesToolStripMenuItem_Click;

                }
            }
           
        }
        public bool Checkifpackageinstalled(string packagename)
        {
            if (packagename.Contains("-"))
            {
                packagename = packagename.Replace("-", "_");
            }
            string[] dirs = Directory.GetDirectories(packageinstallpath, packagename+"*", SearchOption.TopDirectoryOnly);
            if (dirs.Length > 0)
            {
                return true;
            }
            else  return false;

        }
        public void Installpip_Click(object sender, EventArgs e)
        {
            runPythonScriptscommandlineAsync("py get-pip.py", $@"{binpath}\scripts\");
        }
        public void PackagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem i = (ToolStripMenuItem)sender;
            string n = i.Text;
            string packagename = packages.Where(o => o.PackageTitle.Equals(n, StringComparison.OrdinalIgnoreCase)).Select(o => o.PackageName).FirstOrDefault();
            runPythonScriptscommandlineAsync($@"pip.exe install {packagename}", $@"{binpath}\scripts\");
            //if (Checkifpackageinstalled(PackageName))
            //{
            //    i.Image = global::AIBuilder.Properties.Resources.verified_account_32px;
            //    MessageBox.Show($"Success Install Package {n}");
            //}
            //else
            //{
            //    i.Image = global::AIBuilder.Properties.Resources.cancel_32px;
            //    MessageBox.Show($"Failed to Install Package {n}");
            //}

        }
        public void updatePIP_Click(object sender, EventArgs e)
        {


            runPythonScriptscommandlineAsync($@"{binpath}\python.exe -m pip install --upgrade pip", $@"{binpath}\scripts\");
        }
        public void InstallPythonNet()
        {
            runPythonScriptscommandlineAsync("pip install pythonnet", $@"{binpath}\scripts\");
        }
        public void PackagesInstalledbutton_Click(object sender, EventArgs e)
        {
            runPythonScriptscommandlineAsync($@"pip.exe list", $@"{binpath}\scripts\");

        }
        public void QtConsoleRun()
        {
            //jupyter qtonsole
            runPythonScriptscommandlineAsync($@"jupyter qtconsole ", $@"{aifolder}");
        }
        public void QtConsoleStop()
        {
            //jupyter qtonsole
            runPythonScriptscommandlineAsync($@"jupyter qtconsole stop", $@"{aifolder}");
        }
        public void JupiterRun()
        {
            runPythonScriptscommandlineAsync($@"jupyter notebook ", $@"{aifolder}");
        }
        public void JupiterStop()
        {
            runPythonScriptscommandlineAsync($@"jupyter notebook stop ", $@"{aifolder}");
        }
        #endregion
        #region "Process Management"

        private void SetupEnvVariables()
        {

            Process = new Process();

            Process.StartInfo = new ProcessStartInfo("cmd.exe");
            // Process.StartInfo.Arguments = "/c";
            Process.StartInfo.CreateNoWindow = true;
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardInput = true;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;

            Process.OutputDataReceived += Process_OutputDataReceived;
            Process.ErrorDataReceived += Process_ErrorDataReceived;
            Process.Exited += Process_Exited;
            Process.Start();
            // 4) Execute process and get output
            Process.BeginErrorReadLine();
            Process.BeginOutputReadLine();
            outputdata = new List<string>();
            Process.StandardInput.WriteLine($@"set PATH={binpath};%PATH%");
            Process.StandardInput.WriteLine($@"set PYTHONPATH={Path.Combine(binpath, "lib")};{Path.Combine(DMEEditor.ConfigEditor.ExePath,"ProjectClasses")};{Path.Combine(DMEEditor.ConfigEditor.ExePath, "OtherDLL")};{Path.Combine(DMEEditor.ConfigEditor.ExePath, "ConnectionDrivers")};{DMEEditor.ConfigEditor.ExePath}");
            Process.StandardInput.WriteLine($@"set PATH={Path.Combine(binpath, "scripts")};%PATH%");
            //      Process.StandardInput.WriteLine("exit");
            numOutputLines = 0;
            // Process.WaitForExit();
        }
        public void RunScript()
        {
            string scripttorun = Path.Combine(aifolder, "tmp.py");
            File.WriteAllText(scripttorun, scriptrichtextbox.Text);
            //var t = Task.Run(() => {   });
            runPythonScriptcommandlineSync($@"{binpath}\python.exe -q {Path.GetFileName(scripttorun)}", aifolder);
            //int milliseconds = 2000;
            //Thread.Sleep(milliseconds);
            //   GetoutputText();
            if (lasttmpcsvhash != GetFileHash(tmpcsvfile))
            {
               bindingSource.DataSource= ConvertStringtoDatatable();
               lasttmpcsvhash = GetFileHash(tmpcsvfile);
            }

        }
        public void runPythonScriptcommandlineSync(string Command, string Commandpath)
        {


            Process Process = new Process();
            Process.StartInfo = new ProcessStartInfo("cmd.exe");
            // Process.StartInfo.Arguments = "/c";
            Process.StartInfo.CreateNoWindow = true;
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardInput = true;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;

            Process.OutputDataReceived += Process_OutputDataReceived;
            Process.ErrorDataReceived += Process_ErrorDataReceived;
            Process.Exited += Process_Exited;
            Process.Start();
            // 4) Execute process and get output
            Process.BeginErrorReadLine();
            Process.BeginOutputReadLine();
            outputdata = new List<string>();
            Process.StandardInput.WriteLine($"set PATH={binpath};%PATH%");
            Process.StandardInput.WriteLine($@"set PYTHONPATH={Path.Combine(binpath, "lib")};{Path.Combine(DMEEditor.ConfigEditor.ExePath, "ProjectClasses")};{Path.Combine(DMEEditor.ConfigEditor.ExePath, "OtherDLL")};{Path.Combine(DMEEditor.ConfigEditor.ExePath, "ConnectionDrivers")};{DMEEditor.ConfigEditor.ExePath}");
            Process.StandardInput.WriteLine($@"set PATH={Path.Combine(binpath, "scripts")};%PATH%");
            Process.StandardInput.WriteLine($@"cd {Commandpath} ");

            Process.StandardInput.WriteLine(Command);
            Process.StandardInput.WriteLine("exit");
            var output = new List<string>();

            while (Process.StandardOutput.Peek() > -1)
            {
                output.Add(Process.StandardOutput.ReadLine());
                outputBoxWriter.WriteLine(Process.StandardOutput.ReadLine());
                DMEEditor.AddLogMessage("Python Module", $"{output.Last()}", DateTime.Now, numOutputLines, null, Errors.Failed);
            }

            while (Process.StandardError.Peek() > -1)
            {
                 output.Add(Process.StandardError.ReadLine());
                outputBoxWriter.WriteLine(Process.StandardError.ReadLine());
                DMEEditor.AddLogMessage("Python Module", $"Error in Python Module {output.Last()}", DateTime.Now, numOutputLines, null, Errors.Failed);

            }
          
            Process.WaitForExit();
            Process.Close();

        }
        public void runPythonScriptscommandlineAsync(string Command, string Commandpath)
        {

            Process.StartInfo.WorkingDirectory = Commandpath;
            Process.StandardInput.WriteLine($@"cd {Commandpath} ");
          
            Process.StandardInput.WriteLine(Command);
            // Process.StandardInput.WriteLine(">.");
            // Process.StandardInput.WriteLine("exit");
            



        }
        private void Process_Exited(object sender, EventArgs e)
        {
            if (outputdata.Count > 0)
            {
                ConvertStringtoDatatable();
            }

        }
        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                numOutputLines++;

                // Add the text to the collected output.
                outputdata.Append(Environment.NewLine + $"[{numOutputLines}] - {e.Data}");
                //this.OutputtextBox.BeginInvoke(new Action(() => {
                //    this.OutputtextBox.AppendText(Environment.NewLine +
                //    $"[{NumOutputLines}] - {e.Data}");
                //}));

                DMEEditor.AddLogMessage("Python Module", $"Error in Python Module {e.Data}", DateTime.Now, numOutputLines, null, Errors.Failed);

            }
        }
        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                if ((!e.Data.ToLower().Contains("c:")) && (!e.Data.Contains("Microsoft")))
                {
                    numOutputLines++;


                    // Add the text to the collected output.

                    //this.OutputtextBox.BeginInvoke(new Action(() => {
                    //    this.OutputtextBox.AppendText(Environment.NewLine +
                    //    $">{e.Data}");
                    //}));
                     outputBoxWriter.WriteLine(e.Data);
                    DMEEditor.AddLogMessage("Python Module", $"{e.Data}", DateTime.Now, numOutputLines, null, Errors.Failed);
                }
                else
                {

                    string withoutSubString = e.Data;
                    int indexOfSubString = e.Data.IndexOf(binpath);
                    if (indexOfSubString != -1)
                    {
                        withoutSubString = e.Data.Remove(indexOfSubString, binpath.Length);
                    }
                    if ((!withoutSubString.Contains("Microsoft")))
                    {
                        DMEEditor.AddLogMessage("Python Module", $">{withoutSubString}", DateTime.Now, numOutputLines, null, Errors.Ok);
                    }

                }


            }


        }
        #endregion
        #region "Output Management"
        private byte[] GetFileHash(string fileName)
        {
            HashAlgorithm sha1 = HashAlgorithm.Create();
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                return sha1.ComputeHash(stream);
        }
        private DataTable ConvertStringtoDatatable()
        {
            DataTable dt;

            if (File.Exists(tmpcsvfile))
            {
                dt = DMEEditor.Utilfunction.CreateDataTableFromFile(tmpcsvfile);
            }
            else
                dt = null;
            // griddatasource.DataSource = null;
            //griddatasource.DataSource = dt;
            //griddatasource.ResetBindings(true);
            //this.OutputdataGridView.AutoGenerateColumns = true;
            ////  this.OutputdataGridView.DataSource = null;
            //this.OutputdataGridView.DataSource = griddatasource;
            //this.OutputdataGridView.Refresh();

            return dt;
        }
       
        #endregion
        #region "File Handling"
        private string lookfortmopcsv()
        {
            string retval = null;
            foreach (StorageFolders item in DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.Scripts && c.FolderPath.Contains("AI")))
            {
                if (File.Exists(Path.Combine(item.FolderPath,"tmp.csv")))
                {
                    retval = Path.Combine(item.FolderPath, "tmp.csv");
                }
            }
            if (retval == null)
            {
                 File.CreateText(Path.Combine(aifolder, "tmp.csv"));
                retval = Path.Combine(aifolder, "tmp.csv");
            }
            return retval;
        }
        public void SaveTextAsFile()
        {
            try
            {
                 SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
                {
                    Title = "Save File",

                    DefaultExt = "py",
                    Filter = "python files(*.py) |*.py",

                    FilterIndex = 1,
                    RestoreDirectory = true

                    //ReadOnlyChecked = true,
                    //ShowReadOnly = true
                };
                saveFileDialog1.InitialDirectory = aifolder;
                //  saveFileDialog1.Multiselect = false;
                DialogResult result = saveFileDialog1.ShowDialog();
                if (result == DialogResult.OK) // Test result.
                {
                    File.WriteAllText(saveFileDialog1.FileName, scriptrichtextbox.Text);
                }
                FilenameLoaded = saveFileDialog1.FileName;
            }
            catch (Exception ex)
            {


                string errmsg = "Error in saving python script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }
        public void SaveTexttoFile()
        {

            try
            {
                if (FilenameLoaded == null)
                {
                    SaveTextAsFile();
                }
                else
                {

                    File.WriteAllText(FilenameLoaded, scriptrichtextbox.Text);
                    MessageBox.Show("ScriptPath Saved");
                }

            }
            catch (Exception ex)
            {


                string errmsg = "Error in saving python script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }
        public string LoadScriptFile(string filename)
        {
            try
            {
                string loadfilename = "";
                DialogResult result= DialogResult.None;
                OpenFileDialog openFileDialog1=new OpenFileDialog();
                if (string.IsNullOrEmpty(filename))
                {
                    openFileDialog1 = new System.Windows.Forms.OpenFileDialog()
                    {
                        Title = "Browse Files",
                        CheckFileExists = true,
                        CheckPathExists = true,
                        DefaultExt = "py",
                        Filter = "python files(*.py) |*.py",
                        FilterIndex = 1,
                        RestoreDirectory = true

                        //ReadOnlyChecked = true,
                        //ShowReadOnly = true
                    };
                    openFileDialog1.InitialDirectory = aifolder;
                    openFileDialog1.Multiselect = false;
                    result = openFileDialog1.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        loadfilename = openFileDialog1.FileName;
                    }
                }
                else
                {
                    if (File.Exists(filename))
                    {
                        loadfilename = filename;
                    }
                   
                }
                
                 String line;
                scriptrichtextbox.Clear();
                if (!string.IsNullOrEmpty(loadfilename)) // Test result.
                {
                    //Pass the file path and file name to the StreamReader constructor
                    StreamReader sr = new StreamReader(loadfilename);
                    //Read the first line of text
                    line = sr.ReadLine();
                    //Continue to read until you reach end of file
                    while (line != null)
                    {
                        //write the lie to console window
                        scriptrichtextbox.AppendText(line + Environment.NewLine);
                        //Read the next line
                        line = sr.ReadLine();
                    }
                    //close the file
                    sr.Close();
                    FilenameLoaded = loadfilename;
                    return filenameLoaded;
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
                string errmsg = "Error in getting python script";
                DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }
        #endregion
        #region "DataSource Handling"
        public IEnumerable<string> GetLocalDB()
        {
            IEnumerable<ConnectionDriversConfig> cndrs = DMEEditor.ConfigEditor.DataDriversClasses.Where(x => x.CreateLocal == true);
            return from x in DMEEditor.ConfigEditor.DataConnections
                   from y in cndrs
                   where x.DriverName == y.PackageName
                   select x.ConnectionName;
        }
        #endregion
    }
}
