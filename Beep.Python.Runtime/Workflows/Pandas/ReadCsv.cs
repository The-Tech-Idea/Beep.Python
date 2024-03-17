using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Workflow.Interfaces;
using TheTechIdea;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Util;
using System.Threading;
using System.Xml.Linq;
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
            // Implement the action logic, invoking _pandasManager.ReadCsv...
            return new PassedArgs();
        }

        public PassedArgs StopAction()
        {
            // Implement the stop logic if applicable...
            return new PassedArgs();
        }
    }


}
