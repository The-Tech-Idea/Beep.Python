using Beep.Python.Model;
using Beep.Python.RuntimeEngine;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheTechIdea.Beep;

namespace Beep.Python.RuntimeEngine
{
    public static class PythonServices
    {
        private static IPythonRunTimeManager PythonRunTimeManager;
        private static bool IsReady  = false;
        private static IDMEEditor DMEditor;
        private static string Pythonruntimepath;
        public static IServiceCollection RegisterPythonService(this IServiceCollection services)
        {
            services.AddSingleton<IPythonRunTimeManager, PythonNetRunTimeManager>();

            return services;
        }
        public static IPythonRunTimeManager InitPythonServices(this IDMEEditor dmeEditor, string pythonruntimepath)
        {
            DMEditor = dmeEditor;
            Pythonruntimepath = pythonruntimepath;
            IsReady= PythonRunTimeManager.Initialize(pythonruntimepath, BinType32or64.p395x64, @"lib\site-packages");
            if(IsReady)
            {
                return PythonRunTimeManager;
            }
            else
            {
                return null;
            }
           
        }
        public static IPythonRunTimeManager GetPythonRunTimeManager(this IPythonRunTimeManager pythonruntimemanager)
        {
           return  PythonRunTimeManager;
        }
        
    }
}
