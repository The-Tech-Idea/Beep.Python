/**
 * TensorFlow/Keras Neural Network Nodes
 * Deep learning model creation and training
 */

const TensorFlowNodes = {
    sequentialModel: {
        type: 'tensorflow_sequential',
        name: 'Sequential Model',
        category: 'tensorflow-neural-networks',
        icon: 'bi-diagram-3',
        color: '#ff6f00',
        description: 'Create a sequential neural network model',
        defaults: {
            layers: 'Dense(64, activation="relu"), Dense(32, activation="relu"), Dense(1, activation="sigmoid")',
            optimizer: 'adam',
            loss: 'binary_crossentropy',
            metrics: 'accuracy'
        },
        properties: [
            BaseNode.createProperty('layers', 'Layers', 'text', {
                required: true,
                default: 'Dense(64, activation="relu"), Dense(32, activation="relu"), Dense(1, activation="sigmoid")',
                placeholder: 'Dense(64, activation="relu"), Dense(1)',
                help: 'Comma-separated layer definitions'
            }),
            BaseNode.createProperty('optimizer', 'Optimizer', 'select', {
                default: 'adam',
                options: ['adam', 'sgd', 'rmsprop', 'adagrad', 'adamax', 'nadam']
            }),
            BaseNode.createProperty('loss', 'Loss Function', 'text', {
                default: 'binary_crossentropy',
                placeholder: 'binary_crossentropy, categorical_crossentropy, mse',
                help: 'Loss function for training'
            }),
            BaseNode.createProperty('metrics', 'Metrics', 'text', {
                default: 'accuracy',
                placeholder: 'accuracy, precision, recall',
                help: 'Comma-separated metrics to monitor'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const layers = data.layers || '';
            const optimizer = data.optimizer || 'adam';
            const loss = data.loss || 'binary_crossentropy';
            const metrics = data.metrics || 'accuracy';
            
            if (!layers) return `# Sequential Model: Layers required`;
            
            let code = 'from tensorflow import keras\n';
            code += 'from tensorflow.keras import layers\n';
            code += `model = keras.Sequential([\n`;
            
            const layerList = layers.split(',').map(l => l.trim());
            layerList.forEach((layer, idx) => {
                code += `    layers.${layer}${idx < layerList.length - 1 ? ',' : ''}\n`;
            });
            
            code += `])\n`;
            code += `model.compile(optimizer='${optimizer}', loss='${loss}', metrics=['${metrics}'])\n`;
            code += `print(model.summary())\n`;
            
            return code;
        }
    },

    addLayer: {
        type: 'tensorflow_add_layer',
        name: 'Add Layer',
        category: 'tensorflow-neural-networks',
        icon: 'bi-plus-circle',
        color: '#ff8800',
        description: 'Add a layer to neural network',
        defaults: {
            layer_type: 'Dense',
            units: 64,
            activation: 'relu'
        },
        properties: [
            BaseNode.createProperty('layer_type', 'Layer Type', 'select', {
                default: 'Dense',
                options: ['Dense', 'Conv2D', 'LSTM', 'GRU', 'Dropout', 'BatchNormalization', 'Flatten'],
                help: 'Type of layer to add'
            }),
            BaseNode.createProperty('units', 'Units/Neurons', 'number', {
                default: 64,
                min: 1,
                max: 10000,
                help: 'Number of units/neurons'
            }),
            BaseNode.createProperty('activation', 'Activation', 'select', {
                default: 'relu',
                options: ['relu', 'sigmoid', 'tanh', 'softmax', 'linear', 'elu', 'selu'],
                help: 'Activation function'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const layerType = data.layer_type || 'Dense';
            const units = data.units || 64;
            const activation = data.activation || 'relu';
            
            let code = 'from tensorflow.keras import layers\n';
            code += `# Add ${layerType} layer\n`;
            code += `layer = layers.${layerType}(${units}, activation='${activation}')\n`;
            code += `# model.add(layer)\n`;
            
            return code;
        }
    },

    trainModel: {
        type: 'tensorflow_train',
        name: 'Train Model',
        category: 'tensorflow-neural-networks',
        icon: 'bi-play-circle',
        color: '#ff6f00',
        description: 'Train a neural network model',
        defaults: {
            epochs: 10,
            batch_size: 32,
            validation_split: 0.2,
            verbose: 1
        },
        properties: [
            BaseNode.createProperty('epochs', 'Epochs', 'number', {
                default: 10,
                min: 1,
                max: 1000
            }),
            BaseNode.createProperty('batch_size', 'Batch Size', 'number', {
                default: 32,
                min: 1,
                max: 1024
            }),
            BaseNode.createProperty('validation_split', 'Validation Split', 'number', {
                default: 0.2,
                min: 0,
                max: 1,
                step: 0.01
            }),
            BaseNode.createProperty('verbose', 'Verbose', 'number', {
                default: 1,
                min: 0,
                max: 2,
                help: '0=silent, 1=progress bar, 2=one line per epoch'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const epochs = data.epochs || 10;
            const batchSize = data.batch_size || 32;
            const valSplit = data.validation_split || 0.2;
            const verbose = data.verbose !== undefined ? data.verbose : 1;
            
            let code = `history = model.fit(X_train, y_train, epochs=${epochs}, batch_size=${batchSize}, validation_split=${valSplit}, verbose=${verbose})\n`;
            code += `print('Training complete')\n`;
            code += `# Plot training history: plt.plot(history.history['loss'])\n`;
            
            return code;
        }
    },

    evaluateModel: {
        type: 'tensorflow_evaluate',
        name: 'Evaluate Model',
        category: 'tensorflow-neural-networks',
        icon: 'bi-check-circle',
        color: '#ff8800',
        description: 'Evaluate model performance',
        defaults: {
            batch_size: 32,
            verbose: 1
        },
        properties: [
            BaseNode.createProperty('batch_size', 'Batch Size', 'number', {
                default: 32,
                min: 1,
                max: 1024
            }),
            BaseNode.createProperty('verbose', 'Verbose', 'number', {
                default: 1,
                min: 0,
                max: 2
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const batchSize = data.batch_size || 32;
            const verbose = data.verbose !== undefined ? data.verbose : 1;
            
            let code = `loss, accuracy = model.evaluate(X_test, y_test, batch_size=${batchSize}, verbose=${verbose})\n`;
            code += `print(f'Test Loss: {loss:.4f}')\n`;
            code += `print(f'Test Accuracy: {accuracy:.4f}')\n`;
            
            return code;
        }
    }
};

// Register all TensorFlow nodes
Object.values(TensorFlowNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = TensorFlowNodes;
}

