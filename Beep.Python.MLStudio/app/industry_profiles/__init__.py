"""
Industry Profiles System
Provides domain-specific UI experiences for different industries
"""
from dataclasses import dataclass, field
from typing import List, Dict, Optional
import json
from pathlib import Path


@dataclass
class Scenario:
    """A user-friendly scenario within an industry"""
    id: str
    name: str
    description: str
    icon: str  # Bootstrap icon class
    difficulty: str  # beginner, intermediate, advanced
    estimated_time: str  # e.g., "5 mins", "15 mins"
    steps: List[Dict]
    default_template: Optional[str] = None
    sample_data: Optional[str] = None
    custom_icon: Optional[str] = None  # Path to custom icon image
    
    def to_dict(self):
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'icon': self.icon,
            'difficulty': self.difficulty,
            'estimated_time': self.estimated_time,
            'steps': self.steps,
            'default_template': self.default_template,
            'sample_data': self.sample_data,
            'custom_icon': self.custom_icon
        }


@dataclass  
class IndustryProfile:
    """Defines a complete industry-specific experience"""
    id: str
    name: str
    description: str
    icon: str  # Bootstrap icon class
    color: str
    gradient: str
    tagline: str
    
    # Scenarios for this industry
    scenarios: List[Scenario] = field(default_factory=list)
    
    # UI customization
    terminology: Dict[str, str] = field(default_factory=dict)  # ML term -> Industry term
    dashboard_widgets: List[str] = field(default_factory=list)
    hidden_features: List[str] = field(default_factory=list)
    
    # Node filtering
    recommended_nodes: List[str] = field(default_factory=list)
    hidden_nodes: List[str] = field(default_factory=list)
    
    # Custom branding
    custom_icon: Optional[str] = None  # Path to custom icon image
    icon_folder: Optional[str] = None  # Folder containing custom icons
    
    def to_dict(self):
        return {
            'id': self.id,
            'name': self.name,
            'description': self.description,
            'icon': self.icon,
            'color': self.color,
            'gradient': self.gradient,
            'tagline': self.tagline,
            'scenarios': [s.to_dict() for s in self.scenarios],
            'terminology': self.terminology,
            'dashboard_widgets': self.dashboard_widgets,
            'hidden_features': self.hidden_features,
            'recommended_nodes': self.recommended_nodes,
            'hidden_nodes': self.hidden_nodes,
            'custom_icon': self.custom_icon,
            'icon_folder': self.icon_folder
        }


class IndustryProfileManager:
    """Manages all industry profiles"""
    
    def __init__(self):
        self._profiles: Dict[str, IndustryProfile] = {}
        self._load_default_profiles()
    
    def _load_default_profiles(self):
        """Load built-in industry profiles"""
        # Advanced/General Mode
        self.register(IndustryProfile(
            id='advanced',
            name='Advanced Mode',
            description='Full access to all ML features and nodes. For ML practitioners and data scientists.',
            icon='bi-cpu',
            color='#8b949e',  # Theme: TextSecondaryColor
            gradient='linear-gradient(135deg, #8b949e 0%, #6e7681 100%)',  # Theme-based gray
            tagline='Complete ML Toolkit',
            scenarios=[],
            terminology={},
            dashboard_widgets=['all_projects', 'experiments', 'models'],
            hidden_features=[],
            recommended_nodes=[],  # All nodes
            hidden_nodes=[]
        ))
        
        # Finance & Economics Mode
        self.register(IndustryProfile(
            id='finance',
            name='Finance & Economics',
            description='Tailored for financial analysts, traders, and economists. Predict markets, assess risks, and detect fraud.',
            icon='bi-bank2',
            color='#00d4ff',  # Theme: InfoColor
            gradient='linear-gradient(135deg, #00d4ff 0%, #00b8e6 100%)',  # Theme-based cyan
            tagline='Smart Financial Analytics',
            scenarios=[
                Scenario(
                    id='stock_prediction',
                    name='Stock Price Prediction',
                    description='Predict future stock prices using historical data and technical indicators',
                    icon='bi-graph-up-arrow',
                    difficulty='beginner',
                    estimated_time='10 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Stock Data', 'description': 'Upload CSV with Date, Open, High, Low, Close, Volume'},
                        {'step': 2, 'title': 'Select Prediction Target', 'description': 'Choose what to predict (Close price, Returns, etc.)'},
                        {'step': 3, 'title': 'Configure Model', 'description': 'Choose prediction horizon and model type'},
                        {'step': 4, 'title': 'Train & Evaluate', 'description': 'Train the model and view predictions'}
                    ],
                    default_template='time_series_regression',
                    sample_data='stock_prices.csv'
                ),
                Scenario(
                    id='credit_risk',
                    name='Credit Risk Scoring',
                    description='Assess loan default risk using customer financial data',
                    icon='bi-shield-check',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Customer Data', 'description': 'Upload CSV with customer financial information'},
                        {'step': 2, 'title': 'Define Risk Criteria', 'description': 'Specify what constitutes high/low risk'},
                        {'step': 3, 'title': 'Build Risk Model', 'description': 'The system will automatically select the best model'},
                        {'step': 4, 'title': 'Generate Risk Scores', 'description': 'Get risk scores for each customer'}
                    ],
                    default_template='binary_classification',
                    sample_data='loan_data.csv'
                ),
                Scenario(
                    id='fraud_detection',
                    name='Fraud Detection',
                    description='Identify fraudulent transactions using anomaly detection',
                    icon='bi-exclamation-triangle',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Transaction Data', 'description': 'Upload CSV with transaction records'},
                        {'step': 2, 'title': 'Configure Detection', 'description': 'Set sensitivity and detection parameters'},
                        {'step': 3, 'title': 'Train Detector', 'description': 'Build anomaly detection model'},
                        {'step': 4, 'title': 'Review Alerts', 'description': 'View flagged suspicious transactions'}
                    ],
                    default_template='anomaly_detection',
                    sample_data='transactions.csv'
                ),
                Scenario(
                    id='portfolio_optimization',
                    name='Portfolio Optimization',
                    description='Optimize investment portfolios for maximum returns with controlled risk',
                    icon='bi-pie-chart',
                    difficulty='intermediate',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Asset Data', 'description': 'Upload historical returns for your assets'},
                        {'step': 2, 'title': 'Set Constraints', 'description': 'Define risk tolerance and investment constraints'},
                        {'step': 3, 'title': 'Optimize', 'description': 'Run optimization algorithm'},
                        {'step': 4, 'title': 'View Allocation', 'description': 'See optimal portfolio weights'}
                    ],
                    default_template='clustering',
                    sample_data='portfolio_returns.csv'
                ),
                Scenario(
                    id='customer_churn',
                    name='Customer Churn Prediction',
                    description='Predict which customers are likely to leave',
                    icon='bi-person-dash',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Customer Data', 'description': 'Upload CSV with customer behavior data'},
                        {'step': 2, 'title': 'Select Features', 'description': 'Choose relevant customer attributes'},
                        {'step': 3, 'title': 'Train Model', 'description': 'Build churn prediction model'},
                        {'step': 4, 'title': 'Identify At-Risk', 'description': 'View customers likely to churn'}
                    ],
                    default_template='binary_classification',
                    sample_data='customer_data.csv'
                ),
                Scenario(
                    id='market_segmentation',
                    name='Market Segmentation',
                    description='Segment customers into distinct groups for targeted marketing',
                    icon='bi-people',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Customer Data', 'description': 'Upload CSV with customer demographics and behavior'},
                        {'step': 2, 'title': 'Choose Segments', 'description': 'Specify number of segments to create'},
                        {'step': 3, 'title': 'Run Clustering', 'description': 'Group customers automatically'},
                        {'step': 4, 'title': 'Analyze Segments', 'description': 'View segment characteristics'}
                    ],
                    default_template='clustering',
                    sample_data='customer_segments.csv'
                ),
                Scenario(
                    id='loan_default_prediction',
                    name='Loan Default Prediction',
                    description='Predict which loans are likely to default before they happen',
                    icon='bi-exclamation-circle',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Loan Portfolio', 'description': 'Upload CSV with loan details and borrower information'},
                        {'step': 2, 'title': 'Define Default Criteria', 'description': 'Set what constitutes a default (30/60/90 days past due)'},
                        {'step': 3, 'title': 'Train Default Model', 'description': 'Build model to predict loan defaults'},
                        {'step': 4, 'title': 'Assess Portfolio Risk', 'description': 'View default probabilities for each loan'}
                    ],
                    default_template='binary_classification',
                    sample_data='loan_portfolio.csv'
                ),
                Scenario(
                    id='market_volatility',
                    name='Market Volatility Prediction',
                    description='Predict market volatility to manage risk and optimize trading strategies',
                    icon='bi-graph-up-arrow',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Market Data', 'description': 'Upload historical price data and market indicators'},
                        {'step': 2, 'title': 'Calculate Volatility', 'description': 'Compute historical volatility metrics'},
                        {'step': 3, 'title': 'Train Volatility Model', 'description': 'Build model to predict future volatility'},
                        {'step': 4, 'title': 'View Forecasts', 'description': 'See predicted volatility for risk management'}
                    ],
                    default_template='regression',
                    sample_data='market_volatility.csv'
                ),
                Scenario(
                    id='currency_prediction',
                    name='Currency Exchange Rate Prediction',
                    description='Predict foreign exchange rates for trading and hedging',
                    icon='bi-currency-exchange',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload FX Data', 'description': 'Upload historical exchange rates and economic indicators'},
                        {'step': 2, 'title': 'Select Currency Pair', 'description': 'Choose which currency pair to predict'},
                        {'step': 3, 'title': 'Train FX Model', 'description': 'Build time series model for exchange rates'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See forecasted exchange rates'}
                    ],
                    default_template='time_series_regression',
                    sample_data='currency_rates.csv'
                ),
                Scenario(
                    id='insurance_claims',
                    name='Insurance Claim Prediction',
                    description='Predict claim likelihood and amounts for underwriting and pricing',
                    icon='bi-shield-check',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Policy Data', 'description': 'Upload policyholder information and policy details'},
                        {'step': 2, 'title': 'Define Claim Types', 'description': 'Specify claim categories (auto, health, property, etc.)'},
                        {'step': 3, 'title': 'Train Claim Model', 'description': 'Build model to predict claim probability and amount'},
                        {'step': 4, 'title': 'Assess Risk', 'description': 'View predicted claim risks for pricing'}
                    ],
                    default_template='regression',
                    sample_data='insurance_claims.csv'
                ),
                Scenario(
                    id='revenue_forecasting',
                    name='Revenue Forecasting',
                    description='Forecast company revenue using historical sales and market data',
                    icon='bi-cash-stack',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Sales Data', 'description': 'Upload historical revenue and sales data'},
                        {'step': 2, 'title': 'Select Forecast Horizon', 'description': 'Choose prediction period (monthly, quarterly, yearly)'},
                        {'step': 3, 'title': 'Train Forecast Model', 'description': 'Build time series forecasting model'},
                        {'step': 4, 'title': 'View Forecast', 'description': 'See predicted revenue with confidence intervals'}
                    ],
                    default_template='time_series_regression',
                    sample_data='revenue_data.csv'
                ),
                Scenario(
                    id='customer_lifetime_value',
                    name='Customer Lifetime Value',
                    description='Predict total value a customer will bring over their relationship',
                    icon='bi-person-check',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Customer Data', 'description': 'Upload customer transaction and behavior history'},
                        {'step': 2, 'title': 'Calculate Historical CLV', 'description': 'Compute past customer lifetime values'},
                        {'step': 3, 'title': 'Train CLV Model', 'description': 'Build model to predict future customer value'},
                        {'step': 4, 'title': 'View CLV Scores', 'description': 'See predicted lifetime value for each customer'}
                    ],
                    default_template='regression',
                    sample_data='customer_clv.csv'
                ),
                Scenario(
                    id='price_elasticity',
                    name='Price Elasticity Analysis',
                    description='Analyze how price changes affect demand to optimize pricing',
                    icon='bi-tag',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Sales & Price Data', 'description': 'Upload historical sales volumes and prices'},
                        {'step': 2, 'title': 'Define Products', 'description': 'Select products or product categories to analyze'},
                        {'step': 3, 'title': 'Calculate Elasticity', 'description': 'Build model to estimate price elasticity'},
                        {'step': 4, 'title': 'Optimize Pricing', 'description': 'View recommended prices for maximum revenue'}
                    ],
                    default_template='regression',
                    sample_data='price_elasticity.csv'
                ),
                Scenario(
                    id='bond_yield_prediction',
                    name='Bond Yield Prediction',
                    description='Predict bond yields for fixed income portfolio management',
                    icon='bi-graph-up',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Bond Data', 'description': 'Upload bond characteristics and market data'},
                        {'step': 2, 'title': 'Select Bond Features', 'description': 'Choose relevant factors (maturity, credit rating, etc.)'},
                        {'step': 3, 'title': 'Train Yield Model', 'description': 'Build model to predict bond yields'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See predicted yields for portfolio optimization'}
                    ],
                    default_template='regression',
                    sample_data='bond_yields.csv'
                ),
                Scenario(
                    id='trading_signal',
                    name='Trading Signal Generation',
                    description='Generate buy/sell signals using technical and fundamental analysis',
                    icon='bi-arrow-left-right',
                    difficulty='advanced',
                    estimated_time='30 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Market Data', 'description': 'Upload price, volume, and technical indicators'},
                        {'step': 2, 'title': 'Define Signal Rules', 'description': 'Set criteria for buy/sell signals'},
                        {'step': 3, 'title': 'Train Signal Model', 'description': 'Build model to generate trading signals'},
                        {'step': 4, 'title': 'View Signals', 'description': 'See recommended buy/sell signals with confidence'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='trading_signals.csv'
                )
            ],
            terminology={
                'features': 'Financial Indicators',
                'target': 'Prediction Variable',
                'training': 'Model Calibration',
                'prediction': 'Forecast',
                'classification': 'Risk Category',
                'regression': 'Value Prediction',
                'clustering': 'Segmentation',
                'model': 'Analytics Model'
            },
            dashboard_widgets=['portfolio_summary', 'risk_metrics', 'recent_predictions'],
            hidden_features=['tensorflow_nodes', 'pytorch_nodes'],
            recommended_nodes=[
                'data_load_csv', 'preprocess_select_features_target', 'preprocess_train_test_split',
                'sklearn_standard_scaler', 'algorithm_random_forest_classifier', 
                'algorithm_logistic_regression', 'algorithm_isolation_forest',
                'algorithm_kmeans', 'evaluate_metrics', 'output_save_model'
            ],
            hidden_nodes=['tensorflow_sequential', 'pytorch_model']
        ))
        
        # Petroleum Engineering Mode
        self.register(IndustryProfile(
            id='petroleum',
            name='Petroleum Engineering',
            description='Designed for reservoir engineers and production analysts. Analyze well logs, forecast production, and optimize operations.',
            icon='bi-droplet-half',
            color='#00ff88',  # Theme: PrimaryColor
            gradient='linear-gradient(135deg, #00ff88 0%, #00cc6f 100%)',  # Theme-based green
            tagline='Intelligent Reservoir Analytics',
            scenarios=[
                Scenario(
                    id='well_log_analysis',
                    name='Well Log Interpretation',
                    description='Automatically interpret well logs to identify lithology and reservoir quality',
                    icon='bi-bar-chart-steps',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Well Log Data', 'description': 'Upload LAS file or CSV with log curves (GR, RHOB, NPHI, etc.)'},
                        {'step': 2, 'title': 'Select Log Curves', 'description': 'Choose which curves to analyze'},
                        {'step': 3, 'title': 'Train Lithology Model', 'description': 'Build facies classification model'},
                        {'step': 4, 'title': 'View Interpretation', 'description': 'See predicted lithology track'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='well_logs.csv'
                ),
                Scenario(
                    id='production_forecast',
                    name='Production Forecasting',
                    description='Predict future oil/gas production using historical data',
                    icon='bi-graph-up',
                    difficulty='beginner',
                    estimated_time='10 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Production Data', 'description': 'Upload CSV with date and production rates'},
                        {'step': 2, 'title': 'Configure Forecast', 'description': 'Set forecast horizon and parameters'},
                        {'step': 3, 'title': 'Generate Forecast', 'description': 'Build and run forecasting model'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See production forecast chart'}
                    ],
                    default_template='time_series_regression',
                    sample_data='production_history.csv'
                ),
                Scenario(
                    id='decline_curve',
                    name='Decline Curve Analysis',
                    description='Fit decline curves and estimate ultimate recovery (EUR)',
                    icon='bi-arrow-down-right',
                    difficulty='beginner',
                    estimated_time='10 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Production Data', 'description': 'Upload production history for the well'},
                        {'step': 2, 'title': 'Select Decline Model', 'description': 'Choose exponential, hyperbolic, or harmonic decline'},
                        {'step': 3, 'title': 'Fit Curve', 'description': 'Automatically fit decline parameters'},
                        {'step': 4, 'title': 'Calculate EUR', 'description': 'View estimated ultimate recovery'}
                    ],
                    default_template='regression',
                    sample_data='production_decline.csv'
                ),
                Scenario(
                    id='sweet_spot_detection',
                    name='Sweet Spot Detection',
                    description='Identify optimal drilling locations using geological and production data',
                    icon='bi-geo-alt',
                    difficulty='intermediate',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Geological Data', 'description': 'Upload well and seismic attribute data'},
                        {'step': 2, 'title': 'Define Success Criteria', 'description': 'Specify what makes a good well'},
                        {'step': 3, 'title': 'Train Predictor', 'description': 'Build sweet spot prediction model'},
                        {'step': 4, 'title': 'Generate Map', 'description': 'View probability map of sweet spots'}
                    ],
                    default_template='binary_classification',
                    sample_data='well_attributes.csv'
                ),
                Scenario(
                    id='reservoir_clustering',
                    name='Reservoir Rock Typing',
                    description='Group reservoir rocks into distinct flow units',
                    icon='bi-layers',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Core Data', 'description': 'Upload porosity, permeability, and other core measurements'},
                        {'step': 2, 'title': 'Select Properties', 'description': 'Choose properties for clustering'},
                        {'step': 3, 'title': 'Run Clustering', 'description': 'Automatically identify rock types'},
                        {'step': 4, 'title': 'Analyze Types', 'description': 'View rock type characteristics'}
                    ],
                    default_template='clustering',
                    sample_data='core_data.csv'
                ),
                Scenario(
                    id='equipment_failure',
                    name='Equipment Failure Prediction',
                    description='Predict equipment failures before they happen using sensor data',
                    icon='bi-exclamation-octagon',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Sensor Data', 'description': 'Upload time-series sensor readings'},
                        {'step': 2, 'title': 'Define Failure Events', 'description': 'Identify known failure events'},
                        {'step': 3, 'title': 'Train Predictor', 'description': 'Build failure prediction model'},
                        {'step': 4, 'title': 'Set Up Alerts', 'description': 'Configure early warning thresholds'}
                    ],
                    default_template='binary_classification',
                    sample_data='sensor_readings.csv'
                ),
                Scenario(
                    id='pipeline_integrity',
                    name='Pipeline Integrity Monitoring',
                    description='Monitor pipeline health and predict potential leaks or failures using pressure, flow, and inspection data',
                    icon='bi-pipe',
                    difficulty='intermediate',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Pipeline Data', 'description': 'Upload pressure, flow rate, temperature, and inspection data'},
                        {'step': 2, 'title': 'Define Integrity Metrics', 'description': 'Set thresholds for normal vs. abnormal conditions'},
                        {'step': 3, 'title': 'Train Monitoring Model', 'description': 'Build anomaly detection model for pipeline health'},
                        {'step': 4, 'title': 'Set Up Monitoring Dashboard', 'description': 'Configure real-time alerts and health scores'}
                    ],
                    default_template='anomaly_detection',
                    sample_data='pipeline_data.csv'
                ),
                Scenario(
                    id='facility_maintenance',
                    name='Facility Maintenance Optimization',
                    description='Predict maintenance needs and optimize scheduling for production facilities',
                    icon='bi-building',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Facility Data', 'description': 'Upload equipment history, maintenance logs, and performance data'},
                        {'step': 2, 'title': 'Define Maintenance Events', 'description': 'Identify past maintenance activities and outcomes'},
                        {'step': 3, 'title': 'Train Maintenance Predictor', 'description': 'Build model to predict optimal maintenance timing'},
                        {'step': 4, 'title': 'Generate Schedule', 'description': 'View recommended maintenance schedule and priorities'}
                    ],
                    default_template='regression',
                    sample_data='facility_maintenance.csv'
                ),
                Scenario(
                    id='reservoir_properties',
                    name='Reservoir Property Prediction from Logs',
                    description='Predict porosity, permeability, and saturation from well log data',
                    icon='bi-calculator',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Well Logs', 'description': 'Upload LAS files or CSV with log curves (GR, RHOB, NPHI, DT, etc.)'},
                        {'step': 2, 'title': 'Upload Core Data', 'description': 'Upload core measurements for training (porosity, permeability)'},
                        {'step': 3, 'title': 'Train Property Predictor', 'description': 'Build regression model to predict reservoir properties'},
                        {'step': 4, 'title': 'Generate Property Logs', 'description': 'View predicted porosity, permeability, and saturation curves'}
                    ],
                    default_template='regression',
                    sample_data='logs_and_core.csv'
                ),
                Scenario(
                    id='water_cut_prediction',
                    name='Water Cut Prediction',
                    description='Predict water cut percentage in production streams to optimize separation',
                    icon='bi-droplet',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Production Data', 'description': 'Upload well production data with water cut measurements'},
                        {'step': 2, 'title': 'Select Features', 'description': 'Choose relevant well and reservoir properties'},
                        {'step': 3, 'title': 'Train Water Cut Model', 'description': 'Build model to predict water cut percentage'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See predicted water cut for planning and optimization'}
                    ],
                    default_template='regression',
                    sample_data='production_water_cut.csv'
                ),
                Scenario(
                    id='gas_lift_optimization',
                    name='Gas Lift Optimization',
                    description='Optimize gas injection rates to maximize oil production',
                    icon='bi-wind',
                    difficulty='advanced',
                    estimated_time='30 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Well Data', 'description': 'Upload production data with gas injection rates and oil production'},
                        {'step': 2, 'title': 'Define Optimization Goals', 'description': 'Set constraints (max gas, min production, etc.)'},
                        {'step': 3, 'title': 'Train Production Model', 'description': 'Build model relating gas injection to oil production'},
                        {'step': 4, 'title': 'Optimize Injection Rates', 'description': 'Get recommended gas injection rates for each well'}
                    ],
                    default_template='regression',
                    sample_data='gas_lift_data.csv'
                ),
                Scenario(
                    id='drilling_optimization',
                    name='Drilling Performance Optimization',
                    description='Optimize drilling parameters (ROP, WOB, RPM) to reduce drilling time and costs',
                    icon='bi-speedometer2',
                    difficulty='advanced',
                    estimated_time='30 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Drilling Data', 'description': 'Upload drilling logs with ROP, WOB, RPM, and formation data'},
                        {'step': 2, 'title': 'Define Performance Metrics', 'description': 'Set targets for drilling rate and cost'},
                        {'step': 3, 'title': 'Train Performance Model', 'description': 'Build model to predict drilling rate from parameters'},
                        {'step': 4, 'title': 'Optimize Parameters', 'description': 'Get recommended drilling parameters for each formation'}
                    ],
                    default_template='regression',
                    sample_data='drilling_logs.csv'
                ),
                Scenario(
                    id='corrosion_prediction',
                    name='Corrosion Rate Prediction',
                    description='Predict corrosion rates in pipelines and equipment to plan maintenance',
                    icon='bi-shield-exclamation',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Corrosion Data', 'description': 'Upload inspection data with corrosion measurements and environmental conditions'},
                        {'step': 2, 'title': 'Select Factors', 'description': 'Choose relevant factors (temperature, pressure, fluid composition, etc.)'},
                        {'step': 3, 'title': 'Train Corrosion Model', 'description': 'Build model to predict corrosion rates'},
                        {'step': 4, 'title': 'Plan Maintenance', 'description': 'View predicted corrosion rates and maintenance schedules'}
                    ],
                    default_template='regression',
                    sample_data='corrosion_data.csv'
                ),
                Scenario(
                    id='flow_assurance',
                    name='Flow Assurance Analysis',
                    description='Predict hydrate, wax, and asphaltene formation risks in pipelines',
                    icon='bi-snow',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Flow Data', 'description': 'Upload pressure, temperature, and fluid composition data'},
                        {'step': 2, 'title': 'Define Risk Criteria', 'description': 'Set thresholds for hydrate/wax/asphaltene formation'},
                        {'step': 3, 'title': 'Train Risk Model', 'description': 'Build model to predict flow assurance issues'},
                        {'step': 4, 'title': 'View Risk Assessment', 'description': 'See predicted risks and recommended mitigation strategies'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='flow_assurance.csv'
                ),
                Scenario(
                    id='pvt_analysis',
                    name='PVT Property Prediction',
                    description='Predict pressure-volume-temperature properties for reservoir fluids',
                    icon='bi-thermometer-half',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload PVT Data', 'description': 'Upload laboratory PVT measurements and fluid compositions'},
                        {'step': 2, 'title': 'Select Properties', 'description': 'Choose properties to predict (bubble point, viscosity, etc.)'},
                        {'step': 3, 'title': 'Train PVT Model', 'description': 'Build model to predict PVT properties'},
                        {'step': 4, 'title': 'Generate PVT Tables', 'description': 'View predicted properties for reservoir simulation'}
                    ],
                    default_template='regression',
                    sample_data='pvt_data.csv'
                ),
                Scenario(
                    id='artificial_lift_selection',
                    name='Artificial Lift Selection',
                    description='Recommend optimal artificial lift method (ESP, gas lift, rod pump) for wells',
                    icon='bi-arrow-up-circle',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Well Data', 'description': 'Upload well characteristics, production history, and reservoir properties'},
                        {'step': 2, 'title': 'Define Selection Criteria', 'description': 'Set goals (cost, production, reliability)'},
                        {'step': 3, 'title': 'Train Selection Model', 'description': 'Build model to recommend lift method'},
                        {'step': 4, 'title': 'View Recommendations', 'description': 'See recommended lift method for each well'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='well_lift_data.csv'
                ),
                Scenario(
                    id='production_allocation',
                    name='Production Allocation',
                    description='Allocate production from commingled wells to individual zones',
                    icon='bi-diagram-3',
                    difficulty='advanced',
                    estimated_time='30 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Production Data', 'description': 'Upload commingled production and well test data'},
                        {'step': 2, 'title': 'Define Zones', 'description': 'Specify contributing zones and their properties'},
                        {'step': 3, 'title': 'Train Allocation Model', 'description': 'Build model to allocate production to zones'},
                        {'step': 4, 'title': 'View Allocation', 'description': 'See allocated production for each zone over time'}
                    ],
                    default_template='regression',
                    sample_data='commingled_production.csv'
                )
            ],
            terminology={
                'features': 'Well Properties',
                'target': 'Target Variable',
                'training': 'Model Training',
                'prediction': 'Prediction',
                'classification': 'Category Prediction',
                'regression': 'Value Estimation',
                'clustering': 'Rock Typing',
                'model': 'Prediction Model'
            },
            dashboard_widgets=['well_summary', 'production_chart', 'recent_analyses'],
            hidden_features=['finance_nodes'],
            recommended_nodes=[
                'data_load_csv', 'preprocess_select_features_target', 'preprocess_train_test_split',
                'sklearn_standard_scaler', 'sklearn_robust_scaler',
                'algorithm_random_forest_classifier', 'algorithm_random_forest_regressor',
                'algorithm_kmeans', 'algorithm_gaussian_mixture',
                'evaluate_metrics', 'output_save_model'
            ],
            hidden_nodes=['finance_nodes']
        ))
        
        # Healthcare Mode
        self.register(IndustryProfile(
            id='healthcare',
            name='Healthcare & Medical',
            description='For healthcare professionals and researchers. Predict patient outcomes, diagnose conditions, and analyze medical data.',
            icon='bi-heart-pulse',
            color='#bd93f9',  # Theme: TertiaryColor
            gradient='linear-gradient(135deg, #bd93f9 0%, #9d6ff9 100%)',  # Theme-based purple
            tagline='Intelligent Medical Analytics',
            scenarios=[
                Scenario(
                    id='disease_prediction',
                    name='Disease Risk Prediction',
                    description='Predict patient risk for specific diseases based on health data',
                    icon='bi-clipboard2-pulse',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Patient Data', 'description': 'Upload CSV with patient health records'},
                        {'step': 2, 'title': 'Select Risk Factors', 'description': 'Choose relevant health indicators'},
                        {'step': 3, 'title': 'Train Risk Model', 'description': 'Build disease prediction model'},
                        {'step': 4, 'title': 'View Risk Scores', 'description': 'See patient risk assessments'}
                    ],
                    default_template='binary_classification',
                    sample_data='patient_data.csv'
                ),
                Scenario(
                    id='readmission_prediction',
                    name='Hospital Readmission',
                    description='Predict likelihood of patient hospital readmission',
                    icon='bi-hospital',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Admission Data', 'description': 'Upload patient admission history'},
                        {'step': 2, 'title': 'Define Readmission Window', 'description': 'Set 30-day or 90-day readmission criteria'},
                        {'step': 3, 'title': 'Build Model', 'description': 'Train readmission prediction model'},
                        {'step': 4, 'title': 'Identify High Risk', 'description': 'View patients at risk of readmission'}
                    ],
                    default_template='binary_classification',
                    sample_data='admissions.csv'
                ),
                Scenario(
                    id='patient_segmentation',
                    name='Patient Segmentation',
                    description='Group patients for personalized treatment plans',
                    icon='bi-people',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Patient Data', 'description': 'Upload patient demographics and health data'},
                        {'step': 2, 'title': 'Select Criteria', 'description': 'Choose segmentation criteria'},
                        {'step': 3, 'title': 'Run Clustering', 'description': 'Group patients automatically'},
                        {'step': 4, 'title': 'Analyze Groups', 'description': 'View patient segment profiles'}
                    ],
                    default_template='clustering',
                    sample_data='patient_profiles.csv'
                ),
                Scenario(
                    id='treatment_outcome',
                    name='Treatment Outcome Prediction',
                    description='Predict patient response to specific treatments or medications',
                    icon='bi-heart-pulse-fill',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Treatment Data', 'description': 'Upload patient data with treatment history and outcomes'},
                        {'step': 2, 'title': 'Define Outcome Metrics', 'description': 'Specify success criteria (recovery, improvement, etc.)'},
                        {'step': 3, 'title': 'Train Outcome Model', 'description': 'Build model to predict treatment effectiveness'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See predicted outcomes for treatment planning'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='treatment_outcomes.csv'
                ),
                Scenario(
                    id='length_of_stay',
                    name='Hospital Length of Stay Prediction',
                    description='Predict how long patients will stay in hospital for resource planning',
                    icon='bi-calendar-range',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Admission Data', 'description': 'Upload patient admission records and diagnoses'},
                        {'step': 2, 'title': 'Select Features', 'description': 'Choose relevant factors (age, diagnosis, procedures, etc.)'},
                        {'step': 3, 'title': 'Train LOS Model', 'description': 'Build model to predict length of stay'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See predicted stay duration for capacity planning'}
                    ],
                    default_template='regression',
                    sample_data='length_of_stay.csv'
                ),
                Scenario(
                    id='medication_adherence',
                    name='Medication Adherence Prediction',
                    description='Predict which patients are likely to miss medications',
                    icon='bi-capsule',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Medication Data', 'description': 'Upload prescription and adherence history'},
                        {'step': 2, 'title': 'Define Adherence Criteria', 'description': 'Set thresholds for good vs. poor adherence'},
                        {'step': 3, 'title': 'Train Adherence Model', 'description': 'Build model to predict medication compliance'},
                        {'step': 4, 'title': 'Identify At-Risk Patients', 'description': 'View patients likely to miss medications'}
                    ],
                    default_template='binary_classification',
                    sample_data='medication_adherence.csv'
                ),
                Scenario(
                    id='disease_progression',
                    name='Disease Progression Prediction',
                    description='Predict how diseases will progress over time',
                    icon='bi-activity',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Patient History', 'description': 'Upload longitudinal patient data and test results'},
                        {'step': 2, 'title': 'Define Progression Stages', 'description': 'Specify disease stages or severity levels'},
                        {'step': 3, 'title': 'Train Progression Model', 'description': 'Build model to predict disease progression'},
                        {'step': 4, 'title': 'View Forecasts', 'description': 'See predicted progression timeline'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='disease_progression.csv'
                ),
                Scenario(
                    id='lab_result_interpretation',
                    name='Lab Result Interpretation',
                    description='Classify lab results as normal, abnormal, or critical',
                    icon='bi-clipboard-data',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Lab Data', 'description': 'Upload lab test results and reference ranges'},
                        {'step': 2, 'title': 'Define Categories', 'description': 'Set normal, abnormal, and critical thresholds'},
                        {'step': 3, 'title': 'Train Classifier', 'description': 'Build model to classify lab results'},
                        {'step': 4, 'title': 'View Classifications', 'description': 'See categorized lab results'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='lab_results.csv'
                ),
                Scenario(
                    id='drug_effectiveness',
                    name='Drug Effectiveness Prediction',
                    description='Predict how effective a drug will be for specific patients',
                    icon='bi-prescription2',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Drug Trial Data', 'description': 'Upload patient data with drug responses'},
                        {'step': 2, 'title': 'Define Effectiveness Metrics', 'description': 'Specify success criteria (symptom reduction, etc.)'},
                        {'step': 3, 'title': 'Train Effectiveness Model', 'description': 'Build model to predict drug response'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See predicted drug effectiveness for patients'}
                    ],
                    default_template='regression',
                    sample_data='drug_effectiveness.csv'
                ),
                Scenario(
                    id='patient_mortality',
                    name='Patient Mortality Risk',
                    description='Assess patient mortality risk for critical care planning',
                    icon='bi-heartbreak',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload ICU Data', 'description': 'Upload critical care patient data and vitals'},
                        {'step': 2, 'title': 'Define Risk Levels', 'description': 'Set low, medium, high risk categories'},
                        {'step': 3, 'title': 'Train Risk Model', 'description': 'Build model to predict mortality risk'},
                        {'step': 4, 'title': 'View Risk Scores', 'description': 'See mortality risk assessments for care planning'}
                    ],
                    default_template='binary_classification',
                    sample_data='mortality_risk.csv'
                ),
                Scenario(
                    id='surgical_outcome',
                    name='Surgical Outcome Prediction',
                    description='Predict surgical success rates and complications',
                    icon='bi-hospital',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Surgical Data', 'description': 'Upload patient data and surgical procedure details'},
                        {'step': 2, 'title': 'Define Outcomes', 'description': 'Specify success, complication, or failure criteria'},
                        {'step': 3, 'title': 'Train Outcome Model', 'description': 'Build model to predict surgical results'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See predicted outcomes for surgical planning'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='surgical_outcomes.csv'
                ),
                Scenario(
                    id='epidemic_prediction',
                    name='Epidemic Outbreak Prediction',
                    description='Predict disease outbreaks using epidemiological data',
                    icon='bi-virus',
                    difficulty='advanced',
                    estimated_time='30 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Epidemiological Data', 'description': 'Upload case counts, demographics, and location data'},
                        {'step': 2, 'title': 'Select Indicators', 'description': 'Choose relevant factors (population density, travel, etc.)'},
                        {'step': 3, 'title': 'Train Outbreak Model', 'description': 'Build model to predict outbreak likelihood'},
                        {'step': 4, 'title': 'View Risk Map', 'description': 'See predicted outbreak risks by location'}
                    ],
                    default_template='regression',
                    sample_data='epidemic_data.csv'
                )
            ],
            terminology={
                'features': 'Health Indicators',
                'target': 'Outcome Variable',
                'training': 'Model Training',
                'prediction': 'Prediction',
                'classification': 'Diagnosis/Risk',
                'regression': 'Value Estimation',
                'clustering': 'Patient Grouping',
                'model': 'Clinical Model'
            },
            dashboard_widgets=['patient_summary', 'outcome_metrics', 'recent_analyses'],
            hidden_features=[],
            recommended_nodes=[
                'data_load_csv', 'preprocess_select_features_target', 'preprocess_train_test_split',
                'sklearn_standard_scaler', 'algorithm_random_forest_classifier',
                'algorithm_logistic_regression', 'algorithm_kmeans',
                'evaluate_metrics', 'output_save_model'
            ],
            hidden_nodes=[]
        ))
        
        # Manufacturing Mode
        self.register(IndustryProfile(
            id='manufacturing',
            name='Manufacturing & Industry',
            description='For production managers and quality engineers. Predict defects, optimize processes, and reduce downtime.',
            icon='bi-gear-wide-connected',
            color='#00ff88',  # Theme: PrimaryColor
            gradient='linear-gradient(135deg, #00ff88 0%, #00d4ff 100%)',  # Theme-based green to cyan
            tagline='Smart Factory Analytics',
            scenarios=[
                Scenario(
                    id='quality_prediction',
                    name='Quality Prediction',
                    description='Predict product quality before final inspection',
                    icon='bi-patch-check',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Process Data', 'description': 'Upload manufacturing process parameters'},
                        {'step': 2, 'title': 'Define Quality Criteria', 'description': 'Specify pass/fail or quality grades'},
                        {'step': 3, 'title': 'Train Predictor', 'description': 'Build quality prediction model'},
                        {'step': 4, 'title': 'Monitor Quality', 'description': 'View real-time quality predictions'}
                    ],
                    default_template='binary_classification',
                    sample_data='process_data.csv'
                ),
                Scenario(
                    id='predictive_maintenance',
                    name='Predictive Maintenance',
                    description='Predict machine failures and schedule maintenance proactively',
                    icon='bi-tools',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Sensor Data', 'description': 'Upload machine sensor readings'},
                        {'step': 2, 'title': 'Define Failure Events', 'description': 'Mark known failure occurrences'},
                        {'step': 3, 'title': 'Build Model', 'description': 'Train failure prediction model'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See machines likely to fail soon'}
                    ],
                    default_template='binary_classification',
                    sample_data='sensor_data.csv'
                ),
                Scenario(
                    id='process_optimization',
                    name='Process Optimization',
                    description='Find optimal process parameters for best output',
                    icon='bi-sliders',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Historical Data', 'description': 'Upload process parameters and outcomes'},
                        {'step': 2, 'title': 'Define Objective', 'description': 'Specify what to optimize'},
                        {'step': 3, 'title': 'Run Optimization', 'description': 'Find optimal parameter settings'},
                        {'step': 4, 'title': 'Apply Settings', 'description': 'View recommended parameters'}
                    ],
                    default_template='regression',
                    sample_data='process_history.csv'
                ),
                Scenario(
                    id='defect_classification',
                    name='Defect Classification',
                    description='Classify product defects by type and severity',
                    icon='bi-x-circle',
                    difficulty='beginner',
                    estimated_time='15 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Defect Data', 'description': 'Upload defect records with images or measurements'},
                        {'step': 2, 'title': 'Define Defect Types', 'description': 'Specify defect categories (crack, scratch, dent, etc.)'},
                        {'step': 3, 'title': 'Train Classifier', 'description': 'Build model to classify defects'},
                        {'step': 4, 'title': 'View Classifications', 'description': 'See defect types and root cause analysis'}
                    ],
                    default_template='multiclass_classification',
                    sample_data='defect_data.csv'
                ),
                Scenario(
                    id='supply_chain_optimization',
                    name='Supply Chain Optimization',
                    description='Optimize inventory levels and supplier selection',
                    icon='bi-box-seam',
                    difficulty='advanced',
                    estimated_time='30 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Supply Chain Data', 'description': 'Upload inventory, demand, and supplier data'},
                        {'step': 2, 'title': 'Define Objectives', 'description': 'Set goals (minimize cost, maximize service level)'},
                        {'step': 3, 'title': 'Train Optimization Model', 'description': 'Build model to optimize supply chain'},
                        {'step': 4, 'title': 'View Recommendations', 'description': 'See optimal inventory levels and supplier choices'}
                    ],
                    default_template='regression',
                    sample_data='supply_chain.csv'
                ),
                Scenario(
                    id='energy_consumption',
                    name='Energy Consumption Prediction',
                    description='Predict energy usage to optimize costs and reduce waste',
                    icon='bi-lightning-charge',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Energy Data', 'description': 'Upload historical energy consumption and production data'},
                        {'step': 2, 'title': 'Select Factors', 'description': 'Choose relevant variables (production volume, weather, etc.)'},
                        {'step': 3, 'title': 'Train Consumption Model', 'description': 'Build model to predict energy usage'},
                        {'step': 4, 'title': 'View Forecasts', 'description': 'See predicted consumption for planning'}
                    ],
                    default_template='regression',
                    sample_data='energy_consumption.csv'
                ),
                Scenario(
                    id='production_scheduling',
                    name='Production Scheduling Optimization',
                    description='Optimize production schedules to minimize downtime and maximize throughput',
                    icon='bi-calendar-event',
                    difficulty='advanced',
                    estimated_time='30 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Production Data', 'description': 'Upload machine availability, orders, and setup times'},
                        {'step': 2, 'title': 'Define Constraints', 'description': 'Set production constraints and priorities'},
                        {'step': 3, 'title': 'Train Scheduler', 'description': 'Build model to optimize production schedule'},
                        {'step': 4, 'title': 'View Schedule', 'description': 'See optimized production schedule'}
                    ],
                    default_template='regression',
                    sample_data='production_schedule.csv'
                ),
                Scenario(
                    id='inventory_optimization',
                    name='Inventory Optimization',
                    description='Optimize stock levels to balance cost and service level',
                    icon='bi-boxes',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Inventory Data', 'description': 'Upload demand history, lead times, and costs'},
                        {'step': 2, 'title': 'Set Service Level', 'description': 'Define target service level (stockout probability)'},
                        {'step': 3, 'title': 'Train Inventory Model', 'description': 'Build model to calculate optimal stock levels'},
                        {'step': 4, 'title': 'View Recommendations', 'description': 'See recommended reorder points and quantities'}
                    ],
                    default_template='regression',
                    sample_data='inventory_data.csv'
                ),
                Scenario(
                    id='supplier_quality',
                    name='Supplier Quality Assessment',
                    description='Evaluate and predict supplier quality performance',
                    icon='bi-award',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Supplier Data', 'description': 'Upload supplier performance history and quality metrics'},
                        {'step': 2, 'title': 'Define Quality Criteria', 'description': 'Set quality standards and scoring'},
                        {'step': 3, 'title': 'Train Quality Model', 'description': 'Build model to predict supplier quality'},
                        {'step': 4, 'title': 'View Ratings', 'description': 'See supplier quality scores and rankings'}
                    ],
                    default_template='regression',
                    sample_data='supplier_quality.csv'
                ),
                Scenario(
                    id='waste_reduction',
                    name='Waste Reduction Analysis',
                    description='Identify sources of waste and predict waste generation',
                    icon='bi-recycle',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Waste Data', 'description': 'Upload waste generation records and process data'},
                        {'step': 2, 'title': 'Define Waste Types', 'description': 'Categorize waste (material, time, energy, etc.)'},
                        {'step': 3, 'title': 'Train Waste Model', 'description': 'Build model to predict waste generation'},
                        {'step': 4, 'title': 'View Analysis', 'description': 'See waste predictions and reduction opportunities'}
                    ],
                    default_template='regression',
                    sample_data='waste_data.csv'
                ),
                Scenario(
                    id='equipment_efficiency',
                    name='Equipment Efficiency Analysis',
                    description='Analyze and predict equipment efficiency to optimize operations',
                    icon='bi-speedometer',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Equipment Data', 'description': 'Upload performance metrics and operating conditions'},
                        {'step': 2, 'title': 'Calculate Efficiency', 'description': 'Define efficiency metrics (OEE, utilization, etc.)'},
                        {'step': 3, 'title': 'Train Efficiency Model', 'description': 'Build model to predict equipment efficiency'},
                        {'step': 4, 'title': 'View Predictions', 'description': 'See efficiency forecasts and improvement opportunities'}
                    ],
                    default_template='regression',
                    sample_data='equipment_efficiency.csv'
                ),
                Scenario(
                    id='quality_control',
                    name='Statistical Process Control',
                    description='Monitor process quality using control charts and anomaly detection',
                    icon='bi-graph-up',
                    difficulty='advanced',
                    estimated_time='25 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Process Data', 'description': 'Upload quality measurements over time'},
                        {'step': 2, 'title': 'Set Control Limits', 'description': 'Define upper and lower control limits'},
                        {'step': 3, 'title': 'Train Control Model', 'description': 'Build model to detect out-of-control conditions'},
                        {'step': 4, 'title': 'View Control Charts', 'description': 'See process control charts and alerts'}
                    ],
                    default_template='anomaly_detection',
                    sample_data='quality_control.csv'
                ),
                Scenario(
                    id='demand_forecasting',
                    name='Demand Forecasting',
                    description='Forecast product demand for production and inventory planning',
                    icon='bi-cart',
                    difficulty='intermediate',
                    estimated_time='20 mins',
                    steps=[
                        {'step': 1, 'title': 'Upload Sales Data', 'description': 'Upload historical sales and demand data'},
                        {'step': 2, 'title': 'Select Forecast Horizon', 'description': 'Choose prediction period (daily, weekly, monthly)'},
                        {'step': 3, 'title': 'Train Forecast Model', 'description': 'Build time series model for demand'},
                        {'step': 4, 'title': 'View Forecasts', 'description': 'See predicted demand with confidence intervals'}
                    ],
                    default_template='time_series_regression',
                    sample_data='demand_forecast.csv'
                )
            ],
            terminology={
                'features': 'Process Parameters',
                'target': 'Output Variable',
                'training': 'Model Training',
                'prediction': 'Prediction',
                'classification': 'Quality Grade',
                'regression': 'Value Prediction',
                'clustering': 'Process Grouping',
                'model': 'Process Model'
            },
            dashboard_widgets=['production_summary', 'quality_metrics', 'maintenance_alerts'],
            hidden_features=[],
            recommended_nodes=[
                'data_load_csv', 'preprocess_select_features_target', 'preprocess_train_test_split',
                'sklearn_standard_scaler', 'algorithm_random_forest_classifier',
                'algorithm_random_forest_regressor', 'algorithm_isolation_forest',
                'evaluate_metrics', 'output_save_model'
            ],
            hidden_nodes=[]
        ))
    
    def register(self, profile: IndustryProfile):
        """Register an industry profile"""
        self._profiles[profile.id] = profile
    
    def get(self, profile_id: str) -> Optional[IndustryProfile]:
        """Get a profile by ID"""
        return self._profiles.get(profile_id)
    
    def list_all(self) -> List[IndustryProfile]:
        """List all registered profiles"""
        return list(self._profiles.values())
    
    def get_scenarios(self, profile_id: str) -> List[Scenario]:
        """Get scenarios for a profile"""
        profile = self.get(profile_id)
        return profile.scenarios if profile else []


# Global instance
profile_manager = IndustryProfileManager()

