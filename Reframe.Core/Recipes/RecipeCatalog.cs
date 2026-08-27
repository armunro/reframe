using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Reframe.Core.Structured.Transformers;
using Reframe.Core.Tabular.Converters;
using Reframe.Core.Tabular.Parsers;
using Reframe.Core.Transformers.Case;
using Reframe.Core.Transformers.Developer;
using Reframe.Core.Transformers.Encoding;
using Reframe.Core.Transformers.Line;

namespace Reframe.Core.Recipes;

public class RecipeCatalogItem
{
    public string ActionId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "⚡";
    public Dictionary<string, string> DefaultParameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public RecipeStep CreateStep() => new RecipeStep(ActionId, Title, Category, Description, Icon, DefaultParameters);
}

public static class RecipeCatalog
{
    private static readonly List<RecipeCatalogItem> _catalogItems = new();
    private static readonly Dictionary<string, Func<string?, Dictionary<string, string>, string>> _stepExecutors = new(StringComparer.OrdinalIgnoreCase);

    static RecipeCatalog()
    {
        // -------------------------------------------------------------
        // Data Extraction
        // -------------------------------------------------------------
        Register("ExtractUrls", "Extract URLs", "Extraction", "Extract all HTTP/HTTPS links into separate lines", "🌐",
            (text, _) => ExtractUrls(text));

        Register("ExtractEmails", "Extract Emails", "Extraction", "Extract all email addresses into separate lines", "📧",
            (text, _) => ExtractPattern(text, @"\b[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}\b"));

        Register("ExtractNumbers", "Extract Numbers", "Extraction", "Extract all numeric tokens into separate lines", "🔢",
            (text, _) => ExtractPattern(text, @"\b-?\d+(?:\.\d+)?\b"));

        Register("ExtractIpv4", "Extract IPv4 Addresses", "Extraction", "Extract all IPv4 network addresses", "🖥️",
            (text, _) => ExtractPattern(text, @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b"));

        Register("ExtractRegex", "Regex Extract", "Extraction", "Extract matches using custom regular expression pattern", "🎯",
            (text, p) =>
            {
                string pattern = p.GetValueOrDefault("Pattern", @"\w+");
                int group = int.TryParse(p.GetValueOrDefault("Group", "0"), out int g) ? g : 0;
                return LineTransformers.ExtractRegex(text, pattern, group);
            },
            new() { ["Pattern"] = @"\w+", ["Group"] = "0" });

        // -------------------------------------------------------------
        // Line Operations
        // -------------------------------------------------------------
        Register("Deduplicate", "Deduplicate Lines", "Lines", "Remove duplicate lines keeping distinct items", "✨",
            (text, _) => LineTransformers.DeduplicateLines(text, DeduplicateMode.Distinct, false));

        Register("DeduplicateCaseSensitive", "Deduplicate (Case-Sensitive)", "Lines", "Remove duplicate lines with exact case sensitivity", "✨",
            (text, _) => LineTransformers.DeduplicateLines(text, DeduplicateMode.Distinct, true));

        Register("SortAlphabetical", "Sort Alphabetically (A ➔ Z)", "Lines", "Sort lines alphabetically in ascending order", "🔤",
            (text, _) => LineTransformers.SortLines(text, SortOrder.CaseInsensitiveAsc));

        Register("SortAlphabeticalDesc", "Sort Alphabetically (Z ➔ A)", "Lines", "Sort lines alphabetically in descending order", "🔤",
            (text, _) => LineTransformers.SortLines(text, SortOrder.CaseInsensitiveDesc));

        Register("SortNatural", "Sort Naturally (1, 2, 10)", "Lines", "Sort lines using natural numeric ordering", "🔢",
            (text, _) => LineTransformers.SortLines(text, SortOrder.NaturalNumericAsc));

        Register("SortByLength", "Sort by Length", "Lines", "Sort lines by character length", "📏",
            (text, _) => LineTransformers.SortLines(text, SortOrder.LengthAsc));

        Register("ReverseLines", "Reverse Line Order", "Lines", "Reverse the order of all lines", "🔄",
            (text, _) => LineTransformers.SortLines(text, SortOrder.Reverse));

        Register("TrimLines", "Trim Lines", "Lines", "Trim leading and trailing whitespace from each line", "🧹",
            (text, _) => LineTransformers.TrimLines(text, trimStart: true, trimEnd: true, removeEmptyLines: true, collapseWhitespace: false));

        Register("RemoveEmptyLines", "Remove Empty Lines", "Lines", "Remove blank or whitespace-only lines", "🗑️",
            (text, _) => LineTransformers.TrimLines(text, trimStart: false, trimEnd: false, removeEmptyLines: true, collapseWhitespace: false));

        Register("CollapseWhitespace", "Collapse Whitespace", "Lines", "Collapse multiple spaces/tabs into a single space", "💨",
            (text, _) => LineTransformers.TrimLines(text, trimStart: true, trimEnd: true, removeEmptyLines: false, collapseWhitespace: true));

        Register("JoinLines", "Join Lines", "Lines", "Join lines with custom delimiter", "🔗",
            (text, p) =>
            {
                string delim = p.GetValueOrDefault("Delimiter", ", ");
                return LineTransformers.JoinLines(text, delim, QuoteStyle.None);
            },
            new() { ["Delimiter"] = ", " });

        Register("SplitLines", "Split into Lines", "Lines", "Split delimited text into individual lines", "✂️",
            (text, p) =>
            {
                string delim = p.GetValueOrDefault("Delimiter", ",");
                return LineTransformers.SplitLine(text, string.IsNullOrEmpty(delim) ? null : delim);
            },
            new() { ["Delimiter"] = "," });

        Register("QuoteSingle", "Wrap in Single Quotes", "Lines", "Wrap each line in 'single quotes'", "💬",
            (text, _) => LineTransformers.QuoteLines(text, QuoteStyle.SingleQuotes));

        Register("QuoteDouble", "Wrap in Double Quotes", "Lines", "Wrap each line in \"double quotes\"", "💬",
            (text, _) => LineTransformers.QuoteLines(text, QuoteStyle.DoubleQuotes));

        Register("QuoteBackticks", "Wrap in Backticks", "Lines", "Wrap each line in `backticks`", "💬",
            (text, _) => LineTransformers.QuoteLines(text, QuoteStyle.Backticks));

        Register("AddPrefixSuffix", "Add Prefix / Suffix", "Lines", "Add prefix and suffix strings to each line", "➕",
            (text, p) =>
            {
                string prefix = p.GetValueOrDefault("Prefix", "");
                string suffix = p.GetValueOrDefault("Suffix", "");
                return LineTransformers.AddPrefixSuffix(text, prefix, suffix);
            },
            new() { ["Prefix"] = "", ["Suffix"] = "" });

        Register("NumberLines", "Number Lines", "Lines", "Prepend line numbers to lines", "🔢",
            (text, p) =>
            {
                string format = p.GetValueOrDefault("Format", "{n}. ");
                int start = int.TryParse(p.GetValueOrDefault("Start", "1"), out int s) ? s : 1;
                return LineTransformers.NumberLines(text, format, start);
            },
            new() { ["Format"] = "{n}. ", ["Start"] = "1" });

        // -------------------------------------------------------------
        // Code Literals & Query Wrapping
        // -------------------------------------------------------------
        Register("ToJsonArray", "Wrap in JSON Array", "Code", "Format input lines as a JSON array", "📦",
            (text, _) => DeveloperTransformers.ToJsonArray(text, true));

        Register("ToYamlArray", "Wrap in YAML List", "Code", "Format input lines as a YAML sequence", "📄",
            (text, _) => DeveloperTransformers.ToYamlArray(text));

        Register("ToCSharpArray", "Wrap in C# Array", "Code", "Format input lines as a C# string[] array literal", "💻",
            (text, _) => DeveloperTransformers.ToCSharpArray(text, "items", false));

        Register("ToCSharpList", "Wrap in C# List<string>", "Code", "Format input lines as a C# List<string>", "💻",
            (text, _) => DeveloperTransformers.ToCSharpArray(text, "items", true));

        Register("ToTypeScriptArray", "Wrap in TypeScript Array", "Code", "Format input lines as a TypeScript const array", "📜",
            (text, _) => DeveloperTransformers.ToTypeScriptArray(text, "items"));

        Register("ToPythonList", "Wrap in Python List", "Code", "Format input lines as a Python list", "🐍",
            (text, _) => DeveloperTransformers.ToPythonList(text, "items"));

        Register("SqlIn", "Generate SQL IN (...)", "Code", "Format input items into a single-line SQL IN clause", "🗄️",
            (text, _) => DeveloperTransformers.ToSqlInClause(text, false));

        Register("SqlInMultiLine", "Generate SQL IN (Multi-Line)", "Code", "Format input items into a multi-line SQL IN clause", "🗄️",
            (text, _) => DeveloperTransformers.ToSqlInClause(text, true));

        // -------------------------------------------------------------
        // Case Conversions
        // -------------------------------------------------------------
        Register("CamelCase", "Convert to camelCase", "Case", "Convert text to camelCase", "🐪",
            (text, _) => CaseTransformers.ChangeCase(text, TextCasing.CamelCase));

        Register("PascalCase", "Convert to PascalCase", "Case", "Convert text to PascalCase", "📐",
            (text, _) => CaseTransformers.ChangeCase(text, TextCasing.PascalCase));

        Register("SnakeCase", "Convert to snake_case", "Case", "Convert text to snake_case", "🐍",
            (text, _) => CaseTransformers.ChangeCase(text, TextCasing.SnakeCase));

        Register("KebabCase", "Convert to kebab-case", "Case", "Convert text to kebab-case", "🍢",
            (text, _) => CaseTransformers.ChangeCase(text, TextCasing.KebabCase));

        Register("ConstantCase", "Convert to CONSTANT_CASE", "Case", "Convert text to UPPER_SNAKE_CASE", "🔠",
            (text, _) => CaseTransformers.ChangeCase(text, TextCasing.ConstantCase));

        Register("TitleCase", "Convert to Title Case", "Case", "Capitalize the first letter of each word", "📰",
            (text, _) => CaseTransformers.ChangeCase(text, TextCasing.TitleCase));

        Register("UpperCase", "Convert to UPPERCASE", "Case", "Convert all text to uppercase", "🔠",
            (text, _) => CaseTransformers.ChangeCase(text, TextCasing.UpperCase));

        Register("LowerCase", "Convert to lowercase", "Case", "Convert all text to lowercase", "🔡",
            (text, _) => CaseTransformers.ChangeCase(text, TextCasing.LowerCase));

        // -------------------------------------------------------------
        // Encodings & Escapes
        // -------------------------------------------------------------
        Register("UrlEncode", "URL Encode", "Encoding", "Percent-encode URL query components", "🌐",
            (text, _) => EncodingTransformers.UrlEncode(text));

        Register("UrlDecode", "URL Decode", "Encoding", "Decode percent-encoded URL string", "🔓",
            (text, _) => EncodingTransformers.UrlDecode(text));

        Register("Base64Encode", "Base64 Encode", "Encoding", "Encode text to Base64 string", "🔐",
            (text, _) => EncodingTransformers.Base64Encode(text));

        Register("Base64Decode", "Base64 Decode", "Encoding", "Decode Base64 string to plain text", "🔓",
            (text, _) => EncodingTransformers.Base64Decode(text));

        Register("HtmlEncode", "HTML Encode", "Encoding", "Escape HTML special characters (&, <, >, \", ')", "🏷️",
            (text, _) => EncodingTransformers.HtmlEncode(text));

        Register("HtmlDecode", "HTML Decode", "Encoding", "Unescape HTML entities to raw characters", "🔓",
            (text, _) => EncodingTransformers.HtmlDecode(text));

        Register("EscapeCSharp", "C# String Escape", "Encoding", "Escape special chars for C# literal strings", "💻",
            (text, _) => EncodingTransformers.EscapeCSharpString(text));

        Register("UnescapeCSharp", "C# String Unescape", "Encoding", "Unescape C# string escape sequences", "💻",
            (text, _) => EncodingTransformers.UnescapeCSharpString(text));

        Register("JwtDecode", "Decode JWT Token", "Encoding", "Decode JWT header and payload into formatted JSON", "🔑",
            (text, _) => EncodingTransformers.JwtDecode(text));

        // -------------------------------------------------------------
        // Structured Data & Conversions
        // -------------------------------------------------------------
        Register("FormatJson", "Beautify JSON", "Structured", "Format and indent JSON with 2 spaces", "✨",
            (text, _) => StructuredTransformers.BeautifyJson(text));

        Register("MinifyJson", "Minify JSON", "Structured", "Remove all extra whitespace and newlines from JSON", "📦",
            (text, _) => StructuredTransformers.MinifyJson(text));

        Register("FormatYaml", "Beautify YAML", "Structured", "Format and re-indent YAML text", "✨",
            (text, _) => StructuredTransformers.BeautifyYaml(text));

        Register("FormatXml", "Beautify XML", "Structured", "Format and indent XML document", "✨",
            (text, _) => StructuredTransformers.BeautifyXml(text));

        Register("MinifyXml", "Minify XML", "Structured", "Compact XML removing whitespace between tags", "📑",
            (text, _) => StructuredTransformers.MinifyXml(text));

        Register("JsonToYaml", "Convert JSON ➔ YAML", "Structured", "Convert JSON document into YAML format", "🔄",
            (text, _) => StructuredTransformers.JsonToYaml(text));

        Register("YamlToJson", "Convert YAML ➔ JSON", "Structured", "Convert YAML document into formatted JSON", "🔄",
            (text, _) => StructuredTransformers.YamlToJson(text));

        Register("XmlToJson", "Convert XML ➔ JSON", "Structured", "Convert XML document into JSON", "🔄",
            (text, _) => StructuredTransformers.XmlToJson(text));

        Register("JsonToXml", "Convert JSON ➔ XML", "Structured", "Convert JSON document into XML", "🔄",
            (text, _) => StructuredTransformers.JsonToXml(text));

        Register("FlattenJson", "Flatten JSON", "Structured", "Flatten nested JSON object into dot-notation paths", "📜",
            (text, _) => StructuredTransformers.Flatten(text, "."));

        Register("UnflattenJson", "Unflatten JSON", "Structured", "Reconstruct nested JSON from dot-notation paths", "📜",
            (text, _) => StructuredTransformers.Unflatten(text, "JSON"));

        Register("SortJsonKeys", "Sort JSON Keys", "Structured", "Recursively sort object property keys alphabetically", "🔤",
            (text, _) => StructuredTransformers.SortKeys(text, false));

        Register("QueryStringToKv", "Query String ➔ Key-Value", "Structured", "Parse URL query parameters into key: value lines", "🌐",
            (text, _) => DeveloperTransformers.QueryStringToKeyValuePairs(text));

        Register("KvToJson", "Key-Value ➔ JSON Object", "Structured", "Convert key: value lines into JSON object", "📦",
            (text, _) => DeveloperTransformers.KeyValuePairsToJson(text));

        Register("ToTypeScriptInterfaces", "Generate TypeScript Interfaces", "Structured", "Generate TypeScript interface types from JSON", "📜",
            (text, _) => DeveloperTransformers.ToTypeScriptInterfaces(text, "Root"));

        Register("ToCSharpClasses", "Generate C# POCO Classes", "Structured", "Generate C# record/class models from JSON", "💻",
            (text, _) => DeveloperTransformers.ToCSharpClasses(text, "Root"));

        Register("ToJsonSchema", "Generate JSON Schema", "Structured", "Generate JSON Schema draft definition from JSON", "📋",
            (text, _) => DeveloperTransformers.ToJsonSchema(text, "Schema"));

        // -------------------------------------------------------------
        // Tabular & Tables
        // -------------------------------------------------------------
        Register("TableToJsonObjects", "Table ➔ JSON Objects", "Tabular", "Convert CSV/table into JSON array of objects", "📦",
            (text, _) =>
            {
                var dataset = TabularParser.DetectAndParse(text);
                return dataset != null ? TabularConverter.ToJsonArrayOfObjects(dataset) : text ?? string.Empty;
            });

        Register("TableToCsv", "Table ➔ CSV", "Tabular", "Convert table into CSV format", "📊",
            (text, _) =>
            {
                var dataset = TabularParser.DetectAndParse(text);
                return dataset != null ? TabularConverter.ToCsv(dataset, ',') : text ?? string.Empty;
            });

        Register("TableToTsv", "Table ➔ TSV", "Tabular", "Convert table into Tab-Separated Values", "📑",
            (text, _) =>
            {
                var dataset = TabularParser.DetectAndParse(text);
                return dataset != null ? TabularConverter.ToTsv(dataset) : text ?? string.Empty;
            });

        Register("TableToMarkdown", "Table ➔ Markdown Table", "Tabular", "Convert table into GitHub-flavored Markdown table", "📋",
            (text, _) =>
            {
                var dataset = TabularParser.DetectAndParse(text);
                return dataset != null ? TabularConverter.ToMarkdownTable(dataset) : text ?? string.Empty;
            });

        Register("TableToHtml", "Table ➔ HTML Table", "Tabular", "Convert table into HTML <table> structure", "🌐",
            (text, _) =>
            {
                var dataset = TabularParser.DetectAndParse(text);
                return dataset != null ? TabularConverter.ToHtmlTable(dataset) : text ?? string.Empty;
            });

        Register("TableToSqlInserts", "Table ➔ SQL INSERTs", "Tabular", "Convert table into SQL INSERT statements", "🗄️",
            (text, _) =>
            {
                var dataset = TabularParser.DetectAndParse(text);
                return dataset != null ? TabularConverter.ToSqlInsertStatements(dataset, "Records") : text ?? string.Empty;
            });
    }

    public static void Register(
        string actionId,
        string title,
        string category,
        string description,
        string icon,
        Func<string?, Dictionary<string, string>, string> executor,
        Dictionary<string, string>? defaultParameters = null)
    {
        var item = new RecipeCatalogItem
        {
            ActionId = actionId,
            Title = title,
            Category = category,
            Description = description,
            Icon = icon,
            DefaultParameters = defaultParameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        _catalogItems.Add(item);
        _stepExecutors[actionId] = executor;
    }

    public static IReadOnlyList<RecipeCatalogItem> GetAllCatalogItems() => _catalogItems;

    public static RecipeCatalogItem? FindCatalogItem(string actionId)
    {
        return _catalogItems.FirstOrDefault(i => string.Equals(i.ActionId, actionId, StringComparison.OrdinalIgnoreCase));
    }

    public static string ExecuteStep(RecipeStep step, string? input)
    {
        if (string.IsNullOrEmpty(step.ActionId)) return input ?? string.Empty;

        if (_stepExecutors.TryGetValue(step.ActionId, out var executor))
        {
            return executor(input, step.Parameters);
        }

        return input ?? string.Empty;
    }

    private static string ExtractUrls(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var matches = Regex.Matches(text, """https?://[^\s<>"'{}|\\^`\[\]]+""", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var results = new List<string>();
        foreach (Match m in matches)
        {
            string url = m.Value.TrimEnd('.', ',', '!', '?', ';', ':', ')', ']');
            if (!string.IsNullOrWhiteSpace(url))
            {
                results.Add(url);
            }
        }
        return string.Join(Environment.NewLine, results);
    }

    private static string ExtractPattern(string? text, string regexPattern)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var matches = Regex.Matches(text, regexPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var results = new List<string>();
        foreach (Match m in matches)
        {
            if (!string.IsNullOrWhiteSpace(m.Value))
            {
                results.Add(m.Value);
            }
        }
        return string.Join(Environment.NewLine, results);
    }
}
