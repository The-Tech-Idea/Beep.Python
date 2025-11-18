


namespace Beep.Python.Model
{
    public interface IPythonTrainingViewModel:IDisposable
    {
        void init();
        void ResetTraining();
        PassedParameters SplitData();
        PassedParameters Train();
    }
}