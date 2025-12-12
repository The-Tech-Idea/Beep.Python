/**
 * Anomaly Detection Algorithm Nodes
 * Algorithms for detecting outliers and anomalies
 */

const AnomalyDetectionNodes = {
    isolationForest: {
        type: 'algo_isolation_forest',
        name: 'Isolation Forest',
        category: 'algorithms-anomaly',
        icon: 'bi-exclamation-triangle',
        color: '#ff5722',
        description: 'Isolation Forest for anomaly detection',
        defaults: {
            n_estimators: 100,
            contamination: 0.1,
            max_samples: 'auto',
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_estimators', 'N Estimators', 'number', {
                default: 100,
                min: 1,
                max: 1000,
                help: 'Number of base estimators'
            }),
            BaseNode.createProperty('contamination', 'Contamination', 'number', {
                default: 0.1,
                min: 0,
                max: 0.5,
                step: 0.01,
                help: 'Expected proportion of outliers'
            }),
            BaseNode.createProperty('max_samples', 'Max Samples', 'text', {
                default: 'auto',
                placeholder: 'auto, 256, or 0.5',
                help: 'Number of samples to draw for each estimator'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.n_estimators) params.push(`n_estimators=${data.n_estimators}`);
            if (data.contamination !== undefined) params.push(`contamination=${data.contamination}`);
            if (data.max_samples) {
                if (data.max_samples === 'auto' || isNaN(data.max_samples)) {
                    params.push(`max_samples='${data.max_samples}'`);
                } else {
                    params.push(`max_samples=${data.max_samples}`);
                }
            }
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# Isolation Forest Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: if_model, predictions, anomaly_scores\n\n`;
            code += `from sklearn.ensemble import IsolationForest\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `# Ensure only numeric data is used\n`;
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_anomaly = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_anomaly = ${inputVar}\n\n`;
            
            code += `if_model = IsolationForest(${paramStr})\n`;
            code += `predictions = if_model.fit_predict(X_anomaly)\n`;
            code += `anomaly_scores = if_model.score_samples(X_anomaly)\n`;
            code += `# -1 for anomalies, 1 for normal\n`;
            code += `n_anomalies = (predictions == -1).sum()\n`;
            code += `print(f'Detected {n_anomalies} anomalies ({n_anomalies/len(X_anomaly):.2%})')\n`;
            
            context.setVariable(node.id, 'if_model');
            return code;
        }
    },

    oneClassSVM: {
        type: 'algo_one_class_svm',
        name: 'One-Class SVM',
        category: 'algorithms-anomaly',
        icon: 'bi-shield-exclamation',
        color: '#e91e63',
        description: 'One-Class Support Vector Machine',
        defaults: {
            nu: 0.5,
            kernel: 'rbf',
            gamma: 'scale'
        },
        properties: [
            BaseNode.createProperty('nu', 'Nu', 'number', {
                default: 0.5,
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Upper bound on fraction of outliers'
            }),
            BaseNode.createProperty('kernel', 'Kernel', 'select', {
                default: 'rbf',
                options: ['linear', 'poly', 'rbf', 'sigmoid']
            }),
            BaseNode.createProperty('gamma', 'Gamma', 'select', {
                default: 'scale',
                options: ['scale', 'auto'],
                help: 'Kernel coefficient'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.nu !== undefined) params.push(`nu=${data.nu}`);
            if (data.kernel) params.push(`kernel='${data.kernel}'`);
            if (data.gamma) params.push(`gamma='${data.gamma}'`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# One-Class SVM Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: ocsvm_model, predictions\n\n`;
            code += `from sklearn.svm import OneClassSVM\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `# Ensure only numeric data is used\n`;
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_anomaly = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_anomaly = ${inputVar}\n\n`;
            
            code += `ocsvm_model = OneClassSVM(${paramStr})\n`;
            code += `predictions = ocsvm_model.fit_predict(X_anomaly)\n`;
            code += `n_anomalies = (predictions == -1).sum()\n`;
            code += `print(f'Detected {n_anomalies} anomalies ({n_anomalies/len(X_anomaly):.2%})')\n`;
            
            context.setVariable(node.id, 'ocsvm_model');
            return code;
        }
    },

    localOutlierFactor: {
        type: 'algo_local_outlier_factor',
        name: 'Local Outlier Factor',
        category: 'algorithms-anomaly',
        icon: 'bi-exclamation-circle',
        color: '#9c27b0',
        description: 'Local Outlier Factor (LOF)',
        defaults: {
            n_neighbors: 20,
            contamination: 0.1,
            algorithm: 'auto',
            metric: 'minkowski'
        },
        properties: [
            BaseNode.createProperty('n_neighbors', 'N Neighbors', 'number', {
                default: 20,
                min: 2,
                max: 100,
                help: 'Number of neighbors to use'
            }),
            BaseNode.createProperty('contamination', 'Contamination', 'number', {
                default: 0.1,
                min: 0,
                max: 0.5,
                step: 0.01,
                help: 'Expected proportion of outliers'
            }),
            BaseNode.createProperty('algorithm', 'Algorithm', 'select', {
                default: 'auto',
                options: ['auto', 'ball_tree', 'kd_tree', 'brute']
            }),
            BaseNode.createProperty('metric', 'Metric', 'select', {
                default: 'minkowski',
                options: ['minkowski', 'euclidean', 'manhattan', 'cosine']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.n_neighbors) params.push(`n_neighbors=${data.n_neighbors}`);
            if (data.contamination !== undefined) params.push(`contamination=${data.contamination}`);
            if (data.algorithm) params.push(`algorithm='${data.algorithm}'`);
            if (data.metric) params.push(`metric='${data.metric}'`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# Local Outlier Factor Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: lof_model, predictions\n\n`;
            code += `from sklearn.neighbors import LocalOutlierFactor\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_lof = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_lof = ${inputVar}\n\n`;
            
            code += `lof_model = LocalOutlierFactor(${paramStr})\n`;
            code += `predictions = lof_model.fit_predict(X_lof)\n`;
            code += `n_anomalies = (predictions == -1).sum()\n`;
            code += `print(f'LOF: {n_anomalies} anomalies ({n_anomalies/len(X_lof):.2%})')\n`;
            
            context.setVariable(node.id, 'lof_model');
            return code;
        }
    },

    ellipticEnvelope: {
        type: 'algo_elliptic_envelope',
        name: 'Elliptic Envelope',
        category: 'algorithms-anomaly',
        icon: 'bi-shield-slash',
        color: '#607d8b',
        description: 'Elliptic Envelope for Gaussian-distributed data',
        defaults: {
            contamination: 0.1,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('contamination', 'Contamination', 'number', {
                default: 0.1,
                min: 0,
                max: 0.5,
                step: 0.01,
                help: 'Expected proportion of outliers'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.contamination !== undefined) params.push(`contamination=${data.contamination}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# Elliptic Envelope Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: ee_model, predictions\n\n`;
            code += `from sklearn.covariance import EllipticEnvelope\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_ee = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_ee = ${inputVar}\n\n`;
            
            code += `ee_model = EllipticEnvelope(${paramStr})\n`;
            code += `predictions = ee_model.fit_predict(X_ee)\n`;
            code += `n_anomalies = (predictions == -1).sum()\n`;
            code += `print(f'Elliptic Envelope: {n_anomalies} anomalies ({n_anomalies/len(X_ee):.2%})')\n`;
            
            context.setVariable(node.id, 'ee_model');
            return code;
        }
    }
};

// Register anomaly detection nodes
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(AnomalyDetectionNodes, 'anomaly detection');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(AnomalyDetectionNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register anomaly detection node ${nodeDef.type}:`, error);
        }
    });
    console.log('Registered anomaly detection algorithm nodes');
} else {
    console.warn('Dependencies not ready for algorithms_anomaly.js');
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AnomalyDetectionNodes;
}

