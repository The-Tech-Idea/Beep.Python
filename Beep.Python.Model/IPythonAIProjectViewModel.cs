using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using TheTechIdea.Beep.Editor.UOW;

namespace Beep.Python.Model
{
    public interface IPythonAIProjectViewModel:IDisposable
    {
        string CurrentProjectFolder { get; }
        double EvalScore { get; }
        double F1Accuracy { get; }
        bool IsDataReady { get; }
        bool IsInit { get; }
        bool IsModelEvaluated { get; }
        bool IsModelPredicted { get; }
        bool IsModelTrained { get; }
        bool IsTrainDataLoaded { get; }
        bool IsTrainingReady { get; }
        double MaeScore { get; }
        double MseScore { get; }
        ObservableBindingList<PythonProject> Projects { get; }
      
        double RmseScore { get; }
        UnitofWork<PythonProject> UnitofWork { get; set; }

        bool CleanData();
        void CreateParameters();
        bool CreateProject(string projectname);
        bool Eval();
        void Get(string currentEntity);
        bool GetFeatures();
        Dictionary<string, object> GetParameters();
        string GetTestFileString();
        string GetTrainingFileString();
        void initialize();
        bool InitPythonMLModule();
        void LoadProjects();
        bool Predict();
        void SaveProject();
        bool SetupTraining();
        bool Train();
    }
}