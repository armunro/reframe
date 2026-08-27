using System.Text.RegularExpressions;
using Reframe.Core.Tabular.Models;

namespace Reframe.Core.Tabular.Parsers;

public static class MarkdownTableParser
{
    private static readonly Regex SeparatorRowRegex = new(
        @"^\s*(?:\|\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?|:?-+:?\s*(\|\s*:?-+:?\s*)+\|?|:?-+:?\s*\|\s*)\s*$",
        RegexOptions.Compiled);

    public static bool IsMarkdownTable(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return false;

        // Check if line 1 (second line) matches markdown separator row like `|---|---|` or `---|---`
        for (int i = 0; i < Math.Min(3, lines.Length); i++)
        {
            if (SeparatorRowRegex.IsMatch(lines[i]))
            {
                return true;
            }
        }

        return false;
    }

    public static TabularData Parse(string? text, bool? assumeHeader = null)
    {
        bool hasHeaders = assumeHeader ?? true;
        var result = new TabularData { Delimiter = '|', HasHeaders = hasHeaders };
        if (string.IsNullOrWhiteSpace(text)) return result;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return result;

        int separatorIndex = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (SeparatorRowRegex.IsMatch(lines[i]))
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex > 0)
        {
            var headerCells = SplitMarkdownRow(lines[separatorIndex - 1]);
            var dataRows = new List<List<string>>();

            for (int i = separatorIndex + 1; i < lines.Length; i++)
            {
                var rowCells = SplitMarkdownRow(lines[i]);
                if (rowCells.Count > 0)
                {
                    while (rowCells.Count < headerCells.Count)
                    {
                        rowCells.Add(string.Empty);
                    }
                    dataRows.Add(rowCells.Take(headerCells.Count).ToList());
                }
            }

            if (hasHeaders)
            {
                result.Columns = headerCells;
                result.Rows = dataRows;
            }
            else
            {
                int maxCols = headerCells.Count;
                result.Columns = Enumerable.Range(1, maxCols).Select(i => $"Column {i}").ToList();
                var allRows = new List<List<string>> { headerCells };
                allRows.AddRange(dataRows);
                result.Rows = allRows;
            }
        }
        else
        {
            // Parse as simple pipe-delimited table
            var rows = lines.Select(SplitMarkdownRow).Where(r => r.Count > 0).ToList();
            if (rows.Count > 0)
            {
                if (hasHeaders)
                {
                    result.Columns = rows[0];
                    result.Rows = rows.Skip(1).ToList();
                }
                else
                {
                    int maxCols = rows.Max(r => r.Count);
                    result.Columns = Enumerable.Range(1, maxCols).Select(i => $"Column {i}").ToList();
                    result.Rows = rows;
                }
            }
        }

        return result;
    }

    private static List<string> SplitMarkdownRow(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.StartsWith('|')) trimmed = trimmed[1..];
        if (trimmed.EndsWith('|')) trimmed = trimmed[..^1];

        return trimmed.Split('|').Select(c => c.Trim()).ToList();
    }
}
