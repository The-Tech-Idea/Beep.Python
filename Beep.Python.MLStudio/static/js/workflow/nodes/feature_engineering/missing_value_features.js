/**
 * Missing Value Indicator Feature Engineering Nodes
 * Create features indicating missing values
 */

const MissingValueFeatureNodes = {
    missingValueIndicators: {
        type: 'fe_missing_indicators',
        name: 'Missing Value Indicators',
        category: 'feature-engineering',
        icon: 'bi-question-circle',
        color: '#6c757d',
        description: 'Create binary features indicating missing values',
        defaults: {
            columns: ''
        },
        properties: [
            BaseNode.createProperty('columns', 'Columns', 'text', {
                required: true,
                placeholder: 'col1, col2 or leave empty for all',
                help: 'Comma-separated columns (empty = all columns)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columns = data.columns || '';
            
            let code = `# Missing value indicators\n`;
            
            if (columns) {
                const colList = columns.split(',').map(c => c.trim());
                colList.forEach(col => {
                    code += `${inputVar}['${col}_is_missing'] = ${inputVar}['${col}'].isna().astype(int)\n`;
                });
            } else {
                code += `for col in ${inputVar}.columns:\n`;
                code += `    if ${inputVar}[col].isna().any():\n`;
                code += `        ${inputVar}[f'{col}_is_missing'] = ${inputVar}[col].isna().astype(int)\n`;
            }
            
            code += `print(f'Created missing value indicator features')\n`;
            
            return code;
        }
    },

    missingValueCount: {
        type: 'fe_missing_count',
        name: 'Missing Value Count',
        category: 'feature-engineering',
        icon: 'bi-list-ol',
        color: '#495057',
        description: 'Create feature with count of missing values per row',
        defaults: {},
        properties: [],
        generateCode: (node, context) => {
            const inputVar = context ? context.getInputVariable(node) : 'df';
            
            let code = `# Missing value count per row\n`;
            code += `${inputVar}['missing_count'] = ${inputVar}.isna().sum(axis=1)\n`;
            code += `${inputVar}['missing_ratio'] = ${inputVar}['missing_count'] / len(${inputVar}.columns)\n`;
            code += `print(f'Created missing value count features')\n`;
            
            return code;
        }
    },

    missingValuePattern: {
        type: 'fe_missing_pattern',
        name: 'Missing Value Pattern',
        category: 'feature-engineering',
        icon: 'bi-diagram-3',
        color: '#343a40',
        description: 'Create features indicating patterns of missing values',
        defaults: {
            column_groups: ''
        },
        properties: [
            BaseNode.createProperty('column_groups', 'Column Groups', 'text', {
                placeholder: 'col1,col2|col3,col4',
                help: 'Pipe-separated groups of columns (comma-separated within groups)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const columnGroups = data.column_groups || '';
            
            let code = `# Missing value patterns\n`;
            
            if (columnGroups) {
                const groups = columnGroups.split('|').map(g => g.trim());
                groups.forEach((group, idx) => {
                    const cols = group.split(',').map(c => c.trim());
                    code += `${inputVar}[f'missing_group_{idx+1}'] = ${inputVar}[${cols.map(c => `'${c}'`).join(', ')}].isna().all(axis=1).astype(int)\n`;
                });
            } else {
                code += `# Count missing in numeric vs categorical\n`;
                code += `numeric_cols = ${inputVar}.select_dtypes(include=['number']).columns\n`;
                code += `categorical_cols = ${inputVar}.select_dtypes(include=['object']).columns\n`;
                code += `${inputVar}['missing_numeric'] = ${inputVar}[numeric_cols].isna().sum(axis=1)\n`;
                code += `${inputVar}['missing_categorical'] = ${inputVar}[categorical_cols].isna().sum(axis=1)\n`;
            }
            
            code += `print(f'Created missing value pattern features')\n`;
            
            return code;
        }
    }
};

// Register all missing value feature nodes
Object.values(MissingValueFeatureNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = MissingValueFeatureNodes;
}

