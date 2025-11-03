namespace Beep.Python.Winform.PackageManagement
{
    partial class uc_Packages
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tableLayoutMain = new System.Windows.Forms.TableLayoutPanel();
            flowHeader = new System.Windows.Forms.FlowLayoutPanel();
            lblPackageSet = new System.Windows.Forms.Label();
            comboPackageSet = new System.Windows.Forms.ComboBox();
            lblEnvironment = new System.Windows.Forms.Label();
            comboEnvironment = new System.Windows.Forms.ComboBox();
            btnRefreshEnvironments = new System.Windows.Forms.Button();
            btnSelectAll = new System.Windows.Forms.Button();
            btnClearSelection = new System.Windows.Forms.Button();
            btnInstallSelected = new System.Windows.Forms.Button();
            lblSelectedCount = new System.Windows.Forms.Label();
            txtSetDescription = new System.Windows.Forms.TextBox();
            checkedListPackages = new System.Windows.Forms.CheckedListBox();
            panelInstallStatus = new System.Windows.Forms.FlowLayoutPanel();
            progressInstall = new System.Windows.Forms.ProgressBar();
            btnCancelInstall = new System.Windows.Forms.Button();
            lblInstallStatus = new System.Windows.Forms.Label();
            lstLog = new System.Windows.Forms.ListBox();
            tableLayoutMain.SuspendLayout();
            flowHeader.SuspendLayout();
            panelInstallStatus.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutMain
            // 
            tableLayoutMain.ColumnCount = 1;
            tableLayoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutMain.Controls.Add(flowHeader, 0, 0);
            tableLayoutMain.Controls.Add(txtSetDescription, 0, 1);
            tableLayoutMain.Controls.Add(checkedListPackages, 0, 2);
            tableLayoutMain.Controls.Add(panelInstallStatus, 0, 3);
            tableLayoutMain.Controls.Add(lstLog, 0, 4);
            tableLayoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutMain.Location = new System.Drawing.Point(0, 0);
            tableLayoutMain.Name = "tableLayoutMain";
            tableLayoutMain.RowCount = 5;
            tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            tableLayoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            tableLayoutMain.Size = new System.Drawing.Size(1200, 700);
            tableLayoutMain.TabIndex = 0;
            // 
            // flowHeader
            // 
            flowHeader.AutoSize = true;
            flowHeader.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            flowHeader.Controls.Add(lblPackageSet);
            flowHeader.Controls.Add(comboPackageSet);
            flowHeader.Controls.Add(lblEnvironment);
            flowHeader.Controls.Add(comboEnvironment);
            flowHeader.Controls.Add(btnRefreshEnvironments);
            flowHeader.Controls.Add(btnSelectAll);
            flowHeader.Controls.Add(btnClearSelection);
            flowHeader.Controls.Add(btnInstallSelected);
            flowHeader.Controls.Add(lblSelectedCount);
            flowHeader.Dock = System.Windows.Forms.DockStyle.Fill;
            flowHeader.Location = new System.Drawing.Point(0, 0);
            flowHeader.Margin = new System.Windows.Forms.Padding(0);
            flowHeader.Name = "flowHeader";
            flowHeader.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            flowHeader.Size = new System.Drawing.Size(1200, 68);
            flowHeader.TabIndex = 0;
            flowHeader.WrapContents = false;
            // 
            // lblPackageSet
            // 
            lblPackageSet.AutoSize = true;
            lblPackageSet.Location = new System.Drawing.Point(15, 8);
            lblPackageSet.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            lblPackageSet.Name = "lblPackageSet";
            lblPackageSet.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            lblPackageSet.Size = new System.Drawing.Size(132, 40);
            lblPackageSet.TabIndex = 0;
            lblPackageSet.Text = "Package Set";
            // 
            // comboPackageSet
            // 
            comboPackageSet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboPackageSet.FormattingEnabled = true;
            comboPackageSet.Location = new System.Drawing.Point(153, 12);
            comboPackageSet.Name = "comboPackageSet";
            comboPackageSet.Size = new System.Drawing.Size(220, 40);
            comboPackageSet.TabIndex = 1;
            comboPackageSet.SelectedIndexChanged += comboPackageSet_SelectedIndexChanged;
            // 
            // lblEnvironment
            // 
            lblEnvironment.AutoSize = true;
            lblEnvironment.Location = new System.Drawing.Point(379, 8);
            lblEnvironment.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            lblEnvironment.Name = "lblEnvironment";
            lblEnvironment.Padding = new System.Windows.Forms.Padding(16, 8, 0, 0);
            lblEnvironment.Size = new System.Drawing.Size(146, 40);
            lblEnvironment.TabIndex = 2;
            lblEnvironment.Text = "Environment";
            // 
            // comboEnvironment
            // 
            comboEnvironment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboEnvironment.FormattingEnabled = true;
            comboEnvironment.Location = new System.Drawing.Point(531, 12);
            comboEnvironment.Name = "comboEnvironment";
            comboEnvironment.Size = new System.Drawing.Size(220, 40);
            comboEnvironment.TabIndex = 3;
            // 
            // btnRefreshEnvironments
            // 
            btnRefreshEnvironments.AutoSize = true;
            btnRefreshEnvironments.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            btnRefreshEnvironments.Location = new System.Drawing.Point(757, 12);
            btnRefreshEnvironments.Margin = new System.Windows.Forms.Padding(3, 3, 12, 3);
            btnRefreshEnvironments.Name = "btnRefreshEnvironments";
            btnRefreshEnvironments.Size = new System.Drawing.Size(105, 44);
            btnRefreshEnvironments.TabIndex = 4;
            btnRefreshEnvironments.Text = "Refresh";
            btnRefreshEnvironments.UseVisualStyleBackColor = true;
            btnRefreshEnvironments.Click += btnRefreshEnvironments_Click;
            // 
            // btnSelectAll
            // 
            btnSelectAll.AutoSize = true;
            btnSelectAll.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            btnSelectAll.Location = new System.Drawing.Point(877, 12);
            btnSelectAll.Margin = new System.Windows.Forms.Padding(3, 3, 12, 3);
            btnSelectAll.Name = "btnSelectAll";
            btnSelectAll.Size = new System.Drawing.Size(115, 44);
            btnSelectAll.TabIndex = 5;
            btnSelectAll.Text = "Select All";
            btnSelectAll.UseVisualStyleBackColor = true;
            btnSelectAll.Click += btnSelectAll_Click;
            // 
            // btnClearSelection
            // 
            btnClearSelection.AutoSize = true;
            btnClearSelection.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            btnClearSelection.Location = new System.Drawing.Point(1007, 12);
            btnClearSelection.Margin = new System.Windows.Forms.Padding(3, 3, 12, 3);
            btnClearSelection.Name = "btnClearSelection";
            btnClearSelection.Size = new System.Drawing.Size(151, 44);
            btnClearSelection.TabIndex = 6;
            btnClearSelection.Text = "Clear Selection";
            btnClearSelection.UseVisualStyleBackColor = true;
            btnClearSelection.Click += btnClearSelection_Click;
            // 
            // btnInstallSelected
            // 
            btnInstallSelected.AutoSize = true;
            btnInstallSelected.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            btnInstallSelected.Location = new System.Drawing.Point(1166, 12);
            btnInstallSelected.Margin = new System.Windows.Forms.Padding(3, 3, 12, 3);
            btnInstallSelected.Name = "btnInstallSelected";
            btnInstallSelected.Size = new System.Drawing.Size(149, 44);
            btnInstallSelected.TabIndex = 7;
            btnInstallSelected.Text = "Install Selected";
            btnInstallSelected.UseVisualStyleBackColor = true;
            btnInstallSelected.Click += btnInstallSelected_Click;
            // 
            // lblSelectedCount
            // 
            lblSelectedCount.AutoSize = true;
            lblSelectedCount.Location = new System.Drawing.Point(1321, 8);
            lblSelectedCount.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            lblSelectedCount.Name = "lblSelectedCount";
            lblSelectedCount.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            lblSelectedCount.Size = new System.Drawing.Size(139, 40);
            lblSelectedCount.TabIndex = 8;
            lblSelectedCount.Text = "Selected: 0/0";
            // 
            // txtSetDescription
            // 
            txtSetDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            txtSetDescription.Location = new System.Drawing.Point(12, 80);
            txtSetDescription.Margin = new System.Windows.Forms.Padding(12, 12, 12, 0);
            txtSetDescription.Multiline = true;
            txtSetDescription.Name = "txtSetDescription";
            txtSetDescription.ReadOnly = true;
            txtSetDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtSetDescription.Size = new System.Drawing.Size(1176, 88);
            txtSetDescription.TabIndex = 1;
            // 
            // checkedListPackages
            // 
            checkedListPackages.CheckOnClick = true;
            checkedListPackages.Dock = System.Windows.Forms.DockStyle.Fill;
            checkedListPackages.FormattingEnabled = true;
            checkedListPackages.Location = new System.Drawing.Point(12, 178);
            checkedListPackages.Margin = new System.Windows.Forms.Padding(12, 10, 12, 0);
            checkedListPackages.Name = "checkedListPackages";
            checkedListPackages.Size = new System.Drawing.Size(1176, 352);
            checkedListPackages.TabIndex = 2;
            checkedListPackages.ItemCheck += checkedListPackages_ItemCheck;
            // 
            // panelInstallStatus
            // 
            panelInstallStatus.AutoSize = true;
            panelInstallStatus.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            panelInstallStatus.Controls.Add(progressInstall);
            panelInstallStatus.Controls.Add(btnCancelInstall);
            panelInstallStatus.Controls.Add(lblInstallStatus);
            panelInstallStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            panelInstallStatus.Location = new System.Drawing.Point(0, 530);
            panelInstallStatus.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            panelInstallStatus.Name = "panelInstallStatus";
            panelInstallStatus.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            panelInstallStatus.Size = new System.Drawing.Size(1200, 52);
            panelInstallStatus.TabIndex = 3;
            // 
            // progressInstall
            // 
            progressInstall.Location = new System.Drawing.Point(15, 11);
            progressInstall.Margin = new System.Windows.Forms.Padding(3, 3, 12, 3);
            progressInstall.Name = "progressInstall";
            progressInstall.Size = new System.Drawing.Size(320, 32);
            progressInstall.Style = System.Windows.Forms.ProgressBarStyle.Blocks;
            progressInstall.TabIndex = 0;
            // 
            // btnCancelInstall
            // 
            btnCancelInstall.AutoSize = true;
            btnCancelInstall.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            btnCancelInstall.Enabled = false;
            btnCancelInstall.Location = new System.Drawing.Point(350, 11);
            btnCancelInstall.Margin = new System.Windows.Forms.Padding(3, 3, 12, 3);
            btnCancelInstall.Name = "btnCancelInstall";
            btnCancelInstall.Size = new System.Drawing.Size(159, 44);
            btnCancelInstall.TabIndex = 1;
            btnCancelInstall.Text = "Cancel Install";
            btnCancelInstall.UseVisualStyleBackColor = true;
            btnCancelInstall.Click += btnCancelInstall_Click;
            // 
            // lblInstallStatus
            // 
            lblInstallStatus.AutoSize = true;
            lblInstallStatus.Location = new System.Drawing.Point(524, 11);
            lblInstallStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            lblInstallStatus.Name = "lblInstallStatus";
            lblInstallStatus.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            lblInstallStatus.Size = new System.Drawing.Size(0, 40);
            lblInstallStatus.TabIndex = 2;
            // 
            // lstLog
            // 
            lstLog.Dock = System.Windows.Forms.DockStyle.Fill;
            lstLog.FormattingEnabled = true;
            lstLog.ItemHeight = 32;
            lstLog.Location = new System.Drawing.Point(12, 590);
            lstLog.Margin = new System.Windows.Forms.Padding(12, 0, 12, 12);
            lstLog.Name = "lstLog";
            lstLog.Size = new System.Drawing.Size(1176, 98);
            lstLog.TabIndex = 4;
            // 
            // uc_Packages
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(tableLayoutMain);
            Name = "uc_Packages";
            Size = new System.Drawing.Size(1200, 700);
            tableLayoutMain.ResumeLayout(false);
            tableLayoutMain.PerformLayout();
            flowHeader.ResumeLayout(false);
            flowHeader.PerformLayout();
            panelInstallStatus.ResumeLayout(false);
            panelInstallStatus.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutMain;
        private System.Windows.Forms.FlowLayoutPanel flowHeader;
        private System.Windows.Forms.Label lblPackageSet;
        private System.Windows.Forms.ComboBox comboPackageSet;
        private System.Windows.Forms.Label lblEnvironment;
        private System.Windows.Forms.ComboBox comboEnvironment;
        private System.Windows.Forms.Button btnRefreshEnvironments;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnClearSelection;
        private System.Windows.Forms.Button btnInstallSelected;
        private System.Windows.Forms.Label lblSelectedCount;
        private System.Windows.Forms.TextBox txtSetDescription;
        private System.Windows.Forms.CheckedListBox checkedListPackages;
        private System.Windows.Forms.FlowLayoutPanel panelInstallStatus;
        private System.Windows.Forms.ProgressBar progressInstall;
        private System.Windows.Forms.Button btnCancelInstall;
        private System.Windows.Forms.Label lblInstallStatus;
        private System.Windows.Forms.ListBox lstLog;
    }
}
