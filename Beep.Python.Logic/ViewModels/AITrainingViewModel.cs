using CommunityToolkit.Mvvm.ComponentModel;

using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using Python.Runtime;

namespace Beep.Python.Logic.ViewModels
{
    public partial class AITrainingViewModel : PythonBaseViewModel, IDisposable
    {
        [ObservableProperty]
        bool isFeaturesReady=false;
        [ObservableProperty]
        bool isBusy;
        [ObservableProperty]
        bool isReady;
        [ObservableProperty]
        bool isError;
        [ObservableProperty]
        bool isSuccess;
        [ObservableProperty]
        bool isDataReady=false;
        [ObservableProperty]
        float splitratio=0.6F;
        [ObservableProperty]
        bool isInit;
        [ObservableProperty]
        bool isTraining;
        [ObservableProperty]
        bool isTesting;
        [ObservableProperty]
        bool isPredicting;
        [ObservableProperty]
        bool isModelSaved;
        [ObservableProperty]
        bool isModelLoaded;
        [ObservableProperty]
        bool isModelTrained;
        [ObservableProperty]
        bool isModelTested;
        [ObservableProperty]
        bool isModelPredicted;
        [ObservableProperty]
        bool isModelEvaluated;
        [ObservableProperty]
        bool isModelExported;
        [ObservableProperty]
        bool isModelImported;
        [ObservableProperty]
        bool isModelDeployed;
        [ObservableProperty]
        bool isModelServed;
        [ObservableProperty]
        bool isModelSavedToDB;
        [ObservableProperty]
        bool isModelLoadedFromDB;
        [ObservableProperty]
        bool isModelTrainedFromDB;
        [ObservableProperty]
        bool isTrainDataLoaded;
        [ObservableProperty]
        bool isTestDataLoaded;
        [ObservableProperty]
        bool isTrainingReady;
        [ObservableProperty]
        bool isResultSubmitted=false;
        [ObservableProperty]
        bool isSubmittionFileGenerated = false;

        [ObservableProperty]
        double f1Accuracy;
        [ObservableProperty]
        double trainScore;
        [ObservableProperty]
        double testScore;
        [ObservableProperty]
        double predictScore;
        [ObservableProperty]
        double evalScore;
        [ObservableProperty]
        double exportScore;
        [ObservableProperty]
        double importScore;
        [ObservableProperty]
        double dataClassid;
        [ObservableProperty]
        double algorithimid;


        [ObservableProperty]
        string algorithim;
        [ObservableProperty]
        string traindatafilename;
        [ObservableProperty]
        string testdatafilename;

        [ObservableProperty]
        AI_DATACLASSES currentDataClass;
        [ObservableProperty]
        string aiCompFileDirName;
        [ObservableProperty]
        AI_ALGORITHIMS currentAlgorithim;
        [ObservableProperty]
        List<AI_ALGORITHIMSPARAMS> currentAlgorithimParams =new List<AI_ALGORITHIMSPARAMS>();
        [ObservableProperty]
        string[] features;
        [ObservableProperty]
        string[] selectedfeatures;
        [ObservableProperty]
        List<GenericLOVData> listofFeatures=new  List<GenericLOVData>();
        [ObservableProperty]
        string myAIlibraryfolder;
        private bool disposedValue;

        public IPythonMLManager PythonMLManager { get; set; }
        [ObservableProperty]
        double mseScore;
        [ObservableProperty]
        double rmseScore;
        [ObservableProperty]
        double maeScore;

      //  AIRankingViewModel RankingviewModel;
        public AITrainingViewModel(PythonNetRunTimeManager pythonRuntimeManager, PyModule persistentScope) : base(pythonRuntimeManager, persistentScope)
        {
           
          //  RankingviewModel = new AIRankingViewModel(dMEditor, dhubConfig, repo);
            
            aiCompFileDirName = "/AIComp";
        }
        public bool Split()
        {
            try
            {
             
                Traindatafilename = Path.Combine(CurrentAlgorithim.TRAINFILEPATH, CurrentAlgorithim.TRAINFILENAME);
                Features = PythonMLManager.SplitData(Traindatafilename, Splitratio, GetTrainingFileString(), GetTestFileString());
              
                IsDataReady = true;
                IsTrainingReady = true;
                return true;
            }
            catch (Exception ex)
            {
                IsTrainingReady = false;
                Editor.AddLogMessage("Beep", $"Error in Setup Training  - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }
        }
        public bool CleanData()
        {
            try
            {
                PythonMLManager.RemoveSpecialCharacters("train_data");
                PythonMLManager.RemoveSpecialCharacters("test_data");
                return true;
            }
            catch (Exception ex)
            {
                Editor.AddLogMessage("Beep", $"Error in Clean up  - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }
        }
        public bool SetupTraining(double dataid, double algid)
        {
            try
            {
                DataClassid = dataid;
                Algorithimid = algid;
                GetDataClass(DataClassid);
                GetAlgorithim(Algorithimid);
                Algorithim = CurrentAlgorithim.ALGORITHIM;
                Traindatafilename = Path.Combine(CurrentAlgorithim.TRAINFILEPATH,CurrentAlgorithim.TRAINFILENAME);
                Features=PythonMLManager.SplitData(Traindatafilename, Splitratio,GetTrainingFileString(), GetTestFileString());
                CleanData();
              //  GetTrainData();
              //   GetTestData();
                GetFeatures();
                IsDataReady = true;  
                IsTrainingReady = true;
                IsTrainDataLoaded = true;
                IsModelTrained = false;
                IsModelEvaluated = false;
                IsModelPredicted = false;
                IsSubmittionFileGenerated = false;
                return true;
            }
            catch (Exception ex)
            {
                IsTrainingReady = false;
                IsTrainDataLoaded = false;
                IsModelTrained = false;
                IsModelEvaluated = false;
                IsModelPredicted = false;
                IsSubmittionFileGenerated = false;
                Editor.AddLogMessage("Beep", $"Error in Setup Training  - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }
        }   
        public bool InitPythonModule()
        {
            try
            {
                PythonMLManager = new PythonMLManager((PythonNetRunTimeManager)Editor.GetPythonRunTimeManager());
                PythonMLManager.ImportPythonModule("numpy as np");
                PythonMLManager.ImportPythonModule("pandas as pd");
                IsInit = true;
                return true;
            }
            catch (Exception ex)
            {
                Editor.AddLogMessage("Beep", $"Error in Python Init - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                IsInit = false;
                return false;
            }
         
        }
        public AI_ALGORITHIMS GetAlgorithim(double id)
        {
            CurrentAlgorithim = Repo.LoadDataFirst<AI_ALGORITHIMS>($"select * from AI_ALGORITHIMS where id={id}", null).Result;
            CurrentAlgorithimParams= (List<AI_ALGORITHIMSPARAMS>)Repo.LoadData<AI_ALGORITHIMSPARAMS>($"select * from AI_ALGORITHIMSPARAMS where ALGORITHIM_ID={id}", null).Result;
            return CurrentAlgorithim;
        }   
        public AI_DATACLASSES GetDataClass(double id)
        {
            CurrentDataClass = Repo.LoadDataFirst<AI_DATACLASSES>($"select * from AI_DATACLASSES where id={id}", null).Result;
            return CurrentDataClass;
        }
        public bool Train()
        {
            try
            {
                if(IsTrainDataLoaded)
                {
                    string[] featurs = ["Pclass", "Sex", "SibSp", "Parch"];
                    PythonMLManager.TrainModel(CurrentAlgorithim.ALGORITHIM, MLAlgorithmsHelpers.GetAlgorithm(CurrentAlgorithim.ALGORITHIM), GetParameters(), Selectedfeatures, CurrentDataClass.LABELFIELD);
                    IsModelTrained = true;
                    IsModelEvaluated = false;
                    IsModelPredicted = false;
                    IsSubmittionFileGenerated = false;
                }
            
            }
            catch (Exception ex)
            {
                IsModelTrained = false;
                IsModelEvaluated = false;
                IsModelPredicted = false;
                IsSubmittionFileGenerated = false;
                Editor.AddLogMessage("Beep", $"Error in Python Train - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }

            return true;
        }
        public bool Eval()
        {
            try
            {
                if(IsModelTrained)
                {
                    if(CurrentAlgorithim.ALGORITHIM.Contains("Regress"))
                    {
                        Tuple<double, double, double> PredictScore = PythonMLManager.GetModelRegressionScores(CurrentAlgorithim.ALGORITHIM);
                        MseScore = Math.Round(PredictScore.Item1, 3);
                        RmseScore = Math.Round(PredictScore.Item2, 3);
                        MaeScore = Math.Round(PredictScore.Item3, 3);
                    }
                    else
                    {
                        Tuple<double, double> PredictScore = PythonMLManager.GetModelClassificationScore(CurrentAlgorithim.ALGORITHIM);
                        F1Accuracy = Math.Round(PredictScore.Item2, 3);
                        EvalScore = Math.Round(PredictScore.Item1, 3);
                    }
                   // Tuple<double, double> PredictScore = PythonMLManager.GetModelClassificationScore(CurrentAlgorithim.ALGORITHIM);
                   // F1Accuracy = Math.Round(PredictScore.Item2, 3);
                  //  EvalScore = Math.Round(PredictScore.Item1, 3);
                    IsModelEvaluated = true;
                    IsModelPredicted = false;
                    IsSubmittionFileGenerated = false;
                }
            
            }
            catch (Exception ex)
            {
                IsModelEvaluated = false;
                IsModelPredicted = false;
                IsSubmittionFileGenerated = false;
                Editor.AddLogMessage("Beep", $"Error in Python Prediction - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }

            return true;
        }
        public bool Predict()
        {
            try
            {
                if (!IsModelPredicted)
                {
                    PythonMLManager.LoadPredictionData(GetTestValidationFileString());
                    if (CurrentAlgorithim.ALGORITHIM.Contains("Regress"))
                    {
                        PythonMLManager.PredictRegression(Features);
                    }
                    else
                    {
                        PythonMLManager.PredictClassification(Features);
                    }
                       
                    IsModelPredicted = true;
                }

              
            }
            catch (Exception ex)
            {
                IsModelPredicted = false;
                Editor.AddLogMessage("Beep", $"Error in Python Submit Results - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }

            return true;
        }
        public bool GenerateSubmitionFile()
        {
            try
            {
                if (IsModelPredicted){
                    PythonMLManager.ExportTestResult(GetSubmissionFileString(), CurrentDataClass.PRIMARYFIELD, CurrentDataClass.LABELFIELD);
                    IsSubmittionFileGenerated = true;
                }
              
            }
            catch (Exception ex)
            {
                IsSubmittionFileGenerated = false;
                Editor.AddLogMessage("Beep", $"Error in Python Submit Results - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }

            return true;
        }
        public bool SubmitResult()
        {
            try
            {
                if (IsSubmittionFileGenerated)
                {
                    //RankingviewModel.Get(DhubConfig.userManager.User.KOCNO);
                    double margin = 0;
                    if (CurrentAlgorithim.ALGORITHIM.Contains("Regress"))
                    {
                        margin = 20;
                    }
                    else
                    {
                        margin = 0;
                    }
                    RankingviewModel.AddRanking(GetSubmissionFileString(), CurrentDataClass.ID,margin);
                    RankingviewModel.SaveRanking();
                    IsResultSubmitted = true;
                }

            }
            catch (Exception ex)
            {
                IsResultSubmitted = false;
                Editor.AddLogMessage("Beep", $"Error in Python Submit Results - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }

            return true;
        }
        public bool SubmitModel()
        {
            try
            {
                IsModelSavedToDB = true;
            }
            catch (Exception ex)
            {
                IsModelSavedToDB = false;
                Editor.AddLogMessage("Beep", $"Error in Python Submit Model - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }

            return true;
        }
        public bool GetTestData()
        {
            try
            {
                PythonMLManager.LoadTestData(GetTestFileString());
                IsTestDataLoaded = true;
            }
            catch (Exception ex)
            {
                IsTestDataLoaded = false;
                return false;
            }

            return true;
        }
        public bool GetFeatures()
        {
            try
            {
                ListofFeatures = new List<GenericLOVData>();
                foreach (var item in Features)
                {
                    if ((item != CurrentDataClass.LABELFIELD) && (item != CurrentDataClass.PRIMARYFIELD))
                    {
                        GenericLOVData x = new GenericLOVData() { ID = item, DisplayValue = item };
                        ListofFeatures.Add(x);
                    }

                }
                IsFeaturesReady = true;
            }
            catch (Exception ex)
            {
                IsFeaturesReady = false;
                return false;
            }

            return true;
        }
        public bool GetTrainData()
        {
            try
            {
                Features = PythonMLManager.LoadData(GetTrainingFileString());
                ListofFeatures = new List<GenericLOVData>();
                foreach (var item in Features)
                {
                    if ((item != CurrentDataClass.LABELFIELD) && (item != CurrentDataClass.PRIMARYFIELD))
                    {
                        GenericLOVData x = new GenericLOVData() { ID = item, DisplayValue = item };
                        ListofFeatures.Add(x);
                    }   
                    
                }
                IsTrainDataLoaded = true;
            }
            catch (Exception ex)
            {
                IsTrainDataLoaded = false;
                return false;
            }

            return true;
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
                retval = System.IO.Path.Combine(MyAIlibraryfolder, CurrentAlgorithim.ALGORITHIM);
            }
            return retval;
        }
        public string GetAlgorithimName(string algorithim)
        {
            return Enum.GetName(typeof(MachineLearningAlgorithm), algorithim);
        }
        public string GetTrainingFileString()
        {
            return Path.Combine(CurrentAlgorithim.TRAINFILEPATH, "train_data.csv");
        }
        public string GetTestFileString()
        {
            return Path.Combine(CurrentAlgorithim.TRAINFILEPATH, "test_data.csv");
        }
        public string GetSubmissionFileString()
        {
            return Path.Combine(CurrentAlgorithim.TRAINFILEPATH, "submission.csv");
        }
        public string GetValidationFileString()
        {
            return Path.Combine(CurrentDataClass.URLPATH, CurrentDataClass.TESTDATAFILENAME);
        }
        public string GetTestValidationFileString()
        {
            return Path.Combine(CurrentDataClass.URLPATH, CurrentDataClass.TESTDATAFILENAME);
        }
        public string GetModelFileString()
        {
            return Path.Combine(GetPath(), "model.pkl");
        }
        public string GetModelFileString(string modelname)
        {
            return Path.Combine(GetPath(), $"{modelname}.pkl");
        }
        public Dictionary<string, object> GetParameters()
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            foreach (var item in CurrentAlgorithimParams)
            {
                if (!string.IsNullOrEmpty(item.PARAMETERVALUE))
                {
                    parameters.Add(item.PARAMETERNAME, item.PARAMETERVALUE);
                }

            }
            return parameters;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    PythonMLManager.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AITrainingViewModel()
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
