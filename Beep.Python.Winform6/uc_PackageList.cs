using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;

using System.Data;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;

using TheTechIdea.Beep.Winform.Controls.Basic;
using Beep.Python.RuntimeEngine.Services;



namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python Package List", Name = "uc_PackageList", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_PackageList : uc_Addin
    {
     
        public string AddinName { get; set; } = "Python Package List";
        public string Description { get; set; } = "Python Package List";
        public string ObjectName { get; set; }
        public string ObjectType { get; set; } = "UserControl";
     

       // public IPIPManager pIPManager { get; set; }
      
      //  public IDEManager iDEManager { get; set; }
    

        IBranch RootAppBranch;
        IBranch branch;



        // PythonNetRunTimeManager pythonRunTimeManager;

        private IPythonRunTimeManager PythonRunTimeManager;
     
        public IPackageManagerViewModel Pythonpackagemanager { get; private set; }

        private BindingSource bs = new BindingSource();

        CancellationTokenSource tokenSource;

        Progress<PassedArgs> progress;
        CancellationToken token;
        PythonBaseViewModel pythonBaseViewModel;
        public void Run(IPassedArgs Passedarg)
        {

        }

        public override void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            base.SetConfig(pbl, plogger, putil, args, e, per);
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
          
            PythonRunTimeManager = DMEEditor.GetPythonRunTimeManager();
            Pythonpackagemanager = DMEEditor.GetPythonPackageManager();
            pythonBaseViewModel = (PythonBaseViewModel)Pythonpackagemanager;
          //  PythonRunTimeManager.PackageManager = Pythonpackagemanager;


            Visutil = (IVisManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "RootAppBranch").Any())
            {
                RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "CancellationToken").Any())
            {
                token = (CancellationToken)e.Objects.Where(c => c.Name == "CancellationToken").FirstOrDefault().obj;
            }
            bs.DataSource = null;
            bs.DataSource = Pythonpackagemanager.Packages;
            packagelistDataGridView.DataSource = null;
            packagelistDataGridView.DataSource = bs;
            tokenSource = new CancellationTokenSource();
            token = tokenSource.Token;
            setupmaxmin();
            PythonRunTimeManager.IsBusy = false;
            // packagelistBindingSource.DataSource = PythonRunTimeManager.CurrentRuntimeConfig.Packagelist;

            RunRefresh();
        }
        private void setupmaxmin()
        {
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = PythonRunTimeManager.CurrentRuntimeConfig.Packagelist.Count();
        }
        void StopTask()
        {
            // Attempt to cancel the task politely
            isstopped = true;
            isloading = false;
            isfinish = false;
            tokenSource.Cancel();
            // MessageBox.Show("Job Stopped");

        }
        private  void RunRefresh()
        {
            pythonBaseViewModel.Progress = new Progress<PassedArgs>(percent =>
            {
                //progressBar1.CustomText = percent.ParameterInt1 + " out of " + percent.ParameterInt2;
                toolStripProgressBar1.Maximum= Pythonpackagemanager.Packages.Count;
                toolStripProgressBar1.Value = percent.ParameterInt1;
                //if (Visutil.IsShowingWaitForm)
                //{
                //    Visutil.ShowWaitForm(new PassedArgs() { Messege = percent.Messege });
                //}
                //Visutil.PasstoWaitForm(new PassedArgs() { Messege = percent.Messege });
                MessageLabel.Text= percent.Messege;
                RefreshUI();
                //if (percent.EventType == "Update" && DMEEditor.ErrorObject.Flag == Errors.Failed)
                //{
                //    update(percent.Messege);
                //}
                if (!string.IsNullOrEmpty(percent.EventType))
                {
                    if (percent.EventType == "Stop")
                    {
                        tokenSource.Cancel();
                    }
                }
            });
            CancellationTokenRegistration ctr = pythonBaseViewModel.Token.Register(() => StopTask());
            
          
            //    Action action =
            //() =>
            //    MessageBox.Show("Start");
            //await Task.Run(() =>
            //{
               

            //    if (!isstopped)
            //    {
            //        MessageBox.Show("Finish");
            //    }

            //});
        }
        private bool cellFormattingInProgress = false;
        private bool isstopped;
        private bool isloading;
        private bool isfinish;

        public uc_PackageList()
        {
            InitializeComponent();
            
            packagelistDataGridView.Columns["imageColumn"].DefaultCellStyle.NullValue = null;
            this.packagelistDataGridView.CellContentClick += PackagelistDataGridView_CellContentClick;
            this.packagelistDataGridView.CellValueChanged += PackagelistDataGridView_CellValueChanged;
            this.packagelistDataGridView.CellFormatting += PackagelistDataGridView_CellFormatting;
            this.packagelistDataGridView.DataError += PackagelistDataGridView_DataError;
            this.InstallNewPackagetoolStripButton.Click += InstallNewPackagetoolStripButton_Click;
            this.RefreshtoolStripButton.Click += RefreshtoolStripButton_Click;
            this.InstallPIPtoolStripButton.Click += InstallPIPtoolStripButton_Click;
            this.packagelistBindingNavigatorSaveItem.Click += PackagelistBindingNavigatorSaveItem_Click;

        }

        private void PackagelistBindingNavigatorSaveItem_Click(object? sender, EventArgs e)
        {
            PythonRunTimeManager.SaveConfig();
        }

        private void InstallPIPtoolStripButton_Click(object sender, EventArgs e)
        {
            if (!PythonRunTimeManager.IsInitialized)

                return;
            if (PythonRunTimeManager.IsBusy)
            {
                MessageBox.Show("Please wait until the current operation is finished");
                return;
            }

            Pythonpackagemanager.InstallPipToolAsync();

            
            PythonRunTimeManager.IsBusy = false;
        }

        private void RefreshtoolStripButton_Click(object sender, EventArgs e)
        {
            if (!PythonRunTimeManager.IsInitialized)

                return;
            if (PythonRunTimeManager.IsBusy)
            {
                MessageBox.Show("Please wait until the current operation is finished");
                return;
            }
            // Refresh();
            Pythonpackagemanager.RefreshAllPackagesAsync();
            RefreshUI();
            PythonRunTimeManager.SaveConfig();


        }

        private void InstallNewPackagetoolStripButton_Click(object sender, EventArgs e)
        {
            if (!PythonRunTimeManager.IsInitialized)

                return;
            if (PythonRunTimeManager.IsBusy)
            {
                MessageBox.Show("Please wait until the current operation is finished");
                return;
            }
            if (!string.IsNullOrEmpty(this.NewPackagetoolStripTextBox.Text))
            {
                var retval=Pythonpackagemanager.InstallNewPackageAsync(this.NewPackagetoolStripTextBox.Text);
                

                
              
            }
           
        }

        private void PackagelistDataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            e.Cancel = true;
        }

        private void PackagelistDataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (cellFormattingInProgress)
            {
                return;
            }
            if (packagelistDataGridView.Rows[e.RowIndex].IsNewRow)
            {
                return;
            }
            if (e.RowIndex >= 0 && packagelistDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
            {

                bool installed = (bool)packagelistDataGridView.Rows[e.RowIndex].Cells["installedDataGridViewCheckBoxColumn"].Value;
                cellFormattingInProgress = true;

                this.packagelistDataGridView.CellValueChanged -= PackagelistDataGridView_CellValueChanged;
                this.packagelistDataGridView.CellFormatting -= PackagelistDataGridView_CellFormatting;
                if (packagelistDataGridView.Columns[e.ColumnIndex].Name == "packagenameDataGridViewTextBoxColumn")
                {

                    packagelistDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly = installed;
                }
                if (packagelistDataGridView.Columns[e.ColumnIndex].Name == "packagetitleDataGridViewTextBoxColumn")
                {

                    packagelistDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly = installed;
                }
                //packagetitleDataGridViewTextBoxColumn
                DataGridViewImageCell cell = (DataGridViewImageCell)
                packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["imageColumn"].Index];
                DataGridViewButtonCell but = (DataGridViewButtonCell)
                    packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["UpDateInstallGridButton"].Index];
                but.UseColumnTextForButtonValue = true;
                if (installed)
                {
                    // Set the green circle picture
                   Bitmap bmp = Beep.Python.WinformCore.Properties.Resources.FlagDarkGreen;
                    cell.Value = bmp;

                    //  but.Value = "Status";
                }
                else
                {
                    // Set the red circle picture
                   Bitmap bmp = Beep.Python.WinformCore.Properties.Resources.FlagRed;
                    cell.Value = bmp;
                    //  but.Value = "Not";
                }
                DataGridViewTextBoxCell latestversion = (DataGridViewTextBoxCell)
                    packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["versionDataGridViewTextBoxColumn"].Index];
                DataGridViewTextBoxCell version = (DataGridViewTextBoxCell)
                  packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["updateversionDataGridViewTextBoxColumn"].Index];
                if (version.Value != null && latestversion.Value != null)
                {
                    if (!latestversion.Value.ToString().Equals(version.Value.ToString()))
                    {
                        latestversion.Style.BackColor = System.Drawing.Color.Red;
                    }
                    else
                        latestversion.Style.BackColor = Color.Green;
                }
                else
                    latestversion.Style.BackColor = Color.Green;

            }

            cellFormattingInProgress = false;
        }

      
     
        public void Setup(IDMEEditor dMEEditor, IPythonRunTimeManager pythonRunTimeManager, Progress<PassedArgs> progress,
        CancellationToken token)
        {
            if (!pythonRunTimeManager.IsInitialized)

                return;
            this.progress = progress;
            this.token = token;
            DMEEditor = dMEEditor;
            PythonRunTimeManager = pythonRunTimeManager;
            if (PythonRunTimeManager.CurrentRuntimeConfig.Packagelist.Count == 0)
            {
                RunRefresh();
            }
            else RefreshUI();
            // refersh();
        }
        private async Task<bool> RefershAsync()
        {
            bool retval = false;
            if (PythonRunTimeManager == null)
            {
               // MessageBox.Show("Python runtime manager is not available.");
                return retval;
            }

            if (!PythonRunTimeManager.IsInitialized)
            {
              //  MessageBox.Show("Python runtime is not initialized.");
                return retval;
            }

            if (PythonRunTimeManager.IsBusy)
            {
               // MessageBox.Show("Please wait until the current operation is finished");
                return retval;
            }

            try
            {
                // Visutil.ShowWaitForm(new PassedArgs() { Messege = "Refreshing Status Packages" });
                Pythonpackagemanager.RefreshAllPackagesAsync();
                RefreshUI();
                return true;
                // Visutil.CloseWaitForm();
            }
            catch (Exception ex)
            {
              
                return retval;
                // Visutil.CloseWaitForm();
            }

        }
        
        private void RefreshUI()
        {
            if (!PythonRunTimeManager.IsInitialized)

                return;
            if (PythonRunTimeManager.IsBusy)
            {
                MessageBox.Show("Please wait until the current operation is finished");
                return;
            }
            bs.DataSource = null;
            bs.DataSource = Pythonpackagemanager.Packages;
            packagelistDataGridView.DataSource = null;
            packagelistDataGridView.DataSource = bs;
            packagelistDataGridView.Refresh();
        }
      
        private async void PackagelistDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == packagelistDataGridView.Columns["UpDateInstallGridButton"].Index && e.RowIndex >= 0)
            {
                // Button was clicked, do something here
                PackageDefinition package = (PackageDefinition)bs.Current;
                if (package != null)
                {
                    if (MessageBox.Show("Would like to upgrade the package " + package.PackageName + "?", "Upgrade", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        Pythonpackagemanager.UpgradePackageAsync(package.PackageName);
                        RefreshUI();
                    }

                    // refersh();
                }
            }
        }
        private void PackagelistDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {


            if (e.ColumnIndex == packagelistDataGridView.Columns["InstalleddataGridViewCheckBoxColumn1"].Index && e.RowIndex >= 0)
            {
                bool installed = (bool)packagelistDataGridView.Rows[e.RowIndex].Cells["InstalleddataGridViewCheckBoxColumn1"].Value;
                packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["NamedataGridViewTextBoxColumn2"].Index].ReadOnly = !installed;
                DataGridViewImageCell cell = (DataGridViewImageCell)
                packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["imageColumn"].Index];
                if (cell != null)
                {
                    if (installed)
                    {
                        // Set the green circle picture
                        Bitmap bmp = Beep.Python.WinformCore.Properties.Resources.FlagDarkGreen;
                        cell.Value = bmp;
                    }
                    else
                    {
                        // Set the red circle picture
                        Bitmap bmp = Beep.Python.WinformCore.Properties.Resources.FlagRed;
                        cell.Value = bmp;
                    }
                }
                // Re-attach the event handler
            }
            if (e.ColumnIndex == packagelistDataGridView.Columns["NamedataGridViewTextBoxColumn2"].Index && e.RowIndex >= 0)
            {
                // Text was changed, do something here
            }

            //switch (e.ColumnIndex)
            //{   case 0:
            //        DataGridViewTextBoxCell txt = (DataGridViewTextBoxCell)packagelistDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
            //        break;
            //    case 1:
            //        DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)packagelistDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
            //        if (chk.Value == null)
            //        {
            //            chk.Value = false;
            //        }
            //        else
            //        {
            //            chk.Value = true;
            //        }
            //        break;
            //    case 2:
            //        break;
            //    default:
            //        break;
            //}

        }

    }
}
