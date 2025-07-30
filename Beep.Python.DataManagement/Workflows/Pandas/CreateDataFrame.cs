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
    [Addin(Caption = "Create DataFrame", Name = "CreateDataFrame", misc = "CreateDataFrame", addinType = AddinType.Class, returndataTypename = "string")]
    public class CreateDataFrame : IWorkFlowAction
    {
        private readonly PythonPandasManager _pandasManager;
        private bool _isRunning;
        private bool _isFinished;

        #region Constructor
        public CreateDataFrame(PythonPandasManager pandasManager)
        {
            _pandasManager = pandasManager ?? throw new ArgumentNullException(nameof(pandasManager));
            Id = Guid.NewGuid().ToString();
            ActionTypeName = "CreateDataFrame";
            ClassName = "CreateDataFrameAction";
            Name = "Create DataFrame";
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
                    Messege = "Starting DataFrame creation...",
                    ParameterString1 = "CreateDataFrame_Started"
                });

                // Extract parameters
                string dataFrameName = InParameters[0].ParameterString1;
                string dataSource = InParameters[0].ParameterString2;

                // Check for cancellation
                token.ThrowIfCancellationRequested();

                // Report progress
                progress?.Report(new PassedArgs 
                { 
                    Messege = $"Creating DataFrame '{dataFrameName}'...",
                    ParameterString1 = "CreateDataFrame_Progress",
                    ParameterInt1 = 50
                });

                // Perform the DataFrame creation operation
                _pandasManager.CreateDataFrame(dataFrameName, dataSource);

                // Report progress
                progress?.Report(new PassedArgs 
                { 
                    Messege = $"Successfully created DataFrame '{dataFrameName}'",
                    ParameterString1 = "CreateDataFrame_Progress",
                    ParameterInt1 = 100
                });

                // Set output parameters
                var outputParam = new PassedArgs
                {
                    ParameterString1 = dataFrameName,
                    ParameterString2 = dataSource,
                    Messege = "DataFrame created successfully"
                };
                OutParameters.Add(outputParam);

                // Update result
                result.Messege = "DataFrame creation completed successfully";
                result.ParameterString1 = dataFrameName;
                result.ParameterString2 = dataSource;
                result.EventType = "Success";

                _isFinished = true;
                _isRunning = false;

                // Notify workflow ended
                WorkFlowActionEnded?.Invoke(this, new WorkFlowEventArgs { FlowAction = this });
            }
            catch (OperationCanceledException)
            {
                result.Messege = "DataFrame creation was cancelled";
                result.EventType = "Cancelled";
                _isRunning = false;
                _isFinished = true;
            }
            catch (Exception ex)
            {
                result.Messege = $"Error creating DataFrame: {ex.Message}";
                result.EventType = "Error";
                _isRunning = false;
                _isFinished = true;

                // Log the error
                progress?.Report(new PassedArgs 
                { 
                    Messege = result.Messege,
                    ParameterString1 = "CreateDataFrame_Error"
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
                
                result.Messege = "DataFrame creation stopped successfully";
                result.EventType = "Stopped";
            }
            catch (Exception ex)
            {
                result.Messege = $"Error stopping DataFrame creation: {ex.Message}";
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
                result.Messege = "Missing required input parameters for CreateDataFrame action";
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
                result.Messege = "Data source parameter is required";
                result.EventType = "Error";
                return false;
            }

            return true;
        }
        #endregion
    }
}
