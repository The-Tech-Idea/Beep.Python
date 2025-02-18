﻿using Beep.Python.Model;
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
using Beep.Python.RuntimeEngine.Services;


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
        CancellationTokenSource tokenSource;
        [ObservableProperty]
        CancellationToken token;
        [ObservableProperty]
        IProgress<PassedArgs> progress;
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

        
        public string GetAlgorithimName(string algorithim)
        {
            return Enum.GetName(typeof(MachineLearningAlgorithm), algorithim);
        }

        public PythonBaseViewModel(IBeepService beepservice,IPythonRunTimeManager pythonRuntimeManager)
        {
            Beepservice=beepservice;
            Editor= beepservice.DMEEditor;
            this.PythonRuntime = pythonRuntimeManager;
            InitializePythonEnvironment();
            PythonHelpers._persistentScope = PersistentScope;
            PythonHelpers._pythonRuntimeManager = pythonRuntimeManager;
            pythonDatafolder = Editor.GetPythonDataPath();
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
        public virtual void ImportPythonModule(string moduleName)
        {
            if (!IsInitialized)
            {
                return;
            }
            string script = $"import {moduleName}";
            RunPythonScript(script, null);
        }
        public bool IsInitialized => PythonRuntime.IsInitialized;
        public virtual bool InitializePythonEnvironment()
        {
            bool retval = false;
            if (!PythonRuntime.IsInitialized)
            {
                PythonRuntime.Initialize();
            }
            if (!PythonRuntime.IsInitialized)
            {
                return retval;
            }
            if (PythonRuntime.PersistentScope == null && PythonRuntime.IsInitialized)
            {
                using (Py.GIL())
                {
                    PythonRuntime.PersistentScope = Py.CreateScope("__main__");
                    PythonRuntime.PersistentScope.Exec("models = {}");  // Initialize the models dictionary
                    persistentScope = PythonRuntime.PersistentScope;
                    retval = true;
                }
                retval = true;
            }
           
            return retval;
        }
        public virtual  bool RunPythonScript(string script,dynamic parameters)
        {
            bool retval = false;
            if (!IsInitialized)
            {
                return retval;
            }
            //using (var gil = PythonRuntime.GIL()) // Acquire the Python Global Interpreter Lock
            //{
            //    PersistentScope.Exec(script); // Execute the script in the persistent scope
            //                                   // Handle outputs if needed

            //    // If needed, return results or handle outputs
            //}
            try
            {
                using (var gil = PythonRuntime.GIL())
                {
                    if (parameters != null)
                    {
                        PythonRuntime.PersistentScope.Set(nameof(parameters), parameters);
                    }
                     PythonRuntime.PersistentScope.Exec(script);
                    retval = true;
                }
    //          await  Task.Run(()=> PythonRuntime.PersistentScope.Exec(script));
                retval = true;
            }
            catch (Exception ex)
            {
                Editor.AddLogMessage("Beep", $"Error in running python : {ex.Message}", DateTime.Now, -1, null,     Errors.Failed);
                Console.WriteLine($"Error executing Python script: {ex.Message}");
                return false;
            }

        
           return retval;
        }
        public virtual async Task<string> RunPythonCodeAndGetOutput(IProgress<PassedArgs> progress, string code)
        {
            string wrappedPythonCode = $@"
import sys
import io
import clr

class CustomStringIO(io.StringIO):
    def write(self, s):
        super().write(s)
        output = self.getvalue()
        if output.strip():
            OutputHandler(output.strip())
            self.truncate(0)  # Clear the internal buffer
            self.seek(0)  # Reset the buffer pointer

def capture_output(code, globals_dict):
    original_stdout = sys.stdout
    sys.stdout = CustomStringIO()

    try:
        exec(code, dict(globals_dict))
    finally:
        sys.stdout = original_stdout
";
            bool isImage = false;
            string output = "";

            //using (var gil = PythonRuntime.GIL())
            //{
                    Action<string> OutputHandler = line =>
                    {
                        // runTimeManager.OutputLines.Add(line);
                        progress.Report(new PassedArgs() { Messege = line });
                        Console.WriteLine(line);
                    };
                    PythonRuntime.PersistentScope.Set(nameof(OutputHandler), OutputHandler);

                    await Task.Run(() => PythonRuntime.PersistentScope.Exec(wrappedPythonCode));
                    PyObject captureOutputFunc = PythonRuntime.PersistentScope.GetAttr("capture_output");
                    Dictionary<string, object> globalsDict = new Dictionary<string, object>();

                    PyObject pyCode = code.ToPython();
                    PyObject pyGlobalsDict = globalsDict.ToPython();
                    PyObject result = captureOutputFunc.Invoke(pyCode, pyGlobalsDict);
                    if (result is PyObject pyObj)
                    {
                        var pyObjType = pyObj.GetPythonType();
                        var pyObjTypeName = pyObjType.ToString();


                    }
               
           // }

            IsBusy = false;
            return output;
        }
        public dynamic RunPythonScriptWithResult(string script,dynamic parameters)
        {
            dynamic result = null;
            if (PythonRuntime == null)
            {
                return null;
            }
            if (!PythonRuntime.IsInitialized)
            {
                return result;
            }

            //using (var gil = PythonRuntime.GIL()) // Acquire the Python Global Interpreter Lock
            //{
            //    result = PersistentScope.Exec(script); // Execute the script in the persistent scope
            //}
            result= PythonRuntime.PersistentScope.Exec(script);
            return result;
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

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PythonBaseViewModel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public virtual void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
