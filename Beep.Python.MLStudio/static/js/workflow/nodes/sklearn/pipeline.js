/**
 * Scikit-learn Pipeline Nodes
 * Create ML pipelines for preprocessing and modeling
 */

const SklearnPipelineNodes = {
    makePipeline: {
        type: 'sklearn_make_pipeline',
        name: 'Make Pipeline',
        category: 'sklearn-pipeline',
        icon: 'bi-diagram-2',
        color: '#1976d2',
        description: 'Construct a Pipeline from the given estimators',
        defaults: {
            steps: ''
        },
        properties: [
            BaseNode.createProperty('steps', 'Pipeline Steps', 'text', {
                required: true,
                placeholder: 'StandardScaler, PCA, RandomForestClassifier',
                help: 'Comma-separated list of transformers and estimator'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const steps = data.steps || '';
            
            if (!steps) return `# Pipeline: Steps required`;
            
            let code = 'from sklearn.pipeline import make_pipeline\n';
            code += 'from sklearn.preprocessing import StandardScaler\n';
            code += 'from sklearn.decomposition import PCA\n';
            code += 'from sklearn.ensemble import RandomForestClassifier\n';
            
            const stepList = steps.split(',').map(s => s.trim());
            code += `# Example pipeline creation\n`;
            code += `pipeline = make_pipeline(\n`;
            stepList.forEach((step, idx) => {
                code += `    ${step}()${idx < stepList.length - 1 ? ',' : ''}\n`;
            });
            code += `)\n`;
            code += `# pipeline.fit(X_train, y_train)\n`;
            code += `# y_pred = pipeline.predict(X_test)\n`;
            
            return code;
        }
    },

    featureUnion: {
        type: 'sklearn_feature_union',
        name: 'Feature Union',
        category: 'sklearn-pipeline',
        icon: 'bi-layers',
        color: '#0277bd',
        description: 'Concatenate results of multiple transformer objects',
        defaults: {
            transformers: ''
        },
        properties: [
            BaseNode.createProperty('transformers', 'Transformers', 'text', {
                required: true,
                placeholder: 'scaler:StandardScaler, pca:PCA',
                help: 'Comma-separated name:transformer pairs'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const transformers = data.transformers || '';
            
            if (!transformers) return `# Feature Union: Transformers required`;
            
            let code = 'from sklearn.pipeline import FeatureUnion\n';
            code += 'from sklearn.preprocessing import StandardScaler\n';
            code += 'from sklearn.decomposition import PCA\n';
            
            const transList = transformers.split(',').map(t => t.trim());
            code += `transformers = [\n`;
            transList.forEach(trans => {
                const [name, transformer] = trans.split(':').map(s => s.trim());
                code += `    ('${name}', ${transformer}()),\n`;
            });
            code += `]\n`;
            code += `feature_union = FeatureUnion(transformers)\n`;
            code += `# X_transformed = feature_union.fit_transform(X)\n`;
            
            return code;
        }
    }
};

// Register all pipeline nodes
Object.values(SklearnPipelineNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SklearnPipelineNodes;
}

