/**
 * Workflow Templates
 * Pre-built workflow templates for common ML scenarios
 */

class WorkflowTemplateManager {
    constructor() {
        this.templates = this._initializeTemplates();
    }

    _initializeTemplates() {
        return {
            // Classification Templates
            'classification_basic': {
                id: 'classification_basic',
                name: 'Basic Classification Pipeline',
                description: 'Standard classification workflow: Load → Select Target → Split → Scale → Train → Evaluate → Save',
                category: 'classification',
                icon: 'bi-diagram-2',
                color: '#1976d2',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Classification Pipeline Started' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df',
                                delimiter: ',',
                                header: true
                            }
                        },
                        {
                            id: 'node_select_target',
                            type: 'preprocess_select_features_target',
                            position: { x: 400, y: 150 },
                            data: {
                                target_column: 'target',
                                feature_columns: '',
                                drop_target_from_features: true
                            }
                        },
                        {
                            id: 'node_split',
                            type: 'preprocess_split',
                            position: { x: 600, y: 150 },
                            data: {
                                test_size: 0.2,
                                random_state: 42,
                                stratify: 'y'
                            }
                        },
                        {
                            id: 'node_scale',
                            type: 'preprocess_scale',
                            position: { x: 800, y: 150 },
                            data: {
                                with_mean: true,
                                with_std: true
                            }
                        },
                        {
                            id: 'node_train',
                            type: 'algo_random_forest_classifier',
                            position: { x: 1000, y: 150 },
                            data: {
                                n_estimators: 100,
                                max_depth: null,
                                min_samples_split: 2,
                                min_samples_leaf: 1,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_evaluate',
                            type: 'evaluate_metrics',
                            position: { x: 1200, y: 150 },
                            data: {
                                metrics: ['accuracy', 'precision', 'recall', 'f1']
                            }
                        },
                        {
                            id: 'node_save',
                            type: 'save_model',
                            position: { x: 1400, y: 150 },
                            data: {
                                file_path: 'models/classifier.pkl',
                                format: 'pickle'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_target', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'features', targetPort: 'features' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'target', targetPort: 'target' },
                        { source: 'node_split', target: 'node_scale', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_scale', target: 'node_train', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_train', target: 'node_evaluate', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_evaluate', target: 'node_save', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            'classification_advanced': {
                id: 'classification_advanced',
                name: 'Advanced Classification Pipeline',
                description: 'Advanced classification with data cleaning, encoding, and feature engineering',
                category: 'classification',
                icon: 'bi-diagram-3',
                color: '#0277bd',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Advanced Classification Pipeline' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df',
                                delimiter: ',',
                                header: true
                            }
                        },
                        {
                            id: 'node_clean',
                            type: 'pandas_dropna',
                            position: { x: 350, y: 150 },
                            data: {
                                remove_duplicates: true,
                                handle_missing: 'drop'
                            }
                        },
                        {
                            id: 'node_select_target',
                            type: 'preprocess_select_features_target',
                            position: { x: 500, y: 150 },
                            data: {
                                target_column: 'target',
                                feature_columns: '',
                                drop_target_from_features: true
                            }
                        },
                        {
                            id: 'node_encode',
                            type: 'preprocess_onehot',
                            position: { x: 650, y: 150 },
                            data: {
                                drop: 'first',
                                sparse: false
                            }
                        },
                        {
                            id: 'node_split',
                            type: 'preprocess_split',
                            position: { x: 800, y: 150 },
                            data: {
                                test_size: 0.2,
                                random_state: 42,
                                stratify: 'y'
                            }
                        },
                        {
                            id: 'node_scale',
                            type: 'preprocess_scale',
                            position: { x: 950, y: 150 },
                            data: {
                                with_mean: true,
                                with_std: true
                            }
                        },
                        {
                            id: 'node_train',
                            type: 'algo_random_forest_classifier',
                            position: { x: 1100, y: 150 },
                            data: {
                                n_estimators: 200,
                                max_depth: 10,
                                min_samples_split: 5,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_evaluate',
                            type: 'evaluate_metrics',
                            position: { x: 1250, y: 150 },
                            data: {
                                metrics: ['accuracy', 'precision', 'recall', 'f1', 'roc_auc']
                            }
                        },
                        {
                            id: 'node_save',
                            type: 'save_model',
                            position: { x: 1400, y: 150 },
                            data: {
                                file_path: 'models/advanced_classifier.pkl',
                                format: 'pickle'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_clean', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_clean', target: 'node_select_target', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_target', target: 'node_encode', sourcePort: 'features', targetPort: 'input' },
                        { source: 'node_encode', target: 'node_split', sourcePort: 'output', targetPort: 'features' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'target', targetPort: 'target' },
                        { source: 'node_split', target: 'node_scale', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_scale', target: 'node_train', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_train', target: 'node_evaluate', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_evaluate', target: 'node_save', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            // Regression Templates
            'regression_basic': {
                id: 'regression_basic',
                name: 'Basic Regression Pipeline',
                description: 'Standard regression workflow: Load → Select Target → Split → Scale → Train → Evaluate → Save',
                category: 'regression',
                icon: 'bi-graph-up',
                color: '#2e7d32',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Regression Pipeline Started' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df',
                                delimiter: ',',
                                header: true
                            }
                        },
                        {
                            id: 'node_select_target',
                            type: 'preprocess_select_features_target',
                            position: { x: 400, y: 150 },
                            data: {
                                target_column: 'price',
                                feature_columns: '',
                                drop_target_from_features: true
                            }
                        },
                        {
                            id: 'node_split',
                            type: 'preprocess_split',
                            position: { x: 600, y: 150 },
                            data: {
                                test_size: 0.2,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_scale',
                            type: 'preprocess_scale',
                            position: { x: 800, y: 150 },
                            data: {
                                with_mean: true,
                                with_std: true
                            }
                        },
                        {
                            id: 'node_train',
                            type: 'algo_random_forest_regressor',
                            position: { x: 1000, y: 150 },
                            data: {
                                n_estimators: 100,
                                max_depth: null,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_evaluate',
                            type: 'evaluate_metrics',
                            position: { x: 1200, y: 150 },
                            data: {
                                metrics: ['mse', 'rmse', 'mae', 'r2']
                            }
                        },
                        {
                            id: 'node_save',
                            type: 'save_model',
                            position: { x: 1400, y: 150 },
                            data: {
                                file_path: 'models/regressor.pkl',
                                format: 'pickle'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_target', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'features', targetPort: 'features' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'target', targetPort: 'target' },
                        { source: 'node_split', target: 'node_scale', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_scale', target: 'node_train', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_train', target: 'node_evaluate', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_evaluate', target: 'node_save', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            'regression_linear': {
                id: 'regression_linear',
                name: 'Linear Regression Pipeline',
                description: 'Simple linear regression: Load → Select Target → Split → Train → Evaluate → Save',
                category: 'regression',
                icon: 'bi-graph-up-arrow',
                color: '#388e3c',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Linear Regression Pipeline' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df'
                            }
                        },
                        {
                            id: 'node_select_target',
                            type: 'preprocess_select_features_target',
                            position: { x: 400, y: 150 },
                            data: {
                                target_column: 'price',
                                feature_columns: '',
                                drop_target_from_features: true
                            }
                        },
                        {
                            id: 'node_split',
                            type: 'preprocess_split',
                            position: { x: 600, y: 150 },
                            data: {
                                test_size: 0.2,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_train',
                            type: 'algo_linear_regression',
                            position: { x: 800, y: 150 },
                            data: {
                                fit_intercept: true,
                                normalize: false
                            }
                        },
                        {
                            id: 'node_evaluate',
                            type: 'evaluate_metrics',
                            position: { x: 1000, y: 150 },
                            data: {
                                metrics: ['mse', 'rmse', 'mae', 'r2']
                            }
                        },
                        {
                            id: 'node_save',
                            type: 'save_model',
                            position: { x: 1200, y: 150 },
                            data: {
                                file_path: 'models/linear_regressor.pkl',
                                format: 'pickle'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_target', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'features', targetPort: 'features' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'target', targetPort: 'target' },
                        { source: 'node_split', target: 'node_train', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_train', target: 'node_evaluate', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_evaluate', target: 'node_save', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            // Clustering Templates
            'clustering_basic': {
                id: 'clustering_basic',
                name: 'K-Means Clustering Pipeline',
                description: 'K-Means clustering: Load → Select Features → Scale → Cluster → Visualize',
                category: 'clustering',
                icon: 'bi-diagram-3',
                color: '#9c27b0',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Clustering Pipeline Started' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df'
                            }
                        },
                        {
                            id: 'node_select_columns',
                            type: 'data_select_columns',
                            position: { x: 400, y: 150 },
                            data: {
                                columns: '',
                                keep: true
                            }
                        },
                        {
                            id: 'node_scale',
                            type: 'preprocess_scale',
                            position: { x: 600, y: 150 },
                            data: {
                                with_mean: true,
                                with_std: true
                            }
                        },
                        {
                            id: 'node_cluster',
                            type: 'algo_kmeans',
                            position: { x: 800, y: 150 },
                            data: {
                                n_clusters: 5,
                                init: 'k-means++',
                                n_init: 10,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_visualize',
                            type: 'viz_scatter',
                            position: { x: 1000, y: 150 },
                            data: {
                                x_column: 'feature_1',
                                y_column: 'feature_2',
                                color_by: 'cluster'
                            }
                        },
                        {
                            id: 'node_export',
                            type: 'export_results',
                            position: { x: 1200, y: 150 },
                            data: {
                                file_path: 'outputs/cluster_results.csv',
                                format: 'csv'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_columns', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_columns', target: 'node_scale', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_scale', target: 'node_cluster', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_cluster', target: 'node_visualize', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_visualize', target: 'node_export', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            'clustering_dbscan': {
                id: 'clustering_dbscan',
                name: 'DBSCAN Clustering Pipeline',
                description: 'Density-based clustering: Load → Select Features → Scale → DBSCAN → Visualize',
                category: 'clustering',
                icon: 'bi-diagram-3',
                color: '#e91e63',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'DBSCAN Clustering Pipeline' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df'
                            }
                        },
                        {
                            id: 'node_select_columns',
                            type: 'data_select_columns',
                            position: { x: 400, y: 150 },
                            data: {
                                columns: '',
                                keep: true
                            }
                        },
                        {
                            id: 'node_scale',
                            type: 'preprocess_scale',
                            position: { x: 600, y: 150 },
                            data: {
                                with_mean: true,
                                with_std: true
                            }
                        },
                        {
                            id: 'node_cluster',
                            type: 'algo_dbscan',
                            position: { x: 800, y: 150 },
                            data: {
                                eps: 0.5,
                                min_samples: 5,
                                metric: 'euclidean'
                            }
                        },
                        {
                            id: 'node_visualize',
                            type: 'viz_scatter',
                            position: { x: 1000, y: 150 },
                            data: {
                                x_column: 'feature_1',
                                y_column: 'feature_2',
                                color_by: 'cluster'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_columns', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_columns', target: 'node_scale', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_scale', target: 'node_cluster', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_cluster', target: 'node_visualize', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            // Anomaly Detection Templates
            'anomaly_basic': {
                id: 'anomaly_basic',
                name: 'Anomaly Detection Pipeline',
                description: 'Isolation Forest: Load → Select Features → Scale → Detect Anomalies → Visualize',
                category: 'anomaly',
                icon: 'bi-exclamation-triangle',
                color: '#ff5722',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Anomaly Detection Pipeline' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df'
                            }
                        },
                        {
                            id: 'node_select_columns',
                            type: 'data_select_columns',
                            position: { x: 400, y: 150 },
                            data: {
                                columns: '',
                                keep: true
                            }
                        },
                        {
                            id: 'node_scale',
                            type: 'preprocess_scale',
                            position: { x: 600, y: 150 },
                            data: {
                                with_mean: true,
                                with_std: true
                            }
                        },
                        {
                            id: 'node_detect',
                            type: 'algo_isolation_forest',
                            position: { x: 800, y: 150 },
                            data: {
                                n_estimators: 100,
                                contamination: 0.1,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_visualize',
                            type: 'viz_scatter',
                            position: { x: 1000, y: 150 },
                            data: {
                                x_column: 'feature_1',
                                y_column: 'feature_2',
                                color_by: 'anomaly'
                            }
                        },
                        {
                            id: 'node_export',
                            type: 'export_results',
                            position: { x: 1200, y: 150 },
                            data: {
                                file_path: 'outputs/anomalies.csv',
                                format: 'csv'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_columns', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_columns', target: 'node_scale', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_scale', target: 'node_detect', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_detect', target: 'node_visualize', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_visualize', target: 'node_export', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            // Dimensionality Reduction Templates
            'dimensionality_pca': {
                id: 'dimensionality_pca',
                name: 'PCA Dimensionality Reduction',
                description: 'PCA: Load → Select Features → Scale → Reduce Dimensions → Visualize',
                category: 'dimensionality',
                icon: 'bi-arrow-down-up',
                color: '#f44336',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'PCA Pipeline' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df'
                            }
                        },
                        {
                            id: 'node_select_columns',
                            type: 'data_select_columns',
                            position: { x: 400, y: 150 },
                            data: {
                                columns: '',
                                keep: true
                            }
                        },
                        {
                            id: 'node_scale',
                            type: 'preprocess_scale',
                            position: { x: 600, y: 150 },
                            data: {
                                with_mean: true,
                                with_std: true
                            }
                        },
                        {
                            id: 'node_pca',
                            type: 'algo_pca',
                            position: { x: 800, y: 150 },
                            data: {
                                n_components: 2,
                                whiten: false,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_visualize',
                            type: 'viz_scatter',
                            position: { x: 1000, y: 150 },
                            data: {
                                x_column: 'PC1',
                                y_column: 'PC2'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_columns', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_columns', target: 'node_scale', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_scale', target: 'node_pca', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_pca', target: 'node_visualize', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            // Time Series Templates
            'timeseries_forecast': {
                id: 'timeseries_forecast',
                name: 'Time Series Forecasting',
                description: 'ARIMA: Load Time Series → Select Target → Fit Model → Forecast → Visualize',
                category: 'timeseries',
                icon: 'bi-graph-up',
                color: '#ff9800',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Time Series Forecasting Pipeline' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/timeseries.csv',
                                variable_name: 'df'
                            }
                        },
                        {
                            id: 'node_select_target',
                            type: 'preprocess_select_target',
                            position: { x: 400, y: 150 },
                            data: {
                                target_column: 'value'
                            }
                        },
                        {
                            id: 'node_arima',
                            type: 'timeseries_arima',
                            position: { x: 600, y: 150 },
                            data: {
                                order: '(1,1,1)',
                                seasonal: false
                            }
                        },
                        {
                            id: 'node_forecast',
                            type: 'timeseries_forecast',
                            position: { x: 800, y: 150 },
                            data: {
                                steps: 30
                            }
                        },
                        {
                            id: 'node_visualize',
                            type: 'viz_line',
                            position: { x: 1000, y: 150 },
                            data: {
                                x_column: 'date',
                                y_column: 'value'
                            }
                        },
                        {
                            id: 'node_export',
                            type: 'export_results',
                            position: { x: 1200, y: 150 },
                            data: {
                                file_path: 'outputs/forecast.csv',
                                format: 'csv'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_target', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_target', target: 'node_arima', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_arima', target: 'node_forecast', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_forecast', target: 'node_visualize', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_visualize', target: 'node_export', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            // Text Classification Template
            'text_classification': {
                id: 'text_classification',
                name: 'Text Classification Pipeline',
                description: 'NLP: Load Text → Select Target → TF-IDF → Train Classifier → Evaluate',
                category: 'nlp',
                icon: 'bi-chat-text',
                color: '#00bcd4',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Text Classification Pipeline' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/text_data.csv',
                                variable_name: 'df'
                            }
                        },
                        {
                            id: 'node_select_target',
                            type: 'preprocess_select_features_target',
                            position: { x: 400, y: 150 },
                            data: {
                                target_column: 'label',
                                feature_columns: 'text',
                                drop_target_from_features: true
                            }
                        },
                        {
                            id: 'node_split',
                            type: 'preprocess_split',
                            position: { x: 600, y: 150 },
                            data: {
                                test_size: 0.2,
                                random_state: 42,
                                stratify: 'y'
                            }
                        },
                        {
                            id: 'node_train',
                            type: 'algo_naive_bayes',
                            position: { x: 800, y: 150 },
                            data: {
                                alpha: 1.0
                            }
                        },
                        {
                            id: 'node_evaluate',
                            type: 'evaluate_metrics',
                            position: { x: 1000, y: 150 },
                            data: {
                                metrics: ['accuracy', 'precision', 'recall', 'f1']
                            }
                        },
                        {
                            id: 'node_save',
                            type: 'save_model',
                            position: { x: 1200, y: 150 },
                            data: {
                                file_path: 'models/text_classifier.pkl',
                                format: 'pickle'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_target', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'features', targetPort: 'features' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'target', targetPort: 'target' },
                        { source: 'node_split', target: 'node_train', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_train', target: 'node_evaluate', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_evaluate', target: 'node_save', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            },

            // Deep Learning Template
            'deep_learning_classification': {
                id: 'deep_learning_classification',
                name: 'Deep Learning Classification',
                description: 'Neural Network: Load → Select Target → Split → Scale → Build Model → Train → Evaluate',
                category: 'deep-learning',
                icon: 'bi-cpu',
                color: '#673ab7',
                workflow: {
                    nodes: [
                        {
                            id: 'node_start',
                            type: 'start',
                            position: { x: 50, y: 150 },
                            data: { message: 'Deep Learning Classification Pipeline' }
                        },
                        {
                            id: 'node_load_csv',
                            type: 'data_load_csv',
                            position: { x: 200, y: 150 },
                            data: {
                                file_path: 'data/dataset.csv',
                                variable_name: 'df'
                            }
                        },
                        {
                            id: 'node_select_target',
                            type: 'preprocess_select_features_target',
                            position: { x: 400, y: 150 },
                            data: {
                                target_column: 'target',
                                feature_columns: '',
                                drop_target_from_features: true
                            }
                        },
                        {
                            id: 'node_split',
                            type: 'preprocess_split',
                            position: { x: 600, y: 150 },
                            data: {
                                test_size: 0.2,
                                random_state: 42
                            }
                        },
                        {
                            id: 'node_scale',
                            type: 'preprocess_scale',
                            position: { x: 800, y: 150 },
                            data: {
                                with_mean: true,
                                with_std: true
                            }
                        },
                        {
                            id: 'node_train',
                            type: 'tensorflow_sequential',
                            position: { x: 1000, y: 150 },
                            data: {
                                layers: '128,64,32',
                                activation: 'relu',
                                output_activation: 'softmax',
                                epochs: 50,
                                batch_size: 32
                            }
                        },
                        {
                            id: 'node_evaluate',
                            type: 'evaluate_metrics',
                            position: { x: 1200, y: 150 },
                            data: {
                                metrics: ['accuracy', 'precision', 'recall', 'f1']
                            }
                        },
                        {
                            id: 'node_save',
                            type: 'save_model',
                            position: { x: 1400, y: 150 },
                            data: {
                                file_path: 'models/deep_learning_model.h5',
                                format: 'h5'
                            }
                        }
                    ],
                    edges: [
                        { source: 'node_start', target: 'node_load_csv', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_load_csv', target: 'node_select_target', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'features', targetPort: 'features' },
                        { source: 'node_select_target', target: 'node_split', sourcePort: 'target', targetPort: 'target' },
                        { source: 'node_split', target: 'node_scale', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_scale', target: 'node_train', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_train', target: 'node_evaluate', sourcePort: 'output', targetPort: 'input' },
                        { source: 'node_evaluate', target: 'node_save', sourcePort: 'output', targetPort: 'input' }
                    ]
                }
            }
        };
    }

    /**
     * Get all templates
     */
    getAllTemplates() {
        return Object.values(this.templates);
    }

    /**
     * Get templates by category
     */
    getTemplatesByCategory(category) {
        return Object.values(this.templates).filter(t => t.category === category);
    }

    /**
     * Get template by ID
     */
    getTemplate(templateId) {
        return this.templates[templateId];
    }

    /**
     * Apply template to workflow builder
     * @param {string} templateId - Template ID to apply
     * @param {object} workflowBuilder - Workflow builder instance
     * @param {object} preprocessingOptions - Optional preprocessing options
     * @param {boolean} preprocessingOptions.handleMissing - Add missing value handling
     * @param {boolean} preprocessingOptions.encodeCategorical - Add categorical encoding
     * @param {boolean} preprocessingOptions.dropHighCardinality - Drop high cardinality columns
     */
    applyTemplate(templateId, workflowBuilder, preprocessingOptions = {}) {
        const template = this.getTemplate(templateId);
        if (!template) {
            throw new Error(`Template not found: ${templateId}`);
        }

        // Check if preprocessing options require adding a data prep node
        const addDataPrep = preprocessingOptions.handleMissing || 
                            preprocessingOptions.encodeCategorical || 
                            preprocessingOptions.dropHighCardinality;
        
        // Build list of nodes to check (template nodes + optional data prep node)
        let nodesToValidate = [...template.workflow.nodes];
        if (addDataPrep) {
            nodesToValidate.push({ type: 'auto_data_prep' });
        }

        // Validate all required node types are registered
        const missingTypes = [];
        nodesToValidate.forEach(node => {
            if (typeof nodeRegistry !== 'undefined' && !nodeRegistry.get(node.type)) {
                missingTypes.push(node.type);
            }
        });
        
        if (missingTypes.length > 0) {
            const msg = `Cannot apply template: The following node types are not registered:\n- ${missingTypes.join('\n- ')}\n\nPlease refresh the page and try again.`;
            console.error(msg);
            alert(msg);
            throw new Error(msg);
        }
        
        // Copy template nodes and edges (we may modify them)
        let templateNodes = JSON.parse(JSON.stringify(template.workflow.nodes));
        let templateEdges = JSON.parse(JSON.stringify(template.workflow.edges));

        console.log(`Applying template '${templateId}' with ${templateNodes.length} nodes and ${templateEdges.length} edges`);
        console.log('Preprocessing options:', preprocessingOptions);

        // Clear existing workflow
        workflowBuilder.clearCanvas();
        
        // If preprocessing is enabled, add Auto Data Prep node after Load CSV
        if (addDataPrep) {
            // Find the load_csv node and the node it connects to
            const loadCsvNodeIndex = templateNodes.findIndex(n => n.type === 'data_load_csv');
            if (loadCsvNodeIndex !== -1) {
                const loadCsvNode = templateNodes[loadCsvNodeIndex];
                
                // Find what the load_csv connects to
                const edgeFromLoad = templateEdges.find(e => e.source === loadCsvNode.id);
                
                if (edgeFromLoad) {
                    // Create Auto Data Prep node between load and its target
                    const dataPrepNodeId = 'node_data_prep';
                    const targetNode = templateNodes.find(n => n.id === edgeFromLoad.target);
                    
                    // Position data prep node between load and target
                    const dataPrepX = loadCsvNode.position.x + 150;
                    const dataPrepY = loadCsvNode.position.y;
                    
                    // Shift all nodes after load_csv to the right to make room
                    templateNodes.forEach(n => {
                        if (n.position.x > loadCsvNode.position.x) {
                            n.position.x += 150;
                        }
                    });
                    
                    // Add the data prep node
                    const dataPrepNode = {
                        id: dataPrepNodeId,
                        type: 'auto_data_prep',
                        position: { x: dataPrepX, y: dataPrepY },
                        data: {
                            handle_missing: preprocessingOptions.handleMissing ? 'fill_median' : 'drop',
                            encode_categoricals: preprocessingOptions.encodeCategorical !== false,
                            drop_high_cardinality: preprocessingOptions.dropHighCardinality !== false,
                            max_categories: 10
                        }
                    };
                    
                    // Insert after load_csv node
                    templateNodes.splice(loadCsvNodeIndex + 1, 0, dataPrepNode);
                    
                    // Update edges: load_csv -> data_prep -> original_target
                    // 1. Change load_csv edge to point to data_prep
                    edgeFromLoad.target = dataPrepNodeId;
                    
                    // 2. Add new edge from data_prep to original target
                    templateEdges.push({
                        source: dataPrepNodeId,
                        target: targetNode ? targetNode.id : edgeFromLoad.target,
                        sourcePort: 'output',
                        targetPort: edgeFromLoad.targetPort || 'input'
                    });
                    
                    console.log('Added Auto Data Prep node to template');
                }
            }
        }

        // Generate unique node IDs to avoid conflicts
        const nodeIdMap = {};
        const newNodes = templateNodes.map(node => {
            const newId = `node_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
            nodeIdMap[node.id] = newId;
            return {
                ...node,
                id: newId
            };
        });

        // Create nodes with validation
        let createdCount = 0;
        let failedNodes = [];
        
        newNodes.forEach(node => {
            // Check if node type exists in registry before creating
            if (typeof nodeRegistry !== 'undefined' && !nodeRegistry.get(node.type)) {
                console.error(`Template error: Node type '${node.type}' not found in registry! Skipping node.`);
                failedNodes.push({ id: node.id, type: node.type });
                return;
            }
            
            workflowBuilder.createNode(
                node.type,
                node.position.x,
                node.position.y,
                node.id
            );

            // Verify node was created
            const createdElement = document.getElementById(node.id);
            if (!createdElement) {
                console.error(`Template error: Failed to create node '${node.type}' with ID '${node.id}'`);
                failedNodes.push({ id: node.id, type: node.type });
                return;
            }
            
            createdCount++;

            // Set node data
            const workflowNode = workflowBuilder.workflowNodes.find(n => n.id === node.id);
            if (workflowNode && node.data) {
                workflowNode.data = { ...workflowNode.data, ...node.data };
            }
        });
        
        console.log(`Template nodes created: ${createdCount}/${newNodes.length}`);
        if (failedNodes.length > 0) {
            console.error('Failed to create nodes:', failedNodes);
            alert(`Warning: ${failedNodes.length} node(s) failed to create. Check console for details.`);
        }

        // Create edges with updated node IDs - use same logic as loadWorkflowData
        setTimeout(() => {
            let loadedCount = 0;
            templateEdges.forEach((edge, index) => {
                setTimeout(() => {
                    const sourceId = nodeIdMap[edge.source] || edge.source;
                    const targetId = nodeIdMap[edge.target] || edge.target;
                    
                    const sourceEl = document.getElementById(sourceId);
                    const targetEl = document.getElementById(targetId);
                    
                    if (sourceEl && targetEl && workflowBuilder.jsPlumbInstance) {
                        try {
                            // Use same connection logic as loadWorkflowData
                            const sourcePort = edge.sourcePort || 'output';
                            const targetPort = edge.targetPort || 'input';
                            
                            // Try connecting by node IDs with left/right anchors (same as loadWorkflowData)
                            const connection = workflowBuilder.jsPlumbInstance.connect({
                                source: sourceId,
                                target: targetId,
                                anchors: ['Right', 'Left'],  // Right for output, Left for input
                                paintStyle: { stroke: '#0d6efd', strokeWidth: 2 },
                                hoverPaintStyle: { stroke: '#0a58ca', strokeWidth: 3 },
                                endpoint: ['Dot', { radius: 8 }],
                                overlays: [
                                    ['Arrow', { location: 1, width: 10, length: 10, foldback: 0.8 }]
                                ]
                            });
                            
                            if (connection) {
                                loadedCount++;
                                // Store edge in workflowEdges
                                workflowBuilder.workflowEdges.push({
                                    source: sourceId,
                                    target: targetId,
                                    sourcePort: sourcePort,
                                    targetPort: targetPort
                                });
                            }
                        } catch (error) {
                            console.error(`Error connecting ${sourceId} to ${targetId}:`, error);
                        }
                    } else {
                        console.warn(`Cannot connect: nodes not found`, { sourceId, targetId });
                    }
                }, index * 50); // Stagger connections
            });

            // Auto-save after all connections
            setTimeout(() => {
                console.log(`Template applied: ${loadedCount} connections created`);
                workflowBuilder.autoSave();
            }, templateEdges.length * 50 + 500);
        }, 500);

        return template;
    }

    /**
     * Get template categories
     */
    getCategories() {
        const categories = new Set();
        Object.values(this.templates).forEach(t => categories.add(t.category));
        return Array.from(categories);
    }
}

// Global instance
const workflowTemplateManager = new WorkflowTemplateManager();

// Export
if (typeof window !== 'undefined') {
    window.workflowTemplateManager = workflowTemplateManager;
}

if (typeof module !== 'undefined' && module.exports) {
    module.exports = WorkflowTemplateManager;
}

