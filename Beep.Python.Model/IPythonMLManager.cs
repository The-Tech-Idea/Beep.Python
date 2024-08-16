

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
        string[] ValidateAndPreviewData(string filePath, int numRows );
        bool RemoveSpecialCharacters(string dataFrameName);
        string[] GetFeatures(string filePath);
        Tuple<double,double> GetModelClassificationScore(string modelId);
        Tuple<double, double, double> GetModelRegressionScores(string modelId);
        string[] LoadData(string filePath);
        string[] LoadData(string filePath, string[] selectedFeatures);
        void FilterDataToSelectedFeatures(string[] selectedFeatures);
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
        void HandleCategoricalDataEncoder(string[] categoricalFeatures);
        void HandleMultiValueCategoricalFeatures(string[] multiValueFeatures);
        void HandleDateData(string[] dateFeatures);
        void ImputeMissingValues(string strategy = "mean");
        void ImputeMissingValuesWithFill(string method = "ffill");
        void ImputeMissingValuesWithCustomValue(object customValue);
        void DropMissingValues(string axis = "rows");
        void StandardizeData();
        void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0);
        void RobustScaleData();
        void NormalizeData(string norm = "l2");
        void StandardizeData(string[] selectedFeatures = null);
        void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0, string[] selectedFeatures = null);
        void RobustScaleData(string[] selectedFeatures = null);
        void NormalizeData(string norm = "l2", string[] selectedFeatures = null);
        void GeneratePolynomialFeatures(string[] selectedFeatures = null, int degree = 2, bool includeBias = true, bool interactionOnly = false);
        void ApplyLogTransformation(string[] selectedFeatures = null);
        void ApplyBinning(string[] selectedFeatures, int numberOfBins = 5, bool encodeAsOrdinal = true);
        void ApplyFeatureHashing(string[] selectedFeatures, int nFeatures = 10);
        void ApplyRandomUndersampling(string targetColumn, float samplingStrategy = 0.5f);
        void ApplyRandomOversampling(string targetColumn, float samplingStrategy = 1.0f);
        void ApplySMOTE(string targetColumn, float samplingStrategy = 1.0f);
        void ApplyNearMiss(string targetColumn, int version = 1);
        void ApplyBalancedRandomForest(string targetColumn, int nEstimators = 100);
        void AdjustClassWeights(string modelId, string algorithmName, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn);
        void ConvertTextToLowercase(string columnName);
        void RemovePunctuation(string columnName);
        void RemoveStopwords(string columnName, string language = "english");
        void ApplyStemming(string columnName);
        void ApplyLemmatization(string columnName);
        void ApplyTokenization(string columnName);
        void ExtractDateTimeComponents(string columnName);
        void CalculateTimeDifference(string startColumn, string endColumn, string newColumnName);
        void HandleCyclicalTimeFeatures(string columnName, string featureType);
        void ParseDateColumn(string columnName);
        void HandleMissingDates(string columnName, string method = "fill", string fillValue = null);
        void OneHotEncode(string[] categoricalFeatures);
        void LabelEncode(string[] categoricalFeatures);
        void TargetEncode(string[] categoricalFeatures, string labelColumn);
        void BinaryEncode(string[] categoricalFeatures);
        void FrequencyEncode(string[] categoricalFeatures);
        void TimeSeriesAugmentation(string[] timeSeriesColumns, string augmentationType, double parameter);
        void ApplyVarianceThreshold(double threshold = 0.0);
        void ApplyCorrelationThreshold(double threshold = 0.9);
        void ApplyRFE(string modelId, int n_features_to_select = 5);
        void ApplyL1Regularization(double alpha = 0.01);
        void ApplyTreeBasedFeatureSelection(string modelId);
        void PerformCrossValidation(string modelId, int numFolds = 5);
        void PerformStratifiedSampling(float testSize, string trainFilePath, string testFilePath);
        void ImputeMissingValues(string[] featureList = null, string strategy = "mean");
        void RemoveOutliers(string[] featureList = null, double zThreshold = 3.0);
        void DropDuplicates(string[] featureList = null);
        void StandardizeCategories(string[] featureList = null, Dictionary<string, string> replacements = null);
        void ApplyPCA(int nComponents = 2, string[] featureList = null);
        void ApplyLDA(string labelColumn, int nComponents = 2, string[] featureList = null);
        void ApplyVarianceThreshold(double threshold = 0.0, string[] featureList = null);
        void RandomOverSample(string labelColumn);
        void RandomUnderSample(string labelColumn);
        void ApplySMOTE(string labelColumn);
        string[] GetCategoricalFeatures(string[] selectedFeatures);
        Tuple<string[], string[]> GetCategoricalAndDateFeatures(string[] selectedFeatures);
        void Dispose();
        void ImportPythonModule(string moduleName);
        string[] LoadTestData(string filePath);
        void AddLabelColumnIfMissing(string testDataFilePath, string labelColumn);
        void AddLabelColumnIfMissing(string labelColumn);
        Tuple<string, string> SplitDataClassFile(string urlpath, string filename, double splitRatio);
        bool CreateROC();
        bool CreateConfusionMatrix();
        bool CreateLearningCurve(string modelId, string imagePath);
        bool CreatePrecisionRecallCurve(string modelId, string imagePath);
        bool CreateFeatureImportance(string modelId, string imagePath);
        bool CreateConfusionMatrix(string modelId, string imagePath);
        bool CreateROC(string modelId, string imagePath);
        bool GenerateEvaluationReport(string modelId, string outputHtmlPath);
    }
};