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


namespace Beep.Python.RuntimeEngine.ViewModels
{
    public partial class AIAlgorithimParametersViewModel : PythonBaseViewModel,IDisposable
    {
     

       
     
        [ObservableProperty]
        PythonDataClasses currentDataClass;
        [ObservableProperty]
        PythonAlgorithm currentAlgorithim;
        string aiCompFileDirName;
        [ObservableProperty]
        string[] features;
        [ObservableProperty]
        string[] selectedfeatures;
        [ObservableProperty]
        string myAIlibraryfolder;
        public ObservableBindingList<PythonalgorithmParams> AlgorithmsParameters => Unitofwork.Units;
        public UnitofWork<PythonalgorithmParams> Unitofwork;
        UnitofWork<PythonDataClasses> DataClassUnits;
        PythonAlgorithimsViewModel aIAlgorithimsViewModel;
        private bool disposedValue;

       
        public AIAlgorithimParametersViewModel(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager) : base(beepservice, pythonRuntimeManager)
        {
            Unitofwork = new UnitofWork<PythonalgorithmParams>(Editor, "dhubdb", "PythonalgorithmParams", "ID");
            Unitofwork.Sequencer = "AI_ALGORTHIMSPARAMS_SEQ";
          
            Unitofwork.PostCreate += AlgorithmsParametersunitofWork_PostCreate;
            aIAlgorithimsViewModel=new PythonAlgorithimsViewModel(beepservice, pythonRuntimeManager);
            DataClassUnits = new UnitofWork<PythonDataClasses>(Editor, "dhubdb", "PythonDataClasses", "ID");
         
            aiCompFileDirName = "/AIComp";
           
        }
     
       
        private void AlgorithmsParametersunitofWork_PostCreate(object? sender, UnitofWorkParams e)
        {
            PythonalgorithmParams doc = (PythonalgorithmParams)sender;
            if (CurrentDataClass != null)
            {
                if (doc.DATACLASS_ID == 0)
                {
                    doc.DATACLASS_ID = CurrentDataClass.ID;
                
                }

            }
            doc.ROW_CREATE_DATE = DateTime.Now;
         
            doc.ALGORITHIM_ID = CurrentAlgorithim.ID;

        }
        public void Get(double dataclass_id,double algorithim_id)
        {
            Unitofwork.Get(new List<TheTechIdea.Beep.Report.AppFilter>() { new TheTechIdea.Beep.Report.AppFilter() { FieldName="DATACLASS_ID", Operator ="=", FilterValue=dataclass_id.ToString()} ,
                                                                                                new TheTechIdea.Beep.Report.AppFilter() { FieldName="ALGORITHIM_ID", Operator ="=", FilterValue=algorithim_id.ToString()}});
            CurrentAlgorithim = aIAlgorithimsViewModel.Unitofwork.Get(algorithim_id.ToString());
            if (CurrentAlgorithim != null)
            {
                CurrentDataClass = DataClassUnits.Get(CurrentAlgorithim.DATACLASS_ID.ToString());
            }
        }
        public void Get(double algorithimid)
        {
            Unitofwork.Get(new List<TheTechIdea.Beep.Report.AppFilter>() { new TheTechIdea.Beep.Report.AppFilter() { FieldName = "ALGORITHIM_ID", Operator = "=", FilterValue = algorithimid.ToString() } });

            
            CurrentAlgorithim = aIAlgorithimsViewModel.Unitofwork.Get(algorithimid.ToString());
            if (CurrentAlgorithim != null)
            {
               CurrentDataClass = DataClassUnits.Get(CurrentAlgorithim.DATACLASS_ID.ToString());
            }
            if (AlgorithmsParameters.Count == 0)
            {
                CreateParameters();
            }
        }
        public void CreateParameters()
        {
           
            foreach (var item in ParameterDictionaryForAlgorithms.Where(p=>p.Algorithm.ToString()==CurrentAlgorithim.ALGORITHM))
            {
                if (!Unitofwork.Units.Any(p => p.PARAMETERNAME == item.ParameterName))
                {
                    PythonalgorithmParams doc = new PythonalgorithmParams();
                    doc.ALGORITHIM_ID = CurrentAlgorithim.ID;
                    doc.DATACLASS_ID = CurrentDataClass.ID;
                    doc.PARAMETERNAME = item.ParameterName;

                    doc.PARAMETERDESCRIPTION = item.Description + $" - example : ({item.Example})";
                    doc.ROW_CREATE_DATE = DateTime.Now;
                  //  doc.ROW_CREATE_BY = DhubConfig.userManager.User.KOCNO;
                    Unitofwork.Create(doc);
                }
              
            }
        }
     
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    aIAlgorithimsViewModel=null;
                    Unitofwork.Dispose();
                   
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }


        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AIAlgorithimParametersViewModel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
