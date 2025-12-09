using System;
using System.Collections.Generic;

namespace Beep.Python.RuntimeEngine.Infrastructure;
public class FunctionResult
{
    public object? Value { get; set; } // Main result (can be any type)
    public IDictionary<string, object>? Data { get; set; } // Additional named outputs
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public int? ErrorCode { get; set; } // Numeric or enum code for error type
    public string? ExceptionDetail { get; set; } // Stack trace or exception info (optional)
}

public interface IFunctionCall
{
    string Name { get; }
    string Description { get; }
    IDictionary<string, string> Parameters { get; } // Parameter name and description

    // Optional: Parameter types for richer metadata (SK supports this)
    IDictionary<string, Type>? ParameterTypes { get; }

    // Return a structured result for workflow chaining
    FunctionResult? Invoke(IDictionary<string, object> arguments);

    IEnumerable<string>? Tags { get; }
    string? Example { get; }
    IDictionary<string, object>? Metadata { get; }
}