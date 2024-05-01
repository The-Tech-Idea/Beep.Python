using CommunityToolkit.Mvvm.ComponentModel;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Editor;
using DataManagementModels.Editor;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using System.Collections.Generic;
using Beep.Python.RuntimeEngine.ViewModels;
using TheTechIdea.Beep.Container.Services;
using System;
using System.IO;
using System.Linq;

namespace KOC.ViewModels.AI
{
    public partial class AIAlgorithimsViewModel : PythonBaseViewModel
    {
        [ObservableProperty]
        string selectedAlgorithim;
        [ObservableProperty]
        List<LOVData> listofAlgorithims=new List<LOVData> ();
        [ObservableProperty]
        PythonAlgorithm currentAlgorithim;
        [ObservableProperty]
        PythonDataClasses currentDataClass;
        [ObservableProperty]
        string myAIlibraryfolder;
        public ObservableBindingList<PythonAlgorithm> Algorithms => Unitofwork.Units;
        public UnitofWork<PythonAlgorithm> Unitofwork { get; set; }
        public UnitofWork<PythonDataClasses> DataClassUnits;
        public AIAlgorithimsViewModel(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager) : base(beepservice, pythonRuntimeManager)
        {
            Unitofwork = new UnitofWork<PythonAlgorithm>(Editor, "dhubdb", "PythonAlgorithm", "ID");
            DataClassUnits = new UnitofWork<PythonDataClasses>(Editor, "dhubdb", "PythonDataClasses", "ID");
            Unitofwork.Sequencer = "PythonAlgorithm_SEQ";
            Unitofwork.PostCreate += AlgorithmsunitofWork_PostCreate;
            ListofAlgorithims = new List<LOVData>();
            foreach (var item in Enum.GetNames(typeof(MachineLearningAlgorithm)))
            {
                LOVData data=new LOVData() { ID= item, DisplayValue = item,LOVDESCRIPTION= MLAlgorithmsHelpers.GenerateAlgorithmDescription((MachineLearningAlgorithm)Enum.Parse(typeof(MachineLearningAlgorithm),item)) }; 
                ListofAlgorithims.Add(data);
            }
          
        }
      
        private void AlgorithmsunitofWork_PostCreate(object? sender, UnitofWorkParams e)
        {
            PythonAlgorithm doc= (PythonAlgorithm)sender;
            if (CurrentDataClass != null)
            {
                    doc.DATACLASS_ID = CurrentDataClass.ID;
            }
            doc.ROW_CREATE_DATE = DateTime.Now;
          
            CurrentAlgorithim=doc;
            

        }
        public string GetAlgorithimName(string algorithim)
        {
            return Enum.GetName(typeof(MachineLearningAlgorithm), algorithim);
        }
        public string GetPath()
        {
            MyAIlibraryfolder = System.IO.Path.Combine(PythonDatafolder, CurrentDataClass.NAME);
            string retval = string.Empty;
            Directory.CreateDirectory(MyAIlibraryfolder);
            retval = MyAIlibraryfolder;
            if (CurrentAlgorithim.ALGORITHM != null)
            {
                Directory.CreateDirectory(System.IO.Path.Combine(MyAIlibraryfolder, CurrentAlgorithim.ALGORITHM));
                retval = System.IO.Path.Combine(MyAIlibraryfolder, CurrentAlgorithim.ALGORITHM);
            }
            return retval;
        }
        public PythonAlgorithm Get(double DataClassid)
        {
             Unitofwork.Get(new List<TheTechIdea.Beep.Report.AppFilter>() { new TheTechIdea.Beep.Report.AppFilter() { FieldName="DATACLASS_ID", Operator ="=", FilterValue=DataClassid.ToString()}});

            if (Unitofwork.Units.Count > 0)
            {
                CurrentAlgorithim = Unitofwork.Units.FirstOrDefault();
            }
           
           
            return CurrentAlgorithim;
        }
        public void CreateAlgorathims(string algorithim,double DataClassid)
        {
            CurrentAlgorithim = new PythonAlgorithm() { ALGORITHM = algorithim, DATACLASS_ID = DataClassid };
            Unitofwork.Create(CurrentAlgorithim);
           
        }
        public void Get()
        {
            Unitofwork.Get();
            //Unitofwork.Get(new List<TheTechIdea.Beep.Report.AppFilter>() { new TheTechIdea.Beep.Report.AppFilter() { FieldName="ROW_CREATE_BY", Operator ="=", FilterValue=DhubConfig.userManager.User.KOCNO}});    
        }
        public void SubmitTrainingFile(string filenameandpath)
        {
            CurrentAlgorithim.TRAINFILENAME = Path.GetFileName(filenameandpath);
            CurrentAlgorithim.TRAINFILEPATH = Path.GetDirectoryName(filenameandpath);
            File.Copy(filenameandpath, Path.Combine(GetPath(),Path.GetFileName(filenameandpath)), true);
        }
    }
}
