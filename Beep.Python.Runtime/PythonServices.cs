using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Utilities;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.DriversConfigurations;
using TheTechIdea.Beep.Editor;

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
        public static IPythonVirtualEnvViewModel PythonvirtualEnvViewModel;
        public static IPythonTrainingViewModel PythonTrainingViewModel;
        public static IPythonAIProjectViewModel PythonAIProjectViewModel;
        public static IPythonModelEvaluationGraphsViewModel PythonModelEvaluationGraphsViewModel;
        public static string PythonDataPath;
        public static IServiceCollection RegisterPythonService(this IServiceCollection services,string pythonruntimepath)
        {
            Pythonruntimepath = pythonruntimepath;
            services.AddSingleton<IPythonRunTimeManager,PythonNetRunTimeManager>();
            Createfolder();
            return services;
        }
        private static void Createfolder()
        {
            PythonDataPath= ContainerMisc.CreateAppfolder("Python");
        }

        public static IServiceCollection RegisterPythonVirtualEnvService(this IServiceCollection services)
        {

            services.AddSingleton<IPythonVirtualEnvViewModel, PythonVirtualEnvViewModel>();

            return services;
        }
        public static IPythonVirtualEnvViewModel GetPythonVirtualEnv(this IDMEEditor dmeEditor)
        {

            return PythonvirtualEnvViewModel;
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
        public static IServiceCollection RegisterPythonAIProjectService(this IServiceCollection services)
        {

            services.AddSingleton<IPythonAIProjectViewModel, PythonAIProjectViewModel>();

            return services;
        }
        public static IPythonAIProjectViewModel GetPythonAIProjectViewModel(this IDMEEditor dmeEditor)
        {
            
            return PythonAIProjectViewModel;
        }
        public static IServiceCollection RegisterPythonModelEvaluationGraphsService(this IServiceCollection services)
        {

            services.AddSingleton<IPythonModelEvaluationGraphsViewModel, PythonModelEvaluationGraphsViewModel>();

            return services;
        }
        public static IPythonModelEvaluationGraphsViewModel GetPythonModelEvaluationGraphsViewModel(this IDMEEditor dmeEditor)
        {

            return PythonModelEvaluationGraphsViewModel;
        }
        public static string GetPythonDataPath(this IDMEEditor dmeEditor)
        {
            return PythonDataPath;
        }
        public static IPythonRunTimeManager GetPythonRunTimeManager(this IDMEEditor dmeEditor)
        {
         //   PythonRunTimeManager.DMEditor= dmeEditor;
           return  PythonRunTimeManager;
        }
        public static IPackageManagerViewModel GetPythonPackageManager(this IDMEEditor dmeEditor)
        {
         //   PackageManager.Editor = dmeEditor;
            return PackageManager;
        }
        public static IPythonMLManager GetPythonMLManager(this IDMEEditor dmeEditor)
        {
           
            return PythonMLManager;
        }

    }
}
