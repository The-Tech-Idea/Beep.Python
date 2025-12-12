/**
 * Petroleum Engineering Industry Nodes
 * Specialized nodes for oil & gas, reservoir engineering, and production optimization
 */

const PetroleumNodes = {
    // Well Log Loader
    wellLogLoader: {
        type: 'petroleum_well_log_loader',
        name: 'Well Log Loader',
        category: 'petroleum',
        icon: 'bi-file-earmark-bar-graph',
        color: '#795548',
        description: 'Load well log data from LAS or CSV files',
        defaults: {
            file_format: 'csv',
            file_path: 'data/well_logs.csv',
            well_name: 'Well_1',
            depth_unit: 'ft'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'file', {
                default: 'data/well_logs.csv',
                help: 'Path to well log file',
                fileFilter: '.csv,.las'
            }),
            BaseNode.createProperty('well_name', 'Well Name', 'text', {
                default: 'Well_1',
                help: 'Name of the well'
            }),
            BaseNode.createProperty('depth_unit', 'Depth Unit', 'select', {
                default: 'ft',
                options: [
                    { value: 'ft', label: 'Feet' },
                    { value: 'm', label: 'Meters' }
                ],
                help: 'Unit for depth measurements'
            })
        ],
        inputs: [],
        outputs: [{ id: 'output', label: 'Well Log Data', type: 'dataframe' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const filePath = data.file_path || 'data/well_logs.csv';
            const wellName = data.well_name || 'Well_1';
            
            let code = `# Load Well Log Data\nimport pandas as pd\nimport numpy as np\n\n`;
            code += `df = pd.read_csv('${filePath}')\n`;
            code += `# Standardize column names\n`;
            code += `df.columns = [c.upper().strip() for c in df.columns]\n`;
            code += `print(f'Loaded well log data for ${wellName}: {df.shape}')\n`;
            code += `print(f'Curves: {list(df.columns)}')\n`;
            
            context.setVariable(node.id, 'df');
            return code;
        }
    },
    
    // Production Data Loader
    productionDataLoader: {
        type: 'petroleum_production_data_loader',
        name: 'Production Data Loader',
        category: 'petroleum',
        icon: 'bi-bar-chart-steps',
        color: '#4caf50',
        description: 'Load oil/gas production data',
        defaults: {
            file_path: 'data/production.csv',
            date_column: 'Date',
            oil_column: 'Oil_Rate',
            gas_column: 'Gas_Rate',
            water_column: 'Water_Rate'
        },
        properties: [
            BaseNode.createProperty('file_path', 'File Path', 'file', {
                default: 'data/production.csv',
                help: 'Path to production data file'
            }),
            BaseNode.createProperty('date_column', 'Date Column', 'text', {
                default: 'Date',
                help: 'Column containing dates'
            }),
            BaseNode.createProperty('oil_column', 'Oil Rate Column', 'text', {
                default: 'Oil_Rate',
                help: 'Column containing oil production rate'
            }),
            BaseNode.createProperty('gas_column', 'Gas Rate Column', 'text', {
                default: 'Gas_Rate',
                help: 'Column containing gas production rate'
            }),
            BaseNode.createProperty('water_column', 'Water Rate Column', 'text', {
                default: 'Water_Rate',
                help: 'Column containing water production rate'
            })
        ],
        inputs: [],
        outputs: [{ id: 'output', label: 'Production Data', type: 'dataframe' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const filePath = data.file_path || 'data/production.csv';
            const dateCol = data.date_column || 'Date';
            const oilCol = data.oil_column || 'Oil_Rate';
            const gasCol = data.gas_column || 'Gas_Rate';
            const waterCol = data.water_column || 'Water_Rate';
            
            let code = `# Load Production Data\nimport pandas as pd\nimport numpy as np\n\n`;
            code += `df = pd.read_csv('${filePath}')\n`;
            code += `# Try to parse date column\n`;
            code += `if '${dateCol}' in df.columns:\n`;
            code += `    df['${dateCol}'] = pd.to_datetime(df['${dateCol}'])\n`;
            code += `    df = df.sort_values('${dateCol}')\n`;
            code += `    df['Days_On'] = (df['${dateCol}'] - df['${dateCol}'].min()).dt.days\n\n`;
            
            code += `# Calculate cumulative production\n`;
            code += `if '${oilCol}' in df.columns:\n`;
            code += `    df['Cum_Oil'] = df['${oilCol}'].cumsum()\n`;
            code += `if '${gasCol}' in df.columns:\n`;
            code += `    df['Cum_Gas'] = df['${gasCol}'].cumsum()\n`;
            code += `if '${waterCol}' in df.columns:\n`;
            code += `    df['Cum_Water'] = df['${waterCol}'].cumsum()\n\n`;
            
            code += `print(f'Loaded production data: {df.shape}')\n`;
            
            context.setVariable(node.id, 'df');
            return code;
        }
    },
    
    // Decline Curve Analysis
    declineCurveAnalysis: {
        type: 'petroleum_decline_curve_analysis',
        name: 'Decline Curve Analysis',
        category: 'petroleum',
        icon: 'bi-graph-down',
        color: '#ff9800',
        description: 'Fit Arps decline curves for production forecasting',
        defaults: {
            rate_column: 'Oil_Rate',
            time_column: 'Days_On',
            decline_type: 'hyperbolic',
            forecast_days: 365
        },
        properties: [
            BaseNode.createProperty('rate_column', 'Rate Column', 'text', {
                default: 'Oil_Rate',
                help: 'Column containing production rate'
            }),
            BaseNode.createProperty('time_column', 'Time Column', 'text', {
                default: 'Days_On',
                help: 'Column containing time values'
            }),
            BaseNode.createProperty('decline_type', 'Decline Type', 'select', {
                default: 'hyperbolic',
                options: [
                    { value: 'exponential', label: 'Exponential (b=0)' },
                    { value: 'hyperbolic', label: 'Hyperbolic (0<b<1)' },
                    { value: 'harmonic', label: 'Harmonic (b=1)' }
                ],
                help: 'Type of Arps decline curve'
            }),
            BaseNode.createProperty('forecast_days', 'Forecast Days', 'number', {
                default: 365,
                min: 30,
                max: 3650,
                help: 'Number of days to forecast'
            })
        ],
        inputs: [{ id: 'input', label: 'Production Data', type: 'dataframe' }],
        outputs: [{ id: 'output', label: 'DCA Results', type: 'dict' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const rateCol = data.rate_column || 'Oil_Rate';
            const timeCol = data.time_column || 'Days_On';
            const declineType = data.decline_type || 'hyperbolic';
            const forecastDays = data.forecast_days || 365;
            
            let code = `# Decline Curve Analysis (Arps)\nimport numpy as np\nfrom scipy.optimize import curve_fit\n\n`;
            
            // Define decline functions
            code += `def exponential_decline(t, qi, Di):\n`;
            code += `    return qi * np.exp(-Di * t)\n\n`;
            code += `def hyperbolic_decline(t, qi, Di, b):\n`;
            code += `    return qi / np.power(1 + b * Di * t, 1/b)\n\n`;
            code += `def harmonic_decline(t, qi, Di):\n`;
            code += `    return qi / (1 + Di * t)\n\n`;
            
            // Prepare data
            code += `# Prepare data\n`;
            code += `t = ${inputVar}['${timeCol}'].values\n`;
            code += `q = ${inputVar}['${rateCol}'].values\n\n`;
            code += `# Remove zeros and negatives\n`;
            code += `mask = (q > 0) & (t >= 0)\n`;
            code += `t_clean = t[mask]\n`;
            code += `q_clean = q[mask]\n\n`;
            code += `qi_init = q_clean[0] if len(q_clean) > 0 else 100\n`;
            code += `Di_init = 0.001\n\n`;
            
            // Fit based on decline type
            if (declineType === 'exponential') {
                code += `# Fit exponential decline\n`;
                code += `try:\n`;
                code += `    popt, _ = curve_fit(exponential_decline, t_clean, q_clean, p0=[qi_init, Di_init], maxfev=5000)\n`;
                code += `    qi, Di = popt\n`;
                code += `    decline_params = {'qi': qi, 'Di': Di, 'b': 0}\n`;
                code += `    forecast_func = lambda t: exponential_decline(t, qi, Di)\n`;
                code += `except:\n`;
                code += `    decline_params = {'qi': qi_init, 'Di': Di_init, 'b': 0}\n`;
                code += `    forecast_func = lambda t: exponential_decline(t, qi_init, Di_init)\n\n`;
            } else if (declineType === 'hyperbolic') {
                code += `# Fit hyperbolic decline\n`;
                code += `try:\n`;
                code += `    popt, _ = curve_fit(hyperbolic_decline, t_clean, q_clean, p0=[qi_init, Di_init, 0.5],\n`;
                code += `                       bounds=([0, 0, 0.01], [np.inf, 1, 1.5]), maxfev=5000)\n`;
                code += `    qi, Di, b = popt\n`;
                code += `    decline_params = {'qi': qi, 'Di': Di, 'b': b}\n`;
                code += `    forecast_func = lambda t: hyperbolic_decline(t, qi, Di, b)\n`;
                code += `except:\n`;
                code += `    decline_params = {'qi': qi_init, 'Di': Di_init, 'b': 0.5}\n`;
                code += `    forecast_func = lambda t: hyperbolic_decline(t, qi_init, Di_init, 0.5)\n\n`;
            } else {
                code += `# Fit harmonic decline\n`;
                code += `try:\n`;
                code += `    popt, _ = curve_fit(harmonic_decline, t_clean, q_clean, p0=[qi_init, Di_init], maxfev=5000)\n`;
                code += `    qi, Di = popt\n`;
                code += `    decline_params = {'qi': qi, 'Di': Di, 'b': 1}\n`;
                code += `    forecast_func = lambda t: harmonic_decline(t, qi, Di)\n`;
                code += `except:\n`;
                code += `    decline_params = {'qi': qi_init, 'Di': Di_init, 'b': 1}\n`;
                code += `    forecast_func = lambda t: harmonic_decline(t, qi_init, Di_init)\n\n`;
            }
            
            // Generate forecast and EUR
            code += `# Generate forecast\n`;
            code += `t_forecast = np.arange(0, t_clean.max() + ${forecastDays}, 1)\n`;
            code += `q_forecast = forecast_func(t_forecast)\n\n`;
            code += `# Calculate EUR\n`;
            code += `eur = np.trapz(q_forecast, t_forecast)\n\n`;
            
            code += `print('=== Decline Curve Analysis Results ===')\n`;
            code += `print(f'Decline Type: ${declineType}')\n`;
            code += `print(f'qi: {decline_params["qi"]:.2f}')\n`;
            code += `print(f'Di: {decline_params["Di"]:.6f}')\n`;
            code += `print(f'b: {decline_params["b"]:.2f}')\n`;
            code += `print(f'EUR: {eur:,.0f}')\n`;
            
            context.setVariable(node.id, 'decline_params');
            return code;
        }
    },
    
    // Petrophysical Calculator
    petrophysicalCalculator: {
        type: 'petroleum_petrophysical_calculator',
        name: 'Petrophysical Calculator',
        category: 'petroleum',
        icon: 'bi-calculator',
        color: '#607d8b',
        description: 'Calculate porosity, water saturation, and permeability',
        defaults: {
            rhob_col: 'RHOB',
            nphi_col: 'NPHI',
            rt_col: 'RT',
            rho_matrix: 2.65,
            rho_fluid: 1.0,
            rw: 0.05,
            a: 1.0,
            m: 2.0,
            n: 2.0
        },
        properties: [
            BaseNode.createProperty('rhob_col', 'Density Log (RHOB)', 'text', { default: 'RHOB' }),
            BaseNode.createProperty('nphi_col', 'Neutron Porosity (NPHI)', 'text', { default: 'NPHI' }),
            BaseNode.createProperty('rt_col', 'Resistivity (RT)', 'text', { default: 'RT' }),
            BaseNode.createProperty('rho_matrix', 'Matrix Density', 'number', { default: 2.65, min: 2.0, max: 3.0, step: 0.01 }),
            BaseNode.createProperty('rho_fluid', 'Fluid Density', 'number', { default: 1.0, min: 0.5, max: 1.5, step: 0.1 }),
            BaseNode.createProperty('rw', 'Water Resistivity (Rw)', 'number', { default: 0.05, min: 0.01, max: 1.0, step: 0.01 }),
            BaseNode.createProperty('a', "Archie 'a'", 'number', { default: 1.0, min: 0.5, max: 2.0, step: 0.1 }),
            BaseNode.createProperty('m', "Archie 'm'", 'number', { default: 2.0, min: 1.5, max: 3.0, step: 0.1 }),
            BaseNode.createProperty('n', "Archie 'n'", 'number', { default: 2.0, min: 1.5, max: 3.0, step: 0.1 })
        ],
        inputs: [{ id: 'input', label: 'Well Log Data', type: 'dataframe' }],
        outputs: [{ id: 'output', label: 'Petrophysical Data', type: 'dataframe' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            
            let code = `# Petrophysical Calculations\nimport numpy as np\n\n`;
            
            // Density Porosity
            code += `# Density Porosity\n`;
            code += `if '${data.rhob_col || 'RHOB'}' in ${inputVar}.columns:\n`;
            code += `    rho_matrix = ${data.rho_matrix || 2.65}\n`;
            code += `    rho_fluid = ${data.rho_fluid || 1.0}\n`;
            code += `    ${inputVar}['PHID'] = (rho_matrix - ${inputVar}['${data.rhob_col || 'RHOB'}']) / (rho_matrix - rho_fluid)\n`;
            code += `    ${inputVar}['PHID'] = ${inputVar}['PHID'].clip(0, 0.5)\n\n`;
            
            // Total Porosity
            code += `# Total Porosity\n`;
            code += `if '${data.nphi_col || 'NPHI'}' in ${inputVar}.columns and 'PHID' in ${inputVar}.columns:\n`;
            code += `    ${inputVar}['PHIT'] = (${inputVar}['PHID'] + ${inputVar}['${data.nphi_col || 'NPHI'}']) / 2\n`;
            code += `    ${inputVar}['PHIT'] = ${inputVar}['PHIT'].clip(0, 0.5)\n\n`;
            
            // Water Saturation (Archie)
            code += `# Water Saturation (Archie equation)\n`;
            code += `if '${data.rt_col || 'RT'}' in ${inputVar}.columns and 'PHIT' in ${inputVar}.columns:\n`;
            code += `    Rw, a, m, n = ${data.rw || 0.05}, ${data.a || 1.0}, ${data.m || 2.0}, ${data.n || 2.0}\n`;
            code += `    ${inputVar}['SW'] = np.power(a * Rw / (${inputVar}['${data.rt_col || 'RT'}'] * np.power(${inputVar}['PHIT'], m)), 1/n)\n`;
            code += `    ${inputVar}['SW'] = ${inputVar}['SW'].clip(0, 1)\n`;
            code += `    ${inputVar}['SHC'] = 1 - ${inputVar}['SW']\n\n`;
            
            // Permeability (Timur)
            code += `# Permeability (Timur equation)\n`;
            code += `if 'PHIT' in ${inputVar}.columns and 'SW' in ${inputVar}.columns:\n`;
            code += `    ${inputVar}['PERM'] = 0.136 * np.power(${inputVar}['PHIT'] * 100, 4.4) / np.power(${inputVar}['SW'] * 100 + 0.01, 2)\n`;
            code += `    ${inputVar}['PERM'] = ${inputVar}['PERM'].clip(0.001, 10000)\n\n`;
            
            code += `print('=== Petrophysical Calculations Complete ===')\n`;
            code += `for col in ['PHID', 'PHIT', 'SW', 'SHC', 'PERM']:\n`;
            code += `    if col in ${inputVar}.columns:\n`;
            code += `        print(f'{col}: mean={${inputVar}[col].mean():.4f}')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    },
    
    // Log Normalization
    logNormalization: {
        type: 'petroleum_log_normalization',
        name: 'Log Normalization',
        category: 'petroleum',
        icon: 'bi-sliders',
        color: '#2196f3',
        description: 'Normalize and clean well log data',
        defaults: {
            depth_column: 'DEPTH',
            normalize_gr: true,
            gr_min: 0,
            gr_max: 150
        },
        properties: [
            BaseNode.createProperty('depth_column', 'Depth Column', 'text', { default: 'DEPTH' }),
            BaseNode.createProperty('normalize_gr', 'Normalize GR', 'boolean', { default: true }),
            BaseNode.createProperty('gr_min', 'GR Min', 'number', { default: 0, min: 0 }),
            BaseNode.createProperty('gr_max', 'GR Max', 'number', { default: 150, min: 50 })
        ],
        inputs: [{ id: 'input', label: 'Well Log Data', type: 'dataframe' }],
        outputs: [{ id: 'output', label: 'Normalized Logs', type: 'dataframe' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const depthCol = data.depth_column || 'DEPTH';
            
            let code = `# Log Normalization\nimport numpy as np\n\n`;
            
            code += `# Sort by depth\n`;
            code += `if '${depthCol}' in ${inputVar}.columns:\n`;
            code += `    ${inputVar} = ${inputVar}.sort_values('${depthCol}')\n\n`;
            
            if (data.normalize_gr !== false) {
                code += `# Normalize Gamma Ray\n`;
                code += `if 'GR' in ${inputVar}.columns:\n`;
                code += `    ${inputVar}['GR_NORM'] = (${inputVar}['GR'] - ${data.gr_min || 0}) / (${data.gr_max || 150} - ${data.gr_min || 0})\n`;
                code += `    ${inputVar}['GR_NORM'] = ${inputVar}['GR_NORM'].clip(0, 1)\n\n`;
            }
            
            code += `# Handle common null values in log data\n`;
            code += `for col in ${inputVar}.select_dtypes(include=[np.number]).columns:\n`;
            code += `    if col != '${depthCol}':\n`;
            code += `        ${inputVar}[col] = ${inputVar}[col].replace([-999.25, -999], np.nan)\n`;
            code += `        ${inputVar}[col] = ${inputVar}[col].interpolate(method='linear', limit=5)\n\n`;
            
            code += `print(f'Normalized log data. Shape: {${inputVar}.shape}')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    },
    
    // Lithology Classifier
    lithologyClassifier: {
        type: 'petroleum_lithology_classifier',
        name: 'Lithology Classifier',
        category: 'petroleum',
        icon: 'bi-layers',
        color: '#9c27b0',
        description: 'Classify rock types from well log data',
        defaults: {
            features: 'GR,RHOB,NPHI,DT',
            target: 'LITHOLOGY',
            model_type: 'random_forest'
        },
        properties: [
            BaseNode.createProperty('features', 'Feature Curves', 'text', {
                default: 'GR,RHOB,NPHI,DT',
                help: 'Comma-separated list of log curves to use as features'
            }),
            BaseNode.createProperty('target', 'Lithology Column', 'text', {
                default: 'LITHOLOGY',
                help: 'Column containing lithology labels'
            }),
            BaseNode.createProperty('model_type', 'Model Type', 'select', {
                default: 'random_forest',
                options: [
                    { value: 'random_forest', label: 'Random Forest' },
                    { value: 'gradient_boosting', label: 'Gradient Boosting' },
                    { value: 'svm', label: 'Support Vector Machine' }
                ]
            })
        ],
        inputs: [{ id: 'input', label: 'Well Log Data', type: 'dataframe' }],
        outputs: [{ id: 'output', label: 'Classified Data', type: 'dataframe' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const features = data.features || 'GR,RHOB,NPHI,DT';
            const target = data.target || 'LITHOLOGY';
            
            let code = `# Lithology Classification\n`;
            code += `from sklearn.ensemble import RandomForestClassifier\n`;
            code += `from sklearn.preprocessing import StandardScaler, LabelEncoder\n\n`;
            
            code += `feature_cols = [c.strip() for c in '${features}'.split(',')]\n`;
            code += `available_features = [c for c in feature_cols if c in ${inputVar}.columns]\n\n`;
            
            code += `if available_features and '${target}' in ${inputVar}.columns:\n`;
            code += `    # Prepare data\n`;
            code += `    X = ${inputVar}[available_features].dropna()\n`;
            code += `    y = ${inputVar}.loc[X.index, '${target}']\n\n`;
            code += `    # Encode and scale\n`;
            code += `    le = LabelEncoder()\n`;
            code += `    y_encoded = le.fit_transform(y)\n`;
            code += `    scaler = StandardScaler()\n`;
            code += `    X_scaled = scaler.fit_transform(X)\n\n`;
            code += `    # Train and predict\n`;
            code += `    model = RandomForestClassifier(n_estimators=100, random_state=42)\n`;
            code += `    model.fit(X_scaled, y_encoded)\n`;
            code += `    ${inputVar}.loc[X.index, 'LITHOLOGY_PRED'] = le.inverse_transform(model.predict(X_scaled))\n\n`;
            code += `    print('Lithology Classification Complete')\n`;
            code += `    print(f'Classes: {list(le.classes_)}')\n`;
            code += `else:\n`;
            code += `    print('Cannot perform classification - missing features or target')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    }
};

// Register Petroleum nodes
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(PetroleumNodes, 'Petroleum');
} else if (typeof nodeRegistry !== 'undefined' && typeof BaseNode !== 'undefined') {
    Object.values(PetroleumNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register petroleum node ${nodeDef.type}:`, error);
        }
    });
    console.log('✓ Registered Petroleum nodes');
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PetroleumNodes;
}

