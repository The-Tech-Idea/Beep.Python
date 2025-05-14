using Beep.Python.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Container.Services;
using Beep.Python.RuntimeEngine.Helpers;



namespace Beep.Python.RuntimeEngine.ViewModels
{
    public partial class PythonBaseViewModel : ObservableObject, IDisposable
    {
        [ObservableProperty]
        IPythonRunTimeManager pythonRuntime;
        [ObservableProperty]
        PyModule persistentScope;
        [ObservableProperty]
        bool disposedValue;
        [ObservableProperty]
        public CancellationTokenSource tokenSource;
        [ObservableProperty]
        public CancellationToken token;
        [ObservableProperty]
        public IProgress<PassedArgs> progress;
        [ObservableProperty]
        IDMEEditor editor;
        [ObservableProperty]
        bool isBusy;
        [ObservableProperty]
        string pythonDatafolder;
        [ObservableProperty]
        List<LOVData> listofAlgorithims;
        [ObservableProperty]
        List<ParameterDictionaryForAlgorithm> parameterDictionaryForAlgorithms;
        [ObservableProperty]
        List<string> algorithims;
        public readonly IBeepService Beepservice;
        public PythonSessionInfo SessionInfo;

        public ObservableBindingList<PythonRunTime> AvailablePythonInstallations =>pythonRuntime.PythonInstallations;

        public string GetAlgorithimName(string algorithim)
        {
            return Enum.GetName(typeof(MachineLearningAlgorithm), algorithim);
        }

        public PythonBaseViewModel(IBeepService beepservice,IPythonRunTimeManager pythonRuntimeManager,PythonSessionInfo sessionInfo)
        {
            Beepservice=beepservice;
            Editor= beepservice.DMEEditor;
            this.PythonRuntime = pythonRuntimeManager;
            SessionInfo = sessionInfo;

           
      
           // pythonDatafolder = Editor.GetPythonDataPath();
            ListofAlgorithims = new List<LOVData>();
            foreach (var item in Enum.GetNames(typeof(MachineLearningAlgorithm)))
            {
                LOVData data = new LOVData() { ID = item, DisplayValue = item, LOVDESCRIPTION = MLAlgorithmsHelpers.GenerateAlgorithmDescription((MachineLearningAlgorithm)Enum.Parse(typeof(MachineLearningAlgorithm), item)) };
                ListofAlgorithims.Add(data);
            }
            Algorithims = MLAlgorithmsHelpers.GetAlgorithms();
            ParameterDictionaryForAlgorithms = MLAlgorithmsHelpers.GetParameterDictionaryForAlgorithms();
        }

        public void SendMessege(string messege = null)
        {

            if (Progress != null)
            {
                PassedArgs ps = new PassedArgs { EventType = "Update", Messege = messege, ParameterString1 = Editor.ErrorObject.Message };
                Progress.Report(ps);
            }

        }
        public async Task RefreshPythonInstallationsAsync()
        {
            IsBusy = true;
            try
            {
                await Task.Run(() => {
                    PythonRuntime.RefreshPythonInstalltions();
                });

                // Update any UI properties
                OnPropertyChanged(nameof(AvailablePythonInstallations));
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task<bool> AddCustomPythonInstallation(string path)
        {
            var report = await Task.Run(() => PythonEnvironmentDiagnostics.RunFullDiagnostics(path));

            if (report.PythonFound)
            {
                var config = PythonRunTimeDiagnostics.GetPythonConfig(path);
                if (config != null)
                {
                    PythonRuntime.PythonInstallations.Add(config);
                    PythonRuntime.SaveConfig();
                    return true;
                }
            }

            return false;
        }

        public virtual void ImportPythonModule(string moduleName)
        {
            if (SessionInfo==null)
            {
                return;
            }
            string script = $"import {moduleName}";
            PythonRuntime.ExecuteManager.RunPythonScript(script, null,SessionInfo);
        }
    
         protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }


        public virtual void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
