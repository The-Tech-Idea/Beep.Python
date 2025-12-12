/**
 * XML/HTML File Operations Nodes
 * XML and HTML parsing using lxml
 */

const XMLFileOperationsNodes = {
    readXML: {
        type: 'xml_read',
        name: 'Read XML',
        category: 'xml-file-operations',
        icon: 'bi-file-earmark-code',
        color: '#1976d2',
        description: 'Parse XML file using lxml',
        defaults: {
            file_path: 'data.xml',
            variable_name: 'root'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'data.xml'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'root'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'root';
            const filePath = data.file_path || 'data.xml';
            
            let code = 'from lxml import etree\n';
            code += `${varName} = etree.parse('${filePath}')\n`;
            code += `print(f'Loaded XML from {filePath}')\n`;
            code += `print(f'Root tag: {${varName}.getroot().tag}')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    readHTML: {
        type: 'xml_read_html',
        name: 'Read HTML',
        category: 'xml-file-operations',
        icon: 'bi-file-earmark-code',
        color: '#0277bd',
        description: 'Parse HTML file using lxml',
        defaults: {
            file_path: 'page.html',
            variable_name: 'tree'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                required: true,
                placeholder: 'page.html'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'tree'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'tree';
            const filePath = data.file_path || 'page.html';
            
            let code = 'from lxml import html\n';
            code += `with open('${filePath}', 'r', encoding='utf-8') as f:\n`;
            code += `    ${varName} = html.parse(f)\n`;
            code += `print(f'Loaded HTML from {filePath}')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    xpath: {
        type: 'xml_xpath',
        name: 'XPath Query',
        category: 'xml-file-operations',
        icon: 'bi-search',
        color: '#e65100',
        description: 'Extract data using XPath expression',
        defaults: {
            xpath: '',
            variable_name: 'root'
        },
        properties: [
            BaseNode.createProperty('variable_name', 'XML Variable', 'text', {
                required: true,
                default: 'root',
                help: 'Variable name of parsed XML/HTML'
            }),
            BaseNode.createProperty('xpath', 'XPath Expression', 'text', {
                required: true,
                placeholder: '//div[@class="content"]',
                help: 'XPath expression to query'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'root';
            const xpath = data.xpath || '';
            const outputVar = 'xpath_result';
            
            if (!xpath) return `# XPath: Expression required`;
            
            let code = `result = ${varName}.xpath('${xpath}')\n`;
            code += `print(f'XPath query returned {len(result)} elements')\n`;
            code += `print(result[:5] if len(result) > 5 else result)\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all XML nodes
Object.values(XMLFileOperationsNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = XMLFileOperationsNodes;
}

