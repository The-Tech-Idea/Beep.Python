using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using TheTechIdea.Beep;
using TheTechIdea.Beep.Container;

namespace Beep.Python.RuntimeEngine
{
    public static class PythonServices
    {
        public static IPythonRunTimeManager PythonRunTimeManager;
        private static bool IsReady  = false;
        private static IDMEEditor DMEditor;
        public static string Pythonruntimepath;
        public static IPackageManagerViewModel PackageManager;
        public static IPythonMLManager PythonMLManager;
        public static string PythonDataPath;
        public static IServiceCollection RegisterPythonService(this IServiceCollection services,string pythonruntimepath)
        {
            Pythonruntimepath = pythonruntimepath;
            services.AddSingleton<IPythonRunTimeManager,PythonNetRunTimeManager>();
            //PythonRunTimeManager=new PythonNetRunTimeManager();

            //services.AddSingleton<IPythonRunTimeManager>(PythonRunTimeManager);
            //IsReady = PythonRunTimeManager.Initialize(pythonruntimepath, BinType32or64.p395x64, @"lib\site-packages");
            // check if projects directory exists if not create it 
            Createfolder();
            return services;
        }
        private static void Createfolder()
        {
            PythonDataPath= ContainerMisc.CreateAppfolder("Python");
        }
      
       
        public static IServiceCollection RegisterPythonPackageManagerService(this IServiceCollection services)
        {
          
            services.AddSingleton<IPackageManagerViewModel,PackageManagerViewModel>();

            return services;
        }
        public static IServiceCollection RegisterPythonMLService(this IServiceCollection services)
        {

            services.AddSingleton<IPythonMLManager, PythonMLManager>();

            return services;
        }
        public static string GetPythonDataPath(this IDMEEditor dmeEditor)
        {
            return PythonDataPath;
        }
        public static IPythonRunTimeManager GetPythonRunTimeManager(this IDMEEditor dmeEditor)
        {
            PythonRunTimeManager.DMEditor= dmeEditor;
           return  PythonRunTimeManager;
        }
        public static IPackageManagerViewModel GetPythonPackageManager(this IDMEEditor dmeEditor)
        {
            PackageManager.Editor = dmeEditor;
            return PackageManager;
        }
        public static IPythonMLManager GetPythonMLManager(this IDMEEditor dmeEditor)
        {
           
            return PythonMLManager;
        }

    }
}
