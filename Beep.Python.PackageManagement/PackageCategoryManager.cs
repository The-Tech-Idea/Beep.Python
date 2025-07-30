using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beep.Python.Model;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Manages categorization of Python packages
    /// </summary>
    public class PackageCategoryManager
    {
        private readonly IBeepService _beepService;
        private readonly PackageOperationManager _packageOperations;
        private readonly IProgress<PassedArgs> _progress;
        private readonly Dictionary<string, Dictionary<string, string>> _categoryKeywords;

        public PackageCategoryManager(
            IBeepService beepService,
            PackageOperationManager packageOperations,
            IProgress<PassedArgs> progress = null)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _packageOperations = packageOperations ?? throw new ArgumentNullException(nameof(packageOperations));
            _progress = progress;
            
            _categoryKeywords = InitializeCategoryKeywords();
        }

        /// <summary>
        /// Gets packages in a specific category
        /// </summary>
        /// <param name="environment">The environment containing packages</param>
        /// <param name="category">The category to filter by</param>
        /// <returns>List of packages in the category</returns>
        public List<PackageDefinition> GetPackagesByCategory(PythonVirtualEnvironment environment, PackageCategory category)
        {
            if (environment?.InstalledPackages == null)
            {
                return new List<PackageDefinition>();
            }

            return environment.InstalledPackages
                .Where(p => p.Category == category)
                .ToList();
        }

        /// <summary>
        /// Sets the category for a specific package
        /// </summary>
        /// <param name="environment">The environment containing the package</param>
        /// <param name="packageName">Name of the package</param>
        /// <param name="category">Category to assign</param>
        public void SetPackageCategory(PythonVirtualEnvironment environment, string packageName, PackageCategory category)
        {
            if (environment?.InstalledPackages == null || string.IsNullOrEmpty(packageName))
                return;

            var package = environment.InstalledPackages.FirstOrDefault(p =>
                p.PackageName != null &&
                p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (package != null)
            {
                package.Category = category;
                ReportProgress($"Set category for package '{packageName}' to '{category}'");
            }
        }

        /// <summary>
        /// Updates categories for multiple packages at once
        /// </summary>
        /// <param name="environment">The environment containing the packages</param>
        /// <param name="packageCategories">Dictionary of package names and categories</param>
        public void UpdatePackageCategories(
            PythonVirtualEnvironment environment, 
            Dictionary<string, PackageCategory> packageCategories)
        {
            if (environment?.InstalledPackages == null || packageCategories == null || packageCategories.Count == 0)
                return;

            foreach (var kvp in packageCategories)
            {
                SetPackageCategory(environment, kvp.Key, kvp.Value);
            }

            ReportProgress($"Updated categories for {packageCategories.Count} packages");
        }

        /// <summary>
        /// Populates common package categories based on known package names
        /// </summary>
        /// <param name="environment">The environment containing packages</param>
        /// <returns>True if any packages were categorized</returns>
        public bool PopulateCommonPackageCategories(PythonVirtualEnvironment environment)
        {
            if (environment?.InstalledPackages == null || environment.InstalledPackages.Count == 0)
                return false;

            int categorizedCount = 0;
            var packageToCategory = new Dictionary<string, PackageCategory>(StringComparer.OrdinalIgnoreCase);

            // Build a map of known packages to categories
            FillKnownPackageCategories(packageToCategory);

            // Apply categories to packages
            foreach (var package in environment.InstalledPackages)
            {
                if (package.Category == PackageCategory.Uncategorized &&
                    packageToCategory.TryGetValue(package.PackageName, out var category))
                {
                    package.Category = category;
                    categorizedCount++;
                }
                else if (package.Category == PackageCategory.Uncategorized)
                {
                    // Try keyword matching for remaining uncategorized packages
                    PackageCategory suggestedCategory = SuggestCategoryFromKeywords(package.PackageName, package.Description);
                    if (suggestedCategory != PackageCategory.Uncategorized)
                    {
                        package.Category = suggestedCategory;
                        categorizedCount++;
                    }
                }
            }

            if (categorizedCount > 0)
            {
                ReportProgress($"Categorized {categorizedCount} packages");
            }

            return categorizedCount > 0;
        }

        /// <summary>
        /// Suggests categories for packages based on their names and descriptions
        /// </summary>
        /// <param name="packageNames">List of package names to categorize</param>
        /// <returns>Dictionary mapping package names to suggested categories</returns>
        public async Task<Dictionary<string, PackageCategory>> SuggestCategoriesForPackagesAsync(
            IEnumerable<string> packageNames,
            PythonVirtualEnvironment environment)
        {
            var suggestions = new Dictionary<string, PackageCategory>();

            if (packageNames == null || !packageNames.Any() || environment == null)
                return suggestions;

            try
            {
                var knownCategories = new Dictionary<string, PackageCategory>(StringComparer.OrdinalIgnoreCase);
                FillKnownPackageCategories(knownCategories);

                foreach (var packageName in packageNames)
                {
                    // First check if it's a known package
                    if (knownCategories.TryGetValue(packageName, out var category))
                    {
                        suggestions[packageName] = category;
                        continue;
                    }

                    // If it's in our environment with a non-Uncategorized category, use that
                    var existingPackage = environment.InstalledPackages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase) &&
                        p.Category != PackageCategory.Uncategorized);

                    if (existingPackage != null)
                    {
                        suggestions[packageName] = existingPackage.Category;
                        continue;
                    }

                    // Try keyword-based categorization from package name
                    PackageCategory keywordCategory = SuggestCategoryFromKeywords(packageName, null);
                    if (keywordCategory != PackageCategory.Uncategorized)
                    {
                        suggestions[packageName] = keywordCategory;
                        continue;
                    }

                    // As a last resort, try to look up package info online
                    var onlineInfo = await _packageOperations.CheckIfPackageExistsAsync(packageName);
                    if (onlineInfo != null && !string.IsNullOrEmpty(onlineInfo.Description))
                    {
                        // Use description to suggest a category
                        suggestions[packageName] = SuggestCategoryFromKeywords(packageName, onlineInfo.Description);
                    }
                    else
                    {
                        // Default to Uncategorized
                        suggestions[packageName] = PackageCategory.Uncategorized;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError($"Error suggesting categories: {ex.Message}");
            }

            return suggestions;
        }

        #region Helper Methods

        /// <summary>
        /// Fills a dictionary with mappings of known package names to their categories
        /// </summary>
        private void FillKnownPackageCategories(Dictionary<string, PackageCategory> packageToCategory)
        {
            // Common machine learning packages
            AddPackageCategory(packageToCategory, PackageCategory.MachineLearning, new[] {
                "tensorflow", "keras", "torch", "pytorch", "scikit-learn", "sklearn", "xgboost", "lightgbm",
                "catboost", "fastai", "transformers", "huggingface-hub", "spacy", "gensim", "nltk",
                "bert", "sentence-transformers", "autogluon", "cuml", "mxnet", "theano"
            });

            // Data science & analytics packages
            AddPackageCategory(packageToCategory, PackageCategory.DataScience, new[] {
                "numpy", "pandas", "scipy", "statsmodels", "pymc", "prophet", "dask", "vaex",
                "polars", "datatable", "pyarrow", "jupyter", "ipython", "seaborn", "matplotlib",
                "plotly", "bokeh", "altair", "dash", "streamlit", "panel", "holoviews"
            });

            // Web development packages
            AddPackageCategory(packageToCategory, PackageCategory.WebDevelopment, new[] {
                "flask", "django", "fastapi", "uvicorn", "gunicorn", "starlette", "tornado", "bottle",
                "pyramid", "sanic", "aiohttp", "falcon", "requests", "httpx", "werkzeug", "jinja2",
                "websockets", "selenium", "scrapy", "beautifulsoup4", "bs4"
            });

            // Database packages
            AddPackageCategory(packageToCategory, PackageCategory.Database, new[] {
                "sqlalchemy", "pymongo", "redis", "elasticsearch", "psycopg2", "mysql-connector-python",
                "pymysql", "sqlmodel", "peewee", "clickhouse-driver", "pymssql", "sqlite3", "duckdb",
                "postgresql", "cassandra-driver", "neo4j", "pyspark"
            });

            // Graphics & visualization packages
            AddPackageCategory(packageToCategory, PackageCategory.Graphics, new[] {
                "pillow", "opencv-python", "pycairo", "pyglet", "pygame", "vtk", "vispy", "mayavi",
                "moderngl", "ursina", "pyray", "pyqtgraph", "opengl", "glumpy", "pptk", "blender"
            });

            // User interface packages
            AddPackageCategory(packageToCategory, PackageCategory.UserInterface, new[] {
                "pyqt", "pyside", "wxpython", "kivy", "tkinter", "pygtk", "pygobject", "eel",
                "dearpygui", "customtkinter", "flet", "ttkbootstrap", "pywebview", "gradio"
            });

            // Utilities & tools
            AddPackageCategory(packageToCategory, PackageCategory.Utilities, new[] {
                "tqdm", "rich", "click", "typer", "pydantic", "loguru", "pendulum", "python-dotenv",
                "dateutil", "marshmallow", "pytz", "cython", "numba", "joblib", "dill", "ujson"
            });

            // Testing packages
            AddPackageCategory(packageToCategory, PackageCategory.Testing, new[] {
                "pytest", "unittest", "nose", "coverage", "hypothesis", "mock", "robotframework",
                "behave", "doctest", "pytest-cov", "pylint", "mypy", "flake8"
            });

            // Security packages
            AddPackageCategory(packageToCategory, PackageCategory.Security, new[] {
                "cryptography", "pyopenssl", "passlib", "bcrypt", "pyjwt", "authlib", "oauthlib",
                "python-jose", "paramiko", "pycryptodome", "pyotp", "scrypt"
            });
        }

        private void AddPackageCategory(Dictionary<string, PackageCategory> dict, PackageCategory category, string[] packages)
        {
            foreach (var package in packages)
            {
                if (!dict.ContainsKey(package))
                {
                    dict[package] = category;
                }
            }
        }

        /// <summary>
        /// Initialize category keyword mapping for content-based categorization
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> InitializeCategoryKeywords()
        {
            return new Dictionary<string, Dictionary<string, string>>
            {
                [nameof(PackageCategory.Graphics)] = new Dictionary<string, string> {
                    { "image", "high" }, { "graphic", "high" }, { "plot", "high" }, { "chart", "high" }, 
                    { "visualization", "high" }, { "draw", "medium" }, { "render", "medium" }, 
                    { "color", "low" }, { "picture", "low" }
                },
                
                [nameof(PackageCategory.MachineLearning)] = new Dictionary<string, string> {
                    { "machine learning", "high" }, { "ml", "high" }, { "classifier", "high" }, 
                    { "deep learning", "high" }, { "neural network", "high" }, { "ai", "high" }, 
                    { "artificial intelligence", "high" }, { "prediction", "medium" }, 
                    { "train", "low" }, { "model", "low" }
                },
                
                [nameof(PackageCategory.DataScience)] = new Dictionary<string, string> {
                    { "data science", "high" }, { "analysis", "high" }, { "analytics", "high" }, 
                    { "dataframe", "high" }, { "dataset", "high" }, { "statistical", "high" }, 
                    { "statistics", "high" }, { "data", "medium" }, { "feature", "low" }
                },
                
                [nameof(PackageCategory.WebDevelopment)] = new Dictionary<string, string> {
                    { "web", "high" }, { "http", "high" }, { "html", "high" }, { "css", "high" }, 
                    { "javascript", "high" }, { "api", "high" }, { "rest", "high" }, 
                    { "flask", "high" }, { "django", "high" }, { "server", "medium" }, 
                    { "client", "medium" }, { "request", "medium" }
                },
                
                [nameof(PackageCategory.Database)] = new Dictionary<string, string> {
                    { "database", "high" }, { "sql", "high" }, { "db", "high" }, { "orm", "high" }, 
                    { "query", "high" }, { "storage", "medium" }, { "repository", "medium" }, 
                    { "mongo", "high" }, { "redis", "high" }, { "postgres", "high" },
                    { "table", "low" }, { "record", "low" }
                },

                [nameof(PackageCategory.UserInterface)] = new Dictionary<string, string> {
                    { "ui", "high" }, { "gui", "high" }, { "interface", "high" }, { "widget", "high" }, 
                    { "window", "high" }, { "dialog", "high" }, { "form", "medium" }, 
                    { "button", "medium" }, { "control", "medium" }
                },
                
                [nameof(PackageCategory.Testing)] = new Dictionary<string, string> {
                    { "test", "high" }, { "unittest", "high" }, { "pytest", "high" }, 
                    { "mock", "high" }, { "fixture", "high" }, { "assertion", "high" }, 
                    { "quality assurance", "high" }, { "qa", "medium" }, { "coverage", "medium" }
                },
                
                [nameof(PackageCategory.Utilities)] = new Dictionary<string, string> {
                    { "utility", "high" }, { "helper", "high" }, { "tool", "high" }, 
                    { "common", "medium" }, { "convenience", "medium" }, { "util", "medium" }
                }
            };
        }

        /// <summary>
        /// Suggests a category based on package name and description
        /// </summary>
        private PackageCategory SuggestCategoryFromKeywords(string packageName, string description)
        {
            if (string.IsNullOrEmpty(packageName))
                return PackageCategory.Uncategorized;

            // Combine name and description for better matching
            string text = (packageName + " " + (description ?? "")).ToLowerInvariant();

            // Calculate scores for each category based on keyword matches
            var scores = new Dictionary<PackageCategory, double>();
            
            foreach (var categoryEntry in _categoryKeywords)
            {
                PackageCategory category;
                if (Enum.TryParse(categoryEntry.Key, out category))
                {
                    double score = 0;
                    
                    // Check each keyword
                    foreach (var keywordEntry in categoryEntry.Value)
                    {
                        string keyword = keywordEntry.Key;
                        string weight = keywordEntry.Value;
                        
                        // Apply different weights based on keyword importance
                        double weightValue = weight == "high" ? 3.0 : (weight == "medium" ? 2.0 : 1.0);
                        
                        // Give more weight to exact package name matches
                        if (packageName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            score += weightValue * 2;
                        }
                        // Less weight for description matches
                        else if (text.Contains(keyword))
                        {
                            score += weightValue;
                        }
                    }
                    
                    scores[category] = score;
                }
            }

            // Return the category with the highest score if it's above threshold
            var topCategory = scores.OrderByDescending(s => s.Value).FirstOrDefault();
            return topCategory.Value >= 2.0 ? topCategory.Key : PackageCategory.Uncategorized;
        }

        private void ReportProgress(string message)
        {
            _progress?.Report(new PassedArgs { Messege = message });
            
            // Log to editor if available
            _beepService.DMEEditor?.AddLogMessage("Package Category Manager", message, DateTime.Now, -1, null, Errors.Ok);
        }

        private void ReportError(string message)
        {
            _progress?.Report(new PassedArgs
            {
                Messege = message,
                EventType = "Error",
                Flag = Errors.Failed
            });

            // Log to editor
            _beepService.DMEEditor?.AddLogMessage("Package Category Manager", message, DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion
    }
}