using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for text processing operations using Python scripts
    /// </summary>
    public class PythonTextProcessingAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonTextProcessingAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void ConvertTextToLowercase(string columnName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName
            };

            string script = PythonScriptTemplateManager.GetScript("convert_text_to_lowercase", parameters);
            ExecuteInSession(script);
        }

        public void RemovePunctuation(string columnName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName
            };

            string script = PythonScriptTemplateManager.GetScript("remove_punctuation", parameters);
            ExecuteInSession(script);
        }

        public void RemoveStopwords(string columnName, string language = "english")
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName,
                ["language"] = language
            };

            string script = PythonScriptTemplateManager.GetScript("remove_stopwords", parameters);
            ExecuteInSession(script);
        }

        public void ApplyStemming(string columnName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName
            };

            string script = PythonScriptTemplateManager.GetScript("apply_stemming", parameters);
            ExecuteInSession(script);
        }

        public void ApplyLemmatization(string columnName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName
            };

            string script = PythonScriptTemplateManager.GetScript("apply_lemmatization", parameters);
            ExecuteInSession(script);
        }

        public void ApplyTokenization(string columnName)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName
            };

            string script = PythonScriptTemplateManager.GetScript("apply_tokenization", parameters);
            ExecuteInSession(script);
        }

        public void ApplyTFIDFVectorization(string columnName, int maxFeatures = 1000)
        {
            var parameters = new Dictionary<string, object>
            {
                ["column_name"] = columnName,
                ["max_features"] = maxFeatures
            };

            string script = PythonScriptTemplateManager.GetScript("apply_tfidf_vectorization", parameters);
            ExecuteInSession(script);
        }
    }
}