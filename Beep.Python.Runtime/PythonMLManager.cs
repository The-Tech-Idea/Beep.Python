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
train_data = pd.read_csv('{modifiedFilePath}')
features = train_data.columns.tolist()
";

            RunPythonScript(script, null);
            // Retrieve the features (column names) from the Python script
            return FetchFeaturesFromPython();
        }
        // Method to retrieve the features from Python
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
        public string[] LoadPredictionData(string filePath)
        {
            if (!IsInitialized)
            {
                return null;
            }
            // Convert the file path to a raw string format for Python
            string formattedFilePath = filePath.Replace("\\", "\\\\");
            string script = $@"
import pandas as pd
predict_data = pd.read_csv('{formattedFilePath}')

# Split into features and label
predict_features = predict_data.columns.tolist()
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
        public string[] SplitData(string dataFilePath, float testSize,float validationSize, string trainFilePath, string testFilePath,string validationFilePath)
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
            if (testSize + validationSize >= 1.0)
            {
                throw new ArgumentException("Combined test and validation size must be less than 100% of the data.");
            }
            string formattedFilePath = dataFilePath.Replace("\\", "\\\\");
            string formattedtrainFilePath = trainFilePath.Replace("\\", "\\\\");
            string formattedtestFilePath = testFilePath.Replace("\\", "\\\\");
            string formattedvalidationFilePath = validationFilePath.Replace("\\", "\\\\");
            string script = $@"
import pandas as pd
from sklearn.model_selection import train_test_split

# Load the dataset
data = pd.read_csv('{formattedFilePath}')
# Split the dataset into training and remaining data
train_data, remaining_data = train_test_split(data, test_size={testSize + validationSize})

# Split the remaining data into testing and validation sets
test_data, validation_data = train_test_split(remaining_data, test_size={validationSize / (testSize + validationSize)})


# Save the datasets to files
train_data.to_csv('{formattedtrainFilePath}', index=False)
test_data.to_csv('{formattedtestFilePath}', index=False)
validation_data.to_csv('{formattedvalidationFilePath}', index=False)
test_features = test_data.columns.tolist()
features = data.columns.tolist()
";
            //            train_data.to_csv('{trainFilePath}', index = False)
            //test_data.to_csv('{testFilePath}', index = False)
            RunPythonScript(script, null);
            return FetchFeaturesFromPython();
        }
        public string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath, string validationFilePath, string primaryFeatureKeyID, string labelColumn)
        {
            if (!IsInitialized)
            {
                return null;
            }

            if (testSize >= 1.0)
            {
                throw new ArgumentException("Test size must be less than 100% of the data.");
            }

            string formattedFilePath = dataFilePath.Replace("\\", "\\\\");
            string formattedTrainFilePath = trainFilePath.Replace("\\", "\\\\");
            string formattedTestFilePath = testFilePath.Replace("\\", "\\\\");
            string formattedValidationFilePath = validationFilePath.Replace("\\", "\\\\");

            string script = $@"
import pandas as pd
from sklearn.model_selection import train_test_split

# Load the dataset
data = pd.read_csv('{formattedFilePath}')

# Split the dataset into training and test sets
train_data, test_data = train_test_split(data, test_size={testSize})

# Prepare test data (without label column)
test_data_without_label = test_data.drop(columns=['{labelColumn}'])

# Prepare validation data (only primary key and label)
validation_data = test_data[['{primaryFeatureKeyID}', '{labelColumn}']]

# Save the datasets to files
train_data.to_csv('{formattedTrainFilePath}', index=False)
test_data_without_label.to_csv('{formattedTestFilePath}', index=False)
validation_data.to_csv('{formattedValidationFilePath}', index=False)
test_features = test_data.columns.tolist()
features = data.columns.tolist()
";

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
            string importStatement;
            switch (algorithmName)
            {
                case "LogisticRegression":
                    importStatement = "from sklearn.linear_model import LogisticRegression";
                    break;
                case "RandomForestClassifier":
                    importStatement = "from sklearn.ensemble import RandomForestClassifier";
                    break;
                case "RandomForestRegressor":
                    importStatement = "from sklearn.ensemble import RandomForestRegressor";
                    break;
                case "SVC":  // Support Vector Classification
                    importStatement = "from sklearn.svm import SVC";
                    break;
                case "SVR":  // Support Vector Regression
                    importStatement = "from sklearn.svm import SVR";
                    break;
                case "KNeighborsClassifier":
                    importStatement = "from sklearn.neighbors import KNeighborsClassifier";
                    break;
                case "KNeighborsRegressor":
                    importStatement = "from sklearn.neighbors import KNeighborsRegressor";
                    break;
                case "GradientBoostingClassifier":
                    importStatement = "from sklearn.ensemble import GradientBoostingClassifier";
                    break;
                case "GradientBoostingRegressor":
                    importStatement = "from sklearn.ensemble import GradientBoostingRegressor";
                    break;
                case "DecisionTreeClassifier":
                    importStatement = "from sklearn.tree import DecisionTreeClassifier";
                    break;
                case "DecisionTreeRegressor":
                    importStatement = "from sklearn.tree import DecisionTreeRegressor";
                    break;
                // Add more cases as needed for different algorithms
                default:
                    throw new ArgumentException($"Unsupported algorithm: {algorithmName}");
            }

            string script = $@"
{importStatement}
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
if Y.dtype.kind in 'if':  # 'i' is for integer and 'f' is for float
    Y = Y.astype('category')
# Convert float labels to int if they are essentially binary
if Y.dtype.kind == 'f' and Y.nunique() == 2:
    Y = Y.astype(int)

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
X_predict = pd.get_dummies(predict_data[features])
X_predict.fillna(X_predict.mean(), inplace=True)  # Simple mean imputation for missing values

predictions = model.predict(X_predict)
";

            RunPythonScript(script, null);

            // Retrieve predictions from Python script
            dynamic predictions = FetchPredictionsFromPython(); // Use the method to fetch predictions
            return predictions;
        }
        // Helper method to retrieve predictions from Python
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
        public void ExportTestResult(string filePath, string iDColumn, string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }

            string script = $@"
# Assuming predictions and X_predict are available
output = pd.DataFrame({{
    '{iDColumn}': predict_data['{iDColumn}'],
    '{labelColumn}': predictions
}})
output.to_csv(r'{filePath}', index=False)
";

            RunPythonScript(script, null);
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
                dynamic pyFeatures = _persistentScope.Get("predict_features");
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
    }

}
