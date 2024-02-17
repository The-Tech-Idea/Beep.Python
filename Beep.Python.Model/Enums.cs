
namespace Beep.Python.Model
{
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

    
}
