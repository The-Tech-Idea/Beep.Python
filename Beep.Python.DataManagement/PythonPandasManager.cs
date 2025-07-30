using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beep.Python.Model;
using Python.Runtime;
using TheTechIdea.Beep.Container.Services;

namespace Beep.Python.DataManagement
{
    /// <summary>
    /// Enterprise PythonPandasManager with proper session management, virtual environment support,
    /// error handling, and async operations for comprehensive pandas DataFrame operations
    /// </summary>
    public class PythonPandasManager : IPythonPandasManager
    {
        #region Private Fields
        private readonly object _operationLock = new object();
        private volatile bool _isDisposed = false;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, DateTime> _dataFrameLastAccess = new Dictionary<string, DateTime>();
        
        // Python runtime dependencies
        private readonly IPythonRunTimeManager _pythonRunTimeManager;
        private readonly IPythonCodeExecuteManager _executeManager;
        private readonly IBeepService _beepService;
        
        // Session and Environment management
        private PythonSessionInfo? _configuredSession;
        private PythonVirtualEnvironment? _configuredVirtualEnvironment;
        private PyModule? _sessionScope;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of PythonPandasManager with proper session and environment management
        /// </summary>
        /// <param name="beepservice">BEEP service for application integration</param>
        /// <param name="pythonRuntimeManager">Python runtime manager for execution</param>
        /// <param name="executeManager">Python code execution manager</param>
        public PythonPandasManager(
            IBeepService beepservice, 
            IPythonRunTimeManager pythonRuntimeManager, 
            IPythonCodeExecuteManager executeManager)
        {
            _beepService = beepservice ?? throw new ArgumentNullException(nameof(beepservice));
            _pythonRunTimeManager = pythonRuntimeManager ?? throw new ArgumentNullException(nameof(pythonRuntimeManager));
            _executeManager = executeManager ?? throw new ArgumentNullException(nameof(executeManager));
            _cancellationTokenSource = new CancellationTokenSource();
        }
        #endregion

        #region Session and Environment Configuration
        /// <summary>
        /// Configure the pandas manager to use a specific Python session and virtual environment
        /// This is the recommended approach for multi-user environments
        /// </summary>
        /// <param name="session">Pre-existing Python session to use for execution</param>
        /// <param name="virtualEnvironment">Virtual environment associated with the session</param>
        /// <returns>True if configuration successful</returns>
        public bool ConfigureSession(PythonSessionInfo session, PythonVirtualEnvironment virtualEnvironment)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            
            if (virtualEnvironment == null)
                throw new ArgumentNullException(nameof(virtualEnvironment));

            // Validate that session is associated with the environment
            if (session.VirtualEnvironmentId != virtualEnvironment.ID)
            {
                throw new ArgumentException("Session must be associated with the provided virtual environment");
            }

            // Validate session is active
            if (session.Status != PythonSessionStatus.Active)
            {
                throw new ArgumentException("Session must be in Active status");
            }

            _configuredSession = session;
            _configuredVirtualEnvironment = virtualEnvironment;
            
            // Get or create the session scope
            if (_pythonRunTimeManager.HasScope(session))
            {
                _sessionScope = _pythonRunTimeManager.GetScope(session);
            }
            else
            {
                if (_pythonRunTimeManager.CreateScope(session, virtualEnvironment))
                {
                    _sessionScope = _pythonRunTimeManager.GetScope(session);
                }
                else
                {
                    throw new InvalidOperationException("Failed to create Python scope for session");
                }
            }

            // Initialize pandas environment for this session
            InitializePandasEnvironment();
            
            return true;
        }

        /// <summary>
        /// Configure session using username and optional environment ID
        /// This method will create or reuse a session for the specified user
        /// </summary>
        /// <param name="username">Username for session creation</param>
        /// <param name="environmentId">Specific environment ID, or null for auto-selection</param>
        /// <returns>True if configuration successful</returns>
        public bool ConfigureSessionForUser(string username, string? environmentId = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            if (_pythonRunTimeManager.SessionManager == null)
                throw new InvalidOperationException("Session manager is not available");

            // Create or get existing session for the user
            var session = _pythonRunTimeManager.SessionManager.CreateSession(username, environmentId);
            if (session == null)
            {
                throw new InvalidOperationException($"Failed to create session for user: {username}");
            }

            // Get the virtual environment for this session
            var virtualEnvironment = _pythonRunTimeManager.VirtualEnvmanager?.GetEnvironmentById(session.VirtualEnvironmentId);
            if (virtualEnvironment == null)
            {
                throw new InvalidOperationException($"Virtual environment not found for session: {session.SessionId}");
            }

            return ConfigureSession(session, virtualEnvironment);
        }

        /// <summary>
        /// Get the currently configured session, if any
        /// </summary>
        /// <returns>The configured Python session, or null if not configured</returns>
        public PythonSessionInfo? GetConfiguredSession()
        {
            return _configuredSession;
        }

        /// <summary>
        /// Get the currently configured virtual environment, if any
        /// </summary>
        /// <returns>The configured virtual environment, or null if not configured</returns>
        public PythonVirtualEnvironment? GetConfiguredVirtualEnvironment()
        {
            return _configuredVirtualEnvironment;
        }

        /// <summary>
        /// Check if session is properly configured
        /// </summary>
        /// <returns>True if session and environment are configured</returns>
        public bool IsSessionConfigured()
        {
            return _configuredSession != null && _configuredVirtualEnvironment != null && _sessionScope != null;
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the pandas environment with required imports and error handling
        /// </summary>
        private void InitializePandasEnvironment()
        {
            if (!IsSessionConfigured())
                throw new InvalidOperationException("Session must be configured before initializing pandas environment");

            try
            {
                ExecuteInSession(() =>
                {
                    // Essential pandas imports with error handling
                    string initScript = @"
import sys
import traceback
try:
    import pandas as pd
    import numpy as np
    from datetime import datetime, timedelta
    print('Pandas environment initialized successfully')
except ImportError as e:
    print(f'Import error: {e}')
    sys.exit(1)
except Exception as e:
    print(f'Initialization error: {e}')
    traceback.print_exc()
    sys.exit(1)
";
                    _sessionScope!.Exec(initScript);
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize pandas environment: {ex.Message}", ex);
            }
        }
        #endregion

        #region Core Helper Methods
        /// <summary>
        /// Executes code safely within the session context without manual GIL management
        /// </summary>
        /// <param name="action">Action to execute in session</param>
        private void ExecuteInSession(Action action)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PythonPandasManager));

            if (!IsSessionConfigured())
                throw new InvalidOperationException("Session must be configured before executing pandas operations");

            lock (_operationLock)
            {
                try
                {
                    // Let the runtime manager handle GIL management through the session scope
                    action();
                }
                catch (PythonException pythonEx)
                {
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Session execution error: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Executes code safely within the session context and returns a result without manual GIL management
        /// </summary>
        /// <typeparam name="T">Type of result to return</typeparam>
        /// <param name="func">Function to execute in session</param>
        /// <returns>Result of the function</returns>
        private T ExecuteInSession<T>(Func<T> func)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PythonPandasManager));

            if (!IsSessionConfigured())
                throw new InvalidOperationException("Session must be configured before executing pandas operations");

            lock (_operationLock)
            {
                try
                {
                    // Let the runtime manager handle GIL management through the session scope
                    return func();
                }
                catch (PythonException pythonEx)
                {
                    throw new InvalidOperationException($"Python execution error: {pythonEx.Message}", pythonEx);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Session execution error: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Execute Python code asynchronously using the configured session
        /// </summary>
        /// <param name="pythonCode">Python code to execute</param>
        /// <returns>Execution result</returns>
        private async Task<(bool Success, string? Result, string? ErrorMessage)> ExecutePythonCodeAsync(string pythonCode)
        {
            try
            {
                if (!IsSessionConfigured())
                    throw new InvalidOperationException("Session must be configured before executing pandas operations");

                var result = await _executeManager.ExecuteCodeAsync(pythonCode, _configuredSession!);
                return (result.Success, result.Output, result.Success ? null : result.Output);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Validates that a DataFrame name is valid and not empty
        /// </summary>
        /// <param name="dataFrameName">DataFrame name to validate</param>
        private void ValidateDataFrameName(string dataFrameName)
        {
            if (string.IsNullOrWhiteSpace(dataFrameName))
                throw new ArgumentException("DataFrame name cannot be null or empty.", nameof(dataFrameName));
            
            if (!IsValidPythonIdentifier(dataFrameName))
                throw new ArgumentException($"'{dataFrameName}' is not a valid Python identifier.", nameof(dataFrameName));
        }

        /// <summary>
        /// Validates that a file path exists and is accessible
        /// </summary>
        /// <param name="filePath">File path to validate</param>
        private void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            
            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException($"File not found: {filePath}");
        }

        /// <summary>
        /// Checks if a string is a valid Python identifier
        /// </summary>
        /// <param name="identifier">Identifier to check</param>
        /// <returns>True if valid, false otherwise</returns>
        private bool IsValidPythonIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Basic validation: must start with letter or underscore, followed by letters, digits, or underscores
            return System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        /// <summary>
        /// Updates the last access time for a DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        private void UpdateDataFrameAccess(string dataFrameName)
        {
            if (!string.IsNullOrWhiteSpace(dataFrameName))
            {
                _dataFrameLastAccess[dataFrameName] = DateTime.Now;
            }
        }

        /// <summary>
        /// Safely formats a string for Python execution by escaping special characters
        /// </summary>
        /// <param name="input">Input string to format</param>
        /// <returns>Safely formatted string</returns>
        private string SafeStringFormat(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "''";
            
            return $"'''{input.Replace("'''", "\\'\\'\\'")}'''";
        }
        #endregion

        #region DataFrame Creation and I/O Operations
        /// <summary>
        /// Creates a new DataFrame from data with proper error handling
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="data">Data to create DataFrame from</param>
        public void CreateDataFrame(string dataFrameName, dynamic data)
        {
            ValidateDataFrameName(dataFrameName);
            
            ExecuteInSession(() =>
            {
                string script = $"{dataFrameName} = pd.DataFrame({data})";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Creates a new DataFrame asynchronously
        /// </summary>
        public async Task CreateDataFrameAsync(string dataFrameName, dynamic data, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => CreateDataFrame(dataFrameName, data), cancellationToken);
        }

        /// <summary>
        /// Reads CSV file into DataFrame with comprehensive error handling
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="filePath">Path to CSV file</param>
        public void ReadCsv(string dataFrameName, string filePath)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateFilePath(filePath);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName} = pd.read_csv({SafeStringFormat(filePath)})
    print(f'Successfully loaded CSV: {{len({dataFrameName})}} rows, {{len({dataFrameName}.columns)}} columns')
except Exception as e:
    print(f'Error reading CSV: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Reads CSV file asynchronously
        /// </summary>
        public async Task ReadCsvAsync(string dataFrameName, string filePath, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => ReadCsv(dataFrameName, filePath), cancellationToken);
        }

        /// <summary>
        /// Reads Excel file into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="filePath">Path to Excel file</param>
        public void ReadExcel(string dataFrameName, string filePath)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateFilePath(filePath);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName} = pd.read_excel({SafeStringFormat(filePath)})
    print(f'Successfully loaded Excel: {{len({dataFrameName})}} rows, {{len({dataFrameName}.columns)}} columns')
except Exception as e:
    print(f'Error reading Excel: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Reads JSON file into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="filePath">Path to JSON file</param>
        public void ReadJson(string dataFrameName, string filePath)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateFilePath(filePath);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName} = pd.read_json({SafeStringFormat(filePath)})
    print(f'Successfully loaded JSON: {{len({dataFrameName})}} rows, {{len({dataFrameName}.columns)}} columns')
except Exception as e:
    print(f'Error reading JSON: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Reads data from SQL query into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="sql">SQL query</param>
        /// <param name="connectionString">Database connection string</param>
        public void ReadSql(string dataFrameName, string sql, string connectionString)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));
            
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    import sqlalchemy
    engine = sqlalchemy.create_engine({SafeStringFormat(connectionString)})
    {dataFrameName} = pd.read_sql({SafeStringFormat(sql)}, engine)
    print(f'Successfully loaded SQL data: {{len({dataFrameName})}} rows, {{len({dataFrameName}.columns)}} columns')
except Exception as e:
    print(f'Error reading SQL data: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Reads data from web URL into DataFrame
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame variable</param>
        /// <param name="url">Web URL</param>
        public void ReadFromWeb(string dataFrameName, string url)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    import requests
    response = requests.get({SafeStringFormat(url)})
    response.raise_for_status()
    {dataFrameName} = pd.read_json(response.text)
    print(f'Successfully loaded web data: {{len({dataFrameName})}} rows, {{len({dataFrameName}.columns)}} columns')
except Exception as e:
    print(f'Error reading web data: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }
        #endregion

        #region DataFrame Export Operations
        /// <summary>
        /// Exports DataFrame to CSV file with error handling
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <param name="filePath">Output CSV file path</param>
        public void ToCsv(string dataFrameName, string filePath)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}.to_csv({SafeStringFormat(filePath)}, index=False)
    print(f'Successfully exported {{len({dataFrameName})}} rows to CSV: {filePath}')
except Exception as e:
    print(f'Error exporting to CSV: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Exports DataFrame to CSV asynchronously
        /// </summary>
        public async Task ToCsvAsync(string dataFrameName, string filePath, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => ToCsv(dataFrameName, filePath), cancellationToken);
        }

        /// <summary>
        /// Exports DataFrame to Excel file
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <param name="filePath">Output Excel file path</param>
        public void ToExcel(string dataFrameName, string filePath)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}.to_excel({SafeStringFormat(filePath)}, index=False)
    print(f'Successfully exported {{len({dataFrameName})}} rows to Excel: {filePath}')
except Exception as e:
    print(f'Error exporting to Excel: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Exports DataFrame to JSON file
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <param name="filePath">Output JSON file path</param>
        public void ToJson(string dataFrameName, string filePath)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}.to_json({SafeStringFormat(filePath)}, orient='records', indent=2)
    print(f'Successfully exported {{len({dataFrameName})}} rows to JSON: {filePath}')
except Exception as e:
    print(f'Error exporting to JSON: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Generic export method with format specification
        /// </summary>
        /// <param name="dataFrameName">Name of the DataFrame</param>
        /// <param name="filePath">Output file path</param>
        /// <param name="format">Export format</param>
        public void ExportDataFrame(string dataFrameName, string filePath, string format)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            
            if (string.IsNullOrWhiteSpace(format))
                throw new ArgumentException("Format cannot be null or empty.", nameof(format));
            
            switch (format.ToLowerInvariant())
            {
                case "csv":
                    ToCsv(dataFrameName, filePath);
                    break;
                case "excel":
                case "xlsx":
                    ToExcel(dataFrameName, filePath);
                    break;
                case "json":
                    ToJson(dataFrameName, filePath);
                    break;
                case "parquet":
                    ExecuteInSession(() =>
                    {
                        string script = $@"
try:
    {dataFrameName}.to_parquet({SafeStringFormat(filePath)})
    print(f'Successfully exported {{len({dataFrameName})}} rows to Parquet: {filePath}')
except Exception as e:
    print(f'Error exporting to Parquet: {{e}}')
    raise
";
                        _sessionScope!.Exec(script);
                        UpdateDataFrameAccess(dataFrameName);
                    });
                    break;
                default:
                    throw new ArgumentException($"Unsupported export format: {format}", nameof(format));
            }
        }
        #endregion

        #region Data Selection and Filtering
        /// <summary>
        /// Selects specific columns from DataFrame
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="columns">Columns to select</param>
        public void SelectColumns(string dataFrameName, string newFrameName, string[] columns)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (columns == null || columns.Length == 0)
                throw new ArgumentException("Columns array cannot be null or empty.", nameof(columns));
            
            var columnsStr = string.Join(", ", columns.Select(c => SafeStringFormat(c)));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}[[{columnsStr}]].copy()
    print(f'Selected {{len({newFrameName}.columns)}} columns from {dataFrameName}')
except Exception as e:
    print(f'Error selecting columns: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Filters rows based on condition
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="condition">Filter condition</param>
        public void FilterRows(string dataFrameName, string newFrameName, string condition)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("Filter condition cannot be null or empty.", nameof(condition));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}[{condition}].copy()
    print(f'Filtered {dataFrameName}: {{len({newFrameName})}} rows match condition')
except Exception as e:
    print(f'Error filtering rows: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Advanced filtering with query evaluation
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="query">Query string</param>
        public void QueryDataFrame(string dataFrameName, string newFrameName, string query)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be null or empty.", nameof(query));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.query({SafeStringFormat(query)}).copy()
    print(f'Query result: {{len({newFrameName})}} rows from {dataFrameName}')
except Exception as e:
    print(f'Error executing query: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Sample DataFrame randomly
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="fraction">Fraction to sample</param>
        public void SampleDataFrame(string dataFrameName, string newFrameName, double fraction)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (fraction <= 0 || fraction > 1)
                throw new ArgumentException("Fraction must be between 0 and 1.", nameof(fraction));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.sample(frac={fraction}).copy()
    print(f'Sampled {{len({newFrameName})}} rows ({fraction*100:.1f}%) from {dataFrameName}')
except Exception as e:
    print(f'Error sampling DataFrame: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }
        #endregion

        #region Data Manipulation Operations
        /// <summary>
        /// Adds a new column to DataFrame with validation
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">New column name</param>
        /// <param name="columnData">Column data or expression</param>
        public void AddColumn(string dataFrameName, string columnName, dynamic columnData)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}[{SafeStringFormat(columnName)}] = {columnData}
    print(f'Added column ""{columnName}"" to {dataFrameName}')
except Exception as e:
    print(f'Error adding column: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Drops a column from DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column to drop</param>
        public void DropColumn(string dataFrameName, string columnName)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}.drop(columns=[{SafeStringFormat(columnName)}], inplace=True)
    print(f'Dropped column ""{columnName}"" from {dataFrameName}')
except Exception as e:
    print(f'Error dropping column: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Renames columns in DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="newColumnNames">Mapping of old to new column names</param>
        public void RenameColumns(string dataFrameName, Dictionary<string, string> newColumnNames)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (newColumnNames == null || newColumnNames.Count == 0)
                throw new ArgumentException("Column name mapping cannot be null or empty.", nameof(newColumnNames));
            
            var renameMapping = string.Join(", ", 
                newColumnNames.Select(kvp => $"{SafeStringFormat(kvp.Key)}: {SafeStringFormat(kvp.Value)}"));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}.rename(columns={{{renameMapping}}}, inplace=True)
    print(f'Renamed {{len({newColumnNames.Count})}} columns in {dataFrameName}')
except Exception as e:
    print(f'Error renaming columns: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Applies a function to a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <param name="func">Function to apply</param>
        public void ApplyFunction(string dataFrameName, string columnName, string func)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            if (string.IsNullOrWhiteSpace(func))
                throw new ArgumentException("Function cannot be null or empty.", nameof(func));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}[{SafeStringFormat(columnName)}] = {dataFrameName}[{SafeStringFormat(columnName)}].apply({func})
    print(f'Applied function to column ""{columnName}"" in {dataFrameName}')
except Exception as e:
    print(f'Error applying function: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Converts data type of a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <param name="newType">New data type</param>
        public void ConvertDataType(string dataFrameName, string columnName, string newType)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            if (string.IsNullOrWhiteSpace(newType))
                throw new ArgumentException("Data type cannot be null or empty.", nameof(newType));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}[{SafeStringFormat(columnName)}] = {dataFrameName}[{SafeStringFormat(columnName)}].astype({SafeStringFormat(newType)})
    print(f'Converted column ""{columnName}"" to {newType} in {dataFrameName}')
except Exception as e:
    print(f'Error converting data type: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }
        #endregion

        #region Data Cleaning Operations
        /// <summary>
        /// Handles missing values by dropping rows with NaN
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        public void DropNA(string dataFrameName, string newFrameName)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.dropna().copy()
    dropped_rows = len({dataFrameName}) - len({newFrameName})
    print(f'Dropped {{dropped_rows}} rows with missing values from {dataFrameName}')
except Exception as e:
    print(f'Error dropping NA values: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Fills missing values with specified value
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="value">Value to fill with</param>
        public void FillNA(string dataFrameName, string newFrameName, dynamic value)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.fillna({value}).copy()
    print(f'Filled missing values in {dataFrameName} with {value}')
except Exception as e:
    print(f'Error filling NA values: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Removes duplicate rows
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        public void DropDuplicates(string dataFrameName, string newFrameName)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.drop_duplicates().copy()
    dropped_rows = len({dataFrameName}) - len({newFrameName})
    print(f'Dropped {{dropped_rows}} duplicate rows from {dataFrameName}')
except Exception as e:
    print(f'Error dropping duplicates: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Optimizes memory usage of DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        public void OptimizeMemory(string dataFrameName)
        {
            ValidateDataFrameName(dataFrameName);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    old_memory = {dataFrameName}.memory_usage(deep=True).sum()
    {dataFrameName} = {dataFrameName}.convert_dtypes()
    new_memory = {dataFrameName}.memory_usage(deep=True).sum()
    reduction = (old_memory - new_memory) / old_memory * 100
    print(f'Memory optimization complete for {dataFrameName}: {{reduction:.1f}}% reduction')
except Exception as e:
    print(f'Error optimizing memory: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }
        #endregion

        #region Aggregation and Grouping
        /// <summary>
        /// Groups DataFrame by column and applies aggregation
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="groupByColumn">Column to group by</param>
        /// <param name="aggFunc">Aggregation function</param>
        public void GroupBy(string dataFrameName, string newFrameName, string groupByColumn, string aggFunc)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (string.IsNullOrWhiteSpace(groupByColumn))
                throw new ArgumentException("Group by column cannot be null or empty.", nameof(groupByColumn));
            
            if (string.IsNullOrWhiteSpace(aggFunc))
                throw new ArgumentException("Aggregation function cannot be null or empty.", nameof(aggFunc));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.groupby({SafeStringFormat(groupByColumn)}).{aggFunc}().reset_index()
    print(f'Grouped {dataFrameName} by ""{groupByColumn}"" using {aggFunc}')
except Exception as e:
    print(f'Error in group by operation: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Groups DataFrame by column and applies aggregation asynchronously
        /// </summary>
        public async Task GroupByAsync(string dataFrameName, string newFrameName, string groupByColumn, string aggFunc, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => GroupBy(dataFrameName, newFrameName, groupByColumn, aggFunc), cancellationToken);
        }

        /// <summary>
        /// Creates pivot table
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="index">Index column</param>
        /// <param name="columns">Columns for pivot</param>
        /// <param name="values">Values column</param>
        public void PivotTable(string dataFrameName, string newFrameName, string index, string columns, string values)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (string.IsNullOrWhiteSpace(index))
                throw new ArgumentException("Index column cannot be null or empty.", nameof(index));
            
            if (string.IsNullOrWhiteSpace(columns))
                throw new ArgumentException("Columns parameter cannot be null or empty.", nameof(columns));
            
            if (string.IsNullOrWhiteSpace(values))
                throw new ArgumentException("Values column cannot be null or empty.", nameof(values));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.pivot_table(
        index={SafeStringFormat(index)}, 
        columns={SafeStringFormat(columns)}, 
        values={SafeStringFormat(values)}
    ).reset_index()
    print(f'Created pivot table from {dataFrameName}')
except Exception as e:
    print(f'Error creating pivot table: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Cross tabulation of two columns
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="index">Index column</param>
        /// <param name="columns">Columns for cross-tab</param>
        public void CrossTab(string dataFrameName, string newFrameName, string index, string columns)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (string.IsNullOrWhiteSpace(index))
                throw new ArgumentException("Index column cannot be null or empty.", nameof(index));
            
            if (string.IsNullOrWhiteSpace(columns))
                throw new ArgumentException("Columns parameter cannot be null or empty.", nameof(columns));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = pd.crosstab({dataFrameName}[{SafeStringFormat(index)}], {dataFrameName}[{SafeStringFormat(columns)}])
    print(f'Created cross tabulation from {dataFrameName}')
except Exception as e:
    print(f'Error creating cross tabulation: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }
        #endregion

        #region Statistical Analysis
        /// <summary>
        /// Gets descriptive statistics of DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <returns>Descriptive statistics as string</returns>
        public string Describe(string dataFrameName)
        {
            ValidateDataFrameName(dataFrameName);
            
            return ExecuteInSession(() =>
            {
                dynamic result = _sessionScope!.Eval($"{dataFrameName}.describe()");
                UpdateDataFrameAccess(dataFrameName);
                return result.ToString();
            });
        }

        /// <summary>
        /// Calculates correlation matrix
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <returns>Correlation matrix as string</returns>
        public string Correlation(string dataFrameName)
        {
            ValidateDataFrameName(dataFrameName);
            
            return ExecuteInSession(() =>
            {
                dynamic result = _sessionScope!.Eval($"{dataFrameName}.corr()");
                UpdateDataFrameAccess(dataFrameName);
                return result.ToString();
            });
        }

        /// <summary>
        /// Calculates mean of a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Mean value as string</returns>
        public string CalculateMean(string dataFrameName, string columnName)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            return ExecuteInSession(() =>
            {
                dynamic result = _sessionScope!.Eval($"{dataFrameName}[{SafeStringFormat(columnName)}].mean()");
                UpdateDataFrameAccess(dataFrameName);
                return result.ToString();
            });
        }

        /// <summary>
        /// Calculates median of a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Median value as string</returns>
        public string CalculateMedian(string dataFrameName, string columnName)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            return ExecuteInSession(() =>
            {
                dynamic result = _sessionScope!.Eval($"{dataFrameName}[{SafeStringFormat(columnName)}].median()");
                UpdateDataFrameAccess(dataFrameName);
                return result.ToString();
            });
        }

        /// <summary>
        /// Gets unique values in a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Unique values as string</returns>
        public string UniqueValues(string dataFrameName, string columnName)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            return ExecuteInSession(() =>
            {
                dynamic result = _sessionScope!.Eval($"{dataFrameName}[{SafeStringFormat(columnName)}].unique()");
                UpdateDataFrameAccess(dataFrameName);
                return result.ToString();
            });
        }

        /// <summary>
        /// Gets value counts for a column
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Value counts as string</returns>
        public string ValueCounts(string dataFrameName, string columnName)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            return ExecuteInSession(() =>
            {
                dynamic result = _sessionScope!.Eval($"{dataFrameName}[{SafeStringFormat(columnName)}].value_counts()");
                UpdateDataFrameAccess(dataFrameName);
                return result.ToString();
            });
        }
        #endregion

        #region Time Series Operations
        /// <summary>
        /// Converts column to datetime
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <param name="columnName">Column name</param>
        public void ConvertToDateTime(string dataFrameName, string columnName)
        {
            ValidateDataFrameName(dataFrameName);
            
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name cannot be null or empty.", nameof(columnName));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}[{SafeStringFormat(columnName)}] = pd.to_datetime({dataFrameName}[{SafeStringFormat(columnName)}])
    print(f'Converted column ""{columnName}"" to datetime in {dataFrameName}')
except Exception as e:
    print(f'Error converting to datetime: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Resamples time series data
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="rule">Resampling rule</param>
        /// <param name="aggFunc">Aggregation function</param>
        public void Resample(string dataFrameName, string newFrameName, string rule, string aggFunc)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (string.IsNullOrWhiteSpace(rule))
                throw new ArgumentException("Resampling rule cannot be null or empty.", nameof(rule));
            
            if (string.IsNullOrWhiteSpace(aggFunc))
                throw new ArgumentException("Aggregation function cannot be null or empty.", nameof(aggFunc));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.resample({SafeStringFormat(rule)}).{aggFunc}().reset_index()
    print(f'Resampled {dataFrameName} using rule ""{rule}"" and function ""{aggFunc}""')
except Exception as e:
    print(f'Error resampling data: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Creates rolling window statistics
        /// </summary>
        /// <param name="dataFrameName">Source DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="windowSize">Window size</param>
        /// <param name="operation">Operation to perform</param>
        public void RollingWindow(string dataFrameName, string newFrameName, int windowSize, string operation)
        {
            ValidateDataFrameName(dataFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (windowSize <= 0)
                throw new ArgumentException("Window size must be positive.", nameof(windowSize));
            
            if (string.IsNullOrWhiteSpace(operation))
                throw new ArgumentException("Operation cannot be null or empty.", nameof(operation));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = {dataFrameName}.rolling(window={windowSize}).{operation}()
    print(f'Applied rolling {operation} with window size {windowSize} to {dataFrameName}')
except Exception as e:
    print(f'Error applying rolling window: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }
        #endregion

        #region Data Merging and Joining
        /// <summary>
        /// Merges two DataFrames
        /// </summary>
        /// <param name="leftFrameName">Left DataFrame name</param>
        /// <param name="rightFrameName">Right DataFrame name</param>
        /// <param name="newFrameName">New DataFrame name</param>
        /// <param name="onColumn">Column to join on</param>
        public void MergeDataFrames(string leftFrameName, string rightFrameName, string newFrameName, string onColumn)
        {
            ValidateDataFrameName(leftFrameName);
            ValidateDataFrameName(rightFrameName);
            ValidateDataFrameName(newFrameName);
            
            if (string.IsNullOrWhiteSpace(onColumn))
                throw new ArgumentException("Join column cannot be null or empty.", nameof(onColumn));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = pd.merge({leftFrameName}, {rightFrameName}, on={SafeStringFormat(onColumn)})
    print(f'Merged {leftFrameName} and {rightFrameName} on column ""{onColumn}""')
except Exception as e:
    print(f'Error merging DataFrames: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(leftFrameName);
                UpdateDataFrameAccess(rightFrameName);
                UpdateDataFrameAccess(newFrameName);
            });
        }

        /// <summary>
        /// Concatenates multiple DataFrames
        /// </summary>
        /// <param name="dataFrameNames">Array of DataFrame names</param>
        /// <param name="newFrameName">New DataFrame name</param>
        public void ConcatDataFrames(string[] dataFrameNames, string newFrameName)
        {
            if (dataFrameNames == null || dataFrameNames.Length == 0)
                throw new ArgumentException("DataFrame names array cannot be null or empty.", nameof(dataFrameNames));
            
            ValidateDataFrameName(newFrameName);
            
            foreach (var name in dataFrameNames)
            {
                ValidateDataFrameName(name);
            }
            
            var frames = string.Join(", ", dataFrameNames);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {newFrameName} = pd.concat([{frames}], ignore_index=True)
    print(f'Concatenated {{len([{frames}])}} DataFrames into {newFrameName}')
except Exception as e:
    print(f'Error concatenating DataFrames: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                
                foreach (var name in dataFrameNames)
                {
                    UpdateDataFrameAccess(name);
                }
                UpdateDataFrameAccess(newFrameName);
            });
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Gets DataFrame as JSON string
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <returns>JSON representation</returns>
        public string GetDataFrameAsJson(string dataFrameName)
        {
            ValidateDataFrameName(dataFrameName);
            
            return ExecuteInSession(() =>
            {
                dynamic result = _sessionScope!.Eval($"{dataFrameName}.to_json()");
                UpdateDataFrameAccess(dataFrameName);
                return result.ToString();
            });
        }

        /// <summary>
        /// Checks for null values in DataFrame
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        /// <returns>Null value information</returns>
        public string IsNull(string dataFrameName)
        {
            ValidateDataFrameName(dataFrameName);
            
            return ExecuteInSession(() =>
            {
                dynamic result = _sessionScope!.Eval($"{dataFrameName}.isnull().sum()");
                UpdateDataFrameAccess(dataFrameName);
                return result.ToString();
            });
        }

        /// <summary>
        /// Resets DataFrame index
        /// </summary>
        /// <param name="dataFrameName">DataFrame name</param>
        public void ResetIndex(string dataFrameName)
        {
            ValidateDataFrameName(dataFrameName);
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    {dataFrameName}.reset_index(drop=True, inplace=True)
    print(f'Reset index for {dataFrameName}')
except Exception as e:
    print(f'Error resetting index: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
                UpdateDataFrameAccess(dataFrameName);
            });
        }

        /// <summary>
        /// Sets pandas option
        /// </summary>
        /// <param name="optionName">Option name</param>
        /// <param name="value">Option value</param>
        public void SetPandasOption(string optionName, string value)
        {
            if (string.IsNullOrWhiteSpace(optionName))
                throw new ArgumentException("Option name cannot be null or empty.", nameof(optionName));
            
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Option value cannot be null or empty.", nameof(value));
            
            ExecuteInSession(() =>
            {
                string script = $@"
try:
    pd.set_option({SafeStringFormat(optionName)}, {value})
    print(f'Set pandas option ""{optionName}"" to {value}')
except Exception as e:
    print(f'Error setting pandas option: {{e}}')
    raise
";
                _sessionScope!.Exec(script);
            });
        }
        #endregion

        #region Memory Management and Cleanup
        /// <summary>
        /// Gets memory usage information for all tracked DataFrames
        /// </summary>
        /// <returns>Memory usage report</returns>
        public string GetMemoryUsageReport()
        {
            return ExecuteInSession(() =>
            {
                string script = @"
import gc
memory_info = []
for name in list(globals().keys()):
    obj = globals()[name]
    if hasattr(obj, 'memory_usage') and hasattr(obj, 'shape'):
        try:
            memory = obj.memory_usage(deep=True).sum()
            memory_info.append(f'{name}: {obj.shape} - {memory/1024/1024:.2f} MB')
        except:
            pass
print('\n'.join(memory_info) if memory_info else 'No DataFrames found')
";
                _sessionScope!.Exec(script);
                return "Memory usage report generated - check Python output";
            });
        }

        /// <summary>
        /// Cleans up unused DataFrames based on last access time
        /// </summary>
        /// <param name="maxIdleMinutes">Maximum idle time in minutes before cleanup</param>
        public void CleanupIdleDataFrames(int maxIdleMinutes = 30)
        {
            var cutoffTime = DateTime.Now.AddMinutes(-maxIdleMinutes);
            var framesToCleanup = _dataFrameLastAccess
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            if (framesToCleanup.Any())
            {
                ExecuteInSession(() =>
                {
                    foreach (var frameName in framesToCleanup)
                    {
                        try
                        {
                            string script = $@"
if '{frameName}' in globals():
    del {frameName}
    print(f'Cleaned up DataFrame: {frameName}')
";
                            _sessionScope!.Exec(script);
                            _dataFrameLastAccess.Remove(frameName);
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail on cleanup errors
                            Console.WriteLine($"Warning: Failed to cleanup DataFrame {frameName}: {ex.Message}");
                        }
                    }

                    // Force garbage collection
                    _sessionScope!.Exec("import gc; gc.collect()");
                });
            }
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Disposes of the PythonPandasManager and cleans up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        /// <param name="disposing">True if disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    
                    // Clean up all tracked DataFrames
                    CleanupIdleDataFrames(0); // Clean up all DataFrames regardless of age
                    
                    _cancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    // Log disposal errors but don't throw
                    Console.WriteLine($"Warning during disposal: {ex.Message}");
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }
        #endregion
    }
}