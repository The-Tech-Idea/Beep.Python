﻿

namespace Beep.Python.Model
{
    public interface IPythonMLManager
    {
        bool IsInitialized { get; }
        double GetModelScore(string modelId, ModelMetric metric);
        double GetScoreUsingExistingTestData(string modelId, string metric);
        string[] LoadData(string filePath);
        void LoadData(string filePath, string[] featureColumns, string labelColumn);
        string LoadModel(string filePath);
        dynamic Predict();
        void SaveModel(string modelId, string filePath);
        void SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath);
        void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn);
        void TrainModelWithUpdatedData(string modelId, string updatedTrainDataPath, string[] featureColumns, string labelColumn, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters);
        void ExportTestResult(string filePath, string iDColumn, string labelColumn);
        void Dispose();
        void ImportPythonModule(string moduleName);
        void LoadTestData(string filePath);
    }
}