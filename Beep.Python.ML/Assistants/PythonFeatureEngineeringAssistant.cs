using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for feature engineering operations using Python scripts
    /// </summary>
    public class PythonFeatureEngineeringAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonFeatureEngineeringAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void GeneratePolynomialFeatures(string[] selectedFeatures = null, int degree = 2, bool includeBias = true, bool interactionOnly = false)
        {
            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures ?? Array.Empty<string>(),
                ["degree"] = degree,
                ["include_bias"] = includeBias,
                ["interaction_only"] = interactionOnly
            };

            string script = PythonScriptTemplateManager.GetScript("generate_polynomial_features", parameters);
            ExecuteInSession(script);
        }

        public void ApplyLogTransformation(string[] selectedFeatures = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("apply_log_transformation", parameters);
            ExecuteInSession(script);
        }

        public void ApplyBinning(string[] selectedFeatures, int numberOfBins = 5, bool encodeAsOrdinal = true)
        {
            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures,
                ["number_of_bins"] = numberOfBins,
                ["encode_as_ordinal"] = encodeAsOrdinal
            };

            string script = PythonScriptTemplateManager.GetScript("apply_binning", parameters);
            ExecuteInSession(script);
        }

        public void ApplyFeatureHashing(string[] selectedFeatures, int nFeatures = 10)
        {
            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures,
                ["n_features"] = nFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("apply_feature_hashing", parameters);
            ExecuteInSession(script);
        }
    }
}