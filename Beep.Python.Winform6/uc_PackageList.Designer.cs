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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_PackageList));
            packagelistBindingNavigator = new BindingNavigator(components);
            bindingNavigatorAddNewItem = new ToolStripButton();
            packagelistBindingSource = new BindingSource(components);
            bindingNavigatorCountItem = new ToolStripLabel();
            bindingNavigatorDeleteItem = new ToolStripButton();
            bindingNavigatorMoveFirstItem = new ToolStripButton();
            bindingNavigatorMovePreviousItem = new ToolStripButton();
            bindingNavigatorSeparator = new ToolStripSeparator();
            bindingNavigatorPositionItem = new ToolStripTextBox();
            bindingNavigatorSeparator1 = new ToolStripSeparator();
            bindingNavigatorMoveNextItem = new ToolStripButton();
            bindingNavigatorMoveLastItem = new ToolStripButton();
            bindingNavigatorSeparator2 = new ToolStripSeparator();
            packagelistBindingNavigatorSaveItem = new ToolStripButton();
            RefreshtoolStripButton = new ToolStripButton();
            NewPackagetoolStripTextBox = new ToolStripTextBox();
            InstallNewPackagetoolStripButton = new ToolStripButton();
            InstallPIPtoolStripButton = new ToolStripButton();
            packagelistDataGridView = new DataGridView();
            dataGridViewImageColumn1 = new DataGridViewImageColumn();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripProgressBar1 = new ToolStripProgressBar();
            MessageLabel = new ToolStripStatusLabel();
            ImageColumn = new DataGridViewImageColumn();
            installedDataGridViewCheckBoxColumn = new DataGridViewCheckBoxColumn();
            packagetitleDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            packagenameDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            UpDateInstallGridButton = new DataGridViewButtonColumn();
            versionDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            updateversionDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)packagelistBindingNavigator).BeginInit();
            packagelistBindingNavigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)packagelistBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)packagelistDataGridView).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // packagelistBindingNavigator
            // 
            packagelistBindingNavigator.AddNewItem = bindingNavigatorAddNewItem;
            packagelistBindingNavigator.BindingSource = packagelistBindingSource;
            packagelistBindingNavigator.CountItem = bindingNavigatorCountItem;
            packagelistBindingNavigator.DeleteItem = bindingNavigatorDeleteItem;
            packagelistBindingNavigator.Items.AddRange(new ToolStripItem[] { bindingNavigatorMoveFirstItem, bindingNavigatorMovePreviousItem, bindingNavigatorSeparator, bindingNavigatorPositionItem, bindingNavigatorCountItem, bindingNavigatorSeparator1, bindingNavigatorMoveNextItem, bindingNavigatorMoveLastItem, bindingNavigatorSeparator2, bindingNavigatorAddNewItem, bindingNavigatorDeleteItem, packagelistBindingNavigatorSaveItem, RefreshtoolStripButton, NewPackagetoolStripTextBox, InstallNewPackagetoolStripButton, InstallPIPtoolStripButton });
            packagelistBindingNavigator.Location = new Point(0, 0);
            packagelistBindingNavigator.MoveFirstItem = bindingNavigatorMoveFirstItem;
            packagelistBindingNavigator.MoveLastItem = bindingNavigatorMoveLastItem;
            packagelistBindingNavigator.MoveNextItem = bindingNavigatorMoveNextItem;
            packagelistBindingNavigator.MovePreviousItem = bindingNavigatorMovePreviousItem;
            packagelistBindingNavigator.Name = "packagelistBindingNavigator";
            packagelistBindingNavigator.PositionItem = bindingNavigatorPositionItem;
            packagelistBindingNavigator.Size = new Size(714, 25);
            packagelistBindingNavigator.TabIndex = 0;
            packagelistBindingNavigator.Text = "bindingNavigator1";
            // 
            // bindingNavigatorAddNewItem
            // 
            bindingNavigatorAddNewItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            bindingNavigatorAddNewItem.Image = (Image)resources.GetObject("bindingNavigatorAddNewItem.Image");
            bindingNavigatorAddNewItem.Name = "bindingNavigatorAddNewItem";
            bindingNavigatorAddNewItem.RightToLeftAutoMirrorImage = true;
            bindingNavigatorAddNewItem.Size = new Size(23, 22);
            bindingNavigatorAddNewItem.Text = "Add new";
            // 
            // packagelistBindingSource
            // 
            packagelistBindingSource.DataMember = "Packages";
            packagelistBindingSource.DataSource = typeof(RuntimeEngine.ViewModels.PackageManagerViewModel);
            // 
            // bindingNavigatorCountItem
            // 
            bindingNavigatorCountItem.Name = "bindingNavigatorCountItem";
            bindingNavigatorCountItem.Size = new Size(35, 22);
            bindingNavigatorCountItem.Text = "of {0}";
            bindingNavigatorCountItem.ToolTipText = "Total number of items";
            // 
            // bindingNavigatorDeleteItem
            // 
            bindingNavigatorDeleteItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            bindingNavigatorDeleteItem.Image = (Image)resources.GetObject("bindingNavigatorDeleteItem.Image");
            bindingNavigatorDeleteItem.Name = "bindingNavigatorDeleteItem";
            bindingNavigatorDeleteItem.RightToLeftAutoMirrorImage = true;
            bindingNavigatorDeleteItem.Size = new Size(23, 22);
            bindingNavigatorDeleteItem.Text = "Delete";
            // 
            // bindingNavigatorMoveFirstItem
            // 
            bindingNavigatorMoveFirstItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            bindingNavigatorMoveFirstItem.Image = (Image)resources.GetObject("bindingNavigatorMoveFirstItem.Image");
            bindingNavigatorMoveFirstItem.Name = "bindingNavigatorMoveFirstItem";
            bindingNavigatorMoveFirstItem.RightToLeftAutoMirrorImage = true;
            bindingNavigatorMoveFirstItem.Size = new Size(23, 22);
            bindingNavigatorMoveFirstItem.Text = "Move first";
            // 
            // bindingNavigatorMovePreviousItem
            // 
            bindingNavigatorMovePreviousItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            bindingNavigatorMovePreviousItem.Image = (Image)resources.GetObject("bindingNavigatorMovePreviousItem.Image");
            bindingNavigatorMovePreviousItem.Name = "bindingNavigatorMovePreviousItem";
            bindingNavigatorMovePreviousItem.RightToLeftAutoMirrorImage = true;
            bindingNavigatorMovePreviousItem.Size = new Size(23, 22);
            bindingNavigatorMovePreviousItem.Text = "Move previous";
            // 
            // bindingNavigatorSeparator
            // 
            bindingNavigatorSeparator.Name = "bindingNavigatorSeparator";
            bindingNavigatorSeparator.Size = new Size(6, 25);
            // 
            // bindingNavigatorPositionItem
            // 
            bindingNavigatorPositionItem.AccessibleName = "Position";
            bindingNavigatorPositionItem.AutoSize = false;
            bindingNavigatorPositionItem.Name = "bindingNavigatorPositionItem";
            bindingNavigatorPositionItem.Size = new Size(58, 23);
            bindingNavigatorPositionItem.Text = "0";
            bindingNavigatorPositionItem.ToolTipText = "Current position";
            // 
            // bindingNavigatorSeparator1
            // 
            bindingNavigatorSeparator1.Name = "bindingNavigatorSeparator1";
            bindingNavigatorSeparator1.Size = new Size(6, 25);
            // 
            // bindingNavigatorMoveNextItem
            // 
            bindingNavigatorMoveNextItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            bindingNavigatorMoveNextItem.Image = (Image)resources.GetObject("bindingNavigatorMoveNextItem.Image");
            bindingNavigatorMoveNextItem.Name = "bindingNavigatorMoveNextItem";
            bindingNavigatorMoveNextItem.RightToLeftAutoMirrorImage = true;
            bindingNavigatorMoveNextItem.Size = new Size(23, 22);
            bindingNavigatorMoveNextItem.Text = "Move next";
            // 
            // bindingNavigatorMoveLastItem
            // 
            bindingNavigatorMoveLastItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            bindingNavigatorMoveLastItem.Image = (Image)resources.GetObject("bindingNavigatorMoveLastItem.Image");
            bindingNavigatorMoveLastItem.Name = "bindingNavigatorMoveLastItem";
            bindingNavigatorMoveLastItem.RightToLeftAutoMirrorImage = true;
            bindingNavigatorMoveLastItem.Size = new Size(23, 22);
            bindingNavigatorMoveLastItem.Text = "Move last";
            // 
            // bindingNavigatorSeparator2
            // 
            bindingNavigatorSeparator2.Name = "bindingNavigatorSeparator2";
            bindingNavigatorSeparator2.Size = new Size(6, 25);
            // 
            // packagelistBindingNavigatorSaveItem
            // 
            packagelistBindingNavigatorSaveItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            packagelistBindingNavigatorSaveItem.Image = (Image)resources.GetObject("packagelistBindingNavigatorSaveItem.Image");
            packagelistBindingNavigatorSaveItem.Name = "packagelistBindingNavigatorSaveItem";
            packagelistBindingNavigatorSaveItem.Size = new Size(23, 22);
            packagelistBindingNavigatorSaveItem.Text = "Save Data";
            // 
            // RefreshtoolStripButton
            // 
            RefreshtoolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            RefreshtoolStripButton.Image = (Image)resources.GetObject("RefreshtoolStripButton.Image");
            RefreshtoolStripButton.ImageTransparentColor = Color.Magenta;
            RefreshtoolStripButton.Name = "RefreshtoolStripButton";
            RefreshtoolStripButton.Size = new Size(23, 22);
            // 
            // NewPackagetoolStripTextBox
            // 
            NewPackagetoolStripTextBox.Name = "NewPackagetoolStripTextBox";
            NewPackagetoolStripTextBox.Size = new Size(116, 25);
            // 
            // InstallNewPackagetoolStripButton
            // 
            InstallNewPackagetoolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            InstallNewPackagetoolStripButton.Image = Properties.Resources.PackageLayout;
            InstallNewPackagetoolStripButton.ImageTransparentColor = Color.Magenta;
            InstallNewPackagetoolStripButton.Name = "InstallNewPackagetoolStripButton";
            InstallNewPackagetoolStripButton.Size = new Size(23, 22);
            InstallNewPackagetoolStripButton.Text = "toolStripButton1";
            InstallNewPackagetoolStripButton.ToolTipText = "Install if Package Exist";
            // 
            // InstallPIPtoolStripButton
            // 
            InstallPIPtoolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            InstallPIPtoolStripButton.Image = Properties.Resources.ConfigurationEditor;
            InstallPIPtoolStripButton.ImageTransparentColor = Color.Magenta;
            InstallPIPtoolStripButton.Name = "InstallPIPtoolStripButton";
            InstallPIPtoolStripButton.Size = new Size(23, 22);
            InstallPIPtoolStripButton.Text = "Install PIP";
            // 
            // packagelistDataGridView
            // 
            packagelistDataGridView.AutoGenerateColumns = false;
            packagelistDataGridView.BackgroundColor = Color.White;
            packagelistDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            packagelistDataGridView.Columns.AddRange(new DataGridViewColumn[] { ImageColumn, installedDataGridViewCheckBoxColumn, packagetitleDataGridViewTextBoxColumn, packagenameDataGridViewTextBoxColumn, UpDateInstallGridButton, versionDataGridViewTextBoxColumn, updateversionDataGridViewTextBoxColumn });
            packagelistDataGridView.DataSource = packagelistBindingSource;
            packagelistDataGridView.Dock = DockStyle.Fill;
            packagelistDataGridView.Location = new Point(0, 25);
            packagelistDataGridView.Margin = new Padding(4, 3, 4, 3);
            packagelistDataGridView.Name = "packagelistDataGridView";
            packagelistDataGridView.RowTemplate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopCenter;
            packagelistDataGridView.RowTemplate.DefaultCellStyle.ForeColor = Color.Black;
            packagelistDataGridView.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 255, 192);
            packagelistDataGridView.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.Red;
            packagelistDataGridView.RowTemplate.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            packagelistDataGridView.Size = new Size(714, 795);
            packagelistDataGridView.TabIndex = 1;
            // 
            // dataGridViewImageColumn1
            // 
            dataGridViewImageColumn1.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            dataGridViewImageColumn1.HeaderText = "";
            dataGridViewImageColumn1.Image = Properties.Resources.FlagDarkGreen;
            dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.White;
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripProgressBar1, MessageLabel });
            statusStrip1.Location = new Point(0, 820);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(714, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(39, 17);
            toolStripStatusLabel1.Text = "Status";
            // 
            // toolStripProgressBar1
            // 
            toolStripProgressBar1.BackColor = Color.White;
            toolStripProgressBar1.Name = "toolStripProgressBar1";
            toolStripProgressBar1.Size = new Size(100, 16);
            // 
            // MessageLabel
            // 
            MessageLabel.AutoSize = false;
            MessageLabel.BorderSides = ToolStripStatusLabelBorderSides.Left | ToolStripStatusLabelBorderSides.Top | ToolStripStatusLabelBorderSides.Right | ToolStripStatusLabelBorderSides.Bottom;
            MessageLabel.DisplayStyle = ToolStripItemDisplayStyle.Text;
            MessageLabel.Name = "MessageLabel";
            MessageLabel.Size = new Size(558, 17);
            MessageLabel.Spring = true;
            // 
            // ImageColumn
            // 
            ImageColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            ImageColumn.HeaderText = "";
            ImageColumn.Image = Properties.Resources.FlagDarkGreen;
            ImageColumn.Name = "ImageColumn";
            ImageColumn.Width = 21;
            // 
            // installedDataGridViewCheckBoxColumn
            // 
            installedDataGridViewCheckBoxColumn.DataPropertyName = "installed";
            installedDataGridViewCheckBoxColumn.HeaderText = "installed";
            installedDataGridViewCheckBoxColumn.Name = "installedDataGridViewCheckBoxColumn";
            // 
            // packagetitleDataGridViewTextBoxColumn
            // 
            packagetitleDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            packagetitleDataGridViewTextBoxColumn.DataPropertyName = "packagetitle";
            packagetitleDataGridViewTextBoxColumn.HeaderText = "Title";
            packagetitleDataGridViewTextBoxColumn.Name = "packagetitleDataGridViewTextBoxColumn";
            // 
            // packagenameDataGridViewTextBoxColumn
            // 
            packagenameDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            packagenameDataGridViewTextBoxColumn.DataPropertyName = "packagename";
            packagenameDataGridViewTextBoxColumn.HeaderText = "Name";
            packagenameDataGridViewTextBoxColumn.Name = "packagenameDataGridViewTextBoxColumn";
            // 
            // UpDateInstallGridButton
            // 
            UpDateInstallGridButton.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            UpDateInstallGridButton.DataPropertyName = "buttondisplay";
            UpDateInstallGridButton.HeaderText = "UpDate/Install";
            UpDateInstallGridButton.Name = "UpDateInstallGridButton";
            UpDateInstallGridButton.Text = "Installed";
            UpDateInstallGridButton.UseColumnTextForButtonValue = true;
            UpDateInstallGridButton.Width = 88;
            // 
            // versionDataGridViewTextBoxColumn
            // 
            versionDataGridViewTextBoxColumn.DataPropertyName = "version";
            versionDataGridViewTextBoxColumn.HeaderText = "version";
            versionDataGridViewTextBoxColumn.Name = "versionDataGridViewTextBoxColumn";
            // 
            // updateversionDataGridViewTextBoxColumn
            // 
            updateversionDataGridViewTextBoxColumn.DataPropertyName = "updateversion";
            updateversionDataGridViewTextBoxColumn.HeaderText = "updateversion";
            updateversionDataGridViewTextBoxColumn.Name = "updateversionDataGridViewTextBoxColumn";
            // 
            // uc_PackageList
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(packagelistDataGridView);
            Controls.Add(statusStrip1);
            Controls.Add(packagelistBindingNavigator);
            Margin = new Padding(4, 3, 4, 3);
            Name = "uc_PackageList";
            Size = new Size(714, 842);
            ((System.ComponentModel.ISupportInitialize)packagelistBindingNavigator).EndInit();
            packagelistBindingNavigator.ResumeLayout(false);
            packagelistBindingNavigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)packagelistBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)packagelistDataGridView).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
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
        private System.Windows.Forms.ToolStripTextBox NewPackagetoolStripTextBox;
        private System.Windows.Forms.ToolStripButton InstallNewPackagetoolStripButton;
        private System.Windows.Forms.ToolStripButton InstallPIPtoolStripButton;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripProgressBar toolStripProgressBar1;
        private ToolStripStatusLabel MessageLabel;
        private DataGridViewImageColumn ImageColumn;
        private DataGridViewCheckBoxColumn installedDataGridViewCheckBoxColumn;
        private DataGridViewTextBoxColumn packagetitleDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn packagenameDataGridViewTextBoxColumn;
        private DataGridViewButtonColumn UpDateInstallGridButton;
        private DataGridViewTextBoxColumn versionDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn updateversionDataGridViewTextBoxColumn;
    }
}
