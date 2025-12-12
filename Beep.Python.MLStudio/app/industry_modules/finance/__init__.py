"""
Finance & Economics Industry Module
Specialized ML workflows for financial analysis, trading, and economics
"""

from ..base_module import (
    IndustryModule, ModuleCategory, NodeDefinition, 
    WorkflowTemplate, SampleDataset, ModuleRegistry
)
from typing import Dict, List, Any


class FinanceModule(IndustryModule):
    """
    Finance & Economics Module
    
    Provides specialized nodes and templates for:
    - Stock price prediction
    - Credit risk assessment
    - Fraud detection
    - Portfolio optimization
    - Time series forecasting
    - Economic indicators analysis
    """
    
    @property
    def id(self) -> str:
        return "finance"
    
    @property
    def name(self) -> str:
        return "Finance & Economics"
    
    @property
    def description(self) -> str:
        return "ML tools for financial analysis, trading strategies, risk assessment, and economic forecasting"
    
    @property
    def category(self) -> ModuleCategory:
        return ModuleCategory.FINANCE
    
    @property
    def version(self) -> str:
        return "1.0.0"
    
    @property
    def icon(self) -> str:
        return "bi-graph-up-arrow"
    
    @property
    def color(self) -> str:
        return "#28a745"  # Green for finance
    
    def _initialize(self):
        """Initialize finance-specific nodes, templates, and datasets"""
        self._register_nodes()
        self._register_templates()
        self._register_datasets()
    
    def _register_nodes(self):
        """Register finance-specific nodes"""
        
        # Stock Data Loader
        self.register_node(NodeDefinition(
            id="stock_data_loader",
            name="Stock Data Loader",
            category="data_source",
            description="Load stock price data from Yahoo Finance or CSV",
            icon="bi-graph-up",
            color="#28a745",
            inputs=[],
            outputs=[{"id": "output", "label": "Stock Data", "type": "dataframe"}],
            properties=[
                {"name": "source", "label": "Data Source", "type": "select", 
                 "options": ["yahoo_finance", "csv_file", "api"], "default": "csv_file"},
                {"name": "symbol", "label": "Stock Symbol", "type": "text", "default": "AAPL"},
                {"name": "start_date", "label": "Start Date", "type": "date", "default": "2020-01-01"},
                {"name": "end_date", "label": "End Date", "type": "date", "default": "2024-01-01"},
                {"name": "file_path", "label": "CSV File Path", "type": "file", "default": ""},
            ],
            imports=["import pandas as pd", "import numpy as np"],
            code_template='''# Load Stock Data
import pandas as pd
import numpy as np

{%- if source == "csv_file" %}
df = pd.read_csv('{{ file_path }}', parse_dates=['Date'], index_col='Date')
{%- else %}
# For Yahoo Finance, you would need yfinance: pip install yfinance
# import yfinance as yf
# df = yf.download('{{ symbol }}', start='{{ start_date }}', end='{{ end_date }}')
df = pd.read_csv('data/stock_data.csv', parse_dates=['Date'], index_col='Date')
{%- endif %}
df = df.sort_index()
print(f'Loaded stock data: {df.shape}')
print(f'Date range: {df.index.min()} to {df.index.max()}')
'''
        ))
        
        # Technical Indicators
        self.register_node(NodeDefinition(
            id="technical_indicators",
            name="Technical Indicators",
            category="feature_engineering",
            description="Calculate common technical indicators (SMA, EMA, RSI, MACD, Bollinger Bands)",
            icon="bi-bar-chart-line",
            color="#17a2b8",
            inputs=[{"id": "input", "label": "Stock Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "Data with Indicators", "type": "dataframe"}],
            properties=[
                {"name": "sma_periods", "label": "SMA Periods", "type": "text", "default": "20,50,200"},
                {"name": "ema_periods", "label": "EMA Periods", "type": "text", "default": "12,26"},
                {"name": "rsi_period", "label": "RSI Period", "type": "number", "default": 14},
                {"name": "bb_period", "label": "Bollinger Bands Period", "type": "number", "default": 20},
                {"name": "macd", "label": "Include MACD", "type": "boolean", "default": True},
            ],
            imports=["import pandas as pd", "import numpy as np"],
            code_template='''# Calculate Technical Indicators
import pandas as pd
import numpy as np

# Simple Moving Averages
for period in [{{ sma_periods }}]:
    df[f'SMA_{period}'] = df['Close'].rolling(window=period).mean()

# Exponential Moving Averages
for period in [{{ ema_periods }}]:
    df[f'EMA_{period}'] = df['Close'].ewm(span=period, adjust=False).mean()

# RSI (Relative Strength Index)
delta = df['Close'].diff()
gain = (delta.where(delta > 0, 0)).rolling(window={{ rsi_period }}).mean()
loss = (-delta.where(delta < 0, 0)).rolling(window={{ rsi_period }}).mean()
rs = gain / loss
df['RSI'] = 100 - (100 / (1 + rs))

# Bollinger Bands
df['BB_Middle'] = df['Close'].rolling(window={{ bb_period }}).mean()
bb_std = df['Close'].rolling(window={{ bb_period }}).std()
df['BB_Upper'] = df['BB_Middle'] + (bb_std * 2)
df['BB_Lower'] = df['BB_Middle'] - (bb_std * 2)

{%- if macd %}
# MACD
df['MACD'] = df['Close'].ewm(span=12, adjust=False).mean() - df['Close'].ewm(span=26, adjust=False).mean()
df['MACD_Signal'] = df['MACD'].ewm(span=9, adjust=False).mean()
df['MACD_Histogram'] = df['MACD'] - df['MACD_Signal']
{%- endif %}

# Drop NaN rows from indicator calculations
df = df.dropna()
print(f'Added technical indicators. Shape: {df.shape}')
print(f'New columns: {[c for c in df.columns if c not in ["Open", "High", "Low", "Close", "Volume"]]}')
'''
        ))
        
        # Returns Calculator
        self.register_node(NodeDefinition(
            id="returns_calculator",
            name="Returns Calculator",
            category="feature_engineering",
            description="Calculate various return metrics (daily, log, cumulative)",
            icon="bi-percent",
            color="#ffc107",
            inputs=[{"id": "input", "label": "Price Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "Data with Returns", "type": "dataframe"}],
            properties=[
                {"name": "price_column", "label": "Price Column", "type": "text", "default": "Close"},
                {"name": "daily_returns", "label": "Daily Returns", "type": "boolean", "default": True},
                {"name": "log_returns", "label": "Log Returns", "type": "boolean", "default": True},
                {"name": "cumulative_returns", "label": "Cumulative Returns", "type": "boolean", "default": True},
                {"name": "volatility_window", "label": "Volatility Window", "type": "number", "default": 20},
            ],
            imports=["import pandas as pd", "import numpy as np"],
            code_template='''# Calculate Returns
import numpy as np

price_col = '{{ price_column }}'

{%- if daily_returns %}
df['Daily_Return'] = df[price_col].pct_change()
{%- endif %}

{%- if log_returns %}
df['Log_Return'] = np.log(df[price_col] / df[price_col].shift(1))
{%- endif %}

{%- if cumulative_returns %}
df['Cumulative_Return'] = (1 + df['Daily_Return']).cumprod() - 1
{%- endif %}

# Rolling Volatility
df['Volatility_{{ volatility_window }}d'] = df['Daily_Return'].rolling(window={{ volatility_window }}).std() * np.sqrt(252)

df = df.dropna()
print(f'Calculated returns. Shape: {df.shape}')
'''
        ))
        
        # Risk Metrics
        self.register_node(NodeDefinition(
            id="risk_metrics",
            name="Risk Metrics",
            category="evaluation",
            description="Calculate financial risk metrics (VaR, Sharpe Ratio, Max Drawdown)",
            icon="bi-shield-exclamation",
            color="#dc3545",
            inputs=[{"id": "input", "label": "Returns Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "Risk Metrics", "type": "dict"}],
            properties=[
                {"name": "returns_column", "label": "Returns Column", "type": "text", "default": "Daily_Return"},
                {"name": "confidence_level", "label": "VaR Confidence Level", "type": "number", "default": 0.95},
                {"name": "risk_free_rate", "label": "Risk-Free Rate (annual)", "type": "number", "default": 0.02},
            ],
            imports=["import pandas as pd", "import numpy as np", "from scipy import stats"],
            code_template='''# Calculate Risk Metrics
import numpy as np
from scipy import stats

returns = df['{{ returns_column }}'].dropna()

# Value at Risk (VaR)
var_{{ confidence_level|replace(".", "_") }} = np.percentile(returns, (1 - {{ confidence_level }}) * 100)
cvar = returns[returns <= var_{{ confidence_level|replace(".", "_") }}].mean()

# Sharpe Ratio (annualized)
risk_free_daily = {{ risk_free_rate }} / 252
excess_returns = returns - risk_free_daily
sharpe_ratio = np.sqrt(252) * excess_returns.mean() / excess_returns.std()

# Max Drawdown
cumulative = (1 + returns).cumprod()
running_max = cumulative.cummax()
drawdown = (cumulative - running_max) / running_max
max_drawdown = drawdown.min()

# Sortino Ratio
downside_returns = returns[returns < 0]
sortino_ratio = np.sqrt(252) * excess_returns.mean() / downside_returns.std()

risk_metrics = {
    'VaR_{{ confidence_level }}': var_{{ confidence_level|replace(".", "_") }},
    'CVaR': cvar,
    'Sharpe_Ratio': sharpe_ratio,
    'Sortino_Ratio': sortino_ratio,
    'Max_Drawdown': max_drawdown,
    'Volatility_Annual': returns.std() * np.sqrt(252),
    'Mean_Return_Annual': returns.mean() * 252
}

print('Risk Metrics:')
for metric, value in risk_metrics.items():
    print(f'  {metric}: {value:.4f}')
'''
        ))
        
        # Credit Risk Scorer
        self.register_node(NodeDefinition(
            id="credit_risk_scorer",
            name="Credit Risk Scorer",
            category="evaluation",
            description="Calculate credit risk scores using logistic regression or gradient boosting",
            icon="bi-credit-card-2-front",
            color="#6f42c1",
            inputs=[{"id": "input", "label": "Credit Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "Risk Scores", "type": "dataframe"}],
            properties=[
                {"name": "target_column", "label": "Default Column", "type": "text", "default": "default"},
                {"name": "model_type", "label": "Model Type", "type": "select", 
                 "options": ["logistic_regression", "gradient_boosting", "random_forest"], "default": "logistic_regression"},
            ],
            imports=["from sklearn.linear_model import LogisticRegression", 
                     "from sklearn.ensemble import GradientBoostingClassifier, RandomForestClassifier",
                     "from sklearn.preprocessing import StandardScaler"],
            code_template='''# Credit Risk Scoring
from sklearn.preprocessing import StandardScaler
{%- if model_type == "logistic_regression" %}
from sklearn.linear_model import LogisticRegression
model = LogisticRegression(random_state=42, max_iter=1000)
{%- elif model_type == "gradient_boosting" %}
from sklearn.ensemble import GradientBoostingClassifier
model = GradientBoostingClassifier(random_state=42)
{%- else %}
from sklearn.ensemble import RandomForestClassifier
model = RandomForestClassifier(random_state=42)
{%- endif %}

# Prepare features
y = df['{{ target_column }}']
X = df.drop(columns=['{{ target_column }}'])

# Scale features
scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

# Fit model
model.fit(X_scaled, y)

# Get probability scores
df['Risk_Score'] = model.predict_proba(X_scaled)[:, 1]
df['Risk_Category'] = pd.cut(df['Risk_Score'], bins=[0, 0.3, 0.6, 1.0], labels=['Low', 'Medium', 'High'])

print(f'Credit risk scores calculated')
print(f'Risk distribution: {df["Risk_Category"].value_counts().to_dict()}')
'''
        ))
        
        # Portfolio Optimizer
        self.register_node(NodeDefinition(
            id="portfolio_optimizer",
            name="Portfolio Optimizer",
            category="optimization",
            description="Optimize portfolio weights using Mean-Variance optimization",
            icon="bi-pie-chart",
            color="#20c997",
            inputs=[{"id": "input", "label": "Returns Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "Optimal Weights", "type": "dict"}],
            properties=[
                {"name": "optimization_target", "label": "Optimization Target", "type": "select",
                 "options": ["max_sharpe", "min_volatility", "max_return"], "default": "max_sharpe"},
                {"name": "risk_free_rate", "label": "Risk-Free Rate", "type": "number", "default": 0.02},
                {"name": "min_weight", "label": "Min Weight per Asset", "type": "number", "default": 0.0},
                {"name": "max_weight", "label": "Max Weight per Asset", "type": "number", "default": 1.0},
            ],
            imports=["import numpy as np", "from scipy.optimize import minimize"],
            code_template='''# Portfolio Optimization
import numpy as np
from scipy.optimize import minimize

# Calculate expected returns and covariance
returns_df = df.pct_change().dropna()
expected_returns = returns_df.mean() * 252
cov_matrix = returns_df.cov() * 252

n_assets = len(expected_returns)

def portfolio_volatility(weights):
    return np.sqrt(np.dot(weights.T, np.dot(cov_matrix, weights)))

def portfolio_return(weights):
    return np.dot(weights, expected_returns)

def neg_sharpe_ratio(weights):
    p_ret = portfolio_return(weights)
    p_vol = portfolio_volatility(weights)
    return -(p_ret - {{ risk_free_rate }}) / p_vol

# Constraints
constraints = [{'type': 'eq', 'fun': lambda x: np.sum(x) - 1}]  # Weights sum to 1
bounds = tuple(({{ min_weight }}, {{ max_weight }}) for _ in range(n_assets))

# Initial guess (equal weights)
init_weights = np.array([1/n_assets] * n_assets)

# Optimize
{%- if optimization_target == "max_sharpe" %}
result = minimize(neg_sharpe_ratio, init_weights, method='SLSQP', bounds=bounds, constraints=constraints)
{%- elif optimization_target == "min_volatility" %}
result = minimize(portfolio_volatility, init_weights, method='SLSQP', bounds=bounds, constraints=constraints)
{%- else %}
result = minimize(lambda w: -portfolio_return(w), init_weights, method='SLSQP', bounds=bounds, constraints=constraints)
{%- endif %}

optimal_weights = dict(zip(returns_df.columns, result.x))
print('Optimal Portfolio Weights:')
for asset, weight in optimal_weights.items():
    print(f'  {asset}: {weight:.2%}')

opt_return = portfolio_return(result.x)
opt_vol = portfolio_volatility(result.x)
opt_sharpe = (opt_return - {{ risk_free_rate }}) / opt_vol
print(f'\\nExpected Annual Return: {opt_return:.2%}')
print(f'Expected Volatility: {opt_vol:.2%}')
print(f'Sharpe Ratio: {opt_sharpe:.2f}')
'''
        ))
    
    def _register_templates(self):
        """Register finance-specific workflow templates"""
        
        # Stock Price Prediction Template
        self.register_template(WorkflowTemplate(
            id="stock_prediction",
            name="Stock Price Prediction",
            description="Predict stock prices using technical indicators and machine learning",
            category="prediction",
            icon="bi-graph-up-arrow",
            color="#28a745",
            tags=["stocks", "prediction", "time-series", "trading"],
            difficulty="intermediate",
            nodes=[
                {"id": "start", "type": "start", "position": {"x": 50, "y": 150},
                 "data": {"message": "Stock Prediction Pipeline"}},
                {"id": "load", "type": "finance_stock_data_loader", "position": {"x": 200, "y": 150},
                 "data": {"source": "csv_file", "file_path": "data/stock_data.csv"}},
                {"id": "indicators", "type": "finance_technical_indicators", "position": {"x": 400, "y": 150},
                 "data": {"sma_periods": "20,50", "rsi_period": 14, "macd": True}},
                {"id": "returns", "type": "finance_returns_calculator", "position": {"x": 600, "y": 150},
                 "data": {"price_column": "Close", "daily_returns": True}},
                {"id": "prep", "type": "auto_data_prep", "position": {"x": 800, "y": 150},
                 "data": {}},
                {"id": "select", "type": "preprocess_select_features_target", "position": {"x": 1000, "y": 150},
                 "data": {"target_column": "Daily_Return"}},
                {"id": "split", "type": "preprocess_split", "position": {"x": 1200, "y": 150},
                 "data": {"test_size": 0.2}},
                {"id": "train", "type": "algo_random_forest_regressor", "position": {"x": 1400, "y": 150},
                 "data": {}},
                {"id": "evaluate", "type": "evaluate_metrics", "position": {"x": 1600, "y": 150},
                 "data": {}},
            ],
            edges=[
                {"source": "start", "target": "load"},
                {"source": "load", "target": "indicators"},
                {"source": "indicators", "target": "returns"},
                {"source": "returns", "target": "prep"},
                {"source": "prep", "target": "select"},
                {"source": "select", "target": "split", "sourcePort": "features", "targetPort": "features"},
                {"source": "select", "target": "split", "sourcePort": "target", "targetPort": "target"},
                {"source": "split", "target": "train"},
                {"source": "train", "target": "evaluate"},
            ]
        ))
        
        # Credit Risk Assessment Template
        self.register_template(WorkflowTemplate(
            id="credit_risk",
            name="Credit Risk Assessment",
            description="Build a credit risk model to predict loan defaults",
            category="classification",
            icon="bi-credit-card",
            color="#dc3545",
            tags=["credit", "risk", "classification", "banking"],
            difficulty="intermediate",
            nodes=[
                {"id": "start", "type": "start", "position": {"x": 50, "y": 150},
                 "data": {"message": "Credit Risk Pipeline"}},
                {"id": "load", "type": "data_load_csv", "position": {"x": 200, "y": 150},
                 "data": {"file_path": "data/credit_data.csv"}},
                {"id": "prep", "type": "auto_data_prep", "position": {"x": 400, "y": 150},
                 "data": {}},
                {"id": "select", "type": "preprocess_select_features_target", "position": {"x": 600, "y": 150},
                 "data": {"target_column": "default"}},
                {"id": "split", "type": "preprocess_split", "position": {"x": 800, "y": 150},
                 "data": {"test_size": 0.2, "stratify": "y"}},
                {"id": "scale", "type": "preprocess_scale", "position": {"x": 1000, "y": 150},
                 "data": {}},
                {"id": "train", "type": "algo_logistic_regression", "position": {"x": 1200, "y": 150},
                 "data": {}},
                {"id": "evaluate", "type": "evaluate_metrics", "position": {"x": 1400, "y": 150},
                 "data": {}},
            ],
            edges=[
                {"source": "start", "target": "load"},
                {"source": "load", "target": "prep"},
                {"source": "prep", "target": "select"},
                {"source": "select", "target": "split", "sourcePort": "features", "targetPort": "features"},
                {"source": "select", "target": "split", "sourcePort": "target", "targetPort": "target"},
                {"source": "split", "target": "scale"},
                {"source": "scale", "target": "train"},
                {"source": "train", "target": "evaluate"},
            ]
        ))
        
        # Fraud Detection Template
        self.register_template(WorkflowTemplate(
            id="fraud_detection",
            name="Fraud Detection",
            description="Detect fraudulent transactions using anomaly detection",
            category="anomaly_detection",
            icon="bi-shield-x",
            color="#6f42c1",
            tags=["fraud", "anomaly", "detection", "security"],
            difficulty="advanced",
            nodes=[
                {"id": "start", "type": "start", "position": {"x": 50, "y": 150},
                 "data": {"message": "Fraud Detection Pipeline"}},
                {"id": "load", "type": "data_load_csv", "position": {"x": 200, "y": 150},
                 "data": {"file_path": "data/transactions.csv"}},
                {"id": "prep", "type": "auto_data_prep", "position": {"x": 400, "y": 150},
                 "data": {}},
                {"id": "scale", "type": "preprocess_scale", "position": {"x": 600, "y": 150},
                 "data": {}},
                {"id": "detect", "type": "algo_isolation_forest", "position": {"x": 800, "y": 150},
                 "data": {"contamination": 0.01}},
            ],
            edges=[
                {"source": "start", "target": "load"},
                {"source": "load", "target": "prep"},
                {"source": "prep", "target": "scale"},
                {"source": "scale", "target": "detect"},
            ]
        ))
    
    def _register_datasets(self):
        """Register sample financial datasets"""
        
        self.register_dataset(SampleDataset(
            id="stock_prices",
            name="Sample Stock Prices",
            description="Historical stock prices for major tech companies (AAPL, GOOGL, MSFT)",
            filename="stock_prices.csv",
            format="csv",
            columns=[
                {"name": "Date", "type": "datetime"},
                {"name": "Open", "type": "float"},
                {"name": "High", "type": "float"},
                {"name": "Low", "type": "float"},
                {"name": "Close", "type": "float"},
                {"name": "Volume", "type": "int"},
                {"name": "Symbol", "type": "string"},
            ],
            rows=5000,
            size_kb=250,
            url="https://example.com/datasets/stock_prices.csv"
        ))
        
        self.register_dataset(SampleDataset(
            id="credit_data",
            name="Credit Risk Dataset",
            description="Loan application data with default indicators",
            filename="credit_data.csv",
            format="csv",
            columns=[
                {"name": "age", "type": "int"},
                {"name": "income", "type": "float"},
                {"name": "loan_amount", "type": "float"},
                {"name": "credit_score", "type": "int"},
                {"name": "employment_years", "type": "int"},
                {"name": "default", "type": "int"},
            ],
            rows=10000,
            size_kb=500,
        ))


def register(registry: ModuleRegistry):
    """Register the Finance module with the global registry"""
    registry.register(FinanceModule())

