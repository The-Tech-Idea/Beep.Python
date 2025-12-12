/**
 * Clustering Algorithm Nodes
 * Unsupervised learning algorithms for clustering
 */

const ClusteringAlgorithmNodes = {
    kMeans: {
        type: 'algo_kmeans',
        name: 'K-Means',
        category: 'algorithms-clustering',
        icon: 'bi-diagram-3',
        color: '#9c27b0',
        description: 'K-Means Clustering',
        defaults: {
            n_clusters: 8,
            init: 'k-means++',
            n_init: 10,
            max_iter: 300,
            random_state: 42,
            algorithm: 'auto'
        },
        properties: [
            BaseNode.createProperty('n_clusters', 'N Clusters', 'number', {
                default: 8,
                min: 2,
                max: 100,
                help: 'Number of clusters to form'
            }),
            BaseNode.createProperty('init', 'Initialization', 'select', {
                default: 'k-means++',
                options: ['k-means++', 'random'],
                help: 'Initialization method'
            }),
            BaseNode.createProperty('n_init', 'N Init', 'number', {
                default: 10,
                min: 1,
                max: 100,
                help: 'Number of times to run k-means with different centroids'
            }),
            BaseNode.createProperty('max_iter', 'Max Iterations', 'number', {
                default: 300,
                min: 1,
                max: 10000
            }),
            BaseNode.createProperty('random_state', 'Random State', 'number', {
                default: 42
            }),
            BaseNode.createProperty('algorithm', 'Algorithm', 'select', {
                default: 'auto',
                options: ['auto', 'full', 'elkan']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.n_clusters) params.push(`n_clusters=${data.n_clusters}`);
            if (data.init) params.push(`init='${data.init}'`);
            if (data.n_init) params.push(`n_init=${data.n_init}`);
            if (data.max_iter) params.push(`max_iter=${data.max_iter}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            if (data.algorithm) params.push(`algorithm='${data.algorithm}'`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# K-Means Clustering Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: kmeans_model, labels (cluster assignments)\n\n`;
            code += `from sklearn.cluster import KMeans\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            // Handle numeric data only
            code += `# Ensure only numeric data is used\n`;
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_cluster = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_cluster = ${inputVar}\n\n`;
            
            code += `kmeans_model = KMeans(${paramStr})\n`;
            code += `kmeans_model.fit(X_cluster)\n`;
            code += `labels = kmeans_model.labels_\n`;
            code += `print(f'Clustered into {kmeans_model.n_clusters} clusters')\n`;
            code += `print(f'Cluster sizes: {dict(zip(*np.unique(labels, return_counts=True)))}')\n`;
            
            // Register output
            context.setVariable(node.id, 'kmeans_model');
            
            return code;
        }
    },

    dbscan: {
        type: 'algo_dbscan',
        name: 'DBSCAN',
        category: 'algorithms-clustering',
        icon: 'bi-diagram-3',
        color: '#e91e63',
        description: 'Density-Based Spatial Clustering',
        defaults: {
            eps: 0.5,
            min_samples: 5,
            metric: 'euclidean',
            algorithm: 'auto'
        },
        properties: [
            BaseNode.createProperty('eps', 'Epsilon (eps)', 'number', {
                default: 0.5,
                min: 0.01,
                max: 10,
                step: 0.01,
                help: 'Maximum distance between samples in the same neighborhood'
            }),
            BaseNode.createProperty('min_samples', 'Min Samples', 'number', {
                default: 5,
                min: 1,
                max: 100,
                help: 'Minimum number of samples in a neighborhood'
            }),
            BaseNode.createProperty('metric', 'Metric', 'select', {
                default: 'euclidean',
                options: ['euclidean', 'manhattan', 'cosine', 'haversine']
            }),
            BaseNode.createProperty('algorithm', 'Algorithm', 'select', {
                default: 'auto',
                options: ['auto', 'ball_tree', 'kd_tree', 'brute']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.eps !== undefined) params.push(`eps=${data.eps}`);
            if (data.min_samples) params.push(`min_samples=${data.min_samples}`);
            if (data.metric) params.push(`metric='${data.metric}'`);
            if (data.algorithm) params.push(`algorithm='${data.algorithm}'`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# DBSCAN Clustering Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: dbscan_model, labels (cluster assignments)\n\n`;
            code += `from sklearn.cluster import DBSCAN\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `# Ensure only numeric data is used\n`;
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_cluster = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_cluster = ${inputVar}\n\n`;
            
            code += `dbscan_model = DBSCAN(${paramStr})\n`;
            code += `labels = dbscan_model.fit_predict(X_cluster)\n`;
            code += `n_clusters = len(set(labels)) - (1 if -1 in labels else 0)\n`;
            code += `n_noise = list(labels).count(-1)\n`;
            code += `print(f'Clusters: {n_clusters}, Noise points: {n_noise}')\n`;
            
            // Register output
            context.setVariable(node.id, 'dbscan_model');
            
            return code;
        }
    },

    hierarchicalClustering: {
        type: 'algo_hierarchical_clustering',
        name: 'Hierarchical Clustering',
        category: 'algorithms-clustering',
        icon: 'bi-diagram-3',
        color: '#673ab7',
        description: 'Agglomerative Hierarchical Clustering',
        defaults: {
            n_clusters: 2,
            linkage: 'ward',
            affinity: 'euclidean',
            distance_threshold: null
        },
        properties: [
            BaseNode.createProperty('n_clusters', 'N Clusters', 'number', {
                default: 2,
                min: 2,
                max: 100,
                help: 'Number of clusters to find (or None for distance_threshold)'
            }),
            BaseNode.createProperty('linkage', 'Linkage', 'select', {
                default: 'ward',
                options: ['ward', 'complete', 'average', 'single'],
                help: 'Linkage criterion'
            }),
            BaseNode.createProperty('affinity', 'Affinity', 'select', {
                default: 'euclidean',
                options: ['euclidean', 'l1', 'l2', 'manhattan', 'cosine']
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'X';
            const params = [];
            
            if (data.n_clusters) params.push(`n_clusters=${data.n_clusters}`);
            if (data.linkage) params.push(`linkage='${data.linkage}'`);
            if (data.affinity && data.linkage !== 'ward') params.push(`affinity='${data.affinity}'`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# Hierarchical Clustering Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: hc_model, labels (cluster assignments)\n\n`;
            code += `from sklearn.cluster import AgglomerativeClustering\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `# Ensure only numeric data is used\n`;
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_cluster = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_cluster = ${inputVar}\n\n`;
            
            code += `hc_model = AgglomerativeClustering(${paramStr})\n`;
            code += `labels = hc_model.fit_predict(X_cluster)\n`;
            code += `print(f'Clustered into {hc_model.n_clusters_} clusters')\n`;
            
            // Register output
            context.setVariable(node.id, 'hc_model');
            
            return code;
        }
    },

    gaussianMixture: {
        type: 'algo_gaussian_mixture',
        name: 'Gaussian Mixture',
        category: 'algorithms-clustering',
        icon: 'bi-diagram-3',
        color: '#3f51b5',
        description: 'Gaussian Mixture Model',
        defaults: {
            n_components: 1,
            covariance_type: 'full',
            max_iter: 100,
            random_state: 42
        },
        properties: [
            BaseNode.createProperty('n_components', 'N Components', 'number', {
                default: 1,
                min: 1,
                max: 100,
                help: 'Number of mixture components'
            }),
            BaseNode.createProperty('covariance_type', 'Covariance Type', 'select', {
                default: 'full',
                options: ['full', 'tied', 'diag', 'spherical']
            }),
            BaseNode.createProperty('max_iter', 'Max Iterations', 'number', {
                default: 100,
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
            
            if (data.n_components) params.push(`n_components=${data.n_components}`);
            if (data.covariance_type) params.push(`covariance_type='${data.covariance_type}'`);
            if (data.max_iter) params.push(`max_iter=${data.max_iter}`);
            if (data.random_state !== null && data.random_state !== undefined) params.push(`random_state=${data.random_state}`);
            
            const paramStr = params.length > 0 ? params.join(', ') : '';
            
            let code = `# Gaussian Mixture Node (${node.id})\n`;
            code += `# Input: ${inputVar} (data from previous node)\n`;
            code += `# Output: gmm_model, labels\n\n`;
            code += `from sklearn.mixture import GaussianMixture\n`;
            code += `import pandas as pd\n`;
            code += `import numpy as np\n\n`;
            
            code += `# Ensure only numeric data is used\n`;
            code += `if isinstance(${inputVar}, pd.DataFrame):\n`;
            code += `    numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
            code += `    X_gmm = ${inputVar}[numeric_cols].values\n`;
            code += `else:\n`;
            code += `    X_gmm = ${inputVar}\n\n`;
            
            code += `gmm_model = GaussianMixture(${paramStr})\n`;
            code += `gmm_model.fit(X_gmm)\n`;
            code += `labels = gmm_model.predict(X_gmm)\n`;
            code += `print(f'GMM: {gmm_model.n_components} components, Converged: {gmm_model.converged_}')\n`;
            
            context.setVariable(node.id, 'gmm_model');
            return code;
        }
    },

    meanShift: {
        type: 'algo_mean_shift',
        name: 'Mean Shift',
        category: 'algorithms-clustering',
        icon: 'bi-diagram-3',
        color: '#009688',
        description: 'Mean Shift Clustering',
        defaults: {
            bandwidth: null,
            bin_seeding: false,
            min_bin_freq: 1,
            cluster_all: true
        },
        properties: [
            BaseNode.createProperty('bandwidth', 'Bandwidth', 'number', {
                default: null,
                min: 0.01,
                max: 100,
                step: 0.01,
                help: 'Bandwidth parameter (None for automatic estimation)'
            }),
            BaseNode.createProperty('bin_seeding', 'Bin Seeding', 'boolean', {
                default: false,
                help: 'Use bin seeding to speed up initialization'
            }),
            BaseNode.createProperty('cluster_all', 'Cluster All', 'boolean', {
                default: true,
                help: 'Whether to cluster all points, including outliers'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'X';
            const outputVar = context ? context.getOutputVariable(node) : 'model';
            const params = [];
            
            if (data.bandwidth !== null && data.bandwidth !== undefined) params.push(`bandwidth=${data.bandwidth}`);
            if (data.bin_seeding !== null && data.bin_seeding !== undefined) params.push(`bin_seeding=${data.bin_seeding}`);
            if (data.cluster_all !== null && data.cluster_all !== undefined) params.push(`cluster_all=${data.cluster_all}`);
            
            const paramStr = params.length > 0 ? ', ' + params.join(', ') : '';
            return `from sklearn.cluster import MeanShift
${outputVar} = MeanShift(${paramStr})
labels = ${outputVar}.fit_predict(${inputVar})
n_clusters = len(set(labels))
print(f'Estimated number of clusters: {n_clusters}')`;
        }
    }
};

// Register clustering algorithm nodes
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(ClusteringAlgorithmNodes, 'clustering algorithm');
} else if (typeof BaseNode !== 'undefined' && typeof nodeRegistry !== 'undefined') {
    Object.values(ClusteringAlgorithmNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register clustering node ${nodeDef.type}:`, error);
        }
    });
    console.log('Registered clustering algorithm nodes');
} else {
    console.warn('Dependencies not ready for algorithms_clustering.js');
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ClusteringAlgorithmNodes;
}

