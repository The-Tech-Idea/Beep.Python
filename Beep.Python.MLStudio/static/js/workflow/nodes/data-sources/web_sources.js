/**
 * Web-Based Data Loading Nodes
 * Load data from web sources, APIs, and online services
 */

const WebSourceNodes = {
    loadGoogleSheets: {
        type: 'data_load_google_sheets',
        name: 'Load from Google Sheets',
        category: 'data-sources',
        icon: 'bi-google',
        color: '#4285f4',
        description: 'Load data from Google Sheets',
        defaults: {
            sheet_id: '',
            sheet_name: 'Sheet1',
            credentials_path: '',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('sheet_id', 'Sheet ID', 'text', {
                required: true,
                placeholder: '1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms',
                help: 'Google Sheets ID from the URL'
            }),
            BaseNode.createProperty('sheet_name', 'Sheet Name', 'text', {
                default: 'Sheet1',
                placeholder: 'Sheet1'
            }),
            BaseNode.createProperty('credentials_path', 'Credentials JSON Path', 'text', {
                placeholder: 'path/to/credentials.json',
                help: 'Path to Google API credentials (optional for public sheets)'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const sheetId = data.sheet_id || '';
            const sheetName = data.sheet_name || 'Sheet1';
            const credsPath = data.credentials_path || '';
            
            if (context) context.setVariable(node.id, varName);
            
            let code = '';
            if (credsPath) {
                code += `import gspread\nfrom oauth2client.service_account import ServiceAccountCredentials\n`;
                code += `scope = ['https://spreadsheets.google.com/feeds']\n`;
                code += `creds = ServiceAccountCredentials.from_json_keyfile_name('${credsPath}', scope)\n`;
                code += `client = gspread.authorize(creds)\n`;
                code += `sheet = client.open_by_key('${sheetId}').worksheet('${sheetName}')\n`;
                code += `${varName} = pd.DataFrame(sheet.get_all_records())\n`;
            } else {
                code += `# Public Google Sheet (no authentication)\n`;
                code += `url = f'https://docs.google.com/spreadsheets/d/${sheetId}/gviz/tq?tqx=out:csv&sheet=${sheetName}'\n`;
                code += `${varName} = pd.read_csv(url)\n`;
            }
            code += `print(f'Loaded from Google Sheets: {${varName}.shape}')\n`;
            
            return code;
        }
    },

    loadBigQuery: {
        type: 'data_load_bigquery',
        name: 'Load from BigQuery',
        category: 'data-sources',
        icon: 'bi-cloud-arrow-up',
        color: '#4285f4',
        description: 'Load data from Google BigQuery',
        defaults: {
            project_id: 'my-project',
            query: 'SELECT * FROM `dataset.table` LIMIT 1000',
            credentials_path: '',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('project_id', 'Project ID', 'text', {
                required: true,
                placeholder: 'my-project'
            }),
            BaseNode.createProperty('query', 'SQL Query', 'text', {
                required: true,
                default: 'SELECT * FROM `dataset.table` LIMIT 1000',
                placeholder: 'SELECT * FROM `dataset.table`'
            }),
            BaseNode.createProperty('credentials_path', 'Credentials JSON Path', 'text', {
                placeholder: 'path/to/credentials.json',
                help: 'Path to BigQuery credentials JSON file'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const projectId = data.project_id || 'my-project';
            const query = data.query || 'SELECT * FROM `dataset.table` LIMIT 1000';
            const credsPath = data.credentials_path || '';
            
            if (context) context.setVariable(node.id, varName);
            
            let code = `from google.cloud import bigquery\n`;
            if (credsPath) {
                code += `client = bigquery.Client.from_service_account_json('${credsPath}', project='${projectId}')\n`;
            } else {
                code += `client = bigquery.Client(project='${projectId}')\n`;
            }
            code += `${varName} = client.query('''${query}''').to_dataframe()\n`;
            code += `print(f'Loaded from BigQuery: {${varName}.shape}')\n`;
            
            return code;
        }
    },

    loadURL: {
        type: 'data_load_url',
        name: 'Load from URL',
        category: 'data-sources',
        icon: 'bi-link-45deg',
        color: '#007bff',
        description: 'Load data from a URL (CSV, JSON, Excel, etc.)',
        defaults: {
            url: 'https://example.com/data.csv',
            variable_name: 'df',
            file_format: 'csv'
        },
        properties: [
            BaseNode.createProperty('url', 'URL', 'text', {
                required: true,
                placeholder: 'https://example.com/data.csv'
            }),
            BaseNode.createProperty('file_format', 'File Format', 'select', {
                default: 'csv',
                options: ['csv', 'json', 'excel', 'parquet'],
                help: 'Format of the data at URL'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const url = data.url || 'https://example.com/data.csv';
            const fileFormat = data.file_format || 'csv';
            
            if (context) context.setVariable(node.id, varName);
            
            let code = '';
            if (fileFormat === 'csv') {
                code += `${varName} = pd.read_csv('${url}')\n`;
            } else if (fileFormat === 'json') {
                code += `${varName} = pd.read_json('${url}')\n`;
            } else if (fileFormat === 'excel') {
                code += `${varName} = pd.read_excel('${url}')\n`;
            } else {
                code += `${varName} = pd.read_parquet('${url}')\n`;
            }
            code += `print(f'Loaded from URL: {${varName}.shape}')\n`;
            
            return code;
        }
    },

    loadAPIData: {
        type: 'data_load_api',
        name: 'Load from REST API',
        category: 'data-sources',
        icon: 'bi-cloud-download',
        color: '#17a2b8',
        description: 'Load data from REST API endpoint',
        defaults: {
            url: 'https://api.example.com/data',
            method: 'GET',
            headers: '',
            params: '',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('url', 'API URL', 'text', {
                required: true,
                placeholder: 'https://api.example.com/data'
            }),
            BaseNode.createProperty('method', 'HTTP Method', 'select', {
                default: 'GET',
                options: ['GET', 'POST']
            }),
            BaseNode.createProperty('headers', 'Headers (JSON)', 'text', {
                placeholder: '{"Authorization": "Bearer token"}',
                help: 'JSON string of headers'
            }),
            BaseNode.createProperty('params', 'Parameters (JSON)', 'text', {
                placeholder: '{"page": 1, "limit": 100}',
                help: 'JSON string of query parameters'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const url = data.url || 'https://api.example.com/data';
            const method = data.method || 'GET';
            const headers = data.headers || '';
            const params = data.params || '';
            
            if (context) context.setVariable(node.id, varName);
            
            let code = `import requests\nimport json\n`;
            if (method === 'GET') {
                code += `response = requests.get('${url}'`;
                if (params) {
                    code += `, params=json.loads('${params}')`;
                }
                if (headers) {
                    code += `, headers=json.loads('${headers}')`;
                }
                code += `)\n`;
            } else {
                code += `response = requests.post('${url}'`;
                if (params) {
                    code += `, json=json.loads('${params}')`;
                }
                if (headers) {
                    code += `, headers=json.loads('${headers}')`;
                }
                code += `)\n`;
            }
            code += `response.raise_for_status()\n`;
            code += `data = response.json()\n`;
            code += `${varName} = pd.DataFrame(data) if isinstance(data, list) else pd.json_normalize(data)\n`;
            code += `print(f'Loaded from API: {${varName}.shape}')\n`;
            
            return code;
        }
    }
};

// Register all web source nodes
Object.values(WebSourceNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = WebSourceNodes;
}

