using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for imbalanced data handling operations using Python scripts
    /// </summary>
    public class PythonImbalancedDataAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonImbalancedDataAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void ApplyRandomUndersampling(string targetColumn, float samplingStrategy = 0.5f)
        {
            var parameters = new Dictionary<string, object>
            {
                ["target_column"] = targetColumn,
                ["sampling_strategy"] = samplingStrategy
            };

            string script = PythonScriptTemplateManager.GetScript("apply_random_undersampling", parameters);
            ExecuteInSession(script);
        }

        public void ApplyRandomOversampling(string targetColumn, float samplingStrategy = 1.0f)
        {
            var parameters = new Dictionary<string, object>
            {
                ["target_column"] = targetColumn,
                ["sampling_strategy"] = samplingStrategy
            };

            string script = PythonScriptTemplateManager.GetScript("apply_random_oversampling", parameters);
            ExecuteInSession(script);
        }

        public void ApplySMOTE(string targetColumn, float samplingStrategy = 1.0f)
        {
            var parameters = new Dictionary<string, object>
            {
                ["target_column"] = targetColumn,
                ["sampling_strategy"] = samplingStrategy
            };

            string script = PythonScriptTemplateManager.GetScript("apply_smote", parameters);
            ExecuteInSession(script);
        }

        public void ApplyNearMiss(string targetColumn, int version = 1)
        {
            var parameters = new Dictionary<string, object>
            {
                ["target_column"] = targetColumn,
                ["version"] = version
            };

            string script = PythonScriptTemplateManager.GetScript("apply_near_miss", parameters);
            ExecuteInSession(script);
        }

        public void ApplyBalancedRandomForest(string targetColumn, int nEstimators = 100)
        {
            var parameters = new Dictionary<string, object>
            {
                ["target_column"] = targetColumn,
                ["n_estimators"] = nEstimators
            };

            string script = PythonScriptTemplateManager.GetScript("apply_balanced_random_forest", parameters);
            ExecuteInSession(script);
        }

        public void AdjustClassWeights(string modelId, string algorithmName, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn)
        {
            var scriptParameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["algorithm_name"] = algorithmName,
                ["parameters"] = parameters,
                ["feature_columns"] = featureColumns,
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("adjust_class_weights", scriptParameters);
            ExecuteInSession(script);
        }

        public void RandomOverSample(string labelColumn)
        {
            var parameters = new Dictionary<string, object>
            {
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("random_over_sample", parameters);
            ExecuteInSession(script);
        }

        public void RandomUnderSample(string labelColumn)
        {
            var parameters = new Dictionary<string, object>
            {
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("random_under_sample", parameters);
            ExecuteInSession(script);
        }

        public void ApplySMOTE(string labelColumn)
        {
            var parameters = new Dictionary<string, object>
            {
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("apply_smote_simple", parameters);
            ExecuteInSession(script);
        }
    }
}