

namespace Beep.Python.Model
{
    public interface IPythonMLManager
    {
        bool IsInitialized { get; }
        Tuple<double,double> GetModelScore(string modelId);
        double GetScoreUsingExistingTestData(string modelId, string metric);
        string[] LoadData(string filePath);
        void LoadData(string filePath, string[] featureColumns, string labelColumn);
        string LoadModel(string filePath);
        dynamic Predict();
        void SaveModel(string modelId, string filePath);
        string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath);
        void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn);
        void TrainModelWithUpdatedData(string modelId, string updatedTrainDataPath, string[] featureColumns, string labelColumn, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters);
        void ExportTestResult(string filePath, string iDColumn, string labelColumn);
        void Dispose();
        void ImportPythonModule(string moduleName);
        string[] LoadTestData(string filePath);
        void AddLabelColumnIfMissing(string testDataFilePath, string labelColumn);
        void AddLabelColumnIfMissing(string labelColumn);
        Tuple<string, string> SplitDataClassFile(string urlpath, string filename, double splitRatio);
    }
}