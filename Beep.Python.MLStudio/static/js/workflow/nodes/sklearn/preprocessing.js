/**
 * Scikit-learn Preprocessing Nodes
 * Feature scaling, normalization, and transformation
 */

const SklearnPreprocessingNodes = {
    standardScaler: {
        type: 'sklearn_standard_scaler',
        name: 'Standard Scaler',
        category: 'sklearn-preprocessing',
        icon: 'bi-arrow-left-right',
        color: '#2e7d32',
        description: 'Standardize features by removing mean and scaling to unit variance',
        defaults: {
            with_mean: true,
            with_std: true,
            copy: true
        },
        properties: [
            BaseNode.createProperty('with_mean', 'Center Data', 'boolean', {
                default: true,
                help: 'If True, center the data before scaling'
            }),
            BaseNode.createProperty('with_std', 'Scale to Unit Variance', 'boolean', {
                default: true,
                help: 'If True, scale the data to unit variance'
            }),
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true,
                help: 'If False, try to avoid a copy and do inplace scaling'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const withMean = data.with_mean !== false;
            const withStd = data.with_std !== false;
            
            // Use unique scaler name per node to avoid conflicts
            const scalerName = `scaler_${node.id.replace(/[^a-zA-Z0-9]/g, '_')}`;
            
            let code = `# StandardScaler Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: X_train_scaled, X_test_scaled (or ${inputVar}_scaled)\n\n`;
            code += 'from sklearn.preprocessing import StandardScaler\n';
            code += 'import pandas as pd\n';
            code += 'import numpy as np\n\n';
            code += `${scalerName} = StandardScaler(with_mean=${withMean}, with_std=${withStd})\n\n`;
            
            if (inputVar === 'X_train' || inputVar.includes('train')) {
                // Training data - fit on train, transform both train and test
                code += `# Select only numeric columns for scaling\n`;
                code += `numeric_cols = X_train.select_dtypes(include=[np.number]).columns\n`;
                code += `X_train_numeric = X_train[numeric_cols]\n`;
                code += `X_test_numeric = X_test[numeric_cols]\n\n`;
                
                code += `# Fit on training data, transform both\n`;
                code += `X_train_scaled_numeric = ${scalerName}.fit_transform(X_train_numeric)\n`;
                code += `X_test_scaled_numeric = ${scalerName}.transform(X_test_numeric)\n\n`;
                
                code += `# Reconstruct DataFrames\n`;
                code += `X_train_scaled = X_train.copy()\n`;
                code += `X_test_scaled = X_test.copy()\n`;
                code += `X_train_scaled[numeric_cols] = X_train_scaled_numeric\n`;
                code += `X_test_scaled[numeric_cols] = X_test_scaled_numeric\n\n`;
                code += `print(f'StandardScaler: {X_train.shape} (numeric: {len(numeric_cols)} cols)')\n`;
                
                context.setVariable(node.id, 'X_train_scaled');
            } else {
                // Regular scaling
                const outputVar = inputVar + '_scaled';
                code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
                code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns\n`;
                code += `    ${outputVar} = ${inputVar}.copy()\n`;
                code += `    ${outputVar}[numeric_cols] = ${scalerName}.fit_transform(${inputVar}[numeric_cols])\n`;
                code += `else:\n`;
                code += `    ${outputVar} = ${scalerName}.fit_transform(${inputVar})\n`;
                code += `print(f'StandardScaler: {${inputVar}.shape}')\n`;
                
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    minMaxScaler: {
        type: 'sklearn_minmax_scaler',
        name: 'MinMax Scaler',
        category: 'sklearn-preprocessing',
        icon: 'bi-arrows-fullscreen',
        color: '#1976d2',
        description: 'Transform features by scaling each feature to a given range',
        defaults: {
            feature_range: [0, 1],
            copy: true
        },
        properties: [
            BaseNode.createProperty('min_value', 'Min Value', 'number', {
                default: 0,
                help: 'Minimum value of transformed feature'
            }),
            BaseNode.createProperty('max_value', 'Max Value', 'number', {
                default: 1,
                help: 'Maximum value of transformed feature'
            }),
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const minVal = data.min_value !== undefined ? data.min_value : 0;
            const maxVal = data.max_value !== undefined ? data.max_value : 1;
            
            const scalerName = `scaler_${node.id.replace(/[^a-zA-Z0-9]/g, '_')}`;
            
            let code = `# MinMaxScaler Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: X_train_scaled, X_test_scaled (or ${inputVar}_scaled)\n\n`;
            code += 'from sklearn.preprocessing import MinMaxScaler\n';
            code += 'import pandas as pd\n';
            code += 'import numpy as np\n\n';
            code += `${scalerName} = MinMaxScaler(feature_range=(${minVal}, ${maxVal}))\n\n`;
            
            if (inputVar === 'X_train' || inputVar.includes('train')) {
                code += `# Select only numeric columns for scaling\n`;
                code += `numeric_cols = X_train.select_dtypes(include=[np.number]).columns\n`;
                code += `X_train_numeric = X_train[numeric_cols]\n`;
                code += `X_test_numeric = X_test[numeric_cols]\n\n`;
                
                code += `# Fit on training data, transform both\n`;
                code += `X_train_scaled_numeric = ${scalerName}.fit_transform(X_train_numeric)\n`;
                code += `X_test_scaled_numeric = ${scalerName}.transform(X_test_numeric)\n\n`;
                
                code += `# Reconstruct DataFrames\n`;
                code += `X_train_scaled = X_train.copy()\n`;
                code += `X_test_scaled = X_test.copy()\n`;
                code += `X_train_scaled[numeric_cols] = X_train_scaled_numeric\n`;
                code += `X_test_scaled[numeric_cols] = X_test_scaled_numeric\n\n`;
                code += `print(f'MinMaxScaler: {X_train.shape} (numeric: {len(numeric_cols)} cols)')\n`;
                
                context.setVariable(node.id, 'X_train_scaled');
            } else {
                const outputVar = inputVar + '_scaled';
                code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
                code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns\n`;
                code += `    ${outputVar} = ${inputVar}.copy()\n`;
                code += `    ${outputVar}[numeric_cols] = ${scalerName}.fit_transform(${inputVar}[numeric_cols])\n`;
                code += `else:\n`;
                code += `    ${outputVar} = ${scalerName}.fit_transform(${inputVar})\n`;
                code += `print(f'MinMaxScaler: {${inputVar}.shape}')\n`;
                
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    robustScaler: {
        type: 'sklearn_robust_scaler',
        name: 'Robust Scaler',
        category: 'sklearn-preprocessing',
        icon: 'bi-shield-check',
        color: '#e65100',
        description: 'Scale features using statistics that are robust to outliers',
        defaults: {
            with_centering: true,
            with_scaling: true,
            quantile_range: [0.25, 0.75],
            copy: true
        },
        properties: [
            BaseNode.createProperty('with_centering', 'Center Data', 'boolean', {
                default: true,
                help: 'If True, center the data before scaling'
            }),
            BaseNode.createProperty('with_scaling', 'Scale Data', 'boolean', {
                default: true,
                help: 'If True, scale the data to interquartile range'
            }),
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const withCentering = data.with_centering !== false;
            const withScaling = data.with_scaling !== false;
            
            const scalerName = `scaler_${node.id.replace(/[^a-zA-Z0-9]/g, '_')}`;
            
            let code = `# RobustScaler Node (${node.id})\n`;
            code += 'from sklearn.preprocessing import RobustScaler\n';
            code += 'import pandas as pd\n';
            code += 'import numpy as np\n\n';
            code += `${scalerName} = RobustScaler(with_centering=${withCentering}, with_scaling=${withScaling})\n\n`;
            
            if (inputVar === 'X_train' || inputVar.includes('train')) {
                code += `numeric_cols = X_train.select_dtypes(include=[np.number]).columns\n`;
                code += `X_train_scaled = X_train.copy()\n`;
                code += `X_test_scaled = X_test.copy()\n`;
                code += `X_train_scaled[numeric_cols] = ${scalerName}.fit_transform(X_train[numeric_cols])\n`;
                code += `X_test_scaled[numeric_cols] = ${scalerName}.transform(X_test[numeric_cols])\n`;
                code += `print(f'RobustScaler: {X_train.shape}')\n`;
                
                context.setVariable(node.id, 'X_train_scaled');
            } else {
                const outputVar = inputVar + '_scaled';
                code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
                code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns\n`;
                code += `    ${outputVar} = ${inputVar}.copy()\n`;
                code += `    ${outputVar}[numeric_cols] = ${scalerName}.fit_transform(${inputVar}[numeric_cols])\n`;
                code += `else:\n`;
                code += `    ${outputVar} = ${scalerName}.fit_transform(${inputVar})\n`;
                code += `print(f'RobustScaler: {${inputVar}.shape}')\n`;
                
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    maxAbsScaler: {
        type: 'sklearn_maxabs_scaler',
        name: 'MaxAbs Scaler',
        category: 'sklearn-preprocessing',
        icon: 'bi-arrow-up',
        color: '#7b1fa2',
        description: 'Scale each feature by its maximum absolute value',
        defaults: {
            copy: true
        },
        properties: [
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            
            const scalerName = `scaler_${node.id.replace(/[^a-zA-Z0-9]/g, '_')}`;
            
            let code = `# MaxAbsScaler Node (${node.id})\n`;
            code += 'from sklearn.preprocessing import MaxAbsScaler\n';
            code += 'import pandas as pd\n';
            code += 'import numpy as np\n\n';
            code += `${scalerName} = MaxAbsScaler()\n\n`;
            
            if (inputVar === 'X_train' || inputVar.includes('train')) {
                code += `numeric_cols = X_train.select_dtypes(include=[np.number]).columns\n`;
                code += `X_train_scaled = X_train.copy()\n`;
                code += `X_test_scaled = X_test.copy()\n`;
                code += `X_train_scaled[numeric_cols] = ${scalerName}.fit_transform(X_train[numeric_cols])\n`;
                code += `X_test_scaled[numeric_cols] = ${scalerName}.transform(X_test[numeric_cols])\n`;
                code += `print(f'MaxAbsScaler: {X_train.shape}')\n`;
                
                context.setVariable(node.id, 'X_train_scaled');
            } else {
                const outputVar = inputVar + '_scaled';
                code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
                code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns\n`;
                code += `    ${outputVar} = ${inputVar}.copy()\n`;
                code += `    ${outputVar}[numeric_cols] = ${scalerName}.fit_transform(${inputVar}[numeric_cols])\n`;
                code += `else:\n`;
                code += `    ${outputVar} = ${scalerName}.fit_transform(${inputVar})\n`;
                code += `print(f'MaxAbsScaler: {${inputVar}.shape}')\n`;
                
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    normalizer: {
        type: 'sklearn_normalizer',
        name: 'Normalizer',
        category: 'sklearn-preprocessing',
        icon: 'bi-arrow-down-up',
        color: '#c2185b',
        description: 'Normalize samples individually to unit norm',
        defaults: {
            norm: 'l2',
            copy: true
        },
        properties: [
            BaseNode.createProperty('norm', 'Norm', 'select', {
                default: 'l2',
                options: ['l1', 'l2', 'max'],
                help: 'The norm to use to normalize each non zero sample'
            }),
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const outputVar = inputVar + '_normalized';
            const norm = data.norm || 'l2';
            const copy = data.copy !== false;
            
            let code = 'from sklearn.preprocessing import Normalizer\n';
            code += `normalizer = Normalizer(norm='${norm}', copy=${copy})\n`;
            code += `${outputVar} = normalizer.transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    powerTransformer: {
        type: 'sklearn_power_transformer',
        name: 'Power Transformer',
        category: 'sklearn-preprocessing',
        icon: 'bi-lightning',
        color: '#ff6b6b',
        description: 'Apply a power transform featurewise to make data more Gaussian-like',
        defaults: {
            method: 'yeo-johnson',
            standardize: true,
            copy: true
        },
        properties: [
            BaseNode.createProperty('method', 'Method', 'select', {
                default: 'yeo-johnson',
                options: ['yeo-johnson', 'box-cox'],
                help: 'The power transform method'
            }),
            BaseNode.createProperty('standardize', 'Standardize', 'boolean', {
                default: true,
                help: 'Apply zero mean, unit variance normalization'
            }),
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_transformed';
            const method = data.method || 'yeo-johnson';
            const standardize = data.standardize !== false;
            const copy = data.copy !== false;
            
            let code = 'from sklearn.preprocessing import PowerTransformer\n';
            code += `transformer = PowerTransformer(method='${method}', standardize=${standardize}, copy=${copy})\n`;
            code += `${outputVar} = transformer.fit_transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    quantileTransformer: {
        type: 'sklearn_quantile_transformer',
        name: 'Quantile Transformer',
        category: 'sklearn-preprocessing',
        icon: 'bi-graph-up',
        color: '#4ecdc4',
        description: 'Transform features using quantiles information',
        defaults: {
            n_quantiles: 1000,
            output_distribution: 'uniform',
            copy: true
        },
        properties: [
            BaseNode.createProperty('n_quantiles', 'N Quantiles', 'number', {
                default: 1000,
                min: 10,
                max: 10000,
                help: 'Number of quantiles to be computed'
            }),
            BaseNode.createProperty('output_distribution', 'Output Distribution', 'select', {
                default: 'uniform',
                options: ['uniform', 'normal'],
                help: 'Marginal distribution for the transformed data'
            }),
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_transformed';
            const nQuantiles = data.n_quantiles || 1000;
            const outputDist = data.output_distribution || 'uniform';
            const copy = data.copy !== false;
            
            let code = 'from sklearn.preprocessing import QuantileTransformer\n';
            code += `transformer = QuantileTransformer(n_quantiles=${nQuantiles}, output_distribution='${outputDist}', copy=${copy})\n`;
            code += `${outputVar} = transformer.fit_transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all sklearn preprocessing nodes
Object.values(SklearnPreprocessingNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SklearnPreprocessingNodes;
}

