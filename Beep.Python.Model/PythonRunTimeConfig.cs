using System.Collections.ObjectModel;
using TheTechIdea.Beep.Editor;

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
        private ObservableBindingList<FolderStructure> _folders = new ObservableBindingList<FolderStructure>();
        public ObservableBindingList<FolderStructure> Folders
        {
            get { return _folders; }
            set
            {
                _folders = value;
                SetProperty(ref _folders, value);
            }
        }
        public ObservableBindingList<PythonRunTime> Runtimes { get; set; }= new ObservableBindingList<PythonRunTime>();
        //  public int RunTimeIndex { get; set; } = -1;
        public int RunTimeIndex { get; set; } = -1;
        
      

    }
}
