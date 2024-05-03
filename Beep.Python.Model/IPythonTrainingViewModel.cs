using System;
using TheTechIdea.Util;

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