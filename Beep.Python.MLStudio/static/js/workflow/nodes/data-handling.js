/**
 * Data Handling Nodes
 * Nodes for handling missing values, encoding categorical data, and data cleanup
 */

const DataHandlingNodes = {
    // Handle Missing Values (NaN)
    handleMissingValues: {
        type: 'handle_missing',
        name: 'Handle Missing Values',
        category: 'preprocessing',
        icon: 'bi-bandaid',
        color: '#ff9800',
        description: 'Handle NaN/missing values in dataset',
        defaults: {
            strategy: 'drop_rows',
            fill_value: 0,
            columns: 'all'
        },
        properties: [
            BaseNode.createProperty('strategy', 'Strategy', 'select', {
                default: 'drop_rows',
                options: [
                    { value: 'drop_rows', label: 'Drop rows with missing values' },
                    { value: 'drop_cols', label: 'Drop columns with >50% missing' },
                    { value: 'fill_mean', label: 'Fill with mean (numeric only)' },
                    { value: 'fill_median', label: 'Fill with median (numeric only)' },
                    { value: 'fill_mode', label: 'Fill with mode (most frequent)' },
                    { value: 'fill_constant', label: 'Fill with constant value' },
                    { value: 'fill_forward', label: 'Forward fill (ffill)' },
                    { value: 'fill_backward', label: 'Backward fill (bfill)' }
                ],
                help: 'Strategy for handling missing values'
            }),
            BaseNode.createProperty('fill_value', 'Fill Value', 'text', {
                default: '0',
                placeholder: '0',
                help: 'Value to use when strategy is "fill_constant"',
                showWhen: { strategy: 'fill_constant' }
            }),
            BaseNode.createProperty('columns', 'Apply To', 'select', {
                default: 'all',
                options: [
                    { value: 'all', label: 'All columns' },
                    { value: 'numeric', label: 'Numeric columns only' }
                ],
                help: 'Which columns to apply the strategy to'
            })
        ],
        inputs: [{ id: 'input', label: 'Data', type: 'data' }],
        outputs: [{ id: 'output', label: 'Cleaned Data', type: 'data' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const strategy = data.strategy || 'drop_rows';
            const fillValue = data.fill_value || '0';
            const columns = data.columns || 'all';
            
            let code = `# Handle Missing Values Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${inputVar} (cleaned)\n\n`;
            code += `import numpy as np\n`;
            code += `print(f'Missing values before: {${inputVar}.isnull().sum().sum()}')\n\n`;
            
            if (strategy === 'drop_rows') {
                code += `${inputVar} = ${inputVar}.dropna()\n`;
            } else if (strategy === 'drop_cols') {
                code += `# Drop columns with more than 50% missing values\n`;
                code += `threshold = len(${inputVar}) * 0.5\n`;
                code += `${inputVar} = ${inputVar}.dropna(axis=1, thresh=threshold)\n`;
            } else if (strategy === 'fill_mean') {
                if (columns === 'numeric') {
                    code += `numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns\n`;
                    code += `${inputVar}[numeric_cols] = ${inputVar}[numeric_cols].fillna(${inputVar}[numeric_cols].mean())\n`;
                } else {
                    code += `${inputVar} = ${inputVar}.fillna(${inputVar}.mean(numeric_only=True))\n`;
                }
            } else if (strategy === 'fill_median') {
                if (columns === 'numeric') {
                    code += `numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns\n`;
                    code += `${inputVar}[numeric_cols] = ${inputVar}[numeric_cols].fillna(${inputVar}[numeric_cols].median())\n`;
                } else {
                    code += `${inputVar} = ${inputVar}.fillna(${inputVar}.median(numeric_only=True))\n`;
                }
            } else if (strategy === 'fill_mode') {
                code += `for col in ${inputVar}.columns:\n`;
                code += `    if ${inputVar}[col].isnull().any():\n`;
                code += `        mode_val = ${inputVar}[col].mode()\n`;
                code += `        if len(mode_val) > 0:\n`;
                code += `            ${inputVar}[col] = ${inputVar}[col].fillna(mode_val[0])\n`;
            } else if (strategy === 'fill_constant') {
                // Try to convert to number, otherwise use as string
                const numVal = parseFloat(fillValue);
                if (!isNaN(numVal)) {
                    code += `${inputVar} = ${inputVar}.fillna(${numVal})\n`;
                } else {
                    code += `${inputVar} = ${inputVar}.fillna('${fillValue}')\n`;
                }
            } else if (strategy === 'fill_forward') {
                code += `${inputVar} = ${inputVar}.ffill()\n`;
            } else if (strategy === 'fill_backward') {
                code += `${inputVar} = ${inputVar}.bfill()\n`;
            }
            
            code += `\nprint(f'Missing values after: {${inputVar}.isnull().sum().sum()}')\n`;
            code += `print(f'Data shape: {${inputVar}.shape}')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    },
    
    // Encode Categorical Variables
    encodeCategorical: {
        type: 'encode_categorical',
        name: 'Encode Categorical',
        category: 'preprocessing',
        icon: 'bi-alphabet',
        color: '#9c27b0',
        description: 'Convert string/categorical columns to numeric',
        defaults: {
            method: 'label',
            drop_first: true,
            handle_unknown: 'error'
        },
        properties: [
            BaseNode.createProperty('method', 'Encoding Method', 'select', {
                default: 'label',
                options: [
                    { value: 'label', label: 'Label Encoding (0, 1, 2, ...)' },
                    { value: 'onehot', label: 'One-Hot Encoding (dummy variables)' },
                    { value: 'ordinal', label: 'Ordinal Encoding (preserve order)' },
                    { value: 'frequency', label: 'Frequency Encoding (by count)' }
                ],
                help: 'Method to encode categorical variables'
            }),
            BaseNode.createProperty('drop_first', 'Drop First Column', 'boolean', {
                default: true,
                help: 'For one-hot: drop first column to avoid multicollinearity',
                showWhen: { method: 'onehot' }
            }),
            BaseNode.createProperty('max_categories', 'Max Categories', 'number', {
                default: 10,
                min: 2,
                max: 100,
                help: 'Maximum unique values to encode (columns with more are dropped)',
            })
        ],
        inputs: [{ id: 'input', label: 'Data', type: 'data' }],
        outputs: [{ id: 'output', label: 'Encoded Data', type: 'data' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const method = data.method || 'label';
            const dropFirst = data.drop_first !== false;
            const maxCategories = data.max_categories || 10;
            
            let code = `# Encode Categorical Variables Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${inputVar} (encoded)\n\n`;
            code += `import numpy as np\n`;
            code += `from sklearn.preprocessing import LabelEncoder\n\n`;
            
            code += `# Find categorical columns (object/string type)\n`;
            code += `categorical_cols = ${inputVar}.select_dtypes(include=['object', 'category']).columns.tolist()\n`;
            code += `print(f'Found {len(categorical_cols)} categorical columns: {categorical_cols}')\n\n`;
            
            code += `# Filter columns with too many unique values\n`;
            code += `cols_to_encode = [col for col in categorical_cols if ${inputVar}[col].nunique() <= ${maxCategories}]\n`;
            code += `cols_to_drop = [col for col in categorical_cols if ${inputVar}[col].nunique() > ${maxCategories}]\n`;
            code += `if cols_to_drop:\n`;
            code += `    print(f'Dropping columns with too many categories: {cols_to_drop}')\n`;
            code += `    ${inputVar} = ${inputVar}.drop(columns=cols_to_drop)\n\n`;
            
            if (method === 'label') {
                code += `# Label Encoding\n`;
                code += `label_encoders = {}\n`;
                code += `for col in cols_to_encode:\n`;
                code += `    le = LabelEncoder()\n`;
                code += `    # Handle NaN by filling temporarily\n`;
                code += `    ${inputVar}[col] = ${inputVar}[col].fillna('__MISSING__')\n`;
                code += `    ${inputVar}[col] = le.fit_transform(${inputVar}[col].astype(str))\n`;
                code += `    label_encoders[col] = le\n`;
                code += `print(f'Label encoded {len(cols_to_encode)} columns')\n`;
            } else if (method === 'onehot') {
                code += `# One-Hot Encoding\n`;
                code += `if cols_to_encode:\n`;
                code += `    ${inputVar} = pd.get_dummies(${inputVar}, columns=cols_to_encode, drop_first=${dropFirst ? 'True' : 'False'})\n`;
                code += `    print(f'One-hot encoded. New shape: {${inputVar}.shape}')\n`;
            } else if (method === 'frequency') {
                code += `# Frequency Encoding\n`;
                code += `for col in cols_to_encode:\n`;
                code += `    freq_map = ${inputVar}[col].value_counts(normalize=True).to_dict()\n`;
                code += `    ${inputVar}[col] = ${inputVar}[col].map(freq_map).fillna(0)\n`;
                code += `print(f'Frequency encoded {len(cols_to_encode)} columns')\n`;
            } else {
                // ordinal - same as label for now
                code += `# Ordinal Encoding\n`;
                code += `from sklearn.preprocessing import OrdinalEncoder\n`;
                code += `if cols_to_encode:\n`;
                code += `    oe = OrdinalEncoder(handle_unknown='use_encoded_value', unknown_value=-1)\n`;
                code += `    ${inputVar}[cols_to_encode] = ${inputVar}[cols_to_encode].fillna('__MISSING__')\n`;
                code += `    ${inputVar}[cols_to_encode] = oe.fit_transform(${inputVar}[cols_to_encode].astype(str))\n`;
                code += `    print(f'Ordinal encoded {len(cols_to_encode)} columns')\n`;
            }
            
            code += `\nprint(f'Final data shape: {${inputVar}.shape}')\n`;
            code += `print(f'Data types: {${inputVar}.dtypes.value_counts().to_dict()}')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    },
    
    // Drop Columns
    dropColumns: {
        type: 'drop_columns',
        name: 'Drop Columns',
        category: 'preprocessing',
        icon: 'bi-x-circle',
        color: '#f44336',
        description: 'Remove specific columns from dataset',
        defaults: {
            mode: 'drop_non_numeric',
            columns: ''
        },
        properties: [
            BaseNode.createProperty('mode', 'Mode', 'select', {
                default: 'drop_non_numeric',
                options: [
                    { value: 'drop_non_numeric', label: 'Drop all non-numeric columns' },
                    { value: 'drop_high_null', label: 'Drop columns with >50% nulls' },
                    { value: 'drop_low_variance', label: 'Drop low variance columns' },
                    { value: 'drop_specific', label: 'Drop specific columns (by name)' },
                    { value: 'keep_specific', label: 'Keep only specific columns' }
                ],
                help: 'How to select columns to drop'
            }),
            BaseNode.createProperty('columns', 'Column Names', 'text', {
                default: '',
                placeholder: 'col1, col2, col3',
                help: 'Comma-separated column names',
                showWhen: { mode: ['drop_specific', 'keep_specific'] }
            }),
            BaseNode.createProperty('null_threshold', 'Null Threshold (%)', 'number', {
                default: 50,
                min: 0,
                max: 100,
                help: 'Drop columns with null percentage above this',
                showWhen: { mode: 'drop_high_null' }
            })
        ],
        inputs: [{ id: 'input', label: 'Data', type: 'data' }],
        outputs: [{ id: 'output', label: 'Data', type: 'data' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const mode = data.mode || 'drop_non_numeric';
            const columns = data.columns || '';
            const nullThreshold = data.null_threshold || 50;
            
            let code = `# Drop Columns Node (${node.id})\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${inputVar} (columns removed)\n\n`;
            code += `import numpy as np\n`;
            code += `print(f'Columns before: {list(${inputVar}.columns)}')\n\n`;
            
            if (mode === 'drop_non_numeric') {
                code += `# Keep only numeric columns\n`;
                code += `numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns.tolist()\n`;
                code += `dropped = [c for c in ${inputVar}.columns if c not in numeric_cols]\n`;
                code += `${inputVar} = ${inputVar}[numeric_cols]\n`;
                code += `print(f'Dropped non-numeric columns: {dropped}')\n`;
            } else if (mode === 'drop_high_null') {
                code += `# Drop columns with high null percentage\n`;
                code += `null_pct = ${inputVar}.isnull().sum() / len(${inputVar}) * 100\n`;
                code += `cols_to_drop = null_pct[null_pct > ${nullThreshold}].index.tolist()\n`;
                code += `${inputVar} = ${inputVar}.drop(columns=cols_to_drop)\n`;
                code += `print(f'Dropped high-null columns: {cols_to_drop}')\n`;
            } else if (mode === 'drop_low_variance') {
                code += `# Drop columns with very low variance (constant or near-constant)\n`;
                code += `from sklearn.feature_selection import VarianceThreshold\n`;
                code += `numeric_df = ${inputVar}.select_dtypes(include=[np.number])\n`;
                code += `if len(numeric_df.columns) > 0:\n`;
                code += `    selector = VarianceThreshold(threshold=0.01)\n`;
                code += `    selector.fit(numeric_df.fillna(0))\n`;
                code += `    low_var_cols = numeric_df.columns[~selector.get_support()].tolist()\n`;
                code += `    ${inputVar} = ${inputVar}.drop(columns=low_var_cols, errors='ignore')\n`;
                code += `    print(f'Dropped low-variance columns: {low_var_cols}')\n`;
            } else if (mode === 'drop_specific') {
                const colList = columns.split(',').map(c => c.trim()).filter(c => c);
                code += `# Drop specific columns\n`;
                code += `cols_to_drop = ${JSON.stringify(colList)}\n`;
                code += `${inputVar} = ${inputVar}.drop(columns=cols_to_drop, errors='ignore')\n`;
                code += `print(f'Dropped columns: {cols_to_drop}')\n`;
            } else if (mode === 'keep_specific') {
                const colList = columns.split(',').map(c => c.trim()).filter(c => c);
                code += `# Keep only specific columns\n`;
                code += `cols_to_keep = ${JSON.stringify(colList)}\n`;
                code += `cols_to_keep = [c for c in cols_to_keep if c in ${inputVar}.columns]\n`;
                code += `${inputVar} = ${inputVar}[cols_to_keep]\n`;
                code += `print(f'Kept columns: {cols_to_keep}')\n`;
            }
            
            code += `\nprint(f'Final columns: {list(${inputVar}.columns)}')\n`;
            code += `print(f'Final shape: {${inputVar}.shape}')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    },
    
    // Auto Data Prep - Combines multiple preprocessing steps
    autoDataPrep: {
        type: 'auto_data_prep',
        name: 'Auto Data Prep',
        category: 'preprocessing',
        icon: 'bi-magic',
        color: '#00bcd4',
        description: 'Automatic data preparation (handle nulls, encode, clean)',
        defaults: {
            handle_missing: 'fill_median',
            encode_categoricals: true,
            drop_high_cardinality: true,
            max_categories: 10
        },
        properties: [
            BaseNode.createProperty('handle_missing', 'Missing Value Strategy', 'select', {
                default: 'fill_median',
                options: [
                    { value: 'drop', label: 'Drop rows with missing values' },
                    { value: 'fill_mean', label: 'Fill with mean' },
                    { value: 'fill_median', label: 'Fill with median' },
                    { value: 'fill_mode', label: 'Fill with mode' }
                ],
                help: 'How to handle missing values in numeric columns'
            }),
            BaseNode.createProperty('encode_categoricals', 'Encode Categorical Columns', 'boolean', {
                default: true,
                help: 'Convert string columns to numeric using label encoding'
            }),
            BaseNode.createProperty('drop_high_cardinality', 'Drop High Cardinality', 'boolean', {
                default: true,
                help: 'Drop categorical columns with too many unique values'
            }),
            BaseNode.createProperty('max_categories', 'Max Categories', 'number', {
                default: 10,
                min: 2,
                max: 100,
                help: 'Maximum unique values for categorical encoding'
            })
        ],
        inputs: [{ id: 'input', label: 'Data', type: 'data' }],
        outputs: [{ id: 'output', label: 'Prepared Data', type: 'data' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const handleMissing = data.handle_missing || 'fill_median';
            const encodeCategoricals = data.encode_categoricals !== false;
            const dropHighCardinality = data.drop_high_cardinality !== false;
            const maxCategories = data.max_categories || 10;
            
            let code = `# Auto Data Prep Node (${node.id})\n`;
            code += `# Automatic data preparation pipeline\n`;
            code += `# Input: ${inputVar}\n`;
            code += `# Output: ${inputVar} (prepared)\n\n`;
            code += `import numpy as np\n`;
            code += `from sklearn.preprocessing import LabelEncoder\n\n`;
            
            code += `print(f'Original shape: {${inputVar}.shape}')\n`;
            code += `print(f'Original dtypes: {${inputVar}.dtypes.value_counts().to_dict()}')\n`;
            code += `print(f'Missing values: {${inputVar}.isnull().sum().sum()}')\n\n`;
            
            // Step 1: Handle high cardinality categorical columns
            if (dropHighCardinality) {
                code += `# Step 1: Drop high cardinality categorical columns\n`;
                code += `cat_cols = ${inputVar}.select_dtypes(include=['object', 'category']).columns\n`;
                code += `high_card_cols = [c for c in cat_cols if ${inputVar}[c].nunique() > ${maxCategories}]\n`;
                code += `if high_card_cols:\n`;
                code += `    print(f'Dropping high cardinality columns: {high_card_cols}')\n`;
                code += `    ${inputVar} = ${inputVar}.drop(columns=high_card_cols)\n\n`;
            }
            
            // Step 2: Handle missing values in numeric columns
            code += `# Step 2: Handle missing values in numeric columns\n`;
            code += `numeric_cols = ${inputVar}.select_dtypes(include=[np.number]).columns\n`;
            if (handleMissing === 'drop') {
                code += `${inputVar} = ${inputVar}.dropna(subset=numeric_cols)\n`;
            } else if (handleMissing === 'fill_mean') {
                code += `${inputVar}[numeric_cols] = ${inputVar}[numeric_cols].fillna(${inputVar}[numeric_cols].mean())\n`;
            } else if (handleMissing === 'fill_median') {
                code += `${inputVar}[numeric_cols] = ${inputVar}[numeric_cols].fillna(${inputVar}[numeric_cols].median())\n`;
            } else if (handleMissing === 'fill_mode') {
                code += `for col in numeric_cols:\n`;
                code += `    mode_val = ${inputVar}[col].mode()\n`;
                code += `    if len(mode_val) > 0:\n`;
                code += `        ${inputVar}[col] = ${inputVar}[col].fillna(mode_val[0])\n`;
            }
            code += `print(f'After handling missing numeric: {${inputVar}.isnull().sum().sum()} missing')\n\n`;
            
            // Step 3: Encode categorical columns
            if (encodeCategoricals) {
                code += `# Step 3: Encode categorical columns\n`;
                code += `cat_cols = ${inputVar}.select_dtypes(include=['object', 'category']).columns.tolist()\n`;
                code += `label_encoders = {}\n`;
                code += `for col in cat_cols:\n`;
                code += `    le = LabelEncoder()\n`;
                code += `    ${inputVar}[col] = ${inputVar}[col].fillna('__MISSING__').astype(str)\n`;
                code += `    ${inputVar}[col] = le.fit_transform(${inputVar}[col])\n`;
                code += `    label_encoders[col] = le\n`;
                code += `print(f'Encoded {len(cat_cols)} categorical columns: {cat_cols}')\n\n`;
            }
            
            // Step 4: Final cleanup
            code += `# Step 4: Final cleanup - drop any remaining missing values\n`;
            code += `${inputVar} = ${inputVar}.dropna()\n\n`;
            
            code += `print(f'Final shape: {${inputVar}.shape}')\n`;
            code += `print(f'Final dtypes: {${inputVar}.dtypes.value_counts().to_dict()}')\n`;
            code += `print(f'All columns now numeric: {${inputVar}.select_dtypes(include=[np.number]).shape[1] == ${inputVar}.shape[1]}')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    }
};

// Register nodes when this file loads
if (typeof registerNodesSafely !== 'undefined') {
    // Use the safe registration queue
    registerNodesSafely(DataHandlingNodes, 'DataHandling');
} else if (typeof nodeRegistry !== 'undefined' && typeof BaseNode !== 'undefined') {
    // Direct registration
    Object.values(DataHandlingNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register node ${nodeDef.type}:`, error);
        }
    });
    console.log('✓ Registered DataHandling nodes');
} else {
    console.warn('DataHandlingNodes: Neither registerNodesSafely nor nodeRegistry available');
}

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = DataHandlingNodes;
}

