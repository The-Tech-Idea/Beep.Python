using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for feature selection operations using Python scripts
    /// </summary>
    public class PythonFeatureSelectionAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonFeatureSelectionAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void ApplyVarianceThreshold(double threshold = 0.0)
        {
            var parameters = new Dictionary<string, object>
            {
                ["threshold"] = threshold
            };

            string script = PythonScriptTemplateManager.GetScript("apply_variance_threshold", parameters);
            ExecuteInSession(script);
        }

        public void ApplyCorrelationThreshold(double threshold = 0.9)
        {
            var parameters = new Dictionary<string, object>
            {
                ["threshold"] = threshold
            };

            string script = PythonScriptTemplateManager.GetScript("apply_correlation_threshold", parameters);
            ExecuteInSession(script);
        }

        public void ApplyRFE(string modelId, int n_features_to_select = 5)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["n_features_to_select"] = n_features_to_select
            };

            string script = PythonScriptTemplateManager.GetScript("apply_rfe", parameters);
            ExecuteInSession(script);
        }

        public void ApplyL1Regularization(double alpha = 0.01)
        {
            var parameters = new Dictionary<string, object>
            {
                ["alpha"] = alpha
            };

            string script = PythonScriptTemplateManager.GetScript("apply_l1_regularization", parameters);
            ExecuteInSession(script);
        }

        public void ApplyTreeBasedFeatureSelection(string modelId)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId
            };

            string script = PythonScriptTemplateManager.GetScript("apply_tree_based_feature_selection", parameters);
            ExecuteInSession(script);
        }

        public void ApplyVarianceThreshold(double threshold = 0.0, string[] featureList = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["threshold"] = threshold,
                ["feature_list"] = featureList ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("apply_variance_threshold_with_features", parameters);
            ExecuteInSession(script);
        }
    }
}