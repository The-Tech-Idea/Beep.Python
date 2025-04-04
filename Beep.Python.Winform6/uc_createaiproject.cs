using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Vis;

using TheTechIdea.Beep.Addin;

using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;
using TheTechIdea.Beep.Container;
using Beep.Python.RuntimeEngine.Services;
using TheTechIdea.Beep.Winform.Default.Views.Template;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python AI Project", Name = "uc_createaiproject", misc = "AI", addinType = AddinType.Control)]

    public partial class uc_createaiproject :TemplateUserControl 
    {
        public uc_createaiproject(IBeepService service) : base(service)
        {
            InitializeComponent();
            Details.AddinName = "Python Machine Learning Project Editor";
        }

        public PythonAIProjectViewModel pythonAIProjectViewModel { get; set; }
        IPythonRunTimeManager pythonRunTimeManager;
        public  void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
           
            pythonRunTimeManager = pDMEEditor.GetPythonRunTimeManager();
            pythonAIProjectViewModel = new PythonAIProjectViewModel(pDMEEditor.GetBeepService(), pythonRunTimeManager);
            pythonAIProjectViewModel.initialize();
           
            pythonAIProjectViewModel.Progress = new Progress<PassedArgs>();
            pythonAIProjectViewModel.Editor = pDMEEditor;
            pythonAIProjectViewModelBindingSource.DataSource = pythonAIProjectViewModel;
        }
       
    }
}
