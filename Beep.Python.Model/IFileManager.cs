namespace Beep.Python.Model
{
    public interface IFileManager
    {
        string FilenameLoaded { get; set; }
        bool IsConfigLoaded { get; set; }
        byte[] LastTmpcsvhash { get; set; }
        string Tmpcsvfile { get; set; }

        void CreatedHashTmp();
        void CreateLoadConfig();
        string Lookfortmopcsv();
        void SaveConfig(PythonRunTime config);
    }
}