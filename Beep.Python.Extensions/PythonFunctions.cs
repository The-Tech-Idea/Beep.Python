using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using Beep.Vis.Module;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;

namespace Beep.Python.Extensions
{
    [AddinAttribute(Caption = "Python", Name = "PythonFunctions", ObjectType = "Beep", menu = "Beep", misc = "IFunctionExtension", addinType = AddinType.Class, iconimage = "Python.png", order = 1)]
    public class PythonFunctions : IFunctionExtension
    {
        public IDMEEditor DMEEditor { get; set; }
        public IPassedArgs Passedargs { get; set; }

        private FunctionandExtensionsHelpers ExtensionsHelpers;


        public PythonFunctions(IDMEEditor pdMEEditor, IVisManager pvisManager, ITree ptreeControl)
        {
            DMEEditor = pdMEEditor;
            
            ExtensionsHelpers = new FunctionandExtensionsHelpers(DMEEditor, pvisManager, ptreeControl, DMEEditor.GetPythonRunTimeManager());

        }
        [CommandAttribute(Caption = "Python Manager", Name = "PythonManager", Click = true, iconimage = "newproject.png", ObjectType = "Beep", PointType = EnumPointType.Global)]
        public IErrorsInfo PythonManager(IPassedArgs Passedarguments)
        {
            DMEEditor.ErrorObject.Flag = Errors.Ok;
            try
            {

                ExtensionsHelpers.GetValues(Passedarguments);
                if(ExtensionsHelpers.pbr!=null)
                {
                  if(ExtensionsHelpers.pbr.BranchType== EnumPointType.Entity)
                  {
                        Passedarguments.DatasourceName= ExtensionsHelpers.pbr.DataSourceName;
                        Passedarguments.CurrentEntity= ExtensionsHelpers.pbr.BranchText;
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
        [CommandAttribute(Caption = "Create AI Project", Name = "PythonManagerDataPoint", Click = true, iconimage = "createai.png", ObjectType = "Beep", PointType = EnumPointType.Entity)]
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
    }
}
