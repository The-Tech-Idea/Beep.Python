using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;


namespace Beep.Python.DataManagement.Workflows.Pandas
{
    [Addin(Caption = "Group By", Name = "GroupBy", misc = "GroupBy", addinType = AddinType.Class, returndataTypename = "string")]
    public class GroupBy : IWorkFlowAction
    {
        private readonly PythonPandasManager _pandasManager;
        private bool _isRunning;
        private bool _isFinished;

        #region Constructor
        public GroupBy(PythonPandasManager pandasManager)
        {
            _pandasManager = pandasManager ?? throw new ArgumentNullException(nameof(pandasManager));
            Id = Guid.NewGuid().ToString();
            ActionTypeName = "GroupBy";
            ClassName = "GroupByAction";
            Name = "Group By";
            NextAction = new List<IWorkFlowAction>();
            InParameters = new List<IPassedArgs>();
            OutParameters = new List<IPassedArgs>();
            Rules = new List<IWorkFlowRule>();
        }
        #endregion

        #region Properties
        public IWorkFlowAction PrevAction { get; set; }
        public List<IWorkFlowAction> NextAction { get; set; }
        public List<IPassedArgs> InParameters { get; set; }
        public List<IPassedArgs> OutParameters { get; set; }
        public List<IWorkFlowRule> Rules { get; set; }
        public string Id { get; set; }
        public string ActionTypeName { get; set; }
        public string Code { get; set; }
        public bool IsFinish { get => _isFinished; set => _isFinished = value; }
        public bool IsRunning { get => _isRunning; set => _isRunning = value; }
        public string ClassName { get; set; }
        public string Name { get; set; }
        #endregion

        #region Events
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionStarted;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionEnded;
        public event EventHandler<WorkFlowEventArgs> WorkFlowActionRunning;
        #endregion

        #region Public Methods
        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token)
        {
            var result = new PassedArgs();
            
            try
            {
                if (!ValidateInputParameters(result))
                    return result;

                _isRunning = true;
                _isFinished = false;

                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { FlowAction = this });

                progress?.Report(new PassedArgs 
                { 
                    Messege = "Starting GroupBy operation...",
                    ParameterString1 = "GroupBy_Started"
                });

                string dataFrameName = InParameters[0].ParameterString1;
                string newFrameName = InParameters[0].ParameterString2;
                string groupByColumn = InParameters[0].ParameterString3;
                string aggFunction = InParameters[0].ParameterString1; // Using ParameterString1 for now, should be a separate parameter

                token.ThrowIfCancellationRequested();

                _pandasManager.GroupBy(dataFrameName, newFrameName, groupByColumn, aggFunction);

                progress?.Report(new PassedArgs 
                { 
                    Messege = $"Successfully grouped {dataFrameName} by {groupByColumn} using {aggFunction}",
                    ParameterString1 = "GroupBy_Progress",
                    ParameterInt1 = 100
                });

                var outputParam = new PassedArgs
                {
                    ParameterString1 = dataFrameName,
                    ParameterString2 = newFrameName,
                    ParameterString3 = groupByColumn,
                    Messege = "GroupBy operation completed successfully"
                };
                OutParameters.Add(outputParam);

                result.Messege = "GroupBy operation completed successfully";
                result.ParameterString1 = newFrameName;
                result.EventType = "Success";

                _isFinished = true;
                _isRunning = false;

                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { FlowAction = this });
            }
            catch (OperationCanceledException)
            {
                result.Messege = "GroupBy operation was cancelled";
                result.EventType = "Cancelled";
                _isRunning = false;
                _isFinished = true;
            }
            catch (Exception ex)
            {
                result.Messege = $"Error in GroupBy operation: {ex.Message}";
                result.EventType = "Error";
                _isRunning = false;
                _isFinished = true;

                progress?.Report(new PassedArgs 
                { 
                    Messege = result.Messege,
                    ParameterString1 = "GroupBy_Error"
                });
            }

            return result;
        }

        public async Task<PassedArgs> PerformActionAsync(IProgress<PassedArgs> progress, CancellationToken token)
        {
            return await Task.Run(() => PerformAction(progress, token), token);
        }

        public PassedArgs PerformAction(IProgress<PassedArgs> progress, CancellationToken token, Func<PassedArgs, object> actionToExecute)
        {
            if (actionToExecute == null)
                throw new ArgumentNullException(nameof(actionToExecute));

            var args = PerformAction(progress, token);
            actionToExecute(args);
            return args;
        }

        public PassedArgs StopAction()
        {
            var result = new PassedArgs();
            
            try
            {
                _isRunning = false;
                _isFinished = true;
                
                result.Messege = "GroupBy operation stopped successfully";
                result.EventType = "Stopped";
            }
            catch (Exception ex)
            {
                result.Messege = $"Error stopping GroupBy operation: {ex.Message}";
                result.EventType = "Error";
            }

            return result;
        }
        #endregion

        #region Private Methods
        private bool ValidateInputParameters(PassedArgs result)
        {
            if (InParameters == null || InParameters.Count < 1)
            {
                result.Messege = "Missing required input parameters for GroupBy action";
                result.EventType = "Error";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InParameters[0].ParameterString1))
            {
                result.Messege = "Source DataFrame name parameter is required";
                result.EventType = "Error";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InParameters[0].ParameterString2))
            {
                result.Messege = "Target DataFrame name parameter is required";
                result.EventType = "Error";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InParameters[0].ParameterString3))
            {
                result.Messege = "GroupBy column parameter is required";
                result.EventType = "Error";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InParameters[0].ParameterString3))
            {
                result.Messege = "Aggregation function parameter is required";
                result.EventType = "Error";
                return false;
            }

            return true;
        }
        #endregion
    }
}