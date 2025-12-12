/**
 * Model Deployment Nodes
 * API endpoints and model serving
 */

const ModelDeploymentNodes = {
    saveModel: {
        type: 'deployment_save_model',
        name: 'Save Model for Deployment',
        category: 'model-deployment',
        icon: 'bi-cloud-upload',
        color: '#28a745',
        description: 'Save model in deployment format (TensorFlow SavedModel, ONNX, etc.)',
        defaults: {
            file_path: 'models/deployed_model',
            format: 'savedmodel'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                default: 'models/deployed_model',
                placeholder: 'models/my_model'
            }),
            BaseNode.createProperty('format', 'Format', 'select', {
                default: 'savedmodel',
                options: ['savedmodel', 'h5', 'onnx', 'pickle', 'joblib'],
                help: 'Model serialization format'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const filePath = data.file_path || 'models/deployed_model';
            const format = data.format || 'savedmodel';
            
            let code = 'import os\n';
            code += `os.makedirs(os.path.dirname('${filePath}'), exist_ok=True)\n`;
            
            if (format === 'savedmodel') {
                code += `model.save('${filePath}')\n`;
            } else if (format === 'h5') {
                code += `model.save('${filePath}.h5')\n`;
            } else if (format === 'onnx') {
                code += `# Requires onnx and tf2onnx\n`;
                code += `# import tf2onnx\n`;
                code += `# tf2onnx.convert.from_keras(model, output_path='${filePath}.onnx')\n`;
            } else if (format === 'pickle') {
                code += `import pickle\n`;
                code += `with open('${filePath}.pkl', 'wb') as f:\n`;
                code += `    pickle.dump(model, f)\n`;
            } else if (format === 'joblib') {
                code += `import joblib\n`;
                code += `joblib.dump(model, '${filePath}.joblib')\n`;
            }
            
            code += `print(f'Model saved for deployment: {filePath}')\n`;
            
            return code;
        }
    },

    createAPIEndpoint: {
        type: 'deployment_api_endpoint',
        name: 'Create API Endpoint',
        category: 'model-deployment',
        icon: 'bi-server',
        color: '#17a2b8',
        description: 'Generate Flask/FastAPI endpoint code for model serving',
        defaults: {
            framework: 'flask',
            endpoint: '/predict',
            port: 5000
        },
        properties: [
            BaseNode.createProperty('framework', 'Framework', 'select', {
                default: 'flask',
                options: ['flask', 'fastapi'],
                help: 'Web framework for API'
            }),
            BaseNode.createProperty('endpoint', 'Endpoint Path', 'text', {
                default: '/predict',
                placeholder: '/predict',
                help: 'API endpoint path'
            }),
            BaseNode.createProperty('port', 'Port', 'number', {
                default: 5000,
                min: 1000,
                max: 65535
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const framework = data.framework || 'flask';
            const endpoint = data.endpoint || '/predict';
            const port = data.port || 5000;
            
            let code = '';
            if (framework === 'flask') {
                code += `from flask import Flask, request, jsonify\n`;
                code += `import pickle\n`;
                code += `\n`;
                code += `app = Flask(__name__)\n`;
                code += `\n`;
                code += `# Load model\n`;
                code += `with open('models/model.pkl', 'rb') as f:\n`;
                code += `    model = pickle.load(f)\n`;
                code += `\n`;
                code += `@app.route('${endpoint}', methods=['POST'])\n`;
                code += `def predict():\n`;
                code += `    data = request.get_json()\n`;
                code += `    features = data['features']\n`;
                code += `    prediction = model.predict([features])[0]\n`;
                code += `    return jsonify({'prediction': float(prediction)})\n`;
                code += `\n`;
                code += `if __name__ == '__main__':\n`;
                code += `    app.run(port=${port}, debug=True)\n`;
            } else {
                code += `from fastapi import FastAPI\n`;
                code += `from pydantic import BaseModel\n`;
                code += `import pickle\n`;
                code += `\n`;
                code += `app = FastAPI()\n`;
                code += `\n`;
                code += `# Load model\n`;
                code += `with open('models/model.pkl', 'rb') as f:\n`;
                code += `    model = pickle.load(f)\n`;
                code += `\n`;
                code += `class Features(BaseModel):\n`;
                code += `    features: list\n`;
                code += `\n`;
                code += `@app.post('${endpoint}')\n`;
                code += `def predict(features: Features):\n`;
                code += `    prediction = model.predict([features.features])[0]\n`;
                code += `    return {'prediction': float(prediction)}\n`;
            }
            
            code += `\n# Save this code to api.py and run: python api.py\n`;
            
            return code;
        }
    }
};

// Register all deployment nodes
Object.values(ModelDeploymentNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ModelDeploymentNodes;
}

