using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Reframe.Core.Scripting;

/// <summary>
/// Roslyn-powered C# and LINQ scripting engine for live evaluation of ad-hoc expressions on text input.
/// </summary>
public class CSharpScriptEngine
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly ScriptOptions _scriptOptions;
    private readonly ConcurrentDictionary<string, ScriptRunner<object>> _compiledScriptCache = new();

    public CSharpScriptEngine()
    {
        var references = GetDefaultReferences();

        _scriptOptions = ScriptOptions.Default
            .WithImports(
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.Text.Json",
                "System.Globalization",
                "System.IO")
            .WithReferences(references);
    }

    private static List<MetadataReference> GetDefaultReferences()
    {
        var assemblies = new HashSet<Assembly>
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Regex).Assembly,
            typeof(JsonSerializer).Assembly,
            typeof(ScriptGlobals).Assembly
        };

        void TryAddAssembly(string name)
        {
            try
            {
                var asm = Assembly.Load(name);
                if (asm != null) assemblies.Add(asm);
            }
            catch
            {
                // ignore
            }
        }

        TryAddAssembly("System.Runtime");
        TryAddAssembly("System.Collections");
        TryAddAssembly("System.Linq");
        TryAddAssembly("System.Linq.Expressions");
        TryAddAssembly("System.Text.RegularExpressions");
        TryAddAssembly("System.Text.Json");
        TryAddAssembly("System.Globalization");
        TryAddAssembly("System.IO");
        TryAddAssembly("netstandard");

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.FullName))
            {
                assemblies.Add(asm);
            }
        }

        var references = new List<MetadataReference>();
        foreach (var assembly in assemblies)
        {
            var reference = CreateMetadataReference(assembly);
            if (reference != null)
            {
                references.Add(reference);
            }
        }

        return references;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("SingleFile", "IL3000:AvoidAccessingAssemblyLocation",
        Justification = "Assembly.Location is checked with fallback to in-memory raw metadata for single-file publishing.")]
    private static MetadataReference? CreateMetadataReference(Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(assembly.Location))
        {
            try
            {
                return MetadataReference.CreateFromFile(assembly.Location);
            }
            catch
            {
                // Fall back to raw metadata if location fails
            }
        }

        unsafe
        {
            if (System.Reflection.Metadata.AssemblyExtensions.TryGetRawMetadata(assembly, out byte* blob, out int length))
            {
                var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
                var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                return assemblyMetadata.GetReference();
            }
        }

        return null;
    }

    /// <summary>
    /// Warms up Roslyn script engine in the background to eliminate cold start latency.
    /// </summary>
    public void WarmUp()
    {
        Task.Run(() =>
        {
            try
            {
                Evaluate("1 + 1", string.Empty, TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore warmup exceptions
            }
        });
    }

    /// <summary>
    /// Synchronously evaluates a C# script or expression against the provided input text.
    /// </summary>
    public ScriptResult Evaluate(string script, string input, TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return ScriptResult.Empty;
        }

        var cts = new CancellationTokenSource(timeout ?? DefaultTimeout);
        try
        {
            return Task.Run(() => EvaluateAsync(script, input, cts.Token), cts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            return ScriptResult.Error(script, "Execution timed out (safety threshold exceeded).");
        }
        catch (Exception ex)
        {
            return ScriptResult.Error(script, ex.Message);
        }
    }

    /// <summary>
    /// Asynchronously evaluates a C# script or expression against the provided input text.
    /// </summary>
    public async Task<ScriptResult> EvaluateAsync(string script, string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return ScriptResult.Empty;
        }

        string trimmedScript = script.Trim();
        var swTotal = Stopwatch.StartNew();
        double compileTimeMs = 0;

        try
        {
            var globals = new ScriptGlobals { input = input ?? string.Empty };

            if (!_compiledScriptCache.TryGetValue(trimmedScript, out var runner))
            {
                var swCompile = Stopwatch.StartNew();
                var roslynScript = CSharpScript.Create<object>(trimmedScript, _scriptOptions, typeof(ScriptGlobals));
                
                // Validate diagnostics during compilation
                var diagnostics = roslynScript.Compile(cancellationToken);
                var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                if (errors.Count > 0)
                {
                    var errorMessages = errors.Select(FormatDiagnostic).ToList();
                    return ScriptResult.Error(trimmedScript, errorMessages[0], errorMessages, swTotal.Elapsed.TotalMilliseconds);
                }

                runner = roslynScript.CreateDelegate();
                _compiledScriptCache[trimmedScript] = runner;
                swCompile.Stop();
                compileTimeMs = swCompile.Elapsed.TotalMilliseconds;
            }

            var swExec = Stopwatch.StartNew();
            var state = await runner(globals, cancellationToken).ConfigureAwait(false);
            swExec.Stop();
            swTotal.Stop();

            string formattedOutput = FormatResult(state, globals);
            string returnTypeName = GetFriendlyTypeName(state);

            return ScriptResult.Success(
                trimmedScript,
                formattedOutput,
                state,
                returnTypeName,
                swExec.Elapsed.TotalMilliseconds,
                compileTimeMs);
        }
        catch (CompilationErrorException ex)
        {
            swTotal.Stop();
            var diagnostics = ex.Diagnostics.Select(FormatDiagnostic).ToList();
            string primaryError = diagnostics.FirstOrDefault() ?? ex.Message;
            return ScriptResult.Error(trimmedScript, primaryError, diagnostics, swTotal.Elapsed.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            swTotal.Stop();
            return ScriptResult.Error(trimmedScript, "Script evaluation was cancelled or timed out.", null, swTotal.Elapsed.TotalMilliseconds);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            swTotal.Stop();
            return ScriptResult.Error(trimmedScript, $"Runtime Error: {ex.InnerException.Message}", new[] { ex.InnerException.ToString() }, swTotal.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            swTotal.Stop();
            return ScriptResult.Error(trimmedScript, $"Runtime Error: {ex.Message}", new[] { ex.ToString() }, swTotal.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Formats the raw result or globals buffer into a clean, human-readable string output.
    /// </summary>
    public string FormatResult(object? result, ScriptGlobals globals)
    {
        if (result is null)
        {
            if (globals.output.Count > 0)
            {
                return string.Join(Environment.NewLine, globals.output);
            }
            return string.Empty;
        }

        if (result is string strResult)
        {
            return strResult;
        }

        if (result is IEnumerable<string> stringEnumerable)
        {
            return string.Join(Environment.NewLine, stringEnumerable);
        }

        if (result is IEnumerable enumerable && !(result is IDictionary))
        {
            var items = new List<object?>();
            foreach (var item in enumerable)
            {
                items.Add(item);
            }

            if (items.Count == 0)
            {
                return "[]";
            }

            // If elements are primitive or string, join as lines
            bool allPrimitive = items.All(i => i == null || IsSimpleType(i.GetType()));
            if (allPrimitive)
            {
                return string.Join(Environment.NewLine, items.Select(i => i?.ToString() ?? "null"));
            }

            // Otherwise, serialize as JSON array
            return JsonSerializer.Serialize(items, IndentedJsonOptions);
        }

        if (IsSimpleType(result.GetType()))
        {
            return result.ToString() ?? string.Empty;
        }

        // Complex object: serialize to indented JSON
        try
        {
            return JsonSerializer.Serialize(result, result.GetType(), IndentedJsonOptions);
        }
        catch
        {
            return result.ToString() ?? string.Empty;
        }
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Guid);
    }

    private static string GetFriendlyTypeName(object? result)
    {
        if (result is null) return "void / null";
        var type = result.GetType();
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "int";
        if (type == typeof(double)) return "double";
        if (type == typeof(bool)) return "bool";

        if (result is IEnumerable)
        {
            var elemType = type.IsGenericType ? type.GetGenericArguments()[0].Name : "object";
            return $"IEnumerable<{elemType}>";
        }

        return type.Name;
    }

    private static string FormatDiagnostic(Diagnostic d)
    {
        var lineSpan = d.Location.GetLineSpan();
        int line = lineSpan.StartLinePosition.Line + 1;
        int col = lineSpan.StartLinePosition.Character + 1;
        return $"Line {line}, Col {col}: {d.GetMessage(CultureInfo.InvariantCulture)}";
    }
}
