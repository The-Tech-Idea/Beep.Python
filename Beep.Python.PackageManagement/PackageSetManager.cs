using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Beep.Python.Model;
using TheTechIdea.Beep.Addin;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.RuntimeEngine.PackageManagement
{
    /// <summary>
    /// Manages predefined sets of Python packages
    /// </summary>
    public class PackageSetManager
    {
        private readonly IBeepService _beepService;
        private readonly PythonPackageManager _packageManager;
        private readonly RequirementsFileManager _requirementsManager;
        private readonly IProgress<PassedArgs> _progress;
        private Dictionary<string, PackageSet> _packageSets;

        public PackageSetManager(
            IBeepService beepService,
            PythonPackageManager packageManager,
            RequirementsFileManager requirementsManager,
            IProgress<PassedArgs> progress = null)
        {
            _beepService = beepService ?? throw new ArgumentNullException(nameof(beepService));
            _packageManager = packageManager ?? throw new ArgumentNullException(nameof(packageManager));
            _requirementsManager = requirementsManager ?? throw new ArgumentNullException(nameof(requirementsManager));
            _progress = progress;
            
            InitializePackageSets();
        }

        /// <summary>
        /// Gets a dictionary of all available package sets
        /// </summary>
        public Dictionary<string, PackageSet> GetAllPackageSets()
        {
            return _packageSets;
        }

        /// <summary>
        /// Gets information about all available package sets
        /// </summary>
        public Dictionary<string, List<PackageDefinition>> GetAvailablePackageSets()
        {
            var results = new Dictionary<string, List<PackageDefinition>>();

            foreach (var kvp in _packageSets)
            {
                results[kvp.Key] = kvp.Value.Packages.ToList();
            }

            return results;
        }

        /// <summary>
        /// Installs all packages from a predefined package set
        /// </summary>
        /// <param name="setName">Name of the package set to install</param>
        /// <param name="environment">Target environment</param>
        /// <returns>True if all packages were installed successfully</returns>
        public async Task<bool> InstallPackageSetAsync(string setName, PythonVirtualEnvironment environment)
        {
            if (string.IsNullOrEmpty(setName) || environment == null)
            {
                ReportError($"Invalid package set name or environment");
                return false;
            }

            try
            {
                // Find the requested package set
                if (!_packageSets.TryGetValue(setName.ToLowerInvariant(), out var packageSet))
                {
                    ReportError($"Package set '{setName}' not found");
                    return false;
                }

                // Temporarily disable auto-updates during batch operation
                bool originalAutoUpdate = environment.AutoUpdateRequirements;
                environment.AutoUpdateRequirements = false;

                // Install packages from the set
                bool success = true;
                int totalPackages = packageSet.Packages.Count;
                int current = 0;

                ReportProgress($"Installing {totalPackages} packages from set '{packageSet.Name}'...");

                foreach (var package in packageSet.Packages)
                {
                    current++;
                    string packageSpec = package.PackageName;

                    // Add version constraint if specified
                    if (packageSet.Versions.TryGetValue(package.PackageName, out var version) && !string.IsNullOrEmpty(version))
                    {
                        packageSpec = $"{package.PackageName}{version}";
                    }

                    ReportProgress($"Installing {packageSpec} ({current}/{totalPackages})");
                    bool installResult = await _packageManager.InstallPackageAsync(packageSpec, environment);

                    if (!installResult)
                    {
                        ReportError($"Failed to install {packageSpec}");
                        success = false;
                    }
                }

                // Restore original auto-update setting
                environment.AutoUpdateRequirements = originalAutoUpdate;

                // If auto-update is enabled, update the requirements file
                if (originalAutoUpdate && !string.IsNullOrEmpty(environment.RequirementsFile))
                {
                    await _requirementsManager.GenerateRequirementsFileAsync(environment.RequirementsFile, environment);
                }

                ReportProgress($"Completed installing package set '{packageSet.Name}'. {(success ? "All packages installed successfully." : "Some packages failed to install.")}");
                return success;
            }
            catch (Exception ex)
            {
                ReportError($"Error installing package set: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new package set from the currently installed packages in an environment
        /// </summary>
        /// <param name="setName">Name for the new package set</param>
        /// <param name="environment">Source environment</param>
        /// <param name="description">Description for the package set</param>
        /// <returns>True if the set was created successfully</returns>
        public async Task<bool> SavePackageSetFromEnvironmentAsync(
            string setName, 
            PythonVirtualEnvironment environment, 
            string description = "")
        {
            if (string.IsNullOrEmpty(setName) || environment == null)
            {
                ReportError("Invalid package set name or environment");
                return false;
            }

            try
            {
                // Ensure we have the latest package data
                var packages = await _packageManager.GetAllPackagesAsync(environment);
                if (packages == null || packages.Count == 0)
                {
                    ReportError("No packages found in environment to save as a package set");
                    return false;
                }

                // Determine the dominant category in the current packages
                var categoryCount = packages
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                var dominantCategory = categoryCount.First().Category;

                // Create the package set
                var packageSet = new PackageSet
                {
                    Name = setName,
                    Description = string.IsNullOrEmpty(description)
                        ? $"Package set created from {environment.Name} on {DateTime.Now}"
                        : description,
                    Category = dominantCategory,
                    Packages = packages,
                    Versions = packages
                        .Where(p => !string.IsNullOrEmpty(p.Version))
                        .ToDictionary(
                            p => p.PackageName,
                            p => $"=={p.Version}"
                        )
                };

                // Add to our package sets collection
                string key = setName.ToLowerInvariant().Replace(" ", "_");
                _packageSets[key] = packageSet;

                // Save to a file if requirements directory exists
                string requirementsDir = _requirementsManager.GetDefaultRequirementsDirectory();
                string outputPath = Path.Combine(requirementsDir, $"{key}.txt");

                // Generate requirements file content
                await _requirementsManager.GenerateRequirementsFileAsync(outputPath, environment, true);

                ReportProgress($"Saved package set '{setName}' with {packageSet.Packages.Count} packages to {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Error saving package set: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads package sets from requirements files in the default directory
        /// </summary>
        public void LoadPackageSetsFromFiles()
        {
            try
            {
                string requirementsDir = _requirementsManager.GetDefaultRequirementsDirectory();
                if (!Directory.Exists(requirementsDir))
                {
                    return;
                }

                foreach (string filePath in Directory.GetFiles(requirementsDir, "*.txt"))
                {
                    try
                    {
                        string setName = Path.GetFileNameWithoutExtension(filePath);
                        var requirements = _requirementsManager.ReadRequirementsFile(filePath);

                        if (requirements.Count > 0)
                        {
                            // Create package definitions from requirements
                            var packages = new List<PackageDefinition>();
                            foreach (var req in requirements)
                            {
                                packages.Add(new PackageDefinition
                                {
                                    PackageName = req.Key,
                                    Version = req.Value.TrimStart('=', '>', '<', '~'),
                                    Status = PackageStatus.Available
                                });
                            }

                            // Create package set
                            var packageSet = new PackageSet
                            {
                                Name = setName.Replace('_', ' '),
                                Description = $"Package set loaded from {Path.GetFileName(filePath)}",
                                Category = DetermineSetCategory(packages),
                                Packages = packages,
                                Versions = requirements.ToDictionary(
                                    r => r.Key,
                                    r => r.Value
                                )
                            };

                            _packageSets[setName.ToLowerInvariant()] = packageSet;
                        }
                    }
                    catch (Exception ex)
                    {
                        ReportError($"Error loading package set from {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ReportError($"Error loading package sets: {ex.Message}");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Initializes the dictionary of predefined package sets
        /// </summary>
        private void InitializePackageSets()
        {
            _packageSets = new Dictionary<string, PackageSet>(StringComparer.OrdinalIgnoreCase);

            // Data science essentials
            var dataScience = new PackageSet
            {
                Name = "Data Science Essentials",
                Description = "Essential packages for data science and analysis",
                Category = PackageCategory.DataScience,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "numpy", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "pandas", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "matplotlib", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "seaborn", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "jupyter", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "ipython", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["data_science_essentials"] = dataScience;

            // Machine learning basics
            var mlBasics = new PackageSet
            {
                Name = "Machine Learning Basics",
                Description = "Basic machine learning packages for getting started",
                Category = PackageCategory.MachineLearning,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "scikit-learn", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "scipy", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "numpy", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "pandas", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "matplotlib", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["ml_basics"] = mlBasics;

            // Web development
            var webDev = new PackageSet
            {
                Name = "Web Development",
                Description = "Packages for web development and APIs",
                Category = PackageCategory.WebDevelopment,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "flask", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "requests", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "jinja2", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "werkzeug", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "gunicorn", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["web_development"] = webDev;

            // Deep learning
            var deepLearning = new PackageSet
            {
                Name = "Deep Learning",
                Description = "Packages for deep learning and neural networks",
                Category = PackageCategory.MachineLearning,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "tensorflow", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "keras", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "torch", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "torchvision", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "numpy", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["deep_learning"] = deepLearning;

            // Transformer and multimodal AI workloads
            var transformers = new PackageSet
            {
                Name = "AI Transformers",
                Description = "Packages for Hugging Face, OpenAI, Azure, and multimodal transformer orchestration",
                Category = PackageCategory.MachineLearning,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "transformers", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "accelerate", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "datasets", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "safetensors", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "sentencepiece", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "torch", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "bitsandbytes", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["ai_transformers"] = transformers;

            // Vector store integrations
            var vectorStores = new PackageSet
            {
                Name = "Vector Stores",
                Description = "Vector database connectors and embedding utilities for retrieval-augmented generation",
                Category = PackageCategory.VectorDB,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "faiss-cpu", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "qdrant-client", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "pinecone-client", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "chromadb", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "weaviate-client", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "sentence-transformers", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["vector_stores"] = vectorStores;

            // Streaming ingestion connectors
            var streaming = new PackageSet
            {
                Name = "Streaming Ingestion",
                Description = "Event streaming connectors and processing frameworks for real-time ingestion",
                Category = PackageCategory.Networking,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "kafka-python", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "confluent-kafka", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "fastavro", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "pyspark", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "watchdog", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "aiohttp", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["streaming_ingestion"] = streaming;

            // Document AI pipelines
            var documentAi = new PackageSet
            {
                Name = "Document AI",
                Description = "OCR, PDF processing, and layout understanding packages for document enrichment",
                Category = PackageCategory.FileProcessing,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "pytesseract", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "pypdf", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "pdfplumber", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "python-docx", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "textract", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "layoutparser", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["document_ai"] = documentAi;

            // Agentic automation
            var autoAgents = new PackageSet
            {
                Name = "Auto Agents",
                Description = "Agent orchestration, planning, and tool integration packages",
                Category = PackageCategory.Ragging,
                Packages = new List<PackageDefinition>
                {
                    new PackageDefinition { PackageName = "langchain", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "langgraph", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "openai", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "anthropic", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "google-generativeai", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "cohere", Status = PackageStatus.Available },
                    new PackageDefinition { PackageName = "tenacity", Status = PackageStatus.Available }
                },
                Versions = new Dictionary<string, string>()
            };
            _packageSets["auto_agents"] = autoAgents;

            // Load any custom package sets from files
            LoadPackageSetsFromFiles();
        }

        /// <summary>
        /// Determines the most appropriate category for a set based on its packages
        /// </summary>
        private PackageCategory DetermineSetCategory(List<PackageDefinition> packages)
        {
            if (packages == null || packages.Count == 0)
                return PackageCategory.Uncategorized;

            // If packages already have categories, use the most common one
            var categorized = packages.Where(p => p.Category != PackageCategory.Uncategorized).ToList();
            if (categorized.Count > 0)
            {
                return categorized
                    .GroupBy(p => p.Category)
                    .OrderByDescending(g => g.Count())
                    .First()
                    .Key;
            }

            // Otherwise, try to determine from well-known packages
            if (packages.Any(p => p.PackageName?.Contains("tensorflow") == true ||
                               p.PackageName?.Contains("keras") == true ||
                               p.PackageName?.Contains("torch") == true ||
                               p.PackageName?.Contains("sklearn") == true))
            {
                return PackageCategory.MachineLearning;
            }
            
            if (packages.Any(p => p.PackageName?.Contains("numpy") == true &&
                               packages.Any(p2 => p2.PackageName?.Contains("pandas") == true)))
            {
                return PackageCategory.DataScience;
            }
            
            if (packages.Any(p => p.PackageName?.Contains("flask") == true ||
                               p.PackageName?.Contains("django") == true ||
                               p.PackageName?.Contains("requests") == true))
            {
                return PackageCategory.WebDevelopment;
            }
            
            if (packages.Any(p => p.PackageName?.Contains("pillow") == true ||
                              p.PackageName?.Contains("opencv") == true ||
                              p.PackageName?.Contains("matplotlib") == true))
            {
                return PackageCategory.Graphics;
            }

            if (packages.Any(p => p.PackageName?.Contains("sqlalchemy") == true ||
                               p.PackageName?.Contains("psycopg") == true ||
                               p.PackageName?.Contains("mysql") == true ||
                               p.PackageName?.Contains("sqlite") == true))
            {
                return PackageCategory.Database;
            }

            return PackageCategory.Uncategorized;
        }

        private void ReportProgress(string message)
        {
            _progress?.Report(new PassedArgs { Messege = message });
            
            // Log to editor if available
            _beepService.DMEEditor?.AddLogMessage("Package Set Manager", message, DateTime.Now, -1, null, Errors.Ok);
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
            _beepService.DMEEditor?.AddLogMessage("Package Set Manager", message, DateTime.Now, -1, null, Errors.Failed);
        }

        #endregion
    }
}
