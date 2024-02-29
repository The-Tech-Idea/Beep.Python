using CommunityToolkit.Mvvm.ComponentModel;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea;
using TheTechIdea.Beep;


namespace Beep.Python.RuntimeEngine.ViewModels
{
    public class PythonBaseViewModel : ObservableObject, IDisposable
    {
        public PythonNetRunTimeManager _pythonRuntimeManager;
        public PyModule _persistentScope;
        public bool disposedValue;
        public CancellationTokenSource TokenSource { get; set; }
        public CancellationToken Token { get; set; }
        public IProgress<PassedArgs> Progress { get; set; }
        IDMEEditor Editor;
        public PythonBaseViewModel(PythonNetRunTimeManager pythonRuntimeManager, PyModule persistentScope)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            Editor = _pythonRuntimeManager.DMEditor;
            _persistentScope = persistentScope;
            PythonHelpers._persistentScope = persistentScope;
            PythonHelpers._pythonRuntimeManager = pythonRuntimeManager;
        }
        public PythonBaseViewModel(PythonNetRunTimeManager pythonRuntimeManager)
        {
            _pythonRuntimeManager = pythonRuntimeManager;
            Editor = _pythonRuntimeManager.DMEditor;
            InitializePythonEnvironment();
            PythonHelpers._persistentScope = _persistentScope;
            PythonHelpers._pythonRuntimeManager = pythonRuntimeManager;
        }
        public void SendMessege(string messege = null)
        {

            if (Progress != null)
            {
                PassedArgs ps = new PassedArgs { EventType = "Update", Messege = messege, ParameterString1 = Editor.ErrorObject.Message };
                Progress.Report(ps);
            }

        }
        public PythonBaseViewModel()
        {
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
        public bool IsInitialized => _pythonRuntimeManager.IsInitialized;
        public virtual bool InitializePythonEnvironment()
        {
            bool retval = false;
            if (!_pythonRuntimeManager.IsInitialized)
            {
                _pythonRuntimeManager.Initialize();
            }
            if (!_pythonRuntimeManager.IsInitialized)
            {
                return retval;
            }
            using (Py.GIL())
            {
                _persistentScope = Py.CreateScope("__main__");
                _persistentScope.Exec("models = {}");  // Initialize the models dictionary
                retval = true;
            }
            return retval;
        }
        public virtual void RunPythonScript(string script, dynamic parameters)
        {
            if (!IsInitialized)
            {
                return;
            }
            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                _persistentScope.Exec(script); // Execute the script in the persistent scope
                                               // Handle outputs if needed

                // If needed, return results or handle outputs
            }
        }
        public dynamic RunPythonScriptWithResult(string script, dynamic parameters)
        {
            dynamic result = null;
            if (_pythonRuntimeManager == null)
            {
                return null;
            }
            if (!_pythonRuntimeManager.IsInitialized)
            {
                return result;
            }

            using (Py.GIL()) // Acquire the Python Global Interpreter Lock
            {
                result = _persistentScope.Exec(script); // Execute the script in the persistent scope
            }

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
