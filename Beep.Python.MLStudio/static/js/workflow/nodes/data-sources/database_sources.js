/**
 * Database Data Loading Nodes
 * Load data from various databases
 */

const DatabaseSourceNodes = {
    loadSQLite: {
        type: 'data_load_sqlite',
        name: 'Load from SQLite',
        category: 'data-sources',
        icon: 'bi-database',
        color: '#1976d2',
        description: 'Load data from SQLite database',
        defaults: {
            db_path: 'data/database.db',
            query: 'SELECT * FROM table_name',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('db_path', 'Database Path', 'text', {
                required: true,
                placeholder: 'data/database.db'
            }),
            BaseNode.createProperty('query', 'SQL Query', 'text', {
                required: true,
                default: 'SELECT * FROM table_name',
                placeholder: 'SELECT * FROM users'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const dbPath = data.db_path || 'data/database.db';
            const query = data.query || 'SELECT * FROM table_name';
            
            if (context) context.setVariable(node.id, varName);
            
            return `import sqlite3\nconn = sqlite3.connect('${dbPath}')\n${varName} = pd.read_sql_query('''${query}''', conn)\nconn.close()\nprint(f'Loaded from SQLite: {${varName}.shape}')`;
        }
    },

    loadMySQL: {
        type: 'data_load_mysql',
        name: 'Load from MySQL',
        category: 'data-sources',
        icon: 'bi-database-fill',
        color: '#0277bd',
        description: 'Load data from MySQL database',
        defaults: {
            host: 'localhost',
            port: 3306,
            database: 'mydb',
            username: 'user',
            password: 'password',
            query: 'SELECT * FROM table_name',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('host', 'Host', 'text', {
                required: true,
                default: 'localhost'
            }),
            BaseNode.createProperty('port', 'Port', 'number', {
                default: 3306
            }),
            BaseNode.createProperty('database', 'Database', 'text', {
                required: true,
                placeholder: 'mydb'
            }),
            BaseNode.createProperty('username', 'Username', 'text', {
                required: true,
                placeholder: 'user'
            }),
            BaseNode.createProperty('password', 'Password', 'text', {
                required: true,
                placeholder: 'password',
                inputType: 'password'
            }),
            BaseNode.createProperty('query', 'SQL Query', 'text', {
                required: true,
                default: 'SELECT * FROM table_name'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const host = data.host || 'localhost';
            const port = data.port || 3306;
            const database = data.database || 'mydb';
            const username = data.username || 'user';
            const password = data.password || 'password';
            const query = data.query || 'SELECT * FROM table_name';
            
            if (context) context.setVariable(node.id, varName);
            
            return `from sqlalchemy import create_engine\nengine = create_engine(f'mysql+pymysql://${username}:${password}@${host}:${port}/${database}')\n${varName} = pd.read_sql('''${query}''', engine)\nengine.dispose()\nprint(f'Loaded from MySQL: {${varName}.shape}')`;
        }
    },

    loadPostgreSQL: {
        type: 'data_load_postgresql',
        name: 'Load from PostgreSQL',
        category: 'data-sources',
        icon: 'bi-database-check',
        color: '#e65100',
        description: 'Load data from PostgreSQL database',
        defaults: {
            host: 'localhost',
            port: 5432,
            database: 'mydb',
            username: 'user',
            password: 'password',
            query: 'SELECT * FROM table_name',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('host', 'Host', 'text', {
                required: true,
                default: 'localhost'
            }),
            BaseNode.createProperty('port', 'Port', 'number', {
                default: 5432
            }),
            BaseNode.createProperty('database', 'Database', 'text', {
                required: true,
                placeholder: 'mydb'
            }),
            BaseNode.createProperty('username', 'Username', 'text', {
                required: true,
                placeholder: 'user'
            }),
            BaseNode.createProperty('password', 'Password', 'text', {
                required: true,
                placeholder: 'password',
                inputType: 'password'
            }),
            BaseNode.createProperty('query', 'SQL Query', 'text', {
                required: true,
                default: 'SELECT * FROM table_name'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const host = data.host || 'localhost';
            const port = data.port || 5432;
            const database = data.database || 'mydb';
            const username = data.username || 'user';
            const password = data.password || 'password';
            const query = data.query || 'SELECT * FROM table_name';
            
            if (context) context.setVariable(node.id, varName);
            
            return `from sqlalchemy import create_engine\nengine = create_engine(f'postgresql+psycopg2://${username}:${password}@${host}:${port}/${database}')\n${varName} = pd.read_sql('''${query}''', engine)\nengine.dispose()\nprint(f'Loaded from PostgreSQL: {${varName}.shape}')`;
        }
    },

    loadMongoDB: {
        type: 'data_load_mongodb',
        name: 'Load from MongoDB',
        category: 'data-sources',
        icon: 'bi-database-add',
        color: '#2e7d32',
        description: 'Load data from MongoDB collection',
        defaults: {
            connection_string: 'mongodb://localhost:27017/',
            database: 'mydb',
            collection: 'mycollection',
            query: '{}',
            variable_name: 'df'
        },
        properties: [
            BaseNode.createProperty('connection_string', 'Connection String', 'text', {
                required: true,
                default: 'mongodb://localhost:27017/',
                placeholder: 'mongodb://localhost:27017/'
            }),
            BaseNode.createProperty('database', 'Database', 'text', {
                required: true,
                placeholder: 'mydb'
            }),
            BaseNode.createProperty('collection', 'Collection', 'text', {
                required: true,
                placeholder: 'mycollection'
            }),
            BaseNode.createProperty('query', 'Query (JSON)', 'text', {
                default: '{}',
                placeholder: '{"field": "value"}',
                help: 'MongoDB query as JSON string'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'df'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'df';
            const connStr = data.connection_string || 'mongodb://localhost:27017/';
            const database = data.database || 'mydb';
            const collection = data.collection || 'mycollection';
            const query = data.query || '{}';
            
            if (context) context.setVariable(node.id, varName);
            
            return `from pymongo import MongoClient\nimport json\nclient = MongoClient('${connStr}')\ndb = client['${database}']\ncoll = db['${collection}']\nquery_dict = json.loads('${query}')\ncursor = coll.find(query_dict)\n${varName} = pd.DataFrame(list(cursor))\nclient.close()\nprint(f'Loaded from MongoDB: {${varName}.shape}')`;
        }
    },

    loadRedis: {
        type: 'data_load_redis',
        name: 'Load from Redis',
        category: 'data-sources',
        icon: 'bi-database-down',
        color: '#c2185b',
        description: 'Load data from Redis (key-value store)',
        defaults: {
            host: 'localhost',
            port: 6379,
            key: 'mykey',
            variable_name: 'data'
        },
        properties: [
            BaseNode.createProperty('host', 'Host', 'text', {
                required: true,
                default: 'localhost'
            }),
            BaseNode.createProperty('port', 'Port', 'number', {
                default: 6379
            }),
            BaseNode.createProperty('key', 'Key', 'text', {
                required: true,
                placeholder: 'mykey'
            }),
            BaseNode.createProperty('variable_name', 'Variable Name', 'text', {
                default: 'data'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const varName = data.variable_name || 'data';
            const host = data.host || 'localhost';
            const port = data.port || 6379;
            const key = data.key || 'mykey';
            
            if (context) context.setVariable(node.id, varName);
            
            return `import redis\nimport json\nr = redis.Redis(host='${host}', port=${port}, decode_responses=True)\n${varName} = json.loads(r.get('${key}'))\nprint(f'Loaded from Redis: {type(${varName})}')`;
        }
    }
};

// Register all database source nodes
Object.values(DatabaseSourceNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DatabaseSourceNodes;
}

