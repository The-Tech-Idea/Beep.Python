namespace Beep.Python.Model
{
    /// <summary>
    /// Provides predefined collections of packages for common development scenarios.
    /// </summary>
    public static class PredefinedPackageSets
    {
        /// <summary>
        /// Gets a package set for data science work.
        /// </summary>
        public static PackageSet DataScience => new PackageSet
        {
            Name = "Data Science Essentials",
            Description = "Essential packages for data analysis and visualization",
            Category = PackageCategory.DataScience,
            Packages = new List<string>
            {
                "numpy", "pandas", "matplotlib", "scipy", "jupyter",
                "seaborn", "scikit-learn", "statsmodels", "plotly"
            },
            Versions = new Dictionary<string, string>
            {
                { "numpy", ">=1.20.0" },
                { "pandas", ">=1.3.0" },
                { "matplotlib", ">=3.4.0" },
                { "scikit-learn", ">=1.0.0" }
            }
        };

        /// <summary>
        /// Gets a package set for machine learning work.
        /// </summary>
        public static PackageSet MachineLearning => new PackageSet
        {
            Name = "Machine Learning",
            Description = "Packages for machine learning and AI development",
            Category = PackageCategory.MachineLearning,
            Packages = new List<string>
            {
                "tensorflow", "keras", "torch", "scikit-learn", "xgboost",
                "lightgbm", "catboost", "pandas", "numpy", "matplotlib"
            }
        };

        /// <summary>
        /// Gets a package set for web development.
        /// </summary>
        public static PackageSet WebDevelopment => new PackageSet
        {
            Name = "Web Development",
            Description = "Packages for web application development",
            Category = PackageCategory.WebDevelopment,
            Packages = new List<string>
            {
                "flask", "django", "fastapi", "uvicorn", "requests",
                "beautifulsoup4", "sqlalchemy", "jinja2", "gunicorn"
            }
        };

        /// <summary>
        /// Gets a package set for computer vision work.
        /// </summary>
        public static PackageSet ComputerVision => new PackageSet
        {
            Name = "Computer Vision",
            Description = "Packages for image processing and computer vision",
            Category = PackageCategory.Graphics,
            Packages = new List<string>
            {
                "opencv-python", "pillow", "scikit-image", "tensorflow",
                "torch", "torchvision", "numpy", "matplotlib"
            }
        };

        /// <summary>
        /// Gets a package set for natural language processing.
        /// </summary>
        public static PackageSet NLP => new PackageSet
        {
            Name = "Natural Language Processing",
            Description = "Packages for text processing and NLP",
            Category = PackageCategory.MachineLearning,
            Packages = new List<string>
            {
                "nltk", "spacy", "gensim", "transformers", "textblob",
                "huggingface-hub", "tokenizers", "sentence-transformers", "datasets"
            }
        };


        // Existing package sets...

        /// <summary>
        /// Gets a package set for database operations.
        /// </summary>
        public static PackageSet Database => new PackageSet
        {
            Name = "Database Tools",
            Description = "Packages for database access and ORM",
            Category = PackageCategory.Database,
            Packages = new List<string>
            {
                "sqlalchemy", "psycopg2-binary", "pymysql", "sqlite3", "sqlparse",
                "alembic", "peewee", "mongoengine", "pymongo", "redis"
            }
        };

        /// <summary>
        /// Gets a package set for networking and API development.
        /// </summary>
        public static PackageSet Networking => new PackageSet
        {
            Name = "Networking",
            Description = "Packages for networking, APIs, and web requests",
            Category = PackageCategory.Networking,
            Packages = new List<string>
            {
                "requests", "urllib3", "aiohttp", "websockets", "httpx",
                "grpcio", "twisted", "sockets", "pyngrok", "paramiko"
            }
        };

        /// <summary>
        /// Gets a package set for security testing and cryptography.
        /// </summary>
        public static PackageSet Security => new PackageSet
        {
            Name = "Security",
            Description = "Packages for cryptography, security testing, and authentication",
            Category = PackageCategory.Security,
            Packages = new List<string>
            {
                "cryptography", "pyjwt", "passlib", "bcrypt", "pycryptodome",
                "oauthlib", "pysecrets", "pyopenssl", "cerberus", "authlib"
            }
        };

        /// <summary>
        /// Gets a package set for testing and QA tools.
        /// </summary>
        public static PackageSet Testing => new PackageSet
        {
            Name = "Testing",
            Description = "Packages for unit testing, integration testing, and QA",
            Category = PackageCategory.Testing,
            Packages = new List<string>
            {
                "pytest", "unittest2", "nose", "coverage", "hypothesis",
                "pytest-cov", "tox", "mock", "faker", "selenium"
            }
        };

        /// <summary>
        /// Gets a package set for general utility tools.
        /// </summary>
        public static PackageSet Utilities => new PackageSet
        {
            Name = "Utilities",
            Description = "General utility and helper packages",
            Category = PackageCategory.Utilities,
            Packages = new List<string>
            {
                "tqdm", "click", "rich", "colorama", "pyyaml",
                "python-dotenv", "schedule", "pytz", "appdirs", "tenacity"
            }
        };

        /// <summary>
        /// Gets a package set for scientific computing.
        /// </summary>
        public static PackageSet Scientific => new PackageSet
        {
            Name = "Scientific",
            Description = "Packages for scientific computing and research",
            Category = PackageCategory.Scientific,
            Packages = new List<string>
            {
                "scipy", "sympy", "biopython", "astropy", "qiskit",
                "nilearn", "chempy", "pint", "uncertainties", "pyvista"
            }
        };

        /// <summary>
        /// Gets a package set for mathematical operations.
        /// </summary>
        public static PackageSet Math => new PackageSet
        {
            Name = "Math",
            Description = "Packages for mathematical operations and modeling",
            Category = PackageCategory.Math,
            Packages = new List<string>
            {
                "numpy", "sympy", "statsmodels", "pandas", "numba",
                "mpmath", "patsy", "networkx", "pymc3", "theano"
            }
        };

        /// <summary>
        /// Gets a package set for UI development.
        /// </summary>
        public static PackageSet UserInterface => new PackageSet
        {
            Name = "User Interface",
            Description = "Packages for developing GUIs and user interfaces",
            Category = PackageCategory.UserInterface,
            Packages = new List<string>
            {
                "tkinter", "PyQt5", "wxPython", "PySide6", "kivy",
                "streamlit", "gradio", "pywebview", "flexx", "customtkinter"
            }
        };

        /// <summary>
        /// Gets a package set for audio and video processing.
        /// </summary>
        public static PackageSet AudioVideo => new PackageSet
        {
            Name = "Audio & Video",
            Description = "Packages for audio and video processing",
            Category = PackageCategory.AudioVideo,
            Packages = new List<string>
            {
                "ffmpeg-python", "moviepy", "librosa", "pydub", "pyaudio",
                "opencv-python", "imageio", "pygame", "PyWave", "soundfile"
            }
        };

        /// <summary>
        /// Gets a package set for documentation and code generation.
        /// </summary>
        public static PackageSet Documentation => new PackageSet
        {
            Name = "Documentation",
            Description = "Packages for generating and managing documentation",
            Category = PackageCategory.Documentation,
            Packages = new List<string>
            {
                "sphinx", "mkdocs", "pdoc3", "pydoc-markdown", "jupyter-book",
                "docutils", "recommonmark", "nbsphinx", "myst-parser", "sphinx-rtd-theme"
            }
        };

        /// <summary>
        /// Gets a package set for file processing and manipulation.
        /// </summary>
        public static PackageSet FileProcessing => new PackageSet
        {
            Name = "File Processing",
            Description = "Packages for handling files, formats, and serialization",
            Category = PackageCategory.FileProcessing,
            Packages = new List<string>
            {
                "openpyxl", "xlsxwriter", "pdfminer", "pypdf2", "python-docx",
                "pillow", "pyzipper", "chardet", "magic", "pyavro"
            }
        };

        /// <summary>
        /// Gets a package set for development tools.
        /// </summary>
        public static PackageSet DevTools => new PackageSet
        {
            Name = "Development Tools",
            Description = "Packages for development, debugging, and code analysis",
            Category = PackageCategory.DevTools,
            Packages = new List<string>
            {
                "black", "flake8", "isort", "mypy", "pylint",
                "ipython", "jupyter", "sphinx", "ptvsd", "pyperformance"
            }
        };

        /// <summary>
        /// Gets a dictionary of all predefined package sets.
        /// </summary>
        public static Dictionary<string, PackageSet> AllSets => new Dictionary<string, PackageSet>
        {
            { "data-science", DataScience },
            { "machine-learning", MachineLearning },
            { "web-development", WebDevelopment },
            { "computer-vision", ComputerVision },
            { "nlp", NLP },
            { "database", Database },
            { "networking", Networking },
            { "security", Security },
            { "testing", Testing },
            { "utilities", Utilities },
            { "scientific", Scientific },
            { "math", Math },
            { "user-interface", UserInterface },
            { "audio-video", AudioVideo },
            { "documentation", Documentation },
            { "file-processing", FileProcessing },
            { "dev-tools", DevTools }
        };

        /// <summary>
        /// Gets all package sets grouped by category.
        /// </summary>
        public static Dictionary<PackageCategory, List<PackageSet>> PackageSetsByCategory => new Dictionary<PackageCategory, List<PackageSet>>
        {
            { PackageCategory.Uncategorized, new List<PackageSet>() },
            { PackageCategory.Graphics, new List<PackageSet> { ComputerVision } },
            { PackageCategory.MachineLearning, new List<PackageSet> { MachineLearning, NLP } },
            { PackageCategory.DataScience, new List<PackageSet> { DataScience } },
            { PackageCategory.WebDevelopment, new List<PackageSet> { WebDevelopment } },
            { PackageCategory.DevTools, new List<PackageSet> { DevTools } },
            { PackageCategory.Database, new List<PackageSet> { Database } },
            { PackageCategory.Networking, new List<PackageSet> { Networking } },
            { PackageCategory.Security, new List<PackageSet> { Security } },
            { PackageCategory.Testing, new List<PackageSet> { Testing } },
            { PackageCategory.Utilities, new List<PackageSet> { Utilities } },
            { PackageCategory.Scientific, new List<PackageSet> { Scientific } },
            { PackageCategory.Math, new List<PackageSet> { Math } },
            { PackageCategory.UserInterface, new List<PackageSet> { UserInterface } },
            { PackageCategory.AudioVideo, new List<PackageSet> { AudioVideo } },
            { PackageCategory.Documentation, new List<PackageSet> { Documentation } },
            { PackageCategory.FileProcessing, new List<PackageSet> { FileProcessing } }
        };
    }
}


