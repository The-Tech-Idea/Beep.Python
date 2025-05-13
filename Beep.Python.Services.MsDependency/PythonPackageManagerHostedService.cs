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
    public class PythonPackageManagerHostedService : IHostedService
    {
        private readonly IPythonRunTimeManager _pythonRunTimeManager;
        private readonly string _pythonRuntimePath;

        public PythonPackageManagerHostedService(IPythonRunTimeManager pythonRunTimeManager, string pythonRuntimePath)
        {
            _pythonRunTimeManager = pythonRunTimeManager;
            _pythonRuntimePath = pythonRuntimePath;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsReady = _pythonRunTimeManager.Initialize(_pythonRuntimePath, @"lib\site-Packages");
                if (!IsReady)
                {
                    throw new InvalidOperationException("Failed to initialize the Python runtime.");
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Python runtime initialization failed: {ex.Message}");
                IsReady = false;
            }

            await Task.CompletedTask;
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
