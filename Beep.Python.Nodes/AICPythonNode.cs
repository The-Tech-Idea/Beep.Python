﻿
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;
using Beep.Python.RuntimeEngine.Helpers;
using Beep.Python.Model;




namespace Beep.Python.Nodes
{

    [AddinVisSchema(BranchType = EnumPointType.Root, BranchClass = "PYTHONROOT", RootNodeName = "PYTHONROOTNode")]
    [AddinAttribute(Caption ="Python",misc = "PYHTON", FileType = "PYHTON", iconimage = "pythonroot.svg",menu ="PYHTON",ObjectType ="Beep", ClassType = "LJ")]
    public class AICPythonNode : IBranch
    {
        private IPythonVirtualEnvManager env;
        private IPythonRunTimeManager runmanger;

        public bool IsDataSourceNode { get; set; } = false;
        public string GuidID { get; set; } = Guid.NewGuid().ToString();
        public string ParentGuidID { get; set; }
        public string DataSourceConnectionGuidID { get; set; }
        public string EntityGuidID { get; set; }
        public string MiscStringID { get; set; }
        public bool Visible { get; set; } = true;
        public string MenuID { get; set; }
        //public AICPythonNode(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename)
        //{

        //    BranchText = "Python";
        //    BranchClass = "Python";
        //    IconImageName = "pythonroot.svg";
        //    BranchType = EnumPointType.Root;

        //}
        public AICPythonNode()
        {

         
        }
      

        #region "Properties"
        public int ID { get; set; }
        public EntityStructure EntityStructure { get; set; }
        public string Name { get; set; }
        public string BranchText { get; set; } = "Python";
        public IDMEEditor DMEEditor { get; set; }
        public IDataSource DataSource { get; set; }
        public string DataSourceName { get; set; }
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Root;
        public int BranchID { get; set; }
        public string IconImageName { get; set; } = "pythonroot.svg";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; }
        public string BranchClass { get; set; } = "Python";
        public List<IBranch> ChildBranchs { get; set; } = new List<IBranch>();
        public ITree TreeEditor { get; set; }
        public List<string> BranchActions { get; set; }=new List<string>();
        public object TreeStrucure { get; set; }
        public IAppManager Visutil { get; set; }
        public int MiscID { get; set; }
        public string ObjectType { get; set; } = "Beep.Python";
        public AddinTreeStructure AddinTreeStructure { get; set; }
         public IBranch ParentBranch { get  ; set  ; }

        #endregion "Properties"
        #region "Interface Methods"
        public IErrorsInfo CreateChildNodes()
        {

            try
            {
                CreateNodes();

                DMEEditor.AddLogMessage("Success", "Added Child Nodes", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Child Nodes";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }

        public IErrorsInfo ExecuteBranchAction(string ActionName)
        {
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo MenuItemClicked(string ActionNam)
        {
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo RemoveChildNodes()
        {
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo SetConfig(ITree pTreeEditor, IDMEEditor pDMEEditor, IBranch pParentNode, string pBranchText, int pID, EnumPointType pBranchType, string pimagename)
        {
            try
            {
                TreeEditor = pTreeEditor;
                DMEEditor = pDMEEditor;
      ///          ParentBranchID = pParentNode.ID;
                BranchText = pBranchText;
                BranchType = pBranchType;
                IconImageName = pimagename;
                if (pID != 0)
                {
                    ID = pID;
                }

                //   DMEEditor.AddLogMessage("Success", "Set Config OK", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Set Config";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        #endregion "Interface Methods"
        #region "Exposed Interface"
        [CommandAttribute(Caption = "Script Editor", Hidden = false, DoubleClick = true)]
        public IErrorsInfo ScriptEditor()
        {

            try
            {
                string[] args = { BranchText };
                PassedArgs Passedarguments = new PassedArgs
                {  // Obj= obj,
                    Addin = null,
                    AddinName = null,
                    AddinType = null,
                    DMView = null,
                    CurrentEntity = BranchText,
                    ObjectName = BranchText,
                    Id = BranchID,
                    ObjectType = null,
                    DataSource = null,
                    EventType = "Run"

                };

                Visutil.ShowLogWindow = false;
                Passedarguments.Objects.Add(new ObjectItem() { Name = "TitleText", obj = $"Python Editor" });
                Visutil.ShowPage("uc_PythonEditor",  Passedarguments, DisplayType.InControl);




                DMEEditor.AddLogMessage("Success", "Shown Module " + BranchText, DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Show Module " + BranchText;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;
        }
        [CommandAttribute(Caption = "Refresh Runtimes",iconimage = "refreshpythonenv.svg", Hidden = false, DoubleClick = true)]
        public IErrorsInfo refresh()
        {

            try
            {
                CreateNodes();
            }
            catch (Exception ex)
            {
                string mes = "Could not Show Module " + BranchText;
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            }
            ;
            return DMEEditor.ErrorObject;
        }

        #endregion Exposed Interface"
        #region "Other Methods"
        public IErrorsInfo CreateNodes()
        {

            try
            {
                // Create the child nodes for the Python root node
                //if (ChildBranchs == null)
                //    ChildBranchs = new List<IBranch>();
                foreach (var runtime in PythonEnvironmentDiagnostics.PythonRunTimes)
                {
                    // Create a new branch for each runtime
                    // if not already exists
                    if( runtime == null || string.IsNullOrEmpty(runtime.ID) || string.IsNullOrEmpty(runtime.RuntimePath))
                        continue;   
                    PythonRuntimeNode node=ChildBranchs.FirstOrDefault(n => n is PythonRuntimeNode && n.GuidID == runtime.ID) as PythonRuntimeNode;
                    if (node == null)
                    {
                        node = new PythonRuntimeNode();
                        node.GuidID = runtime.ID;
                        node.DMEEditor = DMEEditor;
                        node.TreeEditor = TreeEditor;
                        node.ParentBranch = this;
                        node.ID = TreeEditor.SeqID;
                        node.PythonRunTime = runtime;

                        node.BranchText= runtime.RuntimePath;
                     //   ChildBranchs.Add(node);

                      //  TreeEditor.AddBranchToParentInBranchsOnly(this, node);
                        TreeEditor.Treebranchhandler.AddBranch(this, node);
                        node.CreateChildNodes();
                    }
                }

            }
            catch (Exception ex)
            {
                string mes = "Could not Create child Nodes";
                if(DMEEditor!= null)             DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
            return DMEEditor.ErrorObject;

        }

        public IBranch CreateCategoryNode(CategoryFolder p)
        {
            throw new NotImplementedException();
        }
        #endregion"Other Methods"
    }
}
