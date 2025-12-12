/**
 * Clustering-Based Feature Engineering Nodes
 * Use clustering to create new features
 */

const ClusteringFeatureNodes = {
    kmeansFeatures: {
        type: 'fe_kmeans_features',
        name: 'K-Means Cluster Features',
        category: 'feature-engineering',
        icon: 'bi-diagram-3',
        color: '#1976d2',
        description: 'Create cluster assignment features using K-Means',
        defaults: {
            columns: '',
            n_clusters: 5,
            feature_name: 'cluster'
        },
        properties: [
            BaseNode.createProperty('columns', 'Feature Columns', 'text', {
                required: true,
                placeholder: 'col1, col2, col3',
                help: 'Comma-separated columns to cluster'
            }),
            BaseNode.createProperty('n_clusters', 'Number of Clusters', 'number', {
                default: 5,
                min: 2,
                max: 50,
                help: 'Number of clusters'
            }),
            BaseNode.createProperty('feature_name', 'Feature Name', 'text', {
                default: 'cluster',
                placeholder: 'cluster',
                help: 'Name for cluster feature'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const nClusters = data.n_clusters || 5;
            const featureName = data.feature_name || 'cluster';
            
            if (!columns) return `# K-Means Features: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            
            let code = 'from sklearn.cluster import KMeans\n';
            code += `import numpy as np\n`;
            code += `# K-Means clustering features\n`;
            code += `X_cluster = ${inputVar}[${colList.map(c => `'${c}'`).join(', ')}].values\n`;
            code += `kmeans = KMeans(n_clusters=${nClusters}, random_state=42, n_init=10)\n`;
            code += `${inputVar}['${featureName}'] = kmeans.fit_predict(X_cluster)\n`;
            code += `${inputVar}['${featureName}_distance'] = kmeans.transform(X_cluster).min(axis=1)\n`;
            code += `print(f'Created K-Means cluster features with {nClusters} clusters')\n`;
            
            return code;
        }
    },

    dbscanFeatures: {
        type: 'fe_dbscan_features',
        name: 'DBSCAN Cluster Features',
        category: 'feature-engineering',
        icon: 'bi-diagram-2',
        color: '#0277bd',
        description: 'Create cluster assignment features using DBSCAN',
        defaults: {
            columns: '',
            eps: 0.5,
            min_samples: 5
        },
        properties: [
            BaseNode.createProperty('columns', 'Feature Columns', 'text', {
                required: true,
                placeholder: 'col1, col2'
            }),
            BaseNode.createProperty('eps', 'Epsilon', 'number', {
                default: 0.5,
                min: 0.1,
                max: 10,
                step: 0.1,
                help: 'Maximum distance between samples'
            }),
            BaseNode.createProperty('min_samples', 'Min Samples', 'number', {
                default: 5,
                min: 2,
                max: 100,
                help: 'Minimum samples in a cluster'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const eps = data.eps !== undefined ? data.eps : 0.5;
            const minSamples = data.min_samples || 5;
            
            if (!columns) return `# DBSCAN Features: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            
            let code = 'from sklearn.cluster import DBSCAN\n';
            code += `# DBSCAN clustering features\n`;
            code += `X_cluster = ${inputVar}[${colList.map(c => `'${c}'`).join(', ')}].values\n`;
            code += `dbscan = DBSCAN(eps=${eps}, min_samples=${minSamples})\n`;
            code += `${inputVar}['dbscan_cluster'] = dbscan.fit_predict(X_cluster)\n`;
            code += `print(f'Created DBSCAN cluster features')\n`;
            
            return code;
        }
    },

    hierarchicalClusterFeatures: {
        type: 'fe_hierarchical_cluster',
        name: 'Hierarchical Cluster Features',
        category: 'feature-engineering',
        icon: 'bi-diagram-3-fill',
        color: '#e65100',
        description: 'Create cluster features using hierarchical clustering',
        defaults: {
            columns: '',
            n_clusters: 5,
            linkage: 'ward'
        },
        properties: [
            BaseNode.createProperty('columns', 'Feature Columns', 'text', {
                required: true,
                placeholder: 'col1, col2'
            }),
            BaseNode.createProperty('n_clusters', 'Number of Clusters', 'number', {
                default: 5,
                min: 2,
                max: 50
            }),
            BaseNode.createProperty('linkage', 'Linkage Method', 'select', {
                default: 'ward',
                options: ['ward', 'complete', 'average', 'single'],
                help: 'Linkage criterion'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const nClusters = data.n_clusters || 5;
            const linkage = data.linkage || 'ward';
            
            if (!columns) return `# Hierarchical Cluster: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            
            let code = 'from sklearn.cluster import AgglomerativeClustering\n';
            code += `# Hierarchical clustering features\n`;
            code += `X_cluster = ${inputVar}[${colList.map(c => `'${c}'`).join(', ')}].values\n`;
            code += `hierarchical = AgglomerativeClustering(n_clusters=${nClusters}, linkage='${linkage}')\n`;
            code += `${inputVar}['hierarchical_cluster'] = hierarchical.fit_predict(X_cluster)\n`;
            code += `print(f'Created hierarchical cluster features with {nClusters} clusters')\n`;
            
            return code;
        }
    },

    pcaFeatures: {
        type: 'fe_pca_features',
        name: 'PCA Features',
        category: 'feature-engineering',
        icon: 'bi-arrow-down-up',
        color: '#2e7d32',
        description: 'Create principal component features',
        defaults: {
            columns: '',
            n_components: 5
        },
        properties: [
            BaseNode.createProperty('columns', 'Feature Columns', 'text', {
                required: true,
                placeholder: 'col1, col2, col3'
            }),
            BaseNode.createProperty('n_components', 'Number of Components', 'number', {
                default: 5,
                min: 1,
                max: 50,
                help: 'Number of principal components'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const nComponents = data.n_components || 5;
            
            if (!columns) return `# PCA Features: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            
            let code = 'from sklearn.decomposition import PCA\n';
            code += `# PCA features\n`;
            code += `X_pca = ${inputVar}[${colList.map(c => `'${c}'`).join(', ')}].values\n`;
            code += `pca = PCA(n_components=${nComponents})\n`;
            code += `pca_features = pca.fit_transform(X_pca)\n`;
            code += `for i in range(${nComponents}):\n`;
            code += `    ${inputVar}[f'pca_component_{i+1}'] = pca_features[:, i]\n`;
            code += `print(f'Created {nComponents} PCA features, explained variance: {pca.explained_variance_ratio_.sum():.2%}')\n`;
            
            return code;
        }
    },

    icaFeatures: {
        type: 'fe_ica_features',
        name: 'ICA Features',
        category: 'feature-engineering',
        icon: 'bi-shuffle',
        color: '#c2185b',
        description: 'Create independent component features',
        defaults: {
            columns: '',
            n_components: 5
        },
        properties: [
            BaseNode.createProperty('columns', 'Feature Columns', 'text', {
                required: true,
                placeholder: 'col1, col2'
            }),
            BaseNode.createProperty('n_components', 'Number of Components', 'number', {
                default: 5,
                min: 1,
                max: 50
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            const nComponents = data.n_components || 5;
            
            if (!columns) return `# ICA Features: Columns required`;
            
            const colList = columns.split(',').map(c => c.trim());
            
            let code = 'from sklearn.decomposition import FastICA\n';
            code += `# ICA features\n`;
            code += `X_ica = ${inputVar}[${colList.map(c => `'${c}'`).join(', ')}].values\n`;
            code += `ica = FastICA(n_components=${nComponents}, random_state=42)\n`;
            code += `ica_features = ica.fit_transform(X_ica)\n`;
            code += `for i in range(${nComponents}):\n`;
            code += `    ${inputVar}[f'ica_component_{i+1}'] = ica_features[:, i]\n`;
            code += `print(f'Created {nComponents} ICA features')\n`;
            
            return code;
        }
    }
};

// Register all clustering feature nodes
Object.values(ClusteringFeatureNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ClusteringFeatureNodes;
}

