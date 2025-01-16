using System;
using System.Collections.Generic;

namespace Beep.Python.Model
{
    public interface IPIPManager
    {
        List<packageCategoryImages> PackageCategorys { get; set; }
        string PackageCatgoryImages { get; }
        string PackageNames { get; }
        List<PackageDefinition> Packages { get; set; }
        string[] Packs { get; set; }

        bool CheckifpackageinstalledAsync(string packagename);
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