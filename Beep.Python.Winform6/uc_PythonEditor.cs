

using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Addin;

using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.Editor;

using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Services;
using TheTechIdea.Beep.Winform.Default.Views.Template;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python Editor", Name = "uc_PythonEditor", misc = "AI", addinType = AddinType.Control)]
    public partial class uc_PythonEditor : TemplateUserControl
    {
        public uc_PythonEditor(IBeepService service) : base(service)
        {
            InitializeComponent();
            Details.AddinName = "Python Editor";
        }

        IPythonRunTimeManager pythonRunTimeManager;
        public  void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            
        
            pythonRunTimeManager = Editor.GetPythonRunTimeManager();

        }
        public override void Configure(Dictionary<string, object> settings)
        {
            base.Configure(settings);
            if (settings.ContainsKey("BeepService"))
            {
              
                pythonRunTimeManager = Editor.GetPythonRunTimeManager();
            }
        }
    }
}
