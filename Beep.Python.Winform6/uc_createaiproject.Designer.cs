namespace Beep.Python.Winform
{
    partial class uc_createaiproject
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_createaiproject));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            statusStrip1 = new StatusStrip();
            splitContainer1 = new SplitContainer();
            toolStrip1 = new ToolStrip();
            toolStripButton1 = new ToolStripButton();
            toolStripButton2 = new ToolStripButton();
            label1 = new Label();
            label10 = new Label();
            label9 = new Label();
            LabelcomboBox = new ComboBox();
            FeaturesbindingSource1 = new BindingSource(components);
            pythonAIProjectViewModelBindingSource = new BindingSource(components);
            PrimarycomboBox = new ComboBox();
            label8 = new Label();
            label7 = new Label();
            dataGridView2 = new DataGridView();
            iDDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            sTEPNAMEDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            oUTPUTFILENAMEDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            PythonDataPipeLinebindingSource = new BindingSource(components);
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            dataGridView1 = new DataGridView();
            pARAMETERNAMEDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            pARAMETERDESCRIPTIONDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            pARAMETERVALUEDataGridViewTextBoxColumn = new DataGridViewTextBoxColumn();
            ParameterDictionaryForAlgorithmsbindingSource = new BindingSource(components);
            GetFilebutton = new Button();
            textBox3 = new TextBox();
            comboBox1 = new ComboBox();
            ListofAlgorithimsBidningSource = new BindingSource(components);
            textBox2 = new TextBox();
            textBox1 = new TextBox();
            FeaturesArraybindingSource = new BindingSource(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)FeaturesbindingSource1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pythonAIProjectViewModelBindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)PythonDataPipeLinebindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ParameterDictionaryForAlgorithmsbindingSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ListofAlgorithimsBidningSource).BeginInit();
            ((System.ComponentModel.ISupportInitialize)FeaturesArraybindingSource).BeginInit();
            SuspendLayout();
            // 
            // statusStrip1
            // 
            statusStrip1.Location = new Point(0, 874);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(753, 22);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // splitContainer1
            // 
            splitContainer1.BorderStyle = BorderStyle.FixedSingle;
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.BackColor = Color.White;
            splitContainer1.Panel1.Controls.Add(toolStrip1);
            splitContainer1.Panel1.Controls.Add(label1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.BackColor = Color.White;
            splitContainer1.Panel2.Controls.Add(label10);
            splitContainer1.Panel2.Controls.Add(label9);
            splitContainer1.Panel2.Controls.Add(LabelcomboBox);
            splitContainer1.Panel2.Controls.Add(PrimarycomboBox);
            splitContainer1.Panel2.Controls.Add(label8);
            splitContainer1.Panel2.Controls.Add(label7);
            splitContainer1.Panel2.Controls.Add(dataGridView2);
            splitContainer1.Panel2.Controls.Add(label6);
            splitContainer1.Panel2.Controls.Add(label5);
            splitContainer1.Panel2.Controls.Add(label4);
            splitContainer1.Panel2.Controls.Add(label3);
            splitContainer1.Panel2.Controls.Add(label2);
            splitContainer1.Panel2.Controls.Add(dataGridView1);
            splitContainer1.Panel2.Controls.Add(GetFilebutton);
            splitContainer1.Panel2.Controls.Add(textBox3);
            splitContainer1.Panel2.Controls.Add(comboBox1);
            splitContainer1.Panel2.Controls.Add(textBox2);
            splitContainer1.Panel2.Controls.Add(textBox1);
            splitContainer1.Size = new Size(753, 874);
            splitContainer1.SplitterDistance = 68;
            splitContainer1.TabIndex = 1;
            // 
            // toolStrip1
            // 
            toolStrip1.Dock = DockStyle.Bottom;
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButton1, toolStripButton2 });
            toolStrip1.Location = new Point(0, 41);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(751, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton1.Image = (Image)resources.GetObject("toolStripButton1.Image");
            toolStripButton1.ImageTransparentColor = Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new Size(23, 22);
            toolStripButton1.Text = "toolStripButton1";
            // 
            // toolStripButton2
            // 
            toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButton2.Image = (Image)resources.GetObject("toolStripButton2.Image");
            toolStripButton2.ImageTransparentColor = Color.Magenta;
            toolStripButton2.Name = "toolStripButton2";
            toolStripButton2.Size = new Size(23, 22);
            toolStripButton2.Text = "toolStripButton2";
            // 
            // label1
            // 
            label1.Dock = DockStyle.Top;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Size = new Size(751, 34);
            label1.TabIndex = 0;
            label1.Text = "AI Project";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            label10.BorderStyle = BorderStyle.FixedSingle;
            label10.Location = new Point(57, 169);
            label10.Name = "label10";
            label10.Size = new Size(100, 23);
            label10.TabIndex = 71;
            label10.Text = "Label";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            label9.BorderStyle = BorderStyle.FixedSingle;
            label9.Location = new Point(57, 140);
            label9.Name = "label9";
            label9.Size = new Size(100, 23);
            label9.TabIndex = 70;
            label9.Text = "Key";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // LabelcomboBox
            // 
            LabelcomboBox.DataSource = FeaturesbindingSource1;
            LabelcomboBox.DisplayMember = "DisplayValue";
            LabelcomboBox.FormattingEnabled = true;
            LabelcomboBox.Location = new Point(165, 169);
            LabelcomboBox.Name = "LabelcomboBox";
            LabelcomboBox.Size = new Size(441, 23);
            LabelcomboBox.TabIndex = 69;
            LabelcomboBox.ValueMember = "DisplayValue";
            // 
            // FeaturesbindingSource1
            // 
            FeaturesbindingSource1.DataMember = "Features";
            FeaturesbindingSource1.DataSource = pythonAIProjectViewModelBindingSource;
            // 
            // pythonAIProjectViewModelBindingSource
            // 
            pythonAIProjectViewModelBindingSource.DataMember = "Projects";
            pythonAIProjectViewModelBindingSource.DataSource = typeof(RuntimeEngine.ViewModels.PythonAIProjectViewModel);
            // 
            // PrimarycomboBox
            // 
            PrimarycomboBox.DataSource = FeaturesbindingSource1;
            PrimarycomboBox.DisplayMember = "DisplayValue";
            PrimarycomboBox.FormattingEnabled = true;
            PrimarycomboBox.Location = new Point(165, 140);
            PrimarycomboBox.Name = "PrimarycomboBox";
            PrimarycomboBox.Size = new Size(441, 23);
            PrimarycomboBox.TabIndex = 68;
            PrimarycomboBox.ValueMember = "DisplayValue";
            // 
            // label8
            // 
            label8.BorderStyle = BorderStyle.FixedSingle;
            label8.Location = new Point(57, 112);
            label8.Name = "label8";
            label8.Size = new Size(100, 23);
            label8.TabIndex = 14;
            label8.Text = "Features";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            label7.BorderStyle = BorderStyle.FixedSingle;
            label7.Location = new Point(59, 531);
            label7.Name = "label7";
            label7.Size = new Size(100, 23);
            label7.TabIndex = 12;
            label7.Text = "Data PipeLine";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dataGridView2
            // 
            dataGridView2.AutoGenerateColumns = false;
            dataGridView2.BackgroundColor = Color.White;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = Color.White;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridView2.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView2.Columns.AddRange(new DataGridViewColumn[] { iDDataGridViewTextBoxColumn, sTEPNAMEDataGridViewTextBoxColumn, oUTPUTFILENAMEDataGridViewTextBoxColumn });
            dataGridView2.DataSource = PythonDataPipeLinebindingSource;
            dataGridView2.Location = new Point(165, 531);
            dataGridView2.Name = "dataGridView2";
            dataGridView2.RowHeadersVisible = false;
            dataGridView2.RowTemplate.Height = 25;
            dataGridView2.Size = new Size(441, 249);
            dataGridView2.TabIndex = 11;
            // 
            // iDDataGridViewTextBoxColumn
            // 
            iDDataGridViewTextBoxColumn.DataPropertyName = "ID";
            iDDataGridViewTextBoxColumn.HeaderText = "ID";
            iDDataGridViewTextBoxColumn.Name = "iDDataGridViewTextBoxColumn";
            // 
            // sTEPNAMEDataGridViewTextBoxColumn
            // 
            sTEPNAMEDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            sTEPNAMEDataGridViewTextBoxColumn.DataPropertyName = "STEPNAME";
            sTEPNAMEDataGridViewTextBoxColumn.HeaderText = "STEP";
            sTEPNAMEDataGridViewTextBoxColumn.Name = "sTEPNAMEDataGridViewTextBoxColumn";
            // 
            // oUTPUTFILENAMEDataGridViewTextBoxColumn
            // 
            oUTPUTFILENAMEDataGridViewTextBoxColumn.DataPropertyName = "OUTPUTFILENAME";
            oUTPUTFILENAMEDataGridViewTextBoxColumn.HeaderText = "OUTPUT FILENAME";
            oUTPUTFILENAMEDataGridViewTextBoxColumn.Name = "oUTPUTFILENAMEDataGridViewTextBoxColumn";
            // 
            // PythonDataPipeLinebindingSource
            // 
            PythonDataPipeLinebindingSource.DataMember = "PythonDataPipeLine";
            PythonDataPipeLinebindingSource.DataSource = pythonAIProjectViewModelBindingSource;
            // 
            // label6
            // 
            label6.BorderStyle = BorderStyle.FixedSingle;
            label6.Location = new Point(57, 81);
            label6.Name = "label6";
            label6.Size = new Size(100, 23);
            label6.TabIndex = 10;
            label6.Text = "Input Data ";
            label6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            label5.BorderStyle = BorderStyle.FixedSingle;
            label5.Location = new Point(57, 21);
            label5.Name = "label5";
            label5.Size = new Size(100, 23);
            label5.TabIndex = 9;
            label5.Text = "Title";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            label4.BorderStyle = BorderStyle.FixedSingle;
            label4.Location = new Point(57, 50);
            label4.Name = "label4";
            label4.Size = new Size(100, 23);
            label4.TabIndex = 8;
            label4.Text = "Description";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.BorderStyle = BorderStyle.FixedSingle;
            label3.Location = new Point(59, 262);
            label3.Name = "label3";
            label3.Size = new Size(100, 23);
            label3.TabIndex = 7;
            label3.Text = "Parameters";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Location = new Point(59, 231);
            label2.Name = "label2";
            label2.Size = new Size(100, 23);
            label2.TabIndex = 6;
            label2.Text = "Algorithm";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // dataGridView1
            // 
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.BackgroundColor = Color.White;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = Color.White;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { pARAMETERNAMEDataGridViewTextBoxColumn, pARAMETERDESCRIPTIONDataGridViewTextBoxColumn, pARAMETERVALUEDataGridViewTextBoxColumn });
            dataGridView1.DataSource = ParameterDictionaryForAlgorithmsbindingSource;
            dataGridView1.Location = new Point(165, 262);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowTemplate.Height = 25;
            dataGridView1.Size = new Size(441, 249);
            dataGridView1.TabIndex = 5;
            // 
            // pARAMETERNAMEDataGridViewTextBoxColumn
            // 
            pARAMETERNAMEDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            pARAMETERNAMEDataGridViewTextBoxColumn.DataPropertyName = "PARAMETERNAME";
            pARAMETERNAMEDataGridViewTextBoxColumn.HeaderText = "PARAMETER NAME";
            pARAMETERNAMEDataGridViewTextBoxColumn.Name = "pARAMETERNAMEDataGridViewTextBoxColumn";
            // 
            // pARAMETERDESCRIPTIONDataGridViewTextBoxColumn
            // 
            pARAMETERDESCRIPTIONDataGridViewTextBoxColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            pARAMETERDESCRIPTIONDataGridViewTextBoxColumn.DataPropertyName = "PARAMETERDESCRIPTION";
            pARAMETERDESCRIPTIONDataGridViewTextBoxColumn.HeaderText = "DESCRIPTION";
            pARAMETERDESCRIPTIONDataGridViewTextBoxColumn.Name = "pARAMETERDESCRIPTIONDataGridViewTextBoxColumn";
            // 
            // pARAMETERVALUEDataGridViewTextBoxColumn
            // 
            pARAMETERVALUEDataGridViewTextBoxColumn.DataPropertyName = "PARAMETERVALUE";
            pARAMETERVALUEDataGridViewTextBoxColumn.HeaderText = "VALUE";
            pARAMETERVALUEDataGridViewTextBoxColumn.Name = "pARAMETERVALUEDataGridViewTextBoxColumn";
            // 
            // ParameterDictionaryForAlgorithmsbindingSource
            // 
            ParameterDictionaryForAlgorithmsbindingSource.DataMember = "PythonAlgorithmParams";
            ParameterDictionaryForAlgorithmsbindingSource.DataSource = pythonAIProjectViewModelBindingSource;
            // 
            // GetFilebutton
            // 
            GetFilebutton.Location = new Point(614, 81);
            GetFilebutton.Name = "GetFilebutton";
            GetFilebutton.Size = new Size(100, 23);
            GetFilebutton.TabIndex = 4;
            GetFilebutton.Text = "Get File";
            GetFilebutton.UseVisualStyleBackColor = true;
            // 
            // textBox3
            // 
            textBox3.BorderStyle = BorderStyle.FixedSingle;
            textBox3.Location = new Point(165, 81);
            textBox3.Name = "textBox3";
            textBox3.PlaceholderText = "Data File";
            textBox3.Size = new Size(167, 23);
            textBox3.TabIndex = 3;
            // 
            // comboBox1
            // 
            comboBox1.DataBindings.Add(new Binding("Text", pythonAIProjectViewModelBindingSource, "Algorithm", true));
            comboBox1.DataSource = ListofAlgorithimsBidningSource;
            comboBox1.DisplayMember = "LOVNAME";
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(165, 231);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(441, 23);
            comboBox1.TabIndex = 2;
            comboBox1.ValueMember = "LOVNAME";
            // 
            // ListofAlgorithimsBidningSource
            // 
            ListofAlgorithimsBidningSource.DataMember = "ListofAlgorithims";
            ListofAlgorithimsBidningSource.DataSource = typeof(RuntimeEngine.ViewModels.PythonAIProjectViewModel);
            // 
            // textBox2
            // 
            textBox2.BackColor = Color.White;
            textBox2.BorderStyle = BorderStyle.FixedSingle;
            textBox2.DataBindings.Add(new Binding("Text", pythonAIProjectViewModelBindingSource, "Description", true));
            textBox2.Location = new Point(165, 50);
            textBox2.Name = "textBox2";
            textBox2.PlaceholderText = "Description";
            textBox2.Size = new Size(443, 23);
            textBox2.TabIndex = 1;
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.FixedSingle;
            textBox1.DataBindings.Add(new Binding("Text", pythonAIProjectViewModelBindingSource, "Title", true));
            textBox1.Location = new Point(165, 21);
            textBox1.Name = "textBox1";
            textBox1.PlaceholderText = "Title";
            textBox1.Size = new Size(443, 23);
            textBox1.TabIndex = 0;
            // 
            // FeaturesArraybindingSource
            // 
            FeaturesArraybindingSource.DataMember = "FeaturesArray";
            FeaturesArraybindingSource.DataSource = pythonAIProjectViewModelBindingSource;
            // 
            // uc_createaiproject
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Controls.Add(statusStrip1);
            Name = "uc_createaiproject";
            Size = new Size(753, 896);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)FeaturesbindingSource1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pythonAIProjectViewModelBindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView2).EndInit();
            ((System.ComponentModel.ISupportInitialize)PythonDataPipeLinebindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)ParameterDictionaryForAlgorithmsbindingSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)ListofAlgorithimsBidningSource).EndInit();
            ((System.ComponentModel.ISupportInitialize)FeaturesArraybindingSource).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private StatusStrip statusStrip1;
        private SplitContainer splitContainer1;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButton1;
        private ToolStripButton toolStripButton2;
        private Label label1;
        private TextBox textBox1;
        private BindingSource pythonAIProjectViewModelBindingSource;
        private TextBox textBox2;
        private ComboBox comboBox1;
        private BindingSource ListofAlgorithimsBidningSource;
        private BindingSource ParameterDictionaryForAlgorithmsbindingSource;
        private Button GetFilebutton;
        private TextBox textBox3;
        private DataGridView dataGridView1;
        private DataGridViewTextBoxColumn pARAMETERNAMEDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn pARAMETERDESCRIPTIONDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn pARAMETERVALUEDataGridViewTextBoxColumn;
        private BindingSource PythonDataPipeLinebindingSource;
        private BindingSource FeaturesArraybindingSource;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label7;
        private DataGridView dataGridView2;
        private DataGridViewTextBoxColumn iDDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn sTEPNAMEDataGridViewTextBoxColumn;
        private DataGridViewTextBoxColumn oUTPUTFILENAMEDataGridViewTextBoxColumn;
        private Label label6;
        private Label label8;
        private ComboBox LabelcomboBox;
        private BindingSource FeaturesbindingSource1;
        private ComboBox PrimarycomboBox;
        private Label label10;
        private Label label9;
    }
}
