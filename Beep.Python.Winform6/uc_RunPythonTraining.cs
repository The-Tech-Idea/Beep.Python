using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheTechIdea.Beep.Winform.Controls.Basic;

namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python Package List", Name = "uc_PackageList", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_RunPythonTraining : uc_Addin
    {
        public uc_RunPythonTraining()
        {
            InitializeComponent();
        }
    }
}
