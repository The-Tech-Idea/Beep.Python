using Beep.Python.Model;
using DataManagementModels.Editor;
using TheTechIdea.Beep.Editor;

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
        IPythonMLManager PythonMLManager { get; set; }
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