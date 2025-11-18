using System.Collections.ObjectModel;
 

namespace Beep.Python.Model
{
    public class PythonConfiguration:Entity
    {
        public PythonConfiguration()
        {
            
        }
        private string _packgeofflinepath= null;
        public string PackageOfflinepath
        {
            get { return _packgeofflinepath; }
            set
            {
                _packgeofflinepath = value;
                SetProperty(ref _packgeofflinepath, value);
            }
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
        public List<PythonRunTime> Runtimes { get; set; }= new List<PythonRunTime>();
        //  public int RunTimeIndex { get; set; } = -1;
        public int RunTimeIndex { get; set; } = -1;
        
      

    }
}
