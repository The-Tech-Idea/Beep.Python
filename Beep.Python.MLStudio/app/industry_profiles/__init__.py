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
            color='#6c757d',
            gradient='linear-gradient(135deg, #6c757d 0%, #495057 100%)',
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
            color='#0d6efd',
            gradient='linear-gradient(135deg, #0d6efd 0%, #0a58ca 100%)',
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
            color='#fd7e14',
            gradient='linear-gradient(135deg, #fd7e14 0%, #dc3545 100%)',
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
            color='#dc3545',
            gradient='linear-gradient(135deg, #dc3545 0%, #c82333 100%)',
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
            color='#198754',
            gradient='linear-gradient(135deg, #198754 0%, #157347 100%)',
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

