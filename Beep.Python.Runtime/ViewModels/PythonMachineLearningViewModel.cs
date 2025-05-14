using Beep.Python.Model;
using Python.Runtime;
using System;


using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.ViewModels
{
    public class PythonMachineLearningViewModel : PythonBaseViewModel
    {
        public PythonMachineLearningViewModel(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager, PythonSessionInfo sessionInfo) : base(beepservice, pythonRuntimeManager, sessionInfo)
        {
            DataSetPath = PythonDatafolder;
            
            pythonImports = "import numpy as np\nimport pandas as pd\nfrom sklearn.model_selection import train_test_split\n import matplotlib.pyplot";
           
        }
        public string DataSetPath { get; private set; }
        public string TargetColumn { get; private set; }
        private string pythonImports;
        private string pythonDataPreparation { get; set; }
        private string pythonModelTraining { get; set; }
        private string pythonModelEvaluation { get; set; }



        public void UpdateDataPreparationScript()
        {
            pythonDataPreparation = $@"
data = pd.read_csv('{DataSetPath}')
X = data.drop('{TargetColumn}', axis=1)
Y = data['{TargetColumn}']
X_train, X_test, y_train, y_test = train_test_split(X, Y, test_size=0.2, random_state=42)";
        }

        public void SetModel(string modelModule, string modelName, string parameters)
        {
            if (!pythonImports.Contains(modelModule) && modelModule != "sklearn")
            {
                pythonImports += $"import {modelModule}\n";
            }
            pythonModelTraining = $@"
from {modelModule} import {modelName}
model = {modelName}({parameters})
model.fit(X_train, y_train)";

            // Check if predict_proba is appropriate and available
            pythonModelTraining += $@"
try:
    model_probs = model.predict_proba(X_test)
    has_proba = True
except AttributeError:
    has_proba = False
    predictions = model.predict(X_test)  # Fallback to using predict if predict_proba is not available
";
        }
        public void SetEvaluationMethod(string evaluationScript)
        {
            pythonModelEvaluation = evaluationScript;
        }

        public void ExecuteModel()
        {
            
                try
                {
                    PythonRuntime.ExecuteManager.RunCode(SessionInfo,pythonImports + pythonDataPreparation + pythonModelTraining + pythonModelEvaluation,Progress,Token);
                    Console.WriteLine("Model executed successfully.");
                }
                catch (PythonException ex)
                {
                    Console.WriteLine($"Python Error: {ex.Message}");
                }
           
        }
    }
}
