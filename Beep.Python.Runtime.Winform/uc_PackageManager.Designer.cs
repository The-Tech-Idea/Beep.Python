namespace Beep.Python.Runtime.Winform
{
    partial class uc_PackageManager
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            TheTechIdea.Beep.Winform.Controls.Models.BeepRowConfig beepRowConfig1 = new TheTechIdea.Beep.Winform.Controls.Models.BeepRowConfig();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(uc_PackageManager));
            beepSimpleGrid1 = new TheTechIdea.Beep.Winform.Controls.BeepSimpleGrid();
            pythonPIPManagerBindingSource = new BindingSource(components);
            MainTemplatePanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pythonPIPManagerBindingSource).BeginInit();
            SuspendLayout();
            // 
            // MainTemplatePanel
            // 
            MainTemplatePanel.Controls.Add(beepSimpleGrid1);
            MainTemplatePanel.DrawingRect = new Rectangle(0, 0, 705, 763);
            MainTemplatePanel.Size = new Size(705, 763);
            // 
            // beepSimpleGrid1
            // 
            beepRowConfig1.DisplayIndex = -1;
            beepRowConfig1.Height = 25;
            beepRowConfig1.Id = "f941bdf5-dbd9-4377-b688-1cd50c39cf2c";
            beepRowConfig1.Index = 1;
            beepRowConfig1.IsAggregation = true;
            beepRowConfig1.IsDataLoaded = false;
            beepRowConfig1.IsDeleted = false;
            beepRowConfig1.IsDirty = false;
            beepRowConfig1.IsEditable = false;
            beepRowConfig1.IsNew = false;
            beepRowConfig1.IsReadOnly = false;
            beepRowConfig1.IsSelected = false;
            beepRowConfig1.IsVisible = false;
            beepRowConfig1.OldDisplayIndex = 0;
            beepRowConfig1.RowData = null;
            beepRowConfig1.UpperX = 0;
            beepRowConfig1.UpperY = 0;
            beepRowConfig1.Width = 100;
            beepSimpleGrid1.aggregationRow = beepRowConfig1;
            beepSimpleGrid1.AnimationDuration = 500;
            beepSimpleGrid1.AnimationType = TheTechIdea.Beep.Vis.Modules.DisplayAnimationType.None;
            beepSimpleGrid1.ApplyThemeToChilds = false;
            beepSimpleGrid1.BackColor = Color.FromArgb(255, 255, 255);
            beepSimpleGrid1.BadgeBackColor = Color.Red;
            beepSimpleGrid1.BadgeFont = new Font("Arial", 8F, FontStyle.Bold);
            beepSimpleGrid1.BadgeForeColor = Color.White;
            beepSimpleGrid1.BadgeShape = TheTechIdea.Beep.Vis.Modules.BadgeShape.Circle;
            beepSimpleGrid1.BadgeText = "";
            beepSimpleGrid1.BlockID = null;
            beepSimpleGrid1.BorderColor = Color.Black;
            beepSimpleGrid1.BorderDashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
            beepSimpleGrid1.BorderRadius = 8;
            beepSimpleGrid1.BorderStyle = BorderStyle.FixedSingle;
            beepSimpleGrid1.BorderThickness = 1;
            beepSimpleGrid1.BottomoffsetForDrawingRect = 0;
            beepSimpleGrid1.BoundProperty = null;
            beepSimpleGrid1.CanBeFocused = true;
            beepSimpleGrid1.CanBeHovered = false;
            beepSimpleGrid1.CanBePressed = true;
            beepSimpleGrid1.Category = TheTechIdea.Beep.Utilities.DbFieldCategory.String;
            beepSimpleGrid1.ColumnHeaderFont = new Font("Arial", 8F);
            beepSimpleGrid1.ColumnHeaderHeight = 40;
            beepSimpleGrid1.Columns = (List<TheTechIdea.Beep.Winform.Controls.Models.BeepColumnConfig>)resources.GetObject("beepSimpleGrid1.Columns");
            beepSimpleGrid1.ComponentName = "BeepControl";
            beepSimpleGrid1.DataBindings.Add(new Binding("DataSource", pythonPIPManagerBindingSource, "", true));
            beepSimpleGrid1.DataNavigator = null;
            beepSimpleGrid1.DataSource = null;
            beepSimpleGrid1.DataSourceProperty = null;
            beepSimpleGrid1.DataSourceType = TheTechIdea.Beep.Vis.Modules.GridDataSourceType.Fixed;
            beepSimpleGrid1.DefaultColumnHeaderWidth = 50;
            beepSimpleGrid1.DisabledBackColor = Color.White;
            beepSimpleGrid1.DisabledForeColor = Color.Black;
            beepSimpleGrid1.Dock = DockStyle.Fill;
            beepSimpleGrid1.DrawInBlackAndWhite = false;
            beepSimpleGrid1.DrawingRect = new Rectangle(0, 0, 705, 763);
            beepSimpleGrid1.Easing = TheTechIdea.Beep.Vis.Modules.EasingType.Linear;
            beepSimpleGrid1.EntityName = null;
            beepSimpleGrid1.ExternalDrawingLayer = TheTechIdea.Beep.Winform.Controls.BeepControl.DrawingLayer.AfterAll;
            beepSimpleGrid1.FieldID = null;
            beepSimpleGrid1.FocusBackColor = Color.FromArgb(255, 255, 255);
            beepSimpleGrid1.FocusBorderColor = Color.Gray;
            beepSimpleGrid1.FocusForeColor = Color.FromArgb(33, 37, 41);
            beepSimpleGrid1.FocusIndicatorColor = Color.Blue;
            beepSimpleGrid1.ForeColor = Color.FromArgb(33, 37, 41);
            beepSimpleGrid1.Form = null;
            beepSimpleGrid1.GradientDirection = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            beepSimpleGrid1.GradientEndColor = Color.Gray;
            beepSimpleGrid1.GradientStartColor = Color.Gray;
            beepSimpleGrid1.GuidID = "6c1bd5b7-f3aa-443e-af7c-18e9d97b66cf";
            beepSimpleGrid1.HitAreaEventOn = false;
            beepSimpleGrid1.HitTestControl = null;
            beepSimpleGrid1.HoverBackColor = Color.FromArgb(255, 255, 255);
            beepSimpleGrid1.HoverBorderColor = Color.Gray;
            beepSimpleGrid1.HoveredBackcolor = Color.Wheat;
            beepSimpleGrid1.HoverForeColor = Color.FromArgb(33, 37, 41);
            beepSimpleGrid1.Id = -1;
            beepSimpleGrid1.InactiveBorderColor = Color.Gray;
            beepSimpleGrid1.Info = (TheTechIdea.Beep.Winform.Controls.Models.SimpleItem)resources.GetObject("beepSimpleGrid1.Info");
            beepSimpleGrid1.IsAcceptButton = false;
            beepSimpleGrid1.IsBorderAffectedByTheme = true;
            beepSimpleGrid1.IsCancelButton = false;
            beepSimpleGrid1.IsChild = false;
            beepSimpleGrid1.IsCustomeBorder = false;
            beepSimpleGrid1.IsDefault = false;
            beepSimpleGrid1.IsDeleted = false;
            beepSimpleGrid1.IsDirty = false;
            beepSimpleGrid1.IsEditable = false;
            beepSimpleGrid1.IsFocused = false;
            beepSimpleGrid1.IsFrameless = false;
            beepSimpleGrid1.IsHovered = false;
            beepSimpleGrid1.IsLogging = false;
            beepSimpleGrid1.IsNew = false;
            beepSimpleGrid1.IsPressed = false;
            beepSimpleGrid1.IsReadOnly = false;
            beepSimpleGrid1.IsRequired = false;
            beepSimpleGrid1.IsRounded = true;
            beepSimpleGrid1.IsRoundedAffectedByTheme = true;
            beepSimpleGrid1.IsSelected = false;
            beepSimpleGrid1.IsSelectedOptionOn = false;
            beepSimpleGrid1.IsShadowAffectedByTheme = true;
            beepSimpleGrid1.IsVisible = false;
            beepSimpleGrid1.Items = (List<object>)resources.GetObject("beepSimpleGrid1.Items");
            beepSimpleGrid1.LeftoffsetForDrawingRect = 0;
            beepSimpleGrid1.LinkedProperty = null;
            beepSimpleGrid1.Location = new Point(0, 0);
            beepSimpleGrid1.Name = "beepSimpleGrid1";
            beepSimpleGrid1.OverrideFontSize = TheTechIdea.Beep.Vis.Modules.TypeStyleFontSize.None;
            beepSimpleGrid1.ParentBackColor = Color.Empty;
            beepSimpleGrid1.ParentControl = null;
            beepSimpleGrid1.PercentageText = "";
            beepSimpleGrid1.PressedBackColor = Color.White;
            beepSimpleGrid1.PressedBorderColor = Color.Gray;
            beepSimpleGrid1.PressedForeColor = Color.Gray;
            beepSimpleGrid1.QueryFunction = null;
            beepSimpleGrid1.QueryFunctionName = null;
            beepSimpleGrid1.RightoffsetForDrawingRect = 0;
            beepSimpleGrid1.RowHeight = 25;
            beepSimpleGrid1.SavedGuidID = null;
            beepSimpleGrid1.SavedID = null;
            beepSimpleGrid1.SelectedBackColor = Color.FromArgb(255, 255, 255);
            beepSimpleGrid1.SelectedForeColor = Color.FromArgb(33, 37, 41);
            beepSimpleGrid1.SelectedValue = null;
            beepSimpleGrid1.SelectionColumnWidth = 30;
            beepSimpleGrid1.ShadowColor = Color.Black;
            beepSimpleGrid1.ShadowOffset = 0;
            beepSimpleGrid1.ShadowOpacity = 0.5F;
            beepSimpleGrid1.ShowAggregationRow = false;
            beepSimpleGrid1.ShowAllBorders = false;
            beepSimpleGrid1.ShowBottomBorder = false;
            beepSimpleGrid1.ShowCheckboxes = false;
            beepSimpleGrid1.ShowColumnHeaders = true;
            beepSimpleGrid1.ShowFilter = false;
            beepSimpleGrid1.ShowFocusIndicator = false;
            beepSimpleGrid1.ShowFooter = false;
            beepSimpleGrid1.ShowHeaderPanel = true;
            beepSimpleGrid1.ShowHeaderPanelBorder = true;
            beepSimpleGrid1.ShowHorizontalGridLines = true;
            beepSimpleGrid1.ShowHorizontalScrollBar = true;
            beepSimpleGrid1.ShowLeftBorder = false;
            beepSimpleGrid1.ShowNavigator = true;
            beepSimpleGrid1.ShowRightBorder = false;
            beepSimpleGrid1.ShowRowHeaders = true;
            beepSimpleGrid1.ShowRowNumbers = true;
            beepSimpleGrid1.ShowShadow = false;
            beepSimpleGrid1.ShowSortIcons = true;
            beepSimpleGrid1.ShowTopBorder = false;
            beepSimpleGrid1.ShowVerticalGridLines = true;
            beepSimpleGrid1.ShowVerticalScrollBar = true;
            beepSimpleGrid1.Size = new Size(705, 763);
            beepSimpleGrid1.SlideFrom = TheTechIdea.Beep.Vis.Modules.SlideDirection.Left;
            beepSimpleGrid1.StaticNotMoving = false;
            beepSimpleGrid1.TabIndex = 0;
            beepSimpleGrid1.Tag = MainTemplatePanel;
            beepSimpleGrid1.TempBackColor = Color.Empty;
            beepSimpleGrid1.Text = "beepSimpleGrid1";
            beepSimpleGrid1.TextImageRelation = TextImageRelation.ImageAboveText;
            beepSimpleGrid1.Theme ="DefaultTheme";
            beepSimpleGrid1.TitleHeaderImage = "simpleinfoapps.svg";
            beepSimpleGrid1.TitleText = "";
            beepSimpleGrid1.TitleTextFont = new Font("Arial", 16F);
            beepSimpleGrid1.ToolTipText = "";
            beepSimpleGrid1.TopoffsetForDrawingRect = 0;
            beepSimpleGrid1.UpdateLog = (Dictionary<DateTime, TheTechIdea.Beep.Editor.EntityUpdateInsertLog>)resources.GetObject("beepSimpleGrid1.UpdateLog");
            beepSimpleGrid1.UseGradientBackground = false;
            beepSimpleGrid1.UseThemeFont = true;
            beepSimpleGrid1.XOffset = 0;
            // 
            // pythonPIPManagerBindingSource
            // 
            pythonPIPManagerBindingSource.DataSource = typeof(RuntimeEngine.PythonNetRunTimeManager);
            // 
            // uc_PackageManager
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Name = "uc_PackageManager";
            Size = new Size(705, 763);
            MainTemplatePanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pythonPIPManagerBindingSource).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TheTechIdea.Beep.Winform.Controls.BeepSimpleGrid beepSimpleGrid1;
        private BindingSource pythonPIPManagerBindingSource;
    }
}
