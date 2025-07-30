using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beep.Python.Model;

using CommunityToolkit.Mvvm.ComponentModel;
using Python.Runtime;
using TheTechIdea.Beep.Container.Services;


namespace Beep.Python.ML
{


    public partial class PythonModelEvaluationGraphsViewModel : PythonBaseViewModel, IPythonModelEvaluationGraphsViewModel
    {
        // Initializes and maintains Python environment

        [ObservableProperty]
        int figurewidth = 10;
        [ObservableProperty]
        int figureheight = 8;
        IPythonMLManager PythonMLManager { get; set; }
        public PythonModelEvaluationGraphsViewModel(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager,PythonSessionInfo sessionInfo) : base(beepservice, pythonRuntimeManager, sessionInfo)
        {
           

        }
        public void Init(IPythonMLManager mLManager)
        {
            PythonMLManager = mLManager;
        }
        // General method to execute Python script and save the result as an image
        private void ExecuteAndSavePlot(string script, string savePath)
        {

            try
            {
                PythonMLManager.ImportPythonModule("matplotlib");
                PythonRuntime.ExecuteManager.RunPythonCodeAndGetOutput(Progress, script, SessionInfo);

            }
            catch (PythonException ex)
            {
                Console.WriteLine($"Python Error: {ex.Message}");
            }

        }

        // Method to generate a Confusion Matrix
        public void GenerateConfusionMatrix(string savePath)
        {
            string script = $@"
import matplotlib.pyplot as plt
import seaborn as sns
import pandas as pd  # Assuming you need pandas to handle data
from sklearn.metrics import confusion_matrix

# Sample data loading (modify accordingly)
# y_test = pd.Series([...])
# predictions = pd.Series([...])

# Generate the confusion matrix
conf_matrix = confusion_matrix(y_test, predictions)

# Create the heatmap
plt.figure(figsize=({Figurewidth}, {Figureheight}))  # Optional: Adjust the size of the image
sns.heatmap(conf_matrix, annot=True, fmt='d', cmap='Blues')

# Save the plot to the specified path
plt.savefig('{savePath}')
plt.close()";  // Close the plot to free up memory
            ExecuteAndSavePlot(script, savePath);
        }




        // Method to generate ROC Curve
        public void GenerateROCCurve(string savePath)
        {
            string script = @$"
from sklearn.metrics import roc_curve, auc
fpr, tpr, _ = roc_curve(y_test, model_probs)
roc_auc = auc(fpr, tpr)
plt.figure(figsize=({Figurewidth}, {Figureheight}))
plt.plot(fpr, tpr, label='ROC curve (area = ' + str(roc_auc) + ')')
plt.plot([0, 1], [0, 1], linestyle='--')
plt.legend(loc='lower right')
plt.savefig({savePath});
plt.close();";
            ExecuteAndSavePlot(script, savePath);
        }

        // Method to generate Precision-Recall Curve
        public void GeneratePrecisionRecallCurve(string savePath)
        {
            string script = @$"
from sklearn.metrics import precision_recall_curve, auc
precision, recall, _ = precision_recall_curve(y_test, model_probs)
pr_auc = auc(recall, precision)
plt.figure(figsize=({Figurewidth}, {Figureheight}))
plt.plot(recall, precision, label='PR curve (area = ' + str(pr_auc) + ')')
plt.xlabel('Recall')
plt.ylabel('Precision')
plt.legend(loc='upper right')
plt.savefig({savePath});
plt.close()";
            ExecuteAndSavePlot(script, savePath);
        }

        // Method to generate Feature Importance plot
        public void GenerateFeatureImportance(string savePath)
        {
            string script = @$"
importance = model.feature_importances_
plt.figure(figsize=({Figurewidth}, {Figureheight}))
plt.barh(range(len(importance)), importance, align='center')
plt.yticks(range(len(importance)), feature_names)
plt.xlabel('Feature Importance')
plt.savefig({savePath});
plt.close()";
            ExecuteAndSavePlot(script, savePath);
        }

        // Method to generate Learning Curve
        public void GenerateLearningCurve(string savePath)
        {
            string script = @$"
train_sizes, train_scores, test_scores = learning_curve(model, X, Y)
train_scores_mean = np.mean(train_scores, axis=1)
test_scores_mean = np.mean(test_scores, axis=1)
plt.figure(figsize=({Figurewidth}, {Figureheight}))
plt.plot(train_sizes, train_scores_mean, label='Training score')
plt.plot(train_sizes, test_scores_mean, label='Cross-validation score')
plt.legend(loc='best')
plt.savefig({savePath});
plt.close()";
            ExecuteAndSavePlot(script, savePath);
        }
    }

}
