using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.Workflow;
using TheTechIdea.Beep.Vis;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.Addin;
using Beep.Python.DataManagement;


namespace Beep.Python.DataManagement.Workflows.Pandas
{
    [Addin(Caption = "Add Column", Name = "AddColumn", misc = "AddColumn", addinType = AddinType.Class, returndataTypename = "string")]
    public class AddColumn : IWorkFlowAction
    {
        private readonly PythonPandasManager _pandasManager;
        private bool _isRunning;
        private bool _isFinished;

        #region Constructor
        public AddColumn(PythonPandasManager pandasManager)
        {
            _pandasManager = pandasManager ?? throw new ArgumentNullException(nameof(pandasManager));
            Id = Guid.NewGuid().ToString();
            ActionTypeName = "AddColumn";
            ClassName = "AddColumnAction";
            Name = "Add Column";
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
        public bool IsFinish 
        { 
            get => _isFinished; 
            set => _isFinished = value; 
        }
        public bool IsRunning 
        { 
            get => _isRunning; 
            set => _isRunning = value; 
        }
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
                // Validate input parameters
                if (!ValidateInputParameters(result))
                {
                    return result;
                }

                _isRunning = true;
                _isFinished = false;

                // Notify workflow started
                WorkFlowActionStarted?.Invoke(this, new WorkFlowEventArgs { FlowAction = this });

                // Report progress
                progress?.Report(new PassedArgs 
                { 
                    Messege = "Starting add column operation...",
                    ParameterString1 = "AddColumn_Started"
                });

                // Extract parameters
                string dataFrameName = InParameters[0].ParameterString1;
                string columnName = InParameters[0].ParameterString2;
                string columnData = InParameters[0].ParameterString3;

                // Check for cancellation
                token.ThrowIfCancellationRequested();

                // Perform the add column operation
                _pandasManager.AddColumn(dataFrameName, columnName, columnData);

                // Report progress
                progress?.Report(new PassedArgs 
                { 
                    Messege = $"Successfully added column '{columnName}' to DataFrame '{dataFrameName}'",
                    ParameterString1 = "AddColumn_Progress",
                    ParameterInt1 = 100
                });

                // Set output parameters
                var outputParam = new PassedArgs
                {
                    ParameterString1 = dataFrameName,
                    ParameterString2 = columnName,
                    ParameterString3 = columnData,
                    Messege = "Column added successfully"
                };
                OutParameters.Add(outputParam);

                // Update result
                result.Messege = "Add column operation completed successfully";
                result.ParameterString1 = dataFrameName;
                result.ParameterString2 = columnName;
                result.EventType = "Success";

                _isFinished = true;
                _isRunning = false;

                // Notify workflow ended
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { FlowAction = this });
            }
            catch (OperationCanceledException)
            {
                result.Messege = "Add column operation was cancelled";
                result.EventType = "Cancelled";
                _isRunning = false;
                _isFinished = true;
            }
            catch (Exception ex)
            {
                result.Messege = $"Error adding column: {ex.Message}";
                result.EventType = "Error";
                _isRunning = false;
                _isFinished = true;

                // Log the error
                progress?.Report(new PassedArgs 
                { 
                    Messege = result.Messege,
                    ParameterString1 = "AddColumn_Error"
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
                
                result.Messege = "Add column operation stopped successfully";
                result.EventType = "Stopped";
            }
            catch (Exception ex)
            {
                result.Messege = $"Error stopping add column operation: {ex.Message}";
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
                result.Messege = "Missing required input parameters for AddColumn action";
                result.EventType = "Error";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InParameters[0].ParameterString1))
            {
                result.Messege = "DataFrame name parameter is required";
                result.EventType = "Error";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InParameters[0].ParameterString2))
            {
                result.Messege = "Column name parameter is required";
                result.EventType = "Error";
                return false;
            }

            if (string.IsNullOrWhiteSpace(InParameters[0].ParameterString3))
            {
                result.Messege = "Column data parameter is required";
                result.EventType = "Error";
                return false;
            }

            return true;
        }
        #endregion
    }
}
