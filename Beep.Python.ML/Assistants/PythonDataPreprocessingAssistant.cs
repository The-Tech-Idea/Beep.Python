using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for data preprocessing operations using Python scripts
    /// </summary>
    public class PythonDataPreprocessingAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonDataPreprocessingAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void HandleCategoricalDataEncoder(string[] categoricalFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["categorical_features"] = categoricalFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("handle_categorical_encoder", parameters);
            ExecuteInSession(script);
        }

        public void HandleMultiValueCategoricalFeatures(string[] multiValueFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["multi_value_features"] = multiValueFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("handle_multi_value_categorical", parameters);
            ExecuteInSession(script);
        }

        public void HandleDateData(string[] dateFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["date_features"] = dateFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("handle_date_data", parameters);
            ExecuteInSession(script);
        }

        public void ImputeMissingValues(string strategy = "mean")
        {
            var parameters = new Dictionary<string, object>
            {
                ["strategy"] = strategy
            };

            string script = PythonScriptTemplateManager.GetScript("impute_missing_values", parameters);
            ExecuteInSession(script);
        }

        public void ImputeMissingValuesWithFill(string method = "ffill")
        {
            var parameters = new Dictionary<string, object>
            {
                ["method"] = method
            };

            string script = PythonScriptTemplateManager.GetScript("impute_missing_values_fill", parameters);
            ExecuteInSession(script);
        }

        public void ImputeMissingValuesWithCustomValue(object customValue)
        {
            var parameters = new Dictionary<string, object>
            {
                ["custom_value"] = customValue
            };

            string script = PythonScriptTemplateManager.GetScript("impute_missing_values_custom", parameters);
            ExecuteInSession(script);
        }

        public void DropMissingValues(string axis = "rows")
        {
            var parameters = new Dictionary<string, object>
            {
                ["axis"] = axis
            };

            string script = PythonScriptTemplateManager.GetScript("drop_missing_values", parameters);
            ExecuteInSession(script);
        }

        public void ImputeMissingValues(string[] featureList = null, string strategy = "mean")
        {
            var parameters = new Dictionary<string, object>
            {
                ["feature_list"] = featureList ?? Array.Empty<string>(),
                ["strategy"] = strategy
            };

            string script = PythonScriptTemplateManager.GetScript("impute_missing_values_with_features", parameters);
            ExecuteInSession(script);
        }

        public void StandardizeData()
        {
            string script = PythonScriptTemplateManager.GetScript("standardize_data", null);
            ExecuteInSession(script);
        }

        public void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0)
        {
            var parameters = new Dictionary<string, object>
            {
                ["feature_range_min"] = featureRangeMin,
                ["feature_range_max"] = featureRangeMax
            };

            string script = PythonScriptTemplateManager.GetScript("minmax_scale_data", parameters);
            ExecuteInSession(script);
        }

        public void RobustScaleData()
        {
            string script = PythonScriptTemplateManager.GetScript("robust_scale_data", null);
            ExecuteInSession(script);
        }

        public void NormalizeData(string norm = "l2")
        {
            var parameters = new Dictionary<string, object>
            {
                ["norm"] = norm
            };

            string script = PythonScriptTemplateManager.GetScript("normalize_data", parameters);
            ExecuteInSession(script);
        }

        public void StandardizeData(string[] selectedFeatures = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("standardize_data_with_features", parameters);
            ExecuteInSession(script);
        }

        public void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0, string[] selectedFeatures = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["feature_range_min"] = featureRangeMin,
                ["feature_range_max"] = featureRangeMax,
                ["selected_features"] = selectedFeatures ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("minmax_scale_data_with_features", parameters);
            ExecuteInSession(script);
        }

        public void RobustScaleData(string[] selectedFeatures = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("robust_scale_data_with_features", parameters);
            ExecuteInSession(script);
        }

        public void NormalizeData(string norm = "l2", string[] selectedFeatures = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["norm"] = norm,
                ["selected_features"] = selectedFeatures ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("normalize_data_with_features", parameters);
            ExecuteInSession(script);
        }

        public bool RemoveSpecialCharacters(string dataFrameName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["dataframe_name"] = dataFrameName
            };

            string script = PythonScriptTemplateManager.GetScript("remove_special_characters", parameters);
            return ExecuteInSession(script);
        }
    }
}