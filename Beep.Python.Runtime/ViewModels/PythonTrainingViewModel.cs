﻿using Beep.Python.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using TheTechIdea.Beep.Container.Services;


namespace Beep.Python.RuntimeEngine.ViewModels
{
    public partial class PythonTrainingViewModel :PythonBaseViewModel
    {
        [ObservableProperty]
        float testsize;
        [ObservableProperty]
        string testfilePath;
        [ObservableProperty]
        string trainfilePath;
        [ObservableProperty]
        string modelId;
        [ObservableProperty]
        string filename;
        [ObservableProperty]
        string entityname;
        [ObservableProperty]
        string datasourcename;
        [ObservableProperty]
        bool isFile;
     
      
        [ObservableProperty]
        MachineLearningAlgorithm selectAlgorithm;
        [ObservableProperty]
        List<ParameterDictionaryForAlgorithm> parameterDictionaryForAlgorithms;
        [ObservableProperty]
        bool isDataReady;
        [ObservableProperty]
        bool isTrainingReady;
        [ObservableProperty]
        bool isTrainDataLoaded;
        [ObservableProperty]
        bool isModelTrained;
        [ObservableProperty]
        bool isModelEvaluated;
        [ObservableProperty]
        bool isModelPredicted;
        [ObservableProperty]
        double mseScore;
        [ObservableProperty]
        double rmseScore;
        [ObservableProperty]
        double maeScore;
        [ObservableProperty]
        double f1Accuracy;
        [ObservableProperty]
        double evalScore;
        [ObservableProperty]
        bool isInit;
        [ObservableProperty]
        Dictionary<string,object> parameters;
        [ObservableProperty]
        string[] features;
        [ObservableProperty]
        string[] labels;
        [ObservableProperty]
        string labelColumn;
        [ObservableProperty]
        string[] selectedFeatures;
        IPythonMLManager PythonMLManager;
       
        public PythonTrainingViewModel(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager, PythonSessionInfo sessionInfo) : base(beepservice, pythonRuntimeManager, sessionInfo)
        {
           
          
            
        }
        public void ResetTraining()
        {
            ModelId = string.Empty;
            IsTrainingReady = false;
            IsBusy = false;
            IsDataReady = false;
            IsTrainDataLoaded = false;
            IsModelEvaluated = false;
            IsModelPredicted = false;
        }
        public void init(IPythonMLManager mLManager)
        {
           
            IsInit = true;
            ResetTraining();
            PythonMLManager = mLManager;
        }
        public IErrorsInfo Train()
        {
            if(IsDataReady)
            {
                Editor.AddLogMessage("Beep", $"Error : Data has to be split first and features and Label should be selected", DateTime.Now, 0, null, Errors.Failed);
                return Editor.ErrorObject; ;
            }
            if (IsFile)
            {
                if(string.IsNullOrEmpty(Filename))
                {
                    Editor.AddLogMessage("Beep", $"Error : missing Filename {Filename}", DateTime.Now, 0, null, Errors.Failed);
                    return Editor.ErrorObject; ;
                }
            }
            if(SelectedFeatures==null)
            {
                Editor.AddLogMessage("Beep", $"Error : missing Features List", DateTime.Now, 0, null, Errors.Failed);
                return Editor.ErrorObject; ;
            }
            if (SelectedFeatures.Count()==0)
            {
                Editor.AddLogMessage("Beep", $"Error : missing Features List", DateTime.Now, 0, null, Errors.Failed);
                return Editor.ErrorObject; ;
            }
            if(string.IsNullOrEmpty(LabelColumn))
            {
                Editor.AddLogMessage("Beep", $"Error : missing Label Feature", DateTime.Now, 0, null, Errors.Failed);
                return Editor.ErrorObject; ;
            }
            try
            {
                if(string.IsNullOrEmpty(ModelId)) { 
                    ModelId = Guid.NewGuid().ToString();
                }
                PythonMLManager.TrainModel(ModelId, SelectAlgorithm, Parameters, SelectedFeatures, LabelColumn);
                IsTrainingReady = true;
            }
            catch (Exception ex)
            {
                IsTrainingReady = false;
                Editor.AddLogMessage("Beep", $"Error running training - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
           
            return Editor.ErrorObject; ;
        }
        public IErrorsInfo SplitData()
        {
            if (IsFile)
            {
                if (string.IsNullOrEmpty(Filename))
                {
                    Editor.AddLogMessage("Beep", $"Error : missing Filename {Filename}", DateTime.Now, 0, null, Errors.Failed);
                    return Editor.ErrorObject; ;
                }
            }
            if (string.IsNullOrEmpty(TrainfilePath))
            {
                Editor.AddLogMessage("Beep", $"Error : missing Train file Path ", DateTime.Now, 0, null, Errors.Failed);
                return Editor.ErrorObject; ;
            }
            if (string.IsNullOrEmpty(TestfilePath))
            {
                Editor.AddLogMessage("Beep", $"Error : missing Test file Path ", DateTime.Now, 0, null, Errors.Failed);
                return Editor.ErrorObject; ;
            }
            if (Testsize< 0.5 || Testsize>= 0.7)
            {
                Editor.AddLogMessage("Beep", $"Error : Test size should be between 0.5 and 0.7  ", DateTime.Now, 0, null, Errors.Failed);
                return Editor.ErrorObject; ;
            }
            try
            {
                PythonMLManager.SplitData(Filename, Testsize, TrainfilePath, TestfilePath);
                IsDataReady = true;
            }
            catch (Exception ex)
            {
                IsDataReady = false;
                Editor.AddLogMessage("Beep", $"Error splitting data - {ex.Message}", DateTime.Now, 0, null, Errors.Failed);
            }
            return Editor.ErrorObject; ;
        }
       

    }
}
