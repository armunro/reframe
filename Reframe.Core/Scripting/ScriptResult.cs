using System;
using System.Collections.Generic;

namespace Reframe.Core.Scripting;

/// <summary>
/// Result of evaluating a C# / LINQ script.
/// </summary>
public class ScriptResult
{
    public bool IsSuccess { get; init; }
    public string Script { get; init; } = string.Empty;
    public string Output { get; init; } = string.Empty;
    public object? ReturnValue { get; init; }
    public string ReturnTypeName { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();
    public double ExecutionTimeMs { get; init; }
    public double CompilationTimeMs { get; init; }

    public static ScriptResult Empty => new()
    {
        IsSuccess = true,
        Script = string.Empty,
        Output = string.Empty,
        ReturnTypeName = "void",
        Diagnostics = Array.Empty<string>()
    };

    public static ScriptResult Success(
        string script,
        string output,
        object? returnValue,
        string returnTypeName,
        double executionTimeMs,
        double compilationTimeMs = 0) => new()
    {
        IsSuccess = true,
        Script = script,
        Output = output,
        ReturnValue = returnValue,
        ReturnTypeName = returnTypeName,
        ExecutionTimeMs = executionTimeMs,
        CompilationTimeMs = compilationTimeMs,
        Diagnostics = Array.Empty<string>()
    };

    public static ScriptResult Error(
        string script,
        string errorMessage,
        IReadOnlyList<string>? diagnostics = null,
        double executionTimeMs = 0) => new()
    {
        IsSuccess = false,
        Script = script,
        Output = string.Empty,
        ErrorMessage = errorMessage,
        Diagnostics = diagnostics ?? (string.IsNullOrEmpty(errorMessage) ? Array.Empty<string>() : new[] { errorMessage }),
        ExecutionTimeMs = executionTimeMs
    };
}
