using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Beep.Python.Logic
{
    public class PythonMLManager : IPythonMLManager,IDisposable
    {
        private readonly PythonNetRunTimeManager _pythonRuntimeManager;
        private PyModule _persistentScope;
        public PythonMLManager(PythonNetRunTimeManager pythonRuntimeManager)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            InitializePythonEnvironment();
        }
        public bool IsInitialized => _pythonRuntimeManager.IsInitialized;
        private void InitializePythonEnvironment()
        {
            if (!_pythonRuntimeManager.IsInitialized)
            {
                _pythonRuntimeManager.Initialize();
            }
            if (!_pythonRuntimeManager.IsInitialized)
            {
                return;
            }
            using (Py.GIL())
            {
                _persistentScope = Py.CreateScope("__main__");
                _persistentScope.Exec("models = {}");  // Initialize the models dictionary
            }
        }
        public void TrainModelWithUpdatedData(string modelId, string updatedTrainDataPath, string[] featureColumns, string labelColumn, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters)
        {
            if (!IsInitialized)
            {
                return;
            }
            string algorithmName = Enum.GetName(typeof(MachineLearningAlgorithm), algorithm);
            string features = string.Join(", ", featureColumns.Select(fc => $"updated_data['{fc}']"));
            string paramsDict = String.Join(", ", parameters.Select(kv => $"'{kv.Key}': {kv.Value}"));
            string script = $@"
import pandas as pd
from sklearn import {algorithmName.ToLower()}

# Load updated training data
updated_data = pd.read_csv('{updatedTrainDataPath}')

# Check if the model already exists
if '{modelId}' in models:
    model = models['{modelId}']
else:
    model = {algorithmName.ToLower()}({paramsDict})

# Train or re-train the model
model.fit(updated_data[{features}], updated_data['{labelColumn}'])

# Store or update the model
models['{modelId}'] = model
";

            RunPythonScript(script, null);
        }
        public double GetScoreUsingExistingTestData(string modelId, string metric)
        {
            if (!IsInitialized)
            {
                return -1;
            }
            string script = metric switch
            {
                "accuracy" => $@"
from sklearn.metrics import accuracy_score
model = models['{modelId}']
predictions = model.predict(test_features)
score = accuracy_score(test_labels, predictions)
",
                "f1" => $@"
from sklearn.metrics import f1_score
model = models['{modelId}']
predictions = model.predict(test_features)
score = f1_score(test_labels, predictions)
",
                // Add more cases for different metrics as needed
                _ => throw new ArgumentException("Invalid metric specified")
            };

            RunPythonScript(script, null);

            // Retrieve the score from the Python script
            double score = 0.0; // Implement logic to retrieve the score
            return score;
        }
        public void LoadData(string filePath)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $@"
import pandas as pd
data = pd.read_csv('{filePath}')
";

            RunPythonScript(script, null);
        }
        public void LoadTestData(string filePath)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $@"
import pandas as pd
test = pd.read_csv('{filePath}')
";

            RunPythonScript(script, null);
        }
        public void LoadData(string filePath, string[] featureColumns, string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }
            string featureColumnsString = string.Join(", ", featureColumns.Select(fc => $"'{fc}'"));
            string script = $@"
import pandas as pd

# Load the dataset
data = pd.read_csv('{filePath}')

# Split into features and label
feature_columns = [{featureColumnsString}]
features = data[feature_columns]
label = data['{labelColumn}']

# Store features and label in the Python environment
# Assuming 'train_features', 'train_labels', 'test_features', 'test_labels' are already defined
train_features, test_features = features, features  # Placeholder, modify as needed
train_labels, test_labels = label, label  # Placeholder, modify as needed
";

            RunPythonScript(script, null);
        }
        public void SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath)
        {
            if (!IsInitialized)
            {
                return;
            }
            // Ensure testSize is more than 0.5 to make test set larger
            if (testSize <= 0.5)
            {
                throw new ArgumentException("Test size must be more than 50% of the data.");
            }

            string script = $@"
import pandas as pd
from sklearn.model_selection import train_test_split

# Load the dataset
data = pd.read_csv('{dataFilePath}')

# Split the dataset into training and testing sets
train, test = train_test_split(data, test_size={testSize})

# Save the split datasets to files
train.to_csv('{trainFilePath}', index=False)
test.to_csv('{testFilePath}', index=False)
";

            RunPythonScript(script, null);
        }
        public void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }
            string algorithmName = Enum.GetName(typeof(MachineLearningAlgorithm), algorithm);
            string features = string.Join(", ", featureColumns.Select(fc => $"data['{fc}']"));
            string paramsDict = String.Join(", ", parameters.Select(kv => $"'{kv.Key}': {kv.Value}"));
            string script = $@"
from sklearn import {algorithmName.ToLower()}

# Check if the model already exists
if '{modelId}' in models:
    model = models['{modelId}']
else:
    model = {algorithmName.ToLower()}({paramsDict})

# Train or re-train the model
model.fit(data[{features}], data['{labelColumn}'])

# Store or update the model
models['{modelId}'] = model
";

            RunPythonScript(script, null);
        }
        public dynamic Predict()
        {
            if (!IsInitialized)
            {
                return null;
            }
         //   string inputAsString = inputData.ToString(); // Convert inputData to a string representation
            string script = $@"
predictions = model.predict(test)
";

            RunPythonScript(script, null);

            // Retrieve predictions from Python script
            dynamic predictions = FetchPredictionsFromPython(); // Use the method to fetch predictions
            return predictions;
        }
        // Helper method to retrieve predictions from Python
        private dynamic FetchPredictionsFromPython()
        {
            if (!IsInitialized)
            {
                return null;
            }
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                // Retrieve the 'predictions' variable from the persistent Python scope
                dynamic predictions = _persistentScope.Get("predictions");

                // Convert the Python 'predictions' object to a C# object, if necessary
                // Conversion depends on the expected format of 'predictions'
                return ConvertPythonPredictionsToCSharp(predictions); // Implement this conversion based on your needs
            }
        }
        // Example method to convert Python predictions to a C# data structure
        private dynamic ConvertPythonPredictionsToCSharp(dynamic predictions)
        {
            if (!IsInitialized)
            {
                return null;
            }
            // Implement conversion logic based on the format of your Python predictions
            // For example, converting a NumPy array or Python list to a C# List
            var predictionsList = new List<double>();
            foreach (var item in predictions)
            {
                predictionsList.Add((double)item);
            }
            return predictionsList; // Or return as is, if no conversion is needed
        }
        public void SaveModel(string modelId, string filePath)
        {
            if (!IsInitialized)
            {
                return;
            }

            // Adjust script to save the model corresponding to the provided modelId
            string script = $@"
import joblib
model_to_save = models.get('{modelId}', None)  # Retrieve the model by ID
if model_to_save is not None:
    joblib.dump(model_to_save, '{filePath}')
else:
    print('Model not found.')
";

            RunPythonScript(script, null);
        }
        public string LoadModel(string filePath)
        {
            if (!IsInitialized)
            {
                // Optionally, you might want to provide more feedback here,
                // such as logging a message or throwing an exception
                return null;
            }

            // Generate a unique ID for the model
            string modelId = Guid.NewGuid().ToString();

            // Python script to load the model from the given file path and store it in the models dictionary
            string script = $@"
import joblib
try:
    model = joblib.load('{filePath}')
    models['{modelId}'] = model  # Store the model with its ID in the dictionary
except Exception as e:
    print('Error loading model:', e)
    model_id = None
";

            RunPythonScript(script, null);

            // If there was an error in loading the model, model_id will be set to None in Python
            // Check for this case and handle accordingly
            using (Py.GIL())
            {
                dynamic pyModelId = _persistentScope.Get("model_id");
                if (pyModelId == null || pyModelId.ToString() == "None")
                {
                    return null; // or handle the error as per your application's needs
                }
            }

            // Return the model ID to the caller
            return modelId;
        }
        public double GetModelScore(string modelId, ModelMetric metric)
        {
            if (!IsInitialized)
            {
                return -1;
            }
            string script = metric switch
            {
                ModelMetric.Accuracy => $@"
from sklearn.metrics import accuracy_score
model = models['{modelId}']  # Retrieve model by ID
predictions = model.predict(test_features)
score = accuracy_score(test_labels, predictions)
",
                ModelMetric.F1 => $@"
from sklearn.metrics import f1_score
model = models['{modelId}']  # Retrieve model by ID
predictions = model.predict(test_features)
score = f1_score(test_labels, predictions)
",
                // Add more cases for different metrics as needed
                _ => throw new ArgumentException("Invalid metric specified")
            };

            RunPythonScript(script, null);

            // Retrieve the score from the Python script
            double score = FetchScoreFromPython(); // Implement logic to retrieve the score
            return score;
        }
        // Example helper method to fetch the score from Python
        private double FetchScoreFromPython()
        {
            if (!IsInitialized)
            {
                return -1;
            }
            // Implement logic to retrieve the 'score' variable from Python
            // This might involve fetching the variable's value from the Python scope
            using (Py.GIL())
            {
                dynamic pyScore = _persistentScope.Get("score");
                return pyScore.As<double>(); // Convert the Python score to a C# double
            }
        }
        private void RunPythonScript(string script, dynamic parameters)
        {
            if (!IsInitialized)
            {
                return;
            }
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                _persistentScope.Exec(script); // Execute the script in the persistent scope
                                               // Handle outputs if needed

                // If needed, return results or handle outputs
            }
        }
        public void OutputResultData(string filePath, string iDColumn, string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $@"output = pd.DataFrame({{'{iDColumn.ToLower()}': test_data.{iDColumn.ToLower()}, '{labelColumn.ToLower()}': predictions}})
output.to_csv('{filePath}', index=False)";
            RunPythonScript(script, null);
        }
        public void Dispose()
        {
            _persistentScope.Dispose();
            _pythonRuntimeManager.ShutDown();
        }

    }

}
