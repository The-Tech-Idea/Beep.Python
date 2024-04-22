﻿using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using Beep.Python.RuntimeEngine.ViewModels;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Winform.Controls.Basic;
using TheTechIdea.Logger;
using TheTechIdea.Util;
using TheTechIdea.Beep.Container;

namespace Beep.Python.Winform
{
    [AddinAttribute(Caption = "Python AI Project", Name = "uc_createaiproject", misc = "AI", addinType = AddinType.Control)]

    public partial class uc_createaiproject : uc_Addin
    {
        public uc_createaiproject()
        {
            InitializeComponent();
        }
        public PythonAIProjectViewModel pythonAIProjectViewModel { get; set; }
        IPythonRunTimeManager pythonRunTimeManager;
        public override void SetConfig(IDMEEditor pDMEEditor, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
        {
            base.SetConfig(pDMEEditor, plogger, putil, args, e, per);
            Passedarg=e;
            pythonRunTimeManager = pDMEEditor.GetPythonRunTimeManager();
            pythonAIProjectViewModel = new PythonAIProjectViewModel(pDMEEditor.GetBeepService(), pythonRunTimeManager);
            pythonAIProjectViewModel.initialize();
            if (Passedarg!=null)
            {
                if(Passedarg.EventType=="CREATEAI")
                {
                    pythonAIProjectViewModel.CreateProject(Passedarg.CurrentEntity);
                }
                if(Passedarg.EventType=="EDITAI")
                {
                    pythonAIProjectViewModel.Get(Passedarg.CurrentEntity);
                }
            }
            pythonAIProjectViewModel.Progress = new Progress<PassedArgs>();
            pythonAIProjectViewModel.Editor = pDMEEditor;
            pythonAIProjectViewModelBindingSource.DataSource = pythonAIProjectViewModel;
        }
    }
}
