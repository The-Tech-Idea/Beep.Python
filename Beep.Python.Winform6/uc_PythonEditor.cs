
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
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Services;

namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python Editor", Name = "uc_PythonEditor", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_PythonEditor : uc_Addin
    {
        public uc_PythonEditor()
        {
            InitializeComponent();
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
