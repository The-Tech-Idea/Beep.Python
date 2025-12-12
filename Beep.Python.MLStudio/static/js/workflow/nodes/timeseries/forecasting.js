/**
 * Time Series Nodes
 * ARIMA, seasonal decomposition, and forecasting
 */

const TimeSeriesNodes = {
    seasonalDecompose: {
        type: 'timeseries_seasonal_decompose',
        name: 'Seasonal Decomposition',
        category: 'timeseries',
        icon: 'bi-graph-up-arrow',
        color: '#1976d2',
        description: 'Decompose time series into trend, seasonal, and residual components',
        defaults: {
            model: 'additive',
            period: null
        },
        properties: [
            BaseNode.createProperty('model', 'Model Type', 'select', {
                default: 'additive',
                options: ['additive', 'multiplicative'],
                help: 'Type of seasonal component'
            }),
            BaseNode.createProperty('period', 'Period', 'number', {
                default: null,
                placeholder: '12 for monthly, 7 for weekly',
                help: 'Period of the series (None for auto-detect)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'ts';
            const model = data.model || 'additive';
            const period = data.period !== null && data.period !== undefined ? data.period : null;
            
            let code = 'from statsmodels.tsa.seasonal import seasonal_decompose\n';
            const params = [`model='${model}'`];
            if (period !== null) {
                params.push(`period=${period}`);
            }
            
            code += `decomposition = seasonal_decompose(${inputVar}, ${params.join(', ')})\n`;
            code += `trend = decomposition.trend\n`;
            code += `seasonal = decomposition.seasonal\n`;
            code += `residual = decomposition.resid\n`;
            code += `print('Decomposition complete')\n`;
            code += `# Plot: decomposition.plot()\n`;
            
            return code;
        }
    },

    arima: {
        type: 'timeseries_arima',
        name: 'ARIMA Model',
        category: 'timeseries',
        icon: 'bi-graph-up',
        color: '#0277bd',
        description: 'ARIMA (AutoRegressive Integrated Moving Average) model',
        defaults: {
            order: '1,1,1',
            seasonal_order: null
        },
        properties: [
            BaseNode.createProperty('order', 'ARIMA Order (p,d,q)', 'text', {
                required: true,
                default: '1,1,1',
                placeholder: '1,1,1',
                help: 'Comma-separated: p (AR), d (I), q (MA)'
            }),
            BaseNode.createProperty('seasonal_order', 'Seasonal Order', 'text', {
                placeholder: '1,1,1,12 or leave empty',
                help: 'Comma-separated: P, D, Q, s (for SARIMA)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'ts';
            const order = data.order || '1,1,1';
            const seasonalOrder = data.seasonal_order || null;
            
            const [p, d, q] = order.split(',').map(x => x.trim());
            
            let code = 'from statsmodels.tsa.arima.model import ARIMA\n';
            
            if (seasonalOrder) {
                code += 'from statsmodels.tsa.statespace.sarimax import SARIMAX\n';
                const [P, D, Q, s] = seasonalOrder.split(',').map(x => x.trim());
                code += `model = SARIMAX(${inputVar}, order=(${p}, ${d}, ${q}), seasonal_order=(${P}, ${D}, ${Q}, ${s}))\n`;
            } else {
                code += `model = ARIMA(${inputVar}, order=(${p}, ${d}, ${q}))\n`;
            }
            
            code += `fitted_model = model.fit()\n`;
            code += `print(fitted_model.summary())\n`;
            code += `forecast = fitted_model.forecast(steps=10)\n`;
            code += `print(f'Forecast: {forecast}')\n`;
            
            return code;
        }
    },

    autoArima: {
        type: 'timeseries_auto_arima',
        name: 'Auto ARIMA',
        category: 'timeseries',
        icon: 'bi-magic',
        color: '#e65100',
        description: 'Automatically find optimal ARIMA parameters (requires pmdarima)',
        defaults: {
            seasonal: true,
            m: 12
        },
        properties: [
            BaseNode.createProperty('seasonal', 'Seasonal', 'boolean', {
                default: true,
                help: 'Include seasonal component'
            }),
            BaseNode.createProperty('m', 'Seasonal Period', 'number', {
                default: 12,
                min: 2,
                max: 52,
                help: 'Period of seasonality (12 for monthly)'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context ? context.getInputVariable(node) : 'ts';
            const seasonal = data.seasonal !== false;
            const m = data.m || 12;
            
            let code = 'from pmdarima import auto_arima\n';
            code += `model = auto_arima(${inputVar}, seasonal=${seasonal}, m=${m}, suppress_warnings=True)\n`;
            code += `print(f'Best ARIMA order: {model.order}')\n`;
            if (seasonal) {
                code += `print(f'Best seasonal order: {model.seasonal_order}')\n`;
            }
            code += `forecast = model.predict(n_periods=10)\n`;
            code += `print(f'Forecast: {forecast}')\n`;
            
            return code;
        }
    },

    forecast: {
        type: 'timeseries_forecast',
        name: 'Forecast',
        category: 'timeseries',
        icon: 'bi-calculator',
        color: '#2e7d32',
        description: 'Generate future predictions from time series model',
        defaults: {
            steps: 10,
            model_variable: 'fitted_model'
        },
        properties: [
            BaseNode.createProperty('model_variable', 'Model Variable', 'text', {
                required: true,
                default: 'fitted_model',
                help: 'Variable name of fitted model'
            }),
            BaseNode.createProperty('steps', 'Forecast Steps', 'number', {
                default: 10,
                min: 1,
                max: 1000,
                help: 'Number of periods to forecast'
            })
        ],
        generateCode: (node, context) => {
            const data = node.data || {};
            const modelVar = data.model_variable || 'fitted_model';
            const steps = data.steps || 10;
            
            let code = `forecast = ${modelVar}.forecast(steps=${steps})\n`;
            code += `forecast_ci = ${modelVar}.get_forecast(steps=${steps}).conf_int()\n`;
            code += `print(f'Forecast for next {steps} periods:')\n`;
            code += `print(forecast)\n`;
            code += `print(f'Confidence intervals:')\n`;
            code += `print(forecast_ci)\n`;
            
            return code;
        }
    }
};

// Register all time series nodes
Object.values(TimeSeriesNodes).forEach(nodeDef => {
    BaseNode.validateDefinition(nodeDef);
    nodeRegistry.register(nodeDef);
});

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = TimeSeriesNodes;
}

