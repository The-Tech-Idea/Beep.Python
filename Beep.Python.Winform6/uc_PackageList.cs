using Beep.Python.Model;
using BeepEnterprize.Vis.Module;
using System.Data;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;


namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python Package List", Name = "uc_PackageList", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_PackageList : UserControl,IDM_Addin
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
      
      //  public IDEManager iDEManager { get; set; }
        public IVisManager Visutil { get; set; }


        IBranch RootAppBranch;
        IBranch branch;

       

       // PythonNetRunTimeManager pythonRunTimeManager;


     
        IProgress<PassedArgs> progress;
        CancellationToken token;
      //  frm_PythonFolderManagement pythonFolderManagement;
        public void Run(IPassedArgs Passedarg)
        {

        }

        public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            Passedarg = e;
            Logger = plogger;
            ErrorObject = per;
   
           
            Visutil = (IVisManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;

            if (e.Objects.Where(c => c.Name == "Branch").Any())
            {
                branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "RootAppBranch").Any())
            {
                RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "IPythonRunTimeManager").Any())
            {
                PythonRunTimeManager = (IPythonRunTimeManager)e.Objects.Where(c => c.Name == "IPythonRunTimeManager").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "CancellationToken").Any())
            {
                token = (CancellationToken)e.Objects.Where(c => c.Name == "CancellationToken").FirstOrDefault().obj;
            }
            if (e.Objects.Where(c => c.Name == "IProgress").Any())
            {
                progress = (IProgress<PassedArgs>)e.Objects.Where(c => c.Name == "IProgress").FirstOrDefault().obj;
            }
            if (PythonRunTimeManager.IsInitialized)
            {
                Setup(DMEEditor, PythonRunTimeManager, progress, token);
            }
            else
                return;
            

        }
        private bool cellFormattingInProgress = false;
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
       
                PythonRunTimeManager.InstallPIP(progress, token).ConfigureAwait(true);

            
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
            refersh();
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
                var retval=PythonRunTimeManager.InstallPackage(this.NewPackagetoolStripTextBox.Text, progress, token).ConfigureAwait(true);
                

                
              
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

                bool installed = (bool)packagelistDataGridView.Rows[e.RowIndex].Cells["InstalleddataGridViewCheckBoxColumn1"].Value;
                cellFormattingInProgress = true;

                this.packagelistDataGridView.CellValueChanged -= PackagelistDataGridView_CellValueChanged;
                this.packagelistDataGridView.CellFormatting -= PackagelistDataGridView_CellFormatting;
                if (packagelistDataGridView.Columns[e.ColumnIndex].Name == "NamedataGridViewTextBoxColumn2")
                {

                    packagelistDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly = installed;
                }

                DataGridViewImageCell cell = (DataGridViewImageCell)
                packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["imageColumn"].Index];
                DataGridViewButtonCell but = (DataGridViewButtonCell)
                    packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["UpDateInstallGridButton"].Index];
                but.UseColumnTextForButtonValue = true;
                if (installed)
                {
                    // Set the green circle picture
                    Bitmap bmp = Properties.Resources.FlagDarkGreen;
                    cell.Value = bmp;

                    //  but.Value = "Installed";
                }
                else
                {
                    // Set the red circle picture
                    Bitmap bmp = Properties.Resources.FlagRed;
                    cell.Value = bmp;
                    //  but.Value = "Not";
                }
                DataGridViewTextBoxCell latestversion = (DataGridViewTextBoxCell)
                    packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["updateversion"].Index];
                DataGridViewTextBoxCell version = (DataGridViewTextBoxCell)
                  packagelistDataGridView.Rows[e.RowIndex].Cells[packagelistDataGridView.Columns["VersiondataGridViewTextBoxColumn6"].Index];
                if (version.Value != null && latestversion.Value != null)
                {
                    if (!latestversion.Value.ToString().Equals(version.Value.ToString()))
                    {
                        latestversion.Style.BackColor = Color.Red;
                    }
                    else
                        latestversion.Style.BackColor = Color.Green;
                }
                else
                    latestversion.Style.BackColor = Color.Green;

            }

            cellFormattingInProgress = false;
        }

      
        private IPythonRunTimeManager PythonRunTimeManager;
        private BindingSource bs = new BindingSource();

     
        public void Setup(IDMEEditor dMEEditor, IPythonRunTimeManager pythonRunTimeManager, IProgress<PassedArgs> progress,
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
                refersh();
            }
            else RefreshUI();
            // refersh();
        }
        private void refersh()
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
                PythonRunTimeManager.RefreshInstalledPackagesList(progress, token).ConfigureAwait(true);
                RefreshUI();
                PythonRunTimeManager.SaveConfig();


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
            bs.DataSource = PythonRunTimeManager.CurrentRuntimeConfig.Packagelist;
            packagelistDataGridView.DataSource = null;
            packagelistDataGridView.DataSource = bs;
            packagelistDataGridView.Refresh();
        }
        public void SetDataSource(BindingSource bs)
        {
            packagelistDataGridView.DataSource = bs;
        }
        private void PackagelistDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == packagelistDataGridView.Columns["UpDateInstallGridButton"].Index && e.RowIndex >= 0)
            {
                // Button was clicked, do something here
                PackageDefinition package = (PackageDefinition)bs.Current;
                if (package != null)
                {
                    if (MessageBox.Show("Would like to upgrade the package " + package.packagename + "?", "Upgrade", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        PythonRunTimeManager.UpdatePackage(package.packagename, progress, token).ConfigureAwait(true);
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
                        Bitmap bmp = Properties.Resources.FlagDarkGreen;
                        cell.Value = bmp;
                    }
                    else
                    {
                        // Set the red circle picture
                        Bitmap bmp = Properties.Resources.FlagRed;
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
