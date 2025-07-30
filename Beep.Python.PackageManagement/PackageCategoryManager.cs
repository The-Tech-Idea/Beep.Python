using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Python.Runtime;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Enhanced package category manager with session management, virtual environment support,
    /// and intelligent categorization for Python packages
    /// </summary>
    public class PackageCategoryManager : IDisposable
    {
        #region Private Fields
        private readonly object _operationLock = new object();
        private volatile bool _isDisposed = false;
        
        private readonly IBeepService _beepService;
        private readonly PythonPackageManager _packageManager;
        private readonly IProgress<PassedArgs> _progress;
        private readonly Dictionary<string, Dictionary<string, string>> _categoryKeywords;
        
        // Session and Environment management
        private PythonSessionInfo? _configuredSession;
        private PythonVirtualEnvironment? _configuredEnvironment;
        private PyModule? _sessionScope;
        #endregion

        #region Constructor
        public PackageCategoryManager(
            IBeepService beepService,
            PythonPackageManager packageManager,
            IProgress<PassedArgs> progress = null)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _packageManager = packageManager ?? throw new ArgumentNullException(nameof(packageManager));
            _progress = progress;
            
            _categoryKeywords = InitializeCategoryKeywords();
        }
        #endregion

        #region Session and Environment Configuration
        /// <summary>
        /// Configure the category manager to use a specific session and environment
        /// </summary>
        /// <param name="session">Python session to use for operations</param>
        /// <param name="environment">Virtual environment for category operations</param>
        /// <returns>True if configuration successful</returns>
        public bool ConfigureSession(PythonSessionInfo session, PythonVirtualEnvironment environment)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (environment == null)
                throw new ArgumentNullException(nameof(environment));

            _configuredSession = session;
            _configuredEnvironment = environment;

            return true;
        }

        /// <summary>
        /// Check if session is properly configured
        /// </summary>
        /// <returns>True if session and environment are configured</returns>
        public bool IsSessionConfigured()
        {
            return _configuredSession != null && _configuredEnvironment != null;
        }

        private bool ValidateSessionAndEnvironment()
        {
            if (!IsSessionConfigured())
            {
                ReportError("Session and environment should be configured for optimal category operations.");
                // Don't return false - category operations can work without sessions
            }
            return true;
        }
        #endregion

        #region Core Helper Methods
        /// <summary>
        /// Executes code safely within the session context if available
        /// </summary>
        /// <param name="action">Action to execute in session</param>
        private void ExecuteInSessionIfAvailable(Action action)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PackageCategoryManager));

            lock (_operationLock)
            {
                try
                {
                    action();
                }
                catch (PythonException pythonEx)
                {
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Category operation error: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Executes code safely within the session context and returns a result if available
        /// </summary>
        /// <typeparam name="T">Type of result to return</typeparam>
        /// <param name="func">Function to execute in session</param>
        /// <returns>Result of the function</returns>
        private T ExecuteInSessionIfAvailable<T>(Func<T> func)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PackageCategoryManager));

            lock (_operationLock)
            {
                try
                {
                    return func();
                }
                catch (PythonException pythonEx)
                {
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Category operation error: {ex.Message}", ex);
                }
            }
        }
        #endregion

        #region Package Category Operations
        /// <summary>
        /// Gets packages in a specific category with session support
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

            try
            {
                return ExecuteInSessionIfAvailable(() =>
                {
                    var packages = environment.InstalledPackages
                        .Where(p => p.Category == category)
                        .ToList();
                        
                    ReportProgress($"Found {packages.Count} packages in category '{category}'");
                    return packages;
                });
            }
            catch (Exception ex)
            {
                ReportError($"Error getting packages by category: {ex.Message}");
                return new List<PackageDefinition>();
            }
        }

        /// <summary>
        /// Sets the category for a specific package with session support
        /// </summary>
        /// <param name="environment">The environment containing the package</param>
        /// <param name="packageName">Name of the package</param>
        /// <param name="category">Category to assign</param>
        public void SetPackageCategory(PythonVirtualEnvironment environment, string packageName, PackageCategory category)
        {
            if (environment?.InstalledPackages == null || string.IsNullOrEmpty(packageName))
            {
                ReportError("Invalid environment or package name");
                return;
            }

            try
            {
                ExecuteInSessionIfAvailable(() =>
                {
                    var package = environment.InstalledPackages.FirstOrDefault(p =>
                        p.PackageName != null &&
                        p.PackageName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

                    if (package != null)
                    {
                        var oldCategory = package.Category;
                        package.Category = category;
                        ReportProgress($"Updated package '{packageName}' category from '{oldCategory}' to '{category}'");
                    }
                    else
                    {
                        ReportError($"Package '{packageName}' not found in environment");
                    }
                });
            }
            catch (Exception ex)
            {
                ReportError($"Error setting package category: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates categories for multiple packages at once with session support
        /// </summary>
        /// <param name="environment">The environment containing the packages</param>
        /// <param name="packageCategories">Dictionary of package names and categories</param>
        public void UpdatePackageCategories(
            PythonVirtualEnvironment environment, 
            Dictionary<string, PackageCategory> packageCategories)
        {
            if (environment?.InstalledPackages == null || packageCategories == null || packageCategories.Count == 0)
            {
                ReportError("Invalid environment or package categories");
                return;
            }

            try
            {
                ExecuteInSessionIfAvailable(() =>
                {
                    int updatedCount = 0;
                    
                    foreach (var kvp in packageCategories)
                    {
                        var package = environment.InstalledPackages.FirstOrDefault(p =>
                            p.PackageName != null &&
                            p.PackageName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

                        if (package != null)
                        {
                            package.Category = kvp.Value;
                            updatedCount++;
                        }
                    }

                    ReportProgress($"Updated categories for {updatedCount} out of {packageCategories.Count} packages");
                });
            }
            catch (Exception ex)
            {
                ReportError($"Error updating package categories: {ex.Message}");
            }
        }

        /// <summary>
        /// Populates common package categories based on known package names with session support
        /// </summary>
        /// <param name="environment">The environment containing packages</param>
        /// <returns>True if any packages were categorized</returns>
        public bool PopulateCommonPackageCategories(PythonVirtualEnvironment environment)
        {
            if (environment?.InstalledPackages == null || environment.InstalledPackages.Count == 0)
            {
                ReportError("No packages found in environment to categorize");
                return false;
            }

            try
            {
                return ExecuteInSessionIfAvailable(() =>
                {
                    int categorizedCount = 0;
                    var packageToCategory = new Dictionary<string, PackageCategory>(StringComparer.OrdinalIgnoreCase);

                    // Build a map of known packages to categories
                    FillKnownPackageCategories(packageToCategory);
                    
                    ReportProgress($"Starting categorization of {environment.InstalledPackages.Count} packages...");

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

                    string resultMessage = $"Categorized {categorizedCount} out of {environment.InstalledPackages.Count} packages";
                    ReportProgress(resultMessage);
                    
                    return categorizedCount > 0;
                });
            }
            catch (Exception ex)
            {
                ReportError($"Error populating package categories: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Suggests categories for packages based on their names and descriptions with enhanced session support
        /// </summary>
        /// <param name="packageNames">List of package names to categorize</param>
        /// <param name="environment">Environment for context</param>
        /// <returns>Dictionary mapping package names to suggested categories</returns>
        public async Task<Dictionary<string, PackageCategory>> SuggestCategoriesForPackagesAsync(
            IEnumerable<string> packageNames,
            PythonVirtualEnvironment environment)
        {
            var suggestions = new Dictionary<string, PackageCategory>();

            if (packageNames == null || !packageNames.Any() || environment == null)
            {
                ReportError("Invalid package names or environment for category suggestion");
                return suggestions;
            }

            try
            {
                ReportProgress($"Suggesting categories for {packageNames.Count()} packages...");
                
                var knownCategories = new Dictionary<string, PackageCategory>(StringComparer.OrdinalIgnoreCase);
                FillKnownPackageCategories(knownCategories);

                int processedCount = 0;
                foreach (var packageName in packageNames)
                {
                    processedCount++;
                    
                    // Report progress periodically
                    if (processedCount % 10 == 0 || processedCount == packageNames.Count())
                    {
                        ReportProgress($"Processing package {processedCount}/{packageNames.Count()}: {packageName}");
                    }

                    // First check if it's a known package
                    if (knownCategories.TryGetValue(packageName, out var category))
                    {
                        suggestions[packageName] = category;
                        continue;
                    }

                    // If it's in our environment with a non-Uncategorized category, use that
                    var existingPackage = environment.InstalledPackages?.FirstOrDefault(p =>
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
                    try
                    {
                        var onlineInfo = await _packageManager.CheckIfPackageExistsAsync(packageName);
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
                    catch (Exception ex)
                    {
                        // If online lookup fails, default to uncategorized
                        ReportError($"Failed to lookup package {packageName} online: {ex.Message}");
                        suggestions[packageName] = PackageCategory.Uncategorized;
                    }
                }

                ReportProgress($"Completed category suggestions for {suggestions.Count} packages");
            }
            catch (Exception ex)
            {
                ReportError($"Error suggesting categories: {ex.Message}");
            }

            return suggestions;
        }

        /// <summary>
        /// Gets category statistics for an environment
        /// </summary>
        /// <param name="environment">The environment to analyze</param>
        /// <returns>Dictionary of categories and their package counts</returns>
        public Dictionary<PackageCategory, int> GetCategoryStatistics(PythonVirtualEnvironment environment)
        {
            var statistics = new Dictionary<PackageCategory, int>();

            if (environment?.InstalledPackages == null)
            {
                return statistics;
            }

            try
            {
                return ExecuteInSessionIfAvailable(() =>
                {
                    var stats = environment.InstalledPackages
                        .GroupBy(p => p.Category)
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Ensure all categories are represented
                    foreach (PackageCategory category in Enum.GetValues<PackageCategory>())
                    {
                        if (!stats.ContainsKey(category))
                        {
                            stats[category] = 0;
                        }
                    }

                    ReportProgress($"Generated category statistics for {environment.InstalledPackages.Count} packages");
                    return stats;
                });
            }
            catch (Exception ex)
            {
                ReportError($"Error generating category statistics: {ex.Message}");
                return statistics;
            }
        }
        #endregion

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
                "bert", "sentence-transformers", "autogluon", "cuml", "mxnet", "theano", "stable-baselines3",
                "optuna", "hyperopt", "mlflow", "wandb", "tensorboard", "gym", "gymnasium"
            });

            // Data science & analytics packages
            AddPackageCategory(packageToCategory, PackageCategory.DataScience, new[] {
                "numpy", "pandas", "scipy", "statsmodels", "pymc", "prophet", "dask", "vaex",
                "polars", "datatable", "pyarrow", "jupyter", "ipython", "seaborn", "matplotlib",
                "plotly", "bokeh", "altair", "dash", "streamlit", "panel", "holoviews", "jupyterlab",
                "notebook", "ipywidgets", "nbconvert", "papermill", "great-expectations"
            });

            // Web development packages
            AddPackageCategory(packageToCategory, PackageCategory.WebDevelopment, new[] {
                "flask", "django", "fastapi", "uvicorn", "gunicorn", "starlette", "tornado", "bottle",
                "pyramid", "sanic", "aiohttp", "falcon", "requests", "httpx", "werkzeug", "jinja2",
                "websockets", "selenium", "scrapy", "beautifulsoup4", "bs4", "celery", "redis",
                "django-rest-framework", "flask-restful", "marshmallow", "pydantic"
            });

            // Database packages
            AddPackageCategory(packageToCategory, PackageCategory.Database, new[] {
                "sqlalchemy", "pymongo", "redis", "elasticsearch", "psycopg2", "mysql-connector-python",
                "pymysql", "sqlmodel", "peewee", "clickhouse-driver", "pymssql", "sqlite3", "duckdb",
                "postgresql", "cassandra-driver", "neo4j", "pyspark", "alembic", "databases",
                "asyncpg", "motor", "pyodbc", "cx-oracle"
            });

            // Graphics & visualization packages
            AddPackageCategory(packageToCategory, PackageCategory.Graphics, new[] {
                "pillow", "opencv-python", "pycairo", "pyglet", "pygame", "vtk", "vispy", "mayavi",
                "moderngl", "ursina", "pyray", "pyqtgraph", "opengl", "glumpy", "pptk", "blender",
                "arcade", "panda3d", "pyopengl", "skimage", "scikit-image", "imageio", "wand"
            });

            // User interface packages
            AddPackageCategory(packageToCategory, PackageCategory.UserInterface, new[] {
                "pyqt", "pyside", "wxpython", "kivy", "tkinter", "pygtk", "pygobject", "eel",
                "dearpygui", "customtkinter", "flet", "ttkbootstrap", "pywebview", "gradio",
                "nicegui", "textual", "rich", "urwid", "prompt-toolkit", "toga"
            });

            // Utilities & tools
            AddPackageCategory(packageToCategory, PackageCategory.Utilities, new[] {
                "tqdm", "rich", "click", "typer", "pydantic", "loguru", "pendulum", "python-dotenv",
                "dateutil", "marshmallow", "pytz", "cython", "numba", "joblib", "dill", "ujson",
                "fire", "argparse", "configparser", "pathlib", "shutil", "psutil", "watchdog",
                "schedule", "apscheduler", "colorama", "termcolor", "humanize"
            });

            // Testing packages
            AddPackageCategory(packageToCategory, PackageCategory.Testing, new[] {
                "pytest", "unittest", "nose", "coverage", "hypothesis", "mock", "robotframework",
                "behave", "doctest", "pytest-cov", "pylint", "mypy", "flake8", "black", "isort",
                "pre-commit", "bandit", "safety", "pytest-xdist", "pytest-mock", "factory-boy"
            });

            // Security packages
            AddPackageCategory(packageToCategory, PackageCategory.Security, new[] {
                "cryptography", "pyopenssl", "passlib", "bcrypt", "pyjwt", "authlib", "oauthlib",
                "python-jose", "paramiko", "pycryptodome", "pyotp", "scrypt", "keyring", "secrets",
                "hashlib", "ssl", "certifi", "urllib3"
            });

            // Development tools packages - Use DevTools instead of Development
            AddPackageCategory(packageToCategory, PackageCategory.DevTools, new[] {
                "pip", "setuptools", "wheel", "twine", "poetry", "pipenv", "virtualenv", "conda",
                "git", "pre-commit", "tox", "nox", "invoke", "fabric", "ansible", "docker",
                "kubernetes", "helm", "terraform"
            });

            // Vector database packages
            AddPackageCategory(packageToCategory, PackageCategory.VectorDB, new[] {
                "pinecone-client", "weaviate-client", "chromadb", "qdrant-client", "milvus",
                "faiss-cpu", "faiss-gpu", "annoy", "nmslib", "hnswlib", "pgvector"
            });

            // Embedding packages
            AddPackageCategory(packageToCategory, PackageCategory.Embedding, new[] {
                "sentence-transformers", "openai", "cohere", "langchain", "llama-index",
                "haystack-ai", "embedchain", "semantic-kernel"
            });

            // Scientific computing packages
            AddPackageCategory(packageToCategory, PackageCategory.Scientific, new[] {
                "scipy", "sympy", "astropy", "biopython", "nibabel", "nilearn", "networkx",
                "igraph", "graph-tool", "rdkit", "mdanalysis", "prody", "pymatgen"
            });

            // Math packages
            AddPackageCategory(packageToCategory, PackageCategory.Math, new[] {
                "numpy", "sympy", "mpmath", "gmpy2", "decimal", "fractions", "statistics",
                "random", "math", "cmath", "pystan", "pymc3", "pymc"
            });

            // Audio/Video packages
            AddPackageCategory(packageToCategory, PackageCategory.AudioVideo, new[] {
                "ffmpeg-python", "moviepy", "opencv-python", "librosa", "soundfile", "pydub",
                "audioread", "pyaudio", "wave", "mutagen", "python-vlc"
            });

            // File processing packages
            AddPackageCategory(packageToCategory, PackageCategory.FileProcessing, new[] {
                "openpyxl", "xlsxwriter", "pandas", "python-docx", "pypdf2", "pdfplumber",
                "xlrd", "xlwt", "csvkit", "tabula-py", "camelot-py", "pdfminer"
            });

            // Networking packages
            AddPackageCategory(packageToCategory, PackageCategory.Networking, new[] {
                "requests", "urllib3", "httpx", "aiohttp", "socket", "asyncio", "twisted",
                "paramiko", "fabric", "netmiko", "scapy", "dnspython"
            });

            // Documentation packages
            AddPackageCategory(packageToCategory, PackageCategory.Documentation, new[] {
                "sphinx", "mkdocs", "jupyter", "nbconvert", "pandoc", "gitbook",
                "pydoc", "docutils", "recommonmark", "myst-parser"
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
                    { "color", "low" }, { "picture", "low" }, { "canvas", "medium" }, { "pixel", "medium" }
                },
                
                [nameof(PackageCategory.MachineLearning)] = new Dictionary<string, string> {
                    { "machine learning", "high" }, { "ml", "high" }, { "classifier", "high" }, 
                    { "deep learning", "high" }, { "neural network", "high" }, { "ai", "high" }, 
                    { "artificial intelligence", "high" }, { "prediction", "medium" }, 
                    { "train", "low" }, { "model", "low" }, { "algorithm", "medium" }, { "supervised", "high" },
                    { "unsupervised", "high" }, { "reinforcement", "high" }, { "regression", "high" }
                },
                
                [nameof(PackageCategory.DataScience)] = new Dictionary<string, string> {
                    { "data science", "high" }, { "analysis", "high" }, { "analytics", "high" }, 
                    { "dataframe", "high" }, { "dataset", "high" }, { "statistical", "high" }, 
                    { "statistics", "high" }, { "data", "medium" }, { "feature", "low" },
                    { "exploration", "medium" }, { "visualization", "medium" }, { "notebook", "medium" }
                },
                
                [nameof(PackageCategory.WebDevelopment)] = new Dictionary<string, string> {
                    { "web", "high" }, { "http", "high" }, { "html", "high" }, { "css", "high" }, 
                    { "javascript", "high" }, { "api", "high" }, { "rest", "high" }, 
                    { "flask", "high" }, { "django", "high" }, { "server", "medium" }, 
                    { "client", "medium" }, { "request", "medium" }, { "endpoint", "medium" },
                    { "route", "medium" }, { "middleware", "medium" }
                },
                
                [nameof(PackageCategory.Database)] = new Dictionary<string, string> {
                    { "database", "high" }, { "sql", "high" }, { "db", "high" }, { "orm", "high" }, 
                    { "query", "high" }, { "storage", "medium" }, { "repository", "medium" }, 
                    { "mongo", "high" }, { "redis", "high" }, { "postgres", "high" },
                    { "table", "low" }, { "record", "low" }, { "migration", "medium" }, { "schema", "medium" }
                },

                [nameof(PackageCategory.UserInterface)] = new Dictionary<string, string> {
                    { "ui", "high" }, { "gui", "high" }, { "interface", "high" }, { "widget", "high" }, 
                    { "window", "high" }, { "dialog", "high" }, { "form", "medium" }, 
                    { "button", "medium" }, { "control", "medium" }, { "desktop", "medium" },
                    { "application", "low" }, { "interactive", "medium" }
                },
                
                [nameof(PackageCategory.Testing)] = new Dictionary<string, string> {
                    { "test", "high" }, { "unittest", "high" }, { "pytest", "high" }, 
                    { "mock", "high" }, { "fixture", "high" }, { "assertion", "high" }, 
                    { "quality assurance", "high" }, { "qa", "medium" }, { "coverage", "medium" },
                    { "lint", "medium" }, { "check", "low" }, { "validate", "medium" }
                },
                
                [nameof(PackageCategory.Utilities)] = new Dictionary<string, string> {
                    { "utility", "high" }, { "helper", "high" }, { "tool", "high" }, 
                    { "common", "medium" }, { "convenience", "medium" }, { "util", "medium" },
                    { "library", "low" }, { "framework", "low" }, { "collection", "medium" }
                },

                [nameof(PackageCategory.Security)] = new Dictionary<string, string> {
                    { "security", "high" }, { "crypto", "high" }, { "encryption", "high" },
                    { "authentication", "high" }, { "authorization", "high" }, { "password", "high" },
                    { "token", "medium" }, { "ssl", "high" }, { "tls", "high" }, { "certificate", "medium" },
                    { "hash", "medium" }, { "signature", "medium" }
                },

                [nameof(PackageCategory.DevTools)] = new Dictionary<string, string> {
                    { "development", "high" }, { "build", "high" }, { "deploy", "high" },
                    { "package", "medium" }, { "tool", "high" }, { "cli", "medium" },
                    { "command line", "medium" }, { "automation", "medium" }
                },

                [nameof(PackageCategory.VectorDB)] = new Dictionary<string, string> {
                    { "vector", "high" }, { "embedding", "high" }, { "similarity", "high" },
                    { "search", "medium" }, { "index", "medium" }, { "retrieval", "medium" }
                },

                [nameof(PackageCategory.Scientific)] = new Dictionary<string, string> {
                    { "scientific", "high" }, { "research", "high" }, { "computation", "high" },
                    { "simulation", "medium" }, { "modeling", "medium" }, { "physics", "high" },
                    { "chemistry", "high" }, { "biology", "high" }
                }
            };
        }

        /// <summary>
        /// Suggests a category based on package name and description with enhanced keyword matching
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
                if (Enum.TryParse(categoryEntry.Key, out PackageCategory category))
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

        #region IDisposable Implementation
        /// <summary>
        /// Disposes resources used by the package category manager
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                try
                {
                    // Clean up any resources
                }
                catch (Exception ex)
                {
                    // Log disposal errors but don't throw
                    Console.WriteLine($"Warning during PackageCategoryManager disposal: {ex.Message}");
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }
        #endregion
    }
}