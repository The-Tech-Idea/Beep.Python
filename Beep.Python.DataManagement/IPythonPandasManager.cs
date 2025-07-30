using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Beep.Python.DataManagement
{
    /// <summary>
    /// Interface for Python Pandas operations and DataFrame management
    /// Provides comprehensive data manipulation, analysis, and transformation capabilities
    /// </summary>
    public interface IPythonPandasManager : IDisposable
    {
        #region DataFrame Creation and I/O Operations
        
        /// <summary>
        /// Creates a new DataFrame from data
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="data">Data to create DataFrame from</param>
        void CreateDataFrame(string dataFrameName, dynamic data);
        
        /// <summary>
        /// Creates a new DataFrame asynchronously
        /// </summary>
        Task CreateDataFrameAsync(string dataFrameName, dynamic data, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Reads CSV file into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="filePath">Path to CSV file</param>
        void ReadCsv(string dataFrameName, string filePath);
        
        /// <summary>
        /// Reads Excel file into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="filePath">Path to Excel file</param>
        void ReadExcel(string dataFrameName, string filePath);
        
        /// <summary>
        /// Reads JSON file into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="filePath">Path to JSON file</param>
        void ReadJson(string dataFrameName, string filePath);
        
        /// <summary>
        /// Reads data from SQL query into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="sql">SQL query</param>
        /// <param name="connectionString">Database connection string</param>
        void ReadSql(string dataFrameName, string sql, string connectionString);
        
        /// <summary>
        /// Reads data from web URL into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="url">Web URL</param>
        void ReadFromWeb(string dataFrameName, string url);
        
        #endregion

        #region DataFrame Export Operations
        
        /// <summary>
        /// Exports DataFrame to CSV file
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <param name="filePath">Output CSV file path</param>
        void ToCsv(string dataFrameName, string filePath);
        
        /// <summary>
        /// Exports DataFrame to Excel file
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <param name="filePath">Output Excel file path</param>
        void ToExcel(string dataFrameName, string filePath);
        
        /// <summary>
        /// Exports DataFrame to JSON file
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <param name="filePath">Output JSON file path</param>
        void ToJson(string dataFrameName, string filePath);
        
        /// <summary>
        /// Generic export method with format specification
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="format">Export format</param>
        void ExportDataFrame(string dataFrameName, string filePath, string format);
        
        #endregion

        #region Data Selection and Filtering
        
        /// <summary>
        /// Selects specific columns from DataFrame
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="columns">Columns to select</param>
        void SelectColumns(string dataFrameName, string newFrameName, string[] columns);
        
        /// <summary>
        /// Filters rows based on condition
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="condition">Filter condition</param>
        void FilterRows(string dataFrameName, string newFrameName, string condition);
        
        /// <summary>
        /// Advanced filtering with query evaluation
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="query">Query string</param>
        void QueryDataFrame(string dataFrameName, string newFrameName, string query);
        
        /// <summary>
        /// Sample DataFrame randomly
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="fraction">Fraction to sample</param>
        void SampleDataFrame(string dataFrameName, string newFrameName, double fraction);
        
        #endregion

        #region Data Manipulation Operations
        
        /// <summary>
        /// Adds a new column to DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">New column name</param>
        /// <param name="columnData">Column data or expression</param>
        void AddColumn(string dataFrameName, string columnName, dynamic columnData);
        
        /// <summary>
        /// Drops a column from DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column to drop</param>
        void DropColumn(string dataFrameName, string columnName);
        
        /// <summary>
        /// Renames columns in DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="newColumnNames">Mapping of old to new column names</param>
        void RenameColumns(string dataFrameName, Dictionary<string, string> newColumnNames);
        
        /// <summary>
        /// Applies a function to a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <param name="func">Function to apply</param>
        void ApplyFunction(string dataFrameName, string columnName, string func);
        
        /// <summary>
        /// Converts data type of a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <param name="newType">New data type</param>
        void ConvertDataType(string dataFrameName, string columnName, string newType);
        
        #endregion

        #region Data Cleaning Operations
        
        /// <summary>
        /// Handles missing values by dropping rows with NaN
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        void DropNA(string dataFrameName, string newFrameName);
        
        /// <summary>
        /// Fills missing values with specified value
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="value">Value to fill with</param>
        void FillNA(string dataFrameName, string newFrameName, dynamic value);
        
        /// <summary>
        /// Removes duplicate rows
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        void DropDuplicates(string dataFrameName, string newFrameName);
        
        /// <summary>
        /// Optimizes memory usage of DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        void OptimizeMemory(string dataFrameName);
        
        #endregion

        #region Aggregation and Grouping
        
        /// <summary>
        /// Groups DataFrame by column and applies aggregation
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="groupByColumn">Column to group by</param>
        /// <param name="aggFunc">Aggregation function</param>
        void GroupBy(string dataFrameName, string newFrameName, string groupByColumn, string aggFunc);
        
        /// <summary>
        /// Creates pivot table
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="index">Index column</param>
        /// <param name="columns">Columns for pivot</param>
        /// <param name="values">Values column</param>
        void PivotTable(string dataFrameName, string newFrameName, string index, string columns, string values);
        
        /// <summary>
        /// Cross tabulation of two columns
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="index">Index column</param>
        /// <param name="columns">Columns for cross-tab</param>
        void CrossTab(string dataFrameName, string newFrameName, string index, string columns);
        
        #endregion

        #region Statistical Analysis
        
        /// <summary>
        /// Gets descriptive statistics of DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <returns>Descriptive statistics as string</returns>
        string Describe(string dataFrameName);
        
        /// <summary>
        /// Calculates correlation matrix
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <returns>Correlation matrix as string</returns>
        string Correlation(string dataFrameName);
        
        /// <summary>
        /// Calculates mean of a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Mean value as string</returns>
        string CalculateMean(string dataFrameName, string columnName);
        
        /// <summary>
        /// Calculates median of a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Median value as string</returns>
        string CalculateMedian(string dataFrameName, string columnName);
        
        /// <summary>
        /// Gets unique values in a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Unique values as string</returns>
        string UniqueValues(string dataFrameName, string columnName);
        
        /// <summary>
        /// Gets value counts for a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Value counts as string</returns>
        string ValueCounts(string dataFrameName, string columnName);
        
        #endregion

        #region Time Series Operations
        
        /// <summary>
        /// Converts column to datetime
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        void ConvertToDateTime(string dataFrameName, string columnName);
        
        /// <summary>
        /// Resamples time series data
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="rule">Resampling rule</param>
        /// <param name="aggFunc">Aggregation function</param>
        void Resample(string dataFrameName, string newFrameName, string rule, string aggFunc);
        
        /// <summary>
        /// Creates rolling window statistics
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="windowSize">Window size</param>
        /// <param name="operation">Operation to perform</param>
        void RollingWindow(string dataFrameName, string newFrameName, int windowSize, string operation);
        
        #endregion

        #region Data Merging and Joining
        
        /// <summary>
        /// Merges two DataFrames
        /// </summary>
        /// <param name="leftFrameName">Left DataFrame name</param>
        /// <param name="rightFrameName">Right DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="onColumn">Column to join on</param>
        void MergeDataFrames(string leftFrameName, string rightFrameName, string newFrameName, string onColumn);
        
        /// <summary>
        /// Concatenates multiple DataFrames
        /// </summary>
        /// <param name="dataFrameNames">Array of DataFrame names</param>
        /// <param name="newFrameName">New DataFrame name</param>
        void ConcatDataFrames(string[] dataFrameNames, string newFrameName);
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Gets DataFrame as JSON string
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <returns>JSON representation</returns>
        string GetDataFrameAsJson(string dataFrameName);
        
        /// <summary>
        /// Checks for null values in DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <returns>Null value information</returns>
        string IsNull(string dataFrameName);
        
        /// <summary>
        /// Resets DataFrame index
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        void ResetIndex(string dataFrameName);
        
        /// <summary>
        /// Sets pandas option
        /// </summary>
        /// <param name="optionName">Option name</param>
        /// <param name="value">Option value</param>
        void SetPandasOption(string optionName, string value);
        
        #endregion

        #region Async Operations
        
        /// <summary>
        /// Reads CSV file asynchronously
        /// </summary>
        Task ReadCsvAsync(string dataFrameName, string filePath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Exports DataFrame to CSV asynchronously
        /// </summary>
        Task ToCsvAsync(string dataFrameName, string filePath, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Performs groupby operation asynchronously
        /// </summary>
        Task GroupByAsync(string dataFrameName, string newFrameName, string groupByColumn, string aggFunc, CancellationToken cancellationToken = default);
        
        #endregion
    }
}