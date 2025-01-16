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
        public string Foldername { get; set; }
        public string Folderpath { get; set; }

        public bool Pythonxexist { get; set; }
        public string Xversion { get; set; }



        public string Folder32x { get; set; }
        public string Folder64x { get; set; }
        public bool Folder32xexist { get; set; }
        public bool Folder64xexist { get; set; }
        public bool Python32xexist { get; set; }
        public bool Python64xexist { get; set; }
        public string Folder32xversion { get; set; }
        public string Folder64xversion { get; set; }
        public string Folder32xversiondisplay { get; set; }
        public string Folder64xversiondisplay { get; set; }

      
        public FolderStructure() { }
    }
    public enum FolderIs
    {
        ParentFolder,x32,x64,None
    }
}
