using Beep.Python.Model;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Beep.Python.RuntimeEngine
{
    public static class MLAlgorithmsHelpers
    {
    
            public static string GenerateAlgorithmDescription(MachineLearningAlgorithm algorithm)
            {
                switch (algorithm)
                {
                    case MachineLearningAlgorithm.RandomForestClassifier:
                        return "Random Forest Classifier - An ensemble learning method for classification. It's like asking a group of friends to vote on whether a picture is of a cat or a dog.";
                    case MachineLearningAlgorithm.LogisticRegression:
                        return "Logistic Regression - A linear classification algorithm used for binary and multi-class classification. It's like predicting if it will rain based on past weather data.";
                    case MachineLearningAlgorithm.SVM:
                        return "Support Vector Machine (SVM) - A powerful classification algorithm that finds the best line to separate two groups, like drawing a line between red and blue dots.";
                    case MachineLearningAlgorithm.KNN:
                        return "K-Nearest Neighbors (KNN) - A simple classification algorithm that asks neighbors for advice, similar to asking neighbors if a movie is good or bad.";
                    case MachineLearningAlgorithm.DecisionTreeClassifier:
                        return "Decision Tree Classifier - A tree-based classification algorithm that makes choices like a flowchart, like deciding what to eat based on questions.";
                    case MachineLearningAlgorithm.GradientBoostingClassifier:
                        return "Gradient Boosting Classifier - An ensemble learning method that combines weak opinions for a strong decision, like choosing the best restaurant by asking multiple friends.";
                    case MachineLearningAlgorithm.AdaBoostClassifier:
                        return "AdaBoost Classifier - A boosting algorithm that focuses on what you got wrong last time, similar to studying more for topics you made mistakes in.";
                    case MachineLearningAlgorithm.GaussianNB:
                        return "Gaussian Naive Bayes - A probabilistic classification algorithm that guesses based on patterns you've seen before, like predicting rain based on clouds.";
                    case MachineLearningAlgorithm.MultinomialNB:
                        return "Multinomial Naive Bayes - A variant of Naive Bayes that counts how often things happen, like counting word occurrences in books to predict topics.";
                    case MachineLearningAlgorithm.BernoulliNB:
                        return "Bernoulli Naive Bayes - Another variant of Naive Bayes that checks if something is present or not, like checking if a friend is present at a party.";
                    case MachineLearningAlgorithm.LinearRegression:
                        return "Linear Regression - An algorithm that draws a straight line through data points, like drawing a line to predict a child's height based on their age.";
                    case MachineLearningAlgorithm.LassoRegression:
                        return "Lasso Regression - A linear regression with L1 regularization to simplify by removing less important things, similar to packing only essential items for a trip.";
                    case MachineLearningAlgorithm.RidgeRegression:
                        return "Ridge Regression - A linear regression with L2 regularization that balances between simplicity and accuracy, like adding just the right amount of spice for flavor.";
                    case MachineLearningAlgorithm.ElasticNet:
                        return "Elastic Net - A linear regression with a combination of L1 and L2 regularization, like choosing products that balance quality and price.";
                    case MachineLearningAlgorithm.DecisionTreeRegressor:
                        return "Decision Tree Regressor - A tree-based regression algorithm for making predictions, like estimating a car's price based on its characteristics.";
                    case MachineLearningAlgorithm.RandomForestRegressor:
                        return "Random Forest Regressor - An ensemble regression algorithm that combines friends' price estimates for better accuracy.";
                    case MachineLearningAlgorithm.GradientBoostingRegressor:
                        return "Gradient Boosting Regressor - An ensemble regression algorithm that combines friends' opinions to improve price estimation.";
                    case MachineLearningAlgorithm.SVR:
                        return "Support Vector Regression (SVR) - A regression algorithm that draws a line to predict numbers, similar to drawing a line through points on a graph.";
                    case MachineLearningAlgorithm.KMeans:
                        return "K-Means Clustering - A clustering algorithm that groups similar data points, like grouping similar flowers based on petal and sepal size.";
                    case MachineLearningAlgorithm.DBSCAN:
                        return "DBSCAN - A clustering algorithm that finds clusters of varying shapes and sizes in data, like identifying groups of stars in the night sky.";
                    case MachineLearningAlgorithm.AgglomerativeClustering:
                        return "Agglomerative Clustering - A hierarchical clustering algorithm that builds a cluster hierarchy, like organizing data into a family tree structure.";
                    default:
                        return "Unknown Algorithm";
                }
            }
            public static List<string> GetAlgorithms()
            {
                List<string> algorithmName = Enum.GetNames(typeof(MachineLearningAlgorithm)).ToList();
                return algorithmName;
            }
            public static MachineLearningAlgorithm GetAlgorithm(string algorithm)
            {
                return Enum.Parse<MachineLearningAlgorithm>(algorithm);
            }

            public static List<ParameterDictionaryForAlgorithm> GetParameterDictionaryForAlgorithms()
            {

                var parameters = new List<ParameterDictionaryForAlgorithm>();
                parameters.AddRange(GenerateParametersForAdaBoostClassifier());
                parameters.AddRange(GenerateParametersForDecisionTreeClassifier());
                parameters.AddRange(GenerateParametersForGradientBoostingClassifier());
                parameters.AddRange(GenerateParametersForKNN());
                parameters.AddRange(GenerateParametersForLogisticRegression());
                parameters.AddRange(GenerateParametersForRandomForestClassifier());
                parameters.AddRange(GenerateParametersForSVM());
                parameters.AddRange(GenerateParametersForNaiveBayes());
                parameters.AddRange(GenerateParametersForLinearRegression());
                parameters.AddRange(GenerateParametersForLassoRegression());
                parameters.AddRange(GenerateParametersForRidgeRegression());
                parameters.AddRange(GenerateParametersForElasticNet());
                parameters.AddRange(GenerateParametersForDecisionTreeRegressor());
                parameters.AddRange(GenerateParametersForRandomForestRegressor());
                parameters.AddRange(GenerateParametersForGradientBoostingRegressor());
                parameters.AddRange(GenerateParametersForSVR());
                parameters.AddRange(GenerateParametersForKMeans());
                parameters.AddRange(GenerateParametersForDBSCAN());
                parameters.AddRange(GenerateParametersForAgglomerativeClustering());
                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForRandomForestClassifier()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_estimators",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "The number of trees in the forest.",
            Example = "n_estimators=100"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "criterion",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "The function to measure the quality of a split.",
            Example = "criterion='gini'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_depth",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "The maximum depth of the tree.",
            Example = "max_depth=None" // None means nodes are expanded until all leaves are pure or until all leaves contain less than min_samples_split samples
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_split",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "The minimum number of samples required to split an internal node.",
            Example = "min_samples_split=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_leaf",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "The minimum number of samples required to be at a leaf node.",
            Example = "min_samples_leaf=1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_features",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "The number of features to consider when looking for the best split.",
            Example = "max_features='auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_leaf_nodes",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "Grow trees with max_leaf_nodes in best-first fashion.",
            Example = "max_leaf_nodes=None" // None means unlimited number of leaf nodes
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_impurity_decrease",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "A node will be split if this split induces a decrease of the impurity greater than or equal to this value.",
            Example = "min_impurity_decrease=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "bootstrap",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "Whether bootstrap samples are used when building trees.",
            Example = "bootstrap=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "oob_score",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "Whether to use out-of-bag samples to estimate the generalization accuracy.",
            Example = "oob_score=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "Controls the randomness of the bootstrapping of the samples used when building trees.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "warm_start",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "When set to True, reuse the solution of the previous call to fit and add more estimators to the ensemble, otherwise, just fit a whole new forest.",
            Example = "warm_start=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "class_weight",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "Weights associated with classes in the form {class_label: weight}.",
            Example = "class_weight=None" // None means all classes have weight one
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "ccp_alpha",
            Algorithm = MachineLearningAlgorithm.RandomForestClassifier,
            Description = "Complexity parameter used for Minimal Cost-Complexity Pruning.",
            Example = "ccp_alpha=0.0"
        },
        // Add more parameters as needed
    };

                return parameters;

            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForLogisticRegression()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "penalty",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Specifies the norm used in the penalization ('l1', 'l2', 'elasticnet', 'none').",
            Example = "penalty='l2'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "dual",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Dual or primal formulation. Dual formulation is only implemented for l2 penalty.",
            Example = "dual=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Tolerance for stopping criteria.",
            Example = "tol=0.0001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "C",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Inverse of regularization strength; must be a positive float.",
            Example = "C=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "fit_intercept",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Specifies if a constant (a.k.a. bias or intercept) should be added to the decision function.",
            Example = "fit_intercept=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "intercept_scaling",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Useful only when the solver 'liblinear' is used and fit_intercept is set to True.",
            Example = "intercept_scaling=1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "class_weight",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Weights associated with classes in the form {class_label: weight}.",
            Example = "class_weight=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Used when the solver 'sag', 'saga', or 'liblinear' is used to shuffle the data.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "solver",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Algorithm to use in the optimization problem ('newton-cg', 'lbfgs', 'liblinear', 'sag', 'saga').",
            Example = "solver='lbfgs'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_iter",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Maximum number of iterations taken for the solvers to converge.",
            Example = "max_iter=100"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "multi_class",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "If the option chosen is 'ovr', then a binary problem is fit for each label. For 'multinomial' the loss minimizes a multinomial loss across the entire probability distribution.",
            Example = "multi_class='auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "verbose",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "For the liblinear and lbfgs solvers, set verbose to any positive number for verbosity.",
            Example = "verbose=0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "warm_start",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "When set to True, reuse the solution of the previous call to fit as initialization.",
            Example = "warm_start=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_jobs",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "Number of CPU cores used when parallelizing over classes if multi_class='ovr'.",
            Example = "n_jobs=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "l1_ratio",
            Algorithm = MachineLearningAlgorithm.LogisticRegression,
            Description = "The Elastic-Net mixing parameter, with 0 <= l1_ratio <= 1.",
            Example = "l1_ratio=None"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForSVM()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "C",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Regularization parameter. The strength of the regularization is inversely proportional to C.",
            Example = "C=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "kernel",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Specifies the kernel type to be used in the algorithm ('linear', 'poly', 'rbf', 'sigmoid', 'precomputed').",
            Example = "kernel='rbf'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "degree",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Degree of the polynomial kernel function ('poly'). Ignored by all other kernels.",
            Example = "degree=3"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "gamma",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Kernel coefficient for 'rbf', 'poly', and 'sigmoid'.",
            Example = "gamma='scale'  # or 'auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "coef0",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Independent term in kernel function. It is only significant in 'poly' and 'sigmoid'.",
            Example = "coef0=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "shrinking",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Whether to use the shrinking heuristic.",
            Example = "shrinking=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "probability",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Whether to enable probability estimates.",
            Example = "probability=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Tolerance for stopping criterion.",
            Example = "tol=0.001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "cache_size",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Size of the kernel cache (in MB).",
            Example = "cache_size=200"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "class_weight",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Set the parameter C of class i to class_weight[i]*C for SVC. If not given, all classes are supposed to have weight one.",
            Example = "class_weight=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "verbose",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Enable verbose output. Note that this setting takes advantage of a per-process runtime setting in libsvm, which, if enabled, may not work properly in a multithreaded context.",
            Example = "verbose=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_iter",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Hard limit on iterations within solver, or -1 for no limit.",
            Example = "max_iter=-1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "decision_function_shape",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "Whether to return a one-vs-rest ('ovr') decision function of shape (n_samples, n_classes) or the original one-vs-one ('ovo') decision function of libsvm.",
            Example = "decision_function_shape='ovr'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "break_ties",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "If true, decision_function_shape='ovr', and number of classes > 2, predict will break ties according to the confidence values of decision_function.",
            Example = "break_ties=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.SVM,
            Description = "The seed of the pseudo-random number generator used when shuffling the data for probability estimates.",
            Example = "random_state=None"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForKNN()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_neighbors",
            Algorithm = MachineLearningAlgorithm.KNN,
            Description = "Number of neighbors to use by default for kneighbors queries.",
            Example = "n_neighbors=5"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "weights",
            Algorithm = MachineLearningAlgorithm.KNN,
            Description = "Weight function used in prediction ('uniform', 'distance').",
            Example = "weights='uniform'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "algorithm",
            Algorithm = MachineLearningAlgorithm.KNN,
            Description = "Algorithm used to compute the nearest neighbors ('auto', 'ball_tree', 'kd_tree', 'brute').",
            Example = "algorithm='auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "leaf_size",
            Algorithm = MachineLearningAlgorithm.KNN,
            Description = "Leaf size passed to BallTree or KDTree.",
            Example = "leaf_size=30"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "p",
            Algorithm = MachineLearningAlgorithm.KNN,
            Description = "Power parameter for the Minkowski metric.",
            Example = "p=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "metric",
            Algorithm = MachineLearningAlgorithm.KNN,
            Description = "The distance metric to use for the tree ('minkowski', 'euclidean', 'manhattan', etc.).",
            Example = "metric='minkowski'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "metric_params",
            Algorithm = MachineLearningAlgorithm.KNN,
            Description = "Additional keyword arguments for the metric function.",
            Example = "metric_params=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_jobs",
            Algorithm = MachineLearningAlgorithm.KNN,
            Description = "The number of parallel jobs to run for neighbors search.",
            Example = "n_jobs=None"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForDecisionTreeClassifier()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "criterion",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "The function to measure the quality of a split ('gini', 'entropy').",
            Example = "criterion='gini'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "splitter",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "The strategy used to choose the split at each node ('best', 'random').",
            Example = "splitter='best'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_depth",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "The maximum depth of the tree.",
            Example = "max_depth=None"  // None means unlimited depth
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_split",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "The minimum number of samples required to split an internal node.",
            Example = "min_samples_split=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_leaf",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "The minimum number of samples required to be at a leaf node.",
            Example = "min_samples_leaf=1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_weight_fraction_leaf",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "The minimum weighted fraction of the sum total of weights required to be at a leaf node.",
            Example = "min_weight_fraction_leaf=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_features",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "The number of features to consider when looking for the best split.",
            Example = "max_features=None"  // None means use all features
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "Controls the randomness of the estimator.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_leaf_nodes",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "Grow a tree with max_leaf_nodes in best-first fashion.",
            Example = "max_leaf_nodes=None"  // None means unlimited number of leaf nodes
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_impurity_decrease",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "A node will be split if this split induces a decrease of the impurity greater than or equal to this value.",
            Example = "min_impurity_decrease=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "class_weight",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "Weights associated with classes.",
            Example = "class_weight=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "ccp_alpha",
            Algorithm = MachineLearningAlgorithm.DecisionTreeClassifier,
            Description = "Complexity parameter used for Minimal Cost-Complexity Pruning.",
            Example = "ccp_alpha=0.0"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForGradientBoostingClassifier()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "loss",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The loss function to be optimized ('deviance', 'exponential').",
            Example = "loss='deviance'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "learning_rate",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "Learning rate shrinks the contribution of each tree.",
            Example = "learning_rate=0.1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_estimators",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The number of boosting stages to be run.",
            Example = "n_estimators=100"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "subsample",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The fraction of samples to be used for fitting the individual base learners.",
            Example = "subsample=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "criterion",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The function to measure the quality of a split ('friedman_mse', 'mse', 'mae').",
            Example = "criterion='friedman_mse'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_split",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The minimum number of samples required to split an internal node.",
            Example = "min_samples_split=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_leaf",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The minimum number of samples required to be at a leaf node.",
            Example = "min_samples_leaf=1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_weight_fraction_leaf",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The minimum weighted fraction of the sum total of weights required to be at a leaf node.",
            Example = "min_weight_fraction_leaf=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_depth",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "Maximum depth of the individual regression estimators.",
            Example = "max_depth=3"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_impurity_decrease",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "A node will be split if this split induces a decrease of the impurity greater than or equal to this value.",
            Example = "min_impurity_decrease=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_features",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The number of features to consider when looking for the best split.",
            Example = "max_features=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "Controls the randomness of the estimator.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_leaf_nodes",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "Grow trees with max_leaf_nodes in best-first fashion.",
            Example = "max_leaf_nodes=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "warm_start",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "When set to True, reuse the solution of the previous call to fit and add more estimators to the ensemble.",
            Example = "warm_start=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "validation_fraction",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "The proportion of training data to set aside as validation set for early stopping.",
            Example = "validation_fraction=0.1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_iter_no_change",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "Used to decide if early stopping will be used to terminate training when validation score is not improving.",
            Example = "n_iter_no_change=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "Tolerance for the early stopping.",
            Example = "tol=0.0001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "ccp_alpha",
            Algorithm = MachineLearningAlgorithm.GradientBoostingClassifier,
            Description = "Complexity parameter used for Minimal Cost-Complexity Pruning.",
            Example = "ccp_alpha=0.0"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForAdaBoostClassifier()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "base_estimator",
            Algorithm = MachineLearningAlgorithm.AdaBoostClassifier,
            Description = "The base estimator from which the boosted ensemble is built.",
            Example = "base_estimator=DecisionTreeClassifier(max_depth=1)"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_estimators",
            Algorithm = MachineLearningAlgorithm.AdaBoostClassifier,
            Description = "The maximum number of estimators at which boosting is terminated.",
            Example = "n_estimators=50"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "learning_rate",
            Algorithm = MachineLearningAlgorithm.AdaBoostClassifier,
            Description = "Weight applied to each classifier at each boosting iteration.",
            Example = "learning_rate=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "algorithm",
            Algorithm = MachineLearningAlgorithm.AdaBoostClassifier,
            Description = "The algorithm used to combine the weak learners ('SAMME', 'SAMME.R').",
            Example = "algorithm='SAMME.R'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.AdaBoostClassifier,
            Description = "Controls the randomness of the estimator.",
            Example = "random_state=None"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForNaiveBayes()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
            {
                new ParameterDictionaryForAlgorithm
                {
                    ParameterName = "alpha",
                    Algorithm = MachineLearningAlgorithm.MultinomialNB,
                    Description = "Additive (Laplace/Lidstone) smoothing parameter (0 for no smoothing).",
                    Example = "alpha=1.0"
                },
                new ParameterDictionaryForAlgorithm
                {
                    ParameterName = "fit_prior",
                    Algorithm = MachineLearningAlgorithm.MultinomialNB,
                    Description = "Whether to learn class prior probabilities or not.",
                    Example = "fit_prior=True"
                },
                new ParameterDictionaryForAlgorithm
                {
                    ParameterName = "class_prior",
                    Algorithm = MachineLearningAlgorithm.MultinomialNB,
                    Description = "Prior probabilities of the classes.",
                    Example = "class_prior=None"
                },
                new ParameterDictionaryForAlgorithm
                {
                    ParameterName = "alpha",
                    Algorithm = MachineLearningAlgorithm.BernoulliNB,
                    Description = "Additive (Laplace/Lidstone) smoothing parameter (0 for no smoothing).",
                    Example = "alpha=1.0"
                },
                new ParameterDictionaryForAlgorithm
                {
                    ParameterName = "binarize",
                    Algorithm = MachineLearningAlgorithm.BernoulliNB,
                    Description = "Threshold for binarizing (mapping to booleans) of sample features.",
                    Example = "binarize=0.0"
                },
                new ParameterDictionaryForAlgorithm
                {
                    ParameterName = "fit_prior",
                    Algorithm = MachineLearningAlgorithm.BernoulliNB,
                    Description = "Whether to learn class prior probabilities or not.",
                    Example = "fit_prior=True"
                },
                new ParameterDictionaryForAlgorithm
                {
                    ParameterName = "class_prior",
                    Algorithm = MachineLearningAlgorithm.BernoulliNB,
                    Description = "Prior probabilities of the classes.",
                    Example = "class_prior=None"
                }
            };
                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForLinearRegression()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "fit_intercept",
            Algorithm = MachineLearningAlgorithm.LinearRegression,
            Description = "Whether to calculate the intercept for this model.",
            Example = "fit_intercept=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "normalize",
            Algorithm = MachineLearningAlgorithm.LinearRegression,
            Description = "This parameter is ignored when `fit_intercept` is set to False. If True, the regressors X will be normalized before regression by subtracting the mean and dividing by the l2-norm.",
            Example = "normalize=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "copy_X",
            Algorithm = MachineLearningAlgorithm.LinearRegression,
            Description = "If True, X will be copied; else, it may be overwritten.",
            Example = "copy_X=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_jobs",
            Algorithm = MachineLearningAlgorithm.LinearRegression,
            Description = "The number of jobs to use for the computation. This will only provide speedup for n_targets > 1 and sufficient large problems.",
            Example = "n_jobs=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "positive",
            Algorithm = MachineLearningAlgorithm.LinearRegression,
            Description = "When set to True, forces the coefficients to be positive.",
            Example = "positive=False"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForLassoRegression()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "alpha",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "Constant that multiplies the L1 term. Defaults to 1.0.",
            Example = "alpha=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "fit_intercept",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "Whether to calculate the intercept for this model.",
            Example = "fit_intercept=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "normalize",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "This parameter is ignored when `fit_intercept` is set to False. If True, the regressors X will be normalized before regression.",
            Example = "normalize=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "precompute",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "Whether to use a precomputed Gram matrix to speed up calculations.",
            Example = "precompute=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "copy_X",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "If True, X will be copied; else, it may be overwritten.",
            Example = "copy_X=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_iter",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "The maximum number of iterations.",
            Example = "max_iter=1000"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "The tolerance for the optimization.",
            Example = "tol=0.0001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "warm_start",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "When set to True, reuse the solution of the previous call to fit as initialization.",
            Example = "warm_start=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "positive",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "When set to True, forces the coefficients to be positive.",
            Example = "positive=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "The seed of the pseudo random number generator that selects a random feature to update.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "selection",
            Algorithm = MachineLearningAlgorithm.LassoRegression,
            Description = "If set to 'random', a random coefficient is updated every iteration rather than looping over features sequentially by default.",
            Example = "selection='cyclic'"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForRidgeRegression()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "alpha",
            Algorithm = MachineLearningAlgorithm.RidgeRegression,
            Description = "Regularization strength; must be a positive float. Larger values specify stronger regularization.",
            Example = "alpha=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "fit_intercept",
            Algorithm = MachineLearningAlgorithm.RidgeRegression,
            Description = "Whether to calculate the intercept for this model.",
            Example = "fit_intercept=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "normalize",
            Algorithm = MachineLearningAlgorithm.RidgeRegression,
            Description = "This parameter is ignored when `fit_intercept` is set to False. If True, the regressors X will be normalized before regression.",
            Example = "normalize=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "copy_X",
            Algorithm = MachineLearningAlgorithm.RidgeRegression,
            Description = "If True, X will be copied; else, it may be overwritten.",
            Example = "copy_X=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_iter",
            Algorithm = MachineLearningAlgorithm.RidgeRegression,
            Description = "The maximum number of iterations for conjugate gradient solver.",
            Example = "max_iter=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.RidgeRegression,
            Description = "Precision of the solution.",
            Example = "tol=0.001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "solver",
            Algorithm = MachineLearningAlgorithm.RidgeRegression,
            Description = "Solver to use in the computational routines ('auto', 'svd', 'cholesky', 'lsqr', 'sparse_cg', 'sag', 'saga').",
            Example = "solver='auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.RidgeRegression,
            Description = "The seed of the pseudo-random number generator to use when shuffling the data.",
            Example = "random_state=None"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForElasticNet()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "alpha",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "Constant that multiplies the penalty terms. Defaults to 1.0.",
            Example = "alpha=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "l1_ratio",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "The ElasticNet mixing parameter, with 0 <= l1_ratio <= 1. l1_ratio=0 corresponds to L2 penalty, l1_ratio=1 to L1.",
            Example = "l1_ratio=0.5"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "fit_intercept",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "Whether the intercept should be estimated or not.",
            Example = "fit_intercept=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "normalize",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "This parameter is ignored when `fit_intercept` is set to False. If True, the regressors X will be normalized before regression.",
            Example = "normalize=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "precompute",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "Whether to use a precomputed Gram matrix to speed up calculations.",
            Example = "precompute=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_iter",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "The maximum number of iterations.",
            Example = "max_iter=1000"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "copy_X",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "If True, X will be copied; else, it may be overwritten.",
            Example = "copy_X=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "The tolerance for the optimization.",
            Example = "tol=0.0001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "warm_start",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "When set to True, reuse the solution of the previous call to fit as initialization.",
            Example = "warm_start=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "positive",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "When set to True, forces the coefficients to be positive.",
            Example = "positive=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "The seed of the pseudo-random number generator that selects a random feature to update.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "selection",
            Algorithm = MachineLearningAlgorithm.ElasticNet,
            Description = "If set to 'random', a random coefficient is updated every iteration rather than looping over features sequentially by default.",
            Example = "selection='cyclic'"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForDecisionTreeRegressor()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "criterion",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "The function to measure the quality of a split ('squared_error', 'friedman_mse', 'absolute_error', 'poisson').",
            Example = "criterion='squared_error'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "splitter",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "The strategy used to choose the split at each node ('best', 'random').",
            Example = "splitter='best'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_depth",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "The maximum depth of the tree.",
            Example = "max_depth=None"  // None means unlimited depth
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_split",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "The minimum number of samples required to split an internal node.",
            Example = "min_samples_split=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_leaf",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "The minimum number of samples required to be at a leaf node.",
            Example = "min_samples_leaf=1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_weight_fraction_leaf",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "The minimum weighted fraction of the sum total of weights required to be at a leaf node.",
            Example = "min_weight_fraction_leaf=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_features",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "The number of features to consider when looking for the best split.",
            Example = "max_features=None"  // None means use all features
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "Controls the randomness of the estimator.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_leaf_nodes",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "Grow a tree with max_leaf_nodes in best-first fashion.",
            Example = "max_leaf_nodes=None"  // None means unlimited number of leaf nodes
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_impurity_decrease",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "A node will be split if this split induces a decrease of the impurity greater than or equal to this value.",
            Example = "min_impurity_decrease=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "ccp_alpha",
            Algorithm = MachineLearningAlgorithm.DecisionTreeRegressor,
            Description = "Complexity parameter used for Minimal Cost-Complexity Pruning.",
            Example = "ccp_alpha=0.0"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForRandomForestRegressor()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_estimators",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "The number of trees in the forest.",
            Example = "n_estimators=100"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "criterion",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "The function to measure the quality of a split ('squared_error', 'absolute_error', 'poisson').",
            Example = "criterion='squared_error'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_depth",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "The maximum depth of the tree.",
            Example = "max_depth=None"  // None means unlimited depth
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_split",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "The minimum number of samples required to split an internal node.",
            Example = "min_samples_split=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_leaf",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "The minimum number of samples required to be at a leaf node.",
            Example = "min_samples_leaf=1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_weight_fraction_leaf",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "The minimum weighted fraction of the sum total of weights required to be at a leaf node.",
            Example = "min_weight_fraction_leaf=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_features",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "The number of features to consider when looking for the best split.",
            Example = "max_features='auto'"  // 'auto' means max_features=n_features
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_leaf_nodes",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "Grow trees with max_leaf_nodes in best-first fashion.",
            Example = "max_leaf_nodes=None"  // None means unlimited number of leaf nodes
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_impurity_decrease",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "A node will be split if this split induces a decrease of the impurity greater than or equal to this value.",
            Example = "min_impurity_decrease=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "bootstrap",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "Whether bootstrap samples are used when building trees.",
            Example = "bootstrap=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "oob_score",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "Whether to use out-of-bag samples to estimate the generalization score.",
            Example = "oob_score=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_jobs",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "The number of jobs to run in parallel for both `fit` and `predict`. `-1` means using all processors.",
            Example = "n_jobs=-1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "Controls the randomness of the bootstrapping of the samples used when building trees.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "verbose",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "Controls the verbosity when fitting and predicting.",
            Example = "verbose=0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "warm_start",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "When set to True, reuse the solution of the previous call to fit and add more estimators to the ensemble, otherwise just fit a whole new forest.",
            Example = "warm_start=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "ccp_alpha",
            Algorithm = MachineLearningAlgorithm.RandomForestRegressor,
            Description = "Complexity parameter used for Minimal Cost-Complexity Pruning.",
            Example = "ccp_alpha=0.0"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForGradientBoostingRegressor()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "loss",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The loss function to be optimized ('squared_error', 'absolute_error', 'huber', 'quantile').",
            Example = "loss='squared_error'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "learning_rate",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "Learning rate shrinks the contribution of each tree.",
            Example = "learning_rate=0.1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_estimators",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The number of boosting stages to be run.",
            Example = "n_estimators=100"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "subsample",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The fraction of samples to be used for fitting the individual base learners.",
            Example = "subsample=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "criterion",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The function to measure the quality of a split ('friedman_mse', 'squared_error', 'mse', 'mae').",
            Example = "criterion='friedman_mse'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_split",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The minimum number of samples required to split an internal node.",
            Example = "min_samples_split=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples_leaf",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The minimum number of samples required to be at a leaf node.",
            Example = "min_samples_leaf=1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_weight_fraction_leaf",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The minimum weighted fraction of the sum total of weights required to be at a leaf node.",
            Example = "min_weight_fraction_leaf=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_depth",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "Maximum depth of the individual regression estimators.",
            Example = "max_depth=3"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_impurity_decrease",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "A node will be split if this split induces a decrease of the impurity greater than or equal to this value.",
            Example = "min_impurity_decrease=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_features",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The number of features to consider when looking for the best split.",
            Example = "max_features='auto'"  // 'auto' means max_features=n_features
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_leaf_nodes",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "Grow trees with max_leaf_nodes in best-first fashion.",
            Example = "max_leaf_nodes=None"  // None means unlimited number of leaf nodes
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "Controls the randomness of the bootstrapping of the samples used when building trees.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "verbose",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "Controls the verbosity when fitting and predicting.",
            Example = "verbose=0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "warm_start",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "When set to True, reuse the solution of the previous call to fit and add more estimators to the ensemble, otherwise just fit a whole new forest.",
            Example = "warm_start=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "validation_fraction",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "The proportion of training data to set aside as validation set for early stopping.",
            Example = "validation_fraction=0.1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_iter_no_change",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "Used to decide if early stopping will be used to terminate training when validation score is not improving.",
            Example = "n_iter_no_change=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "Tolerance for the early stopping.",
            Example = "tol=0.0001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "ccp_alpha",
            Algorithm = MachineLearningAlgorithm.GradientBoostingRegressor,
            Description = "Complexity parameter used for Minimal Cost-Complexity Pruning.",
            Example = "ccp_alpha=0.0"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForSVR()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "kernel",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Specifies the kernel type to be used in the algorithm ('linear', 'poly', 'rbf', 'sigmoid', 'precomputed').",
            Example = "kernel='rbf'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "degree",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Degree of the polynomial kernel function ('poly'). Ignored by all other kernels.",
            Example = "degree=3"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "gamma",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Kernel coefficient for 'rbf', 'poly' and 'sigmoid'.",
            Example = "gamma='scale'  # or 'auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "coef0",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Independent term in kernel function. It is only significant in 'poly' and 'sigmoid'.",
            Example = "coef0=0.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Tolerance for stopping criterion.",
            Example = "tol=0.001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "C",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Regularization parameter. The strength of the regularization is inversely proportional to C.",
            Example = "C=1.0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "epsilon",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Epsilon in the epsilon-SVR model. It specifies the epsilon-tube within which no penalty is associated in the training loss function with points predicted within a distance epsilon from the actual value.",
            Example = "epsilon=0.1"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "shrinking",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Whether to use the shrinking heuristic.",
            Example = "shrinking=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "cache_size",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Specify the size of the kernel cache (in MB).",
            Example = "cache_size=200"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "verbose",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Enable verbose output.",
            Example = "verbose=False"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_iter",
            Algorithm = MachineLearningAlgorithm.SVR,
            Description = "Hard limit on iterations within the solver, or -1 for no limit.",
            Example = "max_iter=-1"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForKMeans()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_clusters",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "The number of clusters to form as well as the number of centroids to generate.",
            Example = "n_clusters=8"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "init",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "Method for initialization ('k-means++', 'random' or an ndarray).",
            Example = "init='k-means++'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_init",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "Number of time the k-means algorithm will be run with different centroid seeds.",
            Example = "n_init=10"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "max_iter",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "Maximum number of iterations of the k-means algorithm for a single run.",
            Example = "max_iter=300"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "tol",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "Relative tolerance with regards to Frobenius norm of the difference in the cluster centers of two consecutive iterations to declare convergence.",
            Example = "tol=0.0001"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "precompute_distances",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "Precompute distances (faster but takes more memory).",
            Example = "precompute_distances='auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "verbose",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "Verbosity mode.",
            Example = "verbose=0"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "random_state",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "Determines random number generation for centroid initialization.",
            Example = "random_state=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "copy_x",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "When precomputing distances it is more numerically accurate to center the data first. If copy_x is True, then the original data is not modified. If False, the data is centered first.",
            Example = "copy_x=True"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_jobs",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "The number of jobs to use for the computation.",
            Example = "n_jobs=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "algorithm",
            Algorithm = MachineLearningAlgorithm.KMeans,
            Description = "K-means algorithm to use ('auto', 'full', 'elkan').",
            Example = "algorithm='auto'"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForDBSCAN()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "eps",
            Algorithm = MachineLearningAlgorithm.DBSCAN,
            Description = "The maximum distance between two samples for one to be considered as in the neighborhood of the other.",
            Example = "eps=0.5"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "min_samples",
            Algorithm = MachineLearningAlgorithm.DBSCAN,
            Description = "The number of samples (or total weight) in a neighborhood for a point to be considered as a core point.",
            Example = "min_samples=5"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "metric",
            Algorithm = MachineLearningAlgorithm.DBSCAN,
            Description = "The metric to use when calculating distance between instances in a feature array.",
            Example = "metric='euclidean'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "metric_params",
            Algorithm = MachineLearningAlgorithm.DBSCAN,
            Description = "Additional keyword arguments for the metric function.",
            Example = "metric_params=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "algorithm",
            Algorithm = MachineLearningAlgorithm.DBSCAN,
            Description = "The algorithm to be used by the NearestNeighbors module to compute pointwise distances and find nearest neighbors.",
            Example = "algorithm='auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "leaf_size",
            Algorithm = MachineLearningAlgorithm.DBSCAN,
            Description = "Leaf size passed to BallTree or KDTree.",
            Example = "leaf_size=30"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "p",
            Algorithm = MachineLearningAlgorithm.DBSCAN,
            Description = "The power of the Minkowski metric to be used to calculate distance between points.",
            Example = "p=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_jobs",
            Algorithm = MachineLearningAlgorithm.DBSCAN,
            Description = "The number of parallel jobs to run.",
            Example = "n_jobs=None"
        }
        // Add more parameters as needed
    };

                return parameters;
            }
            public static List<ParameterDictionaryForAlgorithm> GenerateParametersForAgglomerativeClustering()
            {
                var parameters = new List<ParameterDictionaryForAlgorithm>
    {
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "n_clusters",
            Algorithm = MachineLearningAlgorithm.AgglomerativeClustering,
            Description = "The number of clusters to find.",
            Example = "n_clusters=2"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "affinity",
            Algorithm = MachineLearningAlgorithm.AgglomerativeClustering,
            Description = "Metric used to compute the linkage. Can be 'euclidean', 'l1', 'l2', 'manhattan', 'cosine', or 'precomputed'.",
            Example = "affinity='euclidean'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "memory",
            Algorithm = MachineLearningAlgorithm.AgglomerativeClustering,
            Description = "Used to cache the output of the computation of the tree.",
            Example = "memory=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "connectivity",
            Algorithm = MachineLearningAlgorithm.AgglomerativeClustering,
            Description = "Connectivity matrix. Defines for each sample the neighboring samples following a given structure of the data.",
            Example = "connectivity=None"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "compute_full_tree",
            Algorithm = MachineLearningAlgorithm.AgglomerativeClustering,
            Description = "'auto' or True/False. Whether to compute the full tree.",
            Example = "compute_full_tree='auto'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "linkage",
            Algorithm = MachineLearningAlgorithm.AgglomerativeClustering,
            Description = "Which linkage criterion to use. The linkage criterion determines which distance to use between sets of observation. The algorithm will merge the pairs of cluster that minimize this criterion. Can be 'ward', 'complete', 'average', or 'single'.",
            Example = "linkage='ward'"
        },
        new ParameterDictionaryForAlgorithm
        {
            ParameterName = "distance_threshold",
            Algorithm = MachineLearningAlgorithm.AgglomerativeClustering,
            Description = "The linkage distance threshold above which, clusters will not be merged.",
            Example = "distance_threshold=None"
        }
        // Add more parameters as needed
    };

                return parameters;
            }



        }
    }
