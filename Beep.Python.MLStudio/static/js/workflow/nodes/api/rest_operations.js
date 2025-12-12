/**
 * API Nodes
 * REST API calls and data fetching
 */

const APINodes = {
    httpGet: {
        type: 'api_http_get',
        name: 'HTTP GET Request',
        category: 'api',
        icon: 'bi-download',
        color: '#007bff',
        description: 'Make HTTP GET request to fetch data',
        defaults: {
            url: 'https://api.example.com/data',
            headers: '',
            variable_name: 'response'
        },
        properties: [
            BaseNode.createProperty('url', 'URL', 'text', {
                required: true,
                placeholder: 'https://api.example.com/data'
            }),
            BaseNode.createProperty('headers', 'Headers', 'text', {
                placeholder: 'Authorization: Bearer token',
                help: 'Comma-separated header:value pairs'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'response'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const url = data.url || '';
            const headers = data.headers || '';
            const varName = data.variable_name || 'response';
            
            if (!url) return `# HTTP GET: URL required`;
            
            let code = 'import requests\n';
            
            const params = {};
            if (headers) {
                const headerList = headers.split(',').map(h => h.trim());
                headerList.forEach(header => {
                    const [key, value] = header.split(':').map(s => s.trim());
                    params[key] = value;
                });
            }
            
            if (Object.keys(params).length > 0) {
                const headersStr = JSON.stringify(params).replace(/"/g, "'");
                code += `${varName} = requests.get('${url}', headers=${headersStr})\n`;
            } else {
                code += `${varName} = requests.get('${url}')\n`;
            }
            
            code += `${varName}.raise_for_status()  # Raise exception for bad status codes\n`;
            code += `print(f'GET request successful: {${varName}.status_code}')\n`;
            code += `print(f'Response: {${varName}.text[:200]}...')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    httpPost: {
        type: 'api_http_post',
        name: 'HTTP POST Request',
        category: 'api',
        icon: 'bi-upload',
        color: '#28a745',
        description: 'Make HTTP POST request to send data',
        defaults: {
            url: 'https://api.example.com/submit',
            data: '',
            json: true,
            headers: ''
        },
        properties: [
            BaseNode.createProperty('url', 'URL', 'text', {
                required: true,
                placeholder: 'https://api.example.com/submit'
            }),
            BaseNode.createProperty('data', 'Data/JSON', 'text', {
                placeholder: '{"key": "value"}',
                help: 'Data to send (JSON string or variable name)'
            }),
            BaseNode.createProperty('json', 'Send as JSON', 'boolean', {
                default: true,
                help: 'Send data as JSON (True) or form data (False)'
            }),
            BaseNode.createProperty('headers', 'Headers', 'text', {
                placeholder: 'Content-Type: application/json'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const url = data.url || '';
            const postData = data.data || '';
            const asJson = data.json !== false;
            const headers = data.headers || '';
            
            if (!url) return `# HTTP POST: URL required`;
            
            let code = 'import requests\n';
            code += 'import json\n';
            
            const params = {};
            if (headers) {
                const headerList = headers.split(',').map(h => h.trim());
                headerList.forEach(header => {
                    const [key, value] = header.split(':').map(s => s.trim());
                    params[key] = value;
                });
            }
            
            code += `\n# Prepare data\n`;
            if (postData) {
                if (postData.startsWith('{') || postData.startsWith('[')) {
                    code += `payload = ${postData}\n`;
                } else {
                    code += `payload = ${postData}  # Assuming variable name\n`;
                }
            } else {
                code += `payload = {}  # Empty payload\n`;
            }
            
            const headersStr = Object.keys(params).length > 0 ? `, headers=${JSON.stringify(params).replace(/"/g, "'")}` : '';
            const varName = 'response';
            
            if (asJson) {
                code += `${varName} = requests.post('${url}', json=payload${headersStr})\n`;
            } else {
                code += `${varName} = requests.post('${url}', data=payload${headersStr})\n`;
            }
            
            code += `${varName}.raise_for_status()\n`;
            code += `print(f'POST request successful: {${varName}.status_code}')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    parseJSON: {
        type: 'api_parse_json',
        name: 'Parse JSON Response',
        category: 'api',
        icon: 'bi-code-slash',
        color: '#17a2b8',
        description: 'Parse JSON response from API',
        defaults: {
            response_variable: 'response',
            variable_name: 'data'
        },
        properties: [
            BaseNode.createProperty('response_variable', 'Response Variable', 'text', {
                required: true,
                default: 'response',
                help: 'Variable name of requests.Response object'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'data'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const responseVar = data.response_variable || 'response';
            const varName = data.variable_name || 'data';
            
            let code = 'import json\n';
            code += `${varName} = ${responseVar}.json()\n`;
            code += `print(f'Parsed JSON response: {type(${varName})}')\n`;
            code += `print(${varName})\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    }
};

// Register all API nodes
Object.values(APINodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = APINodes;
}

