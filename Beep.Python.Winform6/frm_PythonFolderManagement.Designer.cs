namespace Beep.Python.Winform
{
    partial class frm_PythonFolderManagement
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label packageOfflinepathLabel;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_PythonFolderManagement));
            this.txtRuntimePath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SetFolderbutton = new System.Windows.Forms.Button();
            this.Validatebutton = new System.Windows.Forms.Button();
            this.Browserbutton = new System.Windows.Forms.Button();
            this.GetFolderbutton = new System.Windows.Forms.Button();
            this.RuntimecheckBox = new System.Windows.Forms.CheckBox();
            this.Folder32checkBox1 = new System.Windows.Forms.CheckBox();
            this.Folder64checkBox2 = new System.Windows.Forms.CheckBox();
            this.Python32checkBox3 = new System.Windows.Forms.CheckBox();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Python64checkBox4 = new System.Windows.Forms.CheckBox();
            this.Cancelbutton = new System.Windows.Forms.Button();
            this.pythonConfigurationBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.packageOfflinepathTextBox = new System.Windows.Forms.TextBox();
            this.BrowseOfflinebutton = new System.Windows.Forms.Button();
            this.runtimesBindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.runtimesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.packageTypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.isPythonInstalledDataGridViewCheckBoxColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.messageDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pythonDllDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.binTypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pythonVersionDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aiFolderpathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.packageinstallpathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.binPathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.runtimePathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.scriptPathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.scriptDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lastfilePathDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.iDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.guidObjDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            packageOfflinepathLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pythonConfigurationBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.runtimesBindingSource1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.runtimesBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // txtRuntimePath
            // 
            this.txtRuntimePath.Location = new System.Drawing.Point(133, 17);
            this.txtRuntimePath.Name = "txtRuntimePath";
            this.txtRuntimePath.Size = new System.Drawing.Size(506, 20);
            this.txtRuntimePath.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(98, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Path";
            // 
            // SetFolderbutton
            // 
            this.SetFolderbutton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         //   this.SetFolderbutton.Image = global::Beep.Python.Winform.Properties.Resources.FolderCodeAnalysis;
            this.SetFolderbutton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.SetFolderbutton.Location = new System.Drawing.Point(657, 331);
            this.SetFolderbutton.Name = "SetFolderbutton";
            this.SetFolderbutton.Size = new System.Drawing.Size(102, 23);
            this.SetFolderbutton.TabIndex = 5;
            this.SetFolderbutton.Text = "Save";
            this.SetFolderbutton.UseVisualStyleBackColor = true;
            // 
            // Validatebutton
            // 
         //   this.Validatebutton.Image = global::Beep.Python.Winform.Properties.Resources.Checklist;
            this.Validatebutton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Validatebutton.Location = new System.Drawing.Point(645, 41);
            this.Validatebutton.Name = "Validatebutton";
            this.Validatebutton.Size = new System.Drawing.Size(120, 23);
            this.Validatebutton.TabIndex = 6;
            this.Validatebutton.Text = "Validate Python";
            this.Validatebutton.UseVisualStyleBackColor = true;
            this.Validatebutton.Visible = false;
            // 
            // Browserbutton
            // 
        //    this.Browserbutton.Image = global::Beep.Python.Winform.Properties.Resources.FolderBottomPanel;
            this.Browserbutton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Browserbutton.Location = new System.Drawing.Point(12, 16);
            this.Browserbutton.Name = "Browserbutton";
            this.Browserbutton.Size = new System.Drawing.Size(80, 23);
            this.Browserbutton.TabIndex = 4;
            this.Browserbutton.Text = "Browse";
            this.Browserbutton.UseVisualStyleBackColor = true;
            // 
            // GetFolderbutton
            // 
         //   this.GetFolderbutton.Image = global::Beep.Python.Winform.Properties.Resources.FolderOpened;
            this.GetFolderbutton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.GetFolderbutton.Location = new System.Drawing.Point(645, 16);
            this.GetFolderbutton.Name = "GetFolderbutton";
            this.GetFolderbutton.Size = new System.Drawing.Size(120, 23);
            this.GetFolderbutton.TabIndex = 2;
            this.GetFolderbutton.Text = "Get Folder";
            this.GetFolderbutton.UseVisualStyleBackColor = true;
            // 
            // RuntimecheckBox
            // 
            this.RuntimecheckBox.AutoSize = true;
            this.RuntimecheckBox.Location = new System.Drawing.Point(657, 128);
            this.RuntimecheckBox.Name = "RuntimecheckBox";
            this.RuntimecheckBox.Size = new System.Drawing.Size(101, 17);
            this.RuntimecheckBox.TabIndex = 7;
            this.RuntimecheckBox.Text = "RunTime Folder";
            this.RuntimecheckBox.UseVisualStyleBackColor = true;
            this.RuntimecheckBox.Visible = false;
            // 
            // Folder32checkBox1
            // 
            this.Folder32checkBox1.AutoSize = true;
            this.Folder32checkBox1.Location = new System.Drawing.Point(657, 151);
            this.Folder32checkBox1.Name = "Folder32checkBox1";
            this.Folder32checkBox1.Size = new System.Drawing.Size(70, 17);
            this.Folder32checkBox1.TabIndex = 8;
            this.Folder32checkBox1.Text = "32 Folder";
            this.Folder32checkBox1.UseVisualStyleBackColor = true;
            this.Folder32checkBox1.Visible = false;
            // 
            // Folder64checkBox2
            // 
            this.Folder64checkBox2.AutoSize = true;
            this.Folder64checkBox2.Location = new System.Drawing.Point(657, 174);
            this.Folder64checkBox2.Name = "Folder64checkBox2";
            this.Folder64checkBox2.Size = new System.Drawing.Size(70, 17);
            this.Folder64checkBox2.TabIndex = 9;
            this.Folder64checkBox2.Text = "64 Folder";
            this.Folder64checkBox2.UseVisualStyleBackColor = true;
            this.Folder64checkBox2.Visible = false;
            // 
            // Python32checkBox3
            // 
            this.Python32checkBox3.AutoSize = true;
            this.Python32checkBox3.Location = new System.Drawing.Point(657, 197);
            this.Python32checkBox3.Name = "Python32checkBox3";
            this.Python32checkBox3.Size = new System.Drawing.Size(114, 17);
            this.Python32checkBox3.TabIndex = 10;
            this.Python32checkBox3.Text = "Python in 32Folder";
            this.Python32checkBox3.UseVisualStyleBackColor = true;
            this.Python32checkBox3.Visible = false;
            // 
            // dataGridView1
            // 
            this.dataGridView1.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.guidObjDataGridViewTextBoxColumn,
            this.iDDataGridViewTextBoxColumn,
            this.lastfilePathDataGridViewTextBoxColumn,
            this.scriptDataGridViewTextBoxColumn,
            this.scriptPathDataGridViewTextBoxColumn,
            this.runtimePathDataGridViewTextBoxColumn,
            this.binPathDataGridViewTextBoxColumn,
            this.packageinstallpathDataGridViewTextBoxColumn,
            this.aiFolderpathDataGridViewTextBoxColumn,
            this.pythonVersionDataGridViewTextBoxColumn,
            this.binTypeDataGridViewTextBoxColumn,
            this.pythonDllDataGridViewTextBoxColumn,
            this.messageDataGridViewTextBoxColumn,
            this.isPythonInstalledDataGridViewCheckBoxColumn,
            this.packageTypeDataGridViewTextBoxColumn});
            this.dataGridView1.Location = new System.Drawing.Point(12, 41);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(753, 216);
            this.dataGridView1.TabIndex = 11;
            // 
            // Python64checkBox4
            // 
            this.Python64checkBox4.AutoSize = true;
            this.Python64checkBox4.Location = new System.Drawing.Point(657, 220);
            this.Python64checkBox4.Name = "Python64checkBox4";
            this.Python64checkBox4.Size = new System.Drawing.Size(117, 17);
            this.Python64checkBox4.TabIndex = 12;
            this.Python64checkBox4.Text = "Python in 64 Folder";
            this.Python64checkBox4.UseVisualStyleBackColor = true;
            this.Python64checkBox4.Visible = false;
            // 
            // Cancelbutton
            // 
            this.Cancelbutton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
          //  this.Cancelbutton.Image = global::Beep.Python.Winform.Properties.Resources.FolderError;
            this.Cancelbutton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Cancelbutton.Location = new System.Drawing.Point(33, 331);
            this.Cancelbutton.Name = "Cancelbutton";
            this.Cancelbutton.Size = new System.Drawing.Size(119, 23);
            this.Cancelbutton.TabIndex = 14;
            this.Cancelbutton.Text = "Cancel";
            this.Cancelbutton.UseVisualStyleBackColor = true;
            // 
            // pythonConfigurationBindingSource
            // 
            this.pythonConfigurationBindingSource.DataSource = typeof(Beep.Python.Model.PythonConfiguration);
            // 
            // packageOfflinepathLabel
            // 
            packageOfflinepathLabel.AutoSize = true;
            packageOfflinepathLabel.Location = new System.Drawing.Point(20, 266);
            packageOfflinepathLabel.Name = "packageOfflinepathLabel";
            packageOfflinepathLabel.Size = new System.Drawing.Size(107, 13);
            packageOfflinepathLabel.TabIndex = 14;
            packageOfflinepathLabel.Text = "Package Offlinepath:";
            // 
            // packageOfflinepathTextBox
            // 
            this.packageOfflinepathTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.pythonConfigurationBindingSource, "PackageOfflinepath", true));
            this.packageOfflinepathTextBox.Location = new System.Drawing.Point(133, 263);
            this.packageOfflinepathTextBox.Name = "packageOfflinepathTextBox";
            this.packageOfflinepathTextBox.Size = new System.Drawing.Size(485, 20);
            this.packageOfflinepathTextBox.TabIndex = 15;
            // 
            // BrowseOfflinebutton
            // 
            this.BrowseOfflinebutton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.BrowseOfflinebutton.Location = new System.Drawing.Point(624, 261);
            this.BrowseOfflinebutton.Name = "BrowseOfflinebutton";
            this.BrowseOfflinebutton.Size = new System.Drawing.Size(73, 23);
            this.BrowseOfflinebutton.TabIndex = 16;
            this.BrowseOfflinebutton.Text = "Browse";
            this.BrowseOfflinebutton.UseVisualStyleBackColor = true;
            // 
            // runtimesBindingSource1
            // 
            this.runtimesBindingSource1.DataMember = "Runtimes";
            this.runtimesBindingSource1.DataSource = this.pythonConfigurationBindingSource;
            // 
            // runtimesBindingSource
            // 
            this.runtimesBindingSource.DataSource = this.runtimesBindingSource1;
            // 
            // packageTypeDataGridViewTextBoxColumn
            // 
            this.packageTypeDataGridViewTextBoxColumn.DataPropertyName = "PackageType";
            this.packageTypeDataGridViewTextBoxColumn.HeaderText = "PackageType";
            this.packageTypeDataGridViewTextBoxColumn.Name = "packageTypeDataGridViewTextBoxColumn";
            // 
            // isPythonInstalledDataGridViewCheckBoxColumn
            // 
            this.isPythonInstalledDataGridViewCheckBoxColumn.DataPropertyName = "IsPythonInstalled";
            this.isPythonInstalledDataGridViewCheckBoxColumn.HeaderText = "IsPythonInstalled";
            this.isPythonInstalledDataGridViewCheckBoxColumn.Name = "isPythonInstalledDataGridViewCheckBoxColumn";
            // 
            // messageDataGridViewTextBoxColumn
            // 
            this.messageDataGridViewTextBoxColumn.DataPropertyName = "Message";
            this.messageDataGridViewTextBoxColumn.HeaderText = "Message";
            this.messageDataGridViewTextBoxColumn.Name = "messageDataGridViewTextBoxColumn";
            // 
            // pythonDllDataGridViewTextBoxColumn
            // 
            this.pythonDllDataGridViewTextBoxColumn.DataPropertyName = "PythonDll";
            this.pythonDllDataGridViewTextBoxColumn.HeaderText = "PythonDll";
            this.pythonDllDataGridViewTextBoxColumn.Name = "pythonDllDataGridViewTextBoxColumn";
            // 
            // binTypeDataGridViewTextBoxColumn
            // 
            this.binTypeDataGridViewTextBoxColumn.DataPropertyName = "BinType";
            this.binTypeDataGridViewTextBoxColumn.HeaderText = "BinType";
            this.binTypeDataGridViewTextBoxColumn.Name = "binTypeDataGridViewTextBoxColumn";
            // 
            // pythonVersionDataGridViewTextBoxColumn
            // 
            this.pythonVersionDataGridViewTextBoxColumn.DataPropertyName = "PythonVersion";
            this.pythonVersionDataGridViewTextBoxColumn.HeaderText = "PythonVersion";
            this.pythonVersionDataGridViewTextBoxColumn.Name = "pythonVersionDataGridViewTextBoxColumn";
            // 
            // aiFolderpathDataGridViewTextBoxColumn
            // 
            this.aiFolderpathDataGridViewTextBoxColumn.DataPropertyName = "AiFolderpath";
            this.aiFolderpathDataGridViewTextBoxColumn.HeaderText = "AiFolderpath";
            this.aiFolderpathDataGridViewTextBoxColumn.Name = "aiFolderpathDataGridViewTextBoxColumn";
            // 
            // packageinstallpathDataGridViewTextBoxColumn
            // 
            this.packageinstallpathDataGridViewTextBoxColumn.DataPropertyName = "Packageinstallpath";
            this.packageinstallpathDataGridViewTextBoxColumn.HeaderText = "Packageinstallpath";
            this.packageinstallpathDataGridViewTextBoxColumn.Name = "packageinstallpathDataGridViewTextBoxColumn";
            // 
            // binPathDataGridViewTextBoxColumn
            // 
            this.binPathDataGridViewTextBoxColumn.DataPropertyName = "BinPath";
            this.binPathDataGridViewTextBoxColumn.HeaderText = "BinPath";
            this.binPathDataGridViewTextBoxColumn.Name = "binPathDataGridViewTextBoxColumn";
            // 
            // runtimePathDataGridViewTextBoxColumn
            // 
            this.runtimePathDataGridViewTextBoxColumn.DataPropertyName = "RuntimePath";
            this.runtimePathDataGridViewTextBoxColumn.HeaderText = "RuntimePath";
            this.runtimePathDataGridViewTextBoxColumn.Name = "runtimePathDataGridViewTextBoxColumn";
            // 
            // scriptPathDataGridViewTextBoxColumn
            // 
            this.scriptPathDataGridViewTextBoxColumn.DataPropertyName = "ScriptPath";
            this.scriptPathDataGridViewTextBoxColumn.HeaderText = "ScriptPath";
            this.scriptPathDataGridViewTextBoxColumn.Name = "scriptPathDataGridViewTextBoxColumn";
            // 
            // scriptDataGridViewTextBoxColumn
            // 
            this.scriptDataGridViewTextBoxColumn.DataPropertyName = "Script";
            this.scriptDataGridViewTextBoxColumn.HeaderText = "Script";
            this.scriptDataGridViewTextBoxColumn.Name = "scriptDataGridViewTextBoxColumn";
            // 
            // lastfilePathDataGridViewTextBoxColumn
            // 
            this.lastfilePathDataGridViewTextBoxColumn.DataPropertyName = "LastfilePath";
            this.lastfilePathDataGridViewTextBoxColumn.HeaderText = "LastfilePath";
            this.lastfilePathDataGridViewTextBoxColumn.Name = "lastfilePathDataGridViewTextBoxColumn";
            // 
            // iDDataGridViewTextBoxColumn
            // 
            this.iDDataGridViewTextBoxColumn.DataPropertyName = "ID";
            this.iDDataGridViewTextBoxColumn.HeaderText = "ID";
            this.iDDataGridViewTextBoxColumn.Name = "iDDataGridViewTextBoxColumn";
            // 
            // guidObjDataGridViewTextBoxColumn
            // 
            this.guidObjDataGridViewTextBoxColumn.DataPropertyName = "GuidObj";
            this.guidObjDataGridViewTextBoxColumn.HeaderText = "GuidObj";
            this.guidObjDataGridViewTextBoxColumn.Name = "guidObjDataGridViewTextBoxColumn";
            // 
            // frm_PythonFolderManagement
            // 
            this.AcceptButton = this.SetFolderbutton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancelbutton;
            this.ClientSize = new System.Drawing.Size(774, 366);
            this.Controls.Add(this.BrowseOfflinebutton);
            this.Controls.Add(packageOfflinepathLabel);
            this.Controls.Add(this.packageOfflinepathTextBox);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.Cancelbutton);
            this.Controls.Add(this.Python64checkBox4);
            this.Controls.Add(this.Python32checkBox3);
            this.Controls.Add(this.Folder64checkBox2);
            this.Controls.Add(this.Folder32checkBox1);
            this.Controls.Add(this.RuntimecheckBox);
            this.Controls.Add(this.Validatebutton);
            this.Controls.Add(this.SetFolderbutton);
            this.Controls.Add(this.Browserbutton);
            this.Controls.Add(this.GetFolderbutton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtRuntimePath);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frm_PythonFolderManagement";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Python Folder Management";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pythonConfigurationBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.runtimesBindingSource1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.runtimesBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtRuntimePath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button GetFolderbutton;
        private System.Windows.Forms.Button Browserbutton;
        private System.Windows.Forms.Button SetFolderbutton;
        private System.Windows.Forms.Button Validatebutton;
        private System.Windows.Forms.CheckBox RuntimecheckBox;
        private System.Windows.Forms.CheckBox Folder32checkBox1;
        private System.Windows.Forms.CheckBox Folder64checkBox2;
        private System.Windows.Forms.CheckBox Python32checkBox3;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.CheckBox Python64checkBox4;
        private System.Windows.Forms.DataGridViewCheckBoxColumn folder32xexistDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn folder32xversionDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn folder64xexistDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn folder64xversionDataGridViewTextBoxColumn;
        private System.Windows.Forms.Button Cancelbutton;
        private System.Windows.Forms.BindingSource pythonConfigurationBindingSource;
        private System.Windows.Forms.TextBox packageOfflinepathTextBox;
        private System.Windows.Forms.Button BrowseOfflinebutton;
        private System.Windows.Forms.DataGridViewTextBoxColumn guidObjDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn iDDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn lastfilePathDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn scriptDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn scriptPathDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn runtimePathDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn binPathDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn packageinstallpathDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn aiFolderpathDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn pythonVersionDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn binTypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn pythonDllDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn messageDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn isPythonInstalledDataGridViewCheckBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn packageTypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.BindingSource runtimesBindingSource1;
        private System.Windows.Forms.BindingSource runtimesBindingSource;
    }
}