using Beep.Python.Model;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.ViewModels
{
    public class PythonModelEvaluationGraphsViewModel : PythonBaseViewModel
    {
        public PythonModelEvaluationGraphsViewModel(IBeepService beepservice, IPythonRunTimeManager pythonRuntimeManager) : base(beepservice, pythonRuntimeManager)
        {
        }
        // General method to execute Python script and save the result as an image
        private  void ExecuteAndSavePlot(string script, string savePath)
        {
           try
                {
                    dynamic plt = Py.Import("matplotlib.pyplot");
                    PythonRuntime.RunCode(script,Progress,Token);
                    plt.savefig(savePath);
                    plt.close();
                }
                catch (PythonException ex)
                {
                    Console.WriteLine($"Python Error: {ex.Message}");
                }
           
        }
        // Method to generate a Confusion Matrix
        public  void GenerateConfusionMatrix(string savePath)
        {
            string script = @"
import seaborn as sns
# Assume data is loaded and 'y_test' and 'predictions' are available
conf_matrix = confusion_matrix(y_test, predictions)
sns.heatmap(conf_matrix, annot=True, fmt='d', cmap='Blues')";
            ExecuteAndSavePlot(script, savePath);
        }
        // Method to generate ROC Curve
        public  void GenerateROCCurve(string savePath)
        {
            string script = @"
from sklearn.metrics import roc_curve, auc
fpr, tpr, _ = roc_curve(y_test, model_probs)
roc_auc = auc(fpr, tpr)
plt.figure()
plt.plot(fpr, tpr, label='ROC curve (area = ' + str(roc_auc) + ')')
plt.plot([0, 1], [0, 1], linestyle='--')
plt.legend(loc='lower right')";
            ExecuteAndSavePlot(script, savePath);
        }
        // Method to generate Precision-Recall Curve
        public  void GeneratePrecisionRecallCurve(string savePath)
        {
            string script = @"
from sklearn.metrics import precision_recall_curve, auc
precision, recall, _ = precision_recall_curve(y_test, model_probs)
pr_auc = auc(recall, precision)
plt.plot(recall, precision, label='PR curve (area = ' + str(pr_auc) + ')')
plt.xlabel('Recall')
plt.ylabel('Precision')
plt.legend(loc='upper right')";
            ExecuteAndSavePlot(script, savePath);
        }
        // Method to generate Feature Importance plot
        public  void GenerateFeatureImportance(string savePath)
        {
            string script = @"
importance = model.feature_importances_
plt.barh(range(len(importance)), importance, align='center')
plt.yticks(range(len(importance)), feature_names)
plt.xlabel('Feature Importance')";
            ExecuteAndSavePlot(script, savePath);
        }
        // Method to generate Learning Curve
        public  void GenerateLearningCurve(string savePath)
        {
            string script = @"
train_sizes, train_scores, test_scores = learning_curve(model, X, y)
train_scores_mean = np.mean(train_scores, axis=1)
test_scores_mean = np.mean(test_scores, axis=1)
plt.plot(train_sizes, train_scores_mean, label='Training score')
plt.plot(train_sizes, test_scores_mean, label='Cross-validation score')
plt.legend(loc='best')";
            ExecuteAndSavePlot(script, savePath);
        }
    }

}
