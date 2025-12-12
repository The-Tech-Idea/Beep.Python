/**
 * Database Nodes
 * SQL queries and database connections
 */

const DatabaseNodes = {
    connectDatabase: {
        type: 'database_connect',
        name: 'Connect to Database',
        category: 'database',
        icon: 'bi-database',
        color: '#007bff',
        description: 'Connect to SQL database (SQLite, PostgreSQL, MySQL)',
        defaults: {
            db_type: 'sqlite',
            connection_string: 'sqlite:///database.db',
            variable_name: 'conn'
        },
        properties: [
            BaseNode.createProperty('db_type', 'Database Type', 'select', {
                default: 'sqlite',
                options: ['sqlite', 'postgresql', 'mysql', 'mssql'],
                help: 'Type of database'
            }),
            BaseNode.createProperty('connection_string', 'Connection String', 'text', {
                required: true,
                default: 'sqlite:///database.db',
                placeholder: 'sqlite:///db.db or postgresql://user:pass@host/db',
                help: 'Database connection string'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'conn'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const connStr = data.connection_string || 'sqlite:///database.db';
            const varName = data.variable_name || 'conn';
            
            let code = 'import sqlalchemy as sa\n';
            code += `from sqlalchemy import create_engine\n`;
            code += `\n`;
            code += `${varName} = create_engine('${connStr}')\n`;
            code += `print(f'Connected to database: {connStr.split("://")[0]}')\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    readSQL: {
        type: 'database_read_sql',
        name: 'Read SQL Query',
        category: 'database',
        icon: 'bi-database-check',
        color: '#28a745',
        description: 'Execute SQL query and load into DataFrame',
        defaults: {
            query: 'SELECT * FROM table_name LIMIT 100',
            connection_variable: 'conn',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('connection_variable', 'Connection Variable', 'text', {
                required: true,
                default: 'conn',
                help: 'Variable name of database connection'
            }),
            BaseNode.createProperty('query', 'SQL Query', 'text', {
                required: true,
                default: 'SELECT * FROM table_name LIMIT 100',
                placeholder: 'SELECT * FROM users WHERE age > 18',
                help: 'SQL SELECT query'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const connVar = data.connection_variable || 'conn';
            const query = data.query || '';
            const varName = data.variable_name || 'df';
            
            if (!query) return `# Read SQL: Query required`;
            
            let code = 'import pandas as pd\n';
            code += `${varName} = pd.read_sql('${query}', ${connVar})\n`;
            code += `print(f'Loaded {len(${varName})} rows from database')\n`;
            code += `print(${varName}.head())\n`;
            
            if (context) {
                context.setVariable(node.id, varName);
            }
            
            return code;
        }
    },

    writeSQL: {
        type: 'database_write_sql',
        name: 'Write to Database',
        category: 'database',
        icon: 'bi-database-add',
        color: '#ffc107',
        description: 'Write DataFrame to database table',
        defaults: {
            table_name: 'output_table',
            connection_variable: 'conn',
            if_exists: 'replace'
        },
        properties: [
            BaseNode.createProperty('connection_variable', 'Connection Variable', 'text', {
                required: true,
                default: 'conn'
            }),
            BaseNode.createProperty('table_name', 'Table Name', 'text', {
                required: true,
                default: 'output_table',
                placeholder: 'results_table'
            }),
            BaseNode.createProperty('if_exists', 'If Exists', 'select', {
                default: 'replace',
                options: ['fail', 'replace', 'append'],
                help: 'What to do if table exists'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'df';
            const connVar = data.connection_variable || 'conn';
            const tableName = data.table_name || 'output_table';
            const ifExists = data.if_exists || 'replace';
            
            let code = `${inputVar}.to_sql('${tableName}', ${connVar}, if_exists='${ifExists}', index=False)\n`;
            code += `print(f'Written {len(${inputVar})} rows to table {tableName}')\n`;
            
            return code;
        }
    }
};

// Register all database nodes
Object.values(DatabaseNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DatabaseNodes;
}

