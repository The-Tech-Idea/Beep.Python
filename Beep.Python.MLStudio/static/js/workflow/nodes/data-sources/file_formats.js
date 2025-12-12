/**
 * File Format Data Loading Nodes
 * Load data from various file formats
 */

const FileFormatNodes = {
    loadParquet: {
        type: 'data_load_parquet',
        name: 'Load Parquet',
        category: 'data-sources',
        icon: 'bi-file-earmark-binary',
        color: '#1976d2',
        description: 'Load data from Parquet file (fast columnar format)',
        defaults: {
            file_path: 'data/dataset.parquet',
            variable_name: 'df',
            engine: 'pyarrow'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.parquet'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            }),
            BaseNode.createProperty('engine', 'Engine', 'select', {
                default: 'pyarrow',
                options: ['pyarrow', 'fastparquet'],
                help: 'Parquet engine to use'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.parquet';
            const engine = data.engine || 'pyarrow';
            
            if (context) context.setVariable(node.id, varName);
            
            return `${varName} = pd.read_parquet('${filePath}', engine='${engine}')\nprint(f'Loaded Parquet: {${varName}.shape}')`;
        }
    },

    loadHDF5: {
        type: 'data_load_hdf5',
        name: 'Load HDF5',
        category: 'data-sources',
        icon: 'bi-file-earmark-binary-fill',
        color: '#0277bd',
        description: 'Load data from HDF5 file',
        defaults: {
            file_path: 'data/dataset.h5',
            variable_name: 'df',
            key: 'data'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.h5'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            }),
            BaseNode.createProperty('key', 'Key/Path', 'text', {
                default: 'data',
                help: 'HDF5 key/path to the dataset'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.h5';
            const key = data.key || 'data';
            
            if (context) context.setVariable(node.id, varName);
            
            return `${varName} = pd.read_hdf('${filePath}', key='${key}')\nprint(f'Loaded HDF5: {${varName}.shape}')`;
        }
    },

    loadPickle: {
        type: 'data_load_pickle',
        name: 'Load Pickle',
        category: 'data-sources',
        icon: 'bi-file-earmark-zip',
        color: '#e65100',
        description: 'Load data from Pickle file (Python serialization)',
        defaults: {
            file_path: 'data/dataset.pkl',
            variable_name: 'data'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.pkl'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'data'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'data';
            const filePath = data.file_path || 'data/dataset.pkl';
            
            if (context) context.setVariable(node.id, varName);
            
            return `import pickle\nwith open('${filePath}', 'rb') as f:\n    ${varName} = pickle.load(f)\nprint(f'Loaded Pickle: {type(${varName})}')`;
        }
    },

    loadFeather: {
        type: 'data_load_feather',
        name: 'Load Feather',
        category: 'data-sources',
        icon: 'bi-file-earmark-arrow-down',
        color: '#2e7d32',
        description: 'Load data from Feather file (fast binary format)',
        defaults: {
            file_path: 'data/dataset.feather',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.feather'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.feather';
            
            if (context) context.setVariable(node.id, varName);
            
            return `${varName} = pd.read_feather('${filePath}')\nprint(f'Loaded Feather: {${varName}.shape}')`;
        }
    },

    loadStata: {
        type: 'data_load_stata',
        name: 'Load Stata',
        category: 'data-sources',
        icon: 'bi-file-earmark-text',
        color: '#c2185b',
        description: 'Load data from Stata file (.dta)',
        defaults: {
            file_path: 'data/dataset.dta',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.dta'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.dta';
            
            if (context) context.setVariable(node.id, varName);
            
            return `${varName} = pd.read_stata('${filePath}')\nprint(f'Loaded Stata: {${varName}.shape}')`;
        }
    },

    loadSAS: {
        type: 'data_load_sas',
        name: 'Load SAS',
        category: 'data-sources',
        icon: 'bi-file-earmark-text-fill',
        color: '#7b1fa2',
        description: 'Load data from SAS file (.sas7bdat)',
        defaults: {
            file_path: 'data/dataset.sas7bdat',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.sas7bdat'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.sas7bdat';
            
            if (context) context.setVariable(node.id, varName);
            
            return `${varName} = pd.read_sas('${filePath}')\nprint(f'Loaded SAS: {${varName}.shape}')`;
        }
    },

    loadSPSS: {
        type: 'data_load_spss',
        name: 'Load SPSS',
        category: 'data-sources',
        icon: 'bi-file-earmark-spreadsheet-fill',
        color: '#ff6b6b',
        description: 'Load data from SPSS file (.sav)',
        defaults: {
            file_path: 'data/dataset.sav',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.sav'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.sav';
            
            if (context) context.setVariable(node.id, varName);
            
            return `import pyreadstat\n${varName}, meta = pyreadstat.read_sav('${filePath}')\nprint(f'Loaded SPSS: {${varName}.shape}')`;
        }
    },

    loadORC: {
        type: 'data_load_orc',
        name: 'Load ORC',
        category: 'data-sources',
        icon: 'bi-file-earmark-code-fill',
        color: '#4ecdc4',
        description: 'Load data from ORC file (Optimized Row Columnar)',
        defaults: {
            file_path: 'data/dataset.orc',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.orc'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.orc';
            
            if (context) context.setVariable(node.id, varName);
            
            return `${varName} = pd.read_orc('${filePath}')\nprint(f'Loaded ORC: {${varName}.shape}')`;
        }
    },

    loadAvro: {
        type: 'data_load_avro',
        name: 'Load Avro',
        category: 'data-sources',
        icon: 'bi-file-earmark-zip-fill',
        color: '#95a5a6',
        description: 'Load data from Avro file',
        defaults: {
            file_path: 'data/dataset.avro',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.avro'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.avro';
            
            if (context) context.setVariable(node.id, varName);
            
            return `from fastavro import reader\nimport pandas as pd\nrecords = []\nwith open('${filePath}', 'rb') as f:\n    avro_reader = reader(f)\n    for record in avro_reader:\n        records.append(record)\n${varName} = pd.DataFrame(records)\nprint(f'Loaded Avro: {${varName}.shape}')`;
        }
    },

    loadArrow: {
        type: 'data_load_arrow',
        name: 'Load Arrow',
        category: 'data-sources',
        icon: 'bi-arrow-down-circle',
        color: '#3498db',
        description: 'Load data from Apache Arrow file',
        defaults: {
            file_path: 'data/dataset.arrow',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/dataset.arrow'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/dataset.arrow';
            
            if (context) context.setVariable(node.id, varName);
            
            return `import pyarrow.parquet as pq\ntable = pq.read_table('${filePath}')\n${varName} = table.to_pandas()\nprint(f'Loaded Arrow: {${varName}.shape}')`;
        }
    }
};

// Register all file format nodes
Object.values(FileFormatNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FileFormatNodes;
}

