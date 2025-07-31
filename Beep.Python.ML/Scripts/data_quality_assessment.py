# Data quality assessment and reporting
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns

# Parameters
output_file = '{output_file}'
generate_plots = {generate_plots}

try:
    # Determine which data to assess
    if 'data' in globals():
        assess_data = data
        data_name = 'data'
    elif 'train_data' in globals():
        assess_data = train_data  
        data_name = 'train_data'
    else:
        print("Error: No data available for quality assessment")
        data_quality_successful = False
        globals()['data_quality_successful'] = data_quality_successful
        exit()
    
    print(f"Performing data quality assessment on: {data_name}")
    print(f"Dataset shape: {assess_data.shape}")
    
    # Initialize quality report
    quality_report = {
        'dataset_info': {
            'name': data_name,
            'shape': assess_data.shape,
            'memory_usage': assess_data.memory_usage(deep=True).sum(),
            'columns': assess_data.columns.tolist()
        },
        'data_types': {},
        'missing_values': {},
        'duplicates': {},
        'outliers': {},
        'statistical_summary': {},
        'categorical_analysis': {},
        'recommendations': []
    }
    
    # 1. Data Types Analysis
    print("\n=== Data Types Analysis ===")
    dtype_counts = assess_data.dtypes.value_counts()
    for dtype, count in dtype_counts.items():
        print(f"{dtype}: {count} columns")
        quality_report['data_types'][str(dtype)] = int(count)
    
    # 2. Missing Values Analysis
    print("\n=== Missing Values Analysis ===")
    missing_values = assess_data.isnull().sum()
    missing_percentages = (missing_values / len(assess_data)) * 100
    
    missing_summary = []
    for col in assess_data.columns:
        missing_count = missing_values[col]
        missing_pct = missing_percentages[col]
        
        if missing_count > 0:
            print(f"{col}: {missing_count} ({missing_pct:.2f}%)")
            missing_summary.append({
                'column': col,
                'missing_count': int(missing_count),
                'missing_percentage': float(missing_pct)
            })
    
    if not missing_summary:
        print("No missing values found!")
    
    quality_report['missing_values'] = missing_summary
    
    # 3. Duplicate Analysis
    print("\n=== Duplicate Analysis ===")
    duplicate_count = assess_data.duplicated().sum()
    duplicate_pct = (duplicate_count / len(assess_data)) * 100
    print(f"Duplicate rows: {duplicate_count} ({duplicate_pct:.2f}%)")
    
    quality_report['duplicates'] = {
        'count': int(duplicate_count),
        'percentage': float(duplicate_pct)
    }
    
    # 4. Outlier Detection (for numerical columns)
    print("\n=== Outlier Analysis ===")
    numerical_cols = assess_data.select_dtypes(include=['number']).columns
    outlier_summary = []
    
    for col in numerical_cols:
        Q1 = assess_data[col].quantile(0.25)
        Q3 = assess_data[col].quantile(0.75)
        IQR = Q3 - Q1
        lower_bound = Q1 - 1.5 * IQR
        upper_bound = Q3 + 1.5 * IQR
        
        outliers = assess_data[(assess_data[col] < lower_bound) | (assess_data[col] > upper_bound)]
        outlier_count = len(outliers)
        outlier_pct = (outlier_count / len(assess_data)) * 100
        
        if outlier_count > 0:
            print(f"{col}: {outlier_count} outliers ({outlier_pct:.2f}%)")
            
        outlier_summary.append({
            'column': col,
            'outlier_count': int(outlier_count),
            'outlier_percentage': float(outlier_pct),
            'lower_bound': float(lower_bound),
            'upper_bound': float(upper_bound)
        })
    
    quality_report['outliers'] = outlier_summary
    
    # 5. Statistical Summary
    print("\n=== Statistical Summary ===")
    numerical_summary = {}
    
    for col in numerical_cols:
        col_stats = {
            'mean': float(assess_data[col].mean()),
            'median': float(assess_data[col].median()),
            'std': float(assess_data[col].std()),
            'min': float(assess_data[col].min()),
            'max': float(assess_data[col].max()),
            'skewness': float(assess_data[col].skew()),
            'kurtosis': float(assess_data[col].kurtosis())
        }
        numerical_summary[col] = col_stats
        
        print(f"{col}:")
        print(f"  Mean: {col_stats['mean']:.4f}, Std: {col_stats['std']:.4f}")
        print(f"  Skewness: {col_stats['skewness']:.4f}, Kurtosis: {col_stats['kurtosis']:.4f}")
    
    quality_report['statistical_summary'] = numerical_summary
    
    # 6. Categorical Analysis
    print("\n=== Categorical Analysis ===")
    categorical_cols = assess_data.select_dtypes(include=['object']).columns
    categorical_summary = {}
    
    for col in categorical_cols:
        unique_count = assess_data[col].nunique()
        most_frequent = assess_data[col].mode().iloc[0] if len(assess_data[col].mode()) > 0 else None
        most_frequent_count = assess_data[col].value_counts().iloc[0] if len(assess_data[col].value_counts()) > 0 else 0
        
        categorical_summary[col] = {
            'unique_count': int(unique_count),
            'most_frequent': str(most_frequent),
            'most_frequent_count': int(most_frequent_count),
            'cardinality_ratio': float(unique_count / len(assess_data))
        }
        
        print(f"{col}: {unique_count} unique values")
        print(f"  Most frequent: '{most_frequent}' ({most_frequent_count} times)")
    
    quality_report['categorical_analysis'] = categorical_summary
    
    # 7. Generate Recommendations
    print("\n=== Data Quality Recommendations ===")
    recommendations = []
    
    # Missing values recommendations
    high_missing = [item for item in missing_summary if item['missing_percentage'] > 50]
    if high_missing:
        rec = f"Consider dropping columns with >50% missing values: {[item['column'] for item in high_missing]}"
        recommendations.append(rec)
        print(f"• {rec}")
    
    moderate_missing = [item for item in missing_summary if 5 < item['missing_percentage'] <= 50]
    if moderate_missing:
        rec = f"Consider imputation for columns with moderate missing values: {[item['column'] for item in moderate_missing]}"
        recommendations.append(rec)
        print(f"• {rec}")
    
    # Duplicate recommendations
    if duplicate_count > 0:
        rec = f"Remove {duplicate_count} duplicate rows"
        recommendations.append(rec)
        print(f"• {rec}")
    
    # High cardinality categorical recommendations
    high_cardinality = [col for col, info in categorical_summary.items() if info['cardinality_ratio'] > 0.9]
    if high_cardinality:
        rec = f"Consider feature engineering for high cardinality columns: {high_cardinality}"
        recommendations.append(rec)
        print(f"• {rec}")
    
    # Skewed data recommendations
    highly_skewed = [col for col, info in numerical_summary.items() if abs(info['skewness']) > 2]
    if highly_skewed:
        rec = f"Consider log transformation for highly skewed columns: {highly_skewed}"
        recommendations.append(rec)
        print(f"• {rec}")
    
    quality_report['recommendations'] = recommendations
    
    # 8. Generate Plots (if requested)
    if generate_plots:
        print("\n=== Generating Quality Assessment Plots ===")
        
        # Set up plotting
        plt.style.use('default')
        fig, axes = plt.subplots(2, 2, figsize=(15, 12))
        fig.suptitle(f'Data Quality Assessment - {data_name}', fontsize=16)
        
        # Plot 1: Missing values heatmap
        if missing_summary:
            missing_data = assess_data.isnull()
            sns.heatmap(missing_data, ax=axes[0,0], cbar=True, yticklabels=False)
            axes[0,0].set_title('Missing Values Pattern')
        else:
            axes[0,0].text(0.5, 0.5, 'No Missing Values', ha='center', va='center', transform=axes[0,0].transAxes)
            axes[0,0].set_title('Missing Values Pattern')
        
        # Plot 2: Data types distribution
        dtype_counts_plot = assess_data.dtypes.value_counts()
        axes[0,1].pie(dtype_counts_plot.values, labels=dtype_counts_plot.index, autopct='%1.1f%%')
        axes[0,1].set_title('Data Types Distribution')
        
        # Plot 3: Numerical features distribution
        if len(numerical_cols) > 0:
            sample_col = numerical_cols[0]
            assess_data[sample_col].hist(ax=axes[1,0], bins=30)
            axes[1,0].set_title(f'Distribution of {sample_col}')
            axes[1,0].set_xlabel(sample_col)
        else:
            axes[1,0].text(0.5, 0.5, 'No Numerical Features', ha='center', va='center', transform=axes[1,0].transAxes)
            axes[1,0].set_title('Numerical Distribution')
        
        # Plot 4: Outliers boxplot
        if len(numerical_cols) > 0:
            sample_cols = numerical_cols[:3]  # First 3 numerical columns
            sample_data = assess_data[sample_cols]
            sample_data.boxplot(ax=axes[1,1])
            axes[1,1].set_title('Outliers Detection (Sample)')
            axes[1,1].tick_params(axis='x', rotation=45)
        else:
            axes[1,1].text(0.5, 0.5, 'No Numerical Features', ha='center', va='center', transform=axes[1,1].transAxes)
            axes[1,1].set_title('Outliers Detection')
        
        plt.tight_layout()
        plot_file = output_file.replace('.json', '_plots.png') if output_file != 'None' else 'data_quality_plots.png'
        plt.savefig(plot_file, dpi=300, bbox_inches='tight')
        plt.close()
        
        print(f"Quality assessment plots saved to: {plot_file}")
        quality_report['plots_file'] = plot_file
    
    # Store results
    globals()['quality_report'] = quality_report
    
    # Save report to file if specified
    if output_file and output_file != 'None':
        import json
        with open(output_file, 'w') as f:
            json.dump(quality_report, f, indent=2)
        print(f"Quality report saved to: {output_file}")
    
    print(f"\nData quality assessment completed successfully!")
    print(f"Overall data quality score: {len(recommendations)} issues found")
    
    data_quality_successful = True
    
except Exception as e:
    print(f"Error during data quality assessment: {str(e)}")
    data_quality_successful = False

# Store the result
globals()['data_quality_successful'] = data_quality_successful