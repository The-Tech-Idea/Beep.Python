
// Reorganized PythonRunTimeDiagnostics.cs
using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
//using TheTechIdea.Beep.Editor;

namespace Beep.Python.RuntimeEngine.Helpers
{
    public static class PythonRunTimeDiagnostics
    {
        public static string Bin32FolderName { get; set; } = "x32";
        public static string Bin64FolderName { get; set; } = "x64";
        public static string PythonVersion { get; set; } = "3.14";
        public static List<FolderStructure> Folders { get; set; } = new();
        private static readonly object _lock = new();

        #region ==== File Utilities ====
        public static bool IsFileExist(string path, string fileName = null, string extension = null)
        {
            if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(extension))
                return File.Exists(Path.Combine(path, fileName + "." + extension));
            if (!string.IsNullOrEmpty(fileName))
                return File.Exists(Path.Combine(path, fileName));
            return File.Exists(path);
        }

        public static bool FolderExist(string path, BinType32or64 type)
        {
            var folderName = GetPythonFolderName(path);
            if (type == BinType32or64.p395x32)
                return folderName.Equals(Bin32FolderName, StringComparison.OrdinalIgnoreCase);
            if (type == BinType32or64.p395x64)
                return folderName.Equals(Bin64FolderName, StringComparison.OrdinalIgnoreCase);
            return false;
        }

        public static bool IsFoldersExist(string path)
            => FolderExist(path, BinType32or64.p395x32) || FolderExist(path, BinType32or64.p395x64);

        public static string WriteStringToFile(string path, string code, string filename = "test.py")
        {
            try
            {
                var filePath = Path.Combine(path, filename);
                File.WriteAllText(filePath, code, Encoding.Default);
                return filePath;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> WriteStringToFileAsync(string path, string code, string filename = "test.py")
        {
            try
            {
                var filePath = Path.Combine(path, filename);
                await File.WriteAllTextAsync(filePath, code, Encoding.Default);
                return filePath;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region ==== Python Installation and Configuration ====
        public static bool IsPythonInstalled(string path)
            => File.Exists(Path.Combine(path, "python.exe")) || File.Exists(Path.Combine(path, "conda.exe"));

        public static async Task<bool> IsPythonInstalledAsync(string path)
        {
            if (!Directory.Exists(path)) return false;
            bool dllExists = await Task.Run(() => Directory.EnumerateFiles(path, "python*.dll").Any());
            bool exeExists = await Task.Run(() => File.Exists(Path.Combine(path, "python.exe")));
            return dllExists && exeExists;
        }

        public static BinType32or64 GetDllArchitecture(string dllPath)
        {
            using var stream = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);
            stream.Seek(0x3C, SeekOrigin.Begin);
            int peOffset = reader.ReadInt32();
            stream.Seek(peOffset, SeekOrigin.Begin);
            reader.ReadUInt32();
            return reader.ReadUInt16() switch
            {
                0x8664 => BinType32or64.p395x64,
                0x14C => BinType32or64.p395x32,
                _ => BinType32or64.Unknown
            };
        }

        public static string GetPythonVersionFromPython(string path)
        {
            if (!IsPythonInstalled(path)) return null;
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(path, "python.exe"),
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(startInfo);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }

        public static PythonRunTime GetPythonConfig(string path)
        {
            var config = new PythonRunTime();
            if (!Directory.Exists(path))
            {
                config.Message = "Directory does not exist.";
                return config;
            }

            string version = GetPythonVersionFromPython(path);
            string dllPath = Directory.GetFiles(path, "python*.dll").FirstOrDefault();
            var arch = GetDllArchitecture(dllPath);

            config.RuntimePath = path;
            config.PythonVersion = version;
            config.PythonDll = dllPath;
            config.BinType = arch;
            config.IsPythonInstalled = IsPythonInstalled(path);
            config.Packageinstallpath = Path.Combine(path, "Lib", "site-packages");
            config.ScriptPath = Path.Combine(path, "Scripts");
            config.Message = "Python environment loaded.";
            return config;
        }
        #endregion

        #region ==== Package & Environment ====
        public static string IsCondaInstalled(string path)
        {
            if (File.Exists(Path.Combine(path, "conda.exe")))
                return "conda";
            if (File.Exists(Path.Combine(path, "_conda.exe")))
                return "_conda";
            return null;
        }
        /// <summary>
        /// Sets the names of the 32-bit and 64-bit Python folders.
        /// </summary>
        /// <param name="bin32FolderName">Folder name for 32-bit Python.</param>
        /// <param name="bin64FolderName">Folder name for 64-bit Python.</param>
        public static void SetFolderNames(string bin32FolderName, string bin64FolderName)
        {
            Bin32FolderName = bin32FolderName?.Trim().ToLower() ?? "x32";
            Bin64FolderName = bin64FolderName?.Trim().ToLower() ?? "x64";
        }
        /// <summary>
        /// Sets and ensures the existence of an 'AI' folder in the user's Documents directory.
        /// </summary>
        /// <param name="DMEditor">Reference to the IDMEEditor for configuration access.</param>
        /// <returns>Full path to the AI folder.</returns>
        public static string SetAiFolderPath()
        {
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            string aiFolderPath = Path.Combine(documentsPath, "AI");

            if (!Directory.Exists(aiFolderPath))
            {
                Directory.CreateDirectory(aiFolderPath);
            }

            return aiFolderPath;
        }

        public static PackageType GetPackageType(string path)
        {
            return IsCondaInstalled(path) != null ? PackageType.conda :
                   IsPythonInstalled(path) ? PackageType.pypi : PackageType.None;
        }

        public static async Task<PackageDefinition> CheckIfPackageExistsAsync(string packageName)
        {
            using HttpClient httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync($"https://pypi.org/pypi/{packageName}/json");
                if (!response.IsSuccessStatusCode) return null;
                string content = await response.Content.ReadAsStringAsync();
                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                return new PackageDefinition
                {
                    PackageName = packageName,
                    Version = json.info.version,
                    Description = json.info.description
                };
            }
            catch { return null; }
        }
        #endregion

        #region ==== Internet & Networking ====
        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static bool CheckNet() => InternetGetConnectedState(out _, 0);

        public static async Task<bool> IsUrlReachableAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
        #endregion

        #region ==== Path Helpers ====
        public static string GetPythonFolderName(string path) => new DirectoryInfo(path).Name;
        public static string GetPythonExe(string path)
        {
            if (!Directory.Exists(path)) return null;
            if (File.Exists(Path.Combine(path, "python.exe")))
                return Path.Combine(path, "python.exe");
            return null;
        }
        #endregion

        #region ==== Folder Management ====
        public static void AddFolder(FolderStructure folder)
        {
            lock (_lock)
            {
                if (!Folders.Any(f => f.Folderpath == folder.Folderpath))
                {
                    Folders.Add(folder);
                }
            }
        }
        #endregion
    }
}
