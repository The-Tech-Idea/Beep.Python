namespace Beep.Python.Winform
{
    partial class uc_PackageList
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_PackageList));
            this.packagelistBindingNavigator = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingNavigatorAddNewItem = new System.Windows.Forms.ToolStripButton();
            this.packagelistBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.bindingNavigatorCountItem = new System.Windows.Forms.ToolStripLabel();
            this.bindingNavigatorDeleteItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMoveFirstItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMovePreviousItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorSeparator = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorPositionItem = new System.Windows.Forms.ToolStripTextBox();
            this.bindingNavigatorSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.bindingNavigatorMoveNextItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorMoveLastItem = new System.Windows.Forms.ToolStripButton();
            this.bindingNavigatorSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.packagelistBindingNavigatorSaveItem = new System.Windows.Forms.ToolStripButton();
            this.RefreshtoolStripButton = new System.Windows.Forms.ToolStripButton();
            this.NewPackagetoolStripTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.InstallNewPackagetoolStripButton = new System.Windows.Forms.ToolStripButton();
            this.InstallPIPtoolStripButton = new System.Windows.Forms.ToolStripButton();
            this.packagelistDataGridView = new System.Windows.Forms.DataGridView();
            this.ImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.NamedataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.InstalleddataGridViewCheckBoxColumn1 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.VersiondataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.updateversion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UpDateInstallGridButton = new System.Windows.Forms.DataGridViewButtonColumn();
            this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
            ((System.ComponentModel.ISupportInitialize)(this.packagelistBindingNavigator)).BeginInit();
            this.packagelistBindingNavigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagelistBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.packagelistDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // packagelistBindingNavigator
            // 
            this.packagelistBindingNavigator.AddNewItem = this.bindingNavigatorAddNewItem;
            this.packagelistBindingNavigator.BindingSource = this.packagelistBindingSource;
            this.packagelistBindingNavigator.CountItem = this.bindingNavigatorCountItem;
            this.packagelistBindingNavigator.DeleteItem = this.bindingNavigatorDeleteItem;
            this.packagelistBindingNavigator.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.bindingNavigatorMoveFirstItem,
            this.bindingNavigatorMovePreviousItem,
            this.bindingNavigatorSeparator,
            this.bindingNavigatorPositionItem,
            this.bindingNavigatorCountItem,
            this.bindingNavigatorSeparator1,
            this.bindingNavigatorMoveNextItem,
            this.bindingNavigatorMoveLastItem,
            this.bindingNavigatorSeparator2,
            this.bindingNavigatorAddNewItem,
            this.bindingNavigatorDeleteItem,
            this.packagelistBindingNavigatorSaveItem,
            this.RefreshtoolStripButton,
            this.NewPackagetoolStripTextBox,
            this.InstallNewPackagetoolStripButton,
            this.InstallPIPtoolStripButton});
            this.packagelistBindingNavigator.Location = new System.Drawing.Point(0, 0);
            this.packagelistBindingNavigator.MoveFirstItem = this.bindingNavigatorMoveFirstItem;
            this.packagelistBindingNavigator.MoveLastItem = this.bindingNavigatorMoveLastItem;
            this.packagelistBindingNavigator.MoveNextItem = this.bindingNavigatorMoveNextItem;
            this.packagelistBindingNavigator.MovePreviousItem = this.bindingNavigatorMovePreviousItem;
            this.packagelistBindingNavigator.Name = "packagelistBindingNavigator";
            this.packagelistBindingNavigator.PositionItem = this.bindingNavigatorPositionItem;
            this.packagelistBindingNavigator.Size = new System.Drawing.Size(612, 25);
            this.packagelistBindingNavigator.TabIndex = 0;
            this.packagelistBindingNavigator.Text = "bindingNavigator1";
            // 
            // bindingNavigatorAddNewItem
            // 
            this.bindingNavigatorAddNewItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorAddNewItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorAddNewItem.Image")));
            this.bindingNavigatorAddNewItem.Name = "bindingNavigatorAddNewItem";
            this.bindingNavigatorAddNewItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorAddNewItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorAddNewItem.Text = "Add new";
            // 
            // packagelistBindingSource
            // 
            this.packagelistBindingSource.DataSource = typeof(Beep.Python.Model.PackageDefinition);
            // 
            // bindingNavigatorCountItem
            // 
            this.bindingNavigatorCountItem.Name = "bindingNavigatorCountItem";
            this.bindingNavigatorCountItem.Size = new System.Drawing.Size(35, 22);
            this.bindingNavigatorCountItem.Text = "of {0}";
            this.bindingNavigatorCountItem.ToolTipText = "Total number of items";
            // 
            // bindingNavigatorDeleteItem
            // 
            this.bindingNavigatorDeleteItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorDeleteItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorDeleteItem.Image")));
            this.bindingNavigatorDeleteItem.Name = "bindingNavigatorDeleteItem";
            this.bindingNavigatorDeleteItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorDeleteItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorDeleteItem.Text = "Delete";
            // 
            // bindingNavigatorMoveFirstItem
            // 
            this.bindingNavigatorMoveFirstItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveFirstItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveFirstItem.Image")));
            this.bindingNavigatorMoveFirstItem.Name = "bindingNavigatorMoveFirstItem";
            this.bindingNavigatorMoveFirstItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveFirstItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveFirstItem.Text = "Move first";
            // 
            // bindingNavigatorMovePreviousItem
            // 
            this.bindingNavigatorMovePreviousItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMovePreviousItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMovePreviousItem.Image")));
            this.bindingNavigatorMovePreviousItem.Name = "bindingNavigatorMovePreviousItem";
            this.bindingNavigatorMovePreviousItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMovePreviousItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMovePreviousItem.Text = "Move previous";
            // 
            // bindingNavigatorSeparator
            // 
            this.bindingNavigatorSeparator.Name = "bindingNavigatorSeparator";
            this.bindingNavigatorSeparator.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorPositionItem
            // 
            this.bindingNavigatorPositionItem.AccessibleName = "Position";
            this.bindingNavigatorPositionItem.AutoSize = false;
            this.bindingNavigatorPositionItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.bindingNavigatorPositionItem.Name = "bindingNavigatorPositionItem";
            this.bindingNavigatorPositionItem.Size = new System.Drawing.Size(50, 23);
            this.bindingNavigatorPositionItem.Text = "0";
            this.bindingNavigatorPositionItem.ToolTipText = "Current position";
            // 
            // bindingNavigatorSeparator1
            // 
            this.bindingNavigatorSeparator1.Name = "bindingNavigatorSeparator1";
            this.bindingNavigatorSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // bindingNavigatorMoveNextItem
            // 
            this.bindingNavigatorMoveNextItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveNextItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveNextItem.Image")));
            this.bindingNavigatorMoveNextItem.Name = "bindingNavigatorMoveNextItem";
            this.bindingNavigatorMoveNextItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveNextItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveNextItem.Text = "Move next";
            // 
            // bindingNavigatorMoveLastItem
            // 
            this.bindingNavigatorMoveLastItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.bindingNavigatorMoveLastItem.Image = ((System.Drawing.Image)(resources.GetObject("bindingNavigatorMoveLastItem.Image")));
            this.bindingNavigatorMoveLastItem.Name = "bindingNavigatorMoveLastItem";
            this.bindingNavigatorMoveLastItem.RightToLeftAutoMirrorImage = true;
            this.bindingNavigatorMoveLastItem.Size = new System.Drawing.Size(23, 22);
            this.bindingNavigatorMoveLastItem.Text = "Move last";
            // 
            // bindingNavigatorSeparator2
            // 
            this.bindingNavigatorSeparator2.Name = "bindingNavigatorSeparator2";
            this.bindingNavigatorSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // packagelistBindingNavigatorSaveItem
            // 
            this.packagelistBindingNavigatorSaveItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.packagelistBindingNavigatorSaveItem.Image = ((System.Drawing.Image)(resources.GetObject("packagelistBindingNavigatorSaveItem.Image")));
            this.packagelistBindingNavigatorSaveItem.Name = "packagelistBindingNavigatorSaveItem";
            this.packagelistBindingNavigatorSaveItem.Size = new System.Drawing.Size(23, 22);
            this.packagelistBindingNavigatorSaveItem.Text = "Save Data";
            // 
            // RefreshtoolStripButton
            // 
            this.RefreshtoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.RefreshtoolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("RefreshtoolStripButton.Image")));
            this.RefreshtoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.RefreshtoolStripButton.Name = "RefreshtoolStripButton";
            this.RefreshtoolStripButton.Size = new System.Drawing.Size(23, 22);
            // 
            // NewPackagetoolStripTextBox
            // 
            this.NewPackagetoolStripTextBox.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.NewPackagetoolStripTextBox.Name = "NewPackagetoolStripTextBox";
            this.NewPackagetoolStripTextBox.Size = new System.Drawing.Size(100, 25);
            // 
            // InstallNewPackagetoolStripButton
            // 
            this.InstallNewPackagetoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.InstallNewPackagetoolStripButton.Image = global::Beep.Python.Winform.Properties.Resources.PackageLayout;
            this.InstallNewPackagetoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.InstallNewPackagetoolStripButton.Name = "InstallNewPackagetoolStripButton";
            this.InstallNewPackagetoolStripButton.Size = new System.Drawing.Size(23, 22);
            this.InstallNewPackagetoolStripButton.Text = "toolStripButton1";
            this.InstallNewPackagetoolStripButton.ToolTipText = "Install if Package Exist";
            // 
            // InstallPIPtoolStripButton
            // 
            this.InstallPIPtoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.InstallPIPtoolStripButton.Image = global::Beep.Python.Winform.Properties.Resources.ConfigurationEditor;
            this.InstallPIPtoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.InstallPIPtoolStripButton.Name = "InstallPIPtoolStripButton";
            this.InstallPIPtoolStripButton.Size = new System.Drawing.Size(23, 22);
            this.InstallPIPtoolStripButton.Text = "Install PIP";
            // 
            // packagelistDataGridView
            // 
            this.packagelistDataGridView.AutoGenerateColumns = false;
            this.packagelistDataGridView.BackgroundColor = System.Drawing.Color.White;
            this.packagelistDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.packagelistDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ImageColumn,
            this.NamedataGridViewTextBoxColumn2,
            this.InstalleddataGridViewCheckBoxColumn1,
            this.VersiondataGridViewTextBoxColumn6,
            this.updateversion,
            this.UpDateInstallGridButton});
            this.packagelistDataGridView.DataSource = this.packagelistBindingSource;
            this.packagelistDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packagelistDataGridView.Location = new System.Drawing.Point(0, 25);
            this.packagelistDataGridView.Name = "packagelistDataGridView";
            this.packagelistDataGridView.RowTemplate.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.TopCenter;
            this.packagelistDataGridView.RowTemplate.DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
            this.packagelistDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.packagelistDataGridView.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Red;
            this.packagelistDataGridView.RowTemplate.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.packagelistDataGridView.Size = new System.Drawing.Size(612, 705);
            this.packagelistDataGridView.TabIndex = 1;
            // 
            // ImageColumn
            // 
            this.ImageColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.ImageColumn.HeaderText = "";
            this.ImageColumn.Image = global::Beep.Python.Winform.Properties.Resources.FlagDarkGreen;
            this.ImageColumn.Name = "ImageColumn";
            this.ImageColumn.Width = 21;
            // 
            // NamedataGridViewTextBoxColumn2
            // 
            this.NamedataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.NamedataGridViewTextBoxColumn2.DataPropertyName = "packagename";
            this.NamedataGridViewTextBoxColumn2.HeaderText = "Name";
            this.NamedataGridViewTextBoxColumn2.Name = "NamedataGridViewTextBoxColumn2";
            // 
            // InstalleddataGridViewCheckBoxColumn1
            // 
            this.InstalleddataGridViewCheckBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.InstalleddataGridViewCheckBoxColumn1.DataPropertyName = "installed";
            this.InstalleddataGridViewCheckBoxColumn1.FalseValue = "";
            this.InstalleddataGridViewCheckBoxColumn1.HeaderText = "Installed";
            this.InstalleddataGridViewCheckBoxColumn1.IndeterminateValue = "";
            this.InstalleddataGridViewCheckBoxColumn1.Name = "InstalleddataGridViewCheckBoxColumn1";
            this.InstalleddataGridViewCheckBoxColumn1.ReadOnly = true;
            this.InstalleddataGridViewCheckBoxColumn1.TrueValue = "";
            this.InstalleddataGridViewCheckBoxColumn1.Width = 52;
            // 
            // VersiondataGridViewTextBoxColumn6
            // 
            this.VersiondataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.VersiondataGridViewTextBoxColumn6.DataPropertyName = "version";
            this.VersiondataGridViewTextBoxColumn6.HeaderText = "Version";
            this.VersiondataGridViewTextBoxColumn6.Name = "VersiondataGridViewTextBoxColumn6";
            this.VersiondataGridViewTextBoxColumn6.ReadOnly = true;
            this.VersiondataGridViewTextBoxColumn6.Width = 67;
            // 
            // updateversion
            // 
            this.updateversion.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.updateversion.DataPropertyName = "updateversion";
            this.updateversion.HeaderText = "Update";
            this.updateversion.Name = "updateversion";
            this.updateversion.ReadOnly = true;
            this.updateversion.Width = 67;
            // 
            // UpDateInstallGridButton
            // 
            this.UpDateInstallGridButton.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.UpDateInstallGridButton.DataPropertyName = "buttondisplay";
            this.UpDateInstallGridButton.HeaderText = "UpDate/Install";
            this.UpDateInstallGridButton.Name = "UpDateInstallGridButton";
            this.UpDateInstallGridButton.Text = "Installed";
            this.UpDateInstallGridButton.UseColumnTextForButtonValue = true;
            this.UpDateInstallGridButton.Width = 82;
            // 
            // dataGridViewImageColumn1
            // 
            this.dataGridViewImageColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewImageColumn1.HeaderText = "";
            this.dataGridViewImageColumn1.Image = global::Beep.Python.Winform.Properties.Resources.FlagDarkGreen;
            this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
            // 
            // uc_PackageList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.packagelistDataGridView);
            this.Controls.Add(this.packagelistBindingNavigator);
            this.Name = "uc_PackageList";
            this.Size = new System.Drawing.Size(612, 730);
            ((System.ComponentModel.ISupportInitialize)(this.packagelistBindingNavigator)).EndInit();
            this.packagelistBindingNavigator.ResumeLayout(false);
            this.packagelistBindingNavigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.packagelistBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.packagelistDataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.BindingSource packagelistBindingSource;
        private System.Windows.Forms.BindingNavigator packagelistBindingNavigator;
        private System.Windows.Forms.ToolStripButton bindingNavigatorAddNewItem;
        private System.Windows.Forms.ToolStripLabel bindingNavigatorCountItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorDeleteItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveFirstItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMovePreviousItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator;
        private System.Windows.Forms.ToolStripTextBox bindingNavigatorPositionItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator1;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveNextItem;
        private System.Windows.Forms.ToolStripButton bindingNavigatorMoveLastItem;
        private System.Windows.Forms.ToolStripSeparator bindingNavigatorSeparator2;
        private System.Windows.Forms.ToolStripButton packagelistBindingNavigatorSaveItem;
        private System.Windows.Forms.DataGridView packagelistDataGridView;
        private System.Windows.Forms.ToolStripButton RefreshtoolStripButton;
        private System.Windows.Forms.DataGridViewImageColumn dataGridViewImageColumn1;
        private System.Windows.Forms.DataGridViewImageColumn ImageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn NamedataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewCheckBoxColumn InstalleddataGridViewCheckBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn VersiondataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn updateversion;
        private System.Windows.Forms.DataGridViewButtonColumn UpDateInstallGridButton;
        private System.Windows.Forms.ToolStripTextBox NewPackagetoolStripTextBox;
        private System.Windows.Forms.ToolStripButton InstallNewPackagetoolStripButton;
        private System.Windows.Forms.ToolStripButton InstallPIPtoolStripButton;
    }
}
