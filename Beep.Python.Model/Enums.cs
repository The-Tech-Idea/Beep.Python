namespace Beep.Python.Model
{
    /// <summary>
    /// Categorizes Python packages by their primary purpose.
    /// </summary>
    public enum PackageCategory
    {
        Uncategorized,
        Graphics,
        MachineLearning,
        DataScience,
        VectorDB,
        Embedding,
        Ragging,
        WebDevelopment,
        DevTools,
        Database,
        Networking,
        Security,
        Testing,
        Utilities,
        Scientific,
        Math,
        UserInterface,
        AudioVideo,
        Documentation,
        FileProcessing
    }
    public enum ModelMetric
    {
        Accuracy,
        F1,
        // Add other metrics as needed
    }
    public enum ChartType
    {
        LinePlot,       // Line plot for trends
        BarChart,       // Bar chart for comparing categories/values
        Histogram,      // Histogram for data distribution
        PieChart,       // Pie chart for proportional data
        BoxPlot,        // Box plot for displaying statistical data
        Heatmap,        // Heatmap for matrix-like data
        AreaPlot,       // Area plot for filled area under a curve
        ViolinPlot,     // Violin plot for displaying distribution of data
        BoxenPlot,      // Boxen plot for visualizing large datasets
        HexbinPlot,     // Hexbin plot for 2D binning of data points
        ContourPlot,    // Contour plot for 2D data with contour lines
        ScatterPlot,    // Scatter plot for visualizing the relationship between two variables
        PolarPlot,      // Polar plot for data with circular representation
        SpiderPlot,     // Spider plot for multivariate data
        ErrorBarPlot,   // Error bar plot for displaying uncertainties
        QuiverPlot,     // Quiver plot for vector field data
        StreamPlot,     // Stream plot for displaying flow or streamlines
        Bar3DPlot,      // 3D bar chart
        Scatter3DPlot,  // 3D scatter plot
        BubbleChart,    // Bubble chart for 2D data with varying bubble sizes
        SankeyDiagram,  // Sankey diagram for flow or process visualization
        TreeMap,        // TreeMap for hierarchical data visualization
        WordCloud,      // Word cloud for text data visualization
        ParallelCoordinatesPlot, // Parallel coordinates plot for multivariate data
        BarPlot,
        PairPlot
    }
    public enum MachineLearningAlgorithm
        {
            // Classification Algorithms
            RandomForestClassifier,
            LogisticRegression,
            SVM, // Support Vector Machine
            KNN, // K-Nearest Neighbors
            DecisionTreeClassifier,
            GradientBoostingClassifier,
            AdaBoostClassifier,
            GaussianNB,
            MultinomialNB,
            BernoulliNB,

            // Regression Algorithms
            LinearRegression,
            LassoRegression,
            RidgeRegression,
            ElasticNet,
            DecisionTreeRegressor,
            RandomForestRegressor,
            GradientBoostingRegressor,
        HistGradientBoostingRegressor, // Add this line for historical gradient boosting regressor
        SVR, // Support Vector Regression

            // Clustering Algorithms
            KMeans,
            DBSCAN, // Density-Based Spatial Clustering of Applications with Noise
            AgglomerativeClustering,

            //// Neural Networks and Deep Learning (These might require libraries like TensorFlow or PyTorch)
            //CNN, // Convolutional Neural Network
            //RNN, // Recurrent Neural Network
            //LSTM, // Long Short-Term Memory
            //GAN, // Generative Adversarial Network

            //// Other Specialized Algorithms
            //PCA, // Principal Component Analysis
            //LDA // Linear Discriminant Analysis
            //    // ... Add more as required
        }
    /// <summary>
    /// Enum representing categories of machine learning algorithms.
    /// </summary>
    public enum AlgorithmCategory
    {
        Classification,
        Regression,
        Clustering,
        DimensionalityReduction,
        Others
    }

    #region Transformer Enums

    /// <summary>
    /// Represents the source of a transformer model
    /// </summary>
    public enum TransformerModelSource
    {
        /// <summary>
        /// Model from HuggingFace Hub
        /// </summary>
        HuggingFace,
        
        /// <summary>
        /// Local model file or directory
        /// </summary>
        Local,
        
        /// <summary>
        /// Custom model source (URL, cloud storage, etc.)
        /// </summary>
        Custom,
        
        /// <summary>
        /// OpenAI models
        /// </summary>
        OpenAI,
        
        /// <summary>
        /// Google models (Vertex AI, etc.)
        /// </summary>
        Google,
        
        /// <summary>
        /// Microsoft Azure OpenAI models
        /// </summary>
        Azure,
        
        /// <summary>
        /// Anthropic Claude models
        /// </summary>
        Anthropic,
        
        /// <summary>
        /// Cohere models
        /// </summary>
        Cohere,
        
        /// <summary>
        /// Meta models (Llama, etc.)
        /// </summary>
        Meta,
        
        /// <summary>
        /// Mistral AI models
        /// </summary>
        Mistral,
        
        /// <summary>
        /// Other/Unknown source
        /// </summary>
        Other
    }

    /// <summary>
    /// Represents different types of transformer tasks
    /// </summary>
    public enum TransformerTask
    {
        /// <summary>
        /// Text generation task
        /// </summary>
        TextGeneration,
        
        /// <summary>
        /// Text classification task
        /// </summary>
        TextClassification,
        
        /// <summary>
        /// Named Entity Recognition task
        /// </summary>
        NamedEntityRecognition,
        
        /// <summary>
        /// Question Answering task
        /// </summary>
        QuestionAnswering,
        
        /// <summary>
        /// Text summarization task
        /// </summary>
        Summarization,
        
        /// <summary>
        /// Language translation task
        /// </summary>
        Translation,
        
        /// <summary>
        /// Text embedding generation
        /// </summary>
        FeatureExtraction,
        
        /// <summary>
        /// Sentiment analysis task
        /// </summary>
        SentimentAnalysis,
        
        /// <summary>
        /// Text similarity task
        /// </summary>
        SimilarityComparison,
        
        /// <summary>
        /// Conversational AI task
        /// </summary>
        Conversational,
        
        /// <summary>
        /// Text-to-text generation
        /// </summary>
        Text2TextGeneration,
        
        /// <summary>
        /// Fill in the blank/mask task
        /// </summary>
        FillMask,
        
        /// <summary>
        /// Zero-shot classification
        /// </summary>
        ZeroShotClassification,
        
        /// <summary>
        /// Image captioning
        /// </summary>
        ImageCaptioning,
        
        /// <summary>
        /// Visual question answering
        /// </summary>
        VisualQuestionAnswering,
        
        /// <summary>
        /// Image classification
        /// </summary>
        ImageClassification,
        
        /// <summary>
        /// Object detection
        /// </summary>
        ObjectDetection,
        
        /// <summary>
        /// Audio classification
        /// </summary>
        AudioClassification,
        
        /// <summary>
        /// Automatic speech recognition
        /// </summary>
        AutomaticSpeechRecognition,
        
        /// <summary>
        /// Text-to-speech synthesis
        /// </summary>
        TextToSpeech,
        
        /// <summary>
        /// Tabular data tasks
        /// </summary>
        TabularData,
        
        /// <summary>
        /// Time series forecasting
        /// </summary>
        TimeSeriesForecasting,
        
        /// <summary>
        /// Custom/Other task
        /// </summary>
        Custom
    }

    /// <summary>
    /// Execution device types for transformer models
    /// </summary>
    public enum TransformerDevice
    {
        /// <summary>
        /// CPU execution
        /// </summary>
        CPU,
        
        /// <summary>
        /// CUDA GPU execution
        /// </summary>
        CUDA,
        
        /// <summary>
        /// Metal Performance Shaders (Apple Silicon)
        /// </summary>
        MPS,
        
        /// <summary>
        /// Automatic device selection
        /// </summary>
        Auto
    }

    /// <summary>
    /// Model precision types
    /// </summary>
    public enum ModelPrecision
    {
        /// <summary>
        /// Full precision (32-bit)
        /// </summary>
        Full,
        
        /// <summary>
        /// Half precision (16-bit)
        /// </summary>
        Half,
        
        /// <summary>
        /// 8-bit quantization
        /// </summary>
        Int8,
        
        /// <summary>
        /// 4-bit quantization
        /// </summary>
        Int4,
        
        /// <summary>
        /// Automatic precision selection
        /// </summary>
        Auto
    }

    #endregion
}
