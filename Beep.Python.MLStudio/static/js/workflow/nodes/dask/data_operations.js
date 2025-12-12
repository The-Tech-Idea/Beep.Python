/**
 * Dask Data Operations Nodes
 * Parallel computing for large datasets
 */

const DaskDataOperationsNodes = {
    readCSV: {
        type: 'dask_read_csv',
        name: 'Read CSV (Dask)',
        category: 'dask-data-operations',
        icon: 'bi-file-earmark-spreadsheet',
        color: '#ff6b00',
        description: 'Read CSV file using Dask (for large files)',
        defaults: {
            file_path: 'data/your_dataset.csv',
            variable_name: 'df',
            blocksize: null
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/large_dataset.csv'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            }),
            BaseNode.createProperty('blocksize', 'Block Size (MB)', 'number', {
                default: null,
                placeholder: '64 or leave empty',
                help: 'Size of each partition in MB (None for auto)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/your_dataset.csv';
            const blocksize = data.blocksize !== null && data.blocksize !== undefined ? data.blocksize : null;
            
            let code = 'import dask.dataframe as dd\n';
            const params = [];
            if (blocksize !== null) {
                params.push(`blocksize=${blocksize * 1024 * 1024}`); // Convert MB to bytes
            }
            
            code += `${varName} = dd.read_csv('${filePath}'${params.length > 0 ? ', ' + params.join(', ') : ''})\n`;
            code += `print(f'Loaded Dask DataFrame with {len(${varName})} partitions')\n`;
            code += `print(f'Columns: {list(${varName}.columns)}')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    readParquet: {
        type: 'dask_read_parquet',
        name: 'Read Parquet (Dask)',
        category: 'dask-data-operations',
        icon: 'bi-file-earmark-binary',
        color: '#ff8800',
        description: 'Read Parquet file using Dask',
        defaults: {
            file_path: 'data/your_dataset.parquet',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data/large_dataset.parquet'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const filePath = data.file_path || 'data/your_dataset.parquet';
            
            let code = 'import dask.dataframe as dd\n';
            code += `${varName} = dd.read_parquet('${filePath}')\n`;
            code += `print(f'Loaded Dask DataFrame with {len(${varName})} partitions')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    compute: {
        type: 'dask_compute',
        name: 'Compute (Dask)',
        category: 'dask-data-operations',
        icon: 'bi-cpu',
        color: '#ff6b00',
        description: 'Compute Dask DataFrame to pandas DataFrame',
        defaults: {},
        properties: [],
        generateCode: (node, context) => {
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_computed';
            
            let code = `import pandas as pd\n`;
            code += `${outputVar} = ${inputVar}.compute()\n`;
            code += `print(f'Computed to pandas DataFrame: {${outputVar}.shape}')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    persist: {
        type: 'dask_persist',
        name: 'Persist (Dask)',
        category: 'dask-data-operations',
        icon: 'bi-hdd',
        color: '#ff8800',
        description: 'Persist Dask DataFrame in memory',
        defaults: {},
        properties: [],
        generateCode: (node, context) => {
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_persisted';
            
            let code = `${outputVar} = ${inputVar}.persist()\n`;
            code += `print('DataFrame persisted in memory')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    repartition: {
        type: 'dask_repartition',
        name: 'Repartition (Dask)',
        category: 'dask-data-operations',
        icon: 'bi-grid-3x3',
        color: '#ff6b00',
        description: 'Repartition Dask DataFrame',
        defaults: {
            npartitions: null,
            partition_size: null
        },
        properties: [
            BaseNode.createProperty('npartitions', 'Number of Partitions', 'number', {
                default: null,
                placeholder: '4 or leave empty',
                help: 'Desired number of partitions'
            }),
            BaseNode.createProperty('partition_size', 'Partition Size (MB)', 'number', {
                default: null,
                placeholder: '128 or leave empty',
                help: 'Desired size of each partition in MB'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const outputVar = inputVar + '_repartitioned';
            const npartitions = data.npartitions !== null && data.npartitions !== undefined ? data.npartitions : null;
            const partitionSize = data.partition_size !== null && data.partition_size !== undefined ? data.partition_size : null;
            
            const params = [];
            if (npartitions !== null) {
                params.push(`npartitions=${npartitions}`);
            } else if (partitionSize !== null) {
                params.push(`partition_size='${partitionSize}MB'`);
            }
            
            let code = `${outputVar} = ${inputVar}.repartition(${params.length > 0 ? params.join(', ') : ''})\n`;
            code += `print(f'Repartitioned to {len(${outputVar})} partitions')\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all dask nodes
Object.values(DaskDataOperationsNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DaskDataOperationsNodes;
}

