/**
 * Dimensionality Reduction Algorithm Nodes
 * Algorithms for reducing feature dimensions
 */

const DimensionalityReductionNodes = {
    pca: {
        type: 'algo_pca',
        name: 'PCA',
        category: 'algorithms-dimensionality',
        icon: 'bi-arrow-down-up',
        color: '#f44336',
        description: 'Principal Component Analysis',
        defaults: {
            n_components: null,
            whiten: false,
            svd_solver: 'auto',
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: null,
                min: 1,
                help: 'Number of components to keep (None = all, or float for variance ratio)'
            }),
            BaseNode.createProperty('whiten', 'Whiten', 'boolean', {
                default: false,
                help: 'Whether to whiten the components'
            }),
            BaseNode.createProperty('svd_solver', 'SVD Solver', 'select', {
                default: 'auto',
                options: ['auto', 'full', 'arpack', 'randomized']
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.n_components !== null && data.n_components !== undefined) {
                params.push(`n_components=${data.n_components}`);
            }
            if (data.whiten !== null && data.whiten !== undefined) params.push(`whiten=${data.whiten}`);
            if (data.svd_solver) params.push(`svd_solver='${data.svd_solver}'`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# PCA Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: X_pca (transformed data)\n\n`;
            code += `from sklearn.decomposition import PCA\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `# Ensure only numeric data is used\n`;
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_pca_input = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_pca_input = ${inputVar}\n\n`;
            
            code += `pca = PCA(${paramStr})\n`;
            code += `X_pca = pca.fit_transform(X_pca_input)\n`;
            code += `print(f'PCA: {X_pca_input.shape} -> {X_pca.shape}')\n`;
            code += `print(f'Explained variance: {pca.explained_variance_ratio_.sum():.2%}')\n`;
            
            context.setVariable(node.id, 'X_pca');
            return code;
        }
    },

    tsne: {
        type: 'algo_tsne',
        name: 't-SNE',
        category: 'algorithms-dimensionality',
        icon: 'bi-arrow-down-up',
        color: '#e91e63',
        description: 't-Distributed Stochastic Neighbor Embedding',
        defaults: {
            n_components: 2,
            perplexity: 30.0,
            learning_rate: 200.0,
            n_iter: 1000,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: 2,
                min: 1,
                max: 3,
                help: 'Dimension of the embedded space (typically 2 or 3)'
            }),
            BaseNode.createProperty('perplexity', 'Perplexity', 'number', {
                default: 30.0,
                min: 5,
                max: 50,
                step: 1,
                help: 'Balance between local and global structure'
            }),
            BaseNode.createProperty('learning_rate', 'Learning Rate', 'number', {
                default: 200.0,
                min: 10,
                max: 1000,
                step: 10,
                help: 'Learning rate for optimization'
            }),
            BaseNode.createProperty('n_iter', 'N Iterations', 'number', {
                default: 1000,
                min: 250,
                max: 10000,
                step: 250
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.n_components) params.push(`n_components=${data.n_components}`);
            if (data.perplexity) params.push(`perplexity=${data.perplexity}`);
            if (data.learning_rate) params.push(`learning_rate=${data.learning_rate}`);
            if (data.n_iter) params.push(`n_iter=${data.n_iter}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# t-SNE Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: X_tsne (2D/3D embedding)\n\n`;
            code += `from sklearn.manifold import TSNE\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_tsne_input = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_tsne_input = ${inputVar}\n\n`;
            
            code += `tsne = TSNE(${paramStr})\n`;
            code += `X_tsne = tsne.fit_transform(X_tsne_input)\n`;
            code += `print(f't-SNE: {X_tsne_input.shape} -> {X_tsne.shape}')\n`;
            
            context.setVariable(node.id, 'X_tsne');
            return code;
        }
    },

    ica: {
        type: 'algo_ica',
        name: 'ICA',
        category: 'algorithms-dimensionality',
        icon: 'bi-arrow-down-up',
        color: '#ff9800',
        description: 'Independent Component Analysis',
        defaults: {
            n_components: null,
            algorithm: 'parallel',
            whiten: true,
            fun: 'logcosh',
            max_iter: 200,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: null,
                min: 1,
                help: 'Number of components to extract'
            }),
            BaseNode.createProperty('algorithm', 'Algorithm', 'select', {
                default: 'parallel',
                options: ['parallel', 'deflation']
            }),
            BaseNode.createProperty('whiten', 'Whiten', 'boolean', {
                default: true
            }),
            BaseNode.createProperty('fun', 'Function', 'select', {
                default: 'logcosh',
                options: ['logcosh', 'exp', 'cube']
            }),
            BaseNode.createProperty('max_iter', 'Max Iterations', 'number', {
                default: 200,
                min: 1,
                max: 10000
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.n_components !== null && data.n_components !== undefined) params.push(`n_components=${data.n_components}`);
            if (data.algorithm) params.push(`algorithm='${data.algorithm}'`);
            if (data.whiten !== null && data.whiten !== undefined) params.push(`whiten=${data.whiten}`);
            if (data.fun) params.push(`fun='${data.fun}'`);
            if (data.max_iter) params.push(`max_iter=${data.max_iter}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# ICA Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: X_ica (independent components)\n\n`;
            code += `from sklearn.decomposition import FastICA\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_ica_input = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_ica_input = ${inputVar}\n\n`;
            
            code += `ica = FastICA(${paramStr})\n`;
            code += `X_ica = ica.fit_transform(X_ica_input)\n`;
            code += `print(f'ICA: {X_ica_input.shape} -> {X_ica.shape}')\n`;
            
            context.setVariable(node.id, 'X_ica');
            return code;
        }
    },

    lda: {
        type: 'algo_lda',
        name: 'LDA',
        category: 'algorithms-dimensionality',
        icon: 'bi-arrow-down-up',
        color: '#4caf50',
        description: 'Linear Discriminant Analysis',
        defaults: {
            n_components: null,
            solver: 'svd',
            shrinkage: null
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: null,
                min: 1,
                help: 'Number of components (None = min(n_features, n_classes - 1))'
            }),
            BaseNode.createProperty('solver', 'Solver', 'select', {
                default: 'svd',
                options: ['svd', 'lsqr', 'eigen']
            }),
            BaseNode.createProperty('shrinkage', 'Shrinkage', 'number', {
                default: null,
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Shrinkage parameter (None or float between 0 and 1)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X_train';
            const params = [];
            
            if (data.n_components !== null && data.n_components !== undefined) params.push(`n_components=${data.n_components}`);
            if (data.solver) params.push(`solver='${data.solver}'`);
            if (data.shrinkage !== null && data.shrinkage !== undefined) params.push(`shrinkage=${data.shrinkage}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# LDA Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: X_lda (transformed data)\n\n`;
            code += `from sklearn.discriminant_analysis import LinearDiscriminantAnalysis\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_lda_input = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_lda_input = ${inputVar}\n\n`;
            
            code += `lda = LinearDiscriminantAnalysis(${paramStr})\n`;
            code += `X_lda = lda.fit_transform(X_lda_input, y_train)\n`;
            code += `print(f'LDA: {X_lda_input.shape} -> {X_lda.shape}')\n`;
            
            context.setVariable(node.id, 'X_lda');
            return code;
        }
    },

    umap: {
        type: 'algo_umap',
        name: 'UMAP',
        category: 'algorithms-dimensionality',
        icon: 'bi-arrow-down-up',
        color: '#00bcd4',
        description: 'Uniform Manifold Approximation and Projection (requires umap-learn)',
        defaults: {
            n_components: 2,
            n_neighbors: 15,
            min_dist: 0.1,
            metric: 'euclidean',
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: 2,
                min: 1,
                max: 3,
                help: 'Dimension of the embedded space'
            }),
            BaseNode.createProperty('n_neighbors', 'N Neighbors', 'number', {
                default: 15,
                min: 2,
                max: 100,
                help: 'Size of local neighborhood'
            }),
            BaseNode.createProperty('min_dist', 'Min Distance', 'number', {
                default: 0.1,
                min: 0,
                max: 1,
                step: 0.01,
                help: 'Minimum distance between points in embedded space'
            }),
            BaseNode.createProperty('metric', 'Metric', 'select', {
                default: 'euclidean',
                options: ['euclidean', 'manhattan', 'cosine', 'hamming']
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.n_components) params.push(`n_components=${data.n_components}`);
            if (data.n_neighbors) params.push(`n_neighbors=${data.n_neighbors}`);
            if (data.min_dist !== undefined) params.push(`min_dist=${data.min_dist}`);
            if (data.metric) params.push(`metric='${data.metric}'`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# UMAP Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: X_umap (2D/3D embedding)\n\n`;
            code += `from umap import UMAP\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_umap_input = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_umap_input = ${inputVar}\n\n`;
            
            code += `umap_model = UMAP(${paramStr})\n`;
            code += `X_umap = umap_model.fit_transform(X_umap_input)\n`;
            code += `print(f'UMAP: {X_umap_input.shape} -> {X_umap.shape}')\n`;
            
            context.setVariable(node.id, 'X_umap');
            return code;
        }
    }
};

// Register dimensionality reduction nodes
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(DimensionalityReductionNodes, 'dimensionality reduction');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(DimensionalityReductionNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register dimensionality reduction node ${nodeDef.type}:`, error);
        }
    });
    console.log('Registered dimensionality reduction algorithm nodes');
} else {
    console.warn('Dependencies not ready for algorithms_dimensionality.js');
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DimensionalityReductionNodes;
}

