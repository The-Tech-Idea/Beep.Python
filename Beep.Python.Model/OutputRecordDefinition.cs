using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beep.Python.Model
{
    public class OutputList{
        public OutputList()
        {
             _commandTheme = new OutputTheme();
             _outputTheme = new OutputTheme();
            _commandTheme.BackColor = Color.White;
            _commandTheme.TextColor = Color.Black;
            _commandTheme.IsBold = true;
            _outputTheme.TextColor = Color.Orange;
            _outputTheme.BackColor = Color.Blue;

        }
      

        public string Name { get; private set; }
        public ObservableCollection<OutputRecordDefinition> OutputRecordDefinitions { get; set; } = new ObservableCollection<OutputRecordDefinition>();
        OutputTheme _commandTheme;
        OutputTheme _outputTheme;
        public OutputTheme CommandTheme { get { return _commandTheme; } set { _commandTheme = value; } }
        public OutputTheme OutputTheme { get { return _outputTheme; } set { _outputTheme = value; } }
    }
    public class OutputRecordDefinition
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Format { get; set; }
        public int Id { get; set; }
        public OutputRecordType OutputRecordType { get; set; }
       
     }
    public enum OutputRecordType
    {
       Command,OutMessege
    }
    public class OutputTheme
    {
        public Color TextColor { get; set; }=Color.Black;
        public Color BackColor { get; set; }=Color.White;
        public bool IsBold { get; set; }=false;
        public bool IsItalic { get; set; }=false;
        public bool IsUnderline { get; set; } = false;
        public bool IsStrikeout { get; set; }= false;
        public bool IsVisible { get; set; } = true;
        public bool IsReadOnly { get; set; } = false;
        public bool IsEnabled { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool IsExpanded { get; set; } = false;
        public bool IsChecked { get; set; } = false;
        public bool IsVisibleInOutputWindow { get; set; } = false;
    }
}
