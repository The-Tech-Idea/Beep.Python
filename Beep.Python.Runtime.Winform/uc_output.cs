using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Beep.Python.Winform
{
    public partial class uc_output : UserControl
    {
        public uc_output()
        {
            InitializeComponent();
        }
        int Sequence = 0;
        public OutputList outputList { get; set; }=new OutputList();
        public void AddOutputRecordDefinition(OutputRecordDefinition outputRecordDefinition)
        {
            outputList.OutputRecordDefinitions.Add(outputRecordDefinition);
        }
        public void AddOutputRecordDefinition(string name, string type, string description, string format, int id, OutputRecordType outputRecordType)
        {
            OutputRecordDefinition outputRecordDefinition = new OutputRecordDefinition();
            outputRecordDefinition.Name = name;
            outputRecordDefinition.Type = type;
            outputRecordDefinition.Description = description;
            outputRecordDefinition.Format = format;
            Sequence += 1;
            outputRecordDefinition.Id = Sequence;
            outputRecordDefinition.OutputRecordType = outputRecordType;
            outputList.OutputRecordDefinitions.Add(outputRecordDefinition);
        }
    }
}
