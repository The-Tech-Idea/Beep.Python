using CommunityToolkit.Mvvm.ComponentModel;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;

using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using Python.Runtime;

namespace Beep.Python.Logic.ViewModels

{
    public partial class AIAlgorithimsViewModel : PythonBaseViewModel
    {
        [ObservableProperty]
        string selectedAlgorithim;
        [ObservableProperty]
        List<GenericLOVData> listofAlgorithims=new List<GenericLOVData> ();
        [ObservableProperty]
        Python_Algorithim currentAlgorithim;
        [ObservableProperty]
        PythonDataClasses currentDataClass;
        [ObservableProperty]
        string myAIlibraryfolder;
        public ObservableBindingList<Python_Algorithim> Algorithms => Unitofwork.Units;
        public UnitofWork<Python_Algorithim> Unitofwork { get; set; }
        public UnitofWork<PythonDataClasses> DataClassUnits;
        public AIAlgorithimsViewModel(PythonNetRunTimeManager pythonRuntimeManager, PyModule persistentScope) : base(pythonRuntimeManager, persistentScope)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            _persistentScope = persistentScope;
            Unitofwork = new UnitofWork<Python_Algorithim>(Editor, "dhubdb", "AI_ALGORITHIMS", "ID");
            DataClassUnits=new UnitofWork<PythonDataClasses>(Editor, "dhubdb", "AI_DATACLASSES", "ID");
            Unitofwork.Sequencer = "AI_ALGORITHIMS_SEQ";
            Unitofwork.PostCreate += AlgorithmsunitofWork_PostCreate;
            ListofAlgorithims = new List<GenericLOVData>();
     
            foreach (var item in Enum.GetNames(typeof(MachineLearningAlgorithm)))
            {
                GenericLOVData data=new GenericLOVData() { ID= item, DisplayValue = item,LOVDESCRIPTION= MLAlgorithmsHelpers.GenerateAlgorithmDescription((MachineLearningAlgorithm)Enum.Parse(typeof(MachineLearningAlgorithm),item)) }; 
                ListofAlgorithims.Add(data);
            }
          
        }
      
        private void AlgorithmsunitofWork_PostCreate(object? sender, UnitofWorkParams e)
        {
            Python_Algorithim doc= (Python_Algorithim)sender;
            if (CurrentDataClass != null)
            {
                    doc.DATACLASS_ID = CurrentDataClass.ID;
            }
            doc.ROW_CREATE_DATE = DateTime.Now;
            doc.ROW_CREATE_BY = DhubConfig.userManager.User.KOCNO;
            currentAlgorithim=doc;
            

        }
        public string GetAlgorithimName(string algorithim)
        {
            return Enum.GetName(typeof(MachineLearningAlgorithm), algorithim);
        }
        public string GetPath()
        {
            MyAIlibraryfolder = System.IO.Path.Combine(DhubConfig.Library.GetMyPath(), CurrentDataClass.NAME);
            string retval = string.Empty;
            Directory.CreateDirectory(MyAIlibraryfolder);
            retval = MyAIlibraryfolder;
            if (CurrentAlgorithim.ALGORITHIM != null)
            {
                Directory.CreateDirectory(System.IO.Path.Combine(MyAIlibraryfolder, CurrentAlgorithim.ALGORITHIM));
                retval= System.IO.Path.Combine(MyAIlibraryfolder, CurrentAlgorithim.ALGORITHIM);
            }
            return retval;
        }
        
        public Python_Algorithim Get(double DataClassid)
        {
             Unitofwork.Get(new List<TheTechIdea.Beep.Report.AppFilter>() { new TheTechIdea.Beep.Report.AppFilter() { FieldName="DATACLASS_ID", Operator ="=", FilterValue=DataClassid.ToString()} ,
                                                                                                          new TheTechIdea.Beep.Report.AppFilter() { FieldName="ROW_CREATE_BY", Operator ="=", FilterValue=DhubConfig.userManager.User.KOCNO}});

            if (Unitofwork.Units.Count > 0)
            {
                CurrentAlgorithim = Unitofwork.Units.FirstOrDefault();
            }
           
            CurrentDataClass = DataClassUnits.Get(DataClassid.ToString());
            return CurrentAlgorithim;
        }
        public void CreateAlgorathims(string algorithim,double DataClassid)
        {
            CurrentAlgorithim = new Python_Algorithim() { ALGORITHIM = algorithim, DATACLASS_ID = DataClassid };
            Unitofwork.Create(CurrentAlgorithim);
           
        }
        public void Get()
        {
            Unitofwork.Get(new List<TheTechIdea.Beep.Report.AppFilter>() { new TheTechIdea.Beep.Report.AppFilter() { FieldName="ROW_CREATE_BY", Operator ="=", FilterValue=DhubConfig.userManager.User.KOCNO}});    
        }
        public void SubmitTrainingFile(string filenameandpath)
        {
            CurrentAlgorithim.TRAINFILENAME = Path.GetFileName(filenameandpath);
            CurrentAlgorithim.TRAINFILEPATH = Path.GetDirectoryName(filenameandpath);
            File.Copy(filenameandpath, Path.Combine(GetPath(),Path.GetFileName(filenameandpath)), true);
        }
    }
}
