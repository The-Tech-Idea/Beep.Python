using Autofac;
using Beep.Python.Model;
using Beep.Python.RuntimeEngine.ViewModels;
using TheTechIdea.Beep.Editor;
using System;

namespace Beep.Python.RuntimeEngine.Services
{
    /// <summary>
    /// Static class for registering and accessing Python-related services using Autofac.
    /// </summary>
    public static class PythonServicesAutofac
    {
        private static readonly object _lock = new object();
        private static IPythonRunTimeManager _pythonRunTimeManager;

        /// <summary>
        /// The Autofac Container instance.
        /// </summary>
        public static IContainer Container { get; private set; }

        /// <summary>
        /// The Python runtime manager singleton instance.
        /// </summary>
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

        /// <summary>
        /// Path to the Python runtime directory.
        /// </summary>
        public static string PythonRunTimepath;

        /// <summary>
        /// Path to the Python data directory.
        /// </summary>
        public static string PythonDataPath;

        /// <summary>
        /// Registers the basic Python service with Autofac.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <param name="pythonruntimepath">Path to the Python runtime.</param>
        /// <returns>The container builder for method chaining.</returns>
        public static ContainerBuilder RegisterPythonService(this ContainerBuilder builder, string pythonruntimepath)
        {
            PythonRunTimepath = pythonruntimepath;
            builder.RegisterType<PythonNetRunTimeManager>().As<IPythonRunTimeManager>().SingleInstance();
            CreateFolder();
            return builder;
        }

        /// <summary>
        /// Registers all Python services with Autofac.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <param name="pythonRuntimePath">Path to the Python runtime.</param>
        /// <returns>The container builder for method chaining.</returns>
        public static ContainerBuilder RegisterPythonServices(this ContainerBuilder builder, string pythonRuntimePath)
        {
            PythonRunTimepath = pythonRuntimePath;

            // Register services as singletons
            builder.RegisterType<PythonNetRunTimeManager>().As<IPythonRunTimeManager>().SingleInstance();
            builder.RegisterType<PythonVirtualEnvManager>().As<IPythonVirtualEnvManager>().SingleInstance();
            builder.RegisterType<PythonPackageManager>().As<IPackageManagerViewModel>().SingleInstance();
            builder.RegisterType<PythonMLManager>().As<IPythonMLManager>().SingleInstance();
            builder.RegisterType<PythonAIProjectViewModel>().As<IPythonAIProjectViewModel>().SingleInstance();
            builder.RegisterType<PythonModelEvaluationGraphsViewModel>().As<IPythonModelEvaluationGraphsViewModel>().SingleInstance();

            CreateFolder();
            return builder;
        }

        /// <summary>
        /// Configures the container for use with Python services.
        /// </summary>
        /// <param name="container">The Autofac container instance.</param>
        public static void ConfigureContainer(IContainer container)
        {
            Container = container;
            PythonRunTimeManager = Container.Resolve<IPythonRunTimeManager>();
        }

        private static void CreateFolder()
        {
            try
            {
                PythonDataPath = string.Empty;
                string pythonDataPath = System.IO.Path.Combine(PythonRunTimepath, "PythonData");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create Python folder.", ex);
            }
        }

        #region "Add Services"

        /// <summary>
        /// Registers the Python package manager service.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <returns>The container builder for method chaining.</returns>
        public static ContainerBuilder RegisterPythonPackageManagerService(this ContainerBuilder builder)
        {
            builder.RegisterType<PythonPackageManager>().As<IPackageManagerViewModel>().SingleInstance();
            return builder;
        }

        /// <summary>
        /// Registers the Python ML service.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <returns>The container builder for method chaining.</returns>
        public static ContainerBuilder RegisterPythonMLService(this ContainerBuilder builder)
        {
            builder.RegisterType<PythonMLManager>().As<IPythonMLManager>().SingleInstance();
            return builder;
        }

        /// <summary>
        /// Registers the Python AI project service.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <returns>The container builder for method chaining.</returns>
        public static ContainerBuilder RegisterPythonAIProjectService(this ContainerBuilder builder)
        {
            builder.RegisterType<PythonAIProjectViewModel>().As<IPythonAIProjectViewModel>().SingleInstance();
            return builder;
        }

        /// <summary>
        /// Registers the Python virtual environment service.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <returns>The container builder for method chaining.</returns>
        public static ContainerBuilder RegisterPythonVirtualEnvironment(this ContainerBuilder builder)
        {
            builder.RegisterType<PythonVirtualEnvManager>().As<IPythonVirtualEnvManager>().SingleInstance();
            return builder;
        }

        /// <summary>
        /// Registers the Python model evaluation graphs service.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <returns>The container builder for method chaining.</returns>
        public static ContainerBuilder RegisterPythonModelEvaluationGraphsService(this ContainerBuilder builder)
        {
            builder.RegisterType<PythonModelEvaluationGraphsViewModel>().As<IPythonModelEvaluationGraphsViewModel>().SingleInstance();
            return builder;
        }

        #endregion "Add Services"

        #region "Get Services"

        /// <summary>
        /// Gets the Python runtime manager instance.
        /// </summary>
        /// <returns>The Python runtime manager.</returns>
        public static IPythonRunTimeManager GetPythonRunTimeManager()
        {
            return Container.Resolve<IPythonRunTimeManager>();
        }

        /// <summary>
        /// Gets the Python package manager instance.
        /// </summary>
        /// <returns>The package manager view model.</returns>
        public static IPackageManagerViewModel GetPythonPackageManager() => Container.Resolve<IPackageManagerViewModel>();

        /// <summary>
        /// Gets the Python virtual environment view model instance.
        /// </summary>
        /// <returns>The virtual environment view model.</returns>
        public static IPythonVirtualEnvManager GetPythonVirtualEnv() => Container.Resolve<IPythonVirtualEnvManager>();

        /// <summary>
        /// Gets the Python ML manager instance.
        /// </summary>
        /// <returns>The ML manager.</returns>
        public static IPythonMLManager GetPythonMLManager() => Container.Resolve<IPythonMLManager>();

        /// <summary>
        /// Gets the Python AI project view model instance.
        /// </summary>
        /// <returns>The AI project view model.</returns>
        public static IPythonAIProjectViewModel GetPythonAIProjectViewModel() => Container.Resolve<IPythonAIProjectViewModel>();

        /// <summary>
        /// Gets the Python model evaluation graphs view model instance.
        /// </summary>
        /// <returns>The model evaluation graphs view model.</returns>
        public static IPythonModelEvaluationGraphsViewModel GetPythonModelEvaluationGraphsViewModel() => Container.Resolve<IPythonModelEvaluationGraphsViewModel>();

        /// <summary>
        /// Gets the Python virtual environment view model for a DME editor.
        /// </summary>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The virtual environment view model.</returns>
        public static IPythonVirtualEnvManager GetPythonVirtualEnv(this IDMEEditor dmeEditor)
        {
            return GetPythonVirtualEnv();
        }

        /// <summary>
        /// Gets the Python AI project view model for a DME editor.
        /// </summary>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The AI project view model.</returns>
        public static IPythonAIProjectViewModel GetPythonAIProjectViewModel(this IDMEEditor dmeEditor)
        {
            return GetPythonAIProjectViewModel();
        }

        /// <summary>
        /// Gets the Python model evaluation graphs view model for a DME editor.
        /// </summary>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The model evaluation graphs view model.</returns>
        public static IPythonModelEvaluationGraphsViewModel GetPythonModelEvaluationGraphsViewModel(this IDMEEditor dmeEditor)
        {
            return GetPythonModelEvaluationGraphsViewModel();
        }

        /// <summary>
        /// Gets the Python data path for a DME editor.
        /// </summary>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The Python data path.</returns>
        public static string GetPythonDataPath(this IDMEEditor dmeEditor)
        {
            return PythonDataPath;
        }

        /// <summary>
        /// Gets the Python runtime manager for a DME editor.
        /// </summary>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The Python runtime manager.</returns>
        public static IPythonRunTimeManager GetPythonRunTimeManager(this IDMEEditor dmeEditor)
        {
            return Container.Resolve<IPythonRunTimeManager>();
        }

        /// <summary>
        /// Gets the Python package manager for a DME editor.
        /// </summary>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The package manager view model.</returns>
        public static IPackageManagerViewModel GetPythonPackageManager(this IDMEEditor dmeEditor)
        {
            return Container.Resolve<IPackageManagerViewModel>();
        }

        /// <summary>
        /// Gets the Python ML manager for a DME editor.
        /// </summary>
        /// <param name="dmeEditor">The DME editor.</param>
        /// <returns>The ML manager.</returns>
        public static IPythonMLManager GetPythonMLManager(this IDMEEditor dmeEditor)
        {
            return Container.Resolve<IPythonMLManager>();
        }

        #endregion "Get Services"
    }
}
