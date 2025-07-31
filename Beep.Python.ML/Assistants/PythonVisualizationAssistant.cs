using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for visualization operations using Python scripts
    /// </summary>
    public class PythonVisualizationAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonVisualizationAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public bool CreateROC()
        {
            string script = PythonScriptTemplateManager.GetScript("create_roc", null);
            return ExecuteInSession(script);
        }

        public bool CreateConfusionMatrix()
        {
            string script = PythonScriptTemplateManager.GetScript("create_confusion_matrix", null);
            return ExecuteInSession(script);
        }

        public bool CreateLearningCurve(string modelId, string imagePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["image_path"] = imagePath
            };

            string script = PythonScriptTemplateManager.GetScript("create_learning_curve", parameters);
            return ExecuteInSession(script);
        }

        public bool CreatePrecisionRecallCurve(string modelId, string imagePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["image_path"] = imagePath
            };

            string script = PythonScriptTemplateManager.GetScript("create_precision_recall_curve", parameters);
            return ExecuteInSession(script);
        }

        public bool CreateFeatureImportance(string modelId, string imagePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["image_path"] = imagePath
            };

            string script = PythonScriptTemplateManager.GetScript("create_feature_importance", parameters);
            return ExecuteInSession(script);
        }

        public bool CreateConfusionMatrix(string modelId, string imagePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["image_path"] = imagePath
            };

            string script = PythonScriptTemplateManager.GetScript("create_confusion_matrix_with_model", parameters);
            return ExecuteInSession(script);
        }

        public bool CreateROC(string modelId, string imagePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["image_path"] = imagePath
            };

            string script = PythonScriptTemplateManager.GetScript("create_roc_with_model", parameters);
            return ExecuteInSession(script);
        }

        public bool GenerateEvaluationReport(string modelId, string outputHtmlPath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["output_html_path"] = outputHtmlPath
            };

            string script = PythonScriptTemplateManager.GetScript("generate_evaluation_report", parameters);
            return ExecuteInSession(script);
        }
    }
}