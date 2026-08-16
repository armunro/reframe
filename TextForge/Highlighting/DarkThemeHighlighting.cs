using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace TextForge.Highlighting;

public static class DarkThemeHighlighting
{
    private static readonly Dictionary<string, IHighlightingDefinition> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object SyncLock = new();

    public static readonly IReadOnlyList<string> SupportedLanguages = new[]
    {
        "Auto",
        "Plain Text",
        "CSV",
        "TSV",
        "JSON",
        "SQL",
        "C#",
        "JavaScript",
        "TypeScript",
        "Python",
        "HTML",
        "XML",
        "Markdown",
        "CSS",
        "PowerShell",
        "C++",
        "Java",
        "PHP"
    };

    public static IHighlightingDefinition? GetDefinition(string? languageName)
    {
        if (string.IsNullOrWhiteSpace(languageName) ||
            languageName.Equals("None", StringComparison.OrdinalIgnoreCase) ||
            languageName.Equals("Plain Text", StringComparison.OrdinalIgnoreCase) ||
            languageName.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        lock (SyncLock)
        {
            string canonical = NormalizeLanguageName(languageName);
            if (Cache.TryGetValue(canonical, out var cached))
            {
                return cached;
            }

            IHighlightingDefinition? definition = null;

            if (canonical.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            {
                definition = CreateDelimitedDefinition("CSV", new[] { ".csv" }, ',');
            }
            else if (canonical.Equals("TSV", StringComparison.OrdinalIgnoreCase))
            {
                definition = CreateDelimitedDefinition("TSV", new[] { ".tsv", ".tab" }, '\t');
            }
            else
            {
                var baseDefinition = HighlightingManager.Instance.GetDefinition(canonical)
                    ?? HighlightingManager.Instance.GetDefinitionByExtension("." + canonical.ToLowerInvariant());

                if (baseDefinition != null)
                {
                    definition = ApplyDarkThemeColors(baseDefinition);
                }
            }

            if (definition != null)
            {
                Cache[canonical] = definition;
            }

            return definition;
        }
    }

    public static string NormalizeLanguageName(string name)
    {
        return name.Trim().ToLowerInvariant() switch
        {
            "csv" => "CSV",
            "tsv" or "tab" => "TSV",
            "json" => "Json",
            "c#" or "csharp" or "cs" => "C#",
            "sql" or "tsql" => "TSQL",
            "javascript" or "js" => "JavaScript",
            "typescript" or "ts" => "JavaScript",
            "python" or "py" => "Python",
            "html" or "htm" => "HTML",
            "xml" or "xaml" or "svg" or "config" => "XML",
            "markdown" or "md" => "MarkDown",
            "css" => "CSS",
            "powershell" or "ps1" or "posh" => "PowerShell",
            "c++" or "cpp" or "cplusplus" => "C++",
            "java" => "Java",
            "php" => "PHP",
            _ => name
        };
    }

    private static IHighlightingDefinition CreateDelimitedDefinition(string name, string[] extensions, char delimiter)
    {
        string delimiterEscaped = delimiter == '\t' ? @"\t" : @",";
        string delimiterDisplay = delimiter == '\t' ? @"\t" : @",";

        // XSHD definition for CSV / TSV with rich dark-theme colors:
        // - Quoted strings (warm orange #CE9178)
        // - Delimiters (bold light blue / cyan #569CD6)
        // - Numbers / numeric cells (soft green #B5CEA8)
        // - Booleans / null keywords (purple #C586C0)
        // - Date / timestamps (teal #4EC9B0)
        // - Default unquoted field text (foreground #D4D4D4)
        string xshd = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<SyntaxDefinition name=""{name}"" extensions=""{string.Join(";", extensions)}"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Delimiter"" foreground=""#569CD6"" fontWeight=""bold"" />
    <Color name=""QuotedString"" foreground=""#CE9178"" />
    <Color name=""Number"" foreground=""#B5CEA8"" />
    <Color name=""BooleanOrNull"" foreground=""#C586C0"" fontWeight=""bold"" />
    <Color name=""DateTime"" foreground=""#4EC9B0"" />
    <Color name=""HeaderComment"" foreground=""#6A9955"" fontStyle=""italic"" />

    <RuleSet>
        <!-- Comments (lines starting with # or //) -->
        <Span color=""HeaderComment"" begin=""^(#|//)"" end=""$"" />

        <!-- Quoted strings (supports escaped quotes: """" or \"") -->
        <Span color=""QuotedString"">
            <Begin>&quot;</Begin>
            <End>&quot;</End>
            <RuleSet>
                <Span begin=""&quot;&quot;"" />
                <Span begin=""\\&quot;"" />
                <Span begin=""\\\\"" />
            </RuleSet>
        </Span>

        <!-- Delimiters -->
        <Rule color=""Delimiter"">
            {delimiterEscaped}
        </Rule>

        <!-- Dates / ISO Timestamps (e.g. 2026-08-15, 2026-08-15T20:09:00Z, 2026/08/15) -->
        <Rule color=""DateTime"">
            \b\d{{4}}[-/]\d{{1,2}}[-/]\d{{1,2}}(?:[T ]\d{{1,2}}:\d{{2}}(?::\d{{2}}(?:\.\d+)?)?(?:Z|[+-]\d{{2}}:?\d{{2}})?)?\b
        </Rule>

        <!-- Numbers (integers, decimals, scientific notation, negative, hex/currencies) -->
        <Rule color=""Number"">
            (?&lt;=[{delimiterEscaped}^\s]|^)[+-]?(?:\$\s*)?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?%?(?=[{delimiterEscaped}\s\r\n]|$)
        </Rule>

        <!-- Booleans and Nulls -->
        <Keywords color=""BooleanOrNull"">
            <Word>true</Word>
            <Word>false</Word>
            <Word>TRUE</Word>
            <Word>FALSE</Word>
            <Word>True</Word>
            <Word>False</Word>
            <Word>null</Word>
            <Word>NULL</Word>
            <Word>Null</Word>
            <Word>nil</Word>
            <Word>NIL</Word>
            <Word>NA</Word>
            <Word>N/A</Word>
            <Word>NaN</Word>
            <Word>None</Word>
            <Word>NONE</Word>
        </Keywords>
    </RuleSet>
</SyntaxDefinition>";

        using var stringReader = new StringReader(xshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }

    private static IHighlightingDefinition ApplyDarkThemeColors(IHighlightingDefinition definition)
    {
        // Dark theme color palette (modern VS Code / Rider dark style)
        var keywordBrush = new SimpleHighlightingBrush(Color.FromRgb(0x56, 0x9C, 0xD6));       // #569CD6 Blue
        var controlFlowBrush = new SimpleHighlightingBrush(Color.FromRgb(0xC5, 0x86, 0xC0));   // #C586C0 Purple
        var stringBrush = new SimpleHighlightingBrush(Color.FromRgb(0xCE, 0x91, 0x78));        // #CE9178 Warm Orange
        var commentBrush = new SimpleHighlightingBrush(Color.FromRgb(0x6A, 0x99, 0x55));       // #6A9955 Green
        var numberBrush = new SimpleHighlightingBrush(Color.FromRgb(0xB5, 0xCE, 0xA8));        // #B5CEA8 Soft Green
        var typeBrush = new SimpleHighlightingBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));          // #4EC9B0 Teal
        var methodBrush = new SimpleHighlightingBrush(Color.FromRgb(0xDC, 0xDC, 0xAA));        // #DCDCAA Yellow
        var propertyBrush = new SimpleHighlightingBrush(Color.FromRgb(0x9C, 0xDC, 0xFE));      // #9CDCFE Light Blue
        var punctuationBrush = new SimpleHighlightingBrush(Color.FromRgb(0xD4, 0xD4, 0xD4));   // #D4D4D4 Default Foreground
        var preprocessorBrush = new SimpleHighlightingBrush(Color.FromRgb(0x9B, 0x9B, 0x9B));  // #9B9B9B Gray

        foreach (var color in definition.NamedHighlightingColors)
        {
            var name = color.Name?.ToLowerInvariant() ?? "";

            if (name.Contains("comment") || name.Contains("doc"))
            {
                color.Foreground = commentBrush;
            }
            else if (name.Contains("string") || name.Contains("char") || name.Contains("value"))
            {
                color.Foreground = stringBrush;
            }
            else if (name.Contains("control") || name.Contains("goto") || name.Contains("keyword1"))
            {
                color.Foreground = controlFlowBrush;
            }
            else if (name.Contains("keyword") || name.Contains("reserved") || name.Contains("statement"))
            {
                color.Foreground = keywordBrush;
            }
            else if (name.Contains("type") || name.Contains("class") || name.Contains("interface") || name.Contains("struct"))
            {
                color.Foreground = typeBrush;
            }
            else if (name.Contains("method") || name.Contains("function") || name.Contains("call"))
            {
                color.Foreground = methodBrush;
            }
            else if (name.Contains("number") || name.Contains("digit") || name.Contains("literal"))
            {
                color.Foreground = numberBrush;
            }
            else if (name.Contains("property") || name.Contains("attribute") || name.Contains("param"))
            {
                color.Foreground = propertyBrush;
            }
            else if (name.Contains("tag") || name.Contains("element"))
            {
                color.Foreground = keywordBrush;
            }
            else if (name.Contains("preprocessor") || name.Contains("directive"))
            {
                color.Foreground = preprocessorBrush;
            }
            else if (name.Contains("punctuation") || name.Contains("delimiter"))
            {
                color.Foreground = punctuationBrush;
            }
            else
            {
                // Ensure foreground has readable contrast if it was dark in light theme
                if (color.Foreground is SimpleHighlightingBrush simpleBrush)
                {
                    var c = simpleBrush.GetColor(null);
                    if (c.HasValue && c.Value.R < 80 && c.Value.G < 80 && c.Value.B < 80)
                    {
                        color.Foreground = punctuationBrush;
                    }
                }
            }
        }

        return definition;
    }
}
