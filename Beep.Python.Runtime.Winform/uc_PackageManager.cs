
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;

using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Winform.Default.Views.Template;

namespace Beep.Python.Runtime.Winform
{
    [AddinAttribute(Caption = "Python Package Manager", Name = "uc_PackageManager", misc = "Config", menu = "Configuration", addinType = AddinType.Control, displayType = DisplayType.InControl, ObjectType = "Beep")]
    [AddinVisSchema(BranchID = 1, RootNodeName = "Configuration", Order = 1, ID = 1, BranchText = "Python Package Manager", BranchType = EnumPointType.Function, IconImageName = "pythonpackagemanager.svg", BranchClass = "ADDIN", BranchDescription = "Python Package Manager")]

    public partial class uc_PackageManager : TemplateUserControl, IAddinVisSchema
    {
        public uc_PackageManager(IBeepService service) : base(service)
        {
            InitializeComponent();

            Details.AddinName = "Python Package Manager";
        }

        #region "IAddinVisSchema"
        public string RootNodeName { get; set; } = "Configuration";
        public string CatgoryName { get; set; }
        public int Order { get; set; } = 1;
        public int ID { get; set; } = 1;
        public string BranchText { get; set; } = "Python Package Manager";
        public int Level { get; set; }
        public EnumPointType BranchType { get; set; } = EnumPointType.Function;
        public int BranchID { get; set; } = 1;
        public string IconImageName { get; set; } = "pythonpackagemanager.svg";
        public string BranchStatus { get; set; }
        public int ParentBranchID { get; set; }
        public string BranchDescription { get; set; } = "Python Package Manager";
        public string BranchClass { get; set; } = "ADDIN";
        public string AddinName { get; set; }
        #endregion "IAddinVisSchema"
        public override void OnNavigatedTo(Dictionary<string, object> parameters)
        {
            base.OnNavigatedTo(parameters);
            


        }
        public override void Configure(Dictionary<string, object> settings)
        {
            base.Configure(settings);
           
        }

    }
}
