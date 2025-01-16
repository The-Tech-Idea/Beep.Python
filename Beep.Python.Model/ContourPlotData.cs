using System;
using System.Collections.Generic;
using System.Text;

namespace Beep.Python.Model
{
    public class ContourPlotData
    {
        public double[] X { get; set; }
        public double[] Y { get; set; }
        public double[] Z { get; set; }
        public string Picfile { get; set; }
        public string XLabel { get; set; }
        public string YLabel { get; set; }
        public string Title { get; set; }
        public string Zfield { get; set; }
        public List<Tuple<double, double, string>> PointTitles { get; set; } = new List<Tuple<double, double, string>>();
    }
}
