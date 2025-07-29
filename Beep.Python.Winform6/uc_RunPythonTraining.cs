using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
using Beep.Python.Model;

using TheTechIdea.Beep.Winform.Default.Views.Template;

namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python Machine Learning", Name = "uc_RunPythonTraining", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_RunPythonTraining : TemplateUserControl
    {
        public uc_RunPythonTraining(IBeepService service) : base(service)
        {
            InitializeComponent();

        Details.AddinName = "Python Machine Learning";
        }
      
        IPythonRunTimeManager pythonRunTimeManager;
        public  void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
           
        
            pythonRunTimeManager = Editor.GetPythonRunTimeManager();
        }
    }
}
