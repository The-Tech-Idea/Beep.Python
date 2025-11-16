using Beep.Python.RuntimeEngine.Infrastructure;
using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Templates
{
    /// <summary>
    /// Provides pre-configured environment templates for common use cases.
    /// Templates combine bootstrap options with package profiles for rapid environment setup.
    /// </summary>
    public static class EnvironmentTemplates
    {
        /// <summary>
        /// Minimal Python environment with only essential packages.
        /// Suitable for lightweight scripting and basic automation.
        /// </summary>
        public static BootstrapOptions Minimal => new BootstrapOptions
        {
            EnsureEmbeddedPython = true,
            CreateVirtualEnvironment = true,
            EnvironmentName = "minimal",
            PackageProfiles = new List<string> { "base" },
            SetAsDefault = false
        };

        /// <summary>
        /// Data Science environment with numpy, pandas, matplotlib, scipy, scikit-learn.
        /// Ideal for data analysis, visualization, and statistical computing.
        /// </summary>
        public static BootstrapOptions DataScience => new BootstrapOptions
        {
            EnsureEmbeddedPython = true,
            CreateVirtualEnvironment = true,
            EnvironmentName = "data-science",
            PackageProfiles = new List<string> { "base", "data-science" },
            SetAsDefault = false
        };

        /// <summary>
        /// Machine Learning environment with PyTorch, Transformers, and related tools.
        /// Configured for deep learning, NLP, and model training workflows.
        /// </summary>
        public static BootstrapOptions MachineLearning => new BootstrapOptions
        {
            EnsureEmbeddedPython = true,
            CreateVirtualEnvironment = true,
            EnvironmentName = "machine-learning",
            PackageProfiles = new List<string> { "base", "machine-learning" },
            SetAsDefault = false
        };

        /// <summary>
        /// Web Development environment with Flask, Requests, BeautifulSoup4.
        /// Suitable for web scraping, API development, and HTTP-based workflows.
        /// </summary>
        public static BootstrapOptions WebDevelopment => new BootstrapOptions
        {
            EnsureEmbeddedPython = true,
            CreateVirtualEnvironment = true,
            EnvironmentName = "web-dev",
            PackageProfiles = new List<string> { "base", "web" },
            SetAsDefault = false
        };

        /// <summary>
        /// Full-stack environment combining data science and machine learning packages.
        /// Provides comprehensive toolkit for end-to-end ML pipelines.
        /// </summary>
        public static BootstrapOptions FullStack => new BootstrapOptions
        {
            EnsureEmbeddedPython = true,
            CreateVirtualEnvironment = true,
            EnvironmentName = "full-stack",
            PackageProfiles = new List<string> { "base", "data-science", "machine-learning" },
            SetAsDefault = false
        };

        /// <summary>
        /// Custom environment builder for creating tailored configurations.
        /// </summary>
        /// <param name="name">Environment name</param>
        /// <param name="profiles">Package profiles to include</param>
        /// <param name="useVirtualEnv">Whether to create a virtual environment</param>
        /// <param name="setAsDefault">Whether to set as default runtime</param>
        /// <returns>Configured bootstrap options</returns>
        public static BootstrapOptions Custom(
            string name,
            List<string> profiles,
            bool useVirtualEnv = true,
            bool setAsDefault = false)
        {
            return new BootstrapOptions
            {
                EnsureEmbeddedPython = true,
                CreateVirtualEnvironment = useVirtualEnv,
                EnvironmentName = name,
                PackageProfiles = profiles ?? new List<string> { "base" },
                SetAsDefault = setAsDefault
            };
        }

        /// <summary>
        /// Gets a template by name (case-insensitive).
        /// </summary>
        /// <param name="templateName">Name of the template (minimal, data-science, machine-learning, web-development, full-stack)</param>
        /// <returns>Bootstrap options for the specified template, or null if not found</returns>
        public static BootstrapOptions GetTemplate(string templateName)
        {
            return templateName?.ToLowerInvariant() switch
            {
                "minimal" => Minimal,
                "data-science" or "datascience" or "ds" => DataScience,
                "machine-learning" or "machinelearning" or "ml" => MachineLearning,
                "web-development" or "webdevelopment" or "web-dev" or "web" => WebDevelopment,
                "full-stack" or "fullstack" or "full" => FullStack,
                _ => null
            };
        }

        /// <summary>
        /// Gets all available template names.
        /// </summary>
        public static List<string> GetAvailableTemplates()
        {
            return new List<string>
            {
                "minimal",
                "data-science",
                "machine-learning",
                "web-development",
                "full-stack"
            };
        }

        /// <summary>
        /// Gets descriptions of all available templates.
        /// </summary>
        public static Dictionary<string, string> GetTemplateDescriptions()
        {
            return new Dictionary<string, string>
            {
                ["minimal"] = "Minimal Python environment with only essential packages (pip, setuptools, wheel)",
                ["data-science"] = "Data Science environment with numpy, pandas, matplotlib, scipy, scikit-learn",
                ["machine-learning"] = "Machine Learning environment with PyTorch, Transformers, and related tools",
                ["web-development"] = "Web Development environment with Flask, Requests, BeautifulSoup4",
                ["full-stack"] = "Full-stack environment combining data science and machine learning packages"
            };
        }
    }
}
