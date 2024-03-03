using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep;


namespace Beep.Python.RuntimeEngine
{
    public static class PythonRunTimeDiagnostics
    {
        [System.Runtime.InteropServices.DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static BinType32or64 GetDllArchitecture(string dllPath)
        {
            // Read the first bytes of the DLL to determine if it's 32-bit or 64-bit
            using (var stream = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new BinaryReader(stream))
                {
                    stream.Seek(0x3C, SeekOrigin.Begin);
                    int peOffset = reader.ReadInt32();
                    stream.Seek(peOffset, SeekOrigin.Begin);
                    reader.ReadUInt32(); // "PE\0\0"
                    var machine = reader.ReadUInt16();

                    switch (machine)
                    {
                        case 0x8664: // x64
                            return BinType32or64.p395x64;
                        case 0x14C: // x86
                            return  BinType32or64.p395x32;
                        default:
                            return  BinType32or64.Unknown;
                    }
                }
            }
        }

        public static bool CheckNet()
        {
            int desc;
            return InternetGetConnectedState(out desc, 0);
        }
        public static List<FolderStructure> Folders { get; set; } = new List<FolderStructure>();
        public static string Bin32FolderName { get; set; } = "x32";
        public static string Bin64FolderName { get; set; } = "x64";
        public static string PythonVersion { get; set; } = "3.10";
        public static string Bin64FolderPath { get; set; }
        public static string Bin32FolderPath { get; set; }
        public static void SetFolderNames(string bin32FolderName, string bin64FolderName)
        {
            bin32FolderName = bin32FolderName.ToLower();
            bin64FolderName = bin64FolderName.ToLower();
        }
        public static bool IsFoldersExist(string path)
        {

            if (FolderExist(path, BinType32or64.p395x32) || FolderExist(path, BinType32or64.p395x64))
                return true;
            else return false;

        }
        public static bool IsFileExist(string path)
        {
            return System.IO.File.Exists(path);
        }
        public static bool IsFileExist(string path, string fileName)
        {
            return System.IO.File.Exists(Path.Combine(path, fileName));
        }
        public static bool IsFileExist(string path, string fileName, string extension)
        {
            return System.IO.File.Exists(Path.Combine(path, fileName + "." + extension));
        }
        public static bool FolderExist(string path, BinType32or64 type32Or64)
        {

            string direname = GetPythonFolderName(path);
            if (direname.Equals(Bin64FolderName, StringComparison.CurrentCultureIgnoreCase) && type32Or64 == BinType32or64.p395x64)
            {
                return IsFileExist(path, "python.exe");
            }
            if (direname.Equals(Bin32FolderName, StringComparison.CurrentCultureIgnoreCase) && type32Or64 == BinType32or64.p395x32)
            {
                return IsFileExist(path, "python.exe");
            }
            if (type32Or64 == BinType32or64.p395x32)
            {
                return System.IO.Directory.Exists(Path.Combine(path, Bin32FolderName));
            }
            else
            {
                return System.IO.Directory.Exists(Path.Combine(path, Bin64FolderName));
            }

        }
        public static bool IsPythonInstalled(string path, BinType32or64 type32Or64)
        {
            string direname = GetPythonFolderName(path);
            if (direname.Equals(Bin32FolderName, StringComparison.CurrentCultureIgnoreCase) || direname.Equals(Bin64FolderName, StringComparison.CurrentCultureIgnoreCase))
            {
                return IsFileExist(path, "python.exe");
            }
            else
            {

                if (type32Or64 == BinType32or64.p395x32)
                {
                    return IsFileExist(Path.Combine(path, Bin32FolderName), "python.exe");
                }
                else
                {
                    return IsFileExist(Path.Combine(path, Bin64FolderName), "python.exe");
                }
            }



        }
        public static bool IsPythonInstalled(string path)
        {
            if (Directory.Exists(path))
            {
                bool exist = Directory.EnumerateFiles(path, "python*.dll").Any();
                return IsFileExist(path, "python.exe") && exist;
            }return false;
         
        }
        public static string GetPythonExe(string path)
        {
            if (IsCondaInstalled(path)!=null)
            {
                return Path.Combine(path, IsCondaInstalled(path));
            }
            if (IsPythonInstalled(path))
            {
                return  Path.Combine(path, "python.exe"); 
            }else
                return null;
        }
        public static string IsCondaInstalled(string path)
        {
           
            if( IsFileExist(path, "_conda.exe"))
            {
                return "_conda";
            }
            if (IsFileExist(path, "conda.exe"))
            {
                return "conda";
            }
            else
                return null;
        }
        public static PackageType GetPackageType(string path)
        {
            if (IsCondaInstalled(path) != null)
            {
                return PackageType.conda;
            }
            if (IsPythonInstalled(path))
            {
                return PackageType.pypi;
            }
            else
                return PackageType.None;
        }
        public static string GetPythonVersionFromPython(string path)
        {
            string version = string.Empty;
            if (IsPythonInstalled(path))
            {
                string folderpath = path;
                string direname = GetPythonFolderName(path);
               

                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = Path.Combine(folderpath, "python.exe");
                startInfo.Arguments = "--version";
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo = startInfo;
                process.Start();
                version = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            return version;
        }
        public static string GetPythonVersionFromDll(string path)
        {
            string version = "0";
            if (IsPythonInstalled(path))
            {
                string folderpath = path;
                string direname = GetPythonFolderName(path);
               
                string[] files = Directory.GetFiles(folderpath, "python*.dll");
                foreach (string dir in files)
                {
                    string name = Path.GetFileName(dir);
                    string rt = name.Replace("python", "");
                    rt = rt.Replace(".dll", "");
                    int v = Convert.ToInt16(rt);
                    int lastv = Convert.ToInt16(version);
                    if (v > lastv)
                    {
                        version = rt;
                    }

                }
            }
            return version;
        }
        public static string GetPythonFolderName(string path)
        {
            return new DirectoryInfo(path).Name;
        }
        public static string GetVersion(string path)
        {
            string folderpath = path;
          
            string version = "0";
            string[] files = Directory.GetFiles(folderpath, "python*.dll");
            foreach (string dir in files)
            {
                string name = Path.GetFileName(dir);
                string rt = name.Replace("python", "");
                rt = rt.Replace(".dll", "");
                int v = Convert.ToInt16(rt);
                int lastv = Convert.ToInt16(version);
                if (v > lastv)
                {
                    version = rt;
                }

            }
            return version;
        }
        public static BinType32or64 Get32Or64(string path)
        {
            if (IsPythonInstalled(path))
            {
                string folderpath = path;
                string direname = GetPythonFolderName(path);
                string version = string.Empty;
                string[] files = Directory.GetFiles(folderpath, "python*.dll");
                foreach (string dir in files)
                {
                    string name = Path.GetFileName(dir);
                    string rt = name.Replace("python", "");
                    rt = rt.Replace(".dll", "");
                    int v = Convert.ToInt16(rt);
                    int lastv = Convert.ToInt16(version);
                    if (v > lastv)
                    {
                        version = rt;
                    }

                }
                if (version != "0")
                {
                    string filename =Path.Combine(path,$"python{version}.dll");
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(filename);
                    // Load the assembly
                    Assembly assembly = Assembly.LoadFrom(filename);

                    // Get the processor architecture
                    ProcessorArchitecture architecture = assembly.GetName().ProcessorArchitecture;
                    // Check if the assembly was built for x86 or x64
                    if (architecture == ProcessorArchitecture.X86)
                    {
                        return BinType32or64.p395x32;
                    }
                    else if (architecture == ProcessorArchitecture.Amd64 || architecture == ProcessorArchitecture.IA64)
                    {
                        return BinType32or64.p395x64;
                    }
                }

            }
             return BinType32or64.Unknown;
        }
        //public static PythonRunTime GetPythonConfig(string path, BinType32or64 binType32Or64)
        //{
        //    PythonRunTime config = new PythonRunTime();
        //    config.IsPythonInstalled = false;
        //    string foldername = new DirectoryInfo(path).Name;
        //    string folderpath = string.Empty;
        //    string runtimepath = string.Empty;
        //    string binpath = string.Empty;
        //    bool Is32Exist = FolderExist(path, BinType32or64.p395x32);
        //    bool Is64Exist = FolderExist(path, BinType32or64.p395x64);
        //    bool IsPython32Installed = true;
        //    bool IsPython64Installed = true;
        //    if (!Is32Exist && !Is64Exist)
        //    {
        //        config.IsPythonInstalled = false;
        //        config.Message = "Python is not installed - Cannot Find Folders";
        //        return config;
        //    }


        //    if (!IsPython64Installed && !IsPython32Installed)
        //    {
        //        config.IsPythonInstalled = false;
        //        config.Message = "Python is not installed";
        //        return config;
        //    }
        //    string direname = GetPythonFolderName(path);
        //    if (direname.Equals(Bin32FolderName, StringComparison.CurrentCultureIgnoreCase) || direname.Equals(Bin64FolderName, StringComparison.CurrentCultureIgnoreCase))
        //    {
        //        folderpath = path;
        //        runtimepath = path.Substring(0, path.LastIndexOf('\\'));
        //    }
        //    else
        //    {

        //        if (binType32Or64 == BinType32or64.p395x32)
        //        {
        //            folderpath = Path.Combine(path, Bin32FolderName);
        //        }
        //        else
        //        {
        //            folderpath = Path.Combine(path, Bin64FolderName);
        //        }
        //        runtimepath = path;
        //    }
        //    if (binType32Or64 == BinType32or64.p395x32)
        //    {
        //        if (Is32Exist)
        //        {
        //            IsPython32Installed = IsPythonInstalled(path, BinType32or64.p395x32);
        //            if (IsPython32Installed)
        //            {

        //                config.IsPythonInstalled = true;
        //                Bin32FolderName = foldername;
        //            }
        //            if (!IsPython32Installed)
        //            {
        //                config.IsPythonInstalled = false;
        //                config.Message = "Python is not installed - Cannot Find Folder32X";
        //                return config;
        //            }
        //            binpath = folderpath;
        //        }

        //    }
        //    else
        //    {
        //        if (Is64Exist)
        //        {
        //            IsPython64Installed = IsPythonInstalled(path, BinType32or64.p395x64);
        //            if (IsPython64Installed)
        //            {
        //                config.IsPythonInstalled = true;
        //                Bin64FolderName = foldername;
        //            }
        //            if (!IsPython64Installed)
        //            {
        //                config.IsPythonInstalled = false;
        //                config.Message = "Python is not installed - Cannot Find Folder64X";
        //                return config;
        //            }
        //            binpath = folderpath;
        //        }

        //    }
        //    if (config.IsPythonInstalled)
        //    {
        //        config.RuntimePath = runtimepath;
        //        config.PythonVersion = GetPythonVersionFromDll(binpath);
        //        config.BinPath = folderpath;
        //        config.PythonDll = Path.Combine(config.BinPath, "python" + config.PythonVersion + ".dll");
        //        config.Packageinstallpath = Path.Combine(config.BinPath, "Lib", "site-packages");
        //        config.ScriptPath = Path.Combine(config.BinPath, "Scripts");
        //        config.BinType = binType32Or64;
        //        config.PackageType = GetPackageType(binpath);
        //    }



        //    return config;
        //}
        public static PythonRunTime GetPythonConfig(string path)
        {
            PythonRunTime config = new PythonRunTime();
            config.IsPythonInstalled = false;
            string foldername = new DirectoryInfo(path).Name;
            string folderpath = string.Empty;
            string runtimepath = string.Empty;
            string binpath = string.Empty;
            bool Is32Exist = FolderExist(path, BinType32or64.p395x32);
            bool Is64Exist = FolderExist(path, BinType32or64.p395x64);
            bool IsPython32Installed = true;
            bool IsPython64Installed = true;
            BinType32or64 binType32Or64 = BinType32or64.p395x32;
            //if (!Is32Exist && !Is64Exist)
            //{
            //    config.IsPythonInstalled = false;
            //    config.Message = "Python is not installed - Cannot Find Folders";
            //    return config;
            //}
            string version = GetVersion(path);
            string filename = Path.Combine(path, $"python{version}.dll");
            BinType32or64 retval= GetDllArchitecture(filename);
            if (retval == BinType32or64.p395x32)
            {
                IsPython32Installed = true;
                IsPython64Installed = false;
                Is32Exist = true; Is64Exist=false;
            }
            else
            {
                IsPython64Installed = true;
                IsPython32Installed = false;
                Is32Exist = false;Is64Exist=true    ;
            }
                
            if (!IsPython64Installed && !IsPython32Installed)
            {
                config.IsPythonInstalled = false;
                config.Message = "Python is not installed";
                return config;
            }
            string direname = GetPythonFolderName(path);
            if (direname.Equals(Bin32FolderName, StringComparison.CurrentCultureIgnoreCase) || direname.Equals(Bin64FolderName, StringComparison.CurrentCultureIgnoreCase))
            {
                folderpath = path;
                runtimepath = path.Substring(0, path.LastIndexOf('\\'));
            }

            if (Is32Exist)
            {
                //IsPython32Installed = IsPythonInstalled(path, BinType32or64.p395x32);
                //if (IsPython32Installed)
                //{

                //    config.IsPythonInstalled = true;
                //    Bin32FolderPath = folderpath;
                //}
                //if (!IsPython32Installed)
                //{
                //    config.IsPythonInstalled = false;
                //    config.Message = "Python is not installed - Cannot Find Folder32X";
                //    return config;
                //}
                binpath = folderpath;
                binType32Or64 = BinType32or64.p395x32;
                config.IsPythonInstalled = true;

            }

            if (Is64Exist)
            {
        ////        IsPython64Installed = IsPythonInstalled(path, BinType32or64.p395x64);
        //        if (IsPython64Installed)
        //        {
        //            config.IsPythonInstalled = true;
        //            Bin32FolderPath = folderpath;
        //        }
        //        if (!IsPython64Installed)
        //        {
        //            config.IsPythonInstalled = false;
        //            config.Message = "Python is not installed - Cannot Find Folder64X";
        //            return config;
        //        }
                binpath = folderpath;
                binType32Or64 = BinType32or64.p395x64;
                config.IsPythonInstalled=true;
            }


            if (config.IsPythonInstalled)
            {
                folderpath = path;
                runtimepath = path;
                config.RuntimePath = runtimepath;
                config.PythonVersion = version;
                config.BinPath = runtimepath;
                config.PythonDll = Path.Combine(config.BinPath, "python" + config.PythonVersion + ".dll");
                config.Packageinstallpath = Path.Combine(config.BinPath, "Lib", "site-packages");
                config.ScriptPath = Path.Combine(config.BinPath, "Scripts");
                config.Message = "Found Python";
                config.BinType = binType32Or64;
                config.PackageType = GetPackageType(binpath);
            }



            return config;
        }
        public static string SetAiFolderPath(IDMEEditor DMEditor)
        {
            string AiFolderpath = string.Empty;
            //if (!DMEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.Scripts && c.FolderPath.Contains("AI")).Any())
            //{
            //    if (Directory.Exists(Path.Combine(DMEditor.ConfigEditor.ExePath, "AI")) == false)
            //    {
            //        Directory.CreateDirectory(Path.Combine(DMEditor.ConfigEditor.ExePath, "AI"));

            //    }
            //    if (!DMEditor.ConfigEditor.Config.Folders.Any(item => item.FolderPath.Equals(Path.Combine(DMEditor.ConfigEditor.ExePath, "AI"), StringComparison.OrdinalIgnoreCase)))
            //    {
            //        DMEditor.ConfigEditor.Config.Folders.Add(new StorageFolders(Path.Combine(DMEditor.ConfigEditor.ExePath, "AI"), FolderFileTypes.Scripts));
            //    }
            //    AiFolderpath = Path.Combine(DMEditor.ConfigEditor.ExePath, "AI");
            //}
            //else
            //{
            //    AiFolderpath = DMEditor.ConfigEditor.Config.Folders.Where(c => c.FolderFilesType == FolderFileTypes.Scripts && c.FolderPath.Contains("AI")).FirstOrDefault().FolderPath;
            //}
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AI")) == false)
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AI"));

            }
            return AiFolderpath;
        }
        public static FolderIs CheckPathStatus(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if ((GetPythonFolderName(path) == Bin32FolderName))
                {
                    return FolderIs.x32;
                }
                else
                if ((GetPythonFolderName(path) == Bin64FolderName))
                {
                    return FolderIs.x64;
                }
                else
                if (IsFoldersExist(path))
                {
                    return FolderIs.ParentFolder;
                }
                else
                    return FolderIs.None;
            }
            else
                return FolderIs.None;
        }
        public static string WriteStringToFile(string path,string code, string filename = null)
        {
            string filepath = null;
            try
            {
                if (string.IsNullOrEmpty(filename))
                {
                    filepath = Path.Combine(path, "test.py");
                }
                else
                    filepath = Path.Combine(path, filename);
                // Write file using StreamWriter
                File.WriteAllText(filepath, code, Encoding.Default);
                return filepath;
            }
            catch (Exception ex)
            {
                return null;
                
            }
            
            // Read a file
            // string readText = File.ReadAllText(file)
        }
        public static async Task<PackageDefinition> CheckIfPackageExistsAsync(string packageName)
        {
            using HttpClient httpClient = new HttpClient();
            HttpResponseMessage response;

            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // set timeout to 30 seconds
            try
            {
                response = await httpClient.GetAsync($"https://pypi.org/pypi/{packageName}/json", cts.Token).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                // Network error, API not available, etc.
                Console.WriteLine("An error occurred while checking the package. Please try again later.");
                return null;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"The request to '{packageName}' timed out.");
                return null;
            }

            // If the response status code is OK (200), the package exists
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic packageData = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);
                    string latestVersion = packageData.info.version;
                    string description = packageData.info.description;

                    PackageDefinition packageInfo = new PackageDefinition
                    {
                        packagename = packageName,
                        version = latestVersion,
                        description = description
                    };

                    return packageInfo;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while parsing package data for '{packageName}': {ex.Message}");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"The package '{packageName}' does not exist on PyPI.");
                return null;
            }
        }
    
       
    }
}
