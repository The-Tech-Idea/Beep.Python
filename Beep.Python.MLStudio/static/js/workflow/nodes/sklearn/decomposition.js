/**
 * Scikit-learn Decomposition Nodes
 * Dimensionality reduction and matrix decomposition
 */

const SklearnDecompositionNodes = {
    pca: {
        type: 'sklearn_pca',
        name: 'PCA',
        category: 'sklearn-decomposition',
        icon: 'bi-diagram-2',
        color: '#1976d2',
        description: 'Principal Component Analysis for dimensionality reduction',
        defaults: {
            n_components: null,
            whiten: false,
            random_state: null
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: null,
                placeholder: 'Leave empty for all',
                help: 'Number of components to keep (None for all)'
            }),
            BaseNode.createProperty('whiten', 'Whiten', 'boolean', {
                default: false,
                help: 'Whiten the components'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: null,
                placeholder: '42 or leave empty',
                help: 'Random seed'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_pca';
            const nComponents = data.n_components !== null && data.n_components !== undefined ? data.n_components : null;
            const whiten = data.whiten || false;
            const randomState = data.random_state !== null && data.random_state !== undefined ? data.random_state : null;
            
            let code = 'from sklearn.decomposition import PCA\n';
            const params = [`whiten=${whiten}`];
            if (nComponents !== null) {
                params.push(`n_components=${nComponents}`);
            }
            if (randomState !== null) {
                params.push(`random_state=${randomState}`);
            }
            
            code += `pca = PCA(${params.join(', ')})\n`;
            code += `${outputVar} = pca.fit_transform(${inputVar})\n`;
            code += `print(f'Explained variance ratio: {pca.explained_variance_ratio_[:5]}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    truncatedSVD: {
        type: 'sklearn_truncated_svd',
        name: 'Truncated SVD',
        category: 'sklearn-decomposition',
        icon: 'bi-scissors',
        color: '#0277bd',
        description: 'Dimensionality reduction using truncated SVD',
        defaults: {
            n_components: 2,
            random_state: null
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: 2,
                min: 1,
                max: 100,
                help: 'Desired dimensionality of output data'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: null,
                placeholder: '42 or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_svd';
            const nComponents = data.n_components || 2;
            const randomState = data.random_state !== null && data.random_state !== undefined ? data.random_state : null;
            
            let code = 'from sklearn.decomposition import TruncatedSVD\n';
            const params = [`n_components=${nComponents}`];
            if (randomState !== null) {
                params.push(`random_state=${randomState}`);
            }
            
            code += `svd = TruncatedSVD(${params.join(', ')})\n`;
            code += `${outputVar} = svd.fit_transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    fastICA: {
        type: 'sklearn_fast_ica',
        name: 'Fast ICA',
        category: 'sklearn-decomposition',
        icon: 'bi-lightning',
        color: '#e65100',
        description: 'Fast Independent Component Analysis',
        defaults: {
            n_components: null,
            random_state: null
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: null,
                placeholder: 'Leave empty for all',
                help: 'Number of components to extract'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: null,
                placeholder: '42 or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_ica';
            const nComponents = data.n_components !== null && data.n_components !== undefined ? data.n_components : null;
            const randomState = data.random_state !== null && data.random_state !== undefined ? data.random_state : null;
            
            let code = 'from sklearn.decomposition import FastICA\n';
            const params = [];
            if (nComponents !== null) {
                params.push(`n_components=${nComponents}`);
            }
            if (randomState !== null) {
                params.push(`random_state=${randomState}`);
            }
            
            code += `ica = FastICA(${params.length > 0 ? params.join(', ') : ''})\n`;
            code += `${outputVar} = ica.fit_transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    factorAnalysis: {
        type: 'sklearn_factor_analysis',
        name: 'Factor Analysis',
        category: 'sklearn-decomposition',
        icon: 'bi-pie-chart',
        color: '#7b1fa2',
        description: 'Factor Analysis for dimensionality reduction',
        defaults: {
            n_components: null,
            random_state: null
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: null,
                placeholder: 'Leave empty for auto',
                help: 'Dimensionality of latent space'
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: null,
                placeholder: '42 or leave empty'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = inputVar + '_fa';
            const nComponents = data.n_components !== null && data.n_components !== undefined ? data.n_components : null;
            const randomState = data.random_state !== null && data.random_state !== undefined ? data.random_state : null;
            
            let code = 'from sklearn.decomposition import FactorAnalysis\n';
            const params = [];
            if (nComponents !== null) {
                params.push(`n_components=${nComponents}`);
            }
            if (randomState !== null) {
                params.push(`random_state=${randomState}`);
            }
            
            code += `fa = FactorAnalysis(${params.length > 0 ? params.join(', ') : ''})\n`;
            code += `${outputVar} = fa.fit_transform(${inputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all sklearn decomposition nodes
Object.values(SklearnDecompositionNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SklearnDecompositionNodes;
}

