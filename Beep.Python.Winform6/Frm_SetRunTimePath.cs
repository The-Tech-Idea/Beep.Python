
using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Beep.AIBuilder.Cpython;

namespace Beep.Python.Winform
{
    public partial class Frm_SetRunTimePath : Form
    {
        public Frm_SetRunTimePath()
        {
            InitializeComponent();
        }
        public Frm_SetRunTimePath(ICPythonManager pythonManager)
        {
           
            InitializeComponent();
            CpythonManager = pythonManager;
            Savebutton.Click += Savebutton_Click;
            RunTimePathtextBox.Text = CpythonManager.RuntimePath;
            ShowFileDialogbutton.Click += ShowFileDialogbutton_Click;
            Resetbutton.Click += Resetbutton_Click;
        }

        private void Resetbutton_Click(object sender, EventArgs e)
        {
            CpythonManager.SetRuntimePath("");
            RunTimePathtextBox.Text = CpythonManager.RuntimePath;
        }

        private void ShowFileDialogbutton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result= fbd.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                RunTimePathtextBox.Text = CpythonManager.RuntimePath;
                //string[] files = Directory.GetFiles(fbd.SelectedPath);
                CpythonManager.SetRuntimePath(fbd.SelectedPath);
              
               
            }
        }

        private void Savebutton_Click(object sender, EventArgs e)
        {
            CpythonManager.SetRuntimePath(RunTimePathtextBox.Text);
            CpythonManager.FileManager.SaveConfig( CpythonManager.Config);
            this.Close();
        }

        ICPythonManager CpythonManager;
    }
}
