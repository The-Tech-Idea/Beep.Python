namespace Beep.Python.Model
{
    public interface IPythonModelEvaluationGraphsViewModel:IDisposable
    {
        void GenerateConfusionMatrix(string savePath);
        void GenerateFeatureImportance(string savePath);
        void GenerateLearningCurve(string savePath);
        void GeneratePrecisionRecallCurve(string savePath);
        void GenerateROCCurve(string savePath);
    }
}