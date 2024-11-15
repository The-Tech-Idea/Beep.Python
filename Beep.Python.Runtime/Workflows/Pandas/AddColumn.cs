using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;

using System.Threading;

namespace Beep.Python.RuntimeEngine.Workflows.Pandas
{
    public class AddColumn : IWorkFlowAction
    {
        private PythonPandasManager _pandasManager;

        public AddColumn(PythonPandasManager pandasManager)
        {
            _pandasManager = pandasManager;
            Id = Guid.NewGuid().ToString();
            ActionTypeName = "AddColumn";
            ClassName = "AddColumnAction";
            Name = "Add Column";
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
            // Implement the action logic, invoking _pandasManager.AddColumn...
            return new PassedArgs();
        }

        public PassedArgs StopAction()
        {
            // Implement the stop logic if applicable...
            return new PassedArgs();
        }
    }

}
