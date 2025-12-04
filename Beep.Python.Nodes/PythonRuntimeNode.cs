using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Vis.Modules;
using Errors = TheTechIdea.Beep.ConfigUtil.Errors;
using SystemEnvironment = System.Environment;

namespace Beep.Python.Nodes
{
    [AddinAttribute(Caption = "Python RunTime", BranchType = EnumPointType.Function, Name = "pythonruntime.Beep", misc = "Beep", iconimage = "pythonruntime.svg", menu = "Beep", ObjectType = "Beep")]
    public class PythonRuntimeNode : IBranch
    {
        public PythonRuntimeNode()
        {
            
        }

        public int ID { get ; set ; }
        public bool Visible { get ; set ; }
        public IDMEEditor DMEEditor { get ; set ; }
        public IDataSource DataSource { get ; set ; }
        public string DataSourceName { get ; set ; }
        public List<IBranch> ChildBranchs { get ; set ; }= new List<IBranch>();
        public IBranch ParentBranch { get ; set ; }
        public ITree TreeEditor { get ; set ; }
        public IAppManager Visutil { get ; set ; }
        public List<string> BranchActions { get ; set ; }= new List<string>();
        public EntityStructure EntityStructure { get ; set ; }
        public string ObjectType { get; set; } = "Python Runtime";
        public int MiscID { get ; set ; }
        public bool IsDataSourceNode { get ; set ; }
        public string MenuID { get ; set ; }
        public string GuidID { get ; set ; }= Guid.NewGuid().ToString();
        public string ParentGuidID { get ; set ; }
        public string DataSourceConnectionGuidID { get ; set ; }
        public string EntityGuidID { get ; set ; }
        public string MiscStringID { get ; set ; }
        public string Name { get ; set ; }
        public string BranchText { get ; set ; }= "Python Runtime";
        public int Level { get ; set ; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Function;
        public int BranchID { get ; set ; }
        public string IconImageName { get ; set ; }= "pythonruntime.svg";
        public string BranchStatus { get ; set ; }
        public int ParentBranchID { get ; set ; }
        public string BranchDescription { get ; set ; }
        public string BranchClass { get ; set ; } = "Python Runtime";

        public PythonRunTime PythonRunTime { get; set; } = new PythonRunTime();
        
        public IBranch CreateCategoryNode(CategoryFolder p)
        {
            throw new NotImplementedException();
        }

        public IErrorsInfo CreateChildNodes()
        {
            try
            {
                // Create virtual environment nodes - using the VirtualEnvironments collection from PythonRunTime
                if (PythonRunTime?.VirtualEnvironments != null)
                {
                    foreach (var runtimeEnv in PythonRunTime.VirtualEnvironments)
                    {
                        // Create a new branch for each virtual environment
                        // if not already exists
                        if (runtimeEnv == null || string.IsNullOrEmpty(runtimeEnv.ID) || string.IsNullOrEmpty(runtimeEnv.RuntimePath))
                            continue;
                            
                        PythonVirtualEnvNode node = ChildBranchs.FirstOrDefault(n => n is PythonVirtualEnvNode && n.GuidID == runtimeEnv.ID) as PythonVirtualEnvNode;
                        if (node == null)
                        {
                            node = new PythonVirtualEnvNode();
                            node.GuidID = runtimeEnv.ID;
                            node.DMEEditor = DMEEditor;
                            node.TreeEditor = TreeEditor;
                            node.ParentBranch = this;
                            node.ID = TreeEditor.SeqID;
                            node.PythonRunTime = PythonRunTime;
                            
                            // Convert PythonRunTime to PythonVirtualEnvironment
                            node.VirtualEnvironment = new PythonVirtualEnvironment
                            {
                                ID = runtimeEnv.ID,
                                Name = runtimeEnv.Name ?? "Virtual Environment",
                                Path = runtimeEnv.RuntimePath,
                                PythonConfigID = PythonRunTime.ID,
                                BaseInterpreterPath = PythonRunTime.RuntimePath,
                                PythonVersion = runtimeEnv.PythonVersion,
                                PythonBinary = runtimeEnv.Binary,
                                CreatedBy = "system",
                                CreatedOn = DateTime.Now
                            };
                            
                            node.BranchText = runtimeEnv.Name ?? runtimeEnv.RuntimePath;

                            TreeEditor.Treebranchhandler.AddBranch(this, node);
                        }
                    }
                }
                else
                {
                    // Log that no virtual environments are available
                    DMEEditor.AddLogMessage("Info", "No virtual environments found for this Python runtime", DateTime.Now, 0, null, TheTechIdea.Beep.ConfigUtil.Errors.Ok);
                }
            }
            catch (Exception ex)
            {
                DMEEditor.AddLogMessage("Error", $"Could not create child nodes: {ex.Message}", DateTime.Now, -1, null, Errors.Failed);
                throw;
            }
            return DMEEditor.ErrorObject;
        }

        public IErrorsInfo ExecuteBranchAction(string ActionName)
        {
            throw new NotImplementedException();
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

            }
            catch (Exception)
            {
                throw;
            }
            return DMEEditor.ErrorObject;
        }
    }
}