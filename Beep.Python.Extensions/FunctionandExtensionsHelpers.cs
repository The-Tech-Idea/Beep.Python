using Beep.Vis.Module;
 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;
using Beep.Python.Model;
using DataManagementModels.DriversConfigurations;

namespace Beep.Python.Extensions
{
    public class FunctionandExtensionsHelpers
    {
        public IDMEEditor DMEEditor { get; set; }
        public IPassedArgs Passedargs { get; set; }
        public IVisManager Vismanager { get; set; }
        public IControlManager Controlmanager { get; set; }
        public ITree TreeEditor { get; set; }

        CancellationTokenSource tokenSource;

        CancellationToken token;
        public  IPythonRunTimeManager cpythonManager { get; set; }
        public IDataSource DataSource { get; set; }
        public IBranch pbr { get; set; }
        public IBranch RootBranch { get; set; }
        public IBranch ParentBranch { get; set; }
        public IBranch ViewRootBranch { get; set; }
        public FunctionandExtensionsHelpers(IDMEEditor pdMEEditor, IVisManager pvisManager, ITree ptreeControl, IPythonRunTimeManager pythonManager)
        {
            DMEEditor = pdMEEditor;
            Vismanager = pvisManager;
            TreeEditor = ptreeControl;
            cpythonManager = pythonManager;
            GetValues(DMEEditor.Passedarguments);
        }
        public void GetValues(IPassedArgs Passedarguments)
        {
            if (Passedarguments.Objects.Where(c => c.Name == "VISUTIL").Any())
            {
                Vismanager = (IVisManager)Passedarguments.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            }
            if (Passedarguments.Objects.Where(c => c.Name == "TreeControl").Any())
            {
                TreeEditor = (ITree)Passedarguments.Objects.Where(c => c.Name == "TreeControl").FirstOrDefault().obj;
            }
           
            if (Passedarguments.Objects.Where(c => c.Name == "ControlManager").Any())
            {
                Controlmanager = (IControlManager)Passedarguments.Objects.Where(c => c.Name == "ControlManager").FirstOrDefault().obj;
            }
          

            if (Passedarguments.Objects.Where(i => i.Name == "Branch").Any())
            {
                Passedarguments.Objects.Remove(Passedarguments.Objects.Where(c => c.Name == "Branch").FirstOrDefault());
            }
            if (Passedarguments.Id > 0)
            {
                pbr = TreeEditor.treeBranchHandler.GetBranch(Passedarguments.Id);
            }

            if (Passedarguments.Objects.Where(i => i.Name == "RootBranch").Any())
            {
                Passedarguments.Objects.Remove(Passedarguments.Objects.Where(c => c.Name == "RootBranch").FirstOrDefault());
            }

            if (Passedarguments.Objects.Where(i => i.Name == "ParentBranch").Any())
            {
                Passedarguments.Objects.Remove(Passedarguments.Objects.Where(c => c.Name == "ParentBranch").FirstOrDefault());
            }
            if (pbr != null)
            {
                Passedarguments.DatasourceName = pbr.DataSourceName;
                Passedarguments.CurrentEntity = pbr.BranchText;
                if (pbr.ParentBranchID > 0)
                {
                    ParentBranch = TreeEditor.treeBranchHandler.GetBranch(pbr.ParentBranchID);
                    Passedarguments.Objects.Add(new ObjectItem() { Name = "ParentBranch", obj = ParentBranch });
                }
                Passedarguments.Objects.Add(new ObjectItem() { Name = "Branch", obj = pbr });
                if(pbr.BranchType!= EnumPointType.Root)
                {
                    int idx = TreeEditor.Branches.FindIndex(x => x.BranchClass == pbr.BranchClass && x.BranchType == EnumPointType.Root);
                    if (idx > 0)
                    {
                        RootBranch = TreeEditor.Branches[idx];
                       
                    }
                  
                }
                else
                {
                    RootBranch = pbr;
                }
                
                Passedarguments.Objects.Add(new ObjectItem() { Name = "RootBranch", obj = RootBranch });
            }


         
            if (Passedarguments.DatasourceName != null)
            {
                DataSource = DMEEditor.GetDataSource(Passedarguments.DatasourceName);
                DMEEditor.OpenDataSource(Passedarguments.DatasourceName);
            }



            ViewRootBranch = TreeEditor.Branches[TreeEditor.Branches.FindIndex(x => x.BranchClass == "VIEW" && x.BranchType == EnumPointType.Root)];
        }
        public virtual List<ConnectionProperties> LoadFiles()
        {
            List<ConnectionProperties> retval = new List<ConnectionProperties>();
            try
            {
                string extens = DMEEditor.ConfigEditor.CreateFileExtensionString();
                List<string> filenames = new List<string>();
                filenames = Vismanager.Controlmanager.LoadFilesDialog("*", DMEEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.DataFiles).FirstOrDefault().FolderPath, extens);
                foreach (String file in filenames)
                {
                    {
                        ConnectionProperties f = new ConnectionProperties
                        {
                            FileName = Path.GetFileName(file),
                            FilePath = Path.GetDirectoryName(file),
                            Ext = Path.GetExtension(file).Replace(".", "").ToLower(),
                            ConnectionName = Path.GetFileName(file)


                        };
                        if (f.FilePath.Contains(DMEEditor.ConfigEditor.ExePath))
                        {
                            f.FilePath.Replace(DMEEditor.ConfigEditor.ExePath, ".");
                        }
                        if (f.FilePath.Contains(DMEEditor.ConfigEditor.Config.DataFilePath))
                        {
                            f.FilePath.Replace(DMEEditor.ConfigEditor.Config.DataFilePath, ".");
                        }
                        if (f.FilePath.Contains(DMEEditor.ConfigEditor.Config.ProjectDataPath))
                        {
                            f.FilePath.Replace(DMEEditor.ConfigEditor.Config.ProjectDataPath, ".");
                        }
                        string ext = Path.GetExtension(file).Replace(".", "").ToLower();
                        List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null && p.Favourite == true).ToList();
                        ConnectionDriversConfig c = clss.Where(o => o.extensionstoHandle.Contains(ext) && o.Favourite == true).FirstOrDefault();
                        if (c is null)
                        {
                            c = clss.Where(o => o.classHandler.Equals("CSVDataSource", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        }
                        if (c != null)
                        {
                            f.DriverName = c.PackageName;
                            f.DriverVersion = c.version;
                            f.Category = c.DatasourceCategory;

                            switch (f.Ext.ToLower())
                            {
                                case "txt":
                                    f.DatabaseType = DataSourceType.Text;
                                    break;
                                case "csv":
                                    f.DatabaseType = DataSourceType.CSV;
                                    break;
                                case "xml":
                                    f.DatabaseType = DataSourceType.XML;

                                    break;
                                case "json":
                                    f.DatabaseType = DataSourceType.Json;
                                    break;
                                case "xls":
                                case "xlsx":
                                    f.DatabaseType = DataSourceType.Xls;
                                    break;
                                default:
                                    f.DatabaseType = DataSourceType.Text;
                                    break;
                            }
                            f.Category = DatasourceCategory.FILE;
                            retval.Add(f);

                        }
                        else
                        {
                            DMEEditor.AddLogMessage("Beep", $"Could not Load File {f.ConnectionName}", DateTime.Now, -1, null, Errors.Failed);
                        }

                    }



                }
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
                return null;
            };

        }
        public virtual void LoadDataSetFiles(IBranch br)
        {
            try
            {
                List<ConnectionProperties> files = new List<ConnectionProperties>();
                files = LoadFiles();
                foreach (ConnectionProperties f in files)
                {
                    DMEEditor.ConfigEditor.AddDataConnection(f);
                    DMEEditor.GetDataSource(f.FileName);
                   // CreateFileNode(f.ID, f.FileName, f.ConnectionName);
                }
                DMEEditor.ConfigEditor.SaveDataconnectionsValues();
                DMEEditor.AddLogMessage("Success", "Added Database Connection", DateTime.Now, 0, null, Errors.Ok);
            }
            catch (Exception ex)
            {
                string mes = "Could not Add Database Connection";
                DMEEditor.AddLogMessage(ex.Message, mes, DateTime.Now, -1, mes, Errors.Failed);
            };
        }
        public virtual ConnectionProperties LoadFile()
        {
            ConnectionProperties retval = new ConnectionProperties();
            try
            {
                string pextens = DMEEditor.ConfigEditor.CreateFileExtensionString();
                string pfilename = Vismanager.Controlmanager.LoadFileDialog("*", DMEEditor.ConfigEditor.Config.ProjectDataPath, pextens);
                string pFileName = Path.GetFileName(pfilename);
                string pFilePath = Path.GetDirectoryName(pfilename);
                string pExt = Path.GetExtension(pfilename).Replace(".", "").ToLower();
                if (!pFilePath.Contains(@"ProjectData")){
                    if (AskToCopyFile(pFileName, pFilePath))
                    {
                        pFilePath = @".\ProjectData";
                    }
                }
                
                ConnectionProperties f = new ConnectionProperties
                {
                    FileName = Path.GetFileName(pfilename),
                    Ext = Path.GetExtension(pfilename).Replace(".", "").ToLower(),
                    FilePath= pFilePath,
                    ConnectionName = Path.GetFileName(pfilename)
                };
                if (f.FilePath.Contains(DMEEditor.ConfigEditor.ExePath))
                {
                    pFilePath= @".";
                }
                if (f.FilePath.StartsWith(@DMEEditor.ConfigEditor.Config.DataFilePath, StringComparison.InvariantCultureIgnoreCase))
                {
                    string pathafter = pFilePath.Substring(pFilePath.IndexOf("DataFiles")+9 );
                    if (string.IsNullOrEmpty(pathafter))
                    {
                        pFilePath = @".\DataFiles";
                    }
                    else
                    {
                        pFilePath = @".\DataFiles" + pathafter;
                    }
                    f.FilePath = pFilePath;
                }
                if (f.FilePath.StartsWith(@DMEEditor.ConfigEditor.Config.ProjectDataPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    string pathafter= pFilePath.Substring(pFilePath.IndexOf("ProjectData")+11);
                    if (string.IsNullOrEmpty(pathafter))
                    {
                        pFilePath = @".\ProjectData";
                    }
                    else
                    {
                        pFilePath = @".\ProjectData" + pathafter;
                    }
                    f.FilePath = pFilePath;
                }
                string ext = Path.GetExtension(pfilename).Replace(".", "").ToLower();
                List<ConnectionDriversConfig> clss = DMEEditor.ConfigEditor.DataDriversClasses.Where(p => p.extensionstoHandle != null && p.Favourite == true).ToList();
                ConnectionDriversConfig c = clss.Where(o => o.extensionstoHandle.Contains(ext) && o.Favourite == true).FirstOrDefault();
                if (c is null)
                {
                    c = clss.Where(o => o.classHandler.Equals("CSVDataSource", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                }
                if (c != null)
                {
                    f.DriverName = c.PackageName;
                    f.DriverVersion = c.version;
                    f.Category = c.DatasourceCategory;

                    switch (f.Ext.ToLower())
                    {
                        case "txt":
                            f.DatabaseType = DataSourceType.Text;
                            break;
                        case "csv":
                            f.DatabaseType = DataSourceType.CSV;
                            break;
                        case "xml":
                            f.DatabaseType = DataSourceType.XML;

                            break;
                        case "json":
                            f.DatabaseType = DataSourceType.Json;
                            break;
                        case "xls":
                        case "xlsx":
                            f.DatabaseType = DataSourceType.Xls;
                            break;
                        default:
                            f.DatabaseType = DataSourceType.Text;
                            break;
                    }
                    f.Category = DatasourceCategory.FILE;
                    retval = f;

                }
                else
                {
                    DMEEditor.AddLogMessage("Beep", $"Could not Load File {f.ConnectionName}", DateTime.Now, -1, null, Errors.Failed);
                }
                return retval;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Load Files ", DateTime.Now, -1, mes, Errors.Failed);
              //  Vismanager.CloseWaitForm();
                return null;
            };

        }
        public virtual bool AskToCopyFile(string filename,string sourcPath)
        {
           
            try
            {
                if (Vismanager.Controlmanager.InputBoxYesNo("Beep AI",$"Would you Like to Copy File {filename} to Local Folders?")== DialogResult.OK)
                {
                    CopyFileToLocal(sourcPath, DMEEditor.ConfigEditor.Config.ProjectDataPath,filename);
                }
                return true;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Copy File", DateTime.Now, -1, mes, Errors.Failed);
                //  Vismanager.CloseWaitForm();
                return false;
            }
        }
        public virtual bool CopyFileToLocal(string sourcePath, string destinationPath, string filename)
        {
          
            try
            {
                File.Copy(Path.Combine(sourcePath, filename), Path.Combine(destinationPath, filename)); 
                return true;
            }
            catch (Exception ex)
            {
                string mes = ex.Message;
                DMEEditor.AddLogMessage(ex.Message, "Could not Copy File", DateTime.Now, -1, mes, Errors.Failed);
                //  Vismanager.CloseWaitForm();
                return false;
            }
        }
        public virtual string ChangeLibPackageFolder()
        {
            string path =null;

            string packepath=cpythonManager.CurrentRuntimeConfig.Packageinstallpath;
            if (Vismanager.Controlmanager.InputBox("Beep AI", $"Please Enter New Packages Folder ({packepath}) ", ref path) == DialogResult.OK)
            {

            }
            else
                path = null;
                
            return path;
        }
        public virtual string ChangeRunTimeFolder()
        {
            string path = null;

            string packepath = cpythonManager.CurrentRuntimeConfig.RuntimePath;
            if (Vismanager.Controlmanager.InputBox("Beep AI", $"Please Enter New Runtime Folder ({packepath}) ", ref path) == DialogResult.OK)
            {

            }
            else
                path = null;

            return path;
        }
       
    }
}
