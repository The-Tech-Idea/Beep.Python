
using System.Data;

using TheTechIdea;
using TheTechIdea.Beep;
using TheTechIdea.Beep.DataBase;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Logger;
using TheTechIdea.Util;

using BeepEnterprize.Vis.Module;
using ScintillaNET;
using TheTechIdea.Beep.AIBuilder.Cpython;

using Beep.Python.Winform.Cpython;
using Beep.Python.Winform;

using Beep.Python.Model;

namespace AIBuilder.Cpython
{
    [AddinAttribute(Caption = "CPython Editor", Name = "uc_cpythonscriptrunner", misc = "AI",addinType = AddinType.Control)]
    public partial class uc_cpythonscriptrunner : UserControl,IDM_Addin
	{
		public uc_cpythonscriptrunner()
		{
			InitializeComponent();
		}
		public string ParentName { get; set; }
		public string AddinName { get; set; } = "Iron Python";
		public string Description { get; set; } = "Iron Python";
		public string ObjectName { get; set; }
		public string ObjectType { get; set; } = "UserControl";
		public Boolean DefaultCreate { get; set; } = true;
		public string DllPath { get; set; }
		public string DllName { get; set; }
		public string NameSpace { get; set; }
		public DataSet Dset { get; set; }
		public IErrorsInfo ErrorObject { get; set; }
		public IDMLogger Logger { get; set; }
		public IDMEEditor DMEEditor { get; set; }
		public EntityStructure EntityStructure { get; set; }
		public string EntityName { get; set; }
		public IPassedArgs Passedarg { get; set; }
	//	public PythonHandler Python { get; set; }
        public ICPythonManager CPythonManager { get; set; }
        public MenuManager menuManager { get; set; }
        public IDEManager iDEManager { get; set; }
        public IVisManager Visutil { get; set; }
        IVisManager visManager;
	
		IBranch RootAppBranch;
		IBranch branch;


     


        BindingSource griddatasource = new BindingSource();
		
         
		public void Run(IPassedArgs Passedarg)
		{
			
		}

		public void SetConfig(IDMEEditor pbl, IDMLogger plogger, IUtil putil, string[] args, IPassedArgs e, IErrorsInfo per)
		{
			Passedarg = e;
			Logger = plogger;
			ErrorObject = per;
			DMEEditor = pbl;
			//Python = new PythonHandler(pbl,TextArea,OutputtextBox, griddatasource);
			griddatasource.DataSourceChanged += Griddatasource_DataSourceChanged;
            Visutil = (IVisManager)e.Objects.Where(c => c.Name == "VISUTIL").FirstOrDefault().obj;
            visManager = Visutil;
            if (e.Objects.Where(c => c.Name == "Branch").Any())
			{
				branch = (IBranch)e.Objects.Where(c => c.Name == "Branch").FirstOrDefault().obj;
			}
			if (e.Objects.Where(c => c.Name == "RootAppBranch").Any())
			{
				RootAppBranch = (IBranch)e.Objects.Where(c => c.Name == "RootAppBranch").FirstOrDefault().obj;
			}
	   
			
			//foreach (string cnname in Python.GetLocalDB())
			//{
			//	this.LocalDBcomboBox.Items.Add(cnname);
			//}
			//  this.Disposed += Uc_cpythonscriptrunner_Disposed;
			this.loadToolStripMenuItem.Click += LoadFilebutton_Click;
			 this.saveToolStripMenuItem.Click += SaveFilebutton_Click;
			 this.runToolStripMenuItem.Click += RunScriptbutton_Click;
			 this.Runbutton.Click += RunScriptbutton_Click;
			 this.Savebutton.Click += SavefilepictureBox_Click;
			 this.SaveAsbutton.Click += SaveFilebutton_Click;
			 this.LoadScriptbutton.Click += LoadFilebutton_Click;
			 this.Jupiterbutton.Click += Jupiterbutton_Click;
			//this.tmpfilepathbutton.Click += Tmpfilepathbutton_Click;
			this.Insertlocaldbpathbutton.Click += Insertlocaldbpathbutton_Click;
            this.QtConsolebutton.Click += QtConsolebutton_Click;
			OutputdataGridView.DataSource = griddatasource;

            CPythonManager = new CPythonManager(DMEEditor, DMEEditor.ConfigEditor.JsonLoader);
            //PIPManager  pIP= new PIPManager( );
            //pIP.pythonManager = CPythonManager;
            //ProcessManager processManager = new ProcessManager();
            //processManager.pythonManager = CPythonManager;
            //FileManager fileManager= new FileManager();
            //fileManager.pythonManager = CPythonManager;
            //fileManager.JsonLoader=DMEEditor.ConfigEditor.JsonLoader;
            //CPythonManager.PIPManager= pIP;
            //CPythonManager.ProcessManager= processManager;
            //CPythonManager.FileManager= fileManager;
            //CPythonManager.Init();  
            menuManager = new MenuManager(CPythonManager, TextArea);
            iDEManager = new IDEManager(CPythonManager);

            CPythonManager.SendMessege += CPythonManager_SendMessege;
            LoadScriptbutton.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "load64.png");
            Savebutton.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "save64.png");
            SaveAsbutton.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "saveas64.png");
            Jupiterbutton.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "jupyter64.png");
            QtConsolebutton.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "qt64.png");
            Runbutton.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "run64.png");
            Clearoutputbutton.BackgroundImage = iDEManager.resourceManager.GetImage("AIBuilder.gfx.", "clear.png");
            Clearoutputbutton.Click += Clearoutputbutton_Click;
            LoadScriptbutton.MouseHover += AllButtons_MouseHover;
            Savebutton.MouseHover += AllButtons_MouseHover;
            SaveAsbutton.MouseHover += AllButtons_MouseHover;
            Jupiterbutton.MouseHover += AllButtons_MouseHover;
            QtConsolebutton.MouseHover += AllButtons_MouseHover;
            Runbutton.MouseHover += AllButtons_MouseHover;
            Findbutton.Click += Findbutton_Click;
            SearchText.TextChanged += SearchText_TextChanged;
            loadToolStripMenuItem.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "load.ico");
            saveToolStripMenuItem.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "saveas.ico");
            runToolStripMenuItem.Image = iDEManager.resourceManager.GetImage("Beep.Python.Winform.gfx.", "run.ico");
            runtimePathToolStripMenuItem.Click += RuntimePathToolStripMenuItem_Click;
            iDEManager.SetupPipMenu(packagesToolStripMenuItem);
            //if (CPythonManager.checkifpackageinstalled("pythonnet"))
            //{
            //    CPythonManager.InstallPythonNet();

            //}
            this.Disposed += Uc_cpythonscriptrunner_Disposed;

            // BASIC CONFIG
            TextArea.Dock = System.Windows.Forms.DockStyle.Fill;
            TextArea.TextChanged += (this.OnTextChanged);

            // INITIAL VIEW CONFIG
            TextArea.WrapMode = WrapMode.None;
            TextArea.IndentationGuides = IndentView.LookBoth;

            // STYLING
            InitColors();
            InitSyntaxColoring();

            // NUMBER MARGIN
            InitNumberMargin();

            // BOOKMARK MARGIN
            InitBookmarkMargin();

            // CODE FOLDING MARGIN
            InitCodeFolding();

            // DRAG DROP
            InitDragDropFile();

            // DEFAULT FILE
            //  LoadDataFromFile("../../MainForm.cs");
            //  TextArea.Text="";
            string retval = iDEManager.LoadScriptFile(Path.Combine(CPythonManager.AiFolderpath, "demo1.py"));
            if ( retval!= null)
            {
                TextArea.Text = CPythonManager.ScriptPath;
                Filenamelabel.Text = retval;
            }    ;
            // INIT HOTKEYS
              InitHotkeys();

           menuManager.CreateMenu(menuStrip1,Filenamelabel);
            
        }

        private void RuntimePathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Frm_SetRunTimePath frm = new Frm_SetRunTimePath(CPythonManager);
            frm.ShowDialog();
        }

        private void SearchText_TextChanged(object sender, EventArgs e)
        {
            menuManager.searchtext = SearchText.Text;
        }

        private void Findbutton_Click(object sender, EventArgs e)
        {

            menuManager.BtnNextSearch_Click (sender, e); 
        }

        public int numOutputLines { get; set; }
        private void CPythonManager_SendMessege(object sender, string e)
        {
            if (!String.IsNullOrEmpty(e))
            {
               
                //Add the text to the collected output.

                this.OutputtextBox.BeginInvoke(new Action(() =>
                {
                    this.OutputtextBox.AppendText(Environment.NewLine +
                    $">{e}");
                }));
            }

        }

        private void Clearoutputbutton_Click(object sender, EventArgs e)
        {
            OutputtextBox.Text = "";
        }

        private void AllButtons_MouseHover(object sender, EventArgs e)
        {
            PictureBox p =(PictureBox)sender;
            Hoverpanel.Left = p.Left - 6;
        }

        private void QtConsolebutton_Click(object sender, EventArgs e)
        {
            //CPythonManager.QtConsoleRun();
        }
        private void Insertlocaldbpathbutton_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(this.LocalDBcomboBox.Text))
			{
                string path = "";
				ConnectionProperties cn = DMEEditor.ConfigEditor.DataConnections.Where(o => o.ConnectionName.Equals(this.LocalDBcomboBox.Text, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
				if (cn != null)
				{
                    //
                    //scripttextBox.AppendText(Path.Combine(cn.FilePath, cn.FileName));
                    if (cn.FilePath.StartsWith("."))
                    {
                        path= cn.FilePath.Remove(0,1);
                        char[] invalidPathChars = Path.GetInvalidPathChars();
                        path = DMEEditor.ConfigEditor.ExePath + path + cn.FileName;
                       
                    }else
                        path = Path.Combine(cn.FilePath,  cn.FileName);
                    Clipboard.SetText(path);
				}
			   
			}
			
		}
		private void Tmpfilepathbutton_Click(object sender, EventArgs e)
		{
			//scripttextBox.AppendText(Path.Combine(Python.aifolder,"tmp.csv"));
            Clipboard.SetText(Path.Combine(CPythonManager.AiFolderpath, "tmp.csv"));
        }
		private void Griddatasource_DataSourceChanged(object sender, EventArgs e)
		{
			OutputdataGridView.Refresh();
		}
		private void Uc_cpythonscriptrunner_Disposed(object sender, EventArgs e)
			{
				// Python.JupiterStop();
               //  Python.QtConsoleStop();
			  //  runPythonScriptscommandline($@"jupyter notebook stop ", $@"{aifolder}");
		  
			}
		private void Jupiterbutton_Click(object sender, EventArgs e)
			{
				// Python.JupiterRun();
			   // runPythonScriptscommandline($@"jupyter notebook ", $@"{aifolder}");
			}
		private void SavefilepictureBox_Click(object sender, EventArgs e)
			{

           iDEManager.SaveTexttoFile();

			}
		private void RunScriptbutton_Click(object sender, EventArgs e)
			{
	  
			try
			{
				this.TextArea.Text = this.TextArea.Text.Replace("BeepPath",DMEEditor.ConfigEditor.ExePath);
				this.TextArea.Text = this.TextArea.Text.Replace("BeepLib", $"{Path.Combine(DMEEditor.ConfigEditor.ExePath, "lib")}");
				this.TextArea.Text = this.TextArea.Text.Replace("BeepClasses",$"{Path.Combine(DMEEditor.ConfigEditor.ExePath, "ProjectClasses")}");
				this.TextArea.Text = this.TextArea.Text.Replace("BeepDrivers", $"{Path.Combine(DMEEditor.ConfigEditor.ExePath, "ConnectionDrivers")}");
				this.TextArea.Text = this.TextArea.Text.Replace("BeepOtherDLL", $"{Path.Combine(DMEEditor.ConfigEditor.ExePath, "OtherDLL")}");

                CPythonManager.ProcessManager.RunScript( this.TextArea.Text );
			}
			catch (Exception ex)
			{
				

			}


		}
		private void SaveFilebutton_Click(object sender, EventArgs e)
		{
            iDEManager.SaveTextAsFile();
		}
		private void LoadFilebutton_Click(object sender, EventArgs e)
		{
            string retval = iDEManager.LoadScriptFile(null);
            if (retval != null)
            {
                TextArea.Text = CPythonManager.ScriptPath;
                Filenamelabel.Text = retval;
            }

        }
        private void OnTextChanged(object sender, EventArgs e)
        {

        }
        private void InitSyntaxColoring()
        {

            // Configure the default style
            TextArea.StyleResetDefault();
            TextArea.Styles[Style.Default].Font = "Consolas";
            TextArea.Styles[Style.Default].Size = 10;
            TextArea.Styles[Style.Default].BackColor = menuManager.IntToColor(0x212121);
            TextArea.Styles[Style.Default].ForeColor = menuManager.IntToColor(0xFFFFFF);
            TextArea.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            TextArea.Styles[Style.Cpp.Identifier].ForeColor = menuManager.IntToColor(0xD0DAE2);
            TextArea.Styles[Style.Cpp.Comment].ForeColor = menuManager.IntToColor(0xBD758B);
            TextArea.Styles[Style.Cpp.CommentLine].ForeColor = menuManager.IntToColor(0x40BF57);
            TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = menuManager.IntToColor(0x2FAE35);
            TextArea.Styles[Style.Cpp.Number].ForeColor = menuManager.IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.String].ForeColor = menuManager.IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.Character].ForeColor = menuManager.IntToColor(0xE95454);
            TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = menuManager.IntToColor(0x8AAFEE);
            TextArea.Styles[Style.Cpp.Operator].ForeColor = menuManager.IntToColor(0xE0E0E0);
            TextArea.Styles[Style.Cpp.Regex].ForeColor = menuManager.IntToColor(0xff00ff);
            TextArea.Styles[Style.Cpp.CommentLineDoc].  ForeColor = menuManager.IntToColor(0x77A7DB);
            TextArea.Styles[Style.Cpp.Word].ForeColor = menuManager.IntToColor(0x48A8EE);
            TextArea.Styles[Style.Cpp.Word2].ForeColor = menuManager.IntToColor(0xF98906);
            TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = menuManager.IntToColor(0xB3D991);
            TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = menuManager.IntToColor(0xFF0000);
            TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = menuManager.IntToColor(0x48A8EE);

            TextArea.Lexer = Lexer.Python;

            TextArea.SetKeywords(0, "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield");
            TextArea.SetKeywords(1, "void Null ArgumentError arguments Array Boolean Class Date DefinitionError Error EvalError Function int Math Namespace Number Object RangeError ReferenceError RegExp SecurityError String SyntaxError TypeError uint XML XMLList Boolean Byte Char DateTime Decimal Double Int16 Int32 Int64 IntPtr SByte Single UInt16 UInt32 UInt64 UIntPtr Void Path File System Windows Forms ScintillaNET");

        }
        #region Numbers, Bookmarks, Code Folding

        /// <summary>
        /// the background color of the text area
        /// </summary>
        private const int BACK_COLOR = 0x2A211C;

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private const int FORE_COLOR = 0xB7B7B7;

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        /// <summary>
        /// change this to whatever margin you want the bookmarks/breakpoints to show in
        /// </summary>
        private const int BOOKMARK_MARGIN = 2;
        private const int BOOKMARK_MARKER = 2;

        /// <summary>
        /// change this to whatever margin you want the code folding tree (+/-) to show in
        /// </summary>
        private const int FOLDING_MARGIN = 3;

        /// <summary>
        /// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
        /// </summary>
        private const bool CODEFOLDING_CIRCULAR = true;

        private void InitNumberMargin()
        {

            TextArea.Styles[Style.LineNumber].BackColor = menuManager.IntToColor(BACK_COLOR);
            TextArea.Styles[Style.LineNumber].ForeColor = menuManager.IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].ForeColor = menuManager.IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].BackColor = menuManager.IntToColor(BACK_COLOR);

            var nums = TextArea.Margins[NUMBER_MARGIN];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            TextArea.MarginClick += TextArea_MarginClick;
        }

        private void InitBookmarkMargin()
        {

            //TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

            var margin = TextArea.Margins[BOOKMARK_MARGIN];
            margin.Width = 20;
            margin.Sensitive = true;
            margin.Type = MarginType.Symbol;
            margin.Mask = (1 << BOOKMARK_MARKER);
            //margin.Cursor = MarginCursor.Arrow;

            var marker = TextArea.Markers[BOOKMARK_MARKER];
            marker.Symbol = MarkerSymbol.Circle;
            marker.SetBackColor(menuManager.IntToColor(0xFF003B));
            marker.SetForeColor(menuManager.IntToColor(0x000000));
            marker.SetAlpha(100);

        }

        private void InitCodeFolding()
        {

            TextArea.SetFoldMarginColor(true, menuManager.IntToColor(BACK_COLOR));
            TextArea.SetFoldMarginHighlightColor(true, menuManager.IntToColor(BACK_COLOR));

            // Enable code folding
            TextArea.SetProperty("fold", "1");
            TextArea.SetProperty("fold.compact", "1");

            // Configure a margin to display folding symbols
            TextArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
            TextArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
            TextArea.Margins[FOLDING_MARGIN].Sensitive = true;
            TextArea.Margins[FOLDING_MARGIN].Width = 20;

            // Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                TextArea.Markers[i].SetForeColor(menuManager.IntToColor(BACK_COLOR)); // styles for [+] and [-]
                TextArea.Markers[i].SetBackColor(menuManager.IntToColor(FORE_COLOR)); // styles for [+] and [-]
            }

            // Configure folding markers with respective symbols
            TextArea.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
            TextArea.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
            TextArea.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
            TextArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            TextArea.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
            TextArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            TextArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            TextArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

        }

        private void TextArea_MarginClick(object sender, MarginClickEventArgs e)
        {
            if (e.Margin == BOOKMARK_MARGIN)
            {
                // Do we have a marker for this line?
                const uint mask = (1 << BOOKMARK_MARKER);
                var line = TextArea.Lines[TextArea.LineFromPosition(e.Position)];
                if ((line.MarkerGet() & mask) > 0)
                {
                    // Remove existing bookmark
                    line.MarkerDelete(BOOKMARK_MARKER);
                }
                else
                {
                    // Add bookmark
                    line.MarkerAdd(BOOKMARK_MARKER);
                }
            }
        }

        #endregion
        #region Drag & Drop File

        public void InitDragDropFile()
        {

            TextArea.AllowDrop = true;
            TextArea.DragEnter += delegate (object sender, DragEventArgs e) {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };
            TextArea.DragDrop += delegate (object sender, DragEventArgs e) {

                // get file drop
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {

                    Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
                    if (a != null)
                    {

                        string path = a.GetValue(0).ToString();

                        LoadDataFromFile(path);

                    }
                }
            };

        }

        private void LoadDataFromFile(string path)
        {
            if (File.Exists(path))
            {
                Filenamelabel.Text = Path.GetFileName(path);
                TextArea.Text = File.ReadAllText(path);
            }
        }

        #endregion
      
        private void InitColors()
        {

            TextArea.SetSelectionBackColor(true, menuManager.IntToColor(0x114D9C));

        }
        private void InitHotkeys()
        {

            // register the hotkeys with the WaitForm
            //HotKeyManager.AddHotKey(visManager.MainForm, menuManager.OpenSearch, Keys.F, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.OpenFindDialog, Keys.F, true, false, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.OpenReplaceDialog, Keys.R, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.OpenReplaceDialog, Keys.H, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.Uppercase, Keys.U, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.Lowercase, Keys.L, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.ZoomIn, Keys.Oemplus, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.ZoomOut, Keys.OemMinus, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.ZoomDefault, Keys.D0, true);
            //HotKeyManager.AddHotKey(this.visManager.MainForm, menuManager.CloseSearch, Keys.Escape);

            // remove conflicting hotkeys from scintilla
            TextArea.ClearCmdKey(Keys.Control | Keys.F);
            TextArea.ClearCmdKey(Keys.Control | Keys.R);
            TextArea.ClearCmdKey(Keys.Control | Keys.H);
            TextArea.ClearCmdKey(Keys.Control | Keys.L);
            TextArea.ClearCmdKey(Keys.Control | Keys.U);

        }



       

    }
}