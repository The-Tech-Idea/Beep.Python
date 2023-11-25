using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Beep.Python.Winform
{
    public class BeepJupyterControl : TableLayoutPanel
    {
        private List<TextBox> codeCells;
        private IPythonRunTimeManager _pythonEngine;
        public void AddNewCell()
        {
            TextBox newCell = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill
            };
            codeCells.Add(newCell);

            RowCount += 1;
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(newCell, 0, RowCount - 1);
        }

        public void ExecuteCurrentCell()
        {
            TextBox currentCell = codeCells[RowCount - 1];
            string code = currentCell.Text;
            string result = PythonNetManager.Execute(code);
            AddOutput(result);
            AddNewCell();
        }

        private void AddOutput(string output)
        {
            TextBox outputBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Text = output,
                Dock = DockStyle.Fill
            };

            RowCount += 1;
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(outputBox, 0, RowCount - 1);
        }
    }
}
