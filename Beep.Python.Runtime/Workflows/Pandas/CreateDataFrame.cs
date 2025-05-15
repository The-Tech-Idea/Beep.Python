using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using Beep.Python.RuntimeEngine.DataManagement;


namespace Beep.Python.RuntimeEngine.Workflows.Pandas
{
    public class CreateDataFrame : IWorkFlowAction
    {
        private PythonPandasManager _pandasManager;

        public CreateDataFrame(PythonPandasManager pandasManager)
        {
            _pandasManager = pandasManager;
            Id = Guid.NewGuid().ToString();
            ActionTypeName = "CreateDataFrame";
            ClassName = "CreateDataFrameAction";
            Name = "Create DataFrame";
        }

        public IWorkFlowAction PrevAction { get  ; set  ; }
        public List<IWorkFlowAction> NextAction { get  ; set  ; }
        public List<IPassedArgs> InParameters { get  ; set  ; }
        public List<IPassedArgs> OutParameters { get  ; set  ; }
        public List<IWorkFlowRule> Rules { get  ; set  ; }
        public string Id { get  ; set  ; }
        public string ActionTypeName { get  ; set  ; }
        public string Code { get  ; set  ; }
        public bool IsFinish { get  ; set  ; }
        public bool IsRunning { get  ; set  ; }
        public string ClassName { get  ; set  ; }
        public string Name { get  ; set  ; }

        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;

        // Implement IWorkFlowAction properties and events...

        

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            _pandasManager.CreateDataFrame(InParameters[0].ParameterString1, InParameters[0].ParameterString2);
            return new PassedArgs();
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> actionToExecute)
        {
            throw new NotImplementedException();
        }

        PassedArgs IWorkFlowAction.StopAction()
        {
            return new PassedArgs();
        }
    }


}
