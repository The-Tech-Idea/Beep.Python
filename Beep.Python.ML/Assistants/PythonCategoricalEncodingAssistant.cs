using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for categorical encoding operations using Python scripts
    /// </summary>
    public class PythonCategoricalEncodingAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonCategoricalEncodingAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        private string[] GetStringArrayFromSession(string variableName)
        {
            try
            {
                var script = $@"
import json
if '{variableName}' in globals():
    result_json = json.dumps({variableName})
else:
    result_json = '[]'
";
                ExecuteInSession(script);
                
                var jsonResult = _pythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(null, "result_json", _sessionInfo);
                
                if (!string.IsNullOrEmpty(jsonResult?.ToString()))
                {
                    var cleanJson = jsonResult.ToString().Trim('"').Replace("\\\"", "\"");
                    var result = System.Text.Json.JsonSerializer.Deserialize<string[]>(cleanJson);
                    return result ?? Array.Empty<string>();
                }
            }
            catch (Exception)
            {
                // Return empty array on any error
            }

            return Array.Empty<string>();
        }

        public void OneHotEncode(string[] categoricalFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["categorical_features"] = categoricalFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("one_hot_encode", parameters);
            ExecuteInSession(script);
        }

        public void LabelEncode(string[] categoricalFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["categorical_features"] = categoricalFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("label_encode", parameters);
            ExecuteInSession(script);
        }

        public void TargetEncode(string[] categoricalFeatures, string labelColumn)
        {
            var parameters = new Dictionary<string, object>
            {
                ["categorical_features"] = categoricalFeatures,
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("target_encode", parameters);
            ExecuteInSession(script);
        }

        public void BinaryEncode(string[] categoricalFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["categorical_features"] = categoricalFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("binary_encode", parameters);
            ExecuteInSession(script);
        }

        public void FrequencyEncode(string[] categoricalFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["categorical_features"] = categoricalFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("frequency_encode", parameters);
            ExecuteInSession(script);
        }

        public string[] GetCategoricalFeatures(string[] selectedFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("get_categorical_features", parameters);
            ExecuteInSession(script);

            return GetStringArrayFromSession("categorical_features");
        }

        public Tuple<string[], string[]> GetCategoricalAndDateFeatures(string[] selectedFeatures)
        {
            var parameters = new Dictionary<string, object>
            {
                ["selected_features"] = selectedFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("get_categorical_and_date_features", parameters);
            ExecuteInSession(script);

            var categoricalFeatures = GetStringArrayFromSession("categorical_features");
            var dateFeatures = GetStringArrayFromSession("date_features");
            
            return new Tuple<string[], string[]>(categoricalFeatures, dateFeatures);
        }
    }
}