using Beep.Python.Model;
using Beep.Python.RuntimeEngine.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Beep.Python.RuntimeEngine;

namespace Beep.Python.RuntimeHost.Services;

/// <summary>
/// Code execution manager that uses PythonHost backend infrastructure.
/// Provides code execution capabilities using the PythonHost abstraction layer.
/// </summary>
public class PythonHostCodeExecuteManager : IDisposable
{
    private readonly IPythonHost _pythonHost;
    private readonly IVenvManager? _venvManager;
    private readonly ILogger<PythonHostCodeExecuteManager> _logger;
    private readonly Dictionary<string, IPythonModuleHandle> _moduleHandles = new();
    private readonly Dictionary<string, IPythonObjectHandle> _objectHandles = new();
    private bool _disposed = false;

    public PythonHostCodeExecuteManager(
        IPythonHost pythonHost,
        IVenvManager? venvManager,
        ILogger<PythonHostCodeExecuteManager> logger)
    {
        _pythonHost = pythonHost ?? throw new ArgumentNullException(nameof(pythonHost));
        _venvManager = venvManager;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes Python code asynchronously using PythonHost backend.
    /// </summary>
    public async Task<(bool Success, string Output)> ExecuteCodeAsync(
        string code,
        PythonSessionInfo session,
        int timeoutSeconds = 120,
        IProgress<PassedParameters>? progress = null)
    {
        if (string.IsNullOrEmpty(code))
        {
            progress?.Report(new PassedParameters { Flag = Errors.Warning, Message = "No code provided" });
            return (false, "Error: No code provided");
        }

        if (session == null)
        {
            progress?.Report(new PassedParameters { Flag = Errors.Warning, Message = "No session provided" });
            return (false, "Error: No session provided");
        }

        try
        {
            // Ensure PythonHost is initialized
            if (!_pythonHost.IsInitialized)
            {
                progress?.Report(new PassedParameters { Flag = Errors.Failed, Message = "PythonHost not initialized" });
                return (false, "Error: PythonHost not initialized");
            }

            // Switch to the session's virtual environment if needed
            if (!string.IsNullOrEmpty(session.VirtualEnvironmentId))
            {
                // Get environment path from session and switch PythonHost context
                // This would need integration with existing VirtualEnvManager
            }

            // Execute code using PythonHost backend
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var result = await _pythonHost.ExecuteCode(code, cts.Token);

            if (result.Success)
            {
                progress?.Report(new PassedParameters { Flag = Errors.Ok, Message = "Code executed successfully" });
                return (true, result.Output ?? string.Empty);
            }
            else
            {
                progress?.Report(new PassedParameters { Flag = Errors.Failed, Message = result.Error ?? "Execution failed" });
                return (false, result.Error ?? "Execution failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Python code");
            progress?.Report(new PassedParameters { Flag = Errors.Failed, Message = ex.Message });
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a Python script file using PythonHost backend.
    /// </summary>
    public async Task<(bool Success, string Output)> ExecuteScriptFileAsync(
        string filePath,
        PythonSessionInfo session,
        int timeoutSeconds = 300,
        IProgress<PassedParameters>? progress = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            progress?.Report(new PassedParameters { Flag = Errors.Failed, Message = $"File not found: {filePath}" });
            return (false, $"Error: File not found: {filePath}");
        }

        try
        {
            var scriptContent = await File.ReadAllTextAsync(filePath);
            return await ExecuteCodeAsync(scriptContent, session, timeoutSeconds, progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading script file");
            return (false, $"Error reading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes code with variables using PythonHost backend.
    /// </summary>
    public async Task<object?> ExecuteWithVariablesAsync(
        string code,
        PythonSessionInfo session,
        Dictionary<string, object> variables,
        IProgress<PassedParameters>? progress = null)
    {
        if (variables == null || variables.Count == 0)
        {
            return await ExecuteCodeAsync(code, session, 120, progress);
        }

        try
        {
            // Build Python code that sets variables and executes
            var variableCode = string.Join("\n", variables.Select(kvp =>
                $"{kvp.Key} = {ConvertToPythonLiteral(kvp.Value)}"));

            var fullCode = variableCode + "\n\n" + code;

            var result = await ExecuteCodeAsync(fullCode, session, 120, progress);
            if (result.Success)
            {
                // Try to evaluate and return the result
                // This is simplified - in practice you'd need to capture return value
                return result.Output;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing with variables");
            return null;
        }
    }

    private string ConvertToPythonLiteral(object value)
    {
        return value switch
        {
            null => "None",
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            bool b => b ? "True" : "False",
            int or long or float or double => value.ToString()!,
            _ => JsonSerializer.Serialize(value)
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Cleanup module and object handles
            foreach (var handle in _moduleHandles.Values)
            {
                try { handle.Dispose(); } catch { }
            }
            _moduleHandles.Clear();

            foreach (var handle in _objectHandles.Values)
            {
                try { handle.Dispose(); } catch { }
            }
            _objectHandles.Clear();

            _disposed = true;
        }
    }
}
