
namespace Beep.Python.Model
{
    public enum ModelMetric
    {
        Accuracy,
        F1,
        // Add other metrics as needed
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
