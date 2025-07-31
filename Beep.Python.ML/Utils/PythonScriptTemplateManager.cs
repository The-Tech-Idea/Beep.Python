using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Beep.Python.ML.Utils
{
    /// <summary>
    /// Utility class to load Python script files and perform parameter substitution
    /// </summary>
    public static class PythonScriptTemplateManager
    {
        private static readonly Dictionary<string, string> _scriptCache = new();
        private static readonly string _scriptsBasePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Scripts");

        /// <summary>
        /// Initialize the script templates directory
        /// </summary>
        static PythonScriptTemplateManager()
        {
            EnsureScriptsDirectory();
        }

        /// <summary>
        /// Get a Python script from file with parameter substitutions
        /// </summary>
        /// <param name="scriptName">Name of the Python script file (without .py extension)</param>
        /// <param name="parameters">Parameters to substitute in the template</param>
        /// <returns>Python script with substituted parameters</returns>
        public static string GetScript(string scriptName, Dictionary<string, object> parameters = null)
        {
            var template = LoadScriptFromFile(scriptName);
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            return parameters == null ? template : SubstituteParameters(template, parameters);
        }

        /// <summary>
        /// Load a Python script from file
        /// </summary>
        /// <param name="scriptName">Name of the script file (without .py extension)</param>
        /// <returns>Script content</returns>
        public static string LoadScriptFromFile(string scriptName)
        {
            // Check cache first
            if (_scriptCache.TryGetValue(scriptName, out var cachedScript))
                return cachedScript;

            var scriptPath = Path.Combine(_scriptsBasePath, $"{scriptName}.py");
            
            if (File.Exists(scriptPath))
            {
                var content = File.ReadAllText(scriptPath);
                _scriptCache[scriptName] = content; // Cache for future use
                return content;
            }

            throw new FileNotFoundException($"Python script file not found: {scriptPath}");
        }

        /// <summary>
        /// Substitute parameters in the template using placeholder replacement
        /// </summary>
        /// <param name="template">Template string with {parameter_name} placeholders</param>
        /// <param name="parameters">Parameters to substitute</param>
        /// <returns>Template with substituted parameters</returns>
        private static string SubstituteParameters(string template, Dictionary<string, object> parameters)
        {
            var result = template;
            
            foreach (var param in parameters)
            {
                var placeholder = $"{{{param.Key}}}";
                var value = FormatParameterValue(param.Value);
                result = result.Replace(placeholder, value);
            }

            return result;
        }

        /// <summary>
        /// Format parameter value for Python syntax
        /// </summary>
        /// <param name="value">Parameter value</param>
        /// <returns>Formatted value for Python</returns>
        private static string FormatParameterValue(object value)
        {
            return value switch
            {
                null => "None",
                string str => $"'{str.Replace("'", "\\'")}'", // Escape single quotes
                bool b => b.ToString().ToLower(),
                Dictionary<string, object> dict => FormatDictionary(dict),
                Dictionary<string, object[]> paramGrid => FormatParameterGrid(paramGrid),
                object[] array => FormatArray(array),
                int or long or short => value.ToString(),
                float or double or decimal => value.ToString(),
                _ => $"'{value.ToString().Replace("'", "\\'")}'".ToString()
            };
        }

        /// <summary>
        /// Format dictionary for Python syntax
        /// </summary>
        /// <param name="dict">Dictionary to format</param>
        /// <returns>Python dictionary string</returns>
        private static string FormatDictionary(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0)
                return "{}";

            var pairs = new List<string>();
            foreach (var kvp in dict)
            {
                pairs.Add($"'{kvp.Key}': {FormatParameterValue(kvp.Value)}");
            }

            return "{" + string.Join(", ", pairs) + "}";
        }

        /// <summary>
        /// Format parameter grid for hyperparameter optimization
        /// </summary>
        /// <param name="paramGrid">Parameter grid dictionary</param>
        /// <returns>Python parameter grid string</returns>
        private static string FormatParameterGrid(Dictionary<string, object[]> paramGrid)
        {
            if (paramGrid == null || paramGrid.Count == 0)
                return "{}";

            var pairs = new List<string>();
            foreach (var kvp in paramGrid)
            {
                pairs.Add($"'{kvp.Key}': {FormatArray(kvp.Value)}");
            }

            return "{" + string.Join(", ", pairs) + "}";
        }

        /// <summary>
        /// Format array for Python syntax
        /// </summary>
        /// <param name="array">Array to format</param>
        /// <returns>Python list string</returns>
        private static string FormatArray(object[] array)
        {
            if (array == null || array.Length == 0)
                return "[]";

            var values = new List<string>();
            foreach (var item in array)
            {
                values.Add(FormatParameterValue(item));
            }

            return "[" + string.Join(", ", values) + "]";
        }

        /// <summary>
        /// Check if a script file exists
        /// </summary>
        /// <param name="scriptName">Name of the script file (without .py extension)</param>
        /// <returns>True if file exists</returns>
        public static bool ScriptExists(string scriptName)
        {
            var scriptPath = Path.Combine(_scriptsBasePath, $"{scriptName}.py");
            return File.Exists(scriptPath);
        }

        /// <summary>
        /// Get the full path to a script file
        /// </summary>
        /// <param name="scriptName">Name of the script file (without .py extension)</param>
        /// <returns>Full path to script file</returns>
        public static string GetScriptPath(string scriptName)
        {
            return Path.Combine(_scriptsBasePath, $"{scriptName}.py");
        }

        /// <summary>
        /// Get the scripts directory path
        /// </summary>
        /// <returns>Scripts directory path</returns>
        public static string GetScriptsDirectory() => _scriptsBasePath;

        /// <summary>
        /// List all available Python script files
        /// </summary>
        /// <returns>Array of script names (without .py extension)</returns>
        public static string[] GetAvailableScripts()
        {
            if (!Directory.Exists(_scriptsBasePath))
                return Array.Empty<string>();

            var files = Directory.GetFiles(_scriptsBasePath, "*.py");
            var scriptNames = new string[files.Length];
            
            for (int i = 0; i < files.Length; i++)
            {
                scriptNames[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return scriptNames;
        }

        /// <summary>
        /// Clear the script cache (useful for development/testing)
        /// </summary>
        public static void ClearCache()
        {
            _scriptCache.Clear();
        }

        /// <summary>
        /// Initialize the scripts directory if it doesn't exist
        /// </summary>
        public static void EnsureScriptsDirectory()
        {
            if (!Directory.Exists(_scriptsBasePath))
            {
                Directory.CreateDirectory(_scriptsBasePath);
            }
        }

        /// <summary>
        /// Create default Python script files if they don't exist
        /// </summary>
        public static void CreateDefaultScripts()
        {
            EnsureScriptsDirectory();
            
            // This method can be used to create default script files
            // We'll leave the scripts empty for now since they should be created separately
            var defaultScripts = new[]
            {
                "cross_validation",
                "grid_search", 
                "random_search",
                "model_comparison",
                "comprehensive_evaluation",
                "training_initialization"
            };

            foreach (var scriptName in defaultScripts)
            {
                var scriptPath = Path.Combine(_scriptsBasePath, $"{scriptName}.py");
                if (!File.Exists(scriptPath))
                {
                    // Create empty placeholder files
                    File.WriteAllText(scriptPath, $"# {scriptName}.py - Python script for {scriptName.Replace("_", " ")}\n# This file should contain the Python code for {scriptName.Replace("_", " ")} functionality\n");
                }
            }
        }
    }
}