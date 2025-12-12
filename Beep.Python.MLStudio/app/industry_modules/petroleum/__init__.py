"""
Petroleum Engineering Industry Module
Specialized ML workflows for oil & gas, reservoir engineering, and production optimization
"""

from ..base_module import (
    IndustryModule, ModuleCategory, NodeDefinition, 
    WorkflowTemplate, SampleDataset, ModuleRegistry
)
from typing import Dict, List, Any


class PetroleumModule(IndustryModule):
    """
    Petroleum Engineering Module
    
    Provides specialized nodes and templates for:
    - Production forecasting (Decline Curve Analysis)
    - Well log analysis and interpretation
    - Reservoir characterization
    - Rock/lithology classification
    - PVT (Pressure-Volume-Temperature) analysis
    - EUR (Estimated Ultimate Recovery) prediction
    """
    
    @property
    def id(self) -> str:
        return "petroleum"
    
    @property
    def name(self) -> str:
        return "Petroleum Engineering"
    
    @property
    def description(self) -> str:
        return "ML tools for oil & gas production forecasting, well log analysis, and reservoir characterization"
    
    @property
    def category(self) -> ModuleCategory:
        return ModuleCategory.PETROLEUM
    
    @property
    def version(self) -> str:
        return "1.0.0"
    
    @property
    def icon(self) -> str:
        return "bi-droplet-fill"
    
    @property
    def color(self) -> str:
        return "#795548"  # Brown for petroleum
    
    def _initialize(self):
        """Initialize petroleum-specific nodes, templates, and datasets"""
        self._register_nodes()
        self._register_templates()
        self._register_datasets()
    
    def _register_nodes(self):
        """Register petroleum-specific nodes"""
        
        # Well Log Loader
        self.register_node(NodeDefinition(
            id="well_log_loader",
            name="Well Log Loader",
            category="data_source",
            description="Load well log data from LAS, CSV, or DLIS files",
            icon="bi-file-earmark-bar-graph",
            color="#795548",
            inputs=[],
            outputs=[{"id": "output", "label": "Well Log Data", "type": "dataframe"}],
            properties=[
                {"name": "file_format", "label": "File Format", "type": "select",
                 "options": ["las", "csv", "dlis"], "default": "csv"},
                {"name": "file_path", "label": "File Path", "type": "file", "default": ""},
                {"name": "well_name", "label": "Well Name", "type": "text", "default": "Well_1"},
                {"name": "depth_unit", "label": "Depth Unit", "type": "select",
                 "options": ["ft", "m"], "default": "ft"},
            ],
            imports=["import pandas as pd", "import numpy as np"],
            code_template='''# Load Well Log Data
import pandas as pd
import numpy as np

{%- if file_format == "las" %}
# For LAS files, you would need lasio: pip install lasio
# import lasio
# las = lasio.read('{{ file_path }}')
# df = las.df()
df = pd.read_csv('{{ file_path }}')  # Fallback to CSV
{%- else %}
df = pd.read_csv('{{ file_path }}')
{%- endif %}

# Standardize column names
df.columns = [c.upper().strip() for c in df.columns]
print(f'Loaded well log data for {{ well_name }}: {df.shape}')
print(f'Curves: {list(df.columns)}')
'''
        ))
        
        # Production Data Loader
        self.register_node(NodeDefinition(
            id="production_data_loader",
            name="Production Data Loader",
            category="data_source",
            description="Load oil/gas production data",
            icon="bi-bar-chart-steps",
            color="#4caf50",
            inputs=[],
            outputs=[{"id": "output", "label": "Production Data", "type": "dataframe"}],
            properties=[
                {"name": "file_path", "label": "File Path", "type": "file", "default": ""},
                {"name": "date_column", "label": "Date Column", "type": "text", "default": "Date"},
                {"name": "oil_column", "label": "Oil Rate Column", "type": "text", "default": "Oil_Rate"},
                {"name": "gas_column", "label": "Gas Rate Column", "type": "text", "default": "Gas_Rate"},
                {"name": "water_column", "label": "Water Rate Column", "type": "text", "default": "Water_Rate"},
            ],
            imports=["import pandas as pd", "import numpy as np"],
            code_template='''# Load Production Data
import pandas as pd
import numpy as np

df = pd.read_csv('{{ file_path }}', parse_dates=['{{ date_column }}'])
df = df.sort_values('{{ date_column }}')

# Calculate cumulative production
if '{{ oil_column }}' in df.columns:
    df['Cum_Oil'] = df['{{ oil_column }}'].cumsum()
if '{{ gas_column }}' in df.columns:
    df['Cum_Gas'] = df['{{ gas_column }}'].cumsum()
if '{{ water_column }}' in df.columns:
    df['Cum_Water'] = df['{{ water_column }}'].cumsum()

# Calculate time on production
df['Days_On'] = (df['{{ date_column }}'] - df['{{ date_column }}'].min()).dt.days

print(f'Loaded production data: {df.shape}')
print(f'Date range: {df["{{ date_column }}"].min()} to {df["{{ date_column }}"].max()}')
'''
        ))
        
        # Decline Curve Analysis
        self.register_node(NodeDefinition(
            id="decline_curve_analysis",
            name="Decline Curve Analysis",
            category="analysis",
            description="Fit Arps decline curves to production data",
            icon="bi-graph-down",
            color="#ff9800",
            inputs=[{"id": "input", "label": "Production Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "DCA Results", "type": "dict"}],
            properties=[
                {"name": "rate_column", "label": "Rate Column", "type": "text", "default": "Oil_Rate"},
                {"name": "time_column", "label": "Time Column", "type": "text", "default": "Days_On"},
                {"name": "decline_type", "label": "Decline Type", "type": "select",
                 "options": ["exponential", "hyperbolic", "harmonic"], "default": "hyperbolic"},
                {"name": "forecast_days", "label": "Forecast Days", "type": "number", "default": 365},
            ],
            imports=["import pandas as pd", "import numpy as np", "from scipy.optimize import curve_fit"],
            code_template='''# Decline Curve Analysis (Arps)
import numpy as np
from scipy.optimize import curve_fit

def exponential_decline(t, qi, Di):
    """Exponential decline: q = qi * exp(-Di * t)"""
    return qi * np.exp(-Di * t)

def hyperbolic_decline(t, qi, Di, b):
    """Hyperbolic decline: q = qi / (1 + b * Di * t)^(1/b)"""
    return qi / np.power(1 + b * Di * t, 1/b)

def harmonic_decline(t, qi, Di):
    """Harmonic decline (b=1): q = qi / (1 + Di * t)"""
    return qi / (1 + Di * t)

# Prepare data
t = df['{{ time_column }}'].values
q = df['{{ rate_column }}'].values

# Remove zeros and negatives
mask = (q > 0) & (t >= 0)
t_clean = t[mask]
q_clean = q[mask]

# Initial guesses
qi_init = q_clean[0] if len(q_clean) > 0 else 100
Di_init = 0.001

{%- if decline_type == "exponential" %}
# Fit exponential decline
try:
    popt, pcov = curve_fit(exponential_decline, t_clean, q_clean, p0=[qi_init, Di_init], maxfev=5000)
    qi, Di = popt
    decline_params = {'qi': qi, 'Di': Di, 'b': 0}
    forecast_func = lambda t: exponential_decline(t, qi, Di)
except:
    decline_params = {'qi': qi_init, 'Di': Di_init, 'b': 0}
    forecast_func = lambda t: exponential_decline(t, qi_init, Di_init)
{%- elif decline_type == "hyperbolic" %}
# Fit hyperbolic decline
try:
    popt, pcov = curve_fit(hyperbolic_decline, t_clean, q_clean, p0=[qi_init, Di_init, 0.5], 
                           bounds=([0, 0, 0], [np.inf, 1, 2]), maxfev=5000)
    qi, Di, b = popt
    decline_params = {'qi': qi, 'Di': Di, 'b': b}
    forecast_func = lambda t: hyperbolic_decline(t, qi, Di, b)
except:
    decline_params = {'qi': qi_init, 'Di': Di_init, 'b': 0.5}
    forecast_func = lambda t: hyperbolic_decline(t, qi_init, Di_init, 0.5)
{%- else %}
# Fit harmonic decline
try:
    popt, pcov = curve_fit(harmonic_decline, t_clean, q_clean, p0=[qi_init, Di_init], maxfev=5000)
    qi, Di = popt
    decline_params = {'qi': qi, 'Di': Di, 'b': 1}
    forecast_func = lambda t: harmonic_decline(t, qi, Di)
except:
    decline_params = {'qi': qi_init, 'Di': Di_init, 'b': 1}
    forecast_func = lambda t: harmonic_decline(t, qi_init, Di_init)
{%- endif %}

# Generate forecast
t_forecast = np.arange(0, t_clean.max() + {{ forecast_days }}, 1)
q_forecast = forecast_func(t_forecast)

# Calculate EUR (Estimated Ultimate Recovery)
eur = np.trapz(q_forecast, t_forecast)

dca_results = {
    'decline_type': '{{ decline_type }}',
    'parameters': decline_params,
    'EUR': eur,
    'forecast_days': {{ forecast_days }},
    't_forecast': t_forecast,
    'q_forecast': q_forecast
}

print('=== Decline Curve Analysis Results ===')
print(f'Decline Type: {{ decline_type }}')
print(f'qi: {decline_params["qi"]:.2f}')
print(f'Di: {decline_params["Di"]:.6f}')
print(f'b: {decline_params["b"]:.2f}')
print(f'EUR: {eur:,.0f} (cumulative over forecast period)')
'''
        ))
        
        # Log Normalization
        self.register_node(NodeDefinition(
            id="log_normalization",
            name="Log Normalization",
            category="preprocessing",
            description="Normalize well log curves (depth shift, environmental corrections)",
            icon="bi-sliders",
            color="#2196f3",
            inputs=[{"id": "input", "label": "Well Log Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "Normalized Logs", "type": "dataframe"}],
            properties=[
                {"name": "depth_column", "label": "Depth Column", "type": "text", "default": "DEPTH"},
                {"name": "normalize_gr", "label": "Normalize GR", "type": "boolean", "default": True},
                {"name": "gr_min", "label": "GR Min", "type": "number", "default": 0},
                {"name": "gr_max", "label": "GR Max", "type": "number", "default": 150},
            ],
            imports=["import pandas as pd", "import numpy as np"],
            code_template='''# Log Normalization
import numpy as np

# Standardize depth
if '{{ depth_column }}' in df.columns:
    df = df.sort_values('{{ depth_column }}')

{%- if normalize_gr %}
# Normalize Gamma Ray (GR) to 0-1 range
if 'GR' in df.columns:
    df['GR_NORM'] = (df['GR'] - {{ gr_min }}) / ({{ gr_max }} - {{ gr_min }})
    df['GR_NORM'] = df['GR_NORM'].clip(0, 1)
{%- endif %}

# Handle missing values in log data
for col in df.select_dtypes(include=[np.number]).columns:
    if col != '{{ depth_column }}':
        # Replace -999 (common null value in logs) with NaN
        df[col] = df[col].replace(-999.25, np.nan)
        df[col] = df[col].replace(-999, np.nan)
        # Interpolate small gaps
        df[col] = df[col].interpolate(method='linear', limit=5)

print(f'Normalized log data. Shape: {df.shape}')
print(f'Columns: {list(df.columns)}')
'''
        ))
        
        # Lithology Classifier
        self.register_node(NodeDefinition(
            id="lithology_classifier",
            name="Lithology Classifier",
            category="classification",
            description="Classify rock types from well log data",
            icon="bi-layers",
            color="#9c27b0",
            inputs=[{"id": "input", "label": "Well Log Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "Classified Data", "type": "dataframe"}],
            properties=[
                {"name": "features", "label": "Log Curves (features)", "type": "text", 
                 "default": "GR,RHOB,NPHI,DT"},
                {"name": "target", "label": "Lithology Column", "type": "text", "default": "LITHOLOGY"},
                {"name": "model_type", "label": "Model Type", "type": "select",
                 "options": ["random_forest", "xgboost", "svm"], "default": "random_forest"},
            ],
            imports=["from sklearn.ensemble import RandomForestClassifier"],
            code_template='''# Lithology Classification
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler, LabelEncoder

# Prepare features
feature_cols = [c.strip() for c in '{{ features }}'.split(',')]
available_features = [c for c in feature_cols if c in df.columns]

if not available_features:
    print(f"Warning: No feature columns found. Available: {list(df.columns)}")
else:
    X = df[available_features].dropna()
    
    if '{{ target }}' in df.columns:
        # Supervised classification
        y = df.loc[X.index, '{{ target }}']
        
        # Encode labels
        le = LabelEncoder()
        y_encoded = le.fit_transform(y)
        
        # Scale features
        scaler = StandardScaler()
        X_scaled = scaler.fit_transform(X)
        
        # Train model
        model = RandomForestClassifier(n_estimators=100, random_state=42)
        model.fit(X_scaled, y_encoded)
        
        # Predict
        df.loc[X.index, 'LITHOLOGY_PRED'] = le.inverse_transform(model.predict(X_scaled))
        
        print(f'Lithology classification complete')
        print(f'Classes: {list(le.classes_)}')
        print(f'Feature importance:')
        for feat, imp in sorted(zip(available_features, model.feature_importances_), key=lambda x: -x[1]):
            print(f'  {feat}: {imp:.3f}')
    else:
        print(f"No target column '{{ target }}' found - cannot train supervised model")
'''
        ))
        
        # Petrophysical Calculator
        self.register_node(NodeDefinition(
            id="petrophysical_calculator",
            name="Petrophysical Calculator",
            category="feature_engineering",
            description="Calculate porosity, water saturation, permeability",
            icon="bi-calculator",
            color="#607d8b",
            inputs=[{"id": "input", "label": "Well Log Data", "type": "dataframe"}],
            outputs=[{"id": "output", "label": "Petrophysical Data", "type": "dataframe"}],
            properties=[
                {"name": "rhob_col", "label": "Density Log (RHOB)", "type": "text", "default": "RHOB"},
                {"name": "nphi_col", "label": "Neutron Porosity (NPHI)", "type": "text", "default": "NPHI"},
                {"name": "rt_col", "label": "Resistivity (RT)", "type": "text", "default": "RT"},
                {"name": "rho_matrix", "label": "Matrix Density", "type": "number", "default": 2.65},
                {"name": "rho_fluid", "label": "Fluid Density", "type": "number", "default": 1.0},
                {"name": "rw", "label": "Water Resistivity (Rw)", "type": "number", "default": 0.05},
                {"name": "a", "label": "Archie 'a'", "type": "number", "default": 1.0},
                {"name": "m", "label": "Archie 'm'", "type": "number", "default": 2.0},
                {"name": "n", "label": "Archie 'n'", "type": "number", "default": 2.0},
            ],
            imports=["import numpy as np"],
            code_template='''# Petrophysical Calculations
import numpy as np

# Density Porosity
if '{{ rhob_col }}' in df.columns:
    rho_matrix = {{ rho_matrix }}
    rho_fluid = {{ rho_fluid }}
    df['PHID'] = (rho_matrix - df['{{ rhob_col }}']) / (rho_matrix - rho_fluid)
    df['PHID'] = df['PHID'].clip(0, 0.5)  # Realistic bounds

# Total Porosity (average of density and neutron)
if '{{ nphi_col }}' in df.columns and 'PHID' in df.columns:
    df['PHIT'] = (df['PHID'] + df['{{ nphi_col }}']) / 2
    df['PHIT'] = df['PHIT'].clip(0, 0.5)

# Water Saturation (Archie equation)
if '{{ rt_col }}' in df.columns and 'PHIT' in df.columns:
    Rw = {{ rw }}
    a = {{ a }}
    m = {{ m }}
    n = {{ n }}
    
    # Sw = (a * Rw / (Rt * Phi^m))^(1/n)
    df['SW'] = np.power(a * Rw / (df['{{ rt_col }}'] * np.power(df['PHIT'], m)), 1/n)
    df['SW'] = df['SW'].clip(0, 1)
    
    # Hydrocarbon Saturation
    df['SHC'] = 1 - df['SW']

# Permeability estimate (Timur equation)
if 'PHIT' in df.columns and 'SW' in df.columns:
    # K = 0.136 * (Phi^4.4) / (Swi^2)
    df['PERM'] = 0.136 * np.power(df['PHIT'] * 100, 4.4) / np.power(df['SW'] * 100, 2)
    df['PERM'] = df['PERM'].clip(0.001, 10000)  # mD bounds

print('=== Petrophysical Calculations Complete ===')
for col in ['PHID', 'PHIT', 'SW', 'SHC', 'PERM']:
    if col in df.columns:
        print(f'{col}: mean={df[col].mean():.4f}, min={df[col].min():.4f}, max={df[col].max():.4f}')
'''
        ))
    
    def _register_templates(self):
        """Register petroleum-specific workflow templates"""
        
        # Production Forecasting Template
        self.register_template(WorkflowTemplate(
            id="production_forecast",
            name="Production Forecasting",
            description="Forecast oil/gas production using decline curve analysis",
            category="forecasting",
            icon="bi-graph-down",
            color="#ff9800",
            tags=["production", "DCA", "decline", "forecasting", "oil", "gas"],
            difficulty="intermediate",
            nodes=[
                {"id": "start", "type": "start", "position": {"x": 50, "y": 150},
                 "data": {"message": "Production Forecasting Pipeline"}},
                {"id": "load", "type": "petroleum_production_data_loader", "position": {"x": 250, "y": 150},
                 "data": {"file_path": "data/production.csv"}},
                {"id": "prep", "type": "auto_data_prep", "position": {"x": 450, "y": 150},
                 "data": {}},
                {"id": "dca", "type": "petroleum_decline_curve_analysis", "position": {"x": 650, "y": 150},
                 "data": {"decline_type": "hyperbolic", "forecast_days": 365}},
            ],
            edges=[
                {"source": "start", "target": "load"},
                {"source": "load", "target": "prep"},
                {"source": "prep", "target": "dca"},
            ]
        ))
        
        # Well Log Classification Template
        self.register_template(WorkflowTemplate(
            id="lithology_classification",
            name="Lithology Classification",
            description="Classify rock types from well log data using ML",
            category="classification",
            icon="bi-layers",
            color="#9c27b0",
            tags=["well logs", "lithology", "classification", "petrophysics"],
            difficulty="intermediate",
            nodes=[
                {"id": "start", "type": "start", "position": {"x": 50, "y": 150},
                 "data": {"message": "Lithology Classification Pipeline"}},
                {"id": "load", "type": "petroleum_well_log_loader", "position": {"x": 250, "y": 150},
                 "data": {"file_path": "data/well_logs.csv"}},
                {"id": "normalize", "type": "petroleum_log_normalization", "position": {"x": 450, "y": 150},
                 "data": {}},
                {"id": "petro", "type": "petroleum_petrophysical_calculator", "position": {"x": 650, "y": 150},
                 "data": {}},
                {"id": "classify", "type": "petroleum_lithology_classifier", "position": {"x": 850, "y": 150},
                 "data": {"features": "GR,RHOB,NPHI,DT"}},
            ],
            edges=[
                {"source": "start", "target": "load"},
                {"source": "load", "target": "normalize"},
                {"source": "normalize", "target": "petro"},
                {"source": "petro", "target": "classify"},
            ]
        ))
        
        # Petrophysical Analysis Template
        self.register_template(WorkflowTemplate(
            id="petrophysical_analysis",
            name="Petrophysical Analysis",
            description="Complete petrophysical interpretation workflow",
            category="analysis",
            icon="bi-calculator",
            color="#607d8b",
            tags=["petrophysics", "porosity", "saturation", "permeability"],
            difficulty="beginner",
            nodes=[
                {"id": "start", "type": "start", "position": {"x": 50, "y": 150},
                 "data": {"message": "Petrophysical Analysis Pipeline"}},
                {"id": "load", "type": "petroleum_well_log_loader", "position": {"x": 250, "y": 150},
                 "data": {"file_path": "data/well_logs.csv"}},
                {"id": "normalize", "type": "petroleum_log_normalization", "position": {"x": 450, "y": 150},
                 "data": {}},
                {"id": "petro", "type": "petroleum_petrophysical_calculator", "position": {"x": 650, "y": 150},
                 "data": {}},
            ],
            edges=[
                {"source": "start", "target": "load"},
                {"source": "load", "target": "normalize"},
                {"source": "normalize", "target": "petro"},
            ]
        ))
    
    def _register_datasets(self):
        """Register sample petroleum datasets"""
        
        self.register_dataset(SampleDataset(
            id="well_logs_sample",
            name="Sample Well Logs",
            description="Well log data with GR, RHOB, NPHI, DT, RT curves",
            filename="well_logs.csv",
            format="csv",
            columns=[
                {"name": "DEPTH", "type": "float"},
                {"name": "GR", "type": "float"},
                {"name": "RHOB", "type": "float"},
                {"name": "NPHI", "type": "float"},
                {"name": "DT", "type": "float"},
                {"name": "RT", "type": "float"},
                {"name": "LITHOLOGY", "type": "string"},
            ],
            rows=5000,
            size_kb=300,
        ))
        
        self.register_dataset(SampleDataset(
            id="production_sample",
            name="Sample Production Data",
            description="Monthly oil/gas production data for decline analysis",
            filename="production.csv",
            format="csv",
            columns=[
                {"name": "Date", "type": "datetime"},
                {"name": "Oil_Rate", "type": "float"},
                {"name": "Gas_Rate", "type": "float"},
                {"name": "Water_Rate", "type": "float"},
                {"name": "Well_Name", "type": "string"},
            ],
            rows=1000,
            size_kb=50,
        ))


def register(registry: ModuleRegistry):
    """Register the Petroleum module with the global registry"""
    registry.register(PetroleumModule())

