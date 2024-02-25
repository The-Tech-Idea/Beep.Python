using Beep.Python.Model;
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
        public static IServiceCollection RegisterPythonService(this IServiceCollection services,string pythonruntimepath)
        {
            Pythonruntimepath = pythonruntimepath;

            PythonRunTimeManager=new PythonNetRunTimeManager();
           
            services.AddSingleton<IPythonRunTimeManager>(PythonRunTimeManager);
            IsReady = PythonRunTimeManager.Initialize(pythonruntimepath, BinType32or64.p395x64, @"lib\site-packages");
            return services;
        }
       
        public static IPythonRunTimeManager GetPythonRunTimeManager(this IDMEEditor dmeEditor)
        {
          
           return  PythonRunTimeManager;
        }
        
    }
}
