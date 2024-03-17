using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Interfaces;

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

        public IWorkFlowAction PrevAction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IWorkFlowAction> NextAction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IPassedArgs> InParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IPassedArgs> OutParameters { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IWorkFlowRule> Rules { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ActionTypeName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Code { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsFinish { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsRunning { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string ClassName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;

        // Implement IWorkFlowAction properties and events...

        

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            _pandasManager.CreateDataFrame(InParameters[0].ParameterString1, InParameters[0].ParameterString2);
            return new PassedArgs();
        }

       

        PassedArgs IWorkFlowAction.StopAction()
        {
            return new PassedArgs();
        }
    }


}
