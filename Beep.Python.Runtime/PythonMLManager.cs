using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheTechIdea.Beep.Container.Services;


namespace Beep.Python.RuntimeEngine
{
    public class PythonMLManager : PythonBaseViewModel, IPythonMLManager, IDisposable
    {

        private bool IsInitialized = true; // Ensure this flag is managed based on actual initialization logic
        private Dictionary<string, bool> algorithmSupervision = new Dictionary<string, bool>();

        public bool IsDataLoaded { get; set; } = false;
        public bool IsModelTrained { get; set; } = false;
        public bool IsModelSaved { get; set; } = false;
        public bool IsModelLoaded { get; set; } = false;
        public bool IsModelPredicted { get; set; } = false;
        public bool IsModelScored { get; set; } = false;
        public bool IsModelExported { get; set; } = false;
        public bool IsDataSplit { get; set; } = false;
        public string DataFilePath { get; set; } = string.Empty;
        public string ModelFilePath { get; set; } = string.Empty;
        public string PredictionsFilePath { get; set; } = string.Empty;
        public string TrainingFilePath { get; set; } = string.Empty;
        public string TestingFilePath { get; set; } = string.Empty;
        public string ValidationFilePath { get; set; } = string.Empty;



        public PythonMLManager(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager, PythonSessionInfo sessionInfo) : base(beepservice, pythonRuntimeManager, sessionInfo)
        {
            //  pythonRuntimeManager = pythonRuntimeManager;

            InitializePythonEnvironment();
            InitializeAlgorithmSupervision();
        }

        private void InitializeAlgorithmSupervision()
        {
            // Here you set whether each algorithm is supervised or not
            algorithmSupervision["HistGradientBoostingRegressor"] = true;
            algorithmSupervision["HistGradientBoostingClassifier"] = true;
            algorithmSupervision["LogisticRegression"] = true;
            algorithmSupervision["RandomForestClassifier"] = true;
            algorithmSupervision["RandomForestRegressor"] = true;
            algorithmSupervision["SVC"] = true;
            algorithmSupervision["SVR"] = true;
            algorithmSupervision["KNeighborsClassifier"] = true;
            algorithmSupervision["KNeighborsRegressor"] = true;
            algorithmSupervision["GradientBoostingClassifier"] = true;
            algorithmSupervision["GradientBoostingRegressor"] = true;
            algorithmSupervision["DecisionTreeClassifier"] = true;
            algorithmSupervision["DecisionTreeRegressor"] = true;
            algorithmSupervision["LinearRegression"] = true;
            algorithmSupervision["LassoRegression"] = true;
            algorithmSupervision["RidgeRegression"] = true;
            algorithmSupervision["ElasticNet"] = true;
            algorithmSupervision["KMeans"] = false;
            algorithmSupervision["DBSCAN"] = false;
            algorithmSupervision["AgglomerativeClustering"] = false;
            algorithmSupervision["GaussianNB"] = true;
            algorithmSupervision["MultinomialNB"] = true;
            algorithmSupervision["BernoulliNB"] = true;
            algorithmSupervision["AdaBoostClassifier"] = true;
        }
        #region "Validate CSV File"
        public string[] ValidateAndPreviewData(string filePath, int numRows = 5)
        {
            if (!IsInitialized)
            {
                return null;
            }

            string formattedFilePath = filePath.Replace("\\", "\\\\");

            // Python script to load the first few rows and perform basic validation
            string script = $@"
import pandas as pd

# Load the first few rows of the dataset
preview_data = pd.read_csv('{formattedFilePath}', nrows={numRows})

# Perform basic validation
expected_columns = preview_data.columns.tolist()

# Check for missing values in the preview (optional)
missing_values = preview_data.isnull().sum().tolist()

# Check data types (optional)
data_types = preview_data.dtypes.apply(lambda X: X.name).tolist()

# Assign results to the persistent scope
preview_columns = expected_columns
preview_missing_values = missing_values
preview_data_types = data_types
";

            // Execute the Python script
            RunPythonScript(script, null);

            // Fetch and return the preview column names
            return FetchPreviewColumnsFromPython();
        }

        private string[] FetchPreviewColumnsFromPython()
        {
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                dynamic pyColumns = PythonRuntime.CurrentPersistentScope.Get("preview_columns");
                if (pyColumns == null) return new string[0];

                // Convert the Python list to a C# string array
                var columnsList = new List<string>();
                foreach (var column in pyColumns)
                {
                    columnsList.Add(column.ToString());
                }
                return columnsList.ToArray();
            }
        }

        // Additional methods to fetch missing values and data types if needed
        private int[] FetchPreviewMissingValuesFromPython()
        {
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                dynamic pyMissingValues = PythonRuntime.CurrentPersistentScope.Get("preview_missing_values");
                if (pyMissingValues == null) return new int[0];

                // Convert the Python list to a C# int array
                var missingValuesList = new List<int>();
                foreach (var value in pyMissingValues)
                {
                    missingValuesList.Add((int)value);
                }
                return missingValuesList.ToArray();
            }
        }

        private string[] FetchPreviewDataTypesFromPython()
        {
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                dynamic pyDataTypes = PythonRuntime.CurrentPersistentScope.Get("preview_data_types");
                if (pyDataTypes == null) return new string[0];

                // Convert the Python list to a C# string array
                var dataTypesList = new List<string>();
                foreach (var dtype in pyDataTypes)
                {
                    dataTypesList.Add(dtype.ToString());
                }
                return dataTypesList.ToArray();
            }
        }

        #endregion "Validate CSV File"
        #region "Data File Manup"
        public void FilterDataToSelectedFeatures(string[] selectedFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            // Convert the array of selected features to a Python list format
            string selectedFeaturesList = string.Join(", ", selectedFeatures.Select(f => $"'{f}'"));

            // Python script to filter the datasets based on selected features
            string script = $@"
# List of selected features
selected_features = [{selectedFeaturesList}]

# Filter train_data, test_data, and data based on selected features
if 'train_data' in globals():
    train_data = train_data[selected_features]
if 'test_data' in globals():
    test_data = test_data[selected_features]
if 'data' in globals():
    data = data[selected_features]

# Update the datasets in the Python scope (if needed)
globals()['train_data'] = train_data if 'train_data' in globals() else None
globals()['test_data'] = test_data if 'test_data' in globals() else None
globals()['data'] = data if 'data' in globals() else None
";

            // Execute the Python script
            RunPythonScript(script, null);
        }
        public string[] LoadData(string filePath, string[] selectedFeatures)
        {
            if (!IsInitialized)
            {
                return null;
            }

            // Replace backslashes with double backslashes or use a raw string
            string modifiedFilePath = filePath.Replace("\\", "\\\\");

            // Convert the array of selected features to a Python list format
            string selectedFeaturesList = string.Join(", ", selectedFeatures.Select(f => $"'{f}'"));

            // Python script to load the data and filter by selected features
            string script = $@"
import pandas as pd

# Load the dataset
data = pd.read_csv('{modifiedFilePath}')

# List of selected features
selected_features = [{selectedFeaturesList}]

# Filter the data based on selected features
data = data[selected_features]

# Get the final list of features after filtering
features = data.columns.tolist()

# Store the filtered data back to the global scope if needed
globals()['data'] = data
";

            if (RunPythonScript(script, null))
            {
                IsDataLoaded = true;
                DataFilePath = modifiedFilePath;
            }
            else
            {
                IsDataLoaded = false;
            }

            // Retrieve the features (column names) from the Python script
            return FetchFeaturesFromPython();
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
features = train_data.columns.tolist()
";

            if (RunPythonScript(script, null))
            {
                IsDataLoaded = true;
                DataFilePath = modifiedFilePath;
            }
            else
            {
                IsDataLoaded = false;
            }

            // Retrieve the features (column names) from the Python script
            return FetchFeaturesFromPython();
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
            RemoveSpecialCharacters("predict_data");
            return FetchTestFeaturesFromPython();
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
        #endregion "Data File Manup"
        #region "Split Data"
        public Tuple<string, string> SplitDataClassFile(string urlpath, string filename, double splitRatio)
        {
            try
            {
                ValidateSplitRatio(ref splitRatio); // Ensuring split ratio is valid

                string dataFilePath = Path.Combine(urlpath, filename);


                if (!File.Exists(dataFilePath))
                {

                    return new Tuple<string, string>(null, null);
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
        public string[] SplitData(float testSize, string trainFilePath, string testFilePath)
        {
            if (!IsInitialized)
            {
                return null;
            }
            // Ensure testSize is more than 0.5 to make test set larger
            if (testSize <= 0.1)
            {
                throw new ArgumentException("Test size must be more than 10% of the data.");
            }
            string formattedFilePath = string.Empty; // dataFilePath.Replace("\\", "\\\\");
            string formattedtrainFilePath = trainFilePath.Replace("\\", "\\\\");
            string formattedtestFilePath = testFilePath.Replace("\\", "\\\\");
            string script = $@"
import pandas as pd
from sklearn.model_selection import train_test_split

# Load the dataset
#data = pd.read_csv('{formattedFilePath}')

# Split the dataset into training and testing sets
train_data, test_data = train_test_split(data, test_size={testSize})

# Save the split datasets to files
train_data.to_csv('{formattedtrainFilePath}', index = False)
test_data.to_csv('{formattedtestFilePath}', index = False)
test_features = test_data.columns.tolist()
globals()['train_data'] = train_data
globals()['test_data'] = test_data
globals()['data'] = data
";
            //            train_data.to_csv('{trainFilePath}', index = False)
            //test_data.to_csv('{testFilePath}', index = False)
            RunPythonScript(script, null);
            return FetchFeaturesFromPython();
        }
        public string[] SplitData(string dataFilePath, float testSize, string trainFilePath, string testFilePath)
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

            if (RunPythonScript(script, null))
            {
                IsDataLoaded = true;
                DataFilePath = formattedFilePath;
            }
            else
            {
                IsDataLoaded = false;
            }
            return FetchFeaturesFromPython();
        }
        public string[] SplitData(string dataFilePath, float testSize, float validationSize, string trainFilePath, string testFilePath, string validationFilePath)
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

            if (RunPythonScript(script, null))
            {
                IsDataLoaded = true;
                DataFilePath = formattedFilePath;
            }
            else
            {
                IsDataLoaded = false;
            }
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

            //            string script = $@"
            //import pandas as pd
            //from sklearn.model_selection import train_test_split

            //# Load the dataset
            //data = pd.read_csv('{formattedFilePath}')

            //# Split the dataset into training and test sets
            //train_data, test_data = train_test_split(data, test_size={testSize})

            //# Prepare test data (without label column)
            //test_data_without_label = test_data.drop(columns=['{labelColumn}'])

            //# Prepare validation data (only primary key and label)
            //validation_data = test_data[['{primaryFeatureKeyID}', '{labelColumn}']]

            //# Save the datasets to files
            //train_data.to_csv('{formattedTrainFilePath}', index=False)
            //test_data_without_label.to_csv('{formattedTestFilePath}', index=False)
            //validation_data.to_csv('{formattedValidationFilePath}', index=False)
            //test_features = test_data.columns.tolist()
            //features = data.columns.tolist()
            //";
            string script = $@"
import pandas as pd
import re
from sklearn.model_selection import train_test_split

def remove_special_characters_from_data(df):
    for col in df.columns:
        if df[col].dtype == 'object':  # Checking if the column is of string type
            # Apply the regex to each element in the column to remove special characters
          df[col] = df[col].apply(lambda X: re.sub(r""[^a-zA-Z0-9_]+"", '', str(X)))
    return df

# Function to fix column names (remove spaces and special characters)
def fix_column_names(df):
    return df.rename(columns=lambda X: X.strip().replace(' ', '').replace('/', '').replace('-', ''))

# Load the dataset
data = pd.read_csv('{formattedFilePath}', encoding='cp1252')
data = fix_column_names(data)

# Split the dataset into training and test sets
train_data, test_data = train_test_split(data, test_size={testSize})

# Prepare test data (without label column)
label_column_fixed = '{labelColumn}'.strip().replace(' ', '').replace('/', '').replace('-', '')
primary_feature_key_id_fixed = '{primaryFeatureKeyID}'.strip().replace(' ', '').replace('/', '').replace('-', '')
test_data_without_label = test_data.drop(columns=[label_column_fixed])

# Prepare validation data (only primary key and label)
validation_data = test_data[[primary_feature_key_id_fixed, label_column_fixed]]

train_data = remove_special_characters_from_data(train_data)
test_data_without_label = remove_special_characters_from_data(test_data_without_label)

# Save the datasets to files
train_data.to_csv('{formattedTrainFilePath}', index=False)
test_data_without_label.to_csv('{formattedTestFilePath}', index=False)
validation_data.to_csv('{formattedValidationFilePath}', index=False)
test_features = test_data_without_label.columns.tolist()
features = train_data.columns.tolist()
";


            if (RunPythonScript(script, null))
            {
                IsDataLoaded = true;
            }
            else
            {
                IsDataLoaded = false;
            }
            return FetchFeaturesFromPython();
        }

        #endregion "Split Data"
        #region "Predictions"
        public Tuple<double, double, double> GetModelRegressionScores(string modelId)
        {
            if (!IsInitialized)
            {
                return new Tuple<double, double, double>(-1, -1, -1);
            }

            // Script to prepare test data (X_test and y_test) similarly to how training data was prepared
            string prepareTestDataScript = @"
# Assuming test data is loaded and preprocessed similarly to training data
X_test = pd.get_dummies(test_data[test_features])
X_test.fillna(X_test.mean(), inplace=True)
y_test = test_data[label_column].astype(float)  # Ensure y_test is float for regression
# Align the test set columns with the training set
# This adds missing columns in the test set and sets them to zero
test_encoded = X_test.reindex(columns = X.columns, fill_value=0)
";

            RunPythonScript(prepareTestDataScript, null);
            string script = $@"
from sklearn.metrics import mean_squared_error, mean_absolute_error
import numpy as np
model = models['{modelId}']
predictions = model.predict(test_encoded)
mse = mean_squared_error(y_test, predictions)
rmse = np.sqrt(mse)
mae = mean_absolute_error(y_test, predictions)
# Store the scores in the Python persistent scope
globals()['mse'] = mse
globals()['rmse'] = rmse
globals()['mae'] = mae

";

            RunPythonScript(script, null);

            // Retrieve the scores from the Python script
            double mse = FetchMSEFromPython();
            double rmse = FetchRMSEFromPython();
            double mae = FetchMAEFromPython();

            return new Tuple<double, double, double>(mse, rmse, mae);
        }
        public Tuple<double, double> GetModelClassificationScore(string modelId)
        {
            if (!IsInitialized)
            {
                return new Tuple<double, double>(-1, -1);
            }

            // Script to prepare test data (X_test and y_test) similarly to how training data was prepared
            string prepareTestDataScript = @"
# Assuming test data is loaded and preprocessed similarly to training data
X_test = pd.get_dummies(test_data[test_features])
X_test.fillna(X_test.mean(), inplace=True)
y_test = test_data[label_column]

# Align the test set columns with the training set columns
X_test = X_test.reindex(columns=X.columns, fill_value=0)
";

            RunPythonScript(prepareTestDataScript, null);
            string script = $@"
from sklearn.metrics import accuracy_score
from sklearn.metrics import f1_score
model = models['{modelId}']
predictions = model.predict(X_test)
accuracy = accuracy_score(y_test, predictions)
score = f1_score(y_test, predictions)
# Store the score and accuracy in the Python persistent scope
globals()['score'] = score
globals()['accuracy'] = accuracy
";

            RunPythonScript(script, null);

            // Retrieve the score from the Python script
            return new Tuple<double, double>(FetchScoreFromPython(), FetchAccuracyFromPython());
        }
        public dynamic PredictClassification(string[] training_columns)
        {
            if (!IsInitialized)
            {
                return null;
            }
            string trainingCols = String.Join(", ", training_columns.Select(col => $"'{col}'"));
            //   string inputAsString = inputData.ToString(); // Convert inputData to a string representation
            string script = $@"
X_predict = pd.get_dummies(predict_data[features])

X_predict.fillna(X_predict.mean(), inplace=True)  # Simple mean imputation for missing values
# Align the columns of X_predict to match the training data

predictions = model.predict(X_predict)
";

            RunPythonScript(script, null);

            // Retrieve predictions from Python script
            dynamic predictions = FetchPredictionsFromPython(); // Use the method to fetch predictions
            return predictions;
        }
        public dynamic PredictRegression(string[] training_columns)
        {
            if (!IsInitialized)
            {
                return null;
            }
            string trainingCols = String.Join(", ", training_columns.Select(col => $"'{col}'"));
            // Prepare the script to predict and round off the predictions
            string script = $@"
import numpy as np
X_predict = pd.get_dummies(predict_data[features])

X_predict.fillna(X_predict.mean(), inplace=True)  # Simple mean imputation for missing values
# Align the columns of X_predict to match the training data

predictions = model.predict(X_predict)

# Round predictions to the nearest integer
rounded_predictions = np.rint(predictions)
predictions=rounded_predictions


";

            // Execute the Python script
            RunPythonScript(script, null);

            // Retrieve rounded predictions from Python script
            dynamic rounded_predictions = FetchPredictionsFromPython(); // Make sure this method can handle the rounded predictions
            return rounded_predictions;
        }
        #endregion "Predictions"
        #region "Model Methods"
        public void TrainModel(string modelId, MachineLearningAlgorithm algorithm, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn)
        {
            if (!IsInitialized)
            {
                return;
            }
            string algorithmName = Enum.GetName(typeof(MachineLearningAlgorithm), algorithm);
            string features = string.Join(", ", featureColumns.Select(fc => @$"'{fc}'"));
            string paramsDict = String.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value.ToString()}"));
            bool isSupervised = algorithmSupervision[algorithmName];
            string isSupervisedPythonLiteral = isSupervised ? "True" : "False";
            string importStatement;
            switch (algorithmName)
            {
                case "HistGradientBoostingRegressor":
                    importStatement = "from sklearn.experimental import enable_hist_gradient_boosting"; // Ensure experimental module is imported
                    importStatement += "\nfrom sklearn.ensemble import HistGradientBoostingRegressor";
                    break;

                case "HistGradientBoostingClassifier":
                    importStatement = "from sklearn.experimental import enable_hist_gradient_boosting"; // Ensure experimental module is imported
                    importStatement += "\nfrom sklearn.ensemble import HistGradientBoostingClassifier";
                    break;
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
                case "LinearRegression":
                    importStatement = "from sklearn.linear_model import LinearRegression";
                    break;
                case "LassoRegression":
                    importStatement = "from sklearn.linear_model import Lasso";
                    break;
                case "RidgeRegression":
                    importStatement = "from sklearn.linear_model import Ridge";
                    break;
                case "ElasticNet":
                    importStatement = "from sklearn.linear_model import ElasticNet";
                    break;
                case "KMeans":
                    importStatement = "from sklearn.cluster import KMeans";
                    break;
                case "DBSCAN":
                    importStatement = "from sklearn.cluster import DBSCAN";
                    break;
                case "AgglomerativeClustering":
                    importStatement = "from sklearn.cluster import AgglomerativeClustering";
                    break;
                // Classification Algorithms
                case "GaussianNB":
                    importStatement = "from sklearn.naive_bayes import GaussianNB";
                    break;
                case "MultinomialNB":
                    importStatement = "from sklearn.naive_bayes import MultinomialNB";
                    break;
                case "BernoulliNB":
                    importStatement = "from sklearn.naive_bayes import BernoulliNB";
                    break;
                case "AdaBoostClassifier":
                    importStatement = "from sklearn.ensemble import AdaBoostClassifier";
                    break;
                default:
                    throw new ArgumentException($"Unsupported algorithm: {algorithmName}");
            }
            string script = $@"
{importStatement}
features = [{features}]
label_column ='{labelColumn}'

# If the model exists, retrieve it; otherwise, create it
if '{modelId}' in models:
    # Retrieve the existing model from the dictionary
    model = models['{modelId}']
else:
    # Create a new model instance using the specified algorithm
    model = {algorithmName}({paramsDict})

# Train or re-train the model
X = pd.get_dummies(train_data[features])
X.fillna(X.mean(), inplace=True)  # Simple mean imputation for missing values

if {isSupervisedPythonLiteral}:
    Y = train_data[label_column].fillna(train_data[label_column].mean())  # Impute missing labels
    model.fit(X, Y)
else:
    model.fit(X)

# Store or update the model in the dictionary under the same modelId
models['{modelId}'] = model
";


            RunPythonScript(script, null);
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
                dynamic pyModelId = PythonRuntime.CurrentPersistentScope.Get("model_id");
                if (pyModelId == null || pyModelId.ToString() == "None")
                {
                    return null; // or handle the error as per your application's needs
                }
            }

            // Return the model ID to the caller
            return modelId;
        }
        #endregion "Model Methods"
        #region "Helper Methods"
        public string[] GetFeatures(string filePath)
        {
            if (!IsInitialized)
            {
                return null;
            }

            // Convert the file path to a raw string format for Python
            string formattedFilePath = filePath.Replace("\\", "\\\\");

            // Python script to load the data and extract feature names
            string script = $@"
import pandas as pd

# Read only the first chunk to get column names
chunk_iter = pd.read_csv('{formattedFilePath}', chunksize=1)
chunk = next(chunk_iter)

# Extract column names from the first chunk
features = chunk.columns.tolist()
";

            // Execute the Python script
            RunPythonScript(script, null);

            // Retrieve the features from Python
            return FetchFeaturesFromPython();
        }
        public bool RemoveSpecialCharacters(string dataFrameName)
        {
            if (!IsInitialized)
            {
                return false;
            }

            string script = $@"
import pandas as pd
import re

def remove_special_characters_from_data(df):
    for col in df.columns:
        if df[col].dtype == 'object':  # Checking if the column is of string type
            # Apply the regex to each element in the column to remove special characters
          df[col] = df[col].apply(lambda X: re.sub(r""[^a-zA-Z0-9_]+"", '', str(X)))
    return df

# Apply the function to the DataFrame
{dataFrameName} = remove_special_characters_from_data({dataFrameName})
";

            RunPythonScript(script, null);
            return true;
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
        public void AddLabelColumnIfMissing(string labelColumn)
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
        public void HandleMultiValueCategoricalFeatures(string[] multiValueFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string multiValueFeaturesList = string.Join(", ", multiValueFeatures.Select(f => $"'{f}'"));

            string script = $@"
import pandas as pd

def handle_multi_value_categorical_features(data, feature_list):
    for feature in feature_list:
        # Split the multi-value feature into individual values
        split_features = data[feature].str.split(',', expand=True)
        
        # Get unique values across the entire column to create dummy variables
        unique_values = pd.unique(split_features.values.ravel('K'))
        unique_values = [val for val in unique_values if val is not None]
        
        # For each unique value, create a binary column
        for value in unique_values:
            if value is not None and value != '':
                data[f'{{feature}}_{{value}}'] = split_features.apply(lambda row: int(value in row.values), axis=1)
        
        # Drop the original multi-value feature column
        data = data.drop(columns=[feature])
    
    return data

# List of features with multiple values
multi_value_features = [{multiValueFeaturesList}]

# Process the multi-value features for train_data, test_data, and predict_data if they exist
if 'train_data' in globals():
    train_data = handle_multi_value_categorical_features(train_data, multi_value_features)
    globals()['train_data'] = train_data

if 'test_data' in globals():
    test_data = handle_multi_value_categorical_features(test_data, multi_value_features)
    globals()['test_data'] = test_data

if 'predict_data' in globals():
    predict_data = handle_multi_value_categorical_features(predict_data, multi_value_features)
    globals()['predict_data'] = predict_data
";

            RunPythonScript(script, null);
        }




        public void HandleCategoricalDataEncoder(string[] categoricalFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string categoricalFeaturesList = string.Join(", ", categoricalFeatures.Select(f => $"'{f}'"));

            string script = $@"
import pandas as pd
from sklearn.preprocessing import OneHotEncoder

# Categorical features to encode
categorical_features = [{categoricalFeaturesList}]

# Select categorical columns and apply OneHotEncoder
if 'train_data' in globals():
    encoder = OneHotEncoder(handle_unknown='ignore', sparse=False)
    X_encoded = encoder.fit_transform(train_data[categorical_features])

    # Convert to DataFrame and reassign to train_data
    encoded_columns = encoder.get_feature_names_out(categorical_features)
    train_data_encoded = pd.DataFrame(X_encoded, columns=encoded_columns)
    train_data = pd.concat([train_data.drop(columns=categorical_features), train_data_encoded], axis=1)
    globals()['train_data'] = train_data

if 'test_data' in globals():
    X_encoded = encoder.transform(test_data[categorical_features])

    # Convert to DataFrame and reassign to test_data
    test_data_encoded = pd.DataFrame(X_encoded, columns=encoded_columns)
    test_data = pd.concat([test_data.drop(columns=categorical_features), test_data_encoded], axis=1)
    globals()['test_data'] = test_data
";

            RunPythonScript(script, null);
        }
        public void HandleDateData(string[] dateFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            // Convert the array of date features to a Python list format
            string dateFeaturesList = string.Join(", ", dateFeatures.Select(f => $"'{f}'"));

            // Python script to handle date features
            string script = $@"
import pandas as pd

# List of date features
date_features = [{dateFeaturesList}]

# Ensure all date features are in datetime format
for feature in date_features:
    data[feature] = pd.to_datetime(data[feature], errors='coerce')

# Optionally, extract components like year, month, day
for feature in date_features:
    data[feature + '_year'] = data[feature].dt.year
    data[feature + '_month'] = data[feature].dt.month
    data[feature + '_day'] = data[feature].dt.day

# Or convert to timestamp
for feature in date_features:
    data[feature + '_timestamp'] = data[feature].apply(lambda X: X.timestamp() if pd.notnull(X) else None)

# Optionally, drop the original date columns
data.drop(columns=date_features, inplace=True)
";

            // Execute the Python script
            RunPythonScript(script, null);
        }
        public string[] GetCategoricalFeatures(string[] selectedFeatures)
        {
            if (!IsInitialized)
            {
                return null;
            }

            // Convert the array of selected features to a Python list format
            string selectedFeaturesList = string.Join(", ", selectedFeatures.Select(f => $"'{f}'"));

            // Python script to identify categorical features
            string script = $@"
import pandas as pd

# List of selected features
selected_features = [{selectedFeaturesList}]

# Get the dtypes of the selected features
dtypes = data[selected_features].dtypes

# Filter and get the categorical features
categorical_features = dtypes[dtypes == 'object'].index.tolist()
";

            // Execute the Python script
            RunPythonScript(script, null);

            // Fetch and return the categorical features
            return FetchCategoricalFeaturesFromPython();
        }
        private string[] FetchCategoricalFeaturesFromPython()
        {
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                dynamic pyCategoricalFeatures = PythonRuntime.CurrentPersistentScope.Get("categorical_features");
                if (pyCategoricalFeatures == null) return new string[0];

                // Convert the Python list to a C# string array
                var categoricalFeaturesList = new List<string>();
                foreach (var feature in pyCategoricalFeatures)
                {
                    categoricalFeaturesList.Add(feature.ToString());
                }
                return categoricalFeaturesList.ToArray();
            }
        }
        public Tuple<string[], string[]> GetCategoricalAndDateFeatures(string[] selectedFeatures)
        {
            if (!IsInitialized)
            {
                return null;
            }

            // Convert the array of selected features to a Python list format
            string selectedFeaturesList = string.Join(", ", selectedFeatures.Select(f => $"'{f}'"));

            // Python script to identify categorical and date features
            string script = $@"
import pandas as pd

# List of selected features
selected_features = [{selectedFeaturesList}]

# Get the dtypes of the selected features
dtypes = data[selected_features].dtypes

# Filter and get the categorical features
categorical_features = dtypes[dtypes == 'object'].index.tolist()

# Filter and get the date features (datetime64 or object with date parsing required)
date_features = dtypes[dtypes == 'datetime64[ns]'].index.tolist()


# Ensure the lists are initialized even if empty
if 'categorical_features' not in globals():
    categorical_features = []

if 'date_features' not in globals():
    date_features = []

# Optionally, you might want to include object types that could be dates:
# for col in dtypes[dtypes == 'object'].index:
#     if pd.to_datetime(data[col], errors='coerce').notnull().all():
#         date_features.append(col)
";

            // Execute the Python script
            RunPythonScript(script, null);

            // Fetch and return the categorical and date features
            string[] categoricalFeatures = FetchCategoricalFeaturesFromPython();
            string[] dateFeatures = FetchDateFeaturesFromPython();

            return new Tuple<string[], string[]>(categoricalFeatures, dateFeatures);
        }
        private string[] FetchDateFeaturesFromPython()
        {
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                dynamic pyDateFeatures = PythonRuntime.CurrentPersistentScope.Get("date_features");
                if (pyDateFeatures == null) return new string[0];

                // Convert the Python list to a C# string array
                var dateFeaturesList = new List<string>();
                foreach (var feature in pyDateFeatures)
                {
                    dateFeaturesList.Add(feature.ToString());
                }
                return dateFeaturesList.ToArray();
            }
        }
        public void NormalizeData(string norm = "l2")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.preprocessing import Normalizer

def normalize_data(df, norm='l2'):
    normalizer = Normalizer(norm=norm)
    return pd.DataFrame(normalizer.fit_transform(df), columns=df.columns)

if 'train_data' in globals():
    train_data = normalize_data(train_data, '{norm}')
if 'test_data' in globals():
    test_data = normalize_data(test_data, '{norm}')
if 'data' in globals():
    data = normalize_data(data, '{norm}')
";

            RunPythonScript(script, null);
        }
        public void RobustScaleData()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.preprocessing import RobustScaler

def robust_scale_data(df):
    scaler = RobustScaler()
    return pd.DataFrame(scaler.fit_transform(df), columns=df.columns)

if 'train_data' in globals():
    train_data = robust_scale_data(train_data)
if 'test_data' in globals():
    test_data = robust_scale_data(test_data)
if 'data' in globals():
    data = robust_scale_data(data)
";

            RunPythonScript(script, null);
        }
        public void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.preprocessing import MinMaxScaler

def min_max_scale_data(df, feature_range=(0, 1)):
    scaler = MinMaxScaler(feature_range=feature_range)
    return pd.DataFrame(scaler.fit_transform(df), columns=df.columns)

if 'train_data' in globals():
    train_data = min_max_scale_data(train_data, feature_range=({featureRangeMin}, {featureRangeMax}))
if 'test_data' in globals():
    test_data = min_max_scale_data(test_data, feature_range=({featureRangeMin}, {featureRangeMax}))
if 'data' in globals():
    data = min_max_scale_data(data, feature_range=({featureRangeMin}, {featureRangeMax}))
";

            RunPythonScript(script, null);
        }
        public void StandardizeData()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.preprocessing import StandardScaler

def standardize_data(df):
    scaler = StandardScaler()
    return pd.DataFrame(scaler.fit_transform(df), columns=df.columns)

if 'train_data' in globals():
    train_data = standardize_data(train_data)
if 'test_data' in globals():
    test_data = standardize_data(test_data)
if 'data' in globals():
    data = standardize_data(data)
";

            RunPythonScript(script, null);
        }
        public void DropMissingValues(string axis = "rows")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
import pandas as pd

def drop_missing_values(df, axis='rows'):
    if axis == 'rows':
        return df.dropna()
    elif axis == 'columns':
        return df.dropna(axis=1)
    else:
        raise ValueError('Unknown axis: ' + axis)

if 'train_data' in globals():
    train_data = drop_missing_values(train_data, '{axis}')
if 'test_data' in globals():
    test_data = drop_missing_values(test_data, '{axis}')
if 'data' in globals():
    data = drop_missing_values(data, '{axis}')
";

            RunPythonScript(script, null);
        }
        public void ImputeMissingValuesWithCustomValue(object customValue)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string customValueStr = customValue is string ? $"'{customValue}'" : customValue.ToString();

            string script = $@"
import pandas as pd

def impute_with_custom_value(df, value):
    return df.fillna(value)

if 'train_data' in globals():
    train_data = impute_with_custom_value(train_data, {customValueStr})
if 'test_data' in globals():
    test_data = impute_with_custom_value(test_data, {customValueStr})
if 'data' in globals():
    data = impute_with_custom_value(data, {customValueStr})
";

            RunPythonScript(script, null);
        }

        public void ImputeMissingValuesWithFill(string method = "ffill")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
import pandas as pd

def fill_missing_values(df, method='ffill'):
    return df.fillna(method=method)

if 'train_data' in globals():
    train_data = fill_missing_values(train_data, '{method}')
if 'test_data' in globals():
    test_data = fill_missing_values(test_data, '{method}')
if 'data' in globals():
    data = fill_missing_values(data, '{method}')
";

            RunPythonScript(script, null);
        }

        public void ImputeMissingValues(string strategy = "mean")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
import pandas as pd

def impute_missing_values(df, strategy='mean'):
    if strategy == 'mean':
        return df.fillna(df.mean())
    elif strategy == 'median':
        return df.fillna(df.median())
    elif strategy == 'mode':
        return df.fillna(df.mode().iloc[0])
    else:
        raise ValueError('Unknown strategy: ' + strategy)

if 'train_data' in globals():
    train_data = impute_missing_values(train_data, '{strategy}')
if 'test_data' in globals():
    test_data = impute_missing_values(test_data, '{strategy}')
if 'data' in globals():
    data = impute_missing_values(data, '{strategy}')
";

            RunPythonScript(script, null);
        }
        public void StandardizeData(string[] selectedFeatures = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            // Convert the array of selected features to a Python list format
            string selectedFeaturesList = selectedFeatures != null ?
                string.Join(", ", selectedFeatures.Select(f => $"'{f}'")) : "None";

            string script = $@"
from sklearn.preprocessing import StandardScaler

def standardize_data(df, selected_features=None):
    if selected_features is None:
        # If no features are selected, standardize all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        # Ensure only the selected numerical features are standardized
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    scaler = StandardScaler()
    df[selected_features] = scaler.fit_transform(df[selected_features])
    return df

if 'train_data' in globals():
    train_data = standardize_data(train_data, [{selectedFeaturesList}])
if 'test_data' in globals():
    test_data = standardize_data(test_data, [{selectedFeaturesList}])
if 'data' in globals():
    data = standardize_data(data, [{selectedFeaturesList}])
";

            RunPythonScript(script, null);
        }
        public void MinMaxScaleData(double featureRangeMin = 0.0, double featureRangeMax = 1.0, string[] selectedFeatures = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            // Convert the array of selected features to a Python list format
            string selectedFeaturesList = selectedFeatures != null ?
                string.Join(", ", selectedFeatures.Select(f => $"'{f}'")) : "None";

            string script = $@"
from sklearn.preprocessing import MinMaxScaler

def min_max_scale_data(df, feature_range=(0, 1), selected_features=None):
    if selected_features is None:
        # If no features are selected, scale all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        # Ensure only the selected numerical features are scaled
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    scaler = MinMaxScaler(feature_range=feature_range)
    df[selected_features] = scaler.fit_transform(df[selected_features])
    return df

if 'train_data' in globals():
    train_data = min_max_scale_data(train_data, feature_range=({featureRangeMin}, {featureRangeMax}), selected_features=[{selectedFeaturesList}])
if 'test_data' in globals():
    test_data = min_max_scale_data(test_data, feature_range=({featureRangeMin}, {featureRangeMax}), selected_features=[{selectedFeaturesList}])
if 'data' in globals():
    data = min_max_scale_data(data, feature_range=({featureRangeMin}, {featureRangeMax}), selected_features=[{selectedFeaturesList}])
";

            RunPythonScript(script, null);
        }
        public void RobustScaleData(string[] selectedFeatures = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            // Convert the array of selected features to a Python list format
            string selectedFeaturesList = selectedFeatures != null ?
                string.Join(", ", selectedFeatures.Select(f => $"'{f}'")) : "None";

            string script = $@"
from sklearn.preprocessing import RobustScaler

def robust_scale_data(df, selected_features=None):
    if selected_features is None:
        # If no features are selected, scale all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        # Ensure only the selected numerical features are scaled
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    scaler = RobustScaler()
    df[selected_features] = scaler.fit_transform(df[selected_features])
    return df

if 'train_data' in globals():
    train_data = robust_scale_data(train_data, [{selectedFeaturesList}])
if 'test_data' in globals():
    test_data = robust_scale_data(test_data, [{selectedFeaturesList}])
if 'data' in globals():
    data = robust_scale_data(data, [{selectedFeaturesList}])
";

            RunPythonScript(script, null);
        }
        public void NormalizeData(string norm = "l2", string[] selectedFeatures = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            // Convert the array of selected features to a Python list format
            string selectedFeaturesList = selectedFeatures != null ?
                string.Join(", ", selectedFeatures.Select(f => $"'{f}'")) : "None";

            string script = $@"
from sklearn.preprocessing import Normalizer

def normalize_data(df, norm='l2', selected_features=None):
    if selected_features is None:
        # If no features are selected, normalize all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        # Ensure only the selected numerical features are normalized
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    normalizer = Normalizer(norm=norm)
    df[selected_features] = normalizer.fit_transform(df[selected_features])
    return df

if 'train_data' in globals():
    train_data = normalize_data(train_data, '{norm}', selected_features=[{selectedFeaturesList}])
if 'test_data' in globals():
    test_data = normalize_data(test_data, '{norm}', selected_features=[{selectedFeaturesList}])
if 'data' in globals():
    data = normalize_data(data, '{norm}', selected_features=[{selectedFeaturesList}])
";

            RunPythonScript(script, null);
        }

        #endregion "Helper Methods"
        #region "Feature Engineering"
        public void GeneratePolynomialFeatures(string[] selectedFeatures = null, int degree = 2, bool includeBias = true, bool interactionOnly = false)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string selectedFeaturesList = selectedFeatures != null ?
                string.Join(", ", selectedFeatures.Select(f => $"'{f}'")) : "None";

            string script = $@"
from sklearn.preprocessing import PolynomialFeatures

def generate_polynomial_features(df, selected_features=None, degree=2, include_bias=True, interaction_only=False):
    if selected_features is None:
        # If no features are selected, use all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    poly = PolynomialFeatures(degree=degree, include_bias=include_bias, interaction_only=interaction_only)
    poly_features = poly.fit_transform(df[selected_features])
    poly_feature_names = poly.get_feature_names_out(selected_features)
    
    df_poly = pd.DataFrame(poly_features, columns=poly_feature_names)
    
    # Drop the original features and add the polynomial features
    df = df.drop(columns=selected_features)
    df = pd.concat([df, df_poly], axis=1)
    return df

if 'train_data' in globals():
    train_data = generate_polynomial_features(train_data, [{selectedFeaturesList}], degree={degree}, include_bias={includeBias.ToString().ToLower()}, interaction_only={interactionOnly.ToString().ToLower()})
if 'test_data' in globals():
    test_data = generate_polynomial_features(test_data, [{selectedFeaturesList}], degree={degree}, include_bias={includeBias.ToString().ToLower()}, interaction_only={interactionOnly.ToString().ToLower()})
if 'data' in globals():
    data = generate_polynomial_features(data, [{selectedFeaturesList}], degree={degree}, include_bias={includeBias.ToString().ToLower()}, interaction_only={interactionOnly.ToString().ToLower()})
";

            RunPythonScript(script, null);
        }
        public void ApplyLogTransformation(string[] selectedFeatures = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string selectedFeaturesList = selectedFeatures != null ?
                string.Join(", ", selectedFeatures.Select(f => $"'{f}'")) : "None";

            string script = $@"
import numpy as np

def apply_log_transformation(df, selected_features=None):
    if selected_features is None:
        # If no features are selected, apply to all numerical features
        numerical_features = df.select_dtypes(include=['number']).columns
        selected_features = numerical_features
    else:
        selected_features = [f for f in selected_features if f in df.select_dtypes(include=['number']).columns]
    
    df[selected_features] = df[selected_features].apply(np.log1p)  # log1p to handle log(0)
    return df

if 'train_data' in globals():
    train_data = apply_log_transformation(train_data, [{selectedFeaturesList}])
if 'test_data' in globals():
    test_data = apply_log_transformation(test_data, [{selectedFeaturesList}])
if 'data' in globals():
    data = apply_log_transformation(data, [{selectedFeaturesList}])
";

            RunPythonScript(script, null);
        }
        public void ApplyBinning(string[] selectedFeatures, int numberOfBins = 5, bool encodeAsOrdinal = true)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string selectedFeaturesList = string.Join(", ", selectedFeatures.Select(f => $"'{f}'"));

            string script = $@"
import pandas as pd
import numpy as np

def apply_binning(df, selected_features, number_of_bins=5, encode_as_ordinal=True):
    for feature in selected_features:
        if encode_as_ordinal:
            df[feature + '_binned'] = pd.cut(df[feature], bins=number_of_bins, labels=False)
        else:
            df[feature + '_binned'] = pd.cut(df[feature], bins=number_of_bins)
    return df

if 'train_data' in globals():
    train_data = apply_binning(train_data, [{selectedFeaturesList}], number_of_bins={numberOfBins}, encode_as_ordinal={encodeAsOrdinal.ToString().ToLower()})
if 'test_data' in globals():
    test_data = apply_binning(test_data, [{selectedFeaturesList}], number_of_bins={numberOfBins}, encode_as_ordinal={encodeAsOrdinal.ToString().ToLower()})
if 'data' in globals():
    data = apply_binning(data, [{selectedFeaturesList}], number_of_bins={numberOfBins}, encode_as_ordinal={encodeAsOrdinal.ToString().ToLower()})
";

            RunPythonScript(script, null);
        }
        public void ApplyFeatureHashing(string[] selectedFeatures, int nFeatures = 10)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string selectedFeaturesList = string.Join(", ", selectedFeatures.Select(f => $"'{f}'"));

            string script = $@"
from sklearn.feature_extraction import FeatureHasher

def apply_feature_hashing(df, selected_features, n_features=10):
    hasher = FeatureHasher(input_type='string', n_features=n_features)
    hashed_features = hasher.fit_transform(df[selected_features].astype(str).values)
    hashed_df = pd.DataFrame(hashed_features.toarray(), index=df.index)
    
    # Drop original features and add hashed features
    df = df.drop(columns=selected_features)
    df = pd.concat([df, hashed_df], axis=1)
    return df

if 'train_data' in globals():
    train_data = apply_feature_hashing(train_data, [{selectedFeaturesList}], n_features={nFeatures})
if 'test_data' in globals():
    test_data = apply_feature_hashing(test_data, [{selectedFeaturesList}], n_features={nFeatures})
if 'data' in globals():
    data = apply_feature_hashing(data, [{selectedFeaturesList}], n_features={nFeatures})
";

            RunPythonScript(script, null);
        }


        #endregion "Feature Engineering"
        #region "Handling Imbalanced Data"
        public void ApplyRandomOversampling(string targetColumn, float samplingStrategy = 1.0f)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from imblearn.over_sampling import RandomOverSampler
import pandas as pd

def apply_random_oversampling(df, target_column, sampling_strategy=1.0):
    ros = RandomOverSampler(sampling_strategy=sampling_strategy, random_state=42)
    X_res, y_res = ros.fit_resample(df.drop(columns=[target_column]), df[target_column])
    return pd.concat([X_res, y_res], axis=1)

if 'train_data' in globals():
    train_data = apply_random_oversampling(train_data, '{targetColumn}', sampling_strategy={samplingStrategy})
";

            RunPythonScript(script, null);
        }

        public void ApplyRandomUndersampling(string targetColumn, float samplingStrategy = 0.5f)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from imblearn.under_sampling import RandomUnderSampler
import pandas as pd

def apply_random_undersampling(df, target_column, sampling_strategy=0.5):
    rus = RandomUnderSampler(sampling_strategy=sampling_strategy, random_state=42)
    X_res, y_res = rus.fit_resample(df.drop(columns=[target_column]), df[target_column])
    return pd.concat([X_res, y_res], axis=1)

if 'train_data' in globals():
    train_data = apply_random_undersampling(train_data, '{targetColumn}', sampling_strategy={samplingStrategy})
";

            RunPythonScript(script, null);
        }
        public void ApplySMOTE(string targetColumn, float samplingStrategy = 1.0f)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from imblearn.over_sampling import SMOTE
import pandas as pd

def apply_smote(df, target_column, sampling_strategy=1.0):
    smote = SMOTE(sampling_strategy=sampling_strategy, random_state=42)
    X_res, y_res = smote.fit_resample(df.drop(columns=[target_column]), df[target_column])
    return pd.concat([X_res, y_res], axis=1)

if 'train_data' in globals():
    train_data = apply_smote(train_data, '{targetColumn}', sampling_strategy={samplingStrategy})
";

            RunPythonScript(script, null);
        }
        public void ApplyNearMiss(string targetColumn, int version = 1)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from imblearn.under_sampling import NearMiss
import pandas as pd

def apply_nearmiss(df, target_column, Version=1):
    nearmiss = NearMiss(Version=Version)
    X_res, y_res = nearmiss.fit_resample(df.drop(columns=[target_column]), df[target_column])
    return pd.concat([X_res, y_res], axis=1)

if 'train_data' in globals():
    train_data = apply_nearmiss(train_data, '{targetColumn}', Version={version})
";

            RunPythonScript(script, null);
        }
        public void ApplyBalancedRandomForest(string targetColumn, int nEstimators = 100)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from imblearn.ensemble import BalancedRandomForestClassifier

def apply_balanced_random_forest(df, target_column, n_estimators=100):
    X = pd.get_dummies(df.drop(columns=[target_column]))
    Y = df[target_column]
    model = BalancedRandomForestClassifier(n_estimators=n_estimators, random_state=42)
    model.fit(X, Y)
    return model

if 'train_data' in globals():
    model = apply_balanced_random_forest(train_data, '{targetColumn}', n_estimators={nEstimators})
    models['BalancedRandomForest_{targetColumn}'] = model
";

            RunPythonScript(script, null);
        }
        public void AdjustClassWeights(string modelId, string algorithmName, Dictionary<string, object> parameters, string[] featureColumns, string labelColumn)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string features = string.Join(", ", featureColumns.Select(fc => @$"'{fc}'"));
            string paramsDict = string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
            string script = $@"
from sklearn.utils.class_weight import compute_class_weight

X = pd.get_dummies(train_data[{features}])
Y = train_data[{labelColumn}]

# Compute class weights
class_weights = compute_class_weight('balanced', classes=np.unique(Y), Y=Y)
class_weight_dict = dict(zip(np.unique(Y), class_weights))

# Include class weights in the model parameters
model = {algorithmName}({paramsDict}, class_weight=class_weight_dict)
model.fit(X, Y)

# Store the model
models['{modelId}'] = model
";

            RunPythonScript(script, null);
        }



        #endregion "Handling Imbalanced Data"
        #region "Text Data Preprocessing"
        public void ConvertTextToLowercase(string columnName)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
# Convert text to lowercase
if '{columnName}' in train_data.columns:
    train_data['{columnName}'] = train_data['{columnName}'].str.lower()

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    test_data['{columnName}'] = test_data['{columnName}'].str.lower()
";

            RunPythonScript(script, null);
        }
        public void RemovePunctuation(string columnName)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
import string

# Remove punctuation from text
if '{columnName}' in train_data.columns:
    train_data['{columnName}'] = train_data['{columnName}'].str.translate(str.maketrans('', '', string.punctuation))

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    test_data['{columnName}'] = test_data['{columnName}'].str.translate(str.maketrans('', '', string.punctuation))
";

            RunPythonScript(script, null);
        }
        public void RemoveStopwords(string columnName, string language = "english")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.feature_extraction.text import ENGLISH_STOP_WORDS

# Remove stopwords from text
stopwords = list(ENGLISH_STOP_WORDS)

if '{columnName}' in train_data.columns:
    train_data['{columnName}'] = train_data['{columnName}'].apply(lambda X: ' '.join([word for word in str(X).split() if word not in stopwords]))

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    test_data['{columnName}'] = test_data['{columnName}'].apply(lambda X: ' '.join([word for word in str(X).split() if word not in stopwords]))
";

            RunPythonScript(script, null);
        }
        public void ApplyStemming(string columnName)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from nltk.stem import PorterStemmer

stemmer = PorterStemmer()

# Apply stemming to text
if '{columnName}' in train_data.columns:
    train_data['{columnName}'] = train_data['{columnName}'].apply(lambda X: ' '.join([stemmer.stem(word) for word in str(X).split()]))

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    test_data['{columnName}'] = test_data['{columnName}'].apply(lambda X: ' '.join([stemmer.stem(word) for word in str(X).split()]))
";

            RunPythonScript(script, null);
        }
        public void ApplyLemmatization(string columnName)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from nltk.stem import WordNetLemmatizer

lemmatizer = WordNetLemmatizer()

# Apply lemmatization to text
if '{columnName}' in train_data.columns:
    train_data['{columnName}'] = train_data['{columnName}'].apply(lambda X: ' '.join([lemmatizer.lemmatize(word) for word in str(X).split()]))

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    test_data['{columnName}'] = test_data['{columnName}'].apply(lambda X: ' '.join([lemmatizer.lemmatize(word) for word in str(X).split()]))
";

            RunPythonScript(script, null);
        }
        public void ApplyTokenization(string columnName)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from nltk.tokenize import word_tokenize

# Apply tokenization to text
if '{columnName}' in train_data.columns:
    train_data['{columnName}'] = train_data['{columnName}'].apply(lambda X: word_tokenize(str(X)))

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    test_data['{columnName}'] = test_data['{columnName}'].apply(lambda X: word_tokenize(str(X)))
";

            RunPythonScript(script, null);
        }
        public void ApplyTFIDFVectorization(string columnName, int maxFeatures = 1000)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.feature_extraction.text import TfidfVectorizer

# Apply TF-IDF vectorization
vectorizer = TfidfVectorizer(max_features={maxFeatures})

if '{columnName}' in train_data.columns:
    X_train_tfidf = vectorizer.fit_transform(train_data['{columnName}'])
    train_data_tfidf = pd.DataFrame(X_train_tfidf.toarray(), columns=vectorizer.get_feature_names_out())
    train_data = pd.concat([train_data.drop(columns=['{columnName}']), train_data_tfidf], axis=1)

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    X_test_tfidf = vectorizer.transform(test_data['{columnName}'])
    test_data_tfidf = pd.DataFrame(X_test_tfidf.toarray(), columns=vectorizer.get_feature_names_out())
    test_data = pd.concat([test_data.drop(columns=['{columnName}']), test_data_tfidf], axis=1)
";

            RunPythonScript(script, null);
        }


        #endregion "Text Data Preprocessing"
        #region "Date/Time Features"
        public void ExtractDateTimeComponents(string columnName)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
# Extract components from the date/time column
if '{columnName}' in train_data.columns:
    train_data['{columnName}_year'] = pd.to_datetime(train_data['{columnName}']).dt.year
    train_data['{columnName}_month'] = pd.to_datetime(train_data['{columnName}']).dt.month
    train_data['{columnName}_day'] = pd.to_datetime(train_data['{columnName}']).dt.day
    train_data['{columnName}_hour'] = pd.to_datetime(train_data['{columnName}']).dt.hour
    train_data['{columnName}_minute'] = pd.to_datetime(train_data['{columnName}']).dt.minute
    train_data['{columnName}_dayofweek'] = pd.to_datetime(train_data['{columnName}']).dt.dayofweek

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    test_data['{columnName}_year'] = pd.to_datetime(test_data['{columnName}']).dt.year
    test_data['{columnName}_month'] = pd.to_datetime(test_data['{columnName}']).dt.month
    test_data['{columnName}_day'] = pd.to_datetime(test_data['{columnName}']).dt.day
    test_data['{columnName}_hour'] = pd.to_datetime(test_data['{columnName}']).dt.hour
    test_data['{columnName}_minute'] = pd.to_datetime(test_data['{columnName}']).dt.minute
    test_data['{columnName}_dayofweek'] = pd.to_datetime(test_data['{columnName}']).dt.dayofweek
";

            RunPythonScript(script, null);
        }
        public void CalculateTimeDifference(string startColumn, string endColumn, string newColumnName)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
# Calculate time difference between two date columns
if '{startColumn}' in train_data.columns and '{endColumn}' in train_data.columns:
    train_data['{newColumnName}'] = (pd.to_datetime(train_data['{endColumn}']) - pd.to_datetime(train_data['{startColumn}'])).dt.total_seconds()

if 'test_data' in globals() and '{startColumn}' in test_data.columns and '{endColumn}' in test_data.columns:
    test_data['{newColumnName}'] = (pd.to_datetime(test_data['{endColumn}']) - pd.to_datetime(test_data['{startColumn}'])).dt.total_seconds()
";

            RunPythonScript(script, null);
        }
        public void HandleCyclicalTimeFeatures(string columnName, string featureType)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
import numpy as np

# Handle cyclical nature of time features
if '{columnName}' in train_data.columns:
    max_value = 23 if '{featureType}' == 'hour' else 11 if '{featureType}' == 'month' else 6 if '{featureType}' == 'dayofweek' else None
    if max_value:
        train_data['{columnName}_sin'] = np.sin(2 * np.pi * train_data['{columnName}'] / max_value)
        train_data['{columnName}_cos'] = np.cos(2 * np.pi * train_data['{columnName}'] / max_value)

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    max_value = 23 if '{featureType}' == 'hour' else 11 if '{featureType}' == 'month' else 6 if '{featureType}' == 'dayofweek' else None
    if max_value:
        test_data['{columnName}_sin'] = np.sin(2 * np.pi * test_data['{columnName}'] / max_value)
        test_data['{columnName}_cos'] = np.cos(2 * np.pi * test_data['{columnName}'] / max_value)
";

            RunPythonScript(script, null);
        }
        public void ParseDateColumn(string columnName)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
# Parse the date column into a datetime object
if '{columnName}' in train_data.columns:
    train_data['{columnName}'] = pd.to_datetime(train_data['{columnName}'])

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    test_data['{columnName}'] = pd.to_datetime(test_data['{columnName}'])
";

            RunPythonScript(script, null);
        }
        public void HandleMissingDates(string columnName, string method = "fill", string fillValue = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
# Handle missing dates
if '{columnName}' in train_data.columns:
    if '{method}' == 'fill':
        train_data['{columnName}'] = train_data['{columnName}'].fillna('{fillValue}')
    elif '{method}' == 'interpolate':
        train_data['{columnName}'] = pd.to_datetime(train_data['{columnName}']).interpolate()

if 'test_data' in globals() and '{columnName}' in test_data.columns:
    if '{method}' == 'fill':
        test_data['{columnName}'] = test_data['{columnName}'].fillna('{fillValue}')
    elif '{method}' == 'interpolate':
        test_data['{columnName}'] = pd.to_datetime(test_data['{columnName}']).interpolate()
";

            RunPythonScript(script, null);
        }

        #endregion "Date/Time Features"
        #region "Encoding Categorical Variables"
        public void OneHotEncode(string[] categoricalFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            // Convert the array of selected features to a Python list format
            string featuresList = string.Join(", ", categoricalFeatures.Select(f => $"'{f}'"));

            string script = $@"
import pandas as pd

# List of categorical features to one-hot encode
categorical_features = [{featuresList}]

# One-hot encode the train data
if 'train_data' in globals():
    train_data = pd.get_dummies(train_data, columns=categorical_features, drop_first=False)

# One-hot encode the test data, ensuring it has the same columns as the train data
if 'test_data' in globals():
    test_data = pd.get_dummies(test_data, columns=categorical_features, drop_first=False)
    # Align test_data columns with train_data columns
    test_data = test_data.reindex(columns=train_data.columns, fill_value=0)
";

            RunPythonScript(script, null);
        }
        public void LabelEncode(string[] categoricalFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featuresList = string.Join(", ", categoricalFeatures.Select(f => $"'{f}'"));

            string script = $@"
from sklearn.preprocessing import LabelEncoder

# List of categorical features to label encode
categorical_features = [{featuresList}]

label_encoders = {{}}

# Perform label encoding
for feature in categorical_features:
    le = LabelEncoder()
    if 'train_data' in globals():
        train_data[feature] = le.fit_transform(train_data[feature].astype(str))
    if 'test_data' in globals() and feature in test_data.columns:
        test_data[feature] = le.transform(test_data[feature].astype(str))
    label_encoders[feature] = le

# Store the label encoders in the Python persistent scope if needed
globals()['label_encoders'] = label_encoders
";

            RunPythonScript(script, null);
        }
        public void TargetEncode(string[] categoricalFeatures, string labelColumn)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featuresList = string.Join(", ", categoricalFeatures.Select(f => $"'{f}'"));

            string script = $@"
# List of categorical features to target encode
categorical_features = [{featuresList}]
label_column = '{labelColumn}'

# Perform target encoding
for feature in categorical_features:
    if 'train_data' in globals():
        means = train_data.groupby(feature)[label_column].mean()
        train_data[feature] = train_data[feature].map(means)
    if 'test_data' in globals() and feature in test_data.columns:
        test_data[feature] = test_data[feature].map(means)
";

            RunPythonScript(script, null);
        }
        public void BinaryEncode(string[] categoricalFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featuresList = string.Join(", ", categoricalFeatures.Select(f => $"'{f}'"));

            string script = $@"
import category_encoders as ce

# List of categorical features to binary encode
categorical_features = [{featuresList}]

# Perform binary encoding
encoder = ce.BinaryEncoder(cols=categorical_features)
if 'train_data' in globals():
    train_data = encoder.fit_transform(train_data)
if 'test_data' in globals():
    test_data = encoder.transform(test_data)

# Store the encoder in the Python persistent scope if needed
globals()['binary_encoder'] = encoder
";

            RunPythonScript(script, null);
        }
        public void FrequencyEncode(string[] categoricalFeatures)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featuresList = string.Join(", ", categoricalFeatures.Select(f => $"'{f}'"));

            string script = $@"
# List of categorical features to frequency encode
categorical_features = [{featuresList}]

# Perform frequency encoding
for feature in categorical_features:
    if 'train_data' in globals():
        freq = train_data[feature].value_counts(normalize=True)
        train_data[feature] = train_data[feature].map(freq)
    if 'test_data' in globals() and feature in test_data.columns:
        test_data[feature] = test_data[feature].map(freq)
";

            RunPythonScript(script, null);
        }


        #endregion "Encoding Categorical Variables"
        #region "Time Series Augmentation"
        //Augmentation Type: Make sure the augmentationType passed to the function matches one of the implemented techniques (time_warping, jittering, etc.).
        public void TimeSeriesAugmentation(string[] timeSeriesColumns, string augmentationType, double parameter)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            // Convert the array of time series columns to a Python list format
            string columnsList = string.Join(", ", timeSeriesColumns.Select(f => $"'{f}'"));

            string script = $@"
import numpy as np
import pandas as pd

def time_warping(series, sigma):
    return pd.Series(np.interp(np.linspace(0, len(series)-1, len(series)),
                               np.linspace(0, len(series)-1, len(series)) + np.random.normal(0, sigma, len(series)),
                               series))

def jittering(series, sigma):
    return series + np.random.normal(0, sigma, len(series))

def time_masking(series, mask_ratio):
    mask_len = int(len(series) * mask_ratio)
    mask_start = np.random.randint(0, len(series) - mask_len)
    series[mask_start:mask_start+mask_len] = np.nan
    return series

def window_slicing(series, window_size):
    start_idx = np.random.randint(0, len(series) - window_size)
    return series[start_idx:start_idx + window_size]

def permutation(series, n_segments):
    segments = np.array_split(series, n_segments)
    np.random.shuffle(segments)
    return pd.concat(segments)

# Columns to augment
columns = [{columnsList}]

# Apply augmentation
for col in columns:
    if '{augmentationType}' == 'time_warping':
        train_data[col] = time_warping(train_data[col], sigma={parameter})
    elif '{augmentationType}' == 'jittering':
        train_data[col] = jittering(train_data[col], sigma={parameter})
    elif '{augmentationType}' == 'time_masking':
        train_data[col] = time_masking(train_data[col], mask_ratio={parameter})
    elif '{augmentationType}' == 'window_slicing':
        train_data[col] = window_slicing(train_data[col], window_size=int({parameter}))
    elif '{augmentationType}' == 'permutation':
        train_data[col] = permutation(train_data[col], n_segments=int({parameter}))
";

            RunPythonScript(script, null);
        }

        #endregion "Time Series Augmentation"
        #region "Feature Selection"
        public void ApplyVarianceThreshold(double threshold = 0.0)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.feature_selection import VarianceThreshold

# Apply Variance Threshold
selector = VarianceThreshold(threshold={threshold})
selected_features = selector.fit_transform(train_data)

# Store the selected features back into the train_data
train_data = pd.DataFrame(selected_features, columns=train_data.columns[selector.get_support()])
";

            RunPythonScript(script, null);
        }
        public void ApplyCorrelationThreshold(double threshold = 0.9)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
import numpy as np

# Compute the correlation matrix
corr_matrix = train_data.corr().abs()

# Select upper triangle of correlation matrix
upper = corr_matrix.where(np.triu(np.ones(corr_matrix.shape), k=1).astype(bool))

# Find features with correlation greater than the threshold
to_drop = [column for column in upper.columns if any(upper[column] > {threshold})]

# Drop features
train_data = train_data.drop(to_drop, axis=1)
";

            RunPythonScript(script, null);
        }
        public void ApplyRFE(string modelId, int n_features_to_select = 5)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.feature_selection import RFE

# Retrieve the model from the dictionary
model = models['{modelId}']

# Apply RFE
selector = RFE(model, n_features_to_select={n_features_to_select}, step=1)
selector = selector.fit(train_data, train_data[label_column])

# Store the selected features back into the train_data
selected_features = train_data.columns[selector.get_support()]
train_data = train_data[selected_features]
";

            RunPythonScript(script, null);
        }
        public void ApplyL1Regularization(double alpha = 0.01)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.linear_model import Lasso

# Apply Lasso for feature selection
lasso = Lasso(alpha={alpha})
lasso.fit(train_data, train_data[label_column])

# Select features with non-zero coefficients
selected_features = train_data.columns[(lasso.coef_ != 0).ravel()]

# Store the selected features back into the train_data
train_data = train_data[selected_features]
";

            RunPythonScript(script, null);
        }
        public void ApplyTreeBasedFeatureSelection(string modelId)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
# Retrieve the model from the dictionary
model = models['{modelId}']

# Feature importance from tree-based model
importances = model.feature_importances_
indices = np.argsort(importances)[::-1]

# Store the selected features based on importance
selected_features = train_data.columns[indices]
train_data = train_data[selected_features]
";

            RunPythonScript(script, null);
        }

        #endregion "Feature Selection"
        #region "Cross-Validation and Stratified Sampling"
        public void PerformCrossValidation(string modelId, int numFolds = 5)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.model_selection import cross_val_score

# Retrieve the model from the dictionary
model = models['{modelId}']

# Perform cross-validation
scores = cross_val_score(model, X, Y, cv={numFolds}, scoring='accuracy')

# Calculate the average and standard deviation of the cross-validation scores
avg_score = scores.mean()
std_score = scores.std()

# Store the results in the Python persistent scope
globals()['avg_cross_val_score'] = avg_score
globals()['std_cross_val_score'] = std_score
";

            RunPythonScript(script, null);

            double avgScore = FetchAverageCrossValScoreFromPython();
            double stdScore = FetchStandardDeviationCrossValScoreFromPython();

            Console.WriteLine($"Average Cross-Validation Score: {avgScore}");
            Console.WriteLine($"Standard Deviation of Cross-Validation Scores: {stdScore}");
        }
        private double FetchAverageCrossValScoreFromPython()
        {
            using (Py.GIL())
            {
                dynamic pyScore = PythonRuntime.CurrentPersistentScope.Get("avg_cross_val_score");
                return pyScore.As<double>();
            }
        }
        private double FetchStandardDeviationCrossValScoreFromPython()
        {
            using (Py.GIL())
            {
                dynamic pyScore = PythonRuntime.CurrentPersistentScope.Get("std_cross_val_score");
                return pyScore.As<double>();
            }
        }
        public void PerformStratifiedSampling(float testSize, string trainFilePath, string testFilePath)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from sklearn.model_selection import train_test_split

# Perform stratified sampling to split the data
train_data, test_data = train_test_split(data, test_size={testSize}, stratify=data[label_column])

# Save the split datasets to files
train_data.to_csv('{trainFilePath}', index=False)
test_data.to_csv('{testFilePath}', index=False)
";

            RunPythonScript(script, null);
        }
        #endregion "Cross-Validation and Stratified Sampling"
        #region "Data Cleaning"
        public void ImputeMissingValues(string[] featureList = null, string strategy = "mean")
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featureListPython = featureList != null
                ? string.Join(", ", featureList.Select(f => $"'{f}'"))
                : "None";

            string script = $@"
from sklearn.impute import SimpleImputer
import pandas as pd

# Select the features
features = [{featureListPython}] if {featureListPython} is not None else data.columns.tolist()

# Create the imputer based on the strategy
imputer = SimpleImputer(strategy='{strategy}')

# Apply the imputer to the selected features
data[features] = imputer.fit_transform(data[features])

# Store the cleaned data back in the global scope if needed
globals()['data'] = data
";

            RunPythonScript(script, null);
        }
        public void DropDuplicates(string[] featureList = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featureListPython = featureList != null
                ? string.Join(", ", featureList.Select(f => $"'{f}'"))
                : "None";

            string script = $@"
import pandas as pd

# Drop duplicates based on the selected features
data = data.drop_duplicates(subset=[{featureListPython}] if {featureListPython} is not None else None)

# Store the cleaned data back in the global scope if needed
globals()['data'] = data
";

            RunPythonScript(script, null);
        }
        public void StandardizeCategories(string[] featureList = null, Dictionary<string, string> replacements = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featureListPython = featureList != null
                ? string.Join(", ", featureList.Select(f => $"'{f}'"))
                : "None";

            string replacementsPython = replacements != null
                ? "{" + string.Join(", ", replacements.Select(kv => $"'{kv.Key}': '{kv.Value}'")) + "}"
                : "{}";

            string script = $@"
import pandas as pd

# Select the features
features = [{featureListPython}] if {featureListPython} is not None else data.columns.tolist()

# Apply the replacements to the selected features
for feature in features:
    data[feature] = data[feature].replace({replacementsPython})

# Store the cleaned data back in the global scope if needed
globals()['data'] = data
";

            RunPythonScript(script, null);
        }

        public void RemoveOutliers(string[] featureList = null, double zThreshold = 3.0)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featureListPython = featureList != null
                ? string.Join(", ", featureList.Select(f => $"'{f}'"))
                : "None";

            string script = $@"
import pandas as pd
import numpy as np

# Select the features
features = [{featureListPython}] if {featureListPython} is not None else data.columns.tolist()

# Calculate Z-scores and filter out rows with outliers
z_scores = np.abs((data[features] - data[features].mean()) / data[features].std())
data = data[(z_scores < {zThreshold}).all(axis=1)]

# Store the cleaned data back in the global scope if needed
globals()['data'] = data
";

            RunPythonScript(script, null);
        }

        #endregion "Data Cleaning"
        #region "Dimensionality Reduction"
        public void ApplyPCA(int nComponents = 2, string[] featureList = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featureListPython = featureList != null
                ? string.Join(", ", featureList.Select(f => $"'{f}'"))
                : "None";

            string script = $@"
from sklearn.decomposition import PCA
import pandas as pd

# Select the features
features = [{featureListPython}] if {featureListPython} is not None else data.columns.tolist()

# Apply PCA to the selected features
pca = PCA(n_components={nComponents})
principal_components = pca.fit_transform(data[features])

# Create a DataFrame for the principal components
pc_df = pd.DataFrame(data=principal_components, columns=['PC' + str(i + 1) for i in range(principal_components.shape[1])])

# Combine with the non-reduced features if needed
if {featureListPython} != 'None':
    data = pd.concat([data.drop(columns=features), pc_df], axis=1)
else:
    data = pc_df

# Store the reduced data back in the global scope if needed
globals()['data'] = data
";

            RunPythonScript(script, null);
        }

        public void ApplyLDA(string labelColumn, int nComponents = 2, string[] featureList = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featureListPython = featureList != null
                ? string.Join(", ", featureList.Select(f => $"'{f}'"))
                : "None";

            string script = $@"
from sklearn.discriminant_analysis import LinearDiscriminantAnalysis as LDA
import pandas as pd

# Select the features
features = [{featureListPython}] if {featureListPython} != 'None' else data.columns.tolist()

# Separate the label
X = data[features]
Y = data['{labelColumn}']

# Apply LDA to the selected features
lda = LDA(n_components={nComponents})
lda_components = lda.fit_transform(X, Y)

# Create a DataFrame for the LDA components
lda_df = pd.DataFrame(data=lda_components, columns=['LD' + str(i + 1) for i in range(lda_components.shape[1])])

# Combine with the non-reduced features if needed
if {featureListPython} != 'None':
    data = pd.concat([data.drop(columns=features), lda_df], axis=1)
else:
    data = lda_df

# Store the reduced data back in the global scope if needed
globals()['data'] = data
";

            RunPythonScript(script, null);
        }
        public void ApplyVarianceThreshold(double threshold = 0.0, string[] featureList = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string featureListPython = featureList != null
                ? string.Join(", ", featureList.Select(f => $"'{f}'"))
                : "None";

            string script = $@"
from sklearn.feature_selection import VarianceThreshold
import pandas as pd

# Select the features
features = [{featureListPython}] if {featureListPython} is not None else data.columns.tolist()

# Apply variance threshold
selector = VarianceThreshold(threshold={threshold})
selected_features = selector.fit_transform(data[features])

# Create a DataFrame for the selected features
selected_features_df = pd.DataFrame(data=selected_features, columns=[features[i] for i in selector.get_support(indices=True)])

# Combine with the non-reduced features if needed
if {featureListPython} is not None:
    data = pd.concat([data.drop(columns=features), selected_features_df], axis=1)
else:
    data = selected_features_df

# Store the reduced data back in the global scope if needed
globals()['data'] = data
";

            RunPythonScript(script, null);
        }


        #endregion "Dimensionality Reduction"
        #region "Data Balancing Techniques"
        public void RandomOverSample(string labelColumn)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from imblearn.over_sampling import RandomOverSampler
import pandas as pd

ros = RandomOverSampler()
X, Y = ros.fit_resample(data.drop(columns=['{labelColumn}']), data['{labelColumn}'])

# Combine the resampled features and labels back into a DataFrame
data = pd.concat([pd.DataFrame(X), pd.Series(Y, name='{labelColumn}')], axis=1)

globals()['data'] = data
";

            RunPythonScript(script, null);
        }
        public void RandomUnderSample(string labelColumn)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from imblearn.under_sampling import RandomUnderSampler
import pandas as pd

rus = RandomUnderSampler()
X, Y = rus.fit_resample(data.drop(columns=['{labelColumn}']), data['{labelColumn}'])

# Combine the resampled features and labels back into a DataFrame
data = pd.concat([pd.DataFrame(X), pd.Series(Y, name='{labelColumn}')], axis=1)

globals()['data'] = data
";

            RunPythonScript(script, null);
        }
        public void ApplySMOTE(string labelColumn)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("The Python environment is not initialized.");
            }

            string script = $@"
from imblearn.over_sampling import SMOTE
import pandas as pd

smote = SMOTE()
X, Y = smote.fit_resample(data.drop(columns=['{labelColumn}']), data['{labelColumn}'])

# Combine the resampled features and labels back into a DataFrame
data = pd.concat([pd.DataFrame(X), pd.Series(Y, name='{labelColumn}')], axis=1)

globals()['data'] = data
";

            RunPythonScript(script, null);
        }

        #endregion "Data Balancing Techniques"
        #region "Fetch Values"
        private dynamic FetchPredictionsFromPython()
        {
            if (!IsInitialized)
            {
                return null;
            }
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                // Retrieve the 'predictions' variable from the persistent Python scope
                dynamic predictions = PythonRuntime.CurrentPersistentScope.Get("predictions");

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
                dynamic pyScore = PythonRuntime.CurrentPersistentScope.Get("score");
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
                dynamic pyScore = PythonRuntime.CurrentPersistentScope.Get("accuracy");
                return pyScore.As<double>(); // Convert the Python score to a C# double
            }
        }
        private double FetchMSEFromPython()
        {
            if (!IsInitialized)
            {
                return -1;
            }
            // Implement logic to retrieve the 'mse' variable from Python
            // This might involve fetching the variable's value from the Python scope
            using (Py.GIL())
            {
                dynamic pyMSE = PythonRuntime.CurrentPersistentScope.Get("mse");
                return pyMSE.As<double>(); // Convert the Python MSE to a C# double
            }
        }
        private double FetchRMSEFromPython()
        {
            if (!IsInitialized)
            {
                return -1;
            }
            // Implement logic to retrieve the 'rmse' variable from Python
            // This might involve fetching the variable's value from the Python scope
            using (Py.GIL())
            {
                dynamic pyRMSE = PythonRuntime.CurrentPersistentScope.Get("rmse");
                return pyRMSE.As<double>(); // Convert the Python RMSE to a C# double
            }
        }
        private double FetchMAEFromPython()
        {
            if (!IsInitialized)
            {
                return -1;
            }
            // Implement logic to retrieve the 'mae' variable from Python
            // This might involve fetching the variable's value from the Python scope
            using (Py.GIL())
            {
                dynamic pyMAE = PythonRuntime.CurrentPersistentScope.Get("mae");
                return pyMAE.As<double>(); // Convert the Python MAE to a C# double
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
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock features
            {
                dynamic pyFeatures = PythonRuntime.CurrentPersistentScope.Get("features");
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
                dynamic pyFeatures = PythonRuntime.CurrentPersistentScope.Get("predict_features");
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
        #endregion "Fetch Values"
        #region "Graphs"
        public bool CreateROC()
        {
            if (!IsInitialized)
            {
                return false;
            }

            string script = $@"from sklearn.metrics import roc_curve, auc
import matplotlib.pyplot as plt

# Assuming y_true is your true binary labels and y_score is the score estimated by the model
fpr, tpr, _ = roc_curve(y_true, y_score)
roc_auc = auc(fpr, tpr)

plt.figure()
plt.plot(fpr, tpr, color='darkorange', lw=2, label='ROC curve (area = %0.2f)' % roc_auc)
plt.plot([0, 1], [0, 1], color='navy', lw=2, linestyle='--')
plt.xlim([0.0, 1.0])
plt.ylim([0.0, 1.05])
plt.xlabel('False Positive Rate')
plt.ylabel('True Positive Rate')
plt.Title('Receiver operating characteristic')
plt.legend(loc='lower right')
plt.show()";
            return RunPythonScript(script, null);
        }
        public bool CreateConfusionMatrix()
        {
            if (!IsInitialized)
            {
                return false;
            }

            string script = $@"import matplotlib.pyplot as plt
from sklearn.metrics import confusion_matrix
import seaborn as sns

# Assuming y_true and y_pred are your true and predicted labels
cm = confusion_matrix(y_true, y_pred)

sns.heatmap(cm, annot=True, fmt='d')
plt.xlabel('Predicted')
plt.ylabel('True')
plt.show()";
            return RunPythonScript(script, null);
        }
        public bool CreateLearningCurve(string modelId, string imagePath)
        {
            if (!IsInitialized)
            {
                return false;
            }

            string formattedFilePath = imagePath.Replace("\\", "\\\\");

            // Python script to generate the learning curve
            string script = $@"
from sklearn.model_selection import learning_curve
import matplotlib.pyplot as plt
import numpy as np

# Assuming model is a scikit-learn estimator
model = models['{modelId}']

# Ensure X and Y are defined (assumes they are already available in the Python scope)
X = pd.get_dummies(train_data[features])
X.fillna(X.mean(), inplace=True)  # Simple mean imputation for missing values

Y = train_data[label_column].fillna(train_data[label_column].mean())  # Impute missing labels

# Generate learning curve
train_sizes, train_scores, test_scores = learning_curve(model, X, Y, cv=5, n_jobs=-1, train_sizes=np.linspace(0.1, 1.0, 10))

# Calculate mean and std for train and test scores
train_scores_mean = np.mean(train_scores, axis=1)
train_scores_std = np.std(train_scores, axis=1)
test_scores_mean = np.mean(test_scores, axis=1)
test_scores_std = np.std(test_scores, axis=1)

# Plot the learning curve
plt.figure()
plt.fill_between(train_sizes, train_scores_mean - train_scores_std, train_scores_mean + train_scores_std, alpha=0.1, color='r')
plt.fill_between(train_sizes, test_scores_mean - test_scores_std, test_scores_mean + test_scores_std, alpha=0.1, color='g')
plt.plot(train_sizes, train_scores_mean, 'o-', color='r', label='Training score')
plt.plot(train_sizes, test_scores_mean, 'o-', color='g', label='Cross-validation score')

plt.Title('Learning Curve')
plt.xlabel('Training Examples')
plt.ylabel('Score')
plt.legend(loc='best')
plt.grid()
plt.savefig('{formattedFilePath}')
# plt.show()
";
            return RunPythonScript(script, null);
        }
        public bool CreateFeatureImportance(string modelId, string imagePath)
        {
            if (!IsInitialized)
            {
                return false;
            }

            string formattedFilePath = imagePath.Replace("\\", "\\\\");

            // Python script to generate the feature importance plot
            string script = $@"
import matplotlib.pyplot as plt
import numpy as np

# Assuming model is a tree-based model like RandomForest or GradientBoosting
model = models['{modelId}']

# Ensure that the model has the attribute 'feature_importances_'
if hasattr(model, 'feature_importances_'):
    importances = model.feature_importances_
    indices = np.argsort(importances)[::-1]

    # Only use the top features
    top_features = min(len(importances), X.shape[1])

    plt.figure()
    plt.Title('Feature Importance')
    plt.bar(range(top_features), importances[indices][:top_features], align='center')
    plt.xticks(range(top_features), [features[i] for i in indices[:top_features]], rotation=90)
    plt.xlim([-1, top_features])
    plt.tight_layout()
    plt.savefig('{formattedFilePath}')
    # plt.show()
else:
   # print('The model does not have feature importances.')
";
            return RunPythonScript(script, null);
        }
        public bool CreatePrecisionRecallCurve(string modelId, string imagePath)
        {
            if (!IsInitialized)
            {
                return false;
            }
            string formattedFilePath = imagePath.Replace("\\", "\\\\");

            string script = $@"
from sklearn.metrics import precision_recall_curve, average_precision_score
import matplotlib.pyplot as plt

# Assuming y_test and predictions are available
model = models['{modelId}']
y_score = model.predict_proba(X_test)[:, 1]  # Assuming binary classification
precision, recall, _ = precision_recall_curve(y_test, y_score)
average_precision = average_precision_score(y_test, y_score)

plt.figure()
plt.plot(recall, precision, color='b', lw=2, label='Precision-Recall curve')
plt.xlabel('Recall')
plt.ylabel('Precision')
plt.Title('Precision-Recall Curve (AP={0:0.2f})'.format(average_precision))
plt.legend(loc='lower left')
plt.savefig('{formattedFilePath}')
# plt.show()
";
            return RunPythonScript(script, null);
        }
        public bool CreateConfusionMatrix(string modelId, string imagePath)
        {
            if (!IsInitialized)
            {
                return false;
            }
            string formattedFilePath = imagePath.Replace("\\", "\\\\");
            string script = $@"
import matplotlib.pyplot as plt
from sklearn.metrics import confusion_matrix
import seaborn as sns

# Assuming y_test and predictions are available
model = models['{modelId}']
predictions = model.predict(X_test)
cm = confusion_matrix(y_test, predictions)

sns.heatmap(cm, annot=True, fmt='d', cmap='Blues')
plt.xlabel('Predicted')
plt.ylabel('True')
plt.Title('Confusion Matrix')
plt.savefig('{formattedFilePath}')
# plt.show()
";
            return RunPythonScript(script, null);
        }
        public bool CreateROC(string modelId, string imagePath)
        {
            if (!IsInitialized)
            {
                return false;
            }
            string formattedFilePath = imagePath.Replace("\\", "\\\\");
            string script = $@"
from sklearn.metrics import roc_curve, auc
import matplotlib.pyplot as plt

# Assuming y_test and predictions are available
model = models['{modelId}']
y_score = model.predict_proba(X_test)[:, 1]  # Assuming binary classification
fpr, tpr, _ = roc_curve(y_test, y_score)
roc_auc = auc(fpr, tpr)

plt.figure()
plt.plot(fpr, tpr, color='darkorange', lw=2, label='ROC curve (area = %0.2f)' % roc_auc)
plt.plot([0, 1], [0, 1], color='navy', lw=2, linestyle='--')
plt.xlim([0.0, 1.0])
plt.ylim([0.0, 1.05])
plt.xlabel('False Positive Rate')
plt.ylabel('True Positive Rate')
plt.Title('Receiver Operating Characteristic')
plt.legend(loc='lower right')
plt.savefig('{formattedFilePath}')
# plt.show()
";
            return RunPythonScript(script, null);
        }
        public bool GenerateEvaluationReport(string modelId, string outputHtmlPath)
        {

            if (!IsInitialized)
            {
                return false;
            }

            // Determine which evaluation methods apply to the current model
            bool isClassification = CheckIfClassificationModel(modelId); // Implement this method
            bool isRegression = CheckIfRegressionModel(modelId); // Implement this method
            bool supportsFeatureImportance = CheckIfSupportsFeatureImportance(modelId); // Implement this method

            string rocImagePath = Path.Combine(Path.GetDirectoryName(outputHtmlPath), "roc_curve.png");
            string confusionMatrixImagePath = Path.Combine(Path.GetDirectoryName(outputHtmlPath), "confusion_matrix.png");
            string precisionRecallImagePath = Path.Combine(Path.GetDirectoryName(outputHtmlPath), "precision_recall_curve.png");
            string learningCurveImagePath = Path.Combine(Path.GetDirectoryName(outputHtmlPath), "learning_curve.png");
            string featureImportanceImagePath = Path.Combine(Path.GetDirectoryName(outputHtmlPath), "feature_importance.png");

            // Generate and save the images based on the model type
            bool ret1 = isClassification && CreateROC(modelId, rocImagePath);
            bool ret2 = isClassification && CreateConfusionMatrix(modelId, confusionMatrixImagePath);
            bool ret3 = isClassification && CreatePrecisionRecallCurve(modelId, precisionRecallImagePath);
            bool ret4 = CreateLearningCurve(modelId, learningCurveImagePath); // Applicable to both classification and regression
            bool ret5 = supportsFeatureImportance && CreateFeatureImportance(modelId, featureImportanceImagePath);

            // Create the HTML content conditionally
            string htmlContent = $@"
<html>
<head>
    <Title>Model Evaluation Report</Title>
</head>
<body>
    <h1>Model Evaluation Report</h1>";

            if (ret1)
            {
                htmlContent += $@"
    <h2>ROC Curve</h2>
    <img src='{Path.GetFileName(rocImagePath)}' alt='ROC Curve'>";
            }

            if (ret2)
            {
                htmlContent += $@"
    <h2>Confusion Matrix</h2>
    <img src='{Path.GetFileName(confusionMatrixImagePath)}' alt='Confusion Matrix'>";
            }

            if (ret3)
            {
                htmlContent += $@"
    <h2>Precision-Recall Curve</h2>
    <img src='{Path.GetFileName(precisionRecallImagePath)}' alt='Precision-Recall Curve'>";
            }

            htmlContent += $@"
    <h2>Learning Curve</h2>
    <img src='{Path.GetFileName(learningCurveImagePath)}' alt='Learning Curve'>";

            if (ret5)
            {
                htmlContent += $@"
    <h2>Feature Importance</h2>
    <img src='{Path.GetFileName(featureImportanceImagePath)}' alt='Feature Importance'>";
            }

            htmlContent += @"
</body>
</html>";

            // Save the HTML content to the output file
            File.WriteAllText(outputHtmlPath, htmlContent);
            return ret1 && ret2 && ret3 && ret4 && ret5;


        }
        private bool CheckIfClassificationModel(string modelId)
        {
            if (!IsInitialized)
            {
                return false;
            }

            string script = $@"
from sklearn.base import is_classifier
is_classification = False
if '{modelId}' in models:
    model = models['{modelId}']
    is_classification = is_classifier(model)
globals()['is_classification'] = is_classification
";

            RunPythonScript(script, null);

            using (Py.GIL())
            {
                dynamic isClassification = PythonRuntime.CurrentPersistentScope.Get("is_classification");
                return isClassification.As<bool>();
            }
        }

        private bool CheckIfRegressionModel(string modelId)
        {
            if (!IsInitialized)
            {
                return false;
            }

            string script = $@"
from sklearn.base import is_regressor
is_regression = False
if '{modelId}' in models:
    model = models['{modelId}']
    is_regression = is_regressor(model)
globals()['is_regression'] = is_regression
";

            RunPythonScript(script, null);

            using (Py.GIL())
            {
                dynamic isRegression = PythonRuntime.CurrentPersistentScope.Get("is_regression");
                return isRegression.As<bool>();
            }
        }

        private bool CheckIfSupportsFeatureImportance(string modelId)
        {
            if (!IsInitialized)
            {
                return false;
            }

            string script = $@"
supports_feature_importance = False
if '{modelId}' in models:
    model = models['{modelId}']
    if hasattr(model, 'feature_importances_'):
        supports_feature_importance = True
globals()['supports_feature_importance'] = supports_feature_importance
";

            RunPythonScript(script, null);

            using (Py.GIL())
            {
                dynamic supportsFeatureImportance = PythonRuntime.CurrentPersistentScope.Get("supports_feature_importance");
                return supportsFeatureImportance.As<bool>();
            }
        }

        #endregion
    }


}
