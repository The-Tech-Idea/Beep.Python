using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Beep.Python.Model;
using Beep.Python.Services;

namespace Beep.Python.Winform.PackageManagement
{
    public partial class uc_Packages : UserControl
    {
        private IPythonPackageManager? _packageManager;
        private IPythonVirtualEnvManager? _virtualEnvManager;

        private readonly List<PackageSetViewModel> _packageSetViewModels = new();
        private readonly object _sessionSync = new();

        private CancellationTokenSource? _installCts;
        private bool _isInstalling;
        private bool _initialized;

        private static readonly Dictionary<string, (string Name, string Description)> PackageSetMetadata = new(StringComparer.OrdinalIgnoreCase)
        {
            ["data_science_essentials"] = ("Data Science Essentials", "Essential libraries for dataframe manipulation, charting, and notebooks."),
            ["ml_basics"] = ("Machine Learning Basics", "Core packages for classical machine learning workflows."),
            ["web_development"] = ("Web Development", "Web API scaffolding with Flask and supporting utilities."),
            ["deep_learning"] = ("Deep Learning", "TensorFlow, PyTorch, and tooling for neural network development."),
            ["ai_transformers"] = ("AI Transformers", "Hugging Face, OpenAI, and multimodal transformer dependencies."),
            ["vector_stores"] = ("Vector Stores", "Vector database clients and embedding helpers for RAG scenarios."),
            ["document_ai"] = ("Document AI", "OCR, PDF parsing, and layout understanding libraries."),
            ["auto_agents"] = ("Auto Agents", "Agent orchestration, planning, and tool routing dependencies.")
        };

        private static readonly Dictionary<string, string[]> DefaultPackageSeeds = new(StringComparer.OrdinalIgnoreCase)
        {
            ["data_science_essentials"] = new[] { "numpy", "pandas", "matplotlib", "seaborn", "jupyter", "ipython" },
            ["ml_basics"] = new[] { "scikit-learn", "scipy", "numpy", "pandas", "matplotlib" },
            ["web_development"] = new[] { "flask", "requests", "jinja2", "werkzeug", "gunicorn" },
            ["deep_learning"] = new[] { "tensorflow", "keras", "torch", "torchvision", "numpy" },
            ["ai_transformers"] = new[] { "transformers", "accelerate", "datasets", "safetensors", "sentencepiece", "torch", "bitsandbytes" },
            ["vector_stores"] = new[] { "faiss-cpu", "qdrant-client", "pinecone-client", "chromadb", "weaviate-client", "sentence-transformers" },
            ["document_ai"] = new[] { "pytesseract", "pypdf", "pdfplumber", "python-docx", "textract", "layoutparser" },
            ["auto_agents"] = new[] { "langchain", "langgraph", "openai", "anthropic", "google-generativeai", "cohere", "tenacity" }
        };

        public uc_Packages()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (_initialized || IsInDesignMode())
            {
                return;
            }

            _initialized = true;
            InitializeServices();
            LoadPackageSets();
            LoadEnvironments();
            ResetInstallState();
        }

        private bool IsInDesignMode() =>
            LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode;

        private sealed class PackageSetViewModel
        {
            public PackageSetViewModel(string key, string displayName, string description, List<PackageDefinition> packages)
            {
                Key = key;
                DisplayName = displayName;
                Description = description;
                Packages = packages ?? new List<PackageDefinition>();
            }

            public string Key { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public List<PackageDefinition> Packages { get; }
        }
    }
}
