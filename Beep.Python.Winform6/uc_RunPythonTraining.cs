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
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Addin;

using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;
using TheTechIdea.Beep.Container;
using TheTechIdea.Beep.Container.Services;
using Beep.Python.RuntimeEngine;
using Beep.Python.Model;

namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python Machine Learning", Name = "uc_RunPythonTraining", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_RunPythonTraining : uc_Addin
    {
        public uc_RunPythonTraining()
        {
            InitializeComponent();
            AddinName = "Python Machine Learning";
        }
        IBeepService beepService;
        IPythonRunTimeManager pythonRunTimeManager;
        public override void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            base.SetConfig(pDMEEditor, plogger, putil, args, e, per);
            beepService = DMEEditor.GetBeepService();
            pythonRunTimeManager = DMEEditor.GetPythonRunTimeManager();
        }
    }
}
