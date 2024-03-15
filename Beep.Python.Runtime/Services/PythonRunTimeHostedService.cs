using Beep.Python.Model;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Services
{
    public class PythonRunTimeHostedService : IHostedService
    {
        private readonly IPythonRunTimeManager _pythonRunTimeManager;
        private readonly string _pythonRuntimePath;

        public PythonRunTimeHostedService(IPythonRunTimeManager pythonRunTimeManager, string pythonRuntimePath)
        {
            _pythonRunTimeManager = pythonRunTimeManager;
            _pythonRuntimePath = pythonRuntimePath;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Perform initialization logic here to ensure it runs after the application starts
            IsReady = _pythonRunTimeManager.Initialize(_pythonRuntimePath, BinType32or64.p395x64, @"lib\site-packages");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Cleanup if needed
            return Task.CompletedTask;
        }

        // Property to check if the service is ready (might be used elsewhere)
        public bool IsReady { get; private set; }
    }

}
