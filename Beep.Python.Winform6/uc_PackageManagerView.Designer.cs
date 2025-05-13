

using TheTechIdea.Beep.Winform.Controls.Grid;

namespace Beep.Python.Winform
{
    partial class uc_PackageManagerView
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
            tableLayoutPanel1 = new TableLayoutPanel();
            toolStrip1 = new ToolStrip();
            label1 = new Label();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripProgressBar1 = new ToolStripProgressBar();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            MessageLabel = new ToolStripStatusLabel();
            beepGrid1 = new BeepGrid();
            bindingSource1 = new BindingSource(components);
            toolStripButton1 = new ToolStripButton();
            tableLayoutPanel1.SuspendLayout();
            toolStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)bindingSource1).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(toolStrip1, 0, 1);
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(statusStrip1, 0, 3);
            tableLayoutPanel1.Controls.Add(beepGrid1, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 716F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
            tableLayoutPanel1.Size = new Size(830, 813);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // toolStrip1
            // 
            toolStrip1.Dock = DockStyle.Fill;
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButton1 });
            toolStrip1.Location = new Point(0, 31);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(830, 34);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // label1
            // 
            label1.BackColor = Color.White;
            label1.Dock = DockStyle.Fill;
            label1.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(824, 31);
            label1.TabIndex = 2;
            label1.Text = "Python Package Manager";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // statusStrip1
            // 
            statusStrip1.Dock = DockStyle.Fill;
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripProgressBar1, toolStripStatusLabel2, MessageLabel });
            statusStrip1.Location = new Point(0, 781);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(830, 24);
            statusStrip1.TabIndex = 0;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.BackColor = Color.White;
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(48, 19);
            toolStripStatusLabel1.Text = "Status : ";
            // 
            // toolStripProgressBar1
            // 
            toolStripProgressBar1.Name = "toolStripProgressBar1";
            toolStripProgressBar1.Size = new Size(300, 18);
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(0, 19);
            // 
            // MessageLabel
            // 
            MessageLabel.BackColor = Color.White;
            MessageLabel.Name = "MessageLabel";
            MessageLabel.Size = new Size(634, 19);
            MessageLabel.Spring = true;
            // 
            // beepGrid1
            // 
          
            beepGrid1.AllowDrop = true;
            beepGrid1.AllowUserToAddRows = true;
            beepGrid1.AllowUserToDeleteRows = true;
            beepGrid1.bindingSource = bindingSource1;
            beepGrid1.BorderStyle = BorderStyle.FixedSingle;
            beepGrid1.DataSource = bindingSource1;
           
            beepGrid1.DMEEditor = null;
            beepGrid1.Dock = DockStyle.Fill;
         
            beepGrid1.EntityStructure = null;
          
            beepGrid1.Location = new Point(5, 68);
         
            beepGrid1.Margin = new Padding(5, 3, 5, 3);
            beepGrid1.Name = "beepGrid1";
          
            beepGrid1.ReadOnly = false;
           
            beepGrid1.ShowFilterPanel = false;
            beepGrid1.ShowTotalsPanel = false;
            beepGrid1.Size = new Size(820, 710);
          
            beepGrid1.TabIndex = 3;
          
            beepGrid1.VerifyDelete = true;
         
            // 
            // bindingSource1
            // 
            bindingSource1.DataMember = "Packages";
            bindingSource1.DataSource = typeof(RuntimeEngine.ViewModels.PythonPackageManagerViewModel);
            // 
            // toolStripButton1
            // 
            toolStripButton1.Image = Beep.Python.WinformCore.Properties.Resources.RunUpdate;
            toolStripButton1.ImageTransparentColor = Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new Size(83, 31);
            toolStripButton1.Text = "Refresh All";
            // 
            // uc_PackageManagerView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tableLayoutPanel1);
            Name = "uc_PackageManagerView";
            Size = new Size(830, 813);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)bindingSource1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripProgressBar toolStripProgressBar1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel MessageLabel;
        private ToolStrip toolStrip1;
        private Label label1;
        private BeepGrid beepGrid1;
        private BindingSource bindingSource1;
        private ToolStripButton toolStripButton1;
    }
}
