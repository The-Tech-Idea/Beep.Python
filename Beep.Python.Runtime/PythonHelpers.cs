using Beep.Python.Model;
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
        public static IPythonRunTimeManager _pythonRuntimeManager { get; set; }
        public static PyModule _persistentScope { get; set; }
       

        // Helper method to flatten your Z array if it's 2D
        public static double[] FlattenZArray(double[,] z)
        {
            int numRows = z.GetLength(0);
            int numCols = z.GetLength(1);
            double[] z1D = new double[numRows * numCols];

            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols; j++)
                {
                    z1D[i * numCols + j] = z[i, j];
                }
            }
            return z1D;
        }
    }
}
