using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for dimensionality reduction operations using Python scripts
    /// </summary>
    public class PythonDimensionalityReductionAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonDimensionalityReductionAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void ApplyPCA(int nComponents = 2, string[] featureList = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["n_components"] = nComponents,
                ["feature_list"] = featureList ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("apply_pca", parameters);
            ExecuteInSession(script);
        }

        public void ApplyLDA(string labelColumn, int nComponents = 2, string[] featureList = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["label_column"] = labelColumn,
                ["n_components"] = nComponents,
                ["feature_list"] = featureList ?? Array.Empty<string>()
            };

            string script = PythonScriptTemplateManager.GetScript("apply_lda", parameters);
            ExecuteInSession(script);
        }
    }
}