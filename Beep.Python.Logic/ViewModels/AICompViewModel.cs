using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TheTechIdea.Util;
using TheTechIdea.Beep.Editor;
using DataManagementModels.Editor;
using Beep.Python.RuntimeEngine;
using Beep.Python.Model;
using Python.Runtime;




namespace Beep.Python.Logic.ViewModels
{
    public partial class AICompViewModel : PythonBaseViewModel, IDisposable
    {
        [ObservableProperty]
        string compFilePath;
        [ObservableProperty]
        string modelsFilePath;
        [ObservableProperty]
        string aiCompFileDirName;

        [ObservableProperty]
        bool isChanged=false;
      
        [ObservableProperty]
        PythonDataClasses currentDataClass;
        [ObservableProperty]
        string filenamepath;
        [ObservableProperty]
        string testfilenamepath;
        [ObservableProperty]
        string validationfilenamepath;
        [ObservableProperty]
        string trainingfilenamepath;
        [ObservableProperty]
        string labelField;
        [ObservableProperty]
        string[] features;
        [ObservableProperty]
        List<GenericLOVData> labelFeatures =new List<GenericLOVData>();
        [ObservableProperty]
        List<GenericLOVData> primaryKeyFeatures = new List<GenericLOVData>();
        [ObservableProperty]
        string keyfield;
        public UnitofWork<PythonDataClasses> UnitofWork;
        private bool disposedValue;

        public IPythonMLManager PythonMLManager { get; set; }
        //public IAICompManager AICompManager { get; set; }
        public ObservableBindingList<PythonDataClasses> DataClasses => UnitofWork.Units;

        public bool IsInit { get; private set; }

        public AICompViewModel(PythonNetRunTimeManager pythonRuntimeManager, PyModule persistentScope) : base(pythonRuntimeManager, persistentScope)
        {
         
            UnitofWork = new UnitofWork<PythonDataClasses>(Editor,"dhubdb","AI_DATACLASSES", "ID");
            UnitofWork.Sequencer = "AI_DATACLASSES_SEQ";
         
            UnitofWork.PostCreate += DataClassesunitofWork_PostCreate;
          
            aiCompFileDirName = "AIComp";
            GetCompFilePath();
          
        }
        public bool InitPythonModule()
        {
            try
            {
                PythonMLManager = new PythonMLManager((PythonNetRunTimeManager)Editor.GetPythonRunTimeManager());
                PythonMLManager.ImportPythonModule("numpy as np");
                PythonMLManager.ImportPythonModule("pandas as pd");
                IsInit = true;
                return true;
            }
            catch (Exception ex)
            {
                Editor.AddLogMessage("Beep", $"Error in Python Init - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                IsInit = false;
                return false;
            }

        }
        public void ShutDownPython()
        {
            PythonMLManager.Dispose();
        }
        public void GetCompFilePath()
        {
            if(!Directory.Exists(Path.Combine(libraryManagerViewModel.GlobalPath, AiCompFileDirName)))
            {
                Directory.CreateDirectory(Path.Combine(libraryManagerViewModel.GlobalPath, AiCompFileDirName));
            }
           
            if (CurrentDataClass != null)
            {
                if (!Directory.Exists(Path.Combine(libraryManagerViewModel.GlobalPath, AiCompFileDirName, CurrentDataClass.ID.ToString())))
                {
                    Directory.CreateDirectory(Path.Combine(libraryManagerViewModel.GlobalPath, AiCompFileDirName, CurrentDataClass.ID.ToString()));
                }
                CurrentDataClass.URLPATH = Path.Combine(libraryManagerViewModel.GlobalPath, AiCompFileDirName, CurrentDataClass.ID.ToString());
            }
        }
        public void Get()
        {
            UnitofWork.Get();

        }
        public void Get(double dataclassid)
        {
            UnitofWork.Get(new List<TheTechIdea.Beep.Report.AppFilter>() { new TheTechIdea.Beep.Report.AppFilter() {  FieldName="ID", FilterValue=dataclassid.ToString(), Operator="="} });
            if(UnitofWork.Units.Count>0)
            {
                CurrentDataClass = UnitofWork.Units[0];
                Trainingfilenamepath = CurrentDataClass.TRAININGFILENAME;
                Testfilenamepath = CurrentDataClass.TESTDATAFILENAME;
                Validationfilenamepath = CurrentDataClass.VALIDATIONDATAFILENAME;
                Filenamepath = CurrentDataClass.FILENAME;
                if(CurrentDataClass.FEATURES!=null ) {
                    Features = CurrentDataClass.FEATURES.Split(',');
                }
               
                GetFeatures();
               
                
            }
        }
        private void DataClassesunitofWork_PostCreate(object? sender, UnitofWorkParams e)
        {
            PythonDataClasses doc = (PythonDataClasses)sender;
            
           
        }
        public PythonDataClasses CreateDataClass()
        {
            PythonDataClasses aI_DATACLASSES = new PythonDataClasses();
            UnitofWork.Create(aI_DATACLASSES);
            CurrentDataClass = aI_DATACLASSES;
            IsChanged = true;
            GetCompFilePath();
            return CurrentDataClass;
        }
        public void UpdateDataClass(string name, string datafilename, string filepath)
        {
            try
            {
               string classpath = Path.Combine(aiCompFileDirName, name);
                Editor.ErrorObject.Ex = null;
                Editor.ErrorObject.Flag = Errors.Ok;
                if (File.Exists(datafilename))
                {
                    if (currentDataClass != null)
                    {
                        libraryManagerViewModel.MoveFileToGlobleLibrary(datafilename, filepath, classpath, true);
                        CurrentDataClass.URLPATH = classpath;
                        CurrentDataClass.FILENAME = datafilename;
                        CurrentDataClass.NAME = name;
                        UnitofWork.Update(CurrentDataClass);
                        UnitofWork.Commit();
                    }
                   
                }

                
            }
            catch (Exception ex)
            {
                Editor.ErrorObject.Ex = ex;
                Editor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} -  {ex.Message}";
                Editor.ErrorObject.Flag = Errors.Failed;
            }
            //  return Editor.ErrorObject;
        }
        public void UpdateDataClass(PythonDataClasses dlc,string name, string datafilename, string filepath)
        {
            try
            {
                string classpath = Path.Combine(aiCompFileDirName, name);
                Editor.ErrorObject.Ex = null;
                Editor.ErrorObject.Flag = Errors.Ok;
                if (File.Exists(datafilename))
                {
                    libraryManagerViewModel.MoveFileToGlobleLibrary(datafilename, filepath, classpath, true);
                    dlc.URLPATH = classpath;
                    dlc.FILENAME = datafilename;
                    dlc.NAME = name;
                }

                UnitofWork.Update(dlc);
              //  UnitofWork.Commit();
            }
            catch (Exception ex)
            {
                Editor.ErrorObject.Ex = ex;
                Editor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} -  {ex.Message}";
                Editor.ErrorObject.Flag = Errors.Failed;
            }
            //  return Editor.ErrorObject;
        }
        public void AddDataClass(string name,string datafilename,string filepath,bool meonly=true)
        {
            try
            {
                string classpath = Path.Combine(aiCompFileDirName, name);
                Editor.ErrorObject.Ex = null;
                Editor.ErrorObject.Flag = Errors.Ok;
                PythonDataClasses  dlc =new PythonDataClasses();
                if(File.Exists(datafilename))
                {
                    libraryManagerViewModel.MoveFileToGlobleLibrary(datafilename, filepath, classpath, true);
                    dlc.URLPATH = classpath;
                    dlc.FILENAME = datafilename;
                    dlc.NAME = name;
                }
                
                UnitofWork.Create(dlc);
               // UnitofWork.Commit();
            }
            catch (Exception ex)
            {
                Editor.ErrorObject.Ex = ex;
                Editor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} -  {ex.Message}";
                Editor.ErrorObject.Flag = Errors.Failed;
            }
            //  return Editor.ErrorObject;
        }
        public bool Split(float splitratio)
        {
            try
            {
                InitPythonModule();
                string filename = Path.Combine(CurrentDataClass.URLPATH, CurrentDataClass.FILENAME);
                string Traindatafilename = Path.Combine(CurrentDataClass.URLPATH, CurrentDataClass.TRAININGFILENAME);
                string Testdatafilename = Path.Combine(CurrentDataClass.URLPATH, CurrentDataClass.TESTDATAFILENAME);
                string Validatedatafilename = Path.Combine(CurrentDataClass.URLPATH, CurrentDataClass.VALIDATIONDATAFILENAME);
                Features = PythonMLManager.SplitData(filename , splitratio,Traindatafilename, Testdatafilename, Validatedatafilename,CurrentDataClass.PRIMARYFIELD,CurrentDataClass.LABELFIELD);
                CurrentDataClass.FEATURES = string.Join(",", Features);
                GetFeatures();
               
                return true;
            }
            catch (Exception ex)
            {
            
                Editor.AddLogMessage("Beep", $"Error in Setup Training  - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }
        }
       
        public bool GetFeatures()
        {
            try
            {
                if (Features == null)
                {
                    string filename;
                    if (!string.IsNullOrEmpty(CurrentDataClass.FILENAME))
                    {
                        filename = Path.Combine(CurrentDataClass.URLPATH, CurrentDataClass.FILENAME);
                    }
                    else
                    {
                        filename = Path.Combine(CurrentDataClass.URLPATH, CurrentDataClass.TRAININGFILENAME);
                    }
                    GetFeatures(filename);
                    CurrentDataClass.FEATURES = string.Join(",", Features);
                }
                if (CurrentDataClass.FEATURES != null)
                {
                    Features = CurrentDataClass.FEATURES.Split(',');
                }
                if (LabelFeatures == null || LabelFeatures.Count==0)
                {
                 
                    PrimaryKeyFeatures = new List<GenericLOVData>();
                    LabelFeatures = new List<GenericLOVData>();

               
                    for (int i = 0; i < Features.Length - 1; i++)
                    {
                        GenericLOVData x = new GenericLOVData() { ID = Features[i], DisplayValue = Features[i] };
                        LabelFeatures.Add(x);
                        PrimaryKeyFeatures.Add(x);
                      
                    }
                    Keyfield = Features[Features.Length - 1];
                    LabelField = Features[Features.Length - 1];

                }
              
              
                return true;
            }
            catch (Exception ex)
            {

                Editor.AddLogMessage("Beep", $"Error in Setup Training  - {ex.Message}", DateTime.Now, -1, null, TheTechIdea.Util.Errors.Failed);
                return false;
            }
        }
        private string CreateSplitFile(string path, string prefix, string originalFileName, string[] data)
        {
            string newFileName = Path.Combine(path, $"{prefix}{originalFileName}");
            File.WriteAllLines(newFileName, data);
            return newFileName;
        }
        private void CreateValidationFile(string path, string sourceFileName)
        {
            string validationFileName = Path.Combine(path, $"validation_{Path.GetFileName(sourceFileName)}");
            File.Copy(sourceFileName, validationFileName);

            string[] lines = File.ReadAllLines(validationFileName);
            ClearLabelColumn(lines);
            File.WriteAllLines(validationFileName, lines);

            CurrentDataClass.VALIDATIONDATAFILENAME = Path.GetFileName(validationFileName);
        }
        private void ClearLabelColumn(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');

                if (columns.Length > 0)
                {
                    columns[columns.Length - 1] = ""; // Clearing the label column
                }

                lines[i] = string.Join(",", columns);
            }
        }
        public void SplitDataClassFilev1(PythonDataClasses dataClasses, double splitRatio)
        {
            try
            {
                ValidateSplitRatio(ref splitRatio); // Ensuring split ratio is valid

                string dataFilePath = Path.Combine(dataClasses.URLPATH, dataClasses.FILENAME);
                Editor.ErrorObject.Ex = null;
                Editor.ErrorObject.Flag = Errors.Ok;

                if (!File.Exists(dataFilePath))
                {
                   
                    Editor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} File doesnot exist";
                    Editor.ErrorObject.Flag = Errors.Failed;
                    return;
                }

                string[] lines = File.ReadAllLines(dataFilePath);
                ShuffleData(lines); // Shuffling the data

                int totalLines = lines.Length;
                int trainingLinesCount = (int)(totalLines * splitRatio);
                int testingLinesCount = totalLines - trainingLinesCount;

                string[] trainingData = lines.Take(trainingLinesCount).ToArray();
                string[] testingData = lines.Skip(trainingLinesCount).Take(testingLinesCount).ToArray();

                string trainingFileName = CreateSplitFile(dataClasses.URLPATH, "train_", dataClasses.FILENAME, trainingData);
                string testingFileName = CreateSplitFile(dataClasses.URLPATH, "test_", dataClasses.FILENAME, testingData);

                dataClasses.TRAININGFILENAME = Path.GetFileName(trainingFileName);
                dataClasses.TESTDATAFILENAME = Path.GetFileName(testingFileName);

                CreateValidationFile(dataClasses.URLPATH, testingFileName);
            }
            catch (Exception ex)
            {
                Editor.ErrorObject.Ex = ex;
                Editor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} -  {ex.Message}";
                Editor.ErrorObject.Flag = Errors.Failed;
            }
        }
        private void ShuffleData(string[] lines)
        {
            if (lines.Length <= 1) return; // No need to shuffle if only header or no data

            var random = new Random();
            for (int i = lines.Length - 1; i > 0; i--)
            {
                int j = random.Next(1, i + 1); // Start from 1 to skip the header row
                var temp = lines[j];
                lines[j] = lines[i];
                lines[i] = temp;
            }
        }
        private void ValidateSplitRatio(ref double splitRatio)
        {
            // Define the acceptable range for the split ratio
            const double minRatio = 0.6; // 60%
            const double maxRatio = 0.8; // 80%

            // Check if the split ratio is within the acceptable range
            if (splitRatio < minRatio || splitRatio > maxRatio)
            {
                Editor.AddLogMessage("Beep", $"Split ratio must be between {minRatio * 100}% and {maxRatio * 100}%", DateTime.Now, -1, null, Errors.Failed);
            }
        }
        public void GetFeatures(PythonDataClasses dATACLASSES)
        {
            string datafilename = Path.Combine(dATACLASSES.URLPATH, dATACLASSES.TRAININGFILENAME);
            GetFeatures(datafilename);
        }
        public void GetFeatures(string datafilename)
        {

            string[] lines = File.ReadAllLines(datafilename);
            string line = lines[0];
            string[] cols = line.Split(',');
            PrimaryKeyFeatures = new List<GenericLOVData>();
            LabelFeatures = new List<GenericLOVData>();

            Features = new string[cols.Length - 1];
            for (int i = 0; i < cols.Length - 1; i++)
            {
                GenericLOVData x = new GenericLOVData() { ID = cols[i], DisplayValue = cols[i] };
                LabelFeatures.Add(x);
                PrimaryKeyFeatures.Add(x);
                Features[i] = cols[i];
            }
            Keyfield = cols[cols.Length - 1];
            LabelField = cols[cols.Length - 1];
        }
        public void SplitDataClassFile(PythonDataClasses dATACLASSES, double splitRatio)
        {
            try
            {
                string classpath = dATACLASSES.URLPATH;
                string datafilename = Path.Combine(dATACLASSES.URLPATH,dATACLASSES.FILENAME);
                Editor.ErrorObject.Ex = null;
                Editor.ErrorObject.Flag = Errors.Ok;
                if (File.Exists(datafilename))
                {
                    // split data file using split ratio
                    // save the split data file in the same folder
                    // update the dataclass with the new file name
                    // generate 2 files one for training and one for testing
                    // save the file names in the dataclass
                    // all files are csv files
                    // Get the total number of lines in the file
                    // Get the number of lines for training and testing
                    splitRatio = splitRatio / 100;
                    string[] lines = File.ReadAllLines(datafilename);
                    // get the feature names from the first line
                    // get the label name from the last column
                       string line = lines[0];
                        string[] cols = line.Split(',');
                        if (Features == null)
                        {
                            Features = new string[cols.Length - 1];
                            for (int i = 0; i < cols.Length - 1; i++)
                            {
                                Features[i] = cols[i];
                            }
                        }
                        if (Keyfield == null)
                        {
                            Keyfield = cols[cols.Length - 1];
                        }
                        if (LabelField == null)
                        {
                            LabelField = cols[cols.Length - 1];
                        }
                    
                    int total = lines.Length;
                    int train = (int)(total * splitRatio);
                    int test = total - train;
                    string[] traindata = new string[train];
                    string[] testdata = new string[test];
                    for (int i = 0; i < total; i++)
                    {
                        if (i < train)
                        {
                            traindata[i] = lines[i];
                        }
                        else
                        {
                            testdata[i - train] = lines[i];
                        }
                    }
                    string trainfilename = Path.Combine(classpath, "train_" + dATACLASSES.FILENAME);
                    string testfilename = Path.Combine(classpath, "test_" + dATACLASSES.FILENAME);
                    File.WriteAllLines(trainfilename, traindata);
                    File.WriteAllLines(testfilename, testdata);
                    // Create the training and testing files
                    dATACLASSES.TRAININGFILENAME = "train_" + dATACLASSES.FILENAME;
                    dATACLASSES.TESTDATAFILENAME = "test_" + dATACLASSES.FILENAME;
                    // copy testfile to validation file and rename it and update label field value to null
                    string validationfilename = Path.Combine(classpath, "validation_" + dATACLASSES.FILENAME);
                    
                    File.Copy(testfilename, validationfilename);
                    //update the label column field value to null in  validation file
                    string[] vlines = File.ReadAllLines(validationfilename);
                    for (int i = 0; i < vlines.Length; i++)
                    {
                        string[] vcols = vlines[i].Split(',');
                        vcols[vcols.Length - 1] = "";
                        vlines[i] = string.Join(",", vcols);
                    }
                    File.WriteAllLines(validationfilename, vlines);

                    dATACLASSES.VALIDATIONDATAFILENAME = "validation_" + dATACLASSES.FILENAME;
                    
                    

                }

                UnitofWork.Update(dATACLASSES);
               // UnitofWork.Commit();
            }
            catch (Exception ex)
            {
                Editor.ErrorObject.Ex = ex;
                Editor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} -  {ex.Message}";
                Editor.ErrorObject.Flag = Errors.Failed;
            }
            //  return Editor.ErrorObject;
        }
        public void SubmitResultFile(PythonDataClasses dATACLASSES,string filename_w_path)
        {
            try
            {
                string classpath = dATACLASSES.URLPATH;
                string datafilename = Path.Combine(dATACLASSES.URLPATH, dATACLASSES.FILENAME);

                Editor.ErrorObject.Ex = null;
                Editor.ErrorObject.Flag = Errors.Ok;
            }
            catch (Exception ex)
            {
                Editor.ErrorObject.Ex = ex;
                Editor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} -  {ex.Message}";
                Editor.ErrorObject.Flag = Errors.Failed;
            }
        
        }     
        [RelayCommand]
        public void GetDataClasses()
        {
            try
            {
                Editor.ErrorObject.Ex = null;
                Editor.ErrorObject.Flag = Errors.Ok;
                UnitofWork.Get();
            }
            catch (Exception ex)
            {
                Editor.ErrorObject.Ex = ex;
                Editor.ErrorObject.Message = $"Error in  {System.Reflection.MethodBase.GetCurrentMethod().Name} -  {ex.Message}";
                Editor.ErrorObject.Flag = Errors.Failed;
            }
            //  return Editor.ErrorObject;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    PythonMLManager.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AICompViewModel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
