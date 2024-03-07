using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    public class GenericLOVData
    {
        public GenericLOVData()
        {
            GuidID=Guid.NewGuid().ToString();
        }
        public string ID { get; set; }
        public string LOVNAME { get; set; }
        public string DisplayValue { get; set; }
        public string LOVVALUE { get; set; }
        public double DoubleValue { get; set; }
        public float FloatValue { get; set; }
        public DateTime DateValue { get; set; }
        public DateTime TimeValue { get; set; }
        public DateTime TimestampValue { get; set; }

        public string LOVDESCRIPTION { get; set; }  
        public string GuidID { get; set; }

    }
}
