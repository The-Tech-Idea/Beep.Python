/**
 * Preprocessing Nodes
 * Data preprocessing and transformation nodes
 */

const PreprocessingNodes = {
    // Select Features and Target (Essential for ML pipelines)
    selectFeaturesTarget: {
        type: 'preprocess_select_features_target',
        name: 'Select Features & Target',
        category: 'preprocessing',
        icon: 'bi-columns-gap',
        color: '#6a1b9a',
        description: 'Select feature columns (X) and target column (y)',
        ports: {
            inputs: [{ name: 'input', label: 'Data' }],
            outputs: [
                { name: 'features', label: 'X (Features)' },
                { name: 'target', label: 'y (Target)' }
            ]
        },
        defaults: {
            target_column: '',
            feature_columns: '',
            drop_target_from_features: true
        },
        // Custom property renderer for column selection
        hasCustomProperties: true,
        renderCustomProperties: (nodeId, nodeData) => {
            // Escape values to prevent HTML injection
            const escapeHtml = (str) => {
                if (!str) return '';
                return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
            };
            
            const targetCol = escapeHtml(nodeData.target_column || '');
            const featureCols = escapeHtml(nodeData.feature_columns || '');
            const dropTarget = nodeData.drop_target_from_features !== false;
            const safeNodeId = escapeHtml(nodeId);
            
            return `
                <div class="mb-3">
                    <button class="btn btn-primary btn-sm w-100 mb-3" onclick="openFeaturesTargetSelector('${safeNodeId}')">
                        <i class="bi bi-columns-gap"></i> Select Columns from Data
                    </button>
                </div>
                <div class="mb-3">
                    <label class="form-label small">Target Column (y)</label>
                    <div class="input-group input-group-sm">
                        <input type="text" class="form-control" id="prop_target_column_${safeNodeId}" 
                               value="${targetCol}" placeholder="e.g., label, class, target"
                               oninput="window.workflowBuilder && window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'target_column', this.value)"
                               onchange="window.workflowBuilder && window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'target_column', this.value)">
                        <button class="btn btn-outline-secondary" type="button" title="Select from data columns"
                                onclick="if(typeof showColumnSelectorModal === 'function') { showColumnSelectorModal('${safeNodeId}', 'target_column', false, function(val) { var el = document.getElementById('prop_target_column_${safeNodeId}'); if(el) { el.value = val; } if(window.workflowBuilder) { window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'target_column', val); } }); }">
                            <i class="bi bi-list-columns"></i>
                        </button>
                    </div>
                    <small class="form-text text-muted">Column to predict (the label)</small>
                </div>
                <div class="mb-3">
                    <label class="form-label small">Feature Columns (X)</label>
                    <div class="input-group input-group-sm">
                        <input type="text" class="form-control" id="prop_feature_columns_${safeNodeId}" 
                               value="${featureCols}" placeholder="Leave empty for all except target"
                               oninput="window.workflowBuilder && window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'feature_columns', this.value)"
                               onchange="window.workflowBuilder && window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'feature_columns', this.value)">
                        <button class="btn btn-outline-secondary" type="button" title="Select from data columns"
                                onclick="if(typeof showColumnSelectorModal === 'function') { showColumnSelectorModal('${safeNodeId}', 'feature_columns', true, function(val) { var el = document.getElementById('prop_feature_columns_${safeNodeId}'); if(el) { el.value = val; } if(window.workflowBuilder) { window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'feature_columns', val); } }); }">
                            <i class="bi bi-list-columns"></i>
                        </button>
                    </div>
                    <small class="form-text text-muted">Comma-separated. Empty = all columns except target.</small>
                </div>
                <div class="mb-3">
                    <div class="form-check">
                        <input class="form-check-input" type="checkbox" id="prop_drop_target_${safeNodeId}" 
                               ${dropTarget ? 'checked' : ''}
                               onchange="window.workflowBuilder && window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'drop_target_from_features', this.checked)">
                        <label class="form-check-label small" for="prop_drop_target_${safeNodeId}">
                            Auto-exclude Target from Features
                        </label>
                    </div>
                    <small class="form-text text-muted">Automatically remove target column from features</small>
                </div>
            `;
        },
        properties: [
            BaseNode.createProperty('target_column', 'Target Column (y)', 'column_select', {
                required: true,
                default: '',
                placeholder: 'Click to select from data columns',
                help: 'Name of the column to predict (the label/target)'
            }),
            BaseNode.createProperty('feature_columns', 'Feature Columns (X)', 'columns', {
                placeholder: 'Leave empty for all columns except target',
                help: 'Comma-separated list of feature columns. Leave empty to use all columns except target.',
                multiple: true
            }),
            BaseNode.createProperty('drop_target_from_features', 'Auto-exclude Target from Features', 'boolean', {
                default: true,
                help: 'Automatically remove target column from features'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            // Get input from previous node via connection
            const inputVar = context.getInputVariable(node) || 'df';
            const targetCol = data.target_column || 'target';
            const featureCols = data.feature_columns || '';
            const dropTarget = data.drop_target_from_features !== false;
            
            // This node's output variable names
            const featuresVar = 'X';
            const targetVar = 'y';
            
            let code = `# Select Features & Target Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${featuresVar} (features), ${targetVar} (target)\n\n`;
            
            code += `${targetVar} = ${inputVar}['${targetCol}'].copy()\n`;
            
            if (featureCols.trim()) {
                // Specific columns selected
                const cols = featureCols.split(',').map(c => `'${c.trim()}'`).join(', ');
                code += `${featuresVar} = ${inputVar}[[${cols}]].copy()\n`;
            } else {
                // All columns except target
                if (dropTarget) {
                    code += `${featuresVar} = ${inputVar}.drop(columns=['${targetCol}']).copy()\n`;
                } else {
                    code += `${featuresVar} = ${inputVar}.copy()\n`;
                }
            }
            code += `print(f'Features ({featuresVar}): {${featuresVar}.shape}')\n`;
            code += `print(f'Target ({targetVar}): {${targetVar}.shape}')\n`;
            code += `print(f'Feature columns: {list(${featuresVar}.columns)}')\n`;
            
            // Register outputs in context for downstream nodes
            context.setVariable(node.id + '_features', featuresVar);
            context.setVariable(node.id + '_target', targetVar);
            context.setVariable(node.id, featuresVar);  // Default output is features
            
            return code;
        }
    },

    // Simple Target Selector (for quick use)
    selectTarget: {
        type: 'preprocess_select_target',
        name: 'Select Target Column',
        category: 'preprocessing',
        icon: 'bi-bullseye',
        color: '#d32f2f',
        description: 'Select the target/label column for prediction',
        defaults: {
            target_column: ''
        },
        hasCustomProperties: true,
        renderCustomProperties: (nodeId, nodeData) => {
            const escapeHtml = (str) => {
                if (!str) return '';
                return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
            };
            
            const targetCol = escapeHtml(nodeData.target_column || '');
            const safeNodeId = escapeHtml(nodeId);
            
            return `
                <div class="mb-3">
                    <label class="form-label small">Target Column (y)</label>
                    <div class="input-group input-group-sm">
                        <input type="text" class="form-control" id="prop_target_column_${safeNodeId}" 
                               value="${targetCol}" placeholder="Enter or select column..."
                               oninput="window.workflowBuilder && window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'target_column', this.value)"
                               onchange="window.workflowBuilder && window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'target_column', this.value)">
                        <button class="btn btn-primary" type="button" title="Select from data columns"
                                onclick="if(typeof showColumnSelectorModal === 'function') { showColumnSelectorModal('${safeNodeId}', 'target_column', false, function(val) { var el = document.getElementById('prop_target_column_${safeNodeId}'); if(el) { el.value = val; } if(window.workflowBuilder) { window.workflowBuilder.updateNodeProperty('${safeNodeId}', 'target_column', val); } }); }">
                            <i class="bi bi-list-columns"></i> Select
                        </button>
                    </div>
                    <small class="form-text text-muted">The column you want to predict</small>
                </div>
            `;
        },
        properties: [
            BaseNode.createProperty('target_column', 'Target Column', 'column_select', {
                required: true,
                default: '',
                placeholder: 'Click to select from data columns',
                help: 'Name of the column to predict'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            const targetCol = data.target_column || 'target';
            
            let code = `# Extract target column\n`;
            code += `y = ${inputVar}['${targetCol}'].copy()\n`;
            code += `X = ${inputVar}.drop(columns=['${targetCol}']).copy()\n`;
            code += `print(f'Target: ${targetCol}, Features: {X.shape[1]} columns')\n`;
            
            context.setVariable(node.id, 'X');
            
            return code;
        }
    },

    // Drop Columns Node
    dropColumns: {
        type: 'preprocess_drop_columns',
        name: 'Drop Columns',
        category: 'preprocessing',
        icon: 'bi-trash',
        color: '#f44336',
        description: 'Remove specified columns from dataset',
        defaults: {
            columns: ''
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns to Drop', 'text', {
                required: true,
                placeholder: 'col1, col2, col3',
                help: 'Comma-separated list of column names to drop'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            const columns = data.columns || '';
            
            if (!columns.trim()) {
                return `# Drop Columns: No columns specified\n${inputVar}_clean = ${inputVar}.copy()`;
            }
            
            const colList = columns.split(',').map(c => `'${c.trim()}'`).join(', ');
            return `# Drop specified columns\n${inputVar}_clean = ${inputVar}.drop(columns=[${colList}], errors='ignore')\nprint(f'Dropped columns. Remaining: {${inputVar}_clean.shape[1]} columns')`;
        }
    },

    trainTestSplit: {
        type: 'preprocess_split',
        name: 'Train/Test Split',
        category: 'preprocessing',
        icon: 'bi-scissors',
        color: '#e65100',
        description: 'Split data into train/test sets',
        ports: {
            inputs: [
                { name: 'features', label: 'X (Features)' },
                { name: 'target', label: 'y (Target)' }
            ],
            outputs: [
                { name: 'output', label: 'Split Data' }
            ]
        },
        defaults: {
            test_size: 0.2,
            random_state: 42,
            shuffle: true,
            stratify: null
        },
        properties: [
            BaseNode.createProperty('test_size', 'Test Size', 'number', {
                default: 0.2,
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Proportion of dataset to include in test split'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42,
                help: 'Random seed for reproducibility'
            }),
            BaseNode.createProperty('shuffle', 'Shuffle', 'boolean', {
                default: true,
                help: 'Whether to shuffle before splitting'
            }),
            BaseNode.createProperty('stratify', 'Stratify', 'text', {
                placeholder: 'y or leave empty',
                help: 'Column to stratify by (for classification)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            
            // Get input variables from connected nodes via context
            const inputs = context.getInputVariables ? context.getInputVariables(node) : {};
            const xVar = inputs.features || inputs.input || context.getInputVariable(node) || 'X';
            const yVar = inputs.target || 'y';
            
            const testSize = data.test_size || 0.2;
            const randomState = data.random_state || 42;
            const shuffle = data.shuffle !== false;
            const stratify = data.stratify || null;
            
            // Output variable names (standard ML convention)
            const xTrainVar = 'X_train';
            const xTestVar = 'X_test';
            const yTrainVar = 'y_train';
            const yTestVar = 'y_test';
            
            let code = `# Train/Test Split Node (${node.id})\n`;
            code += `# Input: ${xVar} (features), ${yVar} (target)\n`;
            code += `# Output: ${xTrainVar}, ${xTestVar}, ${yTrainVar}, ${yTestVar}\n\n`;
            code += `from sklearn.model_selection import train_test_split\n`;
            
            const stratifyParam = stratify ? `, stratify=${stratify}` : '';
            code += `${xTrainVar}, ${xTestVar}, ${yTrainVar}, ${yTestVar} = train_test_split(${xVar}, ${yVar}, test_size=${testSize}, random_state=${randomState}, shuffle=${shuffle}${stratifyParam})\n`;
            code += `print(f'Training: {${xTrainVar}.shape}, Test: {${xTestVar}.shape}')\n`;
            
            // Register outputs in context for downstream nodes
            context.setVariable(node.id, xTrainVar);  // Default output is X_train
            context.setVariable(node.id + '_X_train', xTrainVar);
            context.setVariable(node.id + '_X_test', xTestVar);
            context.setVariable(node.id + '_y_train', yTrainVar);
            context.setVariable(node.id + '_y_test', yTestVar);
            
            return code;
        }
    },

    standardScaler: {
        type: 'preprocess_scale',
        name: 'Standard Scaler',
        category: 'preprocessing',
        icon: 'bi-arrow-left-right',
        color: '#2e7d32',
        description: 'Standardize features (mean=0, std=1)',
        imports: 'from sklearn.preprocessing import StandardScaler',
        defaults: {
            with_mean: true,
            with_std: true
        },
        properties: [
            BaseNode.createProperty('with_mean', 'With Mean', 'boolean', {
                default: true,
                help: 'Center data before scaling'
            }),
            BaseNode.createProperty('with_std', 'With Std', 'boolean', {
                default: true,
                help: 'Scale to unit variance'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            const withMean = data.with_mean !== false;
            const withStd = data.with_std !== false;
            
            // Use unique scaler name per node to avoid conflicts
            const nodeIdSanitized = node.id.replace(/[^a-zA-Z0-9]/g, '_');
            const scalerName = `scaler_${nodeIdSanitized}`;
            
            // Each node is self-contained and handles ONLY numeric columns
            let code = `# Standard Scaler Node (${node.id})\n`;
            code += `# Input: ${inputVar} (from previous node)\n`;
            code += `# Output: X_train_scaled, X_test_scaled (scaled numeric data)\n\n`;
            code += `from sklearn.preprocessing import StandardScaler\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            if (inputVar === 'X_train' || inputVar.includes('train')) {
                // Working with train/test split data
                code += `# Select only numeric columns for scaling\n`;
                code += `if isinstance(X_train, pd.DataFrame):\n`;
                code += `    numeric_cols = X_train.select_dtypes(include=[np.number]).columns.tolist()\n`;
                code += `    if len(numeric_cols) == 0:\n`;
                code += `        raise ValueError("No numeric columns found in X_train. StandardScaler requires numeric data.")\n`;
                code += `    print(f'Scaling {len(numeric_cols)} numeric columns: {numeric_cols}')\n`;
                code += `    ${scalerName} = StandardScaler(with_mean=${withMean}, with_std=${withStd})\n`;
                code += `    X_train_scaled = X_train.copy()\n`;
                code += `    X_test_scaled = X_test.copy()\n`;
                code += `    X_train_scaled[numeric_cols] = ${scalerName}.fit_transform(X_train[numeric_cols])\n`;
                code += `    X_test_scaled[numeric_cols] = ${scalerName}.transform(X_test[numeric_cols])\n`;
                code += `else:\n`;
                code += `    # Assume all data is numeric (numpy array)\n`;
                code += `    ${scalerName} = StandardScaler(with_mean=${withMean}, with_std=${withStd})\n`;
                code += `    X_train_scaled = ${scalerName}.fit_transform(X_train)\n`;
                code += `    X_test_scaled = ${scalerName}.transform(X_test)\n`;
                code += `print(f'Scaled X_train: {X_train.shape} -> {X_train_scaled.shape}')\n`;
                code += `print(f'Scaled X_test: {X_test.shape} -> {X_test_scaled.shape}')\n`;
                
                // Set output variable for downstream nodes
                context.setVariable(node.id, 'X_train_scaled');
            } else {
                // Regular scaling (not from train/test split)
                const outputVar = `${inputVar}_scaled`;
                code += `# Select only numeric columns for scaling\n`;
                code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
                code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
                code += `    if len(numeric_cols) == 0:\n`;
                code += `        raise ValueError("No numeric columns found. StandardScaler requires numeric data.")\n`;
                code += `    print(f'Scaling {len(numeric_cols)} numeric columns: {numeric_cols}')\n`;
                code += `    ${scalerName} = StandardScaler(with_mean=${withMean}, with_std=${withStd})\n`;
                code += `    ${outputVar} = ${inputVar}.copy()\n`;
                code += `    ${outputVar}[numeric_cols] = ${scalerName}.fit_transform(${inputVar}[numeric_cols])\n`;
                code += `else:\n`;
                code += `    ${scalerName} = StandardScaler(with_mean=${withMean}, with_std=${withStd})\n`;
                code += `    ${outputVar} = ${scalerName}.fit_transform(${inputVar})\n`;
                code += `print(f'Scaled {${inputVar}.shape}')\n`;
                
                // Set output variable for downstream nodes
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    minMaxScaler: {
        type: 'preprocess_minmax',
        name: 'MinMax Scaler',
        category: 'preprocessing',
        icon: 'bi-arrows-fullscreen',
        color: '#1976d2',
        description: 'Scale features to 0-1 range',
        imports: 'from sklearn.preprocessing import MinMaxScaler',
        defaults: {
            feature_range: [0, 1]
        },
        properties: [
            BaseNode.createProperty('min_value', 'Min Value', 'number', {
                default: 0,
                help: 'Minimum value after scaling'
            }),
            BaseNode.createProperty('max_value', 'Max Value', 'number', {
                default: 1,
                help: 'Maximum value after scaling'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            const minVal = data.min_value !== undefined ? data.min_value : 0;
            const maxVal = data.max_value !== undefined ? data.max_value : 1;
            
            // Use unique scaler name per node to avoid conflicts - ensure it's a valid Python identifier
            const nodeIdSanitized = node.id.replace(/[^a-zA-Z0-9]/g, '_');
            const scalerName = `scaler_${nodeIdSanitized}`;
            const outputVar = `${inputVar}_scaled`;
            
            // Each node is self-contained - include import and define all variables
            let code = `# MinMax Scaler Node (${node.id})\n`;
            code += `from sklearn.preprocessing import MinMaxScaler\n`;
            code += `${scalerName} = MinMaxScaler(feature_range=(${minVal}, ${maxVal}))\n`;
            code += `${outputVar} = ${scalerName}.fit_transform(${inputVar})\n`;
            code += `print(f'Scaled {${inputVar}.shape} to {${outputVar}.shape}')\n`;
            
            // Set output variable for downstream nodes
            context.setVariable(node.id, outputVar);
            
            return code;
        }
    },

    labelEncoder: {
        type: 'preprocess_encode',
        name: 'Label Encoder',
        category: 'preprocessing',
        icon: 'bi-tag',
        color: '#c2185b',
        description: 'Encode categorical labels',
        defaults: {
            handle_unknown: 'error'
        },
        properties: [
            BaseNode.createProperty('handle_unknown', 'Handle Unknown', 'select', {
                default: 'error',
                options: ['error', 'ignore'],
                help: 'How to handle unknown categories'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            
            return `from sklearn.preprocessing import LabelEncoder\nlabel_encoder = LabelEncoder()\n${inputVar}_encoded = label_encoder.fit_transform(${inputVar})`;
        }
    },

    oneHotEncoder: {
        type: 'preprocess_onehot',
        name: 'One-Hot Encoder',
        category: 'preprocessing',
        icon: 'bi-grid-3x3',
        color: '#7b1fa2',
        description: 'One-hot encode categorical variables',
        defaults: {
            drop: 'first',
            sparse: false
        },
        properties: [
            BaseNode.createProperty('drop', 'Drop', 'select', {
                default: 'first',
                options: ['first', 'if_binary', null],
                help: 'Strategy to drop one category'
            }),
            BaseNode.createProperty('sparse', 'Sparse', 'boolean', {
                default: false,
                help: 'Return sparse matrix'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node);
            const drop = data.drop || 'first';
            const sparse = data.sparse || false;
            const dropParam = drop ? `, drop='${drop}'` : '';
            
            return `from sklearn.preprocessing import OneHotEncoder\nencoder = OneHotEncoder(sparse=${sparse}${dropParam})\n${inputVar}_encoded = encoder.fit_transform(${inputVar})`;
        }
    }
};

// Register all preprocessing nodes
// Register all preprocessing nodes (safe registration)
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(PreprocessingNodes, 'preprocessing');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(PreprocessingNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register node ${nodeDef.type}:`, error);
        }
    });
} else {
    console.warn('Dependencies not ready for preprocessing.js');
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PreprocessingNodes;
}

