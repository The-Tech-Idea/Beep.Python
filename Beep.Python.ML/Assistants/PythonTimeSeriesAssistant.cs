using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for time series operations using Python scripts
    /// </summary>
    public class PythonTimeSeriesAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonTimeSeriesAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void TimeSeriesAugmentation(string[] timeSeriesColumns, string augmentationType, double parameter)
        {
            var parameters = new Dictionary<string, object>
            {
                ["time_series_columns"] = timeSeriesColumns,
                ["augmentation_type"] = augmentationType,
                ["parameter"] = parameter
            };

            string script = PythonScriptTemplateManager.GetScript("time_series_augmentation", parameters);
            ExecuteInSession(script);
        }
    }
}