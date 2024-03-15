using Beep.Python.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using DataManagementModels.Editor;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine.ViewModels
{
    public partial class PythonAIProjectViewModel:PythonBaseViewModel
    {
        public bool IsDataReady { get; private set; }
        public bool IsTrainingReady { get; private set; }
        public bool IsTrainDataLoaded { get; private set; }
        public bool IsModelTrained { get; private set; }
        public bool IsModelEvaluated { get; private set; }
        public bool IsModelPredicted { get; private set; }
        public double MseScore { get; private set; }
        public double RmseScore { get; private set; }
        public double MaeScore { get; private set; }
        public double F1Accuracy { get; private set; }
        public double EvalScore { get; private set; }
        public bool IsInit { get; private set; }
        public string CurrentProjectFolder
        {
            get
            {
                string retval = "";
                if (CurrentProject != null)
                {
                    if (CurrentProject.ProjectName != null)
                    {
                        return Path.Combine(PythonDatafolder, CurrentProject.ProjectName);
                    }
                }
                return retval;

            }
        }
        [ObservableProperty]
        PythonProject currentProject;
        public ObservableBindingList<PythonProject> Projects => UnitofWork.Units;
        [ObservableProperty]
        List<LOVData>  listofAlgorithims;
        [ObservableProperty]    
        List<string> algorithims;
        [ObservableProperty]
        List<ParameterDictionaryForAlgorithm> parameterDictionaryForAlgorithms;
        public UnitofWork<PythonProject> UnitofWork { get; set; }
        public IPythonMLManager PythonMLManager { get; set; }
    
        public PythonAIProjectViewModel() : base()
        {
            initialize();
        }
        public PythonAIProjectViewModel(IPythonRunTimeManager pythonRuntimeManager, PyModule persistentScope) : base(pythonRuntimeManager, persistentScope)
        {
            initialize();
        }
        public PythonAIProjectViewModel(IPythonRunTimeManager pythonRuntimeManager) : base(pythonRuntimeManager)
        {
            InitializePythonEnvironment();
            initialize();
        }
        public bool InitPythonMLModule()
        {
            try
            {
                IPythonRunTimeManager p =Editor.GetPythonRunTimeManager();
                PythonMLManager = new PythonMLManager(p,p.PersistentScope);
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
        public void initialize()
        {
            InitPythonMLModule();
            UnitofWork = new UnitofWork<PythonProject>(Editor,true,new ObservableBindingList<PythonProject>() { }, "ProjectGuidValue");
            UnitofWork.PrimaryKey= "ProjectGuidValue";
            ListofAlgorithims = new List<LOVData>();

            LoadProjects();
            foreach (var item in Enum.GetNames(typeof(MachineLearningAlgorithm)))
            {
                LOVData data = new LOVData() { ID = item, DisplayValue = item, LOVDESCRIPTION = MLAlgorithmsHelpers.GenerateAlgorithmDescription((MachineLearningAlgorithm)Enum.Parse(typeof(MachineLearningAlgorithm), item)) };
                ListofAlgorithims.Add(data);
            }
            Algorithims = MLAlgorithmsHelpers.GetAlgorithms();
            ParameterDictionaryForAlgorithms = MLAlgorithmsHelpers.GetParameterDictionaryForAlgorithms();
        }
        public void LoadProjects()
        {
            List<PythonProject> ls = Editor.ConfigEditor.JsonLoader.DeserializeObject<PythonProject>(Path.Combine(PythonDatafolder, "Projects.json"));
            if(ls != null)
            {
                UnitofWork.Units = new ObservableBindingList<PythonProject>(ls);
            }
            else
                UnitofWork.Units = new ObservableBindingList<PythonProject>();
        }
        public void SaveProject()
        {
            Editor.ConfigEditor.JsonLoader.Serialize(Path.Combine(PythonDatafolder, "Projects.json"), UnitofWork.Units);
        }
        public bool CreateProject(string projectname)
        {
            try
            {
                PythonProject pythonProject = new PythonProject();
                pythonProject.ProjectName = projectname;
                pythonProject.ProjectGuidValue = Guid.NewGuid().ToString();
                UnitofWork.Units.Add(pythonProject);
                CurrentProject = UnitofWork.Units[UnitofWork.Getindex(pythonProject)];
                CreateProjectFolder(projectname);

            }
            catch (Exception ex)
            {
                Editor.AddLogMessage("Beep", $"Error in Create Project - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }
         return true;
        }
        public bool SetupTraining()
        {
            try
            {
                CurrentProject.FeaturesArray = PythonMLManager.SplitData(CurrentProject.DataFile, CurrentProject.Splitratio, GetTrainingFileString(), GetTestFileString());
                CleanData();
                GetFeatures();
                IsDataReady = true;
                IsTrainingReady = true;
                IsTrainDataLoaded = true;
                IsModelTrained = false;
                IsModelEvaluated = false;
                IsModelPredicted = false;
               
                return true;
            }
            catch (Exception ex)
            {
                IsTrainingReady = false;
                IsTrainDataLoaded = false;
                IsModelTrained = false;
                IsModelEvaluated = false;
                IsModelPredicted = false;
             
                Editor.AddLogMessage("Beep", $"Error in Setup Training  - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }
        }
        public bool Train()
        {
            try
            {
                if (IsTrainDataLoaded)
                {
                    PythonMLManager.TrainModel(CurrentProject.Algorithm, MLAlgorithmsHelpers.GetAlgorithm(CurrentProject.Algorithm), GetParameters(), CurrentProject.FeaturesArray, CurrentProject.Label);
                    IsModelTrained = true;
                    IsModelEvaluated = false;
                    IsModelPredicted = false;
                   
                }

            }
            catch (Exception ex)
            {
                IsModelTrained = false;
                IsModelEvaluated = false;
                IsModelPredicted = false;
               
                Editor.AddLogMessage("Beep", $"Error in Python Train - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }

            return true;
        }
        public bool Eval()
        {
            try
            {
                if (IsModelTrained)
                {
                    if (CurrentProject.Algorithm.Contains("Regress"))
                    {
                        Tuple<double, double, double> PredictScore = PythonMLManager.GetModelRegressionScores(CurrentProject.Algorithm);
                        MseScore = Math.Round(PredictScore.Item1, 3);
                        RmseScore = Math.Round(PredictScore.Item2, 3);
                        MaeScore = Math.Round(PredictScore.Item3, 3);
                    }
                    else
                    {
                        Tuple<double, double> PredictScore = PythonMLManager.GetModelClassificationScore(CurrentProject.Algorithm);
                        F1Accuracy = Math.Round(PredictScore.Item2, 3);
                        EvalScore = Math.Round(PredictScore.Item1, 3);
                    }
                    // Tuple<double, double> PredictScore = PythonMLManager.GetModelClassificationScore(CurrentAlgorithim.ALGORITHIM);
                    // F1Accuracy = Math.Round(PredictScore.Item2, 3);
                    //  EvalScore = Math.Round(PredictScore.Item1, 3);
                    IsModelEvaluated = true;
                    IsModelPredicted = false;
                  
                }

            }
            catch (Exception ex)
            {
                IsModelEvaluated = false;
                IsModelPredicted = false;
             
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
                    PythonMLManager.LoadPredictionData(GetTestFileString());
                    if (CurrentProject.Algorithm.Contains("Regress"))
                    {
                        PythonMLManager.PredictRegression(CurrentProject.FeaturesArray);
                    }
                    else
                    {
                        PythonMLManager.PredictClassification(CurrentProject.FeaturesArray);
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
        #region "Utility Functions"    
        public string GetTrainingFileString()
        {
            return Path.Combine(CurrentProjectFolder, "train_data.csv");
        }
        public string GetTestFileString()
        {
            return Path.Combine(CurrentProjectFolder, "test_data.csv");
        }
        public Dictionary<string, object> GetParameters()
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            foreach (var item in CurrentProject.PythonAlgorithmParams)
            {
                if (!string.IsNullOrEmpty(item.PARAMETERVALUE))
                {
                    parameters.Add(item.PARAMETERNAME, item.PARAMETERVALUE);
                }

            }
            return parameters;
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
        private bool CreateProjectFolder(string projectname)
        {
            try
            {
                string projectfolder = Path.Combine(PythonDatafolder, projectname);
                if (!Directory.Exists(projectfolder))
                {
                    Directory.CreateDirectory(projectfolder);
                }

            }
            catch (Exception ex)
            {
                Editor.AddLogMessage("Beep", $"Error in Create Project - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }
            return true;
        }
        public void CreateParameters()
        {
            foreach (var item in ParameterDictionaryForAlgorithms.Where(p => p.Algorithm.ToString() == CurrentProject.Algorithm))
            {
                if (!CurrentProject.PythonAlgorithmParams.Any(p => p.PARAMETERNAME == item.ParameterName))
                {
                    PythonalgorithmParams doc = new PythonalgorithmParams();
                    doc.ALGORITHIM = CurrentProject.Algorithm;
                    doc.PARAMETERNAME = item.ParameterName;

                    doc.PARAMETERDESCRIPTION = item.Description + $" - example : ({item.Example})";
                    doc.ROW_CREATE_DATE = DateTime.Now;
                    CurrentProject.PythonAlgorithmParams.Add(doc);
                }

            }
        }
        public bool GetFeatures()
        {
            try
            {
                CurrentProject.Features = new List<LOVData>();
                foreach (var item in CurrentProject.FeaturesArray)
                {
                    if ((item != CurrentProject.Label) && (item != CurrentProject.Key))
                    {
                        LOVData x = new LOVData() { ID = item, DisplayValue = item };
                        CurrentProject.Features.Add(x);
                    }

                }
                // IsFeaturesReady = true;
            }
            catch (Exception ex)
            {
                //   IsFeaturesReady = false;
                return false;
            }

            return true;
        }
        #endregion

    }
}
