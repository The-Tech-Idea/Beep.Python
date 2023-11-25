using System;
using System.Collections.Generic;

namespace Beep.Python.Model
{
    public interface IPIPManager
    {
        List<packageCategoryImages> packageCategorys { get; set; }
        string packagecatgoryimages { get; }
        string packagenames { get; }
        List<PackageDefinition> packages { get; set; }
        string[] packs { get; set; }

        bool checkifpackageinstalledAsync(string packagename);
        bool InstallPackage(string packagename);
        bool UpdatePackage(string packagename);
        bool InstallPIP();
        bool UpdatePIP();
        void InstallPythonNet();
        void JupiterRun();
        void JupiterStop();
         void QtConsoleRun();
        void QtConsoleStop();
    }
}