

namespace Beep.Python.Model
{
     public  interface IPythonMLManager:IDisposable
    {
         bool IsDataLoaded {get;set;}
          bool IsModelTrained {get;set;}
          bool IsModelSaved {get;set;}
          bool IsModelLoaded {get;set;}
          bool IsModelPredicted {get;set;}
          bool IsModelScored {get;set;}
          bool IsModelExported {get;set;}
          bool IsDataSplit {get;set;}
          string DataFilePath { get; set; } 
          string ModelFilePath { get; set; } 
          string PredictionsFilePath { get; set; } 
          string TrainingFilePath { get; set; } 
          string TestingFilePath { get; set; } 
          string ValidationFilePath { get; set; } 
        bool IsInitialized { get; }
        bool RemoveSpecialCharacters(string dataFrameName);
        string[] GetFeatures(string filePath);
        Tuple<double,double> GetModelClassificationScore(string modelId);
        Tuple<double, double, double> GetModelRegressionScores(string modelId);
        string[] LoadData(string filePath);
        string LoadModel(string filePath);
        string[] LoadPredictionData(string filePath);
        dynamic PredictClassification(string[] training_columns);
        dynamic PredictRegression(string[] training_columns);
        void SaveModel(string modelId, string filePath);
        string[] SplitData( float testSize, string trainFilePath, string testFilePath);
        string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath, string validationFilePath, string primaryFeatureKeyID, string labelColumn);
        string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath);
        string[] SplitData(string dataFilePath, float testSize, float validationSize, string trainFilePath, string testFilePath, string validationFilePath);
        void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn);
        void ExportTestResult(string filePath, string iDColumn, string labelColumn);
        void Dispose();
        void ImportPythonModule(string moduleName);
        string[] LoadTestData(string filePath);
        void AddLabelColumnIfMissing(string testDataFilePath, string labelColumn);
        void AddLabelColumnIfMissing(string labelColumn);
        Tuple<string, string> SplitDataClassFile(string urlpath, string filename, double splitRatio);
    }
};