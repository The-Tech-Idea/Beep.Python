using System;
using System.Collections.Generic;
using System.Text;

namespace Beep.Python.Model
{
    public class ContourPlotData
    {
        public double[] x { get; set; }
        public double[] y { get; set; }
        public double[] z { get; set; }
        public string picfile { get; set; }
        public string xLabel { get; set; }
        public string yLabel { get; set; }
        public string title { get; set; }
        public string zfield { get; set; }
        public List<Tuple<double, double, string>> pointTitles { get; set; } = new List<Tuple<double, double, string>>();
    }
}
