/**
 * Scikit-learn Encoding Nodes
 * Categorical encoding and transformation
 */

const SklearnEncodingNodes = {
    labelEncoder: {
        type: 'sklearn_label_encoder',
        name: 'Label Encoder',
        category: 'sklearn-encoding',
        icon: 'bi-tag',
        color: '#c2185b',
        description: 'Encode target labels with value between 0 and n_classes-1',
        defaults: {},
        properties: [],
        generateCode: (node, context) => {
            const inputVar = context ? context.getInputVariable(node) : 'y';
            const outputVar = inputVar + '_encoded';
            
            let code = 'from sklearn.preprocessing import LabelEncoder\n';
            code += `label_encoder = LabelEncoder()\n`;
            code += `${outputVar} = label_encoder.fit_transform(${inputVar})\n`;
            code += `print(f'Classes: {label_encoder.classes_}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    oneHotEncoder: {
        type: 'sklearn_onehot_encoder',
        name: 'One-Hot Encoder',
        category: 'sklearn-encoding',
        icon: 'bi-grid-3x3',
        color: '#7b1fa2',
        description: 'Encode categorical features as a one-hot numeric array',
        defaults: {
            drop: null,
            sparse: false,
            handle_unknown: 'error'
        },
        properties: [
            BaseNode.createProperty('drop', 'Drop Strategy', 'select', {
                default: null,
                options: [null, 'first', 'if_binary'],
                help: 'Strategy to drop one category per feature'
            }),
            BaseNode.createProperty('sparse', 'Sparse Output', 'boolean', {
                default: false,
                help: 'Return sparse matrix'
            }),
            BaseNode.createProperty('handle_unknown', 'Handle Unknown', 'select', {
                default: 'error',
                options: ['error', 'ignore'],
                help: 'How to handle unknown categories'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_encoded';
            const drop = data.drop || null;
            const sparse = data.sparse || false;
            const handleUnknown = data.handle_unknown || 'error';
            
            let code = 'from sklearn.preprocessing import OneHotEncoder\n';
            const params = [];
            if (drop) params.push(`drop='${drop}'`);
            params.push(`sparse=${sparse}`);
            params.push(`handle_unknown='${handleUnknown}'`);
            
            code += `encoder = OneHotEncoder(${params.join(', ')})\n`;
            code += `${outputVar} = encoder.fit_transform(${inputVar})\n`;
            
            if (!sparse) {
                code += `# Convert to dense array if needed\n`;
                code += `${outputVar} = ${outputVar}.toarray() if hasattr(${outputVar}, 'toarray') else ${outputVar}\n`;
            }
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    ordinalEncoder: {
        type: 'sklearn_ordinal_encoder',
        name: 'Ordinal Encoder',
        category: 'sklearn-encoding',
        icon: 'bi-list-ol',
        color: '#0277bd',
        description: 'Encode categorical features as an integer array',
        defaults: {
            categories: 'auto',
            handle_unknown: 'error'
        },
        properties: [
            BaseNode.createProperty('handle_unknown', 'Handle Unknown', 'select', {
                default: 'error',
                options: ['error', 'use_encoded_value'],
                help: 'How to handle unknown categories'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_encoded';
            const handleUnknown = data.handle_unknown || 'error';
            
            let code = 'from sklearn.preprocessing import OrdinalEncoder\n';
            code += `encoder = OrdinalEncoder(handle_unknown='${handleUnknown}')\n`;
            code += `${outputVar} = encoder.fit_transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    targetEncoder: {
        type: 'sklearn_target_encoder',
        name: 'Target Encoder',
        category: 'sklearn-encoding',
        icon: 'bi-bullseye',
        color: '#e65100',
        description: 'Target encoding for categorical features',
        defaults: {
            categories: 'auto',
            target_type: 'auto',
            smooth: 'auto',
            cv: 5
        },
        properties: [
            BaseNode.createProperty('smooth', 'Smoothing', 'text', {
                default: 'auto',
                placeholder: 'auto or number',
                help: 'Amount of smoothing (auto or float)'
            }),
            BaseNode.createProperty('cv', 'Cross Validation Folds', 'number', {
                default: 5,
                min: 2,
                max: 20,
                help: 'Number of cross-validation folds'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_encoded';
            const smooth = data.smooth || 'auto';
            const cv = data.cv || 5;
            
            let code = 'from sklearn.preprocessing import TargetEncoder\n';
            const smoothParam = smooth === 'auto' ? "'auto'" : smooth;
            code += `encoder = TargetEncoder(smooth=${smoothParam}, cv=${cv})\n`;
            code += `${outputVar} = encoder.fit_transform(${inputVar}, y)\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    binarizer: {
        type: 'sklearn_binarizer',
        name: 'Binarizer',
        category: 'sklearn-encoding',
        icon: 'bi-toggle-on',
        color: '#2e7d32',
        description: 'Binarize data (set feature values to 0 or 1) according to a threshold',
        defaults: {
            threshold: 0.0,
            copy: true
        },
        properties: [
            BaseNode.createProperty('threshold', 'Threshold', 'number', {
                default: 0.0,
                help: 'Feature values below or equal to this are replaced by 0, above it by 1'
            }),
            BaseNode.createProperty('copy', 'Copy', 'boolean', {
                default: true
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_binarized';
            const threshold = data.threshold !== undefined ? data.threshold : 0.0;
            const copy = data.copy !== false;
            
            let code = 'from sklearn.preprocessing import Binarizer\n';
            code += `binarizer = Binarizer(threshold=${threshold}, copy=${copy})\n`;
            code += `${outputVar} = binarizer.transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all sklearn encoding nodes
Object.values(SklearnEncodingNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SklearnEncodingNodes;
}

