namespace Beep.Python.Model
{
    /// <summary>
    /// Provides predefined collections of packages for common development scenarios.
    /// </summary>
    public static class PredefinedPackageSets
    {
        // Base path for package and category images - adjust as needed for your application
        private const string PackageImageBasePath = "/Beep.Python;component/Images/Packages/";
        private const string CategoryImageBasePath = "/Beep.Python;component/Images/Categories/";

        /// <summary>
        /// Gets a package set for data science work.
        /// </summary>
        public static PackageSet DataScience => new PackageSet
        {
            Name = "Data Science Essentials",
            Description = "Essential packages for data analysis and visualization",
            Category = PackageCategory.DataScience,
            Image = $"{CategoryImageBasePath}data-science.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "numpy", PackageTitle = "NumPy", Description = "Fundamental package for scientific computing with Python", Version = ">=1.20.0", Image = $"{PackageImageBasePath}numpy.svg" },
                new PackageDefinition { PackageName = "pandas", PackageTitle = "Pandas", Description = "Data analysis and manipulation library", Version = ">=1.3.0", Image = $"{PackageImageBasePath}pandas.svg" },
                new PackageDefinition { PackageName = "matplotlib", PackageTitle = "Matplotlib", Description = "Comprehensive library for creating static, animated, and interactive visualizations", Version = ">=3.4.0", Image = $"{PackageImageBasePath}matplotlib.svg" },
                new PackageDefinition { PackageName = "scipy", PackageTitle = "SciPy", Description = "Fundamental algorithms for scientific computing in Python", Image = $"{PackageImageBasePath}scipy.svg" },
                new PackageDefinition { PackageName = "jupyter", PackageTitle = "Jupyter", Description = "Interactive computing environment", Image = $"{PackageImageBasePath}jupyter.svg" },
                new PackageDefinition { PackageName = "seaborn", PackageTitle = "Seaborn", Description = "Statistical data visualization based on matplotlib", Image = $"{PackageImageBasePath}seaborn.svg" },
                new PackageDefinition { PackageName = "scikit-learn", PackageTitle = "Scikit-learn", Description = "Machine learning library for Python", Version = ">=1.0.0", Image = $"{PackageImageBasePath}scikit-learn.svg" },
                new PackageDefinition { PackageName = "statsmodels", PackageTitle = "Statsmodels", Description = "Statistical modeling and econometrics in Python", Image = $"{PackageImageBasePath}statsmodels.svg" },
                new PackageDefinition { PackageName = "plotly", PackageTitle = "Plotly", Description = "Interactive graphing library for Python", Image = $"{PackageImageBasePath}plotly.svg" }
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
            Image = $"{CategoryImageBasePath}machine-learning.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "tensorflow", PackageTitle = "TensorFlow", Description = "Open-source machine learning framework", Image = $"{PackageImageBasePath}tensorflow.svg" },
                new PackageDefinition { PackageName = "keras", PackageTitle = "Keras", Description = "Deep learning API running on top of TensorFlow", Image = $"{PackageImageBasePath}keras.svg" },
                new PackageDefinition { PackageName = "torch", PackageTitle = "PyTorch", Description = "Machine learning framework based on the Torch library", Image = $"{PackageImageBasePath}torch.svg" },
                new PackageDefinition { PackageName = "scikit-learn", PackageTitle = "Scikit-learn", Description = "Machine learning library for Python", Image = $"{PackageImageBasePath}scikit-learn.svg" },
                new PackageDefinition { PackageName = "xgboost", PackageTitle = "XGBoost", Description = "Optimized gradient boosting library", Image = $"{PackageImageBasePath}xgboost.svg" },
                new PackageDefinition { PackageName = "lightgbm", PackageTitle = "LightGBM", Description = "Gradient boosting framework using tree-based learning algorithms", Image = $"{PackageImageBasePath}lightgbm.svg" },
                new PackageDefinition { PackageName = "catboost", PackageTitle = "CatBoost", Description = "Gradient boosting library with categorical features support", Image = $"{PackageImageBasePath}catboost.svg" },
                new PackageDefinition { PackageName = "pandas", PackageTitle = "Pandas", Description = "Data analysis and manipulation library", Image = $"{PackageImageBasePath}pandas.svg" },
                new PackageDefinition { PackageName = "numpy", PackageTitle = "NumPy", Description = "Fundamental package for scientific computing with Python", Image = $"{PackageImageBasePath}numpy.svg" },
                new PackageDefinition { PackageName = "matplotlib", PackageTitle = "Matplotlib", Description = "Comprehensive library for creating static, animated, and interactive visualizations", Image = $"{PackageImageBasePath}matplotlib.svg" }
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
            Image = $"{CategoryImageBasePath}web-development.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "flask", PackageTitle = "Flask", Description = "Lightweight WSGI web application framework", Image = $"{PackageImageBasePath}flask.svg" },
                new PackageDefinition { PackageName = "django", PackageTitle = "Django", Description = "High-level Python web framework", Image = $"{PackageImageBasePath}django.svg" },
                new PackageDefinition { PackageName = "fastapi", PackageTitle = "FastAPI", Description = "Modern, fast web framework for building APIs", Image = $"{PackageImageBasePath}fastapi.svg" },
                new PackageDefinition { PackageName = "uvicorn", PackageTitle = "Uvicorn", Description = "ASGI web server implementation", Image = $"{PackageImageBasePath}uvicorn.svg" },
                new PackageDefinition { PackageName = "requests", PackageTitle = "Requests", Description = "HTTP library for Python", Image = $"{PackageImageBasePath}requests.svg" },
                new PackageDefinition { PackageName = "beautifulsoup4", PackageTitle = "Beautiful Soup", Description = "Library for pulling data out of HTML and XML files", Image = $"{PackageImageBasePath}beautifulsoup4.svg" },
                new PackageDefinition { PackageName = "sqlalchemy", PackageTitle = "SQLAlchemy", Description = "SQL toolkit and Object-Relational Mapping system", Image = $"{PackageImageBasePath}sqlalchemy.svg" },
                new PackageDefinition { PackageName = "jinja2", PackageTitle = "Jinja2", Description = "Template engine for Python", Image = $"{PackageImageBasePath}jinja2.svg" },
                new PackageDefinition { PackageName = "gunicorn", PackageTitle = "Gunicorn", Description = "Python WSGI HTTP Server for UNIX", Image = $"{PackageImageBasePath}gunicorn.svg" }
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
            Image = $"{CategoryImageBasePath}computer-vision.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "opencv-python", PackageTitle = "OpenCV", Description = "Open Source Computer Vision Library", Image = $"{PackageImageBasePath}opencv.svg" },
                new PackageDefinition { PackageName = "pillow", PackageTitle = "Pillow", Description = "Python Imaging Library (Fork)", Image = $"{PackageImageBasePath}pillow.svg" },
                new PackageDefinition { PackageName = "scikit-image", PackageTitle = "Scikit-image", Description = "Image processing in Python", Image = $"{PackageImageBasePath}scikit-image.svg" },
                new PackageDefinition { PackageName = "tensorflow", PackageTitle = "TensorFlow", Description = "Open-source machine learning framework", Image = $"{PackageImageBasePath}tensorflow.svg" },
                new PackageDefinition { PackageName = "torch", PackageTitle = "PyTorch", Description = "Machine learning framework based on the Torch library", Image = $"{PackageImageBasePath}torch.svg" },
                new PackageDefinition { PackageName = "torchvision", PackageTitle = "TorchVision", Description = "Computer vision package for PyTorch", Image = $"{PackageImageBasePath}torchvision.svg" },
                new PackageDefinition { PackageName = "numpy", PackageTitle = "NumPy", Description = "Fundamental package for scientific computing with Python", Image = $"{PackageImageBasePath}numpy.svg" },
                new PackageDefinition { PackageName = "matplotlib", PackageTitle = "Matplotlib", Description = "Comprehensive library for creating static, animated, and interactive visualizations", Image = $"{PackageImageBasePath}matplotlib.svg" }
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
            Image = $"{CategoryImageBasePath}nlp.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "nltk", PackageTitle = "NLTK", Description = "Natural Language Toolkit", Image = $"{PackageImageBasePath}nltk.svg" },
                new PackageDefinition { PackageName = "spacy", PackageTitle = "spaCy", Description = "Industrial-strength Natural Language Processing", Image = $"{PackageImageBasePath}spacy.svg" },
                new PackageDefinition { PackageName = "gensim", PackageTitle = "Gensim", Description = "Topic modeling for humans", Image = $"{PackageImageBasePath}gensim.svg" },
                new PackageDefinition { PackageName = "transformers", PackageTitle = "Transformers", Description = "State-of-the-art Natural Language Processing for PyTorch and TensorFlow", Image = $"{PackageImageBasePath}transformers.svg" },
                new PackageDefinition { PackageName = "textblob", PackageTitle = "TextBlob", Description = "Simplified text processing library", Image = $"{PackageImageBasePath}textblob.svg" },
                new PackageDefinition { PackageName = "huggingface-hub", PackageTitle = "Hugging Face Hub", Description = "Client library to download and publish models and datasets", Image = $"{PackageImageBasePath}huggingface.svg" },
                new PackageDefinition { PackageName = "tokenizers", PackageTitle = "Tokenizers", Description = "Fast state-of-the-art tokenizers optimized for research and production", Image = $"{PackageImageBasePath}tokenizers.svg" },
                new PackageDefinition { PackageName = "sentence-transformers", PackageTitle = "Sentence Transformers", Description = "Sentence and text embeddings using BERT & Co.", Image = $"{PackageImageBasePath}sentence-transformers.svg" },
                new PackageDefinition { PackageName = "datasets", PackageTitle = "Datasets", Description = "Collections of NLP datasets", Image = $"{PackageImageBasePath}datasets.svg" }
            }
        };

        /// <summary>
        /// Gets a package set for vector databases and similarity search.
        /// </summary>
        public static PackageSet VectorDB => new PackageSet
        {
            Name = "Vector Databases",
            Description = "Packages for vector databases and similarity search",
            Category = PackageCategory.VectorDB,
            Image = $"{CategoryImageBasePath}vectordb.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "faiss-cpu", PackageTitle = "FAISS CPU", Description = "Facebook AI Similarity Search for efficient similarity search and clustering", Image = $"{PackageImageBasePath}faiss.svg" },
                new PackageDefinition { PackageName = "milvus", PackageTitle = "Milvus", Description = "Vector database management system for similarity search", Image = $"{PackageImageBasePath}milvus.svg" },
                new PackageDefinition { PackageName = "annoy", PackageTitle = "Annoy", Description = "Approximate Nearest Neighbors library", Image = $"{PackageImageBasePath}annoy.svg" },
                new PackageDefinition { PackageName = "chromadb", PackageTitle = "ChromaDB", Description = "Open-source embedding database", Image = $"{PackageImageBasePath}chromadb.svg" },
                new PackageDefinition { PackageName = "qdrant-client", PackageTitle = "Qdrant", Description = "Vector search engine and database", Image = $"{PackageImageBasePath}qdrant.svg" },
                new PackageDefinition { PackageName = "pinecone-client", PackageTitle = "Pinecone", Description = "Vector database for machine learning applications", Image = $"{PackageImageBasePath}pinecone.svg" },
                new PackageDefinition { PackageName = "pymilvus", PackageTitle = "PyMilvus", Description = "Python client for Milvus", Image = $"{PackageImageBasePath}pymilvus.svg" },
                new PackageDefinition { PackageName = "weaviate-client", PackageTitle = "Weaviate", Description = "Vector search database", Image = $"{PackageImageBasePath}weaviate.svg" }
            }
        };

        /// <summary>
        /// Gets a package set for embeddings generation.
        /// </summary>
        public static PackageSet Embedding => new PackageSet
        {
            Name = "Embeddings",
            Description = "Packages for generating and working with embeddings",
            Category = PackageCategory.Embedding,
            Image = $"{CategoryImageBasePath}embeddings.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "sentence-transformers", PackageTitle = "Sentence Transformers", Description = "Sentence and text embeddings using BERT & Co.", Image = $"{PackageImageBasePath}sentence-transformers.svg" },
                new PackageDefinition { PackageName = "openai", PackageTitle = "OpenAI", Description = "Client for the OpenAI API including embedding models", Image = $"{PackageImageBasePath}openai.svg" },
                new PackageDefinition { PackageName = "gensim", PackageTitle = "Gensim", Description = "Topic modeling and word embeddings", Image = $"{PackageImageBasePath}gensim.svg" },
                new PackageDefinition { PackageName = "tensorflow-hub", PackageTitle = "TensorFlow Hub", Description = "Library to publish, discover, and reuse model components", Image = $"{PackageImageBasePath}tensorflow-hub.svg" },
                new PackageDefinition { PackageName = "transformers", PackageTitle = "Transformers", Description = "State-of-the-art Natural Language Processing", Image = $"{PackageImageBasePath}transformers.svg" },
                new PackageDefinition { PackageName = "tensorflow-text", PackageTitle = "TensorFlow Text", Description = "Text processing operations for TensorFlow", Image = $"{PackageImageBasePath}tensorflow-text.svg" },
                new PackageDefinition { PackageName = "spacy", PackageTitle = "spaCy", Description = "Industrial-strength Natural Language Processing", Image = $"{PackageImageBasePath}spacy.svg" },
                new PackageDefinition { PackageName = "fasttext", PackageTitle = "FastText", Description = "Library for efficient text classification and representation learning", Image = $"{PackageImageBasePath}fasttext.svg" }
            }
        };

        /// <summary>
        /// Gets a package set for RAG (Retrieval Augmented Generation).
        /// </summary>
        public static PackageSet Ragging => new PackageSet
        {
            Name = "RAG Systems",
            Description = "Packages for Retrieval Augmented Generation",
            Category = PackageCategory.Ragging,
            Image = $"{CategoryImageBasePath}rag.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "langchain", PackageTitle = "LangChain", Description = "Building applications with LLMs through composability", Image = $"{PackageImageBasePath}langchain.svg" },
                new PackageDefinition { PackageName = "llama-index", PackageTitle = "LlamaIndex", Description = "Data framework for LLM applications", Image = $"{PackageImageBasePath}llama-index.svg" },
                new PackageDefinition { PackageName = "haystack", PackageTitle = "Haystack", Description = "Framework for building search systems", Image = $"{PackageImageBasePath}haystack.svg" },
                new PackageDefinition { PackageName = "chromadb", PackageTitle = "ChromaDB", Description = "Open-source embedding database", Image = $"{PackageImageBasePath}chromadb.svg" },
                new PackageDefinition { PackageName = "unstructured", PackageTitle = "Unstructured", Description = "Pre-processing for documents, images, and audio", Image = $"{PackageImageBasePath}unstructured.svg" },
                new PackageDefinition { PackageName = "semantic-kernel", PackageTitle = "Semantic Kernel", Description = "SDK for integrating AI services", Image = $"{PackageImageBasePath}semantic-kernel.svg" },
                new PackageDefinition { PackageName = "pypdf", PackageTitle = "PyPDF", Description = "PDF processing library", Image = $"{PackageImageBasePath}pypdf.svg" },
                new PackageDefinition { PackageName = "docx2txt", PackageTitle = "Docx2Txt", Description = "Converts docx files to text", Image = $"{PackageImageBasePath}docx2txt.svg" }
            }
        };

        /// <summary>
        /// Gets a package set for database operations.
        /// </summary>
        public static PackageSet Database => new PackageSet
        {
            Name = "Database Tools",
            Description = "Packages for database access and ORM",
            Category = PackageCategory.Database,
            Image = $"{CategoryImageBasePath}database.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "sqlalchemy", PackageTitle = "SQLAlchemy", Description = "SQL toolkit and Object-Relational Mapping system", Image = $"{PackageImageBasePath}sqlalchemy.svg" },
                new PackageDefinition { PackageName = "psycopg2-binary", PackageTitle = "Psycopg2", Description = "PostgreSQL adapter for Python", Image = $"{PackageImageBasePath}psycopg2.svg" },
                new PackageDefinition { PackageName = "pymysql", PackageTitle = "PyMySQL", Description = "Pure Python MySQL client", Image = $"{PackageImageBasePath}pymysql.svg" },
                new PackageDefinition { PackageName = "sqlite3", PackageTitle = "SQLite3", Description = "DB-API 2.0 interface for SQLite databases", Image = $"{PackageImageBasePath}sqlite.svg" },
                new PackageDefinition { PackageName = "sqlparse", PackageTitle = "SQLParse", Description = "Non-validating SQL parser", Image = $"{PackageImageBasePath}sqlparse.svg" },
                new PackageDefinition { PackageName = "alembic", PackageTitle = "Alembic", Description = "Database migration tool for SQLAlchemy", Image = $"{PackageImageBasePath}alembic.svg" },
                new PackageDefinition { PackageName = "peewee", PackageTitle = "Peewee", Description = "Simple and small ORM for Python", Image = $"{PackageImageBasePath}peewee.svg" },
                new PackageDefinition { PackageName = "mongoengine", PackageTitle = "MongoEngine", Description = "MongoDB Object-Document Mapper", Image = $"{PackageImageBasePath}mongoengine.svg" },
                new PackageDefinition { PackageName = "pymongo", PackageTitle = "PyMongo", Description = "Python driver for MongoDB", Image = $"{PackageImageBasePath}pymongo.svg" },
                new PackageDefinition { PackageName = "redis", PackageTitle = "Redis", Description = "Python client for Redis key-value store", Image = $"{PackageImageBasePath}redis.svg" }
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
            Image = $"{CategoryImageBasePath}networking.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "requests", PackageTitle = "Requests", Description = "HTTP library for Python", Image = $"{PackageImageBasePath}requests.svg" },
                new PackageDefinition { PackageName = "urllib3", PackageTitle = "URLLib3", Description = "HTTP client for Python", Image = $"{PackageImageBasePath}urllib3.svg" },
                new PackageDefinition { PackageName = "aiohttp", PackageTitle = "AIOHTTP", Description = "Asynchronous HTTP client/server framework", Image = $"{PackageImageBasePath}aiohttp.svg" },
                new PackageDefinition { PackageName = "websockets", PackageTitle = "WebSockets", Description = "Library for building WebSocket servers and clients", Image = $"{PackageImageBasePath}websockets.svg" },
                new PackageDefinition { PackageName = "httpx", PackageTitle = "HTTPX", Description = "Fully featured HTTP client", Image = $"{PackageImageBasePath}httpx.svg" },
                new PackageDefinition { PackageName = "grpcio", PackageTitle = "gRPC", Description = "HTTP/2-based RPC framework", Image = $"{PackageImageBasePath}grpc.svg" },
                new PackageDefinition { PackageName = "twisted", PackageTitle = "Twisted", Description = "Event-driven networking engine", Image = $"{PackageImageBasePath}twisted.svg" },
                new PackageDefinition { PackageName = "sockets", PackageTitle = "Sockets", Description = "Low-level networking interface", Image = $"{PackageImageBasePath}sockets.svg" },
                new PackageDefinition { PackageName = "pyngrok", PackageTitle = "PyNgrok", Description = "Python wrapper for Ngrok", Image = $"{PackageImageBasePath}pyngrok.svg" },
                new PackageDefinition { PackageName = "paramiko", PackageTitle = "Paramiko", Description = "SSHv2 protocol library", Image = $"{PackageImageBasePath}paramiko.svg" }
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
            Image = $"{CategoryImageBasePath}security.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "cryptography", PackageTitle = "Cryptography", Description = "Cryptographic recipes and primitives", Image = $"{PackageImageBasePath}cryptography.svg" },
                new PackageDefinition { PackageName = "pyjwt", PackageTitle = "PyJWT", Description = "JSON Web Token implementation", Image = $"{PackageImageBasePath}pyjwt.svg" },
                new PackageDefinition { PackageName = "passlib", PackageTitle = "Passlib", Description = "Password hashing library", Image = $"{PackageImageBasePath}passlib.svg" },
                new PackageDefinition { PackageName = "bcrypt", PackageTitle = "Bcrypt", Description = "Modern password hashing", Image = $"{PackageImageBasePath}bcrypt.svg" },
                new PackageDefinition { PackageName = "pycryptodome", PackageTitle = "PyCryptodome", Description = "Cryptographic library for Python", Image = $"{PackageImageBasePath}pycryptodome.svg" },
                new PackageDefinition { PackageName = "oauthlib", PackageTitle = "OAuthLib", Description = "OAuth request-signing logic", Image = $"{PackageImageBasePath}oauthlib.svg" },
                new PackageDefinition { PackageName = "pysecrets", PackageTitle = "PySecrets", Description = "Generate secure random numbers", Image = $"{PackageImageBasePath}pysecrets.svg" },
                new PackageDefinition { PackageName = "pyopenssl", PackageTitle = "PyOpenSSL", Description = "Python wrapper for OpenSSL", Image = $"{PackageImageBasePath}pyopenssl.svg" },
                new PackageDefinition { PackageName = "cerberus", PackageTitle = "Cerberus", Description = "Lightweight data validation library", Image = $"{PackageImageBasePath}cerberus.svg" },
                new PackageDefinition { PackageName = "authlib", PackageTitle = "Authlib", Description = "Authentication library for OAuth and OpenID", Image = $"{PackageImageBasePath}authlib.svg" }
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
            Image = $"{CategoryImageBasePath}testing.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "pytest", PackageTitle = "PyTest", Description = "Testing framework for Python", Image = $"{PackageImageBasePath}pytest.svg" },
                new PackageDefinition { PackageName = "unittest2", PackageTitle = "UnitTest2", Description = "Enhanced version of unittest", Image = $"{PackageImageBasePath}unittest.svg" },
                new PackageDefinition { PackageName = "nose", PackageTitle = "Nose", Description = "Extends unittest to make testing easier", Image = $"{PackageImageBasePath}nose.svg" },
                new PackageDefinition { PackageName = "coverage", PackageTitle = "Coverage", Description = "Code coverage measurement for Python", Image = $"{PackageImageBasePath}coverage.svg" },
                new PackageDefinition { PackageName = "hypothesis", PackageTitle = "Hypothesis", Description = "Property-based testing library", Image = $"{PackageImageBasePath}hypothesis.svg" },
                new PackageDefinition { PackageName = "pytest-cov", PackageTitle = "PyTest-Cov", Description = "Coverage plugin for pytest", Image = $"{PackageImageBasePath}pytest-cov.svg" },
                new PackageDefinition { PackageName = "tox", PackageTitle = "Tox", Description = "Automate testing for Python packages", Image = $"{PackageImageBasePath}tox.svg" },
                new PackageDefinition { PackageName = "mock", PackageTitle = "Mock", Description = "Mocking and testing library", Image = $"{PackageImageBasePath}mock.svg" },
                new PackageDefinition { PackageName = "faker", PackageTitle = "Faker", Description = "Generate fake data for testing", Image = $"{PackageImageBasePath}faker.svg" },
                new PackageDefinition { PackageName = "selenium", PackageTitle = "Selenium", Description = "Web browser automation", Image = $"{PackageImageBasePath}selenium.svg" }
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
            Image = $"{CategoryImageBasePath}utilities.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "tqdm", PackageTitle = "TQDM", Description = "Fast, extensible progress bar", Image = $"{PackageImageBasePath}tqdm.svg" },
                new PackageDefinition { PackageName = "click", PackageTitle = "Click", Description = "Command line interface creation kit", Image = $"{PackageImageBasePath}click.svg" },
                new PackageDefinition { PackageName = "rich", PackageTitle = "Rich", Description = "Rich text and beautiful formatting in the terminal", Image = $"{PackageImageBasePath}rich.svg" },
                new PackageDefinition { PackageName = "colorama", PackageTitle = "Colorama", Description = "Cross-platform colored terminal text", Image = $"{PackageImageBasePath}colorama.svg" },
                new PackageDefinition { PackageName = "pyyaml", PackageTitle = "PyYAML", Description = "YAML parser and emitter for Python", Image = $"{PackageImageBasePath}pyyaml.svg" },
                new PackageDefinition { PackageName = "python-dotenv", PackageTitle = "Python-dotenv", Description = "Read key-value pairs from .env file", Image = $"{PackageImageBasePath}python-dotenv.svg" },
                new PackageDefinition { PackageName = "schedule", PackageTitle = "Schedule", Description = "Python job scheduling for humans", Image = $"{PackageImageBasePath}schedule.svg" },
                new PackageDefinition { PackageName = "pytz", PackageTitle = "PyTZ", Description = "World timezone definitions", Image = $"{PackageImageBasePath}pytz.svg" },
                new PackageDefinition { PackageName = "appdirs", PackageTitle = "AppDirs", Description = "Determine platform-specific dirs", Image = $"{PackageImageBasePath}appdirs.svg" },
                new PackageDefinition { PackageName = "tenacity", PackageTitle = "Tenacity", Description = "Retry library for Python", Image = $"{PackageImageBasePath}tenacity.svg" }
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
            Image = $"{CategoryImageBasePath}scientific.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "scipy", PackageTitle = "SciPy", Description = "Fundamental algorithms for scientific computing in Python", Image = $"{PackageImageBasePath}scipy.svg" },
                new PackageDefinition { PackageName = "sympy", PackageTitle = "SymPy", Description = "Computer algebra system", Image = $"{PackageImageBasePath}sympy.svg" },
                new PackageDefinition { PackageName = "biopython", PackageTitle = "BioPython", Description = "Tools for biological computation", Image = $"{PackageImageBasePath}biopython.svg" },
                new PackageDefinition { PackageName = "astropy", PackageTitle = "AstroPy", Description = "Astronomy and astrophysics tools", Image = $"{PackageImageBasePath}astropy.svg" },
                new PackageDefinition { PackageName = "qiskit", PackageTitle = "Qiskit", Description = "Quantum computing framework", Image = $"{PackageImageBasePath}qiskit.svg" },
                new PackageDefinition { PackageName = "nilearn", PackageTitle = "Nilearn", Description = "Statistical learning for neuroimaging", Image = $"{PackageImageBasePath}nilearn.svg" },
                new PackageDefinition { PackageName = "chempy", PackageTitle = "ChemPy", Description = "Chemical kinetics and thermodynamics", Image = $"{PackageImageBasePath}chempy.svg" },
                new PackageDefinition { PackageName = "pint", PackageTitle = "Pint", Description = "Physical quantities module", Image = $"{PackageImageBasePath}pint.svg" },
                new PackageDefinition { PackageName = "uncertainties", PackageTitle = "Uncertainties", Description = "Calculations with uncertainties on the quantities", Image = $"{PackageImageBasePath}uncertainties.svg" },
                new PackageDefinition { PackageName = "pyvista", PackageTitle = "PyVista", Description = "3D plotting and mesh analysis", Image = $"{PackageImageBasePath}pyvista.svg" }
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
            Image = $"{CategoryImageBasePath}math.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "numpy", PackageTitle = "NumPy", Description = "Fundamental package for scientific computing with Python", Image = $"{PackageImageBasePath}numpy.svg" },
                new PackageDefinition { PackageName = "sympy", PackageTitle = "SymPy", Description = "Computer algebra system", Image = $"{PackageImageBasePath}sympy.svg" },
                new PackageDefinition { PackageName = "statsmodels", PackageTitle = "Statsmodels", Description = "Statistical modeling and econometrics in Python", Image = $"{PackageImageBasePath}statsmodels.svg" },
                new PackageDefinition { PackageName = "pandas", PackageTitle = "Pandas", Description = "Data analysis and manipulation library", Image = $"{PackageImageBasePath}pandas.svg" },
                new PackageDefinition { PackageName = "numba", PackageTitle = "Numba", Description = "JIT compiler that translates Python functions to optimized machine code", Image = $"{PackageImageBasePath}numba.svg" },
                new PackageDefinition { PackageName = "mpmath", PackageTitle = "mpmath", Description = "Python library for arbitrary-precision floating-point arithmetic", Image = $"{PackageImageBasePath}mpmath.svg" },
                new PackageDefinition { PackageName = "patsy", PackageTitle = "Patsy", Description = "Statistical models building helper", Image = $"{PackageImageBasePath}patsy.svg" },
                new PackageDefinition { PackageName = "networkx", PackageTitle = "NetworkX", Description = "Creation, manipulation, and study of complex networks", Image = $"{PackageImageBasePath}networkx.svg" },
                new PackageDefinition { PackageName = "pymc3", PackageTitle = "PyMC3", Description = "Bayesian statistical modeling and probabilistic machine learning", Image = $"{PackageImageBasePath}pymc3.svg" },
                new PackageDefinition { PackageName = "theano", PackageTitle = "Theano", Description = "Numerical computation library", Image = $"{PackageImageBasePath}theano.svg" }
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
            Image = $"{CategoryImageBasePath}user-interface.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "tkinter", PackageTitle = "Tkinter", Description = "Standard Python interface to the Tk GUI toolkit", Image = $"{PackageImageBasePath}tkinter.svg" },
                new PackageDefinition { PackageName = "PyQt5", PackageTitle = "PyQt5", Description = "Python bindings for Qt application framework", Image = $"{PackageImageBasePath}pyqt5.svg" },
                new PackageDefinition { PackageName = "wxPython", PackageTitle = "wxPython", Description = "GUI toolkit for Python", Image = $"{PackageImageBasePath}wxpython.svg" },
                new PackageDefinition { PackageName = "PySide6", PackageTitle = "PySide6", Description = "Python bindings for the Qt framework", Image = $"{PackageImageBasePath}pyside6.svg" },
                new PackageDefinition { PackageName = "kivy", PackageTitle = "Kivy", Description = "Open source UI framework", Image = $"{PackageImageBasePath}kivy.svg" },
                new PackageDefinition { PackageName = "streamlit", PackageTitle = "Streamlit", Description = "The fastest way to build data apps", Image = $"{PackageImageBasePath}streamlit.svg" },
                new PackageDefinition { PackageName = "gradio", PackageTitle = "Gradio", Description = "Create UIs for your machine learning model", Image = $"{PackageImageBasePath}gradio.svg" },
                new PackageDefinition { PackageName = "pywebview", PackageTitle = "PyWebView", Description = "Lightweight cross-platform wrapper around webview component", Image = $"{PackageImageBasePath}pywebview.svg" },
                new PackageDefinition { PackageName = "flexx", PackageTitle = "Flexx", Description = "Pure Python toolkit for creating GUI's using web technology", Image = $"{PackageImageBasePath}flexx.svg" },
                new PackageDefinition { PackageName = "customtkinter", PackageTitle = "CustomTkinter", Description = "Modern looking Tkinter widgets", Image = $"{PackageImageBasePath}customtkinter.svg" }
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
            Image = $"{CategoryImageBasePath}audio-video.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "ffmpeg-python", PackageTitle = "FFmpeg-Python", Description = "Python bindings for FFmpeg", Image = $"{PackageImageBasePath}ffmpeg.svg" },
                new PackageDefinition { PackageName = "moviepy", PackageTitle = "MoviePy", Description = "Video editing with Python", Image = $"{PackageImageBasePath}moviepy.svg" },
                new PackageDefinition { PackageName = "librosa", PackageTitle = "Librosa", Description = "Audio and music processing", Image = $"{PackageImageBasePath}librosa.svg" },
                new PackageDefinition { PackageName = "pydub", PackageTitle = "PyDub", Description = "Manipulate audio with a simple interface", Image = $"{PackageImageBasePath}pydub.svg" },
                new PackageDefinition { PackageName = "pyaudio", PackageTitle = "PyAudio", Description = "Cross-platform audio I/O", Image = $"{PackageImageBasePath}pyaudio.svg" },
                new PackageDefinition { PackageName = "opencv-python", PackageTitle = "OpenCV", Description = "Open Source Computer Vision Library", Image = $"{PackageImageBasePath}opencv.svg" },
                new PackageDefinition { PackageName = "imageio", PackageTitle = "ImageIO", Description = "Library for reading and writing images", Image = $"{PackageImageBasePath}imageio.svg" },
                new PackageDefinition { PackageName = "pygame", PackageTitle = "PyGame", Description = "Game development and multimedia library", Image = $"{PackageImageBasePath}pygame.svg" },
                new PackageDefinition { PackageName = "PyWave", PackageTitle = "PyWave", Description = "Sound manipulation library", Image = $"{PackageImageBasePath}pywave.svg" },
                new PackageDefinition { PackageName = "soundfile", PackageTitle = "SoundFile", Description = "Read and write sound files", Image = $"{PackageImageBasePath}soundfile.svg" }
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
            Image = $"{CategoryImageBasePath}documentation.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "sphinx", PackageTitle = "Sphinx", Description = "Python documentation generator", Image = $"{PackageImageBasePath}sphinx.svg" },
                new PackageDefinition { PackageName = "mkdocs", PackageTitle = "MkDocs", Description = "Project documentation with Markdown", Image = $"{PackageImageBasePath}mkdocs.svg" },
                new PackageDefinition { PackageName = "pdoc3", PackageTitle = "pdoc3", Description = "API documentation for Python projects", Image = $"{PackageImageBasePath}pdoc3.svg" },
                new PackageDefinition { PackageName = "pydoc-markdown", PackageTitle = "PyDoc-Markdown", Description = "Create Markdown API documentation", Image = $"{PackageImageBasePath}pydoc-markdown.svg" },
                new PackageDefinition { PackageName = "jupyter-book", PackageTitle = "Jupyter Book", Description = "Create beautiful, publication-quality books and documents", Image = $"{PackageImageBasePath}jupyter-book.svg" },
                new PackageDefinition { PackageName = "docutils", PackageTitle = "DocUtils", Description = "Text processing system for documentation", Image = $"{PackageImageBasePath}docutils.svg" },
                new PackageDefinition { PackageName = "recommonmark", PackageTitle = "Recommonmark", Description = "Markdown parser for docutils", Image = $"{PackageImageBasePath}recommonmark.svg" },
                new PackageDefinition { PackageName = "nbsphinx", PackageTitle = "nbsphinx", Description = "Jupyter Notebook Tools for Sphinx", Image = $"{PackageImageBasePath}nbsphinx.svg" },
                new PackageDefinition { PackageName = "myst-parser", PackageTitle = "MyST Parser", Description = "Extended CommonMark parser for Sphinx", Image = $"{PackageImageBasePath}myst-parser.svg" },
                new PackageDefinition { PackageName = "sphinx-rtd-theme", PackageTitle = "Sphinx RTD Theme", Description = "Read the Docs theme for Sphinx", Image = $"{PackageImageBasePath}sphinx-rtd-theme.svg" }
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
            Image = $"{CategoryImageBasePath}file-processing.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "openpyxl", PackageTitle = "OpenPyXL", Description = "Read/write Excel xlsx/xlsm files", Image = $"{PackageImageBasePath}openpyxl.svg" },
                new PackageDefinition { PackageName = "xlsxwriter", PackageTitle = "XlsxWriter", Description = "Create Excel XLSX files", Image = $"{PackageImageBasePath}xlsxwriter.svg" },
                new PackageDefinition { PackageName = "pdfminer", PackageTitle = "PDFMiner", Description = "Tool for extracting information from PDF documents", Image = $"{PackageImageBasePath}pdfminer.svg" },
                new PackageDefinition { PackageName = "pypdf2", PackageTitle = "PyPDF2", Description = "PDF toolkit", Image = $"{PackageImageBasePath}pypdf2.svg" },
                new PackageDefinition { PackageName = "python-docx", PackageTitle = "Python-Docx", Description = "Create and modify Word documents", Image = $"{PackageImageBasePath}python-docx.svg" },
                new PackageDefinition { PackageName = "pillow", PackageTitle = "Pillow", Description = "Python Imaging Library (Fork)", Image = $"{PackageImageBasePath}pillow.svg" },
                new PackageDefinition { PackageName = "pyzipper", PackageTitle = "PyZipper", Description = "AES encryption for zipfile", Image = $"{PackageImageBasePath}pyzipper.svg" },
                new PackageDefinition { PackageName = "chardet", PackageTitle = "Chardet", Description = "Character encoding auto-detection", Image = $"{PackageImageBasePath}chardet.svg" },
                new PackageDefinition { PackageName = "magic", PackageTitle = "Magic", Description = "File type identification", Image = $"{PackageImageBasePath}magic.svg" },
                new PackageDefinition { PackageName = "pyavro", PackageTitle = "PyAvro", Description = "Apache Avro for Python", Image = $"{PackageImageBasePath}pyavro.svg" }
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
            Image = $"{CategoryImageBasePath}dev-tools.svg",
            Packages = new List<PackageDefinition>
            {
                new PackageDefinition { PackageName = "black", PackageTitle = "Black", Description = "The uncompromising code formatter", Image = $"{PackageImageBasePath}black.svg" },
                new PackageDefinition { PackageName = "flake8", PackageTitle = "Flake8", Description = "Code style enforcement tool", Image = $"{PackageImageBasePath}flake8.svg" },
                new PackageDefinition { PackageName = "isort", PackageTitle = "isort", Description = "Sort imports alphabetically", Image = $"{PackageImageBasePath}isort.svg" },
                new PackageDefinition { PackageName = "mypy", PackageTitle = "MyPy", Description = "Static type checker", Image = $"{PackageImageBasePath}mypy.svg" },
                new PackageDefinition { PackageName = "pylint", PackageTitle = "Pylint", Description = "Python code static checker", Image = $"{PackageImageBasePath}pylint.svg" },
                new PackageDefinition { PackageName = "ipython", PackageTitle = "IPython", Description = "Enhanced interactive Python shell", Image = $"{PackageImageBasePath}ipython.svg" },
                new PackageDefinition { PackageName = "jupyter", PackageTitle = "Jupyter", Description = "Interactive computing environment", Image = $"{PackageImageBasePath}jupyter.svg" },
                new PackageDefinition { PackageName = "sphinx", PackageTitle = "Sphinx", Description = "Python documentation generator", Image = $"{PackageImageBasePath}sphinx.svg" },
                new PackageDefinition { PackageName = "ptvsd", PackageTitle = "PTVSD", Description = "Visual Studio debugger for Python", Image = $"{PackageImageBasePath}ptvsd.svg" },
                new PackageDefinition { PackageName = "pyperformance", PackageTitle = "PyPerformance", Description = "Python performance benchmark suite", Image = $"{PackageImageBasePath}pyperformance.svg" }
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
            { "vectordb", VectorDB },
            { "embedding", Embedding },
            { "ragging", Ragging },
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
            { PackageCategory.VectorDB, new List<PackageSet> { VectorDB } },
            { PackageCategory.Embedding, new List<PackageSet> { Embedding } },
            { PackageCategory.Ragging, new List<PackageSet> { Ragging } },
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