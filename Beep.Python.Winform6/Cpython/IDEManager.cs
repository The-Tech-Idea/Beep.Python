using AIBuilder;

using Beep.Python.Model;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Vis.Modules;
using DialogResult = System.Windows.Forms.DialogResult;

namespace TheTechIdea.Beep.AIBuilder.Cpython
{
    public class IDEManager
    {
        public IDEManager(ICPythonManager cPythonManager)
        {
            pythonManager = cPythonManager;
         
        }
        private ICPythonManager pythonManager;
      
        public ResourceManager resourceManager { get; set; } = new ResourceManager();
        public void SetupPipMenu(ToolStripMenuItem packagesToolStripMenuItem)
        {
            string pname;
            string ptitle;
            string category;
            string[] packs = pythonManager.PIPManager.packagenames.Split(',');
            string[] packscategoriesimages = pythonManager.PIPManager.packagecatgoryimages.Split(',');
            foreach (string item in packscategoriesimages)
            {
                string[] imgs = item.Split(';');
                pythonManager.PIPManager.packageCategorys.Add(new packageCategoryImages { category = imgs[0], image = imgs[1] });

            }
            foreach (string item in packs)
            {
                try
                {
                    string[] pc = item.Split(';');

                    pname = pc[0];
                    ptitle = pc[1];
                    category = pc[2];

                    pythonManager.PIPManager.packages.Add(new PackageDefinition { packagename = pname, packagetitle = ptitle, category = category, installpath = pythonManager.Packageinstallpath });
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Could not add {item}");
                }


            }
            ToolStripItem t = packagesToolStripMenuItem.DropDownItems.Add("Install pip");
            t.Click += Installpip_Click;
            t.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "install.ico");

            ToolStripItem tupdate = packagesToolStripMenuItem.DropDownItems.Add("Update pip");
            tupdate.Click += updatePIP_Click;
            tupdate.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "install.ico");
            t = packagesToolStripMenuItem.DropDownItems.Add("List Packages");
            t.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "list.ico");
            t.Click += PackagesInstalledbutton_Click;



            //ToolStripMenuItem AILMitem = new ToolStripMenuItem("ML");
            //ToolStripMenuItem GFXitem = new ToolStripMenuItem("GFX");
            //ToolStripMenuItem Toolsitem = new ToolStripMenuItem("Tools");
            //ToolStripMenuItem Computesitem = new ToolStripMenuItem("Compute");
            //ToolStripMenuItem Guisitem = new ToolStripMenuItem("GUI");
            //packagesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {AILMitem, GFXitem ,Toolsitem, Computesitem,Guisitem });

            //t.Click += InstallPythonNetbutton_Click;
            // ToolStripMenuItem
            foreach (string item in pythonManager.PIPManager.packages.Select(o => o.category).Distinct().ToList())
            {
                ToolStripMenuItem o = new ToolStripMenuItem();
                o.ImageScaling = ToolStripItemImageScaling.SizeToFit;
                o.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                o.Text = item;
                if (pythonManager.PIPManager.packageCategorys.Where(u => u.category.Equals(o.Text, StringComparison.OrdinalIgnoreCase)).Any())
                {
                    switch (item)
                    {
                        case "Gui":

                            o.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "gui.ico");
                            break;
                        case "Tools":
                            o.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "tools.ico");
                            break;
                        case "GFX":
                            o.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "gfx.ico");
                            break;
                        case "ML":
                            o.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "ml.ico");
                            break;
                        case "Compute":
                            o.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "compute.ico");
                            break;
                        default:
                            break;
                    }


                }
                //        o.Text = item;

                packagesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { o });
                foreach (PackageDefinition package in pythonManager.PIPManager.packages.Where(p => p.category == item))
                {


                    t = o.DropDownItems.Add(package.packagetitle);


                    if (pythonManager.PIPManager.checkifpackageinstalledAsync(package.packagename))
                    {
                        t.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "linked.ico");
                    }
                    else
                    {
                        t.Image = resourceManager.GetImage("Beep.Python.Winform.gfx.", "nolink.ico");
                    }
                    t.Click += PackagesToolStripMenuItem_Click;

                }
            }

        }
        public void Installpip_Click(object sender, EventArgs e)
        {
            pythonManager.ProcessManager.runPythonScriptcommandlineSync("py get-pip.py", $@"{pythonManager.BinPath}\scripts\");
        }
        public void PackagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem i = (ToolStripMenuItem)sender;
            string n = i.Text;
            string packagename = pythonManager.PIPManager.packages.Where(o => o.packagetitle.Equals(n, StringComparison.OrdinalIgnoreCase)).Select(o => o.packagename).FirstOrDefault();
            pythonManager.PIPManager.InstallPackage(packagename);
        }
        public void updatePIP_Click(object sender, EventArgs e)
        {
            pythonManager.ProcessManager.runPythonScriptcommandlineSync($@"{pythonManager.BinPath}\python.exe -m pip install --upgrade pip", $@"{pythonManager.BinPath}\scripts\");
        }
        public void PackagesInstalledbutton_Click(object sender, EventArgs e)
        {
            pythonManager.ProcessManager.runPythonScriptcommandlineSync($@"pip.exe list", $@"{pythonManager.BinPath}\scripts\");

        }
        public void SaveTextAsFile()
        {
            try
            {
                SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
                {
                    Title = "Save File",

                    DefaultExt = "py",
                    Filter = "python files(*.py) |*.py",

                    FilterIndex = 1,
                    RestoreDirectory = true

                    //ReadOnlyChecked = true,
                    //ShowReadOnly = true
                };
                saveFileDialog1.InitialDirectory = pythonManager.AiFolderpath;
                //  saveFileDialog1.Multiselect = false;
                DialogResult result = saveFileDialog1.ShowDialog();
                if (result == DialogResult.OK) // Test result.
                {
                    File.WriteAllText(saveFileDialog1.FileName, pythonManager.ScriptPath);
                }
                pythonManager.FileManager.FilenameLoaded = saveFileDialog1.FileName;
            }
            catch (Exception ex)
            {
                string errmsg = "Error in saving python script";
                pythonManager.DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }
        public void SaveTexttoFile()
        {

            try
            {
                if (pythonManager.FileManager.FilenameLoaded == null)
                {
                    SaveTextAsFile();
                }
                else
                {

                    File.WriteAllText(pythonManager.FileManager.FilenameLoaded, pythonManager.ScriptPath);
                    MessageBox.Show("ScriptPath Saved");
                }

            }
            catch (Exception ex)
            {


                string errmsg = "Error in saving python script";
                pythonManager.DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
        }
        public string LoadScriptFile(string filename)
        {
            try
            {
                string loadfilename = "";
                DialogResult result = DialogResult.None;
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                if (string.IsNullOrEmpty(filename))
                {
                    openFileDialog1 = new System.Windows.Forms.OpenFileDialog()
                    {
                        Title = "Browse Files",
                        CheckFileExists = true,
                        CheckPathExists = true,
                        DefaultExt = "py",
                        Filter = "python files(*.py) |*.py",
                        FilterIndex = 1,
                        RestoreDirectory = true

                        //ReadOnlyChecked = true,
                        //ShowReadOnly = true
                    };
                    openFileDialog1.InitialDirectory = pythonManager.AiFolderpath;
                    openFileDialog1.Multiselect = false;
                    result = openFileDialog1.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        loadfilename = openFileDialog1.FileName;
                    }
                }
                else
                {
                    if (File.Exists(filename))
                    {
                        loadfilename = filename;
                    }

                }

                String line;
                pythonManager.ScriptPath = "";
                if (!string.IsNullOrEmpty(loadfilename)) // Test result.
                {
                    //Pass the file path and file name to the StreamReader constructor
                    StreamReader sr = new StreamReader(loadfilename);
                    //Read the first line of text
                    line = sr.ReadLine();
                    //Continue to read until you reach end of file
                    while (line != null)
                    {
                        //write the lie to console window
                        pythonManager.ScriptPath += line + Environment.NewLine;
                        //Read the next line
                        line = sr.ReadLine();
                    }
                    //close the file
                    sr.Close();
                    pythonManager.FileManager.FilenameLoaded = loadfilename;
                    return pythonManager.FileManager.FilenameLoaded;
                }
                return null;
            }
            catch (Exception ex)
            {

                string errmsg = "Error in getting python script";
                pythonManager.DMEEditor.AddLogMessage("Fail", $"{errmsg}:{ex.Message}", DateTime.Now, 0, null, Errors.Failed);
                return null;
            }
        }
    }
}
