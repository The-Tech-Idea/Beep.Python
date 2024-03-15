using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep;

namespace Beep.Python.RuntimeEngine
{
    public static class PythonServices
    {
        private static IPythonRunTimeManager PythonRunTimeManager;
        private static bool IsReady  = false;
        private static IDMEEditor DMEditor;
        private static string Pythonruntimepath;
        private static IPackageManagerViewModel PackageManager;
        public static IServiceCollection RegisterPythonService(this IServiceCollection services,string pythonruntimepath)
        {
            Pythonruntimepath = pythonruntimepath;

            PythonRunTimeManager=new PythonNetRunTimeManager();
           
            services.AddSingleton<IPythonRunTimeManager>(PythonRunTimeManager);
            IsReady = PythonRunTimeManager.Initialize(pythonruntimepath, BinType32or64.p395x64, @"lib\site-packages");
            if (IsReady)
            {
                PackageManager = new PackageManagerViewModel(PythonRunTimeManager);

                services.AddSingleton<IPackageManagerViewModel>(PackageManager);
                PythonRunTimeManager.PackageManager = PackageManager;
            }
            return services;
        }
        public static IServiceCollection RegisterPythonPackageManagerService(this IServiceCollection services, string pythonruntimepath)
        {
            Pythonruntimepath = pythonruntimepath;

            if (IsReady)
            {
                PackageManager = new PackageManagerViewModel(PythonRunTimeManager);
                services.AddSingleton<IPackageManagerViewModel>(PackageManager);
                PythonRunTimeManager.PackageManager = PackageManager;
            }
            return services;
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

    }
}
