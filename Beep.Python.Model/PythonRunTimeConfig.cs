using System.Collections.ObjectModel;

namespace Beep.Python.Model
{
    public class PythonConfiguration
    {
        public PythonConfiguration()
        {
            
        }
        public string PackageOfflinepath { get; set; } = string.Empty;
        public List<FolderStructure> Folders { get; set; } = new List<FolderStructure>();
        public List<PythonRunTime> Runtimes { get; set; }= new List<PythonRunTime>();
        //  public int RunTimeIndex { get; set; } = -1;
        public int RunTimeIndex { get; set; } = -1;
        //    {
        //        if (RunTimeIndex >= 0)
        //        {
        //            return Runtimes[RunTimeIndex];
        //        }else
        //            return null;
                 
        //    } 
        //} 


    }
}
