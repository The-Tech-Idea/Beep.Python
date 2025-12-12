/**
 * Model Monitoring Nodes
 * Drift detection and performance tracking
 */

const ModelMonitoringNodes = {
    detectDrift: {
        type: 'monitoring_drift_detection',
        name: 'Detect Data Drift',
        category: 'model-monitoring',
        icon: 'bi-exclamation-triangle',
        color: '#ffc107',
        description: 'Detect distribution drift in data',
        defaults: {
            reference_data: 'X_train',
            current_data: 'X_test',
            method: 'ks_test'
        },
        properties: [
            BaseNode.createProperty('reference_data', 'Reference Data', 'text', {
                required: true,
                default: 'X_train',
                help: 'Variable name of reference/training data'
            }),
            BaseNode.createProperty('current_data', 'Current Data', 'text', {
                required: true,
                default: 'X_test',
                help: 'Variable name of current/production data'
            }),
            BaseNode.createProperty('method', 'Drift Detection Method', 'select', {
                default: 'ks_test',
                options: ['ks_test', 'psi', 'chi_square'],
                help: 'Statistical test for drift detection'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const refData = data.reference_data || 'X_train';
            const currData = data.current_data || 'X_test';
            const method = data.method || 'ks_test';
            
            let code = 'from scipy import stats\n';
            code += 'import numpy as np\n';
            code += `\n# Drift detection using ${method}\n`;
            
            if (method === 'ks_test') {
                code += `drift_results = {}\n`;
                code += `for col in ${refData}.columns:\n`;
                code += `    if ${refData}[col].dtype in ['float64', 'int64']:\n`;
                code += `        statistic, p_value = stats.ks_2samp(${refData}[col], ${currData}[col])\n`;
                code += `        drift_results[col] = {'statistic': statistic, 'p_value': p_value, 'drift': p_value < 0.05}\n`;
                code += `        if p_value < 0.05:\n`;
                code += `            print(f'⚠ Drift detected in {col} (p={p_value:.4f})')\n`;
            } else if (method === 'psi') {
                code += `# Population Stability Index (PSI)\n`;
                code += `def calculate_psi(expected, actual, bins=10):\n`;
                code += `    breakpoints = np.linspace(expected.min(), expected.max(), bins+1)\n`;
                code += `    expected_percents = np.histogram(expected, breakpoints)[0] / len(expected)\n`;
                code += `    actual_percents = np.histogram(actual, breakpoints)[0] / len(actual)\n`;
                code += `    psi = np.sum((actual_percents - expected_percents) * np.log(actual_percents / expected_percents))\n`;
                code += `    return psi\n`;
                code += `\n`;
                code += `drift_results = {}\n`;
                code += `for col in ${refData}.columns:\n`;
                code += `    if ${refData}[col].dtype in ['float64', 'int64']:\n`;
                code += `        psi = calculate_psi(${refData}[col], ${currData}[col])\n`;
                code += `        drift_results[col] = {'psi': psi, 'drift': psi > 0.2}\n`;
                code += `        if psi > 0.2:\n`;
                code += `            print(f'⚠ Drift detected in {col} (PSI={psi:.4f})')\n`;
            }
            
            code += `print('Drift detection complete')\n`;
            
            return code;
        }
    },

    trackPerformance: {
        type: 'monitoring_track_performance',
        name: 'Track Model Performance',
        category: 'model-monitoring',
        icon: 'bi-speedometer2',
        color: '#17a2b8',
        description: 'Track and log model performance metrics',
        defaults: {
            metrics: 'accuracy,precision,recall',
            log_file: 'performance_log.csv'
        },
        properties: [
            BaseNode.createProperty('metrics', 'Metrics to Track', 'text', {
                default: 'accuracy,precision,recall',
                placeholder: 'accuracy,precision,recall,f1',
                help: 'Comma-separated metrics'
            }),
            BaseNode.createProperty('log_file', 'Log File', 'text', {
                default: 'performance_log.csv',
                placeholder: 'performance_log.csv'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const metrics = data.metrics || 'accuracy,precision,recall';
            const logFile = data.log_file || 'performance_log.csv';
            
            let code = 'from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score\n';
            code += 'import pandas as pd\n';
            code += 'from datetime import datetime\n';
            code += `\n# Calculate metrics\n`;
            code += `y_pred = model.predict(X_test)\n`;
            code += `\nmetrics_dict = {'timestamp': datetime.now()}\n`;
            
            const metricList = metrics.split(',').map(m => m.trim());
            metricList.forEach(metric => {
                if (metric === 'accuracy') {
                    code += `metrics_dict['accuracy'] = accuracy_score(y_test, y_pred)\n`;
                } else if (metric === 'precision') {
                    code += `metrics_dict['precision'] = precision_score(y_test, y_pred, average='weighted')\n`;
                } else if (metric === 'recall') {
                    code += `metrics_dict['recall'] = recall_score(y_test, y_pred, average='weighted')\n`;
                } else if (metric === 'f1') {
                    code += `metrics_dict['f1'] = f1_score(y_test, y_pred, average='weighted')\n`;
                }
            });
            
            code += `\n# Log to file\n`;
            code += `try:\n`;
            code += `    log_df = pd.read_csv('${logFile}')\n`;
            code += `    log_df = pd.concat([log_df, pd.DataFrame([metrics_dict])], ignore_index=True)\n`;
            code += `except FileNotFoundError:\n`;
            code += `    log_df = pd.DataFrame([metrics_dict])\n`;
            code += `\n`;
            code += `log_df.to_csv('${logFile}', index=False)\n`;
            code += `print(f'Performance logged to {logFile}')\n`;
            code += `print(f'Latest metrics: {metrics_dict}')\n`;
            
            return code;
        }
    }
};

// Register all monitoring nodes
Object.values(ModelMonitoringNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ModelMonitoringNodes;
}

