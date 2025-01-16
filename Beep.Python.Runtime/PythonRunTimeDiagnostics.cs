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
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;
using Microsoft.Extensions.Logging;


namespace Beep.Python.RuntimeEngine
{
    public static class PythonRunTimeDiagnostics
    {
        private static readonly object _lock = new object();
        public static string Bin32FolderName { get; set; } = "x32";
        public static string Bin64FolderName { get; set; } = "x64";
        public static string PythonVersion { get; set; } = "3.10";
        public static string Bin64FolderPath { get; set; }
        public static string Bin32FolderPath { get; set; }

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
        public static async Task<bool> IsUrlReachableAsync(string url)
        {
            try
            {
                using HttpClient httpClient = new HttpClient();
                HttpResponseMessage response = await httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        public static bool CheckNet()
        {
            int desc;
            return InternetGetConnectedState(out desc, 0);
        }
        public static List<FolderStructure> Folders { get; set; } = new List<FolderStructure>();
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
        public static async Task<bool> IsFileExistAsync(string path)
        {
            return await Task.Run(() => File.Exists(path));
        }
        public static async Task<bool> IsFileExistAsync(string path, string fileName)
        {
            return await Task.Run(() => File.Exists(Path.Combine(path, fileName)));
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
                return FileExists(path, "python.exe", "conda.exe");
            }
            return false;
         
        }
        public static async Task<bool> IsPythonInstalledAsync(string path)
        {
            if (!Directory.Exists(path)) return false;

            bool dllExists = await Task.Run(() => Directory.EnumerateFiles(path, "python*.dll").Any());
            bool exeExists = await IsFileExistAsync(path, "python.exe");

            return dllExists && exeExists;
        }
        private static bool FileExists(string path, params string[] files)
        {
            foreach (var file in files)
            {
                if (File.Exists(Path.Combine(path, file)))
                    return true;
            }
            return false;
        }
        public static string GetPythonExe(string path)
        {
            if (!Directory.Exists(path)) return null;

            return IsCondaInstalled(path) ?? (IsPythonInstalled(path) ? Path.Combine(path, "python.exe") : null);
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
                startInfo.Arguments = "--Version";
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
        public static async Task<string> GetPythonVersionFromPythonAsync(string path)
        {
            if (!await IsPythonInstalledAsync(path)) return string.Empty;

            return await Task.Run(() =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(path, "python.exe"),
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using Process process = new Process { StartInfo = startInfo };
                process.Start();
                string version = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return version;
            });
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
        public static PythonRunTime GetPythonConfig(string path)
        {
            var config = new PythonRunTime();

            if (!Directory.Exists(path))
            {
                config.Message = "Directory does not exist.";
                return config;
            }

            var version = GetVersion(path);
            var dllPath = Path.Combine(path, $"python{version}.dll");
            var architecture = GetDllArchitecture(dllPath);

            if (architecture == BinType32or64.Unknown)
            {
                config.Message = "Python DLL architecture could not be determined.";
                return config;
            }

            config.IsPythonInstalled = IsPythonInstalled(path);
            if (!config.IsPythonInstalled)
            {
                config.Message = "Python is not installed.";
                return config;
            }

            config.RuntimePath = path;
            config.BinPath = path;
            config.PythonVersion = version;
            config.PythonDll = dllPath;
            config.Packageinstallpath = Path.Combine(path, "Lib", "site-Packages");
            config.ScriptPath = Path.Combine(path, "Scripts");
            config.BinType = architecture;
            config.PackageType = GetPackageType(path);
            config.Message = "Python configuration detected.";

            return config;
        }
        public static async Task<PythonRunTime> GetPythonConfigAsync(string path)
        {
            PythonRunTime config = new PythonRunTime();

            if (!Directory.Exists(path))
            {
                config.Message = "Directory does not exist.";
                return config;
            }

            string version = await Task.Run(() => GetVersion(path));
            string dllPath = Path.Combine(path, $"python{version}.dll");
            BinType32or64 architecture = await Task.Run(() => GetDllArchitecture(dllPath));

            if (architecture == BinType32or64.Unknown)
            {
                config.Message = "Python DLL architecture could not be determined.";
                return config;
            }

            config.IsPythonInstalled = await IsPythonInstalledAsync(path);
            if (!config.IsPythonInstalled)
            {
                config.Message = "Python is not installed.";
                return config;
            }

            config.RuntimePath = path;
            config.BinPath = path;
            config.PythonVersion = version;
            config.PythonDll = dllPath;
            config.Packageinstallpath = Path.Combine(path, "Lib", "site-Packages");
            config.ScriptPath = Path.Combine(path, "Scripts");
            config.BinType = architecture;
            config.PackageType = GetPackageType(path);
            config.Message = "Python configuration detected.";

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
        public static async Task<string> WriteStringToFileAsync(string path, string code, string filename = null)
        {
            try
            {
                string filepath = string.IsNullOrEmpty(filename)
                    ? Path.Combine(path, "test.py")
                    : Path.Combine(path, filename);

                await File.WriteAllTextAsync(filepath, code, Encoding.Default);
                return filepath;
            }
            catch
            {
                return null;
            }
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
                        PackageName = packageName,
                        Version = latestVersion,
                        Description = description
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
        public static void AddFolder(FolderStructure folder)
        {
            lock (_lock)
            {
                Folders.Add(folder);
            }
        }

    }
}
