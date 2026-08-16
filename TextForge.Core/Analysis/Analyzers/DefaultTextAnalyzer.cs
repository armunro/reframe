using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TextForge.Core.Tabular;
using TextForge.Core.Transformers;

namespace TextForge.Core.Analysis;

public class DefaultTextAnalyzer : ITextAnalyzer
{
    private static readonly Regex WordRegex = new(@"\b\S+\b", RegexOptions.Compiled);
    private static readonly Regex SqlInRegex = new(@"^\s*(?:where\s+\w+\s+)?in\s*\((.*)\)\s*;?$", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex KeyValueRegex = new(@"^[\w\.-]+\s*[:=]\s*.+$", RegexOptions.Compiled);

    public static DefaultTextAnalyzer Instance { get; } = new();

    public TextAnalysisResult Analyze(string? text, bool? hasHeaders = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new TextAnalysisResult
            {
                Format = DetectedFormat.Empty,
                FormatDescription = "Empty",
                CharacterCount = 0,
                CharacterCountNoSpaces = 0,
                LineCount = 0,
                NonEmptyLineCount = 0,
                WordCount = 0,
                DistinctLineCount = 0,
                HasHeaders = hasHeaders ?? true
            };
        }

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();

        int charCount = text.Length;
        int charCountNoSpaces = text.Count(c => !char.IsWhiteSpace(c));
        int lineCount = lines.Length;
        int nonEmptyLineCount = nonEmptyLines.Count;
        int wordCount = WordRegex.Matches(text).Count;
        var distinctLines = nonEmptyLines.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        int distinctCount = distinctLines.Count;
        int duplicateCount = nonEmptyLineCount - distinctCount;
        bool hasDuplicates = duplicateCount > 0;

        // Detect Format
        var (format, formatDesc, delimiter, isTabular, colCount, rowCount, sampleHeaders, actualHasHeaders) = DetectFormat(text, lines, nonEmptyLines, hasHeaders);

        return new TextAnalysisResult
        {
            CharacterCount = charCount,
            CharacterCountNoSpaces = charCountNoSpaces,
            LineCount = lineCount,
            NonEmptyLineCount = nonEmptyLineCount,
            WordCount = wordCount,
            DistinctLineCount = distinctCount,
            HasDuplicates = hasDuplicates,
            DuplicateCount = duplicateCount,
            Format = format,
            FormatDescription = formatDesc,
            DetectedDelimiter = delimiter,
            IsTabular = isTabular,
            HasHeaders = actualHasHeaders,
            ColumnCount = colCount,
            RowCount = rowCount,
            SampleHeaders = sampleHeaders
        };
    }

    private static (DetectedFormat Format, string Description, char? Delimiter, bool IsTabular, int ColumnCount, int RowCount, IReadOnlyList<string> SampleHeaders, bool HasHeaders)
        DetectFormat(string rawText, string[] lines, List<string> nonEmptyLines, bool? hasHeaders = null)
    {
        var trimmed = rawText.Trim();

        // 1. HTML Table
        if (HtmlTableParser.IsHtmlTable(rawText))
        {
            var htmlTable = HtmlTableParser.Parse(rawText, hasHeaders);
            if (htmlTable.Columns.Count > 0 && (htmlTable.Rows.Count > 0 || htmlTable.Columns.Count > 1))
            {
                return (DetectedFormat.HtmlTable, $"HTML Table ({htmlTable.Rows.Count} rows, {htmlTable.Columns.Count} cols)", null, true, htmlTable.Columns.Count, htmlTable.Rows.Count, htmlTable.Columns, htmlTable.HasHeaders);
            }
        }

        // 1b. XML Document
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            try
            {
                var xdoc = XDocument.Parse(trimmed);
                if (xdoc.Root != null)
                {
                    int elementCount = xdoc.Descendants().Count();
                    string rootTag = xdoc.Root.Name.LocalName;
                    return (DetectedFormat.Xml, $"XML Document (<{rootTag}>, {elementCount} elements)", null, false, 0, 0, Array.Empty<string>(), true);
                }
            }
            catch
            {
                // Not well-formed XML, continue detection
            }
        }

        // 2. JSON
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    int arrayLen = doc.RootElement.GetArrayLength();
                    return (DetectedFormat.Json, $"JSON Array ({arrayLen} items)", null, false, 0, arrayLen, Array.Empty<string>(), true);
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    int propCount = doc.RootElement.EnumerateObject().Count();
                    return (DetectedFormat.Json, $"JSON Object ({propCount} keys)", null, false, propCount, 0, Array.Empty<string>(), true);
                }
            }
            catch
            {
                // Not valid JSON, continue detection
            }
        }

        // 2b. YAML Detection
        if (TextBeautifier.IsYaml(rawText))
        {
            var yamlTable = TabularParser.TryParseYaml(rawText, hasHeaders);
            if (yamlTable != null && yamlTable.Columns.Count > 0 && yamlTable.Rows.Count > 0)
            {
                return (DetectedFormat.Yaml, $"YAML Array of Objects ({yamlTable.Rows.Count} items, {yamlTable.Columns.Count} properties)", null, true, yamlTable.Columns.Count, yamlTable.Rows.Count, yamlTable.Columns, true);
            }
            if (trimmed.StartsWith('-'))
            {
                return (DetectedFormat.Yaml, $"YAML List ({nonEmptyLines.Count} items)", null, false, 0, 0, Array.Empty<string>(), false);
            }
            return (DetectedFormat.Yaml, "YAML Document", null, false, 0, 0, Array.Empty<string>(), true);
        }

        // 3. Markdown Table
        if (MarkdownTableParser.IsMarkdownTable(rawText))
        {
            var table = MarkdownTableParser.Parse(rawText, hasHeaders);
            if (table.Columns.Count > 0)
            {
                return (DetectedFormat.MarkdownTable, $"Markdown Table ({table.Rows.Count} rows, {table.Columns.Count} cols)", '|', true, table.Columns.Count, table.Rows.Count, table.Columns, table.HasHeaders);
            }
        }

        // 3. SQL IN clause
        if (SqlInRegex.IsMatch(trimmed))
        {
            return (DetectedFormat.SqlInClause, "SQL IN (...) clause", ',', false, 0, 0, Array.Empty<string>(), false);
        }

        // 4. Single Line vs Multi-Line
        if (nonEmptyLines.Count == 1)
        {
            string singleLine = nonEmptyLines[0];

            // Delimited single line?
            char[] testDelims = { ',', '\t', ';', '|' };
            foreach (var d in testDelims)
            {
                var parts = singleLine.Split(d);
                if (parts.Length >= 2)
                {
                    string dName = d == '\t' ? "Tab" : d == ',' ? "Comma" : d == ';' ? "Semicolon" : "Pipe";
                    return (DetectedFormat.DelimitedSingleLine, $"Single Line ({dName}-separated, {parts.Length} items)", d, false, parts.Length, 1, Array.Empty<string>(), false);
                }
            }

            return (DetectedFormat.SingleLine, "Single Line Text", null, false, 1, 1, Array.Empty<string>(), false);
        }

        // 5. Multi-line Tabular (CSV / TSV / Delimited)
        if (nonEmptyLines.Count >= 2)
        {
            var tabular = TabularParser.DetectAndParse(rawText, hasHeaders);
            if (tabular != null && tabular.Columns.Count > 1 && tabular.Rows.Count > 0)
            {
                string formatName = tabular.Delimiter == '\t' ? "TSV Table" : tabular.Delimiter == ',' ? "CSV Table" : "Delimited Table";
                var detectedFormat = tabular.Delimiter == '\t' ? DetectedFormat.TsvTable : DetectedFormat.CsvTable;
                return (detectedFormat, $"{formatName} ({tabular.Rows.Count} rows, {tabular.Columns.Count} cols)", tabular.Delimiter, true, tabular.Columns.Count, tabular.Rows.Count, tabular.Columns, tabular.HasHeaders);
            }

            // Key-Value pairs
            if (nonEmptyLines.Count >= 2 && nonEmptyLines.All(l => KeyValueRegex.IsMatch(l)))
            {
                return (DetectedFormat.KeyValuePairs, $"Key-Value Pairs ({nonEmptyLines.Count} entries)", null, false, 2, nonEmptyLines.Count, new[] { "Key", "Value" }, true);
            }

            // All numbers
            if (nonEmptyLines.All(l => double.TryParse(l.Trim('"', '\'', ' ', ','), out _) || (l.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(l[2..], System.Globalization.NumberStyles.HexNumber, null, out _))))
            {
                return (DetectedFormat.MultiLineNumbers, $"Multi-Line Numbers ({nonEmptyLines.Count} rows)", null, false, 1, nonEmptyLines.Count, Array.Empty<string>(), false);
            }

            return (DetectedFormat.MultiLineList, $"Multi-Line Text List ({nonEmptyLines.Count} rows)", null, false, 1, nonEmptyLines.Count, Array.Empty<string>(), false);
        }

        return (DetectedFormat.SingleLine, "Single Line", null, false, 1, 1, Array.Empty<string>(), false);
    }
}
