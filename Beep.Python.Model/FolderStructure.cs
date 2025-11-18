using System;
using System.Collections.Generic;
using System.Text;
 

namespace Beep.Python.Model
{
    public class FoldersList:Entity
    {
        public FoldersList()
        {
            
        }
        private List<FolderStructure> _folders = new List<FolderStructure>();
        public List<FolderStructure> Folders
        {
            get { return _folders; }
            set
            {
                _folders = value;
                SetProperty(ref _folders, value);
            }
        }

    }
    public class FolderStructure:Entity
    {
        // convert all properties to entity properties like FoldersList prperties

        private string _foldername;
        public string Foldername
        {
            get { return _foldername; }
            set
            {
                _foldername = value;
                SetProperty(ref _foldername, value);
            }
        }
       private string _folderpath;
        public string Folderpath
        {
            get { return _folderpath; }
            set
            {
                _folderpath = value;
                SetProperty(ref _folderpath, value);
            }
        }
        private bool _pythonexist;
        public bool Pythonexist
        {
            get { return _pythonexist; }
            set
            {
                _pythonexist = value;
                SetProperty(ref _pythonexist, value);
            }
        }
       private string _folderversion;
        public string Folderversion
        {
            get { return _folderversion; }
            set
            {
                _folderversion = value;
                SetProperty(ref _folderversion, value);
            }
        }



        private string _folder32x;
        private string _folder64x;
        public string Folder32x
        {
            get { return _folder32x; }
            set
            {
                _folder32x = value;
                SetProperty(ref _folder32x, value);
            }
        }
        public string Folder64x
        {
            get { return _folder64x; }
            set
            {
                _folder64x = value;
                SetProperty(ref _folder64x, value);
            }
        }
        private bool _folder32xexist;
        private bool _folder64xexist;
        public bool Folder32xexist
        {
            get { return _folder32xexist; }
            set
            {
                _folder32xexist = value;
                SetProperty(ref _folder32xexist, value);
            }
        }
        public bool Folder64xexist
        {
            get { return _folder64xexist; }
            set
            {
                _folder64xexist = value;
                SetProperty(ref _folder64xexist, value);
            }
        }

        private bool _python32xexist;
        private bool _python64xexist;
        public bool Python32xexist
        {
            get { return _python32xexist; }
            set
            {
                _python32xexist = value;
                SetProperty(ref _python32xexist, value);
            }
        }
        public bool Python64xexist
        {
            get { return _python64xexist; }
            set
            {
                _python64xexist = value;
                SetProperty(ref _python64xexist, value);
            }
        }
        private string _folder32xversion;
        private string _folder64xversion;
        public string Folder32xversion
        {
            get { return _folder32xversion; }
            set
            {
                _folder32xversion = value;
                SetProperty(ref _folder32xversion, value);
            }
        }
        public string Folder64xversion
        {
            get { return _folder64xversion; }
            set
            {
                _folder64xversion = value;
                SetProperty(ref _folder64xversion, value);
            }
        }
        private string _folder32xversiondisplay;
        private string _folder64xversiondisplay;
        public string Folder32xversiondisplay
        {
            get { return _folder32xversiondisplay; }
            set
            {
                _folder32xversiondisplay = value;
                SetProperty(ref _folder32xversiondisplay, value);
            }
        }
        public string Folder64xversiondisplay
        {
            get { return _folder64xversiondisplay; }
            set
            {
                _folder64xversiondisplay = value;
                SetProperty(ref _folder64xversiondisplay, value);
            }
        }
     
        public FolderStructure() { }
    }
    public enum FolderIs
    {
        ParentFolder,x32,x64,None
    }
}
