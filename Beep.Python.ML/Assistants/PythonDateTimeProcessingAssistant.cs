using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for date/time processing operations using Python scripts
    /// </summary>
    public class PythonDateTimeProcessingAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonDateTimeProcessingAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void ExtractDateTimeComponents(string columnName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName
            };

            string script = PythonScriptTemplateManager.GetScript("extract_datetime_components", parameters);
            ExecuteInSession(script);
        }

        public void CalculateTimeDifference(string startColumn, string endColumn, string newColumnName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["start_column"] = startColumn,
                ["end_column"] = endColumn,
                ["new_column_name"] = newColumnName
            };

            string script = PythonScriptTemplateManager.GetScript("calculate_time_difference", parameters);
            ExecuteInSession(script);
        }

        public void HandleCyclicalTimeFeatures(string columnName, string featureType)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName,
                ["feature_type"] = featureType
            };

            string script = PythonScriptTemplateManager.GetScript("handle_cyclical_time_features", parameters);
            ExecuteInSession(script);
        }

        public void ParseDateColumn(string columnName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName
            };

            string script = PythonScriptTemplateManager.GetScript("parse_date_column", parameters);
            ExecuteInSession(script);
        }

        public void HandleMissingDates(string columnName, string method = "fill", string fillValue = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName,
                ["method"] = method,
                ["fill_value"] = fillValue
            };

            string script = PythonScriptTemplateManager.GetScript("handle_missing_dates", parameters);
            ExecuteInSession(script);
        }
    }
}