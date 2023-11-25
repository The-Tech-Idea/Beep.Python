using System;
using System.Collections.Generic;
using System.Text;

namespace Beep.Python.Model
{
    public class FoldersList
    {
        public FoldersList()
        {
            
        }
        public List<FolderStructure> Folders { get; set; } = new List<FolderStructure>();
    }
    public class FolderStructure
    {
        public string _foldername { get; set; }
        public string _folderpath { get; set; }

        public bool _pythonxexist { get; set; }
        public string xversion { get; set; }



        public string _folder32x { get; set; }
        public string _folder64x { get; set; }
        public bool _folder32xexist { get; set; }
        public bool _folder64xexist { get; set; }
        public bool _python32xexist { get; set; }
        public bool _python64xexist { get; set; }
        public string _folder32xversion { get; set; }
        public string _folder64xversion { get; set; }
        public string _folder32xversiondisplay { get; set; }
        public string _folder64xversiondisplay { get; set; }

      
        public FolderStructure() { }
    }
    public enum FolderIs
    {
        ParentFolder,x32,x64,None
    }
}
