﻿using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using Microsoft.Extensions.DependencyInjection;

using TheTechIdea.Beep.Editor;

using TheTechIdea.Beep.Container;
using System;

namespace Beep.Python.RuntimeEngine.Services
{
    public static class PythonServices
    {
        private static readonly object _lock = new object();
        private static IPythonRunTimeManager _pythonRunTimeManager;
        public static IServiceProvider ServiceProvider { get; private set; }
        public static IPythonRunTimeManager PythonRunTimeManager
        {
            get
            {
                lock (_lock)
                {
                    return _pythonRunTimeManager;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _pythonRunTimeManager = value;
                }
            }
        }

        public static string PythonRunTimepath;
        public static string PythonDataPath;

        public static IServiceCollection RegisterPythonService(this IServiceCollection services, string pythonruntimepath)
        {
            PythonRunTimepath = pythonruntimepath;
            services.AddSingleton<IPythonRunTimeManager, PythonNetRunTimeManager>();
            CreateFolder();
            return services;
        }
        public static IServiceCollection RegisterPythonServices(this IServiceCollection services, string pythonRuntimePath)
        {
            PythonRunTimepath = pythonRuntimePath;
            services.AddSingleton<IPythonRunTimeManager, PythonNetRunTimeManager>();
            services.AddSingleton<IPythonVirtualEnvViewModel, PythonVirtualEnvViewModel>();
            services.AddSingleton<IPackageManagerViewModel, PackageManagerViewModel>();
            services.AddSingleton<IPythonMLManager, PythonMLManager>();
            services.AddSingleton<IPythonAIProjectViewModel, PythonAIProjectViewModel>();
            services.AddSingleton<IPythonModelEvaluationGraphsViewModel, PythonModelEvaluationGraphsViewModel>();
            CreateFolder();
            return services;
        }
        public static void ConfigureServiceProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }
        private static void CreateFolder()
        {
            try
            {
                PythonDataPath = ContainerMisc.CreateAppfolder("Python");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create Python folder.", ex);
            }
        }
        public static T GetService<T>()
        {
            if (ServiceProvider == null)
            {
                throw new InvalidOperationException("Service provider not configured.");
            }

            return ServiceProvider.GetRequiredService<T>();
        }
        #region "Add Services"
        public static IServiceCollection RegisterPythonPackageManagerService(this IServiceCollection services)
        {

            services.AddSingleton<IPackageManagerViewModel, PackageManagerViewModel>();

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
        public static IServiceCollection RegisterPythonVirtualEnvironment(this IServiceCollection services)
        {

            services.AddSingleton<IPythonVirtualEnvViewModel, PythonVirtualEnvViewModel>();

            return services;
        }
        public static IServiceCollection RegisterPythonModelEvaluationGraphsService(this IServiceCollection services)
        {

            services.AddSingleton<IPythonModelEvaluationGraphsViewModel, PythonModelEvaluationGraphsViewModel>();

            return services;
        }

        #endregion "Add Services"
        #region "Get Services"
        public static IPythonRunTimeManager GetPythonRunTimeManager()
        {
            return ServiceProvider.GetRequiredService<IPythonRunTimeManager>();
        }
        public static IPackageManagerViewModel GetPythonPackageManager() => GetService<IPackageManagerViewModel>();
        public static IPythonVirtualEnvViewModel GetPythonVirtualEnv() => GetService<IPythonVirtualEnvViewModel>();
        public static IPythonMLManager GetPythonMLManager() => GetService<IPythonMLManager>();
        public static IPythonAIProjectViewModel GetPythonAIProjectViewModel() => GetService<IPythonAIProjectViewModel>();
        public static IPythonModelEvaluationGraphsViewModel GetPythonModelEvaluationGraphsViewModel() => GetService<IPythonModelEvaluationGraphsViewModel>();
        public static IPythonVirtualEnvViewModel GetPythonVirtualEnv(this IDMEEditor dmeEditor)
        {

            return GetPythonVirtualEnv();
        }
        public static IPythonAIProjectViewModel GetPythonAIProjectViewModel(this IDMEEditor dmeEditor)
        {

            return GetPythonAIProjectViewModel();
        }
        public static IPythonModelEvaluationGraphsViewModel GetPythonModelEvaluationGraphsViewModel(this IDMEEditor dmeEditor)
        {

            return GetPythonModelEvaluationGraphsViewModel();
        }
        public static string GetPythonDataPath(this IDMEEditor dmeEditor)
        {
            return PythonDataPath;
        }
        public static IPythonRunTimeManager GetPythonRunTimeManager(this IDMEEditor dmeEditor)
        {
            return GetService<IPythonRunTimeManager>();
        }
        public static IPackageManagerViewModel GetPythonPackageManager(this IDMEEditor dmeEditor)
        {
            return GetService<IPackageManagerViewModel>();
        }
        public static IPythonMLManager GetPythonMLManager(this IDMEEditor dmeEditor)
        {
            return GetService<IPythonMLManager>();
        }
        #endregion "Get Services"



    }
}
