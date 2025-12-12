/**
 * Cloud Storage Data Loading Nodes
 * Load data from cloud storage services
 */

const CloudStorageNodes = {
    loadS3: {
        type: 'data_load_s3',
        name: 'Load from S3',
        category: 'data-sources',
        icon: 'bi-cloud-download',
        color: '#ff9900',
        description: 'Load data from AWS S3 bucket',
        defaults: {
            bucket: 'my-bucket',
            key: 'data/dataset.csv',
            aws_access_key_id: '',
            aws_secret_access_key: '',
            variable_name: 'df',
            file_format: 'csv'
        },
        properties: [
            BaseNode.createProperty('bucket', 'Bucket Name', 'text', {
                required: true,
                placeholder: 'my-bucket'
            }),
            BaseNode.createProperty('key', 'Object Key/Path', 'text', {
                required: true,
                placeholder: 'data/dataset.csv'
            }),
            BaseNode.createProperty('aws_access_key_id', 'AWS Access Key ID', 'text', {
                placeholder: 'AKIAIOSFODNN7EXAMPLE'
            }),
            BaseNode.createProperty('aws_secret_access_key', 'AWS Secret Access Key', 'text', {
                placeholder: 'wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY',
                inputType: 'password'
            }),
            BaseNode.createProperty('file_format', 'File Format', 'select', {
                default: 'csv',
                options: ['csv', 'parquet', 'json', 'excel', 'pickle'],
                help: 'Format of the file in S3'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const bucket = data.bucket || 'my-bucket';
            const key = data.key || 'data/dataset.csv';
            const accessKey = data.aws_access_key_id || '';
            const secretKey = data.aws_secret_access_key || '';
            const fileFormat = data.file_format || 'csv';
            
            if (context) context.setVariable(node.id, varName);
            
            let code = `import boto3\nfrom io import BytesIO\n`;
            if (accessKey && secretKey) {
                code += `s3 = boto3.client('s3', aws_access_key_id='${accessKey}', aws_secret_access_key='${secretKey}')\n`;
            } else {
                code += `s3 = boto3.client('s3')\n`;
            }
            code += `obj = s3.get_object(Bucket='${bucket}', Key='${key}')\n`;
            
            if (fileFormat === 'csv') {
                code += `${varName} = pd.read_csv(BytesIO(obj['Body'].read()))\n`;
            } else if (fileFormat === 'parquet') {
                code += `${varName} = pd.read_parquet(BytesIO(obj['Body'].read()))\n`;
            } else if (fileFormat === 'json') {
                code += `${varName} = pd.read_json(BytesIO(obj['Body'].read()))\n`;
            } else if (fileFormat === 'excel') {
                code += `${varName} = pd.read_excel(BytesIO(obj['Body'].read()))\n`;
            } else {
                code += `import pickle\n${varName} = pickle.load(BytesIO(obj['Body'].read()))\n`;
            }
            code += `print(f'Loaded from S3: {${varName}.shape}')\n`;
            
            return code;
        }
    },

    loadGoogleCloudStorage: {
        type: 'data_load_gcs',
        name: 'Load from Google Cloud Storage',
        category: 'data-sources',
        icon: 'bi-cloud-download-fill',
        color: '#4285f4',
        description: 'Load data from Google Cloud Storage bucket',
        defaults: {
            bucket: 'my-bucket',
            blob_name: 'data/dataset.csv',
            credentials_path: '',
            variable_name: 'df',
            file_format: 'csv'
        },
        properties: [
            BaseNode.createProperty('bucket', 'Bucket Name', 'text', {
                required: true,
                placeholder: 'my-bucket'
            }),
            BaseNode.createProperty('blob_name', 'Blob Name/Path', 'text', {
                required: true,
                placeholder: 'data/dataset.csv'
            }),
            BaseNode.createProperty('credentials_path', 'Credentials JSON Path', 'text', {
                placeholder: 'path/to/credentials.json',
                help: 'Path to GCS credentials JSON file (optional if using default)'
            }),
            BaseNode.createProperty('file_format', 'File Format', 'select', {
                default: 'csv',
                options: ['csv', 'parquet', 'json', 'excel']
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const bucket = data.bucket || 'my-bucket';
            const blobName = data.blob_name || 'data/dataset.csv';
            const credsPath = data.credentials_path || '';
            const fileFormat = data.file_format || 'csv';
            
            if (context) context.setVariable(node.id, varName);
            
            let code = `from google.cloud import storage\nfrom io import BytesIO\n`;
            if (credsPath) {
                code += `storage_client = storage.Client.from_service_account_json('${credsPath}')\n`;
            } else {
                code += `storage_client = storage.Client()\n`;
            }
            code += `bucket_obj = storage_client.bucket('${bucket}')\n`;
            code += `blob = bucket_obj.blob('${blobName}')\n`;
            code += `data_bytes = blob.download_as_bytes()\n`;
            
            if (fileFormat === 'csv') {
                code += `${varName} = pd.read_csv(BytesIO(data_bytes))\n`;
            } else if (fileFormat === 'parquet') {
                code += `${varName} = pd.read_parquet(BytesIO(data_bytes))\n`;
            } else if (fileFormat === 'json') {
                code += `${varName} = pd.read_json(BytesIO(data_bytes))\n`;
            } else {
                code += `${varName} = pd.read_excel(BytesIO(data_bytes))\n`;
            }
            code += `print(f'Loaded from GCS: {${varName}.shape}')\n`;
            
            return code;
        }
    },

    loadAzureBlob: {
        type: 'data_load_azure',
        name: 'Load from Azure Blob Storage',
        category: 'data-sources',
        icon: 'bi-cloud-arrow-down',
        color: '#0078d4',
        description: 'Load data from Azure Blob Storage',
        defaults: {
            account_name: 'myaccount',
            container: 'mycontainer',
            blob_name: 'data/dataset.csv',
            account_key: '',
            variable_name: 'df',
            file_format: 'csv'
        },
        properties: [
            BaseNode.createProperty('account_name', 'Account Name', 'text', {
                required: true,
                placeholder: 'myaccount'
            }),
            BaseNode.createProperty('container', 'Container Name', 'text', {
                required: true,
                placeholder: 'mycontainer'
            }),
            BaseNode.createProperty('blob_name', 'Blob Name/Path', 'text', {
                required: true,
                placeholder: 'data/dataset.csv'
            }),
            BaseNode.createProperty('account_key', 'Account Key', 'text', {
                placeholder: 'account-key',
                inputType: 'password'
            }),
            BaseNode.createProperty('file_format', 'File Format', 'select', {
                default: 'csv',
                options: ['csv', 'parquet', 'json', 'excel']
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const accountName = data.account_name || 'myaccount';
            const container = data.container || 'mycontainer';
            const blobName = data.blob_name || 'data/dataset.csv';
            const accountKey = data.account_key || '';
            const fileFormat = data.file_format || 'csv';
            
            if (context) context.setVariable(node.id, varName);
            
            let code = `from azure.storage.blob import BlobServiceClient\nfrom io import BytesIO\n`;
            if (accountKey) {
                code += `blob_service = BlobServiceClient(account_url=f'https://${accountName}.blob.core.windows.net', credential='${accountKey}')\n`;
            } else {
                code += `# Using default Azure credentials\n`;
                code += `blob_service = BlobServiceClient(account_url=f'https://${accountName}.blob.core.windows.net')\n`;
            }
            code += `blob_client = blob_service.get_blob_client(container='${container}', blob='${blobName}')\n`;
            code += `data_bytes = blob_client.download_blob().readall()\n`;
            
            if (fileFormat === 'csv') {
                code += `${varName} = pd.read_csv(BytesIO(data_bytes))\n`;
            } else if (fileFormat === 'parquet') {
                code += `${varName} = pd.read_parquet(BytesIO(data_bytes))\n`;
            } else if (fileFormat === 'json') {
                code += `${varName} = pd.read_json(BytesIO(data_bytes))\n`;
            } else {
                code += `${varName} = pd.read_excel(BytesIO(data_bytes))\n`;
            }
            code += `print(f'Loaded from Azure: {${varName}.shape}')\n`;
            
            return code;
        }
    }
};

// Register all cloud storage nodes
Object.values(CloudStorageNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CloudStorageNodes;
}

