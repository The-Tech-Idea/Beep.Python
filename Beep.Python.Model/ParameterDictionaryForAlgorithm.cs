using System;
using System.Collections.Generic;
using System.Text;

namespace Beep.Python.Model
{
    public class ParameterDictionaryForAlgorithm
    {
        public string ParameterName { get; set; }
        public MachineLearningAlgorithm Algorithm { get; set; }
        public string Description { get; set; }  // Added Description property
        public string Example { get; set; }
    }
}
