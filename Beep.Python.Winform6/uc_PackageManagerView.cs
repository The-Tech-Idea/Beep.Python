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
using TheTechIdea.Beep.Winform.Controls.Models;
using TheTechIdea.Beep.Winform.Controls;
using TheTechIdea.Beep.Winform.Default.Views.Template;

namespace Beep.Python.Winform
{
    public partial class uc_PackageManagerView : TemplateUserControl
    {
        private PythonBaseViewModel pythonBaseViewModel;
       
        public uc_PackageManagerView(IBeepService beepService) : base(beepService)
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
          
            ErrorObject = pbl.ErrorObject;
            Logger = pbl.Logger;
            Passedarg = e;
            Editor = pbl;
            Visutil = (IAppManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

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


        public override void Configure(Dictionary<string, object> settings)
        {
            base.Configure(settings);


            PythonRunTimeManager = Editor.GetPythonRunTimeManager();
            packageManager = new PackageManagerViewModel(beepService, PythonRunTimeManager);
            packageManager.Editor = Editor;
            pythonBaseViewModel = (PythonBaseViewModel)packageManager;

            bindingSource1.DataSource = packageManager;

        }
        public override void OnNavigatedTo(Dictionary<string, object> parameters)
        {
            base.OnNavigatedTo(parameters);
          
        }
    }
}
