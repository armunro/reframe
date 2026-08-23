using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace Reframe.Highlighting;

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
        "YAML",
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
            else if (canonical.Equals("YAML", StringComparison.OrdinalIgnoreCase))
            {
                definition = CreateYamlDefinition();
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
            "yaml" or "yml" => "YAML",
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

        // XSHD definition for CSV / TSV with rich dark navy theme colors:
        // - Quoted strings (warm orange #CE9178)
        // - Delimiters (bold slate blue #668BC4)
        // - Numbers / numeric cells (soft sage green #B5CEA8)
        // - Booleans / null keywords (purple #A78BFA)
        // - Date / timestamps (teal #4EC9B0)
        // - Comments (slate gray #5C6682)
        // - Default unquoted field text (crisp foreground #DCE1EB)
        string xshd = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<SyntaxDefinition name=""{name}"" extensions=""{string.Join(";", extensions)}"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Delimiter"" foreground=""#668BC4"" fontWeight=""bold"" />
    <Color name=""QuotedString"" foreground=""#CE9178"" />
    <Color name=""Number"" foreground=""#B5CEA8"" />
    <Color name=""BooleanOrNull"" foreground=""#A78BFA"" fontWeight=""bold"" />
    <Color name=""DateTime"" foreground=""#4EC9B0"" />
    <Color name=""HeaderComment"" foreground=""#5C6682"" fontStyle=""italic"" />

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

    private static IHighlightingDefinition CreateYamlDefinition()
    {
        string xshd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<SyntaxDefinition name=""YAML"" extensions="".yaml;.yml"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Comment"" foreground=""#5C6682"" fontStyle=""italic"" />
    <Color name=""QuotedString"" foreground=""#CE9178"" />
    <Color name=""Key"" foreground=""#7DCFFF"" />
    <Color name=""Delimiter"" foreground=""#668BC4"" fontWeight=""bold"" />
    <Color name=""Number"" foreground=""#B5CEA8"" />
    <Color name=""BooleanOrNull"" foreground=""#A78BFA"" fontWeight=""bold"" />
    <Color name=""Directive"" foreground=""#6B7CA8"" />

    <RuleSet>
        <!-- Comments -->
        <Span color=""Comment"" begin=""#"" end=""$"" />

        <!-- Double quoted strings -->
        <Span color=""QuotedString"">
            <Begin>&quot;</Begin>
            <End>&quot;</End>
            <RuleSet>
                <Span begin=""\\&quot;"" />
                <Span begin=""\\\\"" />
            </RuleSet>
        </Span>

        <!-- Single quoted strings -->
        <Span color=""QuotedString"">
            <Begin>'</Begin>
            <End>'</End>
            <RuleSet>
                <Span begin=""''"" />
            </RuleSet>
        </Span>

        <!-- Document headers and separators -->
        <Rule color=""Directive"">
            ^(?:---|(\.\.\.))(?=\s|$)
        </Rule>

        <!-- Mapping Keys: key before colon -->
        <Rule color=""Key"">
            (?:^[ \t]*|[ \t]*-?[ \t]+)(?:[\w\.\-]+)(?=\s*:)
        </Rule>

        <!-- Colons / Delimiters -->
        <Rule color=""Delimiter"">
            [:\-\[\]\{\},]
        </Rule>

        <!-- Numbers (integer, float, hex, scientific) -->
        <Rule color=""Number"">
            (?&lt;=[\s:\-\[\{]|^)[+-]?(?:0x[\da-fA-F]+|\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)(?=\s*[,\]\}\r\n]|$)
        </Rule>

        <!-- Booleans, Nulls, Special Constants -->
        <Keywords color=""BooleanOrNull"">
            <Word>true</Word>
            <Word>false</Word>
            <Word>TRUE</Word>
            <Word>FALSE</Word>
            <Word>True</Word>
            <Word>False</Word>
            <Word>yes</Word>
            <Word>no</Word>
            <Word>YES</Word>
            <Word>NO</Word>
            <Word>null</Word>
            <Word>NULL</Word>
            <Word>Null</Word>
            <Word>~</Word>
        </Keywords>
    </RuleSet>
</SyntaxDefinition>";

        using var stringReader = new StringReader(xshd);
        using var xmlReader = XmlReader.Create(stringReader);
        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }

    private static IHighlightingDefinition ApplyDarkThemeColors(IHighlightingDefinition definition)
    {
        // Desaturated dark navy theme color palette (Calm Slate Blue rgb(30, 32, 48) style)
        var keywordBrush = new SimpleHighlightingBrush(Color.FromRgb(0x66, 0x8B, 0xC4));       // #668BC4 Slate Blue
        var controlFlowBrush = new SimpleHighlightingBrush(Color.FromRgb(0xA7, 0x8B, 0xFA));   // #A78BFA Soft Lavender
        var stringBrush = new SimpleHighlightingBrush(Color.FromRgb(0xCE, 0x91, 0x78));        // #CE9178 Warm Orange
        var commentBrush = new SimpleHighlightingBrush(Color.FromRgb(0x5C, 0x66, 0x82));       // #5C6682 Slate Gray
        var numberBrush = new SimpleHighlightingBrush(Color.FromRgb(0xB5, 0xCE, 0xA8));        // #B5CEA8 Soft Sage Green
        var typeBrush = new SimpleHighlightingBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));          // #4EC9B0 Teal
        var methodBrush = new SimpleHighlightingBrush(Color.FromRgb(0xDC, 0xDC, 0xAA));        // #DCDCAA Soft Gold / Yellow
        var propertyBrush = new SimpleHighlightingBrush(Color.FromRgb(0x7D, 0xCF, 0xFF));      // #7DCFFF Light Cyan Blue
        var punctuationBrush = new SimpleHighlightingBrush(Color.FromRgb(0xDC, 0xE1, 0xEB));   // #DCE1EB Crisp Slate Foreground
        var preprocessorBrush = new SimpleHighlightingBrush(Color.FromRgb(0x6B, 0x7C, 0xA8));  // #6B7CA8 Muted Slate
        var linkBrush = new SimpleHighlightingBrush(Color.FromRgb(0x4E, 0xA6, 0xEA));          // #4EA6EA Bright Sky Blue / Link

        foreach (var color in definition.NamedHighlightingColors)
        {
            var name = color.Name?.ToLowerInvariant() ?? "";

            if (name.Contains("comment") || name.Contains("doc"))
            {
                color.Foreground = commentBrush;
            }
            else if (name.Contains("link") || name.Contains("url") || name.Contains("uri") || name.Contains("hyperlink") || name.Contains("href"))
            {
                color.Foreground = linkBrush;
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
            else if (name.Contains("string") || name.Contains("char"))
            {
                color.Foreground = stringBrush;
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
