using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace Beep.Python.Winform
{
    [TheTechIdea.Beep.Vis.AddinAttribute(Caption = "Python Folder List", Name = "frm_PythonFolderManagement", misc = "AI", addinType = AddinType.Form)]
    public partial class frm_PythonFolderManagement : Form,IDM_Addin
    {
        public string ParentName { get; set; }
        public string AddinName { get; set; } = "Python Package List";
        public string Description { get; set; } = "Python Package List";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
        public Boolean DefaultCreate { get; set; } = true;
        public string DllPath { get; set; }
        public string DllName { get; set; }
        public string NameSpace { get; set; }
        public DataSet Dset { get; set; }
        public IErrorsInfo ErrorObject { get; set; }
        public IDMLogger Logger { get; set; }
        public IDMEEditor DMEEditor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public IPassedArgs Passedarg { get; set; }

       // public IPIPManager pIPManager { get; set; }

     //   public IDEManager iDEManager { get; set; }
        public BeepEnterprize.Vis.Module.IVisManager Visutil { get; set; }


        BeepEnterprize.Vis.Module.IBranch RootAppBranch;
        BeepEnterprize.Vis.Module.IBranch branch;



      //  PythonNetRunTimeManager pythonRunTimeManager;


        BindingSource griddatasource = new BindingSource();
        IProgress<PassedArgs> progress;
        CancellationToken token;
       // frm_PythonFolderManagement pythonFolderManagement;
        public void Run(IPassedArgs Passedarg)
        {

        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            DMEEditor = pbl;
            //Python = new PythonHandler(pbl,TextArea,OutputtextBox, griddatasource);

            Visutil = (BeepEnterprize.Vis.Module.IVisManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (BeepEnterprize.Vis.Module.IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "RootAppBranch").Any())
            {
                RootAppBranch = (BeepEnterprize.Vis.Module.IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
            }
            if (DMEEditor.Passedarguments.Objects.Where(c => c.Name == "RunTime").Any())
            {
                PythonRuntime = (PythonRunTime)e.Objects.Where(c => c.Name == "RunTime").FirstOrDefault().obj;

            }
            if (e.Objects.Where(c => c.Name == "PythonConfiguration").Any())
            {
                PythonConfig = (PythonConfiguration)e.Objects.Where(c => c.Name == "PythonConfiguration").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "CancellationToken").Any())
            {
                token = (CancellationToken)e.Objects.Where(c => c.Name == "CancellationToken").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "IProgress").Any())
            {
                progress = (IProgress<PassedArgs>)e.Objects.Where(c => c.Name == "IProgress").FirstOrDefault().obj;
            }
            if (PythonConfig==null)
            {
                PythonConfig = new PythonConfiguration();
            }
            Setup(DMEEditor, PythonConfig);
            
        }
     
        public PythonRunTime PythonRuntime { get; set; } = new PythonRunTime();
        public PythonConfiguration PythonConfig { get; set; } = new PythonConfiguration();
       
        private void init()
        {
            runtimesBindingSource = new BindingSource();
            dataGridView1.DataSource = runtimesBindingSource;
            this.GetFolderbutton.Click += GetFolderbutton_Click;
            this.Browserbutton.Click += Browserbutton_Click;
            this.Validatebutton.Click += Validate_Click;
            this.SetFolderbutton.Click += SetFolderbutton_Click;
            this.Cancelbutton.Click += Cancelbutton_Click;
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;
            this.BrowseOfflinebutton.Click += BrowseOfflinebutton_Click;
        }

        private void BrowseOfflinebutton_Click(object sender, EventArgs e)
        {
            string pathtoffolder = string.Empty;
            pathtoffolder= Visutil.Controlmanager.SelectFolderDialog();
            this.packageOfflinepathTextBox.Text = pathtoffolder;
        }

        public frm_PythonFolderManagement()
        {
            InitializeComponent();
            init();
            // this.RuntimecheckBox.
            // this.Folder32checkBox1.Checked = PythonRunTimeDiagnostics.FolderExist(fs._folderpath, BinType32or64.p395x32);
            // this.Folder64checkBox2.Checked = PythonRunTimeDiagnostics.FolderExist(fs._folderpath, BinType32or64.p395x64);
            // this.Python32checkBox3.Checked = PythonRunTimeDiagnostics.IsPythonInstalled(fs._folderpath, BinType32or64.p395x32);
            //this.Python64checkBox4.Checked = PythonRunTimeDiagnostics.IsPythonInstalled(fs._folderpath, BinType32or64.p395x64);
        }

        private void Cancelbutton_Click(object sender, EventArgs e)
        {
            PythonRuntime = new PythonRunTime();
            if (DMEEditor.Passedarguments.Objects.Where(c => c.Name == "RunTime").Any())
            {
                DMEEditor.Passedarguments.Objects.RemoveAt(DMEEditor.Passedarguments.Objects.FindIndex(c => c.Name == "RunTime"));

            }

           

            if (DMEEditor.Passedarguments.Objects.Where(c => c.Name == "PythonConfiguration").Any())
            {
                DMEEditor.Passedarguments.Objects.RemoveAt(DMEEditor.Passedarguments.Objects.FindIndex(c => c.Name == "PythonConfiguration"));

            }

            DMEEditor.ErrorObject.Flag= Errors.Failed;
            DMEEditor.ErrorObject.Message = "User Cancelled";
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            //if (dataGridView1.SelectedRows.Count == 0)
            //{
            //    MessageBox.Show("Please select a folder");
            //    return;
            //}
            //  FolderStructure fs = (FolderStructure)dataGridView1.SelectedRows[0].DataBoundItem;
            //  validateFolder(fs._folderpath);
        }

        private void SetFolderbutton_Click(object sender, EventArgs e)
        {
          
            if (runtimesBindingSource.Current != null)
            {
                PythonRuntime = (PythonRunTime)runtimesBindingSource.Current;
                int idx = PythonConfig.Runtimes.FindIndex(c => c.BinPath.Equals(PythonRuntime.BinPath, StringComparison.InvariantCultureIgnoreCase));
                PythonConfig.RunTimeIndex = idx;
                if (DMEEditor.Passedarguments.Objects.Where(c => c.Name == "RunTime").Any())
                {
                    DMEEditor.Passedarguments.Objects.RemoveAt(DMEEditor.Passedarguments.Objects.FindIndex(c => c.Name == "RunTime"));

                }

                DMEEditor.Passedarguments.Objects.Add(new ObjectItem() { Name = "RunTime", obj = PythonRuntime });
              
                if (DMEEditor.Passedarguments.Objects.Where(c => c.Name == "PythonConfiguration").Any())
                {
                    DMEEditor.Passedarguments.Objects.RemoveAt(DMEEditor.Passedarguments.Objects.FindIndex(c => c.Name == "PythonConfiguration"));

                }
                DMEEditor.Passedarguments.Objects.Add(new ObjectItem() { Name = "PythonConfiguration", obj = PythonConfig });
                DMEEditor.ErrorObject.Flag = Errors.Ok;
                DMEEditor.ErrorObject.Message = "PythonRuntime Selected";

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
                MessageBox.Show("Please select a folder");

        }

        private void Validate_Click(object sender, EventArgs e)
        {
            //if(dataGridView1.SelectedRows.Count == 0)
            //{
            //    MessageBox.Show("Please select a folder");
            //    return;
            //}
            //FolderStructure fs = (FolderStructure)dataGridView1.SelectedRows[0].DataBoundItem;

            //this.RuntimecheckBox.Checked = PythonRunTimeDiagnostics.IsFoldersExist(fs._folderpath);
            //this.Folder32checkBox1.Checked = PythonRunTimeDiagnostics.FolderExist(fs._folderpath, BinType32or64.p395x32);
            //this.Folder64checkBox2.Checked = PythonRunTimeDiagnostics.FolderExist(fs._folderpath, BinType32or64.p395x64);
            //this.Python32checkBox3.Checked = PythonRunTimeDiagnostics.IsPythonInstalled(fs._folderpath, BinType32or64.p395x32);
            //this.Python64checkBox4.Checked = PythonRunTimeDiagnostics.IsPythonInstalled(fs._folderpath, BinType32or64.p395x64);
        }

        private void Browserbutton_Click(object sender, EventArgs e)
        {
            
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.SelectedPath = txtRuntimePath.Text;
            folderBrowserDialog.ShowNewFolderButton = false;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtRuntimePath.Text = folderBrowserDialog.SelectedPath;
                txtRuntimePath.SelectionStart = 0;
                txtRuntimePath.SelectionLength = 0;
                GetFolders(txtRuntimePath.Text);
            }
        }

        private void GetFolderbutton_Click(object sender, EventArgs e)
        {
            GetFolders(txtRuntimePath.Text);
            GetRemovedFolders();
        }

        public void Setup(IDMEEditor dMEEditor, PythonConfiguration pythonConfig)
        {
            DMEEditor = dMEEditor;
            PythonConfig = pythonConfig;
            pythonConfigurationBindingSource.DataSource = pythonConfig;
            runtimesBindingSource.DataSource = PythonConfig.Runtimes;
            runtimesBindingSource.ResetBindings(false);
            dataGridView1.DataSource = runtimesBindingSource;
            if (PythonConfig != null)
            {
                if (PythonConfig.RunTimeIndex>0)
                {
                    if (!string.IsNullOrEmpty(PythonConfig.Runtimes[PythonConfig.RunTimeIndex].RuntimePath))
                    {

                        txtRuntimePath.Text = PythonConfig.Runtimes[PythonConfig.RunTimeIndex].RuntimePath;
                        txtRuntimePath.SelectionStart = 0;
                        txtRuntimePath.SelectionLength = 0;

                    }
                }
             
            }
        }
        public void GetFolders(string dirpath)
        {

            //List<FolderStructure> folders = new List<FolderStructure>();
            if (string.IsNullOrEmpty(dirpath))
            {
                MessageBox.Show("Please select a valid path");
                return;
            } 
            if (!Directory.Exists(dirpath))
            {
                MessageBox.Show("Please select a valid path");
                return;
            }
           
            //------------ check Directory
            if (PythonRunTimeDiagnostics.IsPythonInstalled(dirpath))
            {
                if (!PythonConfig.Runtimes.Any(x => x.BinPath.Equals(dirpath, StringComparison.InvariantCultureIgnoreCase)))
                {
                    PythonConfig.Runtimes.Add(PythonRunTimeDiagnostics.GetPythonConfig(dirpath));
                }
                else
                {
                    PythonRunTime runTimeConfig = PythonConfig.Runtimes.FirstOrDefault(x => x.BinPath.Equals(dirpath, StringComparison.InvariantCultureIgnoreCase));
                    if (runTimeConfig != null)
                    {
                        int idx = PythonConfig.Runtimes.IndexOf(runTimeConfig);
                        PythonConfig.Runtimes[idx] = PythonRunTimeDiagnostics.GetPythonConfig(dirpath);
                    }
                }
            }
            else
            {
                Visutil.Controlmanager.ShowAlert("Beep", "Could Not Find any Python Runtime at this folder", "warning.ico");
            }
            //string[] subdirectoryEntries = Directory.GetDirectories(dirpath);
            ////------------ check sub directotories
            //if (subdirectoryEntries.Length > 0)
            //{
               
            //    foreach (string d in subdirectoryEntries)
            //    {
            //        GetFolders(d);
            //    }
                  
            //}
            

          
            //
            runtimesBindingSource.DataSource = PythonConfig.Runtimes;
            runtimesBindingSource.ResetBindings(false);
            dataGridView1.DataSource = runtimesBindingSource;
        }
        public void GetRemovedFolders()
        {
            List<PythonRunTime> listtodel=new List<PythonRunTime>();
            PythonRunTime runTimeConfig;
            foreach (var item in PythonConfig.Runtimes)
            {
                bool notfound=false;
                if (string.IsNullOrEmpty(item.BinPath))
                {
                    notfound = true;
                }
                if (!Directory.Exists(item.BinPath))
                {
                    notfound = true;
                }
                if (!PythonRunTimeDiagnostics.IsPythonInstalled(item.BinPath))
                {
                    notfound = true;
                }
                if (notfound)
                {
                    if (!string.IsNullOrEmpty(item.BinPath))
                    {
                        runTimeConfig = PythonConfig.Runtimes.FirstOrDefault(x => x.BinPath.Equals(item.BinPath, StringComparison.InvariantCultureIgnoreCase));
                    }
                    else
                    {
                        runTimeConfig = PythonConfig.Runtimes.FirstOrDefault(x => x.ID.Equals(item.ID, StringComparison.InvariantCultureIgnoreCase));
                    }
                    
                    if (runTimeConfig != null)
                    {
                        listtodel.Add(runTimeConfig);
                     
                    }
                }
            } 
            if(MessageBox.Show("There is Paths that either not exist or missing Bin,Would you like to Delete them?","Python",MessageBoxButtons.OKCancel)== DialogResult.OK){
                foreach (var item in listtodel)
                {
                    int idx = PythonConfig.Runtimes.IndexOf(item);
                    PythonConfig.Runtimes.Remove(item);
                }
            }
            //
            runtimesBindingSource.DataSource = PythonConfig.Runtimes;
            runtimesBindingSource.ResetBindings(false);
            dataGridView1.DataSource = runtimesBindingSource;
        }

    }
}
