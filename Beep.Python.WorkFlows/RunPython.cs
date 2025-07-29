using Beep.Python.Model;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Vis;

using TheTechIdea.Beep.Workflow;

namespace Beep.Python.WorkFlows
{
    [AddinAttribute( Caption ="Python",Category = DatasourceCategory.NONE, Description ="Run Python Code",ClassType ="Python",menu ="Script",Name ="Python")]
    public class RunPython : IWorkFlowAction
    {
        public IDMEEditor DMEEditor { get ; set ; }
        public IWorkFlowAction PrevAction { get ; set ; }
        public List<IWorkFlowAction> NextAction { get ; set ; }
        public List<IPassedArgs> InParameters { get ; set ; }
        public List<IPassedArgs> OutParameters { get ; set ; }
        public List<IWorkFlowRule> Rules { get ; set ; }
        public bool IsFinish { get ; set ; }
        public bool IsRunning { get ; set ; }
        public string ClassName { get ; set ; }
        public string Name { get ; set ; }
        public ICPythonManager CPythonManager { get; private set; }
        public string Id { get  ; set  ; }
        public string ActionTypeName { get  ; set  ; }
        public string Code { get  ; set  ; }

        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;

        IProgress<PassedArgs> Progress;
        CancellationToken Token;
        bool pythonready = false;
        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            Progress = progress;
            Token=token;
            IsFinish = false;
            PassedArgs args = new PassedArgs();
            WorkFlowActionStarted.Invoke(this, new WorkFlowEventArgs() { FlowAction = this });
            args.ParameterString1 = "Started Python Script";
            IsRunning = true;
            Progress.Report(args);
            if (DMEEditor.Passedarguments.Objects.Where(c => c.Name == "CPythonManager").Any())
            {
                CPythonManager = (ICPythonManager)DMEEditor.Passedarguments.Objects.FirstOrDefault(c => c.Name == "CPythonManager").obj;
                pythonready = true;
            }
            else
            {
                args.ParameterString1 = "Could not find Passed engine, Init Engine Python";
                Progress.Report(args);
               
            }
            if (pythonready)
            {
                if (!string.IsNullOrEmpty(DMEEditor.Passedarguments.ParameterString1))
                {
                    args.ParameterString1 = "Found Python Script";
                    Progress.Report(args);
                    CPythonManager.ProcessManager.RunScript(DMEEditor.Passedarguments.ParameterString1);
                }
            }
            args.ParameterString1 = "Python Script Ended";
            Progress.Report(args);
            IsRunning = false;
            IsFinish = true;
            return args;
           
        }
      
        public PassedArgs StopAction()
        {
            PassedArgs args = new PassedArgs();
            args.ParameterString1 = "Stopping Python Script";
            Progress.Report(args);
            IsRunning = false;
            IsFinish = true;
            return args;
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> actionToExecute)
        {
            throw new NotImplementedException();
        }
    }
}
