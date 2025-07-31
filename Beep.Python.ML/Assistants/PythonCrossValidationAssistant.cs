using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for cross-validation and sampling operations using Python scripts
    /// </summary>
    public class PythonCrossValidationAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonCrossValidationAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void PerformCrossValidation(string modelId, int numFolds = 5)
        {
            var parameters = new Dictionary<string, object>
            {
                ["model_id"] = modelId,
                ["num_folds"] = numFolds
            };

            string script = PythonScriptTemplateManager.GetScript("perform_cross_validation", parameters);
            ExecuteInSession(script);
        }

        public void PerformStratifiedSampling(float testSize, string trainFilePath, string testFilePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["test_size"] = testSize,
                ["train_file_path"] = trainFilePath,
                ["test_file_path"] = testFilePath
            };

            string script = PythonScriptTemplateManager.GetScript("perform_stratified_sampling", parameters);
            ExecuteInSession(script);
        }
    }
}