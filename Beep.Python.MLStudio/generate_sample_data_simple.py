#!/usr/bin/env python3
"""
Generate Sample Data Files for All Industry Scenarios
Creates realistic CSV sample data files for testing scenarios
"""
import csv
import random
from pathlib import Path
from datetime import datetime, timedelta

# Set random seed for reproducibility
random.seed(42)

# Output directory
output_dir = Path('static/data/samples')
output_dir.mkdir(parents=True, exist_ok=True)

print("Generating sample data files for all industry scenarios...")

def write_csv(filename, headers, rows):
    """Write CSV file"""
    filepath = output_dir / filename
    with open(filepath, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow(headers)
        writer.writerows(rows)
    print(f"[OK] {filename}")

# ============================================================================
# FINANCE SCENARIOS
# ============================================================================

# 1. Stock Price Prediction
dates = [(datetime(2020, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(1461)]
rows = []
for i, date in enumerate(dates):
    base_price = 100 + i * 0.1 + random.gauss(0, 0.5)
    rows.append([
        date,
        round(base_price, 2),
        round(base_price * (1 + random.uniform(0, 0.03)), 2),
        round(base_price * (1 - random.uniform(0, 0.03)), 2),
        round(base_price + random.gauss(0, 0.5), 2),
        random.randint(1000000, 10000000)
    ])
write_csv('stock_prices.csv', ['Date', 'Open', 'High', 'Low', 'Close', 'Volume'], rows)

# 2. Credit Risk Scoring
rows = []
for i in range(1, 1001):
    rows.append([
        f'CUST_{i:04d}',
        random.randint(22, 70),
        random.randint(20000, 150000),
        random.randint(300, 850),
        random.randint(5000, 50000),
        random.choice([12, 24, 36, 48, 60]),
        round(random.uniform(0, 30), 1),
        round(random.uniform(0.1, 0.8), 2),
        random.choice([0, 1])
    ])
write_csv('loan_data.csv', ['customer_id', 'age', 'income', 'credit_score', 'loan_amount', 
                            'loan_term_months', 'employment_years', 'debt_to_income', 'default'], rows)

# 3. Fraud Detection
rows = []
for i in range(1, 5001):
    rows.append([
        f'TXN_{i:06d}',
        round(random.lognormvariate(4, 1.5), 2),
        random.choice(['Retail', 'Restaurant', 'Gas', 'Online', 'Travel']),
        random.randint(0, 23),
        random.randint(0, 6),
        round(random.uniform(0, 100), 1),
        random.randint(1, 20),
        random.choice([0, 1])
    ])
write_csv('transactions.csv', ['transaction_id', 'amount', 'merchant_category', 'hour', 
                              'day_of_week', 'distance_from_home', 'transaction_count_24h', 'is_fraud'], rows)

# 4. Portfolio Returns
dates = [(datetime(2020, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(1461)]
rows = []
for date in dates:
    rows.append([
        date,
        round(random.gauss(0, 0.02), 4),
        round(random.gauss(0, 0.015), 4),
        round(random.gauss(0, 0.025), 4),
        round(random.gauss(0, 0.005), 4),
        round(random.gauss(0, 0.03), 4)
    ])
write_csv('portfolio_returns.csv', ['date', 'stock_a_return', 'stock_b_return', 'stock_c_return', 
                                    'bond_return', 'commodity_return'], rows)

# 5. Customer Churn
rows = []
for i in range(1, 2001):
    tenure = random.randint(1, 60)
    monthly = round(random.uniform(20, 100), 2)
    rows.append([
        f'CUST_{i:05d}',
        random.randint(18, 80),
        tenure,
        monthly,
        round(tenure * monthly + random.uniform(-100, 100), 2),
        random.choice(['Month-to-month', 'One year', 'Two year']),
        random.choice(['Electronic', 'Mailed check', 'Bank transfer']),
        1 if (tenure < 12 and random.random() < 0.3) or random.random() < 0.1 else 0
    ])
write_csv('customer_data.csv', ['customer_id', 'age', 'tenure_months', 'monthly_charges', 
                               'total_charges', 'contract_type', 'payment_method', 'churn'], rows)

# 6. Market Segmentation
rows = []
for i in range(1, 1501):
    rows.append([
        f'CUST_{i:05d}',
        random.randint(18, 75),
        random.randint(20000, 200000),
        random.randint(1, 100),
        random.randint(1, 50),
        round(random.uniform(10, 500), 2),
        random.choice(['Electronics', 'Clothing', 'Food', 'Travel', 'Other'])
    ])
write_csv('customer_segments.csv', ['customer_id', 'age', 'annual_income', 'spending_score', 
                                    'purchase_frequency', 'avg_transaction_value', 'preferred_category'], rows)

# 7. Loan Default Prediction
rows = []
for i in range(1, 2001):
    rows.append([
        f'LOAN_{i:05d}',
        f'CUST_{i:05d}',
        random.randint(5000, 500000),
        random.choice(['Personal', 'Auto', 'Mortgage', 'Business']),
        random.randint(12, 360),
        random.randint(500, 850),
        round(random.uniform(0.1, 0.8), 2),
        random.randint(1, 60),
        round(random.uniform(0.05, 0.15), 4),
        1 if random.random() < 0.12 else 0
    ])
write_csv('loan_portfolio.csv', ['loan_id', 'customer_id', 'loan_amount', 'loan_type', 
                                 'term_months', 'credit_score', 'debt_to_income', 'months_since_origination', 
                                 'interest_rate', 'defaulted'], rows)

# 8. Market Volatility
dates = [(datetime(2020, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(1461)]
rows = []
for date in dates:
    rows.append([
        date,
        round(100 + random.gauss(0, 2), 2),
        round(random.uniform(0.01, 0.05), 4),
        round(random.uniform(0.5, 2.0), 2),
        round(random.uniform(0.02, 0.08), 4)
    ])
write_csv('market_volatility.csv', ['date', 'price', 'historical_volatility', 'vix_index', 'predicted_volatility'], rows)

# 9. Currency Exchange Rates
dates = [(datetime(2020, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(1461)]
rows = []
base_rate = 1.10
for i, date in enumerate(dates):
    base_rate += random.gauss(0, 0.01)
    rows.append([
        date,
        round(base_rate, 4),
        round(random.uniform(0.01, 0.03), 4),
        round(random.uniform(0.02, 0.05), 4),
        round(random.uniform(1.0, 3.0), 2)
    ])
write_csv('currency_rates.csv', ['date', 'usd_eur_rate', 'inflation_rate', 'interest_rate_diff', 'trade_balance'], rows)

# 10. Insurance Claims
rows = []
for i in range(1, 1501):
    rows.append([
        f'POLICY_{i:05d}',
        random.randint(18, 80),
        random.choice(['Auto', 'Home', 'Health', 'Life']),
        round(random.uniform(100, 5000), 2),
        random.randint(0, 5),
        round(random.uniform(0, 50000), 2),
        1 if random.random() < 0.15 else 0
    ])
write_csv('insurance_claims.csv', ['policy_id', 'age', 'policy_type', 'premium', 'claims_count', 
                                   'total_claim_amount', 'has_claim'], rows)

# 11. Revenue Forecasting
dates = [(datetime(2020, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(1461)]
rows = []
base_revenue = 100000
for i, date in enumerate(dates):
    base_revenue += random.gauss(0, 5000)
    rows.append([
        date,
        round(max(0, base_revenue), 2),
        round(random.uniform(0.8, 1.2), 2),
        round(random.uniform(0.05, 0.15), 2)
    ])
write_csv('revenue_data.csv', ['date', 'revenue', 'seasonality_factor', 'growth_rate'], rows)

# 12. Customer Lifetime Value
rows = []
for i in range(1, 1001):
    months = random.randint(1, 60)
    monthly_value = random.uniform(10, 200)
    rows.append([
        f'CUST_{i:05d}',
        months,
        round(monthly_value, 2),
        round(months * monthly_value, 2),
        random.randint(1, 50),
        round(random.uniform(0.7, 0.95), 2),
        round(months * monthly_value * random.uniform(0.8, 1.2), 2)
    ])
write_csv('customer_clv.csv', ['customer_id', 'tenure_months', 'avg_monthly_value', 'historical_clv', 
                               'purchase_frequency', 'retention_rate', 'predicted_clv'], rows)

# 13. Price Elasticity
rows = []
for i in range(1, 501):
    price = random.uniform(10, 100)
    rows.append([
        f'PROD_{i:03d}',
        round(price, 2),
        round(random.uniform(100, 1000), 0),
        round(random.uniform(50, 500), 0),
        round(random.uniform(-2.0, -0.5), 2)
    ])
write_csv('price_elasticity.csv', ['product_id', 'price', 'demand_at_price', 'demand_at_base', 'elasticity'], rows)

# 14. Bond Yields
rows = []
for i in range(1, 301):
    rows.append([
        f'BOND_{i:04d}',
        random.choice(['AAA', 'AA', 'A', 'BBB', 'BB']),
        random.randint(1, 30),
        round(random.uniform(0.5, 5.0), 2),
        round(random.uniform(2.0, 6.0), 2),
        round(random.uniform(0.01, 0.05), 4)
    ])
write_csv('bond_yields.csv', ['bond_id', 'credit_rating', 'maturity_years', 'coupon_rate', 
                              'market_yield', 'predicted_yield'], rows)

# 15. Trading Signals
dates = [(datetime(2023, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(365)]
rows = []
for date in dates:
    rows.append([
        date,
        round(100 + random.gauss(0, 2), 2),
        round(random.uniform(0.5, 2.0), 2),
        round(random.uniform(0.8, 1.2), 2),
        random.choice(['Buy', 'Sell', 'Hold'])
    ])
write_csv('trading_signals.csv', ['date', 'price', 'rsi', 'macd', 'signal'], rows)

# ============================================================================
# PETROLEUM SCENARIOS
# ============================================================================

# 1. Well Log Interpretation
rows = []
for depth in range(5000, 10000, 1):
    rows.append([
        depth,
        round(20 + random.gauss(0, 10), 2),
        round(2.0 + random.gauss(0, 0.3), 3),
        round(0.2 + random.gauss(0, 0.1), 3),
        round(60 + random.gauss(0, 10), 2),
        round(10 ** (1 + random.gauss(0, 0.5)), 2),
        random.choice(['Sandstone', 'Shale', 'Limestone', 'Dolomite'])
    ])
write_csv('well_logs.csv', ['depth', 'GR', 'RHOB', 'NPHI', 'DT', 'RES', 'lithology'], rows)

# 2. Production Forecasting
dates = [(datetime(2020, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(1461)]
rows = []
for i, date in enumerate(dates):
    decline = i * 0.0001
    rows.append([
        date,
        round(1000 * (1 - decline) + random.gauss(0, 50), 2),
        round(2000 * (1 - decline) + random.gauss(0, 100), 2),
        round(random.uniform(50, 200), 2),
        round(3000 - i * 0.5 + random.gauss(0, 50), 2)
    ])
write_csv('production_history.csv', ['date', 'oil_rate_bopd', 'gas_rate_mcfd', 'water_rate_bwpd', 'pressure_psi'], rows)

# 3. Decline Curve Analysis
rows = []
for month in range(1, 61):
    rows.append([
        month,
        round(500 * (0.95 ** month) + random.gauss(0, 10), 2),
        round(sum([500 * (0.95 ** m) * 30 for m in range(1, month+1)]), 2)
    ])
write_csv('production_decline.csv', ['month', 'oil_production_bopd', 'cumulative_oil_bbl'], rows)

# 4. Sweet Spot Detection
rows = []
for i in range(1, 201):
    rows.append([
        f'WELL_{i:03d}',
        round(random.uniform(29.0, 30.0), 4),
        round(random.uniform(-95.0, -94.0), 4),
        random.randint(5000, 12000),
        round(random.uniform(0.05, 0.25), 3),
        round(random.uniform(0.1, 500), 2),
        round(random.uniform(10, 100), 1),
        round(random.uniform(100, 2000), 2),
        1 if random.random() < 0.2 else 0
    ])
write_csv('well_attributes.csv', ['well_id', 'latitude', 'longitude', 'depth_ft', 'porosity', 
                                  'permeability_md', 'net_pay_ft', 'initial_production_bopd', 'is_sweet_spot'], rows)

# 5. Reservoir Rock Typing
rows = []
for i in range(1, 501):
    rows.append([
        f'CORE_{i:04d}',
        round(random.uniform(0.05, 0.30), 3),
        round(random.uniform(0.01, 1000), 2),
        round(random.uniform(2.60, 2.75), 3),
        round(random.uniform(0.10, 0.50), 3),
        round(random.uniform(0, 0.40), 3),
        round(random.uniform(0, 0.20), 3)
    ])
write_csv('core_data.csv', ['sample_id', 'porosity', 'permeability_md', 'grain_density', 
                           'water_saturation', 'clay_content', 'cement_content'], rows)

# 6. Equipment Failure Prediction
dates = [(datetime(2023, 1, 1) + timedelta(hours=i)).strftime('%Y-%m-%d %H:00:00') for i in range(8760)]
rows = []
for date in dates:
    rows.append([
        date,
        round(150 + random.gauss(0, 10), 1),
        round(500 + random.gauss(0, 50), 1),
        round(random.uniform(0, 10), 2),
        round(100 + random.gauss(0, 20), 1),
        1 if random.random() < 0.01 else 0
    ])
write_csv('sensor_readings.csv', ['timestamp', 'temperature_f', 'pressure_psi', 'vibration_mm_s', 
                                  'flow_rate_gpm', 'failure'], rows[:1000])  # Limit to 1000 rows

# 7. Pipeline Integrity Monitoring
dates = [(datetime(2023, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(365)]
rows = []
for date in dates:
    rows.append([
        date,
        round(800 + random.gauss(0, 50), 1),
        round(50000 + random.gauss(0, 5000), 1),
        round(120 + random.gauss(0, 10), 1),
        round(random.uniform(0, 5), 2),
        round(random.uniform(70, 100), 1),
        1 if random.random() < 0.05 else 0
    ])
write_csv('pipeline_data.csv', ['date', 'pressure_psi', 'flow_rate_bpd', 'temperature_f', 
                                'corrosion_rate_mpy', 'inspection_score', 'integrity_issue'], rows)

# 8. Facility Maintenance
rows = []
for i in range(1, 301):
    rows.append([
        f'EQ_{i:04d}',
        round(random.uniform(0, 20), 1),
        round(random.uniform(0, 50000), 1),
        round(random.uniform(100, 200), 1),
        round(random.uniform(100, 500), 1),
        round(random.uniform(0, 15), 2),
        random.randint(0, 50),
        round(random.uniform(0, 365), 1),
        round(random.uniform(30, 180), 1)
    ])
write_csv('facility_maintenance.csv', ['equipment_id', 'age_years', 'operating_hours', 'temperature_avg', 
                                      'pressure_avg', 'vibration_avg', 'maintenance_count', 
                                      'days_since_maintenance', 'maintenance_needed_days'], rows)

# 9. Logs and Core
rows = []
for depth in range(5000, 10000, 1):
    rows.append([
        depth,
        round(20 + random.gauss(0, 10), 2),
        round(2.0 + random.gauss(0, 0.3), 3),
        round(0.2 + random.gauss(0, 0.1), 3),
        round(60 + random.gauss(0, 10), 2),
        round(random.uniform(0.05, 0.30), 3),
        round(random.uniform(0.1, 500), 2),
        round(random.uniform(0.10, 0.50), 3)
    ])
write_csv('logs_and_core.csv', ['depth', 'GR', 'RHOB', 'NPHI', 'DT', 'porosity_core', 
                                'permeability_core', 'water_saturation_core'], rows)

# 10. Water Cut Prediction
rows = []
for i in range(1, 101):
    oil = random.uniform(50, 1000)
    water = random.uniform(10, 500)
    rows.append([
        f'WELL_{i:03d}',
        random.randint(100, 5000),
        round(oil, 2),
        round(water, 2),
        round(random.uniform(100, 2000), 2),
        round(random.uniform(500, 3000), 1),
        round((water / (oil + water)) * 100, 2)
    ])
write_csv('production_water_cut.csv', ['well_id', 'age_days', 'oil_rate_bopd', 'water_rate_bwpd', 
                                      'gas_rate_mcfd', 'pressure_psi', 'water_cut_percent'], rows)

# 11. Gas Lift Optimization
rows = []
for i in range(1, 51):
    gas_inj = random.uniform(100, 2000)
    rows.append([
        f'WELL_{i:03d}',
        round(gas_inj, 1),
        round(200 + gas_inj * 0.3 + random.gauss(0, 50), 2),
        round(random.uniform(50, 300), 2),
        round(random.uniform(200, 800), 1),
        random.randint(5000, 10000)
    ])
write_csv('gas_lift_data.csv', ['well_id', 'gas_injection_rate_mcfd', 'oil_production_bopd', 
                                'water_production_bwpd', 'wellhead_pressure_psi', 'depth_ft'], rows)

# 12. Drilling Logs
rows = []
for depth in range(0, 10000, 10):
    rows.append([
        depth,
        round(random.uniform(10, 100), 1),
        round(random.uniform(5, 50), 1),
        round(random.uniform(50, 200), 1),
        round(random.uniform(200, 600), 1),
        random.choice(['Shale', 'Sandstone', 'Limestone', 'Dolomite'])
    ])
write_csv('drilling_logs.csv', ['depth_ft', 'ROP_ft_hr', 'WOB_klbs', 'RPM', 'flow_rate_gpm', 'formation'], rows)

# 13. Corrosion Data
rows = []
for i in range(1, 201):
    temp = random.uniform(80, 200)
    co2 = random.uniform(0, 5)
    rows.append([
        f'INS_{i:04d}',
        f'LOC_{i:02d}',
        round(temp, 1),
        round(random.uniform(100, 1000), 1),
        round(random.uniform(5.5, 8.5), 2),
        round(co2, 2),
        round(random.uniform(0, 100), 1),
        round(random.uniform(1, 20), 1),
        round(0.5 + temp * 0.01 + co2 * 0.2 + random.gauss(0, 0.3), 2)
    ])
write_csv('corrosion_data.csv', ['inspection_id', 'location', 'temperature_f', 'pressure_psi', 'ph', 
                                 'co2_content_mole', 'h2s_content_ppm', 'flow_velocity_ft_s', 'corrosion_rate_mpy'], rows)

# 14. Flow Assurance
rows = []
for i in range(1, 301):
    rows.append([
        f'SAMP_{i:04d}',
        round(random.uniform(500, 3000), 1),
        round(random.uniform(40, 150), 1),
        round(random.uniform(100, 5000), 1),
        round(random.uniform(0, 50), 2),
        round(random.uniform(0, 10), 2),
        round(random.uniform(0, 5), 2),
        random.choice(['None', 'Hydrate', 'Wax', 'Asphaltene'])
    ])
write_csv('flow_assurance.csv', ['sample_id', 'pressure_psi', 'temperature_f', 'gas_oil_ratio', 
                                 'water_cut_percent', 'wax_content_wt', 'asphaltene_content_wt', 'risk_type'], rows)

# 15. PVT Data
rows = []
for i in range(1, 151):
    rows.append([
        f'PVT_{i:04d}',
        round(random.uniform(500, 5000), 1),
        round(random.uniform(100, 250), 1),
        round(random.uniform(0.6, 1.2), 3),
        round(random.uniform(20, 50), 1),
        round(random.uniform(1000, 4000), 1),
        round(random.uniform(1.0, 2.5), 3),
        round(random.uniform(0.5, 50), 2)
    ])
write_csv('pvt_data.csv', ['sample_id', 'pressure_psi', 'temperature_f', 'gas_gravity', 
                           'oil_gravity_api', 'bubble_point_psi', 'formation_volume_factor', 'viscosity_cp'], rows)

# 16. Well Lift Data
rows = []
for i in range(1, 101):
    rows.append([
        f'WELL_{i:03d}',
        random.randint(3000, 12000),
        round(random.uniform(50, 1500), 2),
        round(random.uniform(0, 80), 2),
        round(random.uniform(100, 5000), 1),
        round(random.uniform(1000, 5000), 1),
        random.choice(['Natural', 'ESP', 'Gas Lift', 'Rod Pump', 'None'])
    ])
write_csv('well_lift_data.csv', ['well_id', 'depth_ft', 'production_rate_bopd', 'water_cut_percent', 
                                 'gas_liquid_ratio', 'reservoir_pressure_psi', 'current_lift_type'], rows)

# 17. Commingled Production
dates = [(datetime(2023, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(365)]
rows = []
for date in dates:
    total_oil = 5000 + random.gauss(0, 500)
    zone_a = random.uniform(0.2, 0.4)
    zone_b = random.uniform(0.3, 0.5)
    zone_c = 1 - zone_a - zone_b
    rows.append([
        date,
        round(total_oil, 2),
        round(10000 + random.gauss(0, 1000), 2),
        round(zone_a, 3),
        round(zone_b, 3),
        round(zone_c, 3),
        round(total_oil * zone_a + random.gauss(0, 50), 2),
        round(total_oil * zone_b + random.gauss(0, 50), 2),
        round(total_oil * zone_c + random.gauss(0, 50), 2)
    ])
write_csv('commingled_production.csv', ['date', 'total_oil_bopd', 'total_gas_mcfd', 'zone_a_contribution', 
                                       'zone_b_contribution', 'zone_c_contribution', 'test_zone_a_bopd', 
                                       'test_zone_b_bopd', 'test_zone_c_bopd'], rows)

# ============================================================================
# HEALTHCARE SCENARIOS
# ============================================================================

# 1. Disease Risk Prediction
rows = []
for i in range(1, 1001):
    rows.append([
        f'PAT_{i:05d}',
        random.randint(18, 90),
        random.choice(['M', 'F']),
        round(random.uniform(18, 40), 1),
        random.randint(90, 180),
        random.randint(60, 120),
        random.randint(120, 300),
        random.randint(70, 200),
        random.choice([0, 1]),
        1 if random.random() < 0.25 else 0
    ])
write_csv('patient_data.csv', ['patient_id', 'age', 'gender', 'bmi', 'blood_pressure_systolic', 
                               'blood_pressure_diastolic', 'cholesterol', 'glucose', 'smoker', 'disease_risk'], rows)

# 2. Hospital Readmission
rows = []
for i in range(1, 2001):
    rows.append([
        f'ADM_{i:05d}',
        f'PAT_{i:05d}',
        random.randint(18, 90),
        random.randint(1, 30),
        random.randint(0, 10),
        random.randint(0, 20),
        random.randint(0, 5),
        random.randint(0, 5),
        random.choice([0, 1]),
        1 if random.random() < 0.15 else 0
    ])
write_csv('admissions.csv', ['admission_id', 'patient_id', 'age', 'length_of_stay', 'num_procedures', 
                            'num_medications', 'num_emergency', 'num_inpatient', 'diabetes', 'readmitted'], rows)

# 3. Patient Segmentation
rows = []
for i in range(1, 801):
    rows.append([
        f'PAT_{i:05d}',
        random.randint(18, 85),
        round(random.uniform(18, 40), 1),
        round(random.uniform(0, 10), 1),
        random.randint(0, 10),
        random.randint(0, 5),
        random.randint(0, 10),
        random.choice(['Private', 'Medicare', 'Medicaid', 'None'])
    ])
write_csv('patient_profiles.csv', ['patient_id', 'age', 'bmi', 'exercise_hours_week', 'medication_count', 
                                   'chronic_conditions', 'hospital_visits_year', 'insurance_type'], rows)

# 4. Treatment Outcome Prediction
rows = []
for i in range(1, 801):
    rows.append([
        f'PAT_{i:05d}',
        random.choice(['Treatment_A', 'Treatment_B', 'Treatment_C']),
        random.randint(18, 80),
        random.choice([0, 1]),
        round(random.uniform(0, 100), 1),
        random.choice(['Excellent', 'Good', 'Fair', 'Poor'])
    ])
write_csv('treatment_outcomes.csv', ['patient_id', 'treatment', 'age', 'comorbidity', 'baseline_score', 
                                     'outcome'], rows)

# 5. Length of Stay
rows = []
for i in range(1, 1501):
    rows.append([
        f'ADM_{i:05d}',
        random.randint(18, 90),
        random.choice(['Emergency', 'Elective', 'Urgent']),
        random.choice(['Cardiac', 'Respiratory', 'Surgical', 'Medical']),
        random.randint(0, 10),
        random.randint(1, 30)
    ])
write_csv('length_of_stay.csv', ['admission_id', 'age', 'admission_type', 'diagnosis_category', 
                                 'procedures_count', 'length_of_stay_days'], rows)

# 6. Medication Adherence
rows = []
for i in range(1, 1001):
    rows.append([
        f'PAT_{i:05d}',
        random.choice(['Hypertension', 'Diabetes', 'Cholesterol', 'Other']),
        random.randint(1, 30),
        round(random.uniform(0.3, 1.0), 2),
        round(random.uniform(0, 100), 1),
        1 if random.random() < 0.3 else 0
    ])
write_csv('medication_adherence.csv', ['patient_id', 'medication_type', 'prescribed_days', 'adherence_rate', 
                                       'missed_doses', 'poor_adherence'], rows)

# 7. Disease Progression
rows = []
for i in range(1, 401):
    rows.append([
        f'PAT_{i:05d}',
        random.randint(0, 60),
        round(random.uniform(0, 100), 1),
        round(random.uniform(0, 100), 1),
        random.choice(['Stage_1', 'Stage_2', 'Stage_3', 'Stage_4'])
    ])
write_csv('disease_progression.csv', ['patient_id', 'months_since_diagnosis', 'baseline_severity', 
                                     'current_severity', 'progression_stage'], rows)

# 8. Lab Result Interpretation
rows = []
for i in range(1, 1001):
    rows.append([
        f'LAB_{i:05d}',
        random.choice(['Glucose', 'Cholesterol', 'Hemoglobin', 'Creatinine', 'ALT']),
        round(random.uniform(50, 200), 1),
        round(random.uniform(70, 100), 1),
        round(random.uniform(100, 120), 1),
        random.choice(['Normal', 'Abnormal', 'Critical'])
    ])
write_csv('lab_results.csv', ['lab_id', 'test_type', 'result_value', 'normal_low', 'normal_high', 
                              'interpretation'], rows)

# 9. Drug Effectiveness
rows = []
for i in range(1, 601):
    rows.append([
        f'PAT_{i:05d}',
        random.choice(['Drug_A', 'Drug_B', 'Drug_C']),
        random.randint(18, 80),
        random.choice(['M', 'F']),
        round(random.uniform(0, 100), 1),
        round(random.uniform(20, 100), 1)
    ])
write_csv('drug_effectiveness.csv', ['patient_id', 'drug', 'age', 'gender', 'baseline_symptom_score', 
                                     'post_treatment_score'], rows)

# 10. Mortality Risk
rows = []
for i in range(1, 801):
    rows.append([
        f'PAT_{i:05d}',
        random.randint(18, 90),
        round(random.uniform(90, 180), 1),
        round(random.uniform(60, 120), 1),
        round(random.uniform(95, 100), 1),
        random.randint(0, 5),
        1 if random.random() < 0.15 else 0
    ])
write_csv('mortality_risk.csv', ['patient_id', 'age', 'systolic_bp', 'diastolic_bp', 'oxygen_saturation', 
                                 'comorbidity_count', 'mortality'], rows)

# 11. Surgical Outcomes
rows = []
for i in range(1, 501):
    rows.append([
        f'SURG_{i:05d}',
        random.choice(['Cardiac', 'Orthopedic', 'General', 'Neurological']),
        random.randint(18, 85),
        random.choice(['Elective', 'Emergency', 'Urgent']),
        random.randint(30, 480),
        random.choice(['Success', 'Complication', 'Failure'])
    ])
write_csv('surgical_outcomes.csv', ['surgery_id', 'surgery_type', 'age', 'urgency', 'duration_minutes', 
                                    'outcome'], rows)

# 12. Epidemic Prediction
dates = [(datetime(2023, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(365)]
locations = [f'LOC_{i:02d}' for i in range(1, 21)]
rows = []
for date in dates:
    for loc in locations:
        rows.append([
            date,
            loc,
            random.randint(0, 100),
            round(random.uniform(1000, 10000), 0),
            round(random.uniform(0.1, 0.5), 2),
            round(random.uniform(0, 1), 2)
        ])
write_csv('epidemic_data.csv', ['date', 'location', 'cases', 'population', 'travel_index', 'outbreak_risk'], rows[:1000])  # Limit rows

# ============================================================================
# MANUFACTURING SCENARIOS
# ============================================================================

# 1. Quality Prediction
rows = []
for i in range(1, 1001):
    temp = random.uniform(150, 250)
    quality = 70 + (temp - 200) * 0.1 + random.gauss(0, 5)
    rows.append([
        f'BATCH_{i:05d}',
        round(temp, 1),
        round(random.uniform(10, 50), 1),
        round(random.uniform(100, 500), 1),
        random.choice(['A', 'B', 'C']),
        random.choice(['Day', 'Night']),
        round(quality, 1),
        1 if quality > 75 else 0
    ])
write_csv('process_data.csv', ['batch_id', 'temperature_c', 'pressure_psi', 'speed_rpm', 
                              'material_batch', 'operator_shift', 'quality_score', 'passed'], rows)

# 2. Predictive Maintenance
dates = [(datetime(2023, 1, 1) + timedelta(hours=i)).strftime('%Y-%m-%d %H:00:00') for i in range(1000)]
machines = [f'MACH_{i:03d}' for i in range(1, 21)]
rows = []
for date in dates:
    rows.append([
        date,
        random.choice(machines),
        round(random.uniform(50, 100), 1),
        round(random.uniform(0, 20), 2),
        round(random.uniform(50, 150), 1),
        round(random.uniform(10, 50), 1),
        1 if random.random() < 0.005 else 0
    ])
write_csv('sensor_data.csv', ['timestamp', 'machine_id', 'temperature_c', 'vibration_mm_s', 
                             'pressure_psi', 'power_consumption_kw', 'failure'], rows)

# 3. Process Optimization
rows = []
for i in range(1, 501):
    temp = random.uniform(150, 250)
    catalyst = random.uniform(10, 50)
    yield_pct = 60 + (temp - 200) * 0.2 + (catalyst - 30) * 0.5 + random.gauss(0, 5)
    grade = 'High' if yield_pct > 85 else 'Medium' if yield_pct > 70 else 'Low'
    rows.append([
        f'RUN_{i:05d}',
        round(temp, 1),
        round(random.uniform(10, 50), 1),
        round(catalyst, 1),
        round(random.uniform(30, 120), 1),
        round(yield_pct, 1),
        grade
    ])
write_csv('process_history.csv', ['run_id', 'temperature_c', 'pressure_psi', 'catalyst_amount_kg', 
                                  'reaction_time_min', 'yield_percent', 'quality_grade'], rows)

# 4. Defect Classification
rows = []
for i in range(1, 1001):
    rows.append([
        f'PROD_{i:05d}',
        round(random.uniform(0, 100), 1),
        round(random.uniform(0, 50), 1),
        round(random.uniform(0, 30), 1),
        random.choice(['Crack', 'Scratch', 'Dent', 'Discoloration', 'None'])
    ])
write_csv('defect_data.csv', ['product_id', 'defect_size_mm', 'defect_depth_mm', 'defect_count', 
                              'defect_type'], rows)

# 5. Supply Chain Optimization
rows = []
for i in range(1, 501):
    rows.append([
        f'ITEM_{i:03d}',
        round(random.uniform(10, 1000), 0),
        round(random.uniform(1, 30), 1),
        round(random.uniform(5, 50), 2),
        round(random.uniform(0.1, 0.3), 2),
        random.choice(['Supplier_A', 'Supplier_B', 'Supplier_C'])
    ])
write_csv('supply_chain.csv', ['item_id', 'demand_units', 'lead_time_days', 'unit_cost', 'holding_cost_rate', 
                               'supplier'], rows)

# 6. Energy Consumption
dates = [(datetime(2023, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(365)]
rows = []
for date in dates:
    rows.append([
        date,
        round(random.uniform(1000, 5000), 1),
        round(random.uniform(100, 1000), 0),
        round(random.uniform(15, 35), 1),
        round(random.uniform(0.8, 1.2), 2)
    ])
write_csv('energy_consumption.csv', ['date', 'energy_kwh', 'production_units', 'temperature_c', 
                                     'efficiency_factor'], rows)

# 7. Production Scheduling
rows = []
for i in range(1, 201):
    rows.append([
        f'ORDER_{i:04d}',
        random.choice(['Product_A', 'Product_B', 'Product_C']),
        random.randint(100, 1000),
        random.randint(1, 30),
        random.randint(60, 480),
        random.choice(['Machine_1', 'Machine_2', 'Machine_3'])
    ])
write_csv('production_schedule.csv', ['order_id', 'product', 'quantity', 'due_date_days', 
                                      'processing_time_min', 'machine'], rows)

# 8. Inventory Optimization
rows = []
for i in range(1, 301):
    rows.append([
        f'ITEM_{i:03d}',
        round(random.uniform(10, 1000), 0),
        round(random.uniform(1, 30), 1),
        round(random.uniform(5, 50), 2),
        round(random.uniform(0.1, 0.3), 2),
        round(random.uniform(50, 500), 0)
    ])
write_csv('inventory_data.csv', ['item_id', 'avg_demand', 'lead_time_days', 'unit_cost', 
                                 'holding_cost_rate', 'current_stock'], rows)

# 9. Supplier Quality
rows = []
for i in range(1, 201):
    rows.append([
        f'SUP_{i:03d}',
        round(random.uniform(0.95, 1.0), 3),
        round(random.uniform(0, 5), 1),
        round(random.uniform(1, 10), 1),
        round(random.uniform(0.7, 1.0), 2),
        round(random.uniform(70, 100), 1)
    ])
write_csv('supplier_quality.csv', ['supplier_id', 'quality_rate', 'defect_rate_percent', 
                                   'delivery_delay_days', 'on_time_delivery', 'quality_score'], rows)

# 10. Waste Reduction
rows = []
for i in range(1, 401):
    rows.append([
        f'BATCH_{i:04d}',
        round(random.uniform(100, 1000), 0),
        round(random.uniform(0, 50), 1),
        round(random.uniform(0, 20), 1),
        round(random.uniform(0, 100), 1),
        random.choice(['Material', 'Time', 'Energy', 'Other'])
    ])
write_csv('waste_data.csv', ['batch_id', 'production_volume', 'material_waste_kg', 'time_waste_hours', 
                            'energy_waste_kwh', 'waste_type'], rows)

# 11. Equipment Efficiency
rows = []
for i in range(1, 201):
    rows.append([
        f'EQ_{i:03d}',
        round(random.uniform(0.6, 1.0), 3),
        round(random.uniform(0.7, 1.0), 3),
        round(random.uniform(0.8, 1.0), 3),
        round(random.uniform(0.5, 0.95), 3),
        round(random.uniform(0.4, 0.9), 3)
    ])
write_csv('equipment_efficiency.csv', ['equipment_id', 'availability', 'performance', 'quality_rate', 
                                       'oee', 'predicted_efficiency'], rows)

# 12. Quality Control
dates = [(datetime(2023, 1, 1) + timedelta(hours=i)).strftime('%Y-%m-%d %H:00:00') for i in range(1000)]
rows = []
for date in dates:
    rows.append([
        date,
        round(100 + random.gauss(0, 5), 2),
        round(random.uniform(95, 105), 2),
        round(random.uniform(100, 110), 2),
        1 if random.random() < 0.05 else 0
    ])
write_csv('quality_control.csv', ['timestamp', 'measurement', 'upper_limit', 'lower_limit', 'out_of_control'], rows)

# 13. Demand Forecasting
dates = [(datetime(2020, 1, 1) + timedelta(days=i)).strftime('%Y-%m-%d') for i in range(1461)]
rows = []
base_demand = 100
for i, date in enumerate(dates):
    base_demand += random.gauss(0, 5)
    rows.append([
        date,
        round(max(0, base_demand), 0),
        round(random.uniform(0.8, 1.2), 2),
        round(random.uniform(0, 1), 2)
    ])
write_csv('demand_forecast.csv', ['date', 'demand', 'seasonality', 'trend'], rows)

print(f"\n[SUCCESS] All sample data files generated successfully in {output_dir}")
print(f"Total files created: {len(list(output_dir.glob('*.csv')))}")

