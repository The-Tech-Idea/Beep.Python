/**
 * Visualization Nodes
 * Plotting and data visualization using matplotlib and seaborn
 */

const VisualizationNodes = {
    scatterPlot: {
        type: 'viz_scatter',
        name: 'Scatter Plot',
        category: 'visualization',
        icon: 'bi-graph-up',
        color: '#1976d2',
        description: 'Create a scatter plot',
        defaults: {
            x: '',
            y: '',
            title: 'Scatter Plot',
            xlabel: '',
            ylabel: '',
            figsize: [10, 6]
        },
        properties: [
            BaseNode.createProperty('x', 'X Column', 'text', {
                required: true,
                placeholder: 'x_column'
            }),
            BaseNode.createProperty('y', 'Y Column', 'text', {
                required: true,
                placeholder: 'y_column'
            }),
            BaseNode.createProperty('title', 'Title', 'text', {
                default: 'Scatter Plot'
            }),
            BaseNode.createProperty('xlabel', 'X Label', 'text', {
                placeholder: 'X Axis Label'
            }),
            BaseNode.createProperty('ylabel', 'Y Label', 'text', {
                placeholder: 'Y Axis Label'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const x = data.x || '';
            const y = data.y || '';
            const title = data.title || 'Scatter Plot';
            const xlabel = data.xlabel || x;
            const ylabel = data.ylabel || y;
            
            if (!x || !y) return `# Scatter Plot: X and Y columns required`;
            
            let code = 'import matplotlib.pyplot as plt\n';
            code += `plt.figure(figsize=(10, 6))\n`;
            code += `plt.scatter(${inputVar}['${x}'], ${inputVar}['${y}'])\n`;
            code += `plt.title('${title}')\n`;
            code += `plt.xlabel('${xlabel}')\n`;
            code += `plt.ylabel('${ylabel}')\n`;
            code += `plt.grid(True, alpha=0.3)\n`;
            code += `plt.tight_layout()\n`;
            code += `plt.show()\n`;
            
            return code;
        }
    },

    linePlot: {
        type: 'viz_line',
        name: 'Line Plot',
        category: 'visualization',
        icon: 'bi-graph-up-arrow',
        color: '#0277bd',
        description: 'Create a line plot',
        defaults: {
            x: '',
            y: '',
            title: 'Line Plot'
        },
        properties: [
            BaseNode.createProperty('x', 'X Column', 'text', {
                required: true,
                placeholder: 'x_column'
            }),
            BaseNode.createProperty('y', 'Y Column', 'text', {
                required: true,
                placeholder: 'y_column'
            }),
            BaseNode.createProperty('title', 'Title', 'text', {
                default: 'Line Plot'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const x = data.x || '';
            const y = data.y || '';
            const title = data.title || 'Line Plot';
            
            if (!x || !y) return `# Line Plot: X and Y columns required`;
            
            let code = 'import matplotlib.pyplot as plt\n';
            code += `plt.figure(figsize=(10, 6))\n`;
            code += `plt.plot(${inputVar}['${x}'], ${inputVar}['${y}'])\n`;
            code += `plt.title('${title}')\n`;
            code += `plt.xlabel('${x}')\n`;
            code += `plt.ylabel('${y}')\n`;
            code += `plt.grid(True, alpha=0.3)\n`;
            code += `plt.tight_layout()\n`;
            code += `plt.show()\n`;
            
            return code;
        }
    },

    histogram: {
        type: 'viz_histogram',
        name: 'Histogram',
        category: 'visualization',
        icon: 'bi-bar-chart',
        color: '#e65100',
        description: 'Create a histogram',
        defaults: {
            column: '',
            bins: 30,
            title: 'Histogram'
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'column_name'
            }),
            BaseNode.createProperty('bins', 'Number of Bins', 'number', {
                default: 30,
                min: 5,
                max: 100
            }),
            BaseNode.createProperty('title', 'Title', 'text', {
                default: 'Histogram'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const bins = data.bins || 30;
            const title = data.title || 'Histogram';
            
            if (!column) return `# Histogram: Column required`;
            
            let code = 'import matplotlib.pyplot as plt\n';
            code += `plt.figure(figsize=(10, 6))\n`;
            code += `plt.hist(${inputVar}['${column}'], bins=${bins}, edgecolor='black', alpha=0.7)\n`;
            code += `plt.title('${title}')\n`;
            code += `plt.xlabel('${column}')\n`;
            code += `plt.ylabel('Frequency')\n`;
            code += `plt.grid(True, alpha=0.3)\n`;
            code += `plt.tight_layout()\n`;
            code += `plt.show()\n`;
            
            return code;
        }
    },

    boxPlot: {
        type: 'viz_boxplot',
        name: 'Box Plot',
        category: 'visualization',
        icon: 'bi-layout-three-columns',
        color: '#2e7d32',
        description: 'Create a box plot',
        defaults: {
            column: '',
            title: 'Box Plot'
        },
        properties: [
            BaseNode.createProperty('column', 'Column', 'text', {
                required: true,
                placeholder: 'column_name'
            }),
            BaseNode.createProperty('title', 'Title', 'text', {
                default: 'Box Plot'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const column = data.column || '';
            const title = data.title || 'Box Plot';
            
            if (!column) return `# Box Plot: Column required`;
            
            let code = 'import matplotlib.pyplot as plt\n';
            code += `plt.figure(figsize=(8, 6))\n`;
            code += `plt.boxplot(${inputVar}['${column}'])\n`;
            code += `plt.title('${title}')\n`;
            code += `plt.ylabel('${column}')\n`;
            code += `plt.grid(True, alpha=0.3)\n`;
            code += `plt.tight_layout()\n`;
            code += `plt.show()\n`;
            
            return code;
        }
    },

    correlationMatrix: {
        type: 'viz_correlation',
        name: 'Correlation Matrix',
        category: 'visualization',
        icon: 'bi-grid-3x3',
        color: '#7b1fa2',
        description: 'Create a correlation heatmap',
        defaults: {},
        properties: [],
        generateCode: (node, context) => {
            const inputVar = context ? context.getInputVariable(node) : 'df';
            
            let code = 'import matplotlib.pyplot as plt\n';
            code += 'import seaborn as sns\n';
            code += `plt.figure(figsize=(10, 8))\n`;
            code += `corr = ${inputVar}.select_dtypes(include=['float64', 'int64']).corr()\n`;
            code += `sns.heatmap(corr, annot=True, fmt='.2f', cmap='coolwarm', center=0, square=True, linewidths=1)\n`;
            code += `plt.title('Correlation Matrix')\n`;
            code += `plt.tight_layout()\n`;
            code += `plt.show()\n`;
            
            return code;
        }
    },

    pairPlot: {
        type: 'viz_pairplot',
        name: 'Pair Plot',
        category: 'visualization',
        icon: 'bi-grid',
        color: '#c2185b',
        description: 'Create a pair plot (seaborn)',
        defaults: {
            hue: null
        },
        properties: [
            BaseNode.createProperty('hue', 'Hue Column', 'text', {
                placeholder: 'category or leave empty',
                help: 'Column name for color coding'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const hue = data.hue || null;
            
            let code = 'import seaborn as sns\n';
            code += 'import matplotlib.pyplot as plt\n';
            const params = hue ? `hue='${hue}'` : '';
            code += `sns.pairplot(${inputVar}${params ? ', ' + params : ''})\n`;
            code += `plt.tight_layout()\n`;
            code += `plt.show()\n`;
            
            return code;
        }
    }
};

// Register all visualization nodes
Object.values(VisualizationNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = VisualizationNodes;
}

