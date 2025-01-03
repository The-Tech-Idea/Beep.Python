using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    /// <summary>
    /// Event args for Python errors containing both .NET and Python information.
    /// </summary>
    public class PythonErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public string PythonTraceback { get; set; }
        public Exception DotNetException { get; set; }
    }
}
