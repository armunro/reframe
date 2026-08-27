using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Reframe.Core.Scripting;

/// <summary>
/// Host globals available directly inside C# / LINQ scripts and scratchpad expressions.
/// </summary>
public class ScriptGlobals
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Gets or sets the primary raw input text passed from the editor.
    /// </summary>
    public string input { get; set; } = string.Empty;

    /// <summary>
    /// Alias for input text.
    /// </summary>
    public string text => input;

    /// <summary>
    /// Input split into lines preserving empty lines.
    /// </summary>
    public string[] lines => input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

    /// <summary>
    /// Input split into lines excluding whitespace-only lines.
    /// </summary>
    public string[] nonEmptyLines => lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

    /// <summary>
    /// Output buffer for manual logging or multi-line scripting.
    /// </summary>
    public List<string> output { get; } = new();

    /// <summary>
    /// Appends a string or object representation to the script output.
    /// </summary>
    public void print(object? value)
    {
        output.Add(value?.ToString() ?? "null");
    }

    /// <summary>
    /// Appends a line to the script output.
    /// </summary>
    public void println(object? value)
    {
        output.Add(value?.ToString() ?? "null");
    }

    /// <summary>
    /// Serializes an object to indented JSON and appends it to the script output.
    /// </summary>
    public void dump(object? value)
    {
        if (value == null)
        {
            output.Add("null");
        }
        else
        {
            output.Add(JsonSerializer.Serialize(value, value.GetType(), IndentedJsonOptions));
        }
    }
}
