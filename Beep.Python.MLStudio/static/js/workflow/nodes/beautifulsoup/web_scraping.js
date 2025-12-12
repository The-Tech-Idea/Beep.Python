/**
 * BeautifulSoup Web Scraping Nodes
 * Web scraping and HTML parsing
 */

const BeautifulSoupNodes = {
    parseHTML: {
        type: 'bs4_parse_html',
        name: 'Parse HTML (BeautifulSoup)',
        category: 'beautifulsoup-web-scraping',
        icon: 'bi-globe',
        color: '#28a745',
        description: 'Parse HTML content using BeautifulSoup',
        defaults: {
            html_source: 'file',
            file_path: 'page.html',
            url: '',
            variable_name: 'soup',
            parser: 'html.parser'
        },
        properties: [
            BaseNode.createProperty('html_source', 'HTML Source', 'select', {
                default: 'file',
                options: ['file', 'url', 'variable'],
                help: 'Source of HTML content'
            }),
            BaseNode.createProperty('file_path', 'File Path', 'text', {
                placeholder: 'page.html',
                help: 'Path to HTML file (if source is file)'
            }),
            BaseNode.createProperty('url', 'URL', 'text', {
                placeholder: 'https://example.com',
                help: 'URL to fetch (if source is url)'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'soup'
            }),
            BaseNode.createProperty('parser', 'Parser', 'select', {
                default: 'html.parser',
                options: ['html.parser', 'lxml', 'html5lib'],
                help: 'HTML parser to use'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'soup';
            const htmlSource = data.html_source || 'file';
            const filePath = data.file_path || 'page.html';
            const url = data.url || '';
            const parser = data.parser || 'html.parser';
            
            let code = 'from bs4 import BeautifulSoup\n';
            
            if (htmlSource === 'url') {
                code += 'import requests\n';
                code += `response = requests.get('${url}')\n`;
                code += `${varName} = BeautifulSoup(response.content, '${parser}')\n`;
            } else if (htmlSource === 'file') {
                code += `with open('${filePath}', 'r', encoding='utf-8') as f:\n`;
                code += `    ${varName} = BeautifulSoup(f.read(), '${parser}')\n`;
            } else {
                code += `# Assuming html_content variable exists\n`;
                code += `${varName} = BeautifulSoup(html_content, '${parser}')\n`;
            }
            
            code += `print(f'Parsed HTML with BeautifulSoup using {parser} parser')\n`;
            code += `print(f'Title: {${varName}.title.string if ${varName}.title else "No title"}')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    findElements: {
        type: 'bs4_find',
        name: 'Find Elements',
        category: 'beautifulsoup-web-scraping',
        icon: 'bi-search',
        color: '#20c997',
        description: 'Find elements by tag, class, or ID',
        defaults: {
            tag: '',
            class_name: '',
            id: '',
            variable_name: 'soup'
        },
        properties: [
            BaseNode.createProperty('variable_name', 'Soup Variable', 'text', {
                required: true,
                default: 'soup',
                help: 'Variable name of BeautifulSoup object'
            }),
            BaseNode.createProperty('tag', 'Tag Name', 'text', {
                placeholder: 'div, p, a',
                help: 'HTML tag name'
            }),
            BaseNode.createProperty('class_name', 'Class Name', 'text', {
                placeholder: 'content',
                help: 'CSS class name'
            }),
            BaseNode.createProperty('id', 'ID', 'text', {
                placeholder: 'main',
                help: 'Element ID'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'soup';
            const tag = data.tag || '';
            const className = data.class_name || '';
            const id = data.id || '';
            const outputVar = 'elements';
            
            let code = '';
            const params = [];
            
            if (tag) {
                params.push(`'${tag}'`);
            }
            
            const attrs = {};
            if (className) attrs['class'] = className;
            if (id) attrs['id'] = id;
            
            if (Object.keys(attrs).length > 0) {
                const attrsStr = JSON.stringify(attrs).replace(/"/g, "'");
                if (tag) {
                    code += `${outputVar} = ${varName}.find_all(${params[0]}, attrs=${attrsStr})\n`;
                } else {
                    code += `${outputVar} = ${varName}.find_all(attrs=${attrsStr})\n`;
                }
            } else if (tag) {
                code += `${outputVar} = ${varName}.find_all(${params[0]})\n`;
            } else {
                code += `# Find: Specify at least tag, class, or id\n`;
                return code;
            }
            
            code += `print(f'Found {len(${outputVar})} elements')\n`;
            code += `print(${outputVar}[:3] if len(${outputVar}) > 3 else ${outputVar})\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    },

    extractText: {
        type: 'bs4_extract_text',
        name: 'Extract Text',
        category: 'beautifulsoup-web-scraping',
        icon: 'bi-text-left',
        color: '#17a2b8',
        description: 'Extract text content from elements',
        defaults: {
            variable_name: 'elements',
            strip: true
        },
        properties: [
            BaseNode.createProperty('variable_name', 'Elements Variable', 'text', {
                required: true,
                default: 'elements',
                help: 'Variable name of found elements'
            }),
            BaseNode.createProperty('strip', 'Strip Whitespace', 'boolean', {
                default: true,
                help: 'Remove leading/trailing whitespace'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'elements';
            const outputVar = varName + '_text';
            const strip = data.strip !== false;
            
            let code = `texts = [elem.get_text(strip=${strip}) for elem in ${varName}]\n`;
            code += `print(f'Extracted {len(texts)} text elements')\n`;
            code += `print(texts[:5] if len(texts) > 5 else texts)\n`;
            
            if (context) {
                context.setVariable(node.id, 'texts');
            }
            
            return code;
        }
    },

    extractLinks: {
        type: 'bs4_extract_links',
        name: 'Extract Links',
        category: 'beautifulsoup-web-scraping',
        icon: 'bi-link-45deg',
        color: '#6f42c1',
        description: 'Extract all links (href attributes) from HTML',
        defaults: {
            variable_name: 'soup'
        },
        properties: [
            BaseNode.createProperty('variable_name', 'Soup Variable', 'text', {
                required: true,
                default: 'soup',
                help: 'Variable name of BeautifulSoup object'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'soup';
            const outputVar = 'links';
            
            let code = `links = [a.get('href') for a in ${varName}.find_all('a', href=True)]\n`;
            code += `print(f'Found {len(links)} links')\n`;
            code += `print(links[:10] if len(links) > 10 else links)\n`;
            
            if (context) {
                context.setVariable(node.id, outputVar);
            }
            
            return code;
        }
    }
};

// Register all BeautifulSoup nodes
Object.values(BeautifulSoupNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = BeautifulSoupNodes;
}

