using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;
using TheTechIdea.Beep.DataBase;
using System.Reflection;
using TheTechIdea.Beep.Container.Services;
using Beep.Python.RuntimeEngine.Helpers;


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
        public IDMEEditor Editor { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string EntityName { get; set; }
        public IPassedArgs Passedarg { get; set; }

       // public IPIPManager pIPManager { get; set; }

     //   public IDEManager iDEManager { get; set; }
        public IAppManager Visutil { get; set; }


       IBranch RootAppBranch;
       IBranch branch;



      //  PythonNetRunTimeManager pythonRunTimeManager;


        BindingSource griddatasource = new BindingSource();
        IProgress<PassedArgs> progress;
        CancellationToken token;
       // frm_PythonFolderManagement pythonFolderManagement;
      

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
            Editor = pbl;
            //Python = new PythonHandler(pbl,TextArea,OutputtextBox, griddatasource);

            Visutil = (IAppManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "RootAppBranch").Any())
            {
                RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
            }
            if (Editor.Passedarguments.Objects.Where(c => c.Name == "RunTime").Any())
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
            Setup(Editor, PythonConfig);
            
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
            pathtoffolder= Visutil.DialogManager.SelectFolderDialog();
            this.packageOfflinepathTextBox.Text = pathtoffolder;
        }

        public frm_PythonFolderManagement()
        {
            InitializeComponent();
            init();
            Details.AddinName = "Python Folder List";
            // this.RuntimecheckBox.
            // this.Folder32checkBox1.Checked = PythonRunTimeDiagnostics.FolderExist(fs.Folderpath, BinType32or64.p395x32);
            // this.Folder64checkBox2.Checked = PythonRunTimeDiagnostics.FolderExist(fs.Folderpath, BinType32or64.p395x64);
            // this.Python32checkBox3.Checked = PythonRunTimeDiagnostics.IsPythonInstalled(fs.Folderpath, BinType32or64.p395x32);
            //this.Python64checkBox4.Checked = PythonRunTimeDiagnostics.IsPythonInstalled(fs.Folderpath, BinType32or64.p395x64);
        }

        private void Cancelbutton_Click(object sender, EventArgs e)
        {
            PythonRuntime = new PythonRunTime();
            if (Editor.Passedarguments.Objects.Where(c => c.Name == "RunTime").Any())
            {
                Editor.Passedarguments.Objects.RemoveAt(Editor.Passedarguments.Objects.FindIndex(c => c.Name == "RunTime"));

            }

           

            if (Editor.Passedarguments.Objects.Where(c => c.Name == "PythonConfiguration").Any())
            {
                Editor.Passedarguments.Objects.RemoveAt(Editor.Passedarguments.Objects.FindIndex(c => c.Name == "PythonConfiguration"));

            }

            Editor.ErrorObject.Flag= Errors.Failed;
            Editor.ErrorObject.Message = "User Cancelled";
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
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
            //  validateFolder(fs.Folderpath);
        }

        private void SetFolderbutton_Click(object sender, EventArgs e)
        {
          
            if (runtimesBindingSource.Current != null)
            {
                PythonRuntime = (PythonRunTime)runtimesBindingSource.Current;
                int idx = PythonConfig.Runtimes.FindIndex(c => c.BinPath.Equals(PythonRuntime.BinPath, StringComparison.InvariantCultureIgnoreCase));
                PythonConfig.RunTimeIndex = idx;
                if (Editor.Passedarguments.Objects.Where(c => c.Name == "RunTime").Any())
                {
                    Editor.Passedarguments.Objects.RemoveAt(Editor.Passedarguments.Objects.FindIndex(c => c.Name == "RunTime"));

                }

                Editor.Passedarguments.Objects.Add(new ObjectItem() { Name = "RunTime", obj = PythonRuntime });
              
                if (Editor.Passedarguments.Objects.Where(c => c.Name == "PythonConfiguration").Any())
                {
                    Editor.Passedarguments.Objects.RemoveAt(Editor.Passedarguments.Objects.FindIndex(c => c.Name == "PythonConfiguration"));

                }
                Editor.Passedarguments.Objects.Add(new ObjectItem() { Name = "PythonConfiguration", obj = PythonConfig });
                Editor.ErrorObject.Flag = Errors.Ok;
                Editor.ErrorObject.Message = "PythonRuntime Selected";

                this.DialogResult = System.Windows.Forms.DialogResult.OK;
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

            //this.RuntimecheckBox.Checked = PythonRunTimeDiagnostics.IsFoldersExist(fs.Folderpath);
            //this.Folder32checkBox1.Checked = PythonRunTimeDiagnostics.FolderExist(fs.Folderpath, BinType32or64.p395x32);
            //this.Folder64checkBox2.Checked = PythonRunTimeDiagnostics.FolderExist(fs.Folderpath, BinType32or64.p395x64);
            //this.Python32checkBox3.Checked = PythonRunTimeDiagnostics.IsPythonInstalled(fs.Folderpath, BinType32or64.p395x32);
            //this.Python64checkBox4.Checked = PythonRunTimeDiagnostics.IsPythonInstalled(fs.Folderpath, BinType32or64.p395x64);
        }

        private void Browserbutton_Click(object sender, EventArgs e)
        {
            
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.SelectedPath = txtRuntimePath.Text;
            folderBrowserDialog.ShowNewFolderButton = false;
            if (folderBrowserDialog.ShowDialog()== System.Windows.Forms.DialogResult.OK )
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
            Editor = dMEEditor;
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
                Visutil.DialogManager.ShowAlert("Beep", "Could Not Find any Python Runtime at this folder", "warning.ico");
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
            if(MessageBox.Show("There is Paths that either not exist or missing Bin,Would you like to Delete them?","Python",MessageBoxButtons.OKCancel)== System.Windows.Forms.DialogResult.OK){
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

       
        #region "IDM_Addin Implementation"
        private readonly IBeepService? beepService;

        //protected EnumBeepThemes _themeEnum = EnumBeepThemes.DefaultTheme;
        //protected BeepTheme _currentTheme = BeepThemesManager.DefaultTheme;
        //[Browsable(true)]
        //public EnumBeepThemes Theme
        //{
        //    get => _themeEnum;
        //    set
        //    {
        //        _themeEnum = value;
        //        _currentTheme = BeepThemesManager.GetTheme(value);
        //        //      this.ApplyTheme();
        //        ApplyTheme();
        //    }
        //}
        //private void BeepThemesManager_ThemeChanged(object? sender, ThemeChangeEventsArgs e)
        //{
        //    Theme = e.NewTheme;
        //}

        public AddinDetails Details { get; set; }
        public Dependencies Dependencies { get; set; }
        public string GuidID { get; set; }

        public event EventHandler OnStart;
        public event EventHandler OnStop;
        public event EventHandler<ErrorEventArgs> OnError;


        public virtual void Configure(Dictionary<string, object> settings)
        {

        }

        public virtual void Dispose()
        {

        }

        public virtual string GetErrorDetails()
        {
            // if error occured return the error details
            // create error messege sring 
            string errormessage = "";
            if (Editor.ErrorObject != null)
            {
                if (Editor.ErrorObject.Errors.Count > 0)
                {
                    foreach (var item in Editor.ErrorObject.Errors)
                    {
                        errormessage += item.Message + "\n";
                    }
                }
            }

            return errormessage;
        }

        public virtual void Initialize()
        {

        }

        public virtual void OnNavigatedTo(Dictionary<string, object> parameters)
        {
           // if (Theme != BeepThemesManager.CurrentTheme) { Theme = BeepThemesManager.CurrentTheme; }
        }

        public virtual void Resume()
        {

        }

        public virtual void Run(IPassedArgs pPassedarg)
        {

        }

        public virtual void Run(params object[] args)
        {

        }

        public virtual Task<IErrorsInfo> RunAsync(IPassedArgs pPassedarg)
        {
            try
            {

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name; // Retrieves "PrintGrid"
                Editor.AddLogMessage("Beep", $"in {methodName} Error : {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return Task.FromResult(Editor.ErrorObject);
        }

        public virtual Task<IErrorsInfo> RunAsync(params object[] args)
        {
            try
            {

            }
            catch (Exception ex)
            {
                string methodName = MethodBase.GetCurrentMethod().Name; // Retrieves "PrintGrid"
                Editor.AddLogMessage("Beep", $"in {methodName} Error : {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
            }
            return Task.FromResult(Editor.ErrorObject);
        }

        public virtual void SetError(string message)
        {

        }

        public virtual void Suspend()
        {

        }
        public void ApplyTheme()
        {
            foreach (Control item in this.Controls)
            {
                // check if item is a usercontrol
                if (item is IBeepUIComponent)
                {
                    // apply theme to usercontrol
                 //   ((IBeepUIComponent)item).Theme = Theme;
                    // ((IBeepUIComponent)item).ApplyTheme();

                }
            }

        }
        #endregion "IDM_Addin Implementation"
    }
}
