using System;
using System.Collections.Generic;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;


using System.Threading;
using Beep.Python.RuntimeEngine.DataManagement;

namespace Beep.Python.RuntimeEngine.Workflows.Pandas
{
    [Addin(Caption = "Read CSV", Name = "ReadCsv", misc = "ReadCsv", addinType = AddinType.Class, returndataTypename = "string")]
    public class ReadCsv : IWorkFlowAction
    {
        private PythonPandasManager _pandasManager;

        public ReadCsv(PythonPandasManager pandasManager)
        {
            _pandasManager = pandasManager;
            Id = Guid.NewGuid().ToString();
            ActionTypeName = "ReadCsv";
            ClassName = "ReadCsvAction";
            Name = "Read CSV";
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
            // Implement the action logic, invoking _pandasManager.ReadCsv...
            return new PassedArgs();
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> actionToExecute)
        {
            throw new NotImplementedException();
        }

        public PassedArgs StopAction()
        {
            // Implement the stop logic if applicable...
            return new PassedArgs();
        }
    }


}
