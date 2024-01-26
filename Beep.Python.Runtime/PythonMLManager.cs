using Beep.Python.Model;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Beep.Python.RuntimeEngine
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
        public void ImportPythonModule(string moduleName)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import {moduleName}";
            RunPythonScript(script, null);
        }
        public bool IsInitialized => _pythonRuntimeManager.IsInitialized;
        private bool InitializePythonEnvironment()
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)
            {
                _pythonRuntimeManager.Initialize();
            }
            if (!_pythonRuntimeManager.IsInitialized)
            {
                return retval;
            }
            using (Py.GIL())
            {
                _persistentScope = Py.CreateScope("__main__");
                _persistentScope.Exec("models = {}");  // Initialize the models dictionary
                retval=true; 
            }
            return retval;
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
        public double EvaluateModel(string modelId, string testFilePath, string[] featureColumns, string labelColumn)
        {
            if (!IsInitialized)
            {
                return -1;
            }

            string formattedTestFilePath = testFilePath.Replace("\\", "\\\\");
            string features = string.Join(", ", featureColumns.Select(fc => $"\"{fc}\""));

            string script = $@"
import pandas as pd
from sklearn.metrics import accuracy_score

# Load the test data
test_data = pd.read_csv('{formattedTestFilePath}')
X_test = pd.get_dummies(test_data[{features}])
y_test = test_data['{labelColumn}']

# Retrieve the model and make predictions
model = models['{modelId}']
predictions = model.predict(X_test)

# Calculate accuracy
score = accuracy_score(y_test, predictions)
";

            RunPythonScript(script, null);

            return FetchScoreFromPython();
        }
        public Tuple<double, double> GetModelScore(string modelId)
        {
            if (!IsInitialized)
            {
                return new Tuple<double, double>(-1,-1);
            }

            // Script to prepare test data (X_test and y_test) similarly to how training data was prepared
            string prepareTestDataScript = @"
# Assuming test data is loaded and preprocessed similarly to training data
X_test = pd.get_dummies(test_data[test_features])
X_test.fillna(X_test.mean(), inplace=True)
y_test = test_data[label_column]
# Align the test set columns with the training set
# This adds missing columns in the test set and sets them to zero
test_encoded = X_test.reindex(columns = X.columns, fill_value=0)
";

            RunPythonScript(prepareTestDataScript, null);
            string script  =$@"
from sklearn.metrics import accuracy_score
from sklearn.metrics import f1_score
model = models['{modelId}']
predictions = model.predict(test_encoded)
accuracy = accuracy_score(y_test, predictions)
score = f1_score(y_test, predictions)
";
           
            RunPythonScript(script, null);

            // Retrieve the score from the Python script
            return new Tuple<double,double>(FetchScoreFromPython(),FetchAccuracyFromPython());
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
        public string[] LoadData(string filePath)
        {
            if (!IsInitialized)
            {
                return null;
            }
            // Replace backslashes with double backslashes or use a raw string
            string modifiedFilePath = filePath.Replace("\\", "\\\\");
            string script = $@"
import pandas as pd
data = pd.read_csv('{modifiedFilePath}')
features = data.columns.tolist()
";

            RunPythonScript(script, null);
            // Retrieve the features (column names) from the Python script
            return FetchFeaturesFromPython();
        }
        // Method to retrieve the features from Python
        private string[] FetchFeaturesFromPython()
        {
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                dynamic pyFeatures = _persistentScope.Get("features");
                if (pyFeatures == null) return new string[0];

                // Convert the Python list to a C# string array
                var featuresList = new List<string>();
                foreach (var feature in pyFeatures)
                {
                    featuresList.Add(feature.ToString());
                }
                return featuresList.ToArray();
            }
        }
        private string[] FetchTestFeaturesFromPython()
        {
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                dynamic pyFeatures = _persistentScope.Get("test_features");
                if (pyFeatures == null) return new string[0];

                // Convert the Python list to a C# string array
                var featuresList = new List<string>();
                foreach (var feature in pyFeatures)
                {
                    featuresList.Add(feature.ToString());
                }
                return featuresList.ToArray();
            }
        }
        public string[] LoadTestData(string filePath)
        {
            if (!IsInitialized)
            {
                return null;
            }
            // Convert the file path to a raw string format for Python
            string formattedFilePath = filePath.Replace("\\", "\\\\");
            string script = $@"
import pandas as pd
test_data = pd.read_csv('{formattedFilePath}')

# Split into features and label
test_features = test_data.columns.tolist()
";

            RunPythonScript(script, null);
            return FetchTestFeaturesFromPython();
        }
        public void AddLabelColumnIfMissing(string testDataFilePath, string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }

            // Convert file paths to a raw string format for Python
            string formattedTestDataFilePath = testDataFilePath.Replace("\\", "\\\\");

            string script = $@"
import pandas as pd

# Load test data
test_data = pd.read_csv(r'{formattedTestDataFilePath}')

# Check if the label column is missing and add it if necessary
if '{labelColumn}' not in test_data.columns:
    test_data['{labelColumn}'] = None  # Assign None for missing label column

# Optionally, save the modified test data back to a file or handle it as needed
test_data.to_csv(r'{formattedTestDataFilePath}', index=False)
";

            RunPythonScript(script, null);
        }
        public void AddLabelColumnIfMissing( string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }

        
            string script = $@"
import pandas as pd


# Check if the label column is missing and add it if necessary
if '{labelColumn}' not in test_data.columns:
    test_data['{labelColumn}'] = None  # Assign None for missing label column
";

            RunPythonScript(script, null);
        }
        public void LoadData(string filePath, string[] featureColumns, string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }
            string formattedFilePath = filePath.Replace("\\", "\\\\");
            string featureColumnsString = string.Join(", ", featureColumns.Select(fc => $"'{fc}'"));
            string script = $@"
import pandas as pd

# Load the dataset
data = pd.read_csv('{formattedFilePath}')

# Split into features and label
feature_columns = [{featureColumnsString}]
features = data[feature_columns]
label = data['{labelColumn}']
label_column ='{labelColumn}'
# Store features and label in the Python environment
# Assuming 'train_features', 'train_labels', 'test_features', 'test_labels' are already defined
train_features, test_features = features, features  # Placeholder, modify as needed
train_labels, test_labels = label, label  # Placeholder, modify as needed
";

            RunPythonScript(script, null);
        }
        public string[] SplitData(string dataFilePath, float testSize,string trainFilePath,string testFilePath)
        {
            if (!IsInitialized)
            {
                return null;
            }
            // Ensure testSize is more than 0.5 to make test set larger
            if (testSize <= 0.5)
            {
                throw new ArgumentException("Test size must be more than 50% of the data.");
            }
            string formattedFilePath = dataFilePath.Replace("\\", "\\\\");
            string formattedtrainFilePath = trainFilePath.Replace("\\", "\\\\");
            string formattedtestFilePath = testFilePath.Replace("\\", "\\\\");
            string script = $@"
import pandas as pd
from sklearn.model_selection import train_test_split

# Load the dataset
data = pd.read_csv('{formattedFilePath}')

# Split the dataset into training and testing sets
train_data, test_data = train_test_split(data, test_size={testSize})

# Save the split datasets to files
train_data.to_csv('{formattedtrainFilePath}', index = False)
test_data.to_csv('{formattedtestFilePath}', index = False)
test_features = test_data.columns.tolist()
features = data.columns.tolist()
";
//            train_data.to_csv('{trainFilePath}', index = False)
//test_data.to_csv('{testFilePath}', index = False)
            RunPythonScript(script, null);
            return FetchFeaturesFromPython();
        }
        public void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }
            string algorithmName = Enum.GetName(typeof(MachineLearningAlgorithm), algorithm);
            string features = string.Join(", ", featureColumns.Select(fc => @$"'{fc}'"));
            string paramsDict = String.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
            string script = $@"
from sklearn.ensemble import {algorithmName}
features = [{features}]
# Check if the model already exists
if '{modelId}' in models:
    model = models['{modelId}']
else:
    model = {algorithmName}({paramsDict})

# Train or re-train the model
X= pd.get_dummies(train_data[features])
X.fillna(X.mean(), inplace=True)  # Simple mean imputation for missing values

Y=train_data['{labelColumn}']
Y = train_data['{labelColumn}'].fillna(train_data['{labelColumn}'].mean())  # Assuming you also want to handle NaN in labels
model.fit(X, Y)
label = train_data['{labelColumn}']
label_column ='{labelColumn}'
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
X_test = pd.get_dummies(test_data[test_features])
predictions = model.predict(X_test)
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
        private double FetchAccuracyFromPython()
        {
            if (!IsInitialized)
            {
                return -1;
            }
            // Implement logic to retrieve the 'score' variable from Python
            // This might involve fetching the variable's value from the Python scope
            using (Py.GIL())
            {
                dynamic pyScore = _persistentScope.Get("accuracy");
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
        public void ExportTestResult(string filePath, string iDColumn, string labelColumn)
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
        public Tuple<string,string> SplitDataClassFile(string urlpath,string filename, double splitRatio)
        {
            try
            {
                ValidateSplitRatio(ref splitRatio); // Ensuring split ratio is valid

                string dataFilePath = Path.Combine(urlpath, filename);
              

                if (!File.Exists(dataFilePath))
                {

                    return new Tuple<string,string>(null,null);
                }

                string[] lines = File.ReadAllLines(dataFilePath);
                ShuffleData(lines); // Shuffling the data

                int totalLines = lines.Length;
                int trainingLinesCount = (int)(totalLines * splitRatio);
                int testingLinesCount = totalLines - trainingLinesCount;

                string[] trainingData = lines.Take(trainingLinesCount).ToArray();
                string[] testingData = lines.Skip(trainingLinesCount).Take(testingLinesCount).ToArray();

                string trainingFileName = CreateSplitFile(urlpath, "train_", filename, trainingData);
                string testingFileName = CreateSplitFile(urlpath, "test_", filename, testingData);

                 string TRAININGFILENAME = Path.GetFileName(trainingFileName);
                string TESTDATAFILENAME = Path.GetFileName(testingFileName);

                CreateValidationFile(urlpath, testingFileName);
                return new Tuple<string, string>(TRAININGFILENAME, TESTDATAFILENAME);
            }
            catch (Exception ex)
            {
                return new Tuple<string, string>(null, null);
                //DMEditor.ErrorObject.Ex = ex;
                //DMEditor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} -  {ex.Message}";
                //DMEditor.ErrorObject.Flag = Errors.Failed;
            }
        }
        private void ShuffleData(string[] lines)
        {
            if (lines.Length <= 1) return; // No need to shuffle if only header or no data

            var random = new Random();
            for (int i = lines.Length - 1; i > 0; i--)
            {
                int j = random.Next(1, i + 1); // Start from 1 to skip the header row
                var temp = lines[j];
                lines[j] = lines[i];
                lines[i] = temp;
            }
        }
        private void ValidateSplitRatio(ref double splitRatio)
        {
            // Define the acceptable range for the split ratio
            const double minRatio = 0.6; // 60%
            const double maxRatio = 0.8; // 80%

            // Check if the split ratio is within the acceptable range
            if (splitRatio < minRatio || splitRatio > maxRatio)
            {
               // DMEditor.AddLogMessage("Beep", $"Split ratio must be between {minRatio * 100}% and {maxRatio * 100}%", DateTime.Now, -1, null, Errors.Failed);
            }
        }
        private string CreateSplitFile(string path, string prefix, string originalFileName, string[] data)
        {
            string newFileName = Path.Combine(path, $"{prefix}{originalFileName}");
            File.WriteAllLines(newFileName, data);
            return newFileName;
        }
        private void CreateValidationFile(string path, string sourceFileName)
        {
            string validationFileName = Path.Combine(path, $"validation_{Path.GetFileName(sourceFileName)}");
            File.Copy(sourceFileName, validationFileName);

            string[] lines = File.ReadAllLines(validationFileName);
            ClearLabelColumn(lines);
            File.WriteAllLines(validationFileName, lines);
          
        }
        private void ClearLabelColumn(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');

                if (columns.Length > 0)
                {
                    columns[columns.Length - 1] = ""; // Clearing the label column
                }

                lines[i] = string.Join(",", columns);
            }
        }
    }

}
