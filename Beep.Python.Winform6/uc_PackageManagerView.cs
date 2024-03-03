using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using System.Data;
using BeepEnterprize.Vis.Module;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Logger;
using TheTechIdea.Util;

namespace Beep.Python.Winform
{
    public partial class uc_PackageManagerView : UserControl,IDM_Addin
    {
        IDMEEditor editor;
        public uc_PackageManagerView()
        {
            InitializeComponent();

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
        public IDMEEditor DMEEditor { get  ; set  ; }
        public EntityStructure EntityStructure { get  ; set  ; }
        public string EntityName { get  ; set  ; }
        public IPassedArgs Passedarg { get  ; set  ; }

        IPackageManagerViewModel  packageManager;
        IPythonRunTimeManager PythonRunTimeManager;
        IVisManager Visutil;
        IProgress<PassedArgs> progress;
        CancellationToken token;
        public void Run(IPassedArgs pPassedarg)
        {
           
        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            packageManager= DMEEditor.GetPythonPackageManager();
            PythonRunTimeManager= DMEEditor.GetPythonRunTimeManager();
            editor = pbl;
            ErrorObject = pbl.ErrorObject;
            Logger = pbl.Logger;
            Passedarg = e;
            DMEEditor = pbl;
            Visutil = (IVisManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
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
                //if (percent.EventType == "Update" && DMEEditor.ErrorObject.Flag == Errors.Failed)
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
                Visutil.ShowWaitForm(new PassedArgs() { Messege = "Refreshing Installed Packages" });
                await PythonRunTimeManager.RefreshInstalledPackagesList(progress, token);
                PythonRunTimeManager.SaveConfig();
                Visutil.CloseWaitForm();

            }

        }
    }
}
