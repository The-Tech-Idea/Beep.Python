/**
 * Template Decision Wizard
 * Helps users select the right template based on their problem and data
 */

class TemplateWizard {
    constructor(templateManager) {
        this.templateManager = templateManager;
        this.currentStep = 0;
        this.answers = {};
        this.recommendations = [];
    }

    getQuestions() {
        return [
            {
                id: 'problem_type',
                question: 'What type of problem are you trying to solve?',
                type: 'single',
                options: [
                    {
                        value: 'classification',
                        label: 'Classification',
                        description: 'Predicting categories or classes (e.g., spam/not spam, disease diagnosis)',
                        icon: 'bi-tags',
                        color: '#1976d2'
                    },
                    {
                        value: 'regression',
                        label: 'Regression',
                        description: 'Predicting continuous values (e.g., price, temperature, sales)',
                        icon: 'bi-graph-up',
                        color: '#2e7d32'
                    },
                    {
                        value: 'clustering',
                        label: 'Clustering',
                        description: 'Finding groups or patterns in data without labels',
                        icon: 'bi-diagram-3',
                        color: '#9c27b0'
                    },
                    {
                        value: 'anomaly',
                        label: 'Anomaly Detection',
                        description: 'Identifying outliers or unusual patterns',
                        icon: 'bi-exclamation-triangle',
                        color: '#ff5722'
                    },
                    {
                        value: 'dimensionality',
                        label: 'Dimensionality Reduction',
                        description: 'Reducing number of features while preserving information',
                        icon: 'bi-arrow-down-up',
                        color: '#f44336'
                    },
                    {
                        value: 'timeseries',
                        label: 'Time Series Forecasting',
                        description: 'Predicting future values based on time-based data',
                        icon: 'bi-clock-history',
                        color: '#ff9800'
                    }
                ]
            },
            {
                id: 'data_size',
                question: 'What is the approximate size of your dataset?',
                type: 'single',
                options: [
                    {
                        value: 'small',
                        label: 'Small (< 10,000 rows)',
                        description: 'Small datasets, quick training'
                    },
                    {
                        value: 'medium',
                        label: 'Medium (10,000 - 100,000 rows)',
                        description: 'Moderate datasets'
                    },
                    {
                        value: 'large',
                        label: 'Large (> 100,000 rows)',
                        description: 'Large datasets, may need optimization'
                    }
                ],
                showIf: (answers) => ['classification', 'regression'].includes(answers.problem_type)
            },
            {
                id: 'interpretability',
                question: 'How important is model interpretability?',
                type: 'single',
                options: [
                    {
                        value: 'high',
                        label: 'Very Important',
                        description: 'Need to understand how the model makes decisions',
                        recommendation: 'classification_basic' // Decision Tree, Logistic Regression
                    },
                    {
                        value: 'medium',
                        label: 'Somewhat Important',
                        description: 'Balance between accuracy and interpretability'
                    },
                    {
                        value: 'low',
                        label: 'Not Important',
                        description: 'Maximum accuracy is the priority',
                        recommendation: 'classification_advanced' // Random Forest, XGBoost
                    }
                ],
                showIf: (answers) => answers.problem_type === 'classification'
            },
            {
                id: 'linear_relationship',
                question: 'Do you expect a linear relationship in your data?',
                type: 'single',
                options: [
                    {
                        value: 'yes',
                        label: 'Yes',
                        description: 'Linear models (Linear Regression, Ridge, Lasso)',
                        recommendation: 'regression_linear'
                    },
                    {
                        value: 'no',
                        label: 'No',
                        description: 'Non-linear models (Random Forest, SVM)',
                        recommendation: 'regression_basic'
                    },
                    {
                        value: 'unknown',
                        label: 'Not Sure',
                        description: 'Start with linear, can switch if needed',
                        recommendation: 'regression_linear'
                    }
                ],
                showIf: (answers) => answers.problem_type === 'regression'
            },
            {
                id: 'cluster_shape',
                question: 'What shape of clusters do you expect?',
                type: 'single',
                options: [
                    {
                        value: 'spherical',
                        label: 'Spherical/Uniform',
                        description: 'K-Means works well',
                        recommendation: 'clustering_basic'
                    },
                    {
                        value: 'irregular',
                        label: 'Irregular/Arbitrary',
                        description: 'DBSCAN for density-based clustering',
                        recommendation: 'clustering_dbscan'
                    },
                    {
                        value: 'unknown',
                        label: 'Not Sure',
                        description: 'Start with K-Means',
                        recommendation: 'clustering_basic'
                    }
                ],
                showIf: (answers) => answers.problem_type === 'clustering'
            },
            {
                id: 'anomaly_type',
                question: 'What type of anomalies are you looking for?',
                type: 'single',
                options: [
                    {
                        value: 'outliers',
                        label: 'Outliers in Normal Data',
                        description: 'Isolation Forest for general outlier detection',
                        recommendation: 'anomaly_basic'
                    },
                    {
                        value: 'novelty',
                        label: 'Novel Patterns',
                        description: 'One-Class SVM for novelty detection',
                        recommendation: 'anomaly_basic'
                    }
                ],
                showIf: (answers) => answers.problem_type === 'anomaly'
            },
            {
                id: 'dimensionality_goal',
                question: 'What is your goal for dimensionality reduction?',
                type: 'single',
                options: [
                    {
                        value: 'visualization',
                        label: 'Visualization (2D/3D)',
                        description: 't-SNE or UMAP for visualization',
                        recommendation: 'dimensionality_pca'
                    },
                    {
                        value: 'feature_reduction',
                        label: 'Feature Reduction',
                        description: 'PCA for reducing features while preserving variance',
                        recommendation: 'dimensionality_pca'
                    },
                    {
                        value: 'preprocessing',
                        label: 'Preprocessing for ML',
                        description: 'PCA before training models',
                        recommendation: 'dimensionality_pca'
                    }
                ],
                showIf: (answers) => answers.problem_type === 'dimensionality'
            },
            {
                id: 'timeseries_type',
                question: 'What type of time series forecasting do you need?',
                type: 'single',
                options: [
                    {
                        value: 'univariate',
                        label: 'Single Variable',
                        description: 'ARIMA for univariate time series',
                        recommendation: 'timeseries_forecast'
                    },
                    {
                        value: 'multivariate',
                        label: 'Multiple Variables',
                        description: 'Advanced models with multiple features',
                        recommendation: 'timeseries_forecast'
                    }
                ],
                showIf: (answers) => answers.problem_type === 'timeseries'
            }
        ];
    }

    getRecommendations() {
        const answers = this.answers;
        const recommendations = [];

        // Direct recommendations from answers
        const questions = this.getQuestions();
        questions.forEach(q => {
            if (q.showIf && !q.showIf(answers)) return;
            
            const selectedOption = q.options.find(opt => answers[q.id] === opt.value);
            if (selectedOption && selectedOption.recommendation) {
                recommendations.push({
                    templateId: selectedOption.recommendation,
                    reason: selectedOption.description,
                    confidence: 'high'
                });
            }
        });

        // Default recommendations based on problem type
        if (recommendations.length === 0) {
            const problemType = answers.problem_type;
            const defaults = {
                'classification': { templateId: 'classification_basic', reason: 'Standard classification pipeline with Random Forest' },
                'regression': { templateId: 'regression_basic', reason: 'Standard regression pipeline with Random Forest' },
                'clustering': { templateId: 'clustering_basic', reason: 'K-Means clustering pipeline' },
                'anomaly': { templateId: 'anomaly_basic', reason: 'Isolation Forest for anomaly detection' },
                'dimensionality': { templateId: 'dimensionality_pca', reason: 'PCA for dimensionality reduction' },
                'timeseries': { templateId: 'timeseries_forecast', reason: 'ARIMA-based time series forecasting' }
            };

            if (defaults[problemType]) {
                recommendations.push({
                    ...defaults[problemType],
                    confidence: 'medium'
                });
            }
        }

        // Add alternative recommendations
        if (answers.problem_type === 'classification') {
            if (answers.interpretability === 'high') {
                recommendations.push({
                    templateId: 'classification_basic',
                    reason: 'Simple, interpretable models',
                    confidence: 'high'
                });
            } else if (answers.interpretability === 'low') {
                recommendations.push({
                    templateId: 'classification_advanced',
                    reason: 'Advanced ensemble methods for maximum accuracy',
                    confidence: 'high'
                });
            }
        }

        return recommendations;
    }

    answer(questionId, value) {
        this.answers[questionId] = value;
    }

    getCurrentQuestion() {
        const questions = this.getQuestions();
        const visibleQuestions = questions.filter(q => {
            if (!q.showIf) return true;
            return q.showIf(this.answers);
        });
        
        // Ensure currentStep is within bounds
        if (this.currentStep >= visibleQuestions.length) {
            this.currentStep = visibleQuestions.length - 1;
        }
        if (this.currentStep < 0) {
            this.currentStep = 0;
        }
        
        return visibleQuestions[this.currentStep];
    }

    next() {
        const questions = this.getQuestions();
        const visibleQuestions = questions.filter(q => {
            if (!q.showIf) return true;
            return q.showIf(this.answers);
        });

        if (this.currentStep < visibleQuestions.length - 1) {
            this.currentStep++;
            return true;
        }
        return false;
    }

    previous() {
        if (this.currentStep > 0) {
            this.currentStep--;
            return true;
        }
        return false;
    }

    reset() {
        this.currentStep = 0;
        this.answers = {};
        this.recommendations = [];
    }

    isComplete() {
        const questions = this.getQuestions();
        const visibleQuestions = questions.filter(q => {
            if (!q.showIf) return true;
            return q.showIf(this.answers);
        });

        return this.currentStep >= visibleQuestions.length - 1 && 
               visibleQuestions.every(q => this.answers[q.id] !== undefined);
    }
}

// Export
if (typeof window !== 'undefined') {
    window.TemplateWizard = TemplateWizard;
}

if (typeof module !== 'undefined' && module.exports) {
    module.exports = TemplateWizard;
}

