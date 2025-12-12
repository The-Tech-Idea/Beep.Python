/**
 * Finance & Economics Industry Nodes
 * Specialized nodes for financial analysis, trading, and risk management
 */

const FinanceNodes = {
    // Stock Data Loader
    stockDataLoader: {
        type: 'finance_stock_data_loader',
        name: 'Stock Data Loader',
        category: 'finance',
        icon: 'bi-graph-up',
        color: '#28a745',
        description: 'Load stock price data from file or API',
        defaults: {
            source: 'csv_file',
            symbol: 'AAPL',
            file_path: 'data/stock_data.csv',
            start_date: '2020-01-01',
            end_date: '2024-01-01'
        },
        properties: [
            BaseNode.createProperty('source', 'Data Source', 'select', {
                default: 'csv_file',
                options: [
                    { value: 'csv_file', label: 'CSV File' },
                    { value: 'yahoo_finance', label: 'Yahoo Finance (requires yfinance)' }
                ],
                help: 'Source of stock data'
            }),
            BaseNode.createProperty('file_path', 'File Path', 'file', {
                default: 'data/stock_data.csv',
                placeholder: 'data/stock_data.csv',
                help: 'Path to CSV file with stock data',
                fileFilter: '.csv'
            }),
            BaseNode.createProperty('symbol', 'Stock Symbol', 'text', {
                default: 'AAPL',
                placeholder: 'AAPL',
                help: 'Stock ticker symbol (for Yahoo Finance)'
            }),
            BaseNode.createProperty('start_date', 'Start Date', 'text', {
                default: '2020-01-01',
                placeholder: 'YYYY-MM-DD',
                help: 'Start date for data'
            }),
            BaseNode.createProperty('end_date', 'End Date', 'text', {
                default: '2024-01-01',
                placeholder: 'YYYY-MM-DD',
                help: 'End date for data'
            })
        ],
        inputs: [],
        outputs: [{ id: 'output', label: 'Stock Data', type: 'dataframe' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const source = data.source || 'csv_file';
            const filePath = data.file_path || 'data/stock_data.csv';
            const symbol = data.symbol || 'AAPL';
            
            let code = `# Load Stock Data\nimport pandas as pd\nimport numpy as np\n\n`;
            
            if (source === 'csv_file') {
                code += `df = pd.read_csv('${filePath}')\n`;
                code += `# Try to parse date column if exists\n`;
                code += `date_cols = [c for c in df.columns if 'date' in c.lower()]\n`;
                code += `if date_cols:\n`;
                code += `    df[date_cols[0]] = pd.to_datetime(df[date_cols[0]])\n`;
                code += `    df = df.set_index(date_cols[0]).sort_index()\n`;
            } else {
                code += `# Yahoo Finance (requires: pip install yfinance)\n`;
                code += `try:\n`;
                code += `    import yfinance as yf\n`;
                code += `    df = yf.download('${symbol}', start='${data.start_date}', end='${data.end_date}')\n`;
                code += `except ImportError:\n`;
                code += `    print("yfinance not installed. Using sample data.")\n`;
                code += `    df = pd.read_csv('data/stock_data.csv')\n`;
            }
            
            code += `\nprint(f'Loaded stock data: {df.shape}')\n`;
            code += `print(f'Columns: {list(df.columns)}')\n`;
            
            context.setVariable(node.id, 'df');
            return code;
        }
    },
    
    // Technical Indicators
    technicalIndicators: {
        type: 'finance_technical_indicators',
        name: 'Technical Indicators',
        category: 'finance',
        icon: 'bi-bar-chart-line',
        color: '#17a2b8',
        description: 'Calculate SMA, EMA, RSI, MACD, Bollinger Bands',
        defaults: {
            price_column: 'Close',
            sma_periods: '20,50',
            ema_periods: '12,26',
            rsi_period: 14,
            bb_period: 20,
            include_macd: true
        },
        properties: [
            BaseNode.createProperty('price_column', 'Price Column', 'text', {
                default: 'Close',
                help: 'Column containing price data'
            }),
            BaseNode.createProperty('sma_periods', 'SMA Periods', 'text', {
                default: '20,50',
                placeholder: '20,50,200',
                help: 'Comma-separated periods for Simple Moving Average'
            }),
            BaseNode.createProperty('ema_periods', 'EMA Periods', 'text', {
                default: '12,26',
                placeholder: '12,26',
                help: 'Comma-separated periods for Exponential Moving Average'
            }),
            BaseNode.createProperty('rsi_period', 'RSI Period', 'number', {
                default: 14,
                min: 2,
                max: 100,
                help: 'Period for Relative Strength Index'
            }),
            BaseNode.createProperty('bb_period', 'Bollinger Bands Period', 'number', {
                default: 20,
                min: 2,
                max: 100,
                help: 'Period for Bollinger Bands'
            }),
            BaseNode.createProperty('include_macd', 'Include MACD', 'boolean', {
                default: true,
                help: 'Calculate MACD indicator'
            })
        ],
        inputs: [{ id: 'input', label: 'Stock Data', type: 'dataframe' }],
        outputs: [{ id: 'output', label: 'Data with Indicators', type: 'dataframe' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const priceCol = data.price_column || 'Close';
            const smaPeriods = data.sma_periods || '20,50';
            const emaPeriods = data.ema_periods || '12,26';
            const rsiPeriod = data.rsi_period || 14;
            const bbPeriod = data.bb_period || 20;
            const includeMACD = data.include_macd !== false;
            
            let code = `# Calculate Technical Indicators\nimport numpy as np\n\n`;
            
            // SMA
            code += `# Simple Moving Averages\n`;
            code += `for period in [${smaPeriods}]:\n`;
            code += `    ${inputVar}[f'SMA_{period}'] = ${inputVar}['${priceCol}'].rolling(window=period).mean()\n\n`;
            
            // EMA
            code += `# Exponential Moving Averages\n`;
            code += `for period in [${emaPeriods}]:\n`;
            code += `    ${inputVar}[f'EMA_{period}'] = ${inputVar}['${priceCol}'].ewm(span=period, adjust=False).mean()\n\n`;
            
            // RSI
            code += `# RSI (Relative Strength Index)\n`;
            code += `delta = ${inputVar}['${priceCol}'].diff()\n`;
            code += `gain = (delta.where(delta > 0, 0)).rolling(window=${rsiPeriod}).mean()\n`;
            code += `loss = (-delta.where(delta < 0, 0)).rolling(window=${rsiPeriod}).mean()\n`;
            code += `rs = gain / loss\n`;
            code += `${inputVar}['RSI'] = 100 - (100 / (1 + rs))\n\n`;
            
            // Bollinger Bands
            code += `# Bollinger Bands\n`;
            code += `${inputVar}['BB_Middle'] = ${inputVar}['${priceCol}'].rolling(window=${bbPeriod}).mean()\n`;
            code += `bb_std = ${inputVar}['${priceCol}'].rolling(window=${bbPeriod}).std()\n`;
            code += `${inputVar}['BB_Upper'] = ${inputVar}['BB_Middle'] + (bb_std * 2)\n`;
            code += `${inputVar}['BB_Lower'] = ${inputVar}['BB_Middle'] - (bb_std * 2)\n\n`;
            
            // MACD
            if (includeMACD) {
                code += `# MACD\n`;
                code += `${inputVar}['MACD'] = ${inputVar}['${priceCol}'].ewm(span=12, adjust=False).mean() - ${inputVar}['${priceCol}'].ewm(span=26, adjust=False).mean()\n`;
                code += `${inputVar}['MACD_Signal'] = ${inputVar}['MACD'].ewm(span=9, adjust=False).mean()\n`;
                code += `${inputVar}['MACD_Histogram'] = ${inputVar}['MACD'] - ${inputVar}['MACD_Signal']\n\n`;
            }
            
            code += `# Drop NaN rows from indicator calculations\n`;
            code += `${inputVar} = ${inputVar}.dropna()\n`;
            code += `print(f'Added technical indicators. Shape: {${inputVar}.shape}')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    },
    
    // Returns Calculator
    returnsCalculator: {
        type: 'finance_returns_calculator',
        name: 'Returns Calculator',
        category: 'finance',
        icon: 'bi-percent',
        color: '#ffc107',
        description: 'Calculate daily, log, and cumulative returns',
        defaults: {
            price_column: 'Close',
            daily_returns: true,
            log_returns: true,
            cumulative_returns: true,
            volatility_window: 20
        },
        properties: [
            BaseNode.createProperty('price_column', 'Price Column', 'text', {
                default: 'Close',
                help: 'Column containing price data'
            }),
            BaseNode.createProperty('daily_returns', 'Daily Returns', 'boolean', {
                default: true,
                help: 'Calculate daily percentage returns'
            }),
            BaseNode.createProperty('log_returns', 'Log Returns', 'boolean', {
                default: true,
                help: 'Calculate logarithmic returns'
            }),
            BaseNode.createProperty('cumulative_returns', 'Cumulative Returns', 'boolean', {
                default: true,
                help: 'Calculate cumulative returns'
            }),
            BaseNode.createProperty('volatility_window', 'Volatility Window', 'number', {
                default: 20,
                min: 5,
                max: 252,
                help: 'Rolling window for volatility calculation'
            })
        ],
        inputs: [{ id: 'input', label: 'Price Data', type: 'dataframe' }],
        outputs: [{ id: 'output', label: 'Returns Data', type: 'dataframe' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const priceCol = data.price_column || 'Close';
            const volWindow = data.volatility_window || 20;
            
            let code = `# Calculate Returns\nimport numpy as np\n\n`;
            
            if (data.daily_returns !== false) {
                code += `${inputVar}['Daily_Return'] = ${inputVar}['${priceCol}'].pct_change()\n`;
            }
            
            if (data.log_returns !== false) {
                code += `${inputVar}['Log_Return'] = np.log(${inputVar}['${priceCol}'] / ${inputVar}['${priceCol}'].shift(1))\n`;
            }
            
            if (data.cumulative_returns !== false) {
                code += `${inputVar}['Cumulative_Return'] = (1 + ${inputVar}['Daily_Return']).cumprod() - 1\n`;
            }
            
            code += `\n# Rolling Volatility (annualized)\n`;
            code += `${inputVar}['Volatility_${volWindow}d'] = ${inputVar}['Daily_Return'].rolling(window=${volWindow}).std() * np.sqrt(252)\n\n`;
            
            code += `${inputVar} = ${inputVar}.dropna()\n`;
            code += `print(f'Calculated returns. Shape: {${inputVar}.shape}')\n`;
            
            context.setVariable(node.id, inputVar);
            return code;
        }
    },
    
    // Risk Metrics
    riskMetrics: {
        type: 'finance_risk_metrics',
        name: 'Risk Metrics',
        category: 'finance',
        icon: 'bi-shield-exclamation',
        color: '#dc3545',
        description: 'Calculate VaR, Sharpe Ratio, Max Drawdown, Sortino Ratio',
        defaults: {
            returns_column: 'Daily_Return',
            confidence_level: 0.95,
            risk_free_rate: 0.02
        },
        properties: [
            BaseNode.createProperty('returns_column', 'Returns Column', 'text', {
                default: 'Daily_Return',
                help: 'Column containing return data'
            }),
            BaseNode.createProperty('confidence_level', 'VaR Confidence', 'number', {
                default: 0.95,
                min: 0.9,
                max: 0.99,
                step: 0.01,
                help: 'Confidence level for VaR calculation'
            }),
            BaseNode.createProperty('risk_free_rate', 'Risk-Free Rate', 'number', {
                default: 0.02,
                min: 0,
                max: 0.2,
                step: 0.01,
                help: 'Annual risk-free rate'
            })
        ],
        inputs: [{ id: 'input', label: 'Returns Data', type: 'dataframe' }],
        outputs: [{ id: 'output', label: 'Risk Metrics', type: 'dict' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const returnsCol = data.returns_column || 'Daily_Return';
            const confidence = data.confidence_level || 0.95;
            const riskFree = data.risk_free_rate || 0.02;
            
            let code = `# Calculate Risk Metrics\nimport numpy as np\n\n`;
            code += `returns = ${inputVar}['${returnsCol}'].dropna()\n\n`;
            
            // VaR
            code += `# Value at Risk (VaR)\n`;
            code += `var_${confidence.toString().replace('.', '_')} = np.percentile(returns, (1 - ${confidence}) * 100)\n`;
            code += `cvar = returns[returns <= var_${confidence.toString().replace('.', '_')}].mean()\n\n`;
            
            // Sharpe Ratio
            code += `# Sharpe Ratio (annualized)\n`;
            code += `risk_free_daily = ${riskFree} / 252\n`;
            code += `excess_returns = returns - risk_free_daily\n`;
            code += `sharpe_ratio = np.sqrt(252) * excess_returns.mean() / excess_returns.std()\n\n`;
            
            // Max Drawdown
            code += `# Max Drawdown\n`;
            code += `cumulative = (1 + returns).cumprod()\n`;
            code += `running_max = cumulative.cummax()\n`;
            code += `drawdown = (cumulative - running_max) / running_max\n`;
            code += `max_drawdown = drawdown.min()\n\n`;
            
            // Sortino Ratio
            code += `# Sortino Ratio\n`;
            code += `downside_returns = returns[returns < 0]\n`;
            code += `sortino_ratio = np.sqrt(252) * excess_returns.mean() / downside_returns.std() if len(downside_returns) > 0 else 0\n\n`;
            
            // Output metrics
            code += `risk_metrics = {\n`;
            code += `    'VaR_${confidence}': var_${confidence.toString().replace('.', '_')},\n`;
            code += `    'CVaR': cvar,\n`;
            code += `    'Sharpe_Ratio': sharpe_ratio,\n`;
            code += `    'Sortino_Ratio': sortino_ratio,\n`;
            code += `    'Max_Drawdown': max_drawdown,\n`;
            code += `    'Annual_Volatility': returns.std() * np.sqrt(252),\n`;
            code += `    'Annual_Return': returns.mean() * 252\n`;
            code += `}\n\n`;
            
            code += `print('=== Risk Metrics ===')\n`;
            code += `for metric, value in risk_metrics.items():\n`;
            code += `    print(f'{metric}: {value:.4f}')\n`;
            
            context.setVariable(node.id, 'risk_metrics');
            return code;
        }
    },
    
    // Portfolio Optimizer
    portfolioOptimizer: {
        type: 'finance_portfolio_optimizer',
        name: 'Portfolio Optimizer',
        category: 'finance',
        icon: 'bi-pie-chart',
        color: '#20c997',
        description: 'Optimize portfolio weights using Mean-Variance optimization',
        defaults: {
            optimization_target: 'max_sharpe',
            risk_free_rate: 0.02,
            min_weight: 0,
            max_weight: 1
        },
        properties: [
            BaseNode.createProperty('optimization_target', 'Optimization Target', 'select', {
                default: 'max_sharpe',
                options: [
                    { value: 'max_sharpe', label: 'Maximum Sharpe Ratio' },
                    { value: 'min_volatility', label: 'Minimum Volatility' },
                    { value: 'max_return', label: 'Maximum Return' }
                ],
                help: 'What to optimize for'
            }),
            BaseNode.createProperty('risk_free_rate', 'Risk-Free Rate', 'number', {
                default: 0.02,
                min: 0,
                max: 0.2,
                step: 0.01,
                help: 'Annual risk-free rate'
            }),
            BaseNode.createProperty('min_weight', 'Min Weight', 'number', {
                default: 0,
                min: 0,
                max: 1,
                step: 0.05,
                help: 'Minimum weight per asset'
            }),
            BaseNode.createProperty('max_weight', 'Max Weight', 'number', {
                default: 1,
                min: 0,
                max: 1,
                step: 0.05,
                help: 'Maximum weight per asset'
            })
        ],
        inputs: [{ id: 'input', label: 'Price/Returns Data', type: 'dataframe' }],
        outputs: [{ id: 'output', label: 'Optimal Weights', type: 'dict' }],
        generateCode: (node, context) => {
            const data = node.data || {};
            const inputVar = context.getInputVariable(node) || 'df';
            const target = data.optimization_target || 'max_sharpe';
            const riskFree = data.risk_free_rate || 0.02;
            const minWeight = data.min_weight || 0;
            const maxWeight = data.max_weight || 1;
            
            let code = `# Portfolio Optimization\nimport numpy as np\nfrom scipy.optimize import minimize\n\n`;
            
            code += `# Calculate returns if not already done\n`;
            code += `if 'Daily_Return' not in ${inputVar}.columns:\n`;
            code += `    returns_df = ${inputVar}.pct_change().dropna()\n`;
            code += `else:\n`;
            code += `    returns_df = ${inputVar}[['Daily_Return']].dropna()\n\n`;
            
            code += `# If single column, we can't optimize - need multiple assets\n`;
            code += `if returns_df.shape[1] == 1:\n`;
            code += `    print("Warning: Single asset - portfolio optimization requires multiple assets")\n`;
            code += `    optimal_weights = {returns_df.columns[0]: 1.0}\n`;
            code += `else:\n`;
            code += `    # Calculate expected returns and covariance\n`;
            code += `    expected_returns = returns_df.mean() * 252\n`;
            code += `    cov_matrix = returns_df.cov() * 252\n`;
            code += `    n_assets = len(expected_returns)\n\n`;
            
            code += `    def portfolio_volatility(weights):\n`;
            code += `        return np.sqrt(np.dot(weights.T, np.dot(cov_matrix, weights)))\n\n`;
            
            code += `    def portfolio_return(weights):\n`;
            code += `        return np.dot(weights, expected_returns)\n\n`;
            
            code += `    def neg_sharpe_ratio(weights):\n`;
            code += `        p_ret = portfolio_return(weights)\n`;
            code += `        p_vol = portfolio_volatility(weights)\n`;
            code += `        return -(p_ret - ${riskFree}) / p_vol if p_vol > 0 else 0\n\n`;
            
            code += `    # Constraints and bounds\n`;
            code += `    constraints = [{'type': 'eq', 'fun': lambda x: np.sum(x) - 1}]\n`;
            code += `    bounds = tuple((${minWeight}, ${maxWeight}) for _ in range(n_assets))\n`;
            code += `    init_weights = np.array([1/n_assets] * n_assets)\n\n`;
            
            if (target === 'max_sharpe') {
                code += `    # Maximize Sharpe Ratio\n`;
                code += `    result = minimize(neg_sharpe_ratio, init_weights, method='SLSQP', bounds=bounds, constraints=constraints)\n`;
            } else if (target === 'min_volatility') {
                code += `    # Minimize Volatility\n`;
                code += `    result = minimize(portfolio_volatility, init_weights, method='SLSQP', bounds=bounds, constraints=constraints)\n`;
            } else {
                code += `    # Maximize Return\n`;
                code += `    result = minimize(lambda w: -portfolio_return(w), init_weights, method='SLSQP', bounds=bounds, constraints=constraints)\n`;
            }
            
            code += `\n    optimal_weights = dict(zip(returns_df.columns, result.x))\n\n`;
            
            code += `print('=== Optimal Portfolio Weights ===')\n`;
            code += `for asset, weight in optimal_weights.items():\n`;
            code += `    if weight > 0.001:  # Only show non-zero weights\n`;
            code += `        print(f'{asset}: {weight:.2%}')\n`;
            
            context.setVariable(node.id, 'optimal_weights');
            return code;
        }
    }
};

// Register Finance nodes
if (typeof registerNodesSafely !== 'undefined') {
    registerNodesSafely(FinanceNodes, 'Finance');
} else if (typeof nodeRegistry !== 'undefined' && typeof BaseNode !== 'undefined') {
    Object.values(FinanceNodes).forEach(nodeDef => {
        try {
            BaseNode.validateDefinition(nodeDef);
            nodeRegistry.register(nodeDef);
        } catch (error) {
            console.error(`Failed to register finance node ${nodeDef.type}:`, error);
        }
    });
    console.log('✓ Registered Finance nodes');
}

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FinanceNodes;
}

