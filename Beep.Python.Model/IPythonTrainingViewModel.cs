using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;


namespace Beep.Python.Model
{
    public interface IPythonTrainingViewModel:IDisposable
    {
        void init();
        void ResetTraining();
        IErrorsInfo SplitData();
        IErrorsInfo Train();
    }
}