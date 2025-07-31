using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for data cleaning operations using Python scripts
    /// </summary>
    public class PythonDataCleaningAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonDataCleaningAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void RemoveOutliers(string[] featureList = null, double zThreshold = 3.0)
        {
            var parameters = new Dictionary<string, object>
            {
                ["feature_list"] = featureList ?? Array.Empty<string>(),
                ["z_threshold"] = zThreshold
            };

            string script = PythonScriptTemplateManager.GetScript("remove_outliers", parameters);
            ExecuteInSession(script);
        }

        public void DropDuplicates(string[] featureList = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["feature_list"] = featureList ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("drop_duplicates", parameters);
            ExecuteInSession(script);
        }

        public void StandardizeCategories(string[] featureList = null, Dictionary<string, string> replacements = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["feature_list"] = featureList ?? Array.Empty<string>(),
                ["replacements"] = replacements ?? new Dictionary<string, string>()
            };

            string script = PythonScriptTemplateManager.GetScript("standardize_categories", parameters);
            ExecuteInSession(script);
        }
    }
}