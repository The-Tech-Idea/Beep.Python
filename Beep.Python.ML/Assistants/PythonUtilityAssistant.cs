using System;
using System.Collections.Generic;
using Beep.Python.Model;
using Beep.Python.ML.Utils;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.ML.Assistants
{
    /// <summary>
    /// Assistant class for utility operations using Python scripts
    /// </summary>
    public class PythonUtilityAssistant
    {
        private readonly IPythonRunTimeManager _pythonRuntime;
        private readonly PythonSessionInfo _sessionInfo;

        public PythonUtilityAssistant(IPythonRunTimeManager pythonRuntime, PythonSessionInfo sessionInfo)
        {
            _pythonRuntime = pythonRuntime;
            _sessionInfo = sessionInfo;
        }

        private bool ExecuteInSession(string script)
        {
            return _pythonRuntime.ExecuteManager.RunPythonScript(script, null, _sessionInfo);
        }

        public void AddLabelColumnIfMissing(string testDataFilePath, string labelColumn)
        {
            var parameters = new Dictionary<string, object>
            {
                ["test_data_file_path"] = testDataFilePath,
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("add_label_column_if_missing_file", parameters);
            ExecuteInSession(script);
        }

        public void AddLabelColumnIfMissing(string labelColumn)
        {
            var parameters = new Dictionary<string, object>
            {
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("add_label_column_if_missing", parameters);
            ExecuteInSession(script);
        }

        public string[] SplitData(float testSize, string trainFilePath, string testFilePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["test_size"] = testSize,
                ["train_file_path"] = trainFilePath,
                ["test_file_path"] = testFilePath
            };

            string script = PythonScriptTemplateManager.GetScript("split_data", parameters);
            ExecuteInSession(script);
            
            return new[] { trainFilePath, testFilePath };
        }

        public string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["data_file_path"] = dataFilePath,
                ["test_size"] = testSize,
                ["train_file_path"] = trainFilePath,
                ["test_file_path"] = testFilePath
            };

            string script = PythonScriptTemplateManager.GetScript("split_data_from_file", parameters);
            ExecuteInSession(script);
            
            return new[] { trainFilePath, testFilePath };
        }

        public string[] SplitData(string dataFilePath, float testSize, float validationSize, string trainFilePath, string testFilePath, string validationFilePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["data_file_path"] = dataFilePath,
                ["test_size"] = testSize,
                ["validation_size"] = validationSize,
                ["train_file_path"] = trainFilePath,
                ["test_file_path"] = testFilePath,
                ["validation_file_path"] = validationFilePath
            };

            string script = PythonScriptTemplateManager.GetScript("split_data_three_way", parameters);
            ExecuteInSession(script);
            
            return new[] { trainFilePath, testFilePath, validationFilePath };
        }

        public string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath, string validationFilePath, string primaryFeatureKeyID, string labelColumn)
        {
            var parameters = new Dictionary<string, object>
            {
                ["data_file_path"] = dataFilePath,
                ["test_size"] = testSize,
                ["train_file_path"] = trainFilePath,
                ["test_file_path"] = testFilePath,
                ["validation_file_path"] = validationFilePath,
                ["primary_feature_key_id"] = primaryFeatureKeyID,
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("split_data_with_key", parameters);
            ExecuteInSession(script);
            
            return new[] { trainFilePath, testFilePath, validationFilePath };
        }

        public Tuple<string, string> SplitDataClassFile(string urlpath, string filename, double splitRatio)
        {
            var parameters = new Dictionary<string, object>
            {
                ["url_path"] = urlpath,
                ["filename"] = filename,
                ["split_ratio"] = splitRatio
            };

            string script = PythonScriptTemplateManager.GetScript("split_data_class_file", parameters);
            ExecuteInSession(script);
            
            return new Tuple<string, string>("train.csv", "test.csv");
        }

        public void ExportTestResult(string filePath, string iDColumn, string labelColumn)
        {
            var parameters = new Dictionary<string, object>
            {
                ["file_path"] = filePath,
                ["id_column"] = iDColumn,
                ["label_column"] = labelColumn
            };

            string script = PythonScriptTemplateManager.GetScript("export_test_result", parameters);
            ExecuteInSession(script);
        }
    }
}