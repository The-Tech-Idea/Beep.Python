﻿
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;


namespace Beep.Python.Extensions
{
    [AddinAttribute(Caption = "Python", Name = "PythonFunctions", ObjectType = "Beep", menu = "Beep", misc = "IFunctionExtension", addinType = AddinType.Class, iconimage = "python.png", order = 1)]
    public class PythonFunctions : IFunctionExtension
    {
        public IDMEEditor DMEEditor { get; set; }
        public IPassedArgs Passedargs { get; set; }

        private IFunctionandExtensionsHelpers ExtensionsHelpers;


        public PythonFunctions(IDMEEditor pdMEEditor, IAppManager pvisManager, ITree ptreeControl)
        {
            DMEEditor = pdMEEditor;
            
            ExtensionsHelpers = ptreeControl.ExtensionsHelpers;

        }
        [CommandAttribute(Caption = "Python Manager", Name = "PythonManager", Click = true, iconimage = "pythonnewproject.png", ObjectType = "Beep", PointType = EnumPointType.Global, Showin = ShowinType.Menu)]
        public IErrorsInfo PythonManager(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

                ExtensionsHelpers.GetValues(Passedarguments);
                if(ExtensionsHelpers.CurrentBranch!=null)
                {
                  if(ExtensionsHelpers.CurrentBranch.BranchType== EnumPointType.Entity)
                  {
                        Passedarguments.DatasourceName= ExtensionsHelpers.CurrentBranch.DataSourceName;
                        Passedarguments.CurrentEntity= ExtensionsHelpers.CurrentBranch.BranchText;
                        Passedarguments.EventType = "CREATEAI";
                        ExtensionsHelpers.Vismanager.ShowPage("uc_pythonmanager", (PassedArgs)Passedarguments, DisplayType.InControl);
                  }    
                }
                
                // DMEEditor.AddLogMessage("Success", $"Open Data Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not create new project {ex.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        [CommandAttribute(Caption = "Create AI Project", Name = "PythonManagerDataPoint", Click = true, iconimage = "createai.png", ObjectType = "Beep", PointType = EnumPointType.Entity, Showin = ShowinType.Menu)]
        public IErrorsInfo PythonManagerDataPoint(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

                ExtensionsHelpers.GetValues(Passedarguments);
                ExtensionsHelpers.Vismanager.ShowPage("uc_createaiproject", (PassedArgs)Passedarguments, DisplayType.InControl);
                // DMEEditor.AddLogMessage("Success", $"Open Data Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Fail", $"Could not create new project {ex.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
            }
            return DMEEditor.ErrorObject;

        }
        //[CommandAttribute(Caption = "Create ML Training ", Name = "PythonTrainingDataPoint", Click = true, iconimage = "createai.png", ObjectType = "Beep", PointType = EnumPointType.Entity, Showin = ShowinType.Menu)]
        //public IErrorsInfo PythonTrainingDataPoint(IPassedArgs Passedarguments)
        //{
        //    DMEEditor.ErrorObject.Flag = Errors.Ok;
        //    try
        //    {

        //        ExtensionsHelpers.GetValues(Passedarguments);
        //        ExtensionsHelpers.Vismanager.ShowPage("uc_RunPythonTraining", (PassedArgs)Passedarguments, DisplayType.InControl);
        //        // DMEEditor.AddLogMessage("Success", $"Open Data Connection", DateTime.Now, 0, null, Errors.Ok);
        //    }
        //    catch (Exception ex)
        //    {
        //        DMEEditor.AddLogMessage("Fail", $"Could not create new project {ex.Message}", DateTime.Now, 0, Passedarguments.DatasourceName, Errors.Failed);
        //    }
        //    return DMEEditor.ErrorObject;

        //}
    }
}
