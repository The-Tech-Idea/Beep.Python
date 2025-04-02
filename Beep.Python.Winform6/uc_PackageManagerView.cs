using Beep.Python.Model;
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

using Beep.Python.RuntimeEngine.ViewModels;
using TheTechIdea.Beep.Container;
using Beep.Python.RuntimeEngine.Services;
using System.ComponentModel;
using System.Reflection;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.Winform
{
    public partial class uc_PackageManagerView : UserControl,IDM_Addin
    {
        private PythonBaseViewModel pythonBaseViewModel;
        IDMEEditor editor;
        public uc_PackageManagerView()
        {
            InitializeComponent();
            Details.AddinName = "Python Package Manager";

        }

        public string ParentName { get  ; set  ; }
        public string ObjectName { get  ; set  ; }
        public string ObjectType { get  ; set  ; }
        public string AddinName { get  ; set  ; }
        public string Description { get  ; set  ; }
        public bool DefaultCreate { get  ; set  ; }
        public string DllPath { get  ; set  ; }
        public string DllName { get  ; set  ; }
        public string NameSpace { get  ; set  ; }
        public IErrorsInfo ErrorObject { get  ; set  ; }
        public IDMLogger Logger { get  ; set  ; }
        public IDMEEditor Editor { get  ; set  ; }
        public EntityStructure EntityStructure { get  ; set  ; }
        public string EntityName { get  ; set  ; }
        public IPassedArgs Passedarg { get  ; set  ; }
  
        IPackageManagerViewModel  packageManager;
        IPythonRunTimeManager PythonRunTimeManager;
        IAppManager Visutil;
        IProgress<PassedArgs> progress;
        CancellationToken token;
   

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            editor = pbl;
            ErrorObject = pbl.ErrorObject;
            Logger = pbl.Logger;
            Passedarg = e;
            Editor = pbl;
            Visutil = (IAppManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;


            PythonRunTimeManager = Editor.GetPythonRunTimeManager();
            packageManager = new PackageManagerViewModel(Editor.GetBeepService(),PythonRunTimeManager);
            packageManager.Editor = Editor;
            pythonBaseViewModel = (PythonBaseViewModel)packageManager;
          
            bindingSource1.DataSource= packageManager;
        }
        private void setupmaxmin()
        {
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = packageManager.Packages.Count;
        }
        private void setupprogressbar()
        {

            progress = new Progress<PassedArgs>(percent =>
            {
                //progressBar1.CustomText = percent.ParameterInt1 + " out of " + percent.ParameterInt2;
                toolStripProgressBar1.Maximum = packageManager.Packages.Count;
                toolStripProgressBar1.Value = percent.ParameterInt1;
                if (Visutil.IsShowingWaitForm)
                {
                    Visutil.ShowWaitForm(new PassedArgs() { Messege = percent.Messege });
                }

                MessageLabel.Text = percent.Messege;
                //if (percent.EventType == "Update" && Editor.ErrorObject.Flag == Errors.Failed)
                //{
                //    update(percent.Messege);
                //}
                if (!string.IsNullOrEmpty(percent.EventType))
                {
                    if (percent.EventType == "Stop")
                    {
                        token.ThrowIfCancellationRequested();
                    }
                }
            });
        }
        private void beepGrid1_Load(object sender, EventArgs e)
        {

        }
        private async void refersh()
        {
            if (!PythonRunTimeManager.IsInitialized)

                return;
            if (PythonRunTimeManager.IsBusy)
            {
                MessageBox.Show("Please wait until the current operation is finished");
                return;
            }
            if (PythonRunTimeManager != null)
            {
                Visutil.ShowWaitForm(new PassedArgs() { Messege = "Refreshing Status Packages" });
                packageManager.RefreshAllPackagesAsync();
                PythonRunTimeManager.SaveConfig();
                Visutil.CloseWaitForm();

            }

        }

        #region "IDM_Addin Implementation"
        private readonly IBeepService? beepService;

        protected EnumBeepThemes _themeEnum = EnumBeepThemes.DefaultTheme;
        protected BeepTheme _currentTheme = BeepThemesManager.DefaultTheme;
        [Browsable(true)]
    
        public EnumBeepThemes Theme
        {
            get => _themeEnum;
            set
            {
                _themeEnum = value;
                _currentTheme = BeepThemesManager.GetTheme(value);
                //      this.ApplyTheme();
                ApplyTheme();
            }
        }
        private void BeepThemesManager_ThemeChanged(object? sender, ThemeChangeEventsArgs e)
        {
            Theme = e.NewTheme;
        }

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
            if (Theme != BeepThemesManager.CurrentTheme) { Theme = BeepThemesManager.CurrentTheme; }
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
                    ((IBeepUIComponent)item).Theme = Theme;
                    // ((IBeepUIComponent)item).ApplyTheme();

                }
            }

        }
        #endregion "IDM_Addin Implementation"
    }
}
