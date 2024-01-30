using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine
{
    public static class PythonHelpers
    {
        public static  PythonNetRunTimeManager _pythonRuntimeManager { get; set; }
        public static PyModule _persistentScope { get; set; }
        public static dynamic RunPythonScriptWithResult(string script, dynamic parameters)
        {
            dynamic result = null;
            if(_pythonRuntimeManager == null)
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
    }
}
